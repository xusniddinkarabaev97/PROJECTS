# GZS Billing - Tunnel Watchdog (localtunnel)
# Keeps subdomain tunnels alive, restarts if dead

$apiUrl = "https://gzs-billing.loca.lt"
$apiPort = 5036
$apiSubdomain = "gzs-billing"
$apiLog = "D:\PROJECTS\billing\lt-api.log"

$adminPort = 5173
$adminSubdomain = "gzs-billing-admin"
$adminLog = "D:\PROJECTS\billing\lt-admin.log"

Write-Host "Watchdog: $apiUrl : $apiPort | Admin : $adminPort" -ForegroundColor Cyan

function Start-Tunnel($port, $subdomain, $logFile) {
    Write-Host "$(Get-Date -Format HH:mm:ss) Restarting $subdomain..." -ForegroundColor Yellow
    Remove-Item $logFile -Force -ErrorAction SilentlyContinue
    Start-Process -FilePath "cmd" -ArgumentList "/c","lt","--port",$port,"--subdomain",$subdomain -RedirectStandardOutput $logFile -WindowStyle Hidden
    Start-Sleep -Seconds 6
}

while ($true) {
    try {
        $r = Invoke-RestMethod -Uri "$apiUrl/api/auth/login" -Method POST -ContentType "application/json" -Body '{"username":"admin","password":"admin123!"}' -TimeoutSec 15
        Write-Host "$(Get-Date -Format HH:mm:ss) API OK" -ForegroundColor Gray
    } catch {
        Write-Host "$(Get-Date -Format HH:mm:ss) API DEAD: $($_.Exception.Message)" -ForegroundColor Red
        Start-Tunnel $apiPort $apiSubdomain $apiLog
    }

    Start-Sleep -Seconds 60
}
