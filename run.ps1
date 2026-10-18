param(
    [int]$WebPort = 5102,
    [switch]$Mobile,
    [switch]$NoBuild,
    [string]$PostgresBin = 'E:\postgresql\bin'
)
$ErrorActionPreference = 'Stop'
$taskRoot = $PSScriptRoot
& (Join-Path $taskRoot 'scripts/Start-LocalBackend.ps1') -PostgresBin $PostgresBin -NoBuild:$NoBuild
$taskLocal = Join-Path $taskRoot '.local'
$taskExisting = Get-NetTCPConnection -LocalPort $WebPort -State Listen -ErrorAction SilentlyContinue
if (!$taskExisting) {
    if (!$NoBuild) {
        & dotnet build (Join-Path $taskRoot 'src/NextGenCoaching.Web') --nologo
        if ($LASTEXITCODE -ne 0) { throw 'Website build failed.' }
    }
    $taskBind = if ($Mobile) { '0.0.0.0' } else { '127.0.0.1' }
    $taskWeb = Start-Process dotnet -ArgumentList @('run','--no-build','--project','src/NextGenCoaching.Web','--launch-profile','http','--urls',"http://${taskBind}:$WebPort") -WorkingDirectory $taskRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $taskLocal 'web.log') -RedirectStandardError (Join-Path $taskLocal 'web-error.log')
    $taskWeb.Id | Set-Content (Join-Path $taskLocal 'web-launcher.pid')
}
$taskReady = $false
for ($taskAttempt = 0; $taskAttempt -lt 40; $taskAttempt++) {
    try {
        $taskResponse = Invoke-WebRequest "http://localhost:$WebPort" -TimeoutSec 3 -UseBasicParsing
        if ($taskResponse.StatusCode -eq 200 -and $taskResponse.Content -match 'blazor') { $taskReady = $true; break }
    } catch { }
    Start-Sleep -Milliseconds 500
}
if (!$taskReady) { throw "The website on port $WebPort did not become ready. Check .local/web.log." }
Write-Host "Website ready: http://localhost:$WebPort"
Write-Host 'Backend ready: http://localhost:5101/swagger'
if ($Mobile) { Write-Host "Phone testing: use http://<your-computer-WiFi-IP>:$WebPort on the same Wi-Fi. Only the website is exposed to the LAN." }
Write-Host 'Development credentials and logs are in .local; keep this folder private.'
