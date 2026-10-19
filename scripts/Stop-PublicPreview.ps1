$ErrorActionPreference='Stop'
$taskRoot=Split-Path $PSScriptRoot -Parent
$taskStatePath=Join-Path $taskRoot '.local/public-preview.json'
if(Test-Path -LiteralPath $taskStatePath){
    $taskState=Get-Content -LiteralPath $taskStatePath -Raw|ConvertFrom-Json
    $taskProcess=Get-Process -Id $taskState.pid -ErrorAction SilentlyContinue
    if($taskProcess -and $taskProcess.Path -eq $taskState.exe -and $taskProcess.StartTime.ToUniversalTime().Ticks -eq [long]$taskState.startedTicks){Stop-Process -Id $taskProcess.Id -Force}
    Remove-Item -LiteralPath $taskStatePath
}
Write-Host 'This managed public preview is stopped. The website and database are preserved.'
