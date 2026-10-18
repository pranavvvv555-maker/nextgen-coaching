param(
    [switch]$ApiOnly,
    [switch]$Database,
    [string]$PostgresBin = 'E:\postgresql\bin'
)
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path $PSScriptRoot -Parent
$taskNames = if ($ApiOnly) { @('NextGenCoaching.Api') } else { @('NextGenCoaching.Api','NextGenCoaching.Web') }
foreach ($taskName in $taskNames) {
    $taskExpected = Join-Path $taskRoot "src/$taskName/bin/Debug/net10.0/$taskName.exe"
    Get-Process -Name $taskName -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq $taskExpected } | Stop-Process -Force
}
if ($Database) {
    $taskData = Join-Path $taskRoot '.local/postgres'
    if (Test-Path (Join-Path $taskData 'PG_VERSION')) {
        & (Join-Path $PostgresBin 'pg_ctl.exe') -D $taskData -m fast -w stop
        if ($LASTEXITCODE -ne 0) { throw 'Could not stop the isolated database. Check its log.' }
    }
}
Write-Host 'Requested local app processes stopped. Saved database files have been preserved.'
