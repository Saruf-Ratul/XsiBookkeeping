@echo off
REM Fix IIS Express auth so Login.aspx uses username/password (not Windows login).
powershell -NoProfile -Command ^
  "$path = Join-Path (Get-Location) '.vs\XsiBookkeeping\config\applicationhost.config';" ^
  "if (-not (Test-Path $path)) { Write-Host 'applicationhost.config not found. Open the solution in Visual Studio and run once.'; exit 1 }" ^
  "$xml = Get-Content $path -Raw;" ^
  "$xml = $xml -replace '<anonymousAuthentication enabled=\"false\" />', '<anonymousAuthentication enabled=\"true\" />';" ^
  "$xml = $xml -replace '<windowsAuthentication enabled=\"true\" />', '<windowsAuthentication enabled=\"false\" />';" ^
  "Set-Content -Path $path -Value $xml -NoNewline;" ^
  "Write-Host 'IIS Express auth fixed: Anonymous ON, Windows OFF.';" ^
  "Write-Host 'Restart debugging in Visual Studio (Stop, then F5).'"
