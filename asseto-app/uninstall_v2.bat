@echo off
setlocal enabledelayedexpansion
net session >nul 2>&1 || (echo [ERROR] Run as Administrator! && pause && exit /b 1)
echo [*] Notifying server (setting agent offline)...
powershell -Command "try { $body = @{status='offline';hostname=$env:COMPUTERNAME} | ConvertTo-Json; Invoke-RestMethod \"http://45.150.25.74:80/api/v1/agents/agent-$env:COMPUTERNAME/heartbeat\" -Method POST -Body $body -ContentType 'application/json' | Out-Null; Write-Host '  Server notified' } catch { Write-Host '  Server unreachable (offline anyway)' }"
echo [*] Stopping scheduled task...
schtasks /delete /tn "DlpdpiEndpointAgent" /f >nul 2>&1
echo [*] Stopping service...
sc stop DlpdpiEndpoint >nul 2>&1
sc delete DlpdpiEndpoint >nul 2>&1
echo [*] Removing files...
rmdir /S /Q "%ProgramData%\DlpdpiAgent" 2>nul
echo.
echo [DONE] Agent removed from this computer.
echo The server record will be auto-cleaned within 7 days.
pause
