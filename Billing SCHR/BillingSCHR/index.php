<?php
error_reporting(E_ALL);
ini_set("display_errors", 1);

define("API_URL", "http://localhost:5121/api/Transactions");

$translations = [
    "uz" => [
        "title" => "Tibbiy xizmat cheki",
        "sub" => "Gospital to'lov cheki",
        "check_id" => "Chek ID",
        "patient" => "Bemor",
        "department" => "Bo'lim",
        "diagnosis" => "Tashxis",
        "doctor" => "Shifokor",
        "amount" => "To'lov summasi",
        "cancel" => "Bekor qilish",
        "pay" => "To'lash",
        "menu" => "Asosiy menyu",
        "msg_success" => "To'lov qabul qilindi!",
        "msg_cancel" => "To'lov bekor qilindi.",
        "service" => "Xizmat turi",
    ],
    "ru" => [
        "title" => "Медицинский чек",
        "sub" => "Чек оплаты медицинских услуг",
        "check_id" => "ID чека",
        "patient" => "Пациент",
        "department" => "Отделение",
        "diagnosis" => "Диагноз",
        "doctor" => "Врач",
        "amount" => "Сумма к оплате",
        "cancel" => "Отмена",
        "pay" => "Оплатить",
        "menu" => "Главное меню",
        "msg_success" => "Оплата принята!",
        "msg_cancel" => "Оплата отменена.",
        "service" => "Тип услуги",
    ],
    "en" => [
        "title" => "Medical Receipt",
        "sub" => "Medical service payment receipt",
        "check_id" => "Check ID",
        "patient" => "Patient",
        "department" => "Department",
        "diagnosis" => "Diagnosis",
        "doctor" => "Doctor",
        "amount" => "Total amount",
        "cancel" => "Cancel",
        "pay" => "Pay",
        "menu" => "Main menu",
        "msg_success" => "Payment accepted!",
        "msg_cancel" => "Payment cancelled.",
        "service" => "Service type",
    ],
];

$lang =
    isset($_GET["lang"]) && isset($translations[$_GET["lang"]])
        ? $_GET["lang"]
        : "uz";
$t = $translations[$lang];

function apiCall($url, $method = "POST", $data = null)
{
    $ch = curl_init($url);
    curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
    curl_setopt($ch, CURLOPT_TIMEOUT, 10);
    curl_setopt($ch, CURLOPT_SSL_VERIFYPEER, false);
    curl_setopt($ch, CURLOPT_SSL_VERIFYHOST, false);
    if ($method === "POST") {
        curl_setopt($ch, CURLOPT_POST, true);
        if ($data) {
            curl_setopt($ch, CURLOPT_HTTPHEADER, [
                "Content-Type: application/json",
            ]);
            curl_setopt($ch, CURLOPT_POSTFIELDS, json_encode($data));
        }
    }
    $response = curl_exec($ch);
    $code = curl_getinfo($ch, CURLINFO_HTTP_CODE);
    $error = curl_error($ch);
    curl_close($ch);
    return [
        "code" => $code,
        "body" => json_decode($response, true),
        "error" => $error,
    ];
}

function generateDemoData()
{
    $departments = [
        "Хирургическое отделение",
        "Терапевтическое отделение",
        "Кардиологическое отделение",
        "Отделение реанимации",
        "Неврологическое отделение",
    ];
    $diagnoses = [
        "Острый аппендицит (K35)",
        "Гипертоническая болезнь (I10)",
        "Ишемическая болезнь сердца (I25)",
        "Пневмония (J18)",
        "Остеохондроз позвоночника (M42)",
    ];
    $doctors = [
        "Полковник Иванов А.В.",
        "Подполковник Петров С.Н.",
        "Майор Саидов Р.К.",
        "Капитан Алиев Б.Т.",
        "Подполковник Каримов Д.М.",
    ];
    $services = [
        "Консультация",
        "Лабораторный анализ",
        "Хирургическая операция",
        "Физиотерапия",
        "Диагностика",
    ];
    $amounts = [50000, 75000, 120000, 30000, 45000, 90000, 350000];

    return [
        "check_id" => "MED-" . rand(10000, 99999),
        "patient" => "Военнослужащий #" . rand(1000, 9999),
        "department" => $departments[array_rand($departments)],
        "diagnosis" => $diagnoses[array_rand($diagnoses)],
        "doctor" => $doctors[array_rand($doctors)],
        "service" => $services[array_rand($services)],
        "amount" => $amounts[array_rand($amounts)],
        "date" => date("d.m.Y H:i"),
    ];
}

$current = generateDemoData();

// --- ACTION HANDLER ---
$action = isset($_GET["action"]) ? $_GET["action"] : "";
$txnId = isset($_GET["txn"]) ? intval($_GET["txn"]) : 0;
$result = null;

