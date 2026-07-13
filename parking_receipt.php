<?php
error_reporting(E_ALL);
ini_set("display_errors", 1);

define("API_URL", "http://whirl.uz/smartparking/api/Transactions");

$translations = [
    "uz" => [
        "title" => "Parkovka Cheki", "sub" => "Avtoturargoh to'lov cheki",
        "check_id" => "Chek ID", "car_num" => "Avto Raqam",
        "entry" => "Kirish", "exit" => "Chiqish", "duration" => "Davomiyligi",
        "total" => "Jami to'lov", "cancel" => "Bekor qilish", "pay" => "To'lash",
        "menu" => "Asosiy menyu", "msg_success" => "To'lov qabul qilindi!",
        "msg_cancel" => "To'lov bekor qilindi.",
    ],
    "ru" => [
        "title" => "Парковочный чек", "sub" => "Чек оплаты парковки",
        "check_id" => "ID чека", "car_num" => "Номер авто",
        "entry" => "Въезд", "exit" => "Выезд", "duration" => "Длительность",
        "total" => "Итого к оплате", "cancel" => "Отмена", "pay" => "Оплатить",
        "menu" => "Главное меню", "msg_success" => "Оплата принята!",
        "msg_cancel" => "Оплата отменена.",
    ],
    "en" => [
        "title" => "Parking Receipt", "sub" => "Parking payment receipt",
        "check_id" => "Check ID", "car_num" => "Car Number",
        "entry" => "Entry", "exit" => "Exit", "duration" => "Duration",
        "total" => "Total amount", "cancel" => "Cancel", "pay" => "Pay",
        "menu" => "Main menu", "msg_success" => "Payment accepted!",
        "msg_cancel" => "Payment cancelled.",
    ],
];

$lang = isset($_GET["lang"]) && isset($translations[$_GET["lang"]]) ? $_GET["lang"] : "uz";
$t = $translations[$lang];

function apiCall($url, $method = "POST", $data = null) {
    $ch = curl_init($url);
    curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
    curl_setopt($ch, CURLOPT_TIMEOUT, 10);
    curl_setopt($ch, CURLOPT_SSL_VERIFYPEER, false);
    curl_setopt($ch, CURLOPT_SSL_VERIFYHOST, false);
    if ($method === "POST") {
        curl_setopt($ch, CURLOPT_POST, true);
        if ($data) {
            curl_setopt($ch, CURLOPT_HTTPHEADER, ["Content-Type: application/json"]);
            curl_setopt($ch, CURLOPT_POSTFIELDS, json_encode($data));
        }
    }
    $response = curl_exec($ch);
    $code = curl_getinfo($ch, CURLINFO_HTTP_CODE);
    curl_close($ch);
    return ["code" => $code, "body" => json_decode($response, true)];
}

function generateDemoData() {
    $regions = ["01","10","20","30","40","50","60","70","80","90","95"];
    $letters = range("A", "Z");
    $hours = rand(1, 12);
    $entry = strtotime("-" . $hours . " hours");
    return [
        "check_id" => "CHK-" . rand(10000, 99999),
        "car_number" => $regions[array_rand($regions)] . $letters[array_rand($letters)] . str_pad(rand(0, 999), 3, "0", STR_PAD_LEFT) . $letters[array_rand($letters)] . $letters[array_rand($letters)],
        "entry_time" => date("d.m.Y H:i", $entry),
        "exit_time" => date("d.m.Y H:i"),
        "entry_iso" => date("c", $entry),
        "exit_iso" => date("c"),
        "hours" => $hours,
        "amount" => $hours * 5500,
    ];
}

$current = generateDemoData();
$action = isset($_GET["action"]) ? $_GET["action"] : "";
$txnId = isset($_GET["txn"]) ? intval($_GET["txn"]) : 0;

if ($action === "complete" && $txnId > 0) {
    apiCall(API_URL . "/{$txnId}/complete");
} elseif ($action === "fail" && $txnId > 0) {
    apiCall(API_URL . "/{$txnId}/fail");
} else {
    $payload = [
        "chekId" => $current["check_id"],
        "avtoRaqam" => $current["car_number"],
        "kirish" => $current["entry_iso"],
        "chiqish" => $current["exit_iso"],
        "davomiyligi" => $current["hours"] . " h",
        "jamiTolov" => $current["amount"],
    ];
    $apiResult = apiCall(API_URL . "/parking", "POST", $payload);
    $txnId = isset($apiResult["body"]["id"]) ? $apiResult["body"]["id"] : 0;
}

