param([int]$WebPort=5102)
$ErrorActionPreference='Stop'
$taskRoot=Split-Path $PSScriptRoot -Parent
& (Join-Path $taskRoot 'run.ps1') -WebPort $WebPort -Mobile -NoBuild
$taskLocal=Join-Path $taskRoot '.local'
$taskStatePath=Join-Path $taskLocal 'public-preview.json'
$taskTool=Get-Command cloudflared -ErrorAction SilentlyContinue
$taskExe=if($taskTool){$taskTool.Source}else{Join-Path $taskRoot 'cloudflared.exe'}
if(!(Test-Path -LiteralPath $taskExe)){throw 'Install cloudflared from Cloudflare before starting a public preview.'}
if(Test-Path -LiteralPath $taskStatePath){
    $taskOld=Get-Content -LiteralPath $taskStatePath -Raw|ConvertFrom-Json
    $taskExisting=Get-Process -Id $taskOld.pid -ErrorAction SilentlyContinue
    if($taskExisting -and $taskExisting.Path -eq $taskOld.exe -and $taskExisting.StartTime.ToUniversalTime().Ticks -eq [long]$taskOld.startedTicks){Write-Host "Existing preview: $($taskOld.url)";return}
}
$taskLog=Join-Path $taskLocal 'public-preview.log';$taskError=Join-Path $taskLocal 'public-preview-error.log'
$taskProcess=Start-Process -FilePath $taskExe -ArgumentList @('tunnel','--no-autoupdate','--protocol','http2','--url',"http://127.0.0.1:$WebPort") -WindowStyle Hidden -PassThru -RedirectStandardOutput $taskLog -RedirectStandardError $taskError
for($taskAttempt=0;$taskAttempt -lt 60;$taskAttempt++){
    $taskText=(Get-Content -LiteralPath $taskLog -Raw -ErrorAction SilentlyContinue)+(Get-Content -LiteralPath $taskError -Raw -ErrorAction SilentlyContinue)
    $taskMatch=[regex]::Match($taskText,'https://[a-z0-9-]+\.trycloudflare\.com')
    if($taskMatch.Success){
        @{pid=$taskProcess.Id;exe=$taskExe;startedTicks=$taskProcess.StartTime.ToUniversalTime().Ticks;url=$taskMatch.Value;port=$WebPort}|ConvertTo-Json|Set-Content -LiteralPath $taskStatePath
        $taskMatch.Value|Set-Content -LiteralPath (Join-Path $taskLocal 'PUBLIC-PREVIEW-LINK.txt')
        Write-Host "Public preview: $($taskMatch.Value)";Write-Host 'Keep this PC awake, online and running. Anyone with the link can open the site.';return
    }
    if($taskProcess.HasExited){throw 'Sharing exited. See the private public-preview logs.'}
    Start-Sleep -Milliseconds 500
}
throw 'No preview link was returned. See .local/public-preview-error.log.'