if ($action === "complete" && $txnId > 0) {
    $result = apiCall(API_URL . "/{$txnId}/complete");
} elseif ($action === "fail" && $txnId > 0) {
    $result = apiCall(API_URL . "/{$txnId}/fail");
} else {
    // First load: create transaction
    $payload = [
        "chekId" => $current["check_id"],
        "patient" => $current["patient"],
        "department" => $current["department"],
        "diagnosis" => $current["diagnosis"],
        "doctor" => $current["doctor"],
        "service" => $current["service"],
        "jamiTolov" => $current["amount"],
    ];
    $apiResult = apiCall(API_URL . "/medical", "POST", $payload);
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
        .receipt { background: #fff; width: 100%; max-width: 380px; padding: 25px; border-radius: 20px; box-shadow: 0 10px 25px rgba(0,0,0,0.1); }
        .info-row { display: flex; justify-content: space-between; padding: 10px 0; border-bottom: 1px solid #f0f0f0; }
        .info-label { color: #666; font-size: 13px; }
        .info-value { font-weight: 600; color: #333; text-align: right; }
        .total-box { background: #f0f7ff; padding: 15px; border-radius: 12px; margin: 20px 0; text-align: center; border: 2px solid #0066cc; }
        .price { font-size: 26px; font-weight: 800; color: #0066cc; display: block; }
        .actions { display: flex; gap: 10px; }
        .btn { flex: 1; padding: 12px; border: none; border-radius: 10px; cursor: pointer; font-weight: 600; font-size: 15px; text-decoration: none; text-align: center; display: inline-block; transition: transform 0.1s; }
        .btn:hover { transform: scale(1.02); }
        .btn-pay { background: #0066cc; color: white; }
        .btn-cancel { background: #e5e7eb; color: #374151; }
        .btn-menu { background: #0066cc; color: white; display: block; margin-top: 10px; }
        .status { text-align: center; font-size: 12px; color: #888; margin-top: 8px; }
        .result { text-align: center; padding: 40px 20px; }
        .result h2 { margin-bottom: 10px; }
        .hospital-badge { background: #dc2626; color: white; padding: 4px 10px; border-radius: 20px; font-size: 10px; font-weight: 700; display: inline-block; margin-bottom: 10px; }
        .header-logo { text-align: center; margin-bottom: 5px; }
    </style>
</head>
<body>

<div class="lang-switcher">
    <a href="?lang=uz">O'Z</a> | <a href="?lang=ru">RU</a> | <a href="?lang=en">EN</a>
</div>

<?php if ($isDone): ?>
    <div class="receipt result">
        <h2 style="color: <?php echo $action === "complete"
            ? "#2e7d32"
            : "#c62828"; ?>">
            <?php echo $action === "complete" ? "&#x2705;" : "&#x274C;"; ?>
            <?php echo $action === "complete"
                ? $t["msg_success"]
                : $t["msg_cancel"]; ?>
        </h2>
        <p style="color: #666">#<?php echo $txnId; ?></p>
        <a href="?" class="btn btn-menu"><?php echo $t["menu"]; ?></a>
    </div>
<?php else: ?>
    <div class="receipt">
        <div class="header-logo">
            <span class="hospital-badge">MoD HOSPITAL</span>
        </div>
        <div style="text-align:center;"><h2><?php echo $t[
            "title"
        ]; ?></h2><p style="color:#888;font-size:12px;"><?php echo $t[
    "sub"
]; ?></p></div>

        <div class="info-row">
            <span class="info-label"><?php echo $t["check_id"]; ?>:</span>
            <span class="info-value"><?php echo $current["check_id"]; ?></span>
        </div>
        <div class="info-row">
            <span class="info-label"><?php echo $t["patient"]; ?>:</span>
            <span class="info-value"><?php echo $current["patient"]; ?></span>
        </div>
        <div class="info-row">
            <span class="info-label"><?php echo $t["department"]; ?>:</span>
            <span class="info-value"><?php echo $current["department"]; ?></span>
        </div>
        <div class="info-row">
            <span class="info-label"><?php echo $t["diagnosis"]; ?>:</span>
            <span class="info-value"><?php echo $current["diagnosis"]; ?></span>
        </div>
        <div class="info-row">
            <span class="info-label"><?php echo $t["doctor"]; ?>:</span>
            <span class="info-value"><?php echo $current["doctor"]; ?></span>
        </div>
        <div class="info-row">
            <span class="info-label"><?php echo $t["service"]; ?>:</span>
            <span class="info-value"><?php echo $current["service"]; ?></span>
        </div>
        <div class="info-row">
            <span class="info-label"><?php echo $t["date"] ?? "Sana"; ?>:</span>
            <span class="info-value"><?php echo $current["date"]; ?></span>
        </div>

        <div class="total-box">
            <span style="color:#666;"><?php echo $t["amount"]; ?>:</span>
            <span class="price"><?php echo number_format(
                $current["amount"],
                0,
                ".",
                " ",
            ); ?> so'm</span>
        </div>

        <div class="actions">
            <a href="?action=fail&txn=<?php echo $txnId; ?>&lang=<?php echo $lang; ?>" class="btn btn-cancel"><?php echo $t[
    "cancel"
]; ?></a>
            <a href="?action=complete&txn=<?php echo $txnId; ?>&lang=<?php echo $lang; ?>" class="btn btn-pay"><?php echo $t[
    "pay"
]; ?></a>
        </div>
        <div class="status">#<?php
        echo $txnId;
        echo $txnId === 0 ? " &#x26A0;" : "";
        ?></div>
    </div>
<?php endif; ?>

</body>
</html>