$isDone = $action === "complete" || $action === "fail";
?>
<!DOCTYPE html>
<html lang="<?php echo $lang; ?>">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title><?php echo $t["title"]; ?></title>
    <style>
        body { font-family: 'Segoe UI', sans-serif; background: #f4f7f6; display: flex; flex-direction: column; align-items: center; justify-content: center; min-height: 100vh; margin: 0; }
        .lang-switcher { margin-bottom: 20px; }
        .lang-switcher a { margin: 0 5px; text-decoration: none; color: #0066cc; font-weight: bold; }
        .receipt { background: #fff; width: 100%; max-width: 360px; padding: 25px; border-radius: 20px; box-shadow: 0 10px 25px rgba(0,0,0,0.1); }
        .info-row { display: flex; justify-content: space-between; padding: 10px 0; border-bottom: 1px solid #f9f9f9; }
        .total-box { background: #f8f9fa; padding: 15px; border-radius: 12px; margin: 20px 0; text-align: center; }
        .price { font-size: 24px; font-weight: 800; color: #2e7d32; display: block; }
        .actions { display: flex; gap: 10px; }
        .btn { flex: 1; padding: 12px; border: none; border-radius: 10px; cursor: pointer; font-weight: 600; font-size: 15px; text-decoration: none; text-align: center; display: inline-block; }
        .btn-pay { background: #0066cc; color: white; }
        .btn-cancel { background: #e5e7eb; color: #374151; }
        .btn-menu { background: #0066cc; color: white; display: block; margin-top: 10px; }
        .status { text-align: center; font-size: 12px; color: #888; margin-top: 8px; }
        .result { text-align: center; padding: 40px 20px; }
        .result h2 { margin-bottom: 10px; }
    </style>
</head>
<body>
<div class="lang-switcher">
    <a href="?lang=uz">O'Z</a> | <a href="?lang=ru">RU</a> | <a href="?lang=en">EN</a>
</div>
<?php if ($isDone): ?>
    <div class="receipt result">
        <h2 style="color: <?php echo $action === "complete" ? "#2e7d32" : "#c62828"; ?>">
            <?php echo $action === "complete" ? "&#x2705;" : "&#x274C;"; ?>
            <?php echo $action === "complete" ? $t["msg_success"] : $t["msg_cancel"]; ?>
        </h2>
        <p style="color: #666">#<?php echo $txnId; ?></p>
        <a href="?" class="btn btn-menu"><?php echo $t["menu"]; ?></a>
    </div>
<?php else: ?>
    <div class="receipt">
        <div style="text-align:center;"><h2><?php echo $t["title"]; ?></h2><p><?php echo $t["sub"]; ?></p></div>
        <div class="info-row"><span><?php echo $t["check_id"]; ?>:</span> <b><?php echo $current["check_id"]; ?></b></div>
        <div class="info-row"><span><?php echo $t["car_num"]; ?>:</span> <b><?php echo $current["car_number"]; ?></b></div>
        <div class="info-row"><span><?php echo $t["entry"]; ?>:</span> <b><?php echo $current["entry_time"]; ?></b></div>
        <div class="info-row"><span><?php echo $t["exit"]; ?>:</span> <b><?php echo $current["exit_time"]; ?></b></div>
        <div class="info-row"><span><?php echo $t["duration"]; ?>:</span> <b><?php echo $current["hours"]; ?> h</b></div>
        <div class="total-box"><span><?php echo $t["total"]; ?>:</span><span class="price"><?php echo number_format($current["amount"], 0, ".", " "); ?> so'm</span></div>
        <div class="actions">
            <a href="?action=fail&txn=<?php echo $txnId; ?>&lang=<?php echo $lang; ?>" class="btn btn-cancel"><?php echo $t["cancel"]; ?></a>
            <a href="?action=complete&txn=<?php echo $txnId; ?>&lang=<?php echo $lang; ?>" class="btn btn-pay"><?php echo $t["pay"]; ?></a>
        </div>
        <div class="status">#<?php echo $txnId; ?></div>
    </div>
<?php endif; ?>
</body>
</html>
