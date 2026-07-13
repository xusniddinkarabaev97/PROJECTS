using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GzsBilling.Infrastructure.Persistence;

namespace GzsBilling.Api.Controllers;

[ApiController]
[AllowAnonymous]
public class MobilePayController : ControllerBase
{
    private readonly BillingDbContext _db;

    public MobilePayController(BillingDbContext db)
    {
        _db = db;
    }

    [HttpGet("/pay/{columnId:guid}")]
    [Produces("text/html")]
    public async Task<IActionResult> PayPage(Guid columnId)
    {
        var column = await _db.Columns.FirstOrDefaultAsync(c => c.Id == columnId);
        if (column == null)
            return Content("<html><body style='background:#0d1117;color:#c9d1d9;font-family:sans-serif;display:flex;align-items:center;justify-content:center;height:100vh'><h2>Kalonka topilmadi</h2></body></html>", "text/html");

        var station = await _db.Stations.FindAsync(column.StationId);
        var stationName = station?.Name ?? "Noma'lum";

        var html = $$"""
<!DOCTYPE html>
<html lang='uz'>
<head>
<meta charset='UTF-8'>
<meta name='viewport' content='width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no'>
<title>To'lov - {{stationName}}</title>
<style>
* { margin:0; padding:0; box-sizing:border-box; }
body { background: linear-gradient(180deg, #0d1117 0%, #161b22 100%); color:#c9d1d9; font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,sans-serif; min-height:100vh; display:flex; flex-direction:column; align-items:center; justify-content:center; padding:16px; }
.card { background:#161b22; border:1px solid #30363d; border-radius:16px; padding:28px 24px; width:100%; max-width:400px; box-shadow: 0 4px 24px rgba(0,0,0,0.5); }
.logo { text-align:center; font-size:1.5rem; font-weight:700; color:#58a6ff; margin-bottom:8px; letter-spacing:2px; }
.subtitle { text-align:center; color:#8b949e; font-size:0.85rem; margin-bottom:20px; }
.info-row { display:flex; justify-content:space-between; padding:8px 0; border-bottom:1px solid #21262d; font-size:0.9rem; }
.info-label { color:#8b949e; }
.info-value { color:#c9d1d9; font-weight:500; text-align:right; }
input,select { width:100%; padding:14px; background:#0d1117; border:1px solid #30363d; border-radius:8px; color:#c9d1d9; font-size:1.2rem; margin-top:8px; outline:none; }
input:focus,select:focus { border-color:#58a6ff; box-shadow:0 0 0 3px rgba(88,166,255,0.1); }
label { display:block; margin-top:16px; font-size:0.85rem; color:#8b949e; }
.btn { width:100%; padding:16px; border:none; border-radius:10px; font-size:1.05rem; font-weight:600; cursor:pointer; margin-top:20px; color:#fff; }
.btn-pay { background:#238636; }
.btn-pay:disabled { opacity:0.5; }
.provider-btn { padding:12px 18px; border:2px solid #30363d; border-radius:8px; background:transparent; color:#c9d1d9; cursor:pointer; font-size:0.95rem; margin:4px; }
.provider-btn.selected { border-color:#58a6ff; background:rgba(88,166,255,0.1); }
.result { background:rgba(46,160,67,0.1); border:1px solid #2ea043; border-radius:12px; padding:20px; margin-top:20px; text-align:center; }
.result-icon { font-size:3rem; }
.result-title { color:#3fb950; font-size:1.1rem; font-weight:600; margin:8px 0; }
.result-row { display:flex; justify-content:space-between; padding:6px 0; font-size:0.85rem; color:#8b949e; }
.liters { text-align:center; color:#58a6ff; font-size:1.2rem; font-weight:600; margin:8px 0; }
.spinner { display:inline-block; width:20px; height:20px; border:2px solid rgba(255,255,255,0.3); border-top-color:#fff; border-radius:50%; animation:spin .6s linear infinite; vertical-align:middle; margin-right:8px; }
@keyframes spin { to{transform:rotate(360deg)} }
.error { background:rgba(248,81,73,0.1); border:1px solid rgba(248,81,73,0.3); border-radius:8px; padding:12px; color:#f85149; font-size:0.85rem; margin-top:12px; text-align:center; }
</style>
</head>
<body>
<div style='text-align:center;font-size:1.6rem;font-weight:700;color:#fff;border-bottom:2px solid #3fb950;padding:12px 0;margin-bottom:16px;width:100%;max-width:400px'>⛽ TO'LOV</div>
<div class='card' id='app'>
  <div class='logo'>⛽ GZS FUEL</div>
  <div class='subtitle'>{{stationName}}</div>
  <div style='text-align:center;color:#3fb950;font-size:0.85rem;margin-bottom:12px'>Sizning telefoningizdan to'lov</div>

  <div id='info'>
    <div class='info-row'><span class='info-label'>Kalonka</span><span class='info-value'>{{column.Name}}</span></div>
    <div class='info-row'><span class='info-label'>Yoqilg'i</span><span class='info-value'>{{column.FuelType}}</span></div>
    <div class='info-row'><span class='info-label'>Narx</span><span class='info-value'>{{column.PricePerLiter:N0}} UZS/litr</span></div>
  </div>

  <div id='form'>
    <label>Summa (UZS)</label>
    <input type='number' id='amount' placeholder='5000' min='100' value='' oninput='calcLiters()'>

    <div class='liters' id='liters'></div>

    <label>To'lov tizimi</label>
    <div style='display:flex;flex-wrap:wrap;margin-top:8px' id='providers'>
      <button class='provider-btn' onclick='selectProvider("uzcard",this)'>Uzcard</button>
      <button class='provider-btn' onclick='selectProvider("humo",this)'>Humo</button>
      <button class='provider-btn' onclick='selectProvider("click",this)'>Click</button>
      <button class='provider-btn' onclick='selectProvider("payme",this)'>Payme</button>
      <button class='provider-btn' onclick='selectProvider("apelsin",this)'>Apelsin</button>
    </div>

    <label>Avto raqam (ixtiyoriy)</label>
    <input type='text' id='carNumber' placeholder='01A123AA'>

    <button class='btn btn-pay' id='payBtn' onclick='doPayment()'>To'lash</button>
    <div class='error' id='error' style='display:none'></div>
  </div>

  <div id='success' style='display:none'></div>
</div>

<script>
const columnId = '{{columnId}}';
const pricePerLiter = {{column.PricePerLiter.ToString(System.Globalization.CultureInfo.InvariantCulture)}};
let selectedProvider = '';

function calcLiters() {
  const amount = parseFloat(document.getElementById('amount').value) || 0;
  if (amount > 0 && pricePerLiter > 0) {
    document.getElementById('liters').textContent = (amount / pricePerLiter).toFixed(2) + ' litr';
  } else {
    document.getElementById('liters').textContent = '';
  }
}

function selectProvider(p, btn) {
  selectedProvider = p;
  document.querySelectorAll('.provider-btn').forEach(b => b.classList.remove('selected'));
  btn.classList.add('selected');
}

async function doPayment() {
  const amount = parseFloat(document.getElementById('amount').value);
  const carNumber = document.getElementById('carNumber').value.trim();
  const errorDiv = document.getElementById('error');
  const payBtn = document.getElementById('payBtn');

  errorDiv.style.display = 'none';

  if (!amount || amount < 100) {
    errorDiv.textContent = 'Summa kamida 100 UZS bo\'lishi kerak';
    errorDiv.style.display = 'block';
    return;
  }
  if (!selectedProvider) {
    errorDiv.textContent = 'To\'lov tizimini tanlang';
    errorDiv.style.display = 'block';
    return;
  }

  payBtn.disabled = true;
  payBtn.innerHTML = '<span class="spinner"></span>To\'lov...';

  try {
    const resp = await fetch('/api/pay/' + columnId, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ amount, paymentSystem: selectedProvider, carNumber })
    });

    const data = await resp.json();

    if (!resp.ok) {
      throw new Error(data.error || data.message || 'Xatolik');
    }

    document.getElementById('form').style.display = 'none';
    document.getElementById('info').style.display = 'none';

    const dt = new Date(data.processedAt);
    document.getElementById('success').innerHTML =
      '<div class="result">' +
      '<div class="result-icon">✅</div>' +
      '<div class="result-title">To\'lov qabul qilindi!</div>' +
      '<div class="result-row"><span>Summa</span><span>' + data.amount.toLocaleString() + ' UZS</span></div>' +
      '<div class="result-row"><span>Litraj</span><span>' + data.liters + ' litr</span></div>' +
      '<div class="result-row"><span>Tizim</span><span>' + data.paymentSystem + '</span></div>' +
      '<div class="result-row"><span>Chek ID</span><span style="font-size:0.75rem">' + data.transactionId + '</span></div>' +
      '<div class="result-row"><span>Vaqt</span><span>' + dt.toLocaleString() + '</span></div>' +
      '</div>';
    document.getElementById('success').style.display = 'block';

  } catch(err) {
    errorDiv.textContent = err.message;
    errorDiv.style.display = 'block';
    payBtn.disabled = false;
    payBtn.innerHTML = 'To\'lash';
  }
}
</script>
</body>
</html>
""";

        return Content(html, "text/html; charset=utf-8");
    }
}
