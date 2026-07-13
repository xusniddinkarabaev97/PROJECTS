Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  GZS Billing - Doimiy Publik URL" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$apiUrl = "https://gzs-billing.loca.lt"
$adminUrl = "https://gzs-billing-admin.loca.lt"

Write-Host "  Swagger:  $apiUrl/swagger" -ForegroundColor Green
Write-Host "  Admin:    $adminUrl" -ForegroundColor Green
Write-Host ""

# Test API
try {
    $r = Invoke-RestMethod -Uri "$apiUrl/api/auth/login" -Method POST -ContentType "application/json" -Body '{"username":"admin","password":"admin123!"}' -TimeoutSec 8
    Write-Host "  Login:  OK ($($r.role))" -ForegroundColor Green
} catch { Write-Host "  Login:  FAILED - $_" -ForegroundColor Red }

Write-Host ""
Write-Host "  Login: admin / admin123!" -ForegroundColor Gray
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Start-Process "$apiUrl/swagger"
Start-Process $adminUrl
