param([string]$PostgresBin = 'E:\postgresql\bin', [switch]$SkipBrowser)
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path $PSScriptRoot -Parent
$taskConfig = Get-Content (Join-Path $taskRoot '.local/backend.json') -Raw | ConvertFrom-Json
$taskConnection = [System.Data.Common.DbConnectionStringBuilder]::new()
$taskConnection.set_ConnectionString($taskConfig.ConnectionStrings.DefaultConnection)
if ($taskConnection['Host'] -ne '127.0.0.1' -or [int]$taskConnection['Port'] -ne 55432) { throw 'Use only the isolated development cluster for this verification.' }
foreach ($taskPort in @(5113,5114)) { if (Get-NetTCPConnection -LocalPort $taskPort -State Listen -ErrorAction SilentlyContinue) { throw "Port $taskPort is occupied." } }
$taskId = [Guid]::NewGuid().ToString('N').Substring(0,8)
$taskDatabase = 'nextgen_ready_' + $taskId
$taskRestoreDatabase = 'nextgen_restore_' + $taskId
$taskFolder = Join-Path $taskRoot ".local/production-$taskId"
New-Item -ItemType Directory -Path $taskFolder -Force | Out-Null
$taskEnvNames = @('PGPASSWORD','ASPNETCORE_ENVIRONMENT','ConnectionStrings__DefaultConnection','Jwt__Secret','Api__ProxyKey','Seed__AdminEmail','Seed__AdminPassword','Seed__IncludeDemoData','DataProtection__KeyPath','Api__BaseUrl','Hosting__HttpsRedirect')
$taskPrevious = @{}
foreach ($taskName in $taskEnvNames) { $taskPrevious[$taskName] = [Environment]::GetEnvironmentVariable($taskName) }
$taskCreated = $false; $taskRestoreCreated = $false; $taskApi = $null; $taskWeb = $null
$taskResults = [System.Collections.Generic.List[object]]::new()
function Confirm-TaskReady([string]$Url, $Process) {
    for ($taskAttempt = 0; $taskAttempt -lt 60; $taskAttempt++) {
        try { if ((Invoke-WebRequest $Url -UseBasicParsing -TimeoutSec 2).StatusCode -eq 200) { return } } catch { }
        if ($Process.HasExited) { throw 'Production application failed; see private verification logs.' }
        Start-Sleep -Milliseconds 350
    }
    throw 'Production application readiness timed out.'
}
try {
    foreach ($taskProject in @('Api','Web')) {
        & dotnet publish (Join-Path $taskRoot "src/NextGenCoaching.$taskProject") -c Release --no-restore -o (Join-Path $taskRoot "publish/readiness/$taskProject") --nologo
        if ($LASTEXITCODE -ne 0) { throw 'Release publish failed.' }
    }
    $env:PGPASSWORD = [string]$taskConnection['Password']
    & (Join-Path $PostgresBin 'createdb.exe') -h 127.0.0.1 -p 55432 -U nextgen_dev $taskDatabase
    if ($LASTEXITCODE -ne 0) { throw 'Could not create disposable database.' }; $taskCreated = $true
    $taskConnection['Database'] = $taskDatabase
    $env:ASPNETCORE_ENVIRONMENT = 'Production'
    $env:ConnectionStrings__DefaultConnection = $taskConnection.get_ConnectionString()
    $env:Jwt__Secret = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
    $env:Api__ProxyKey = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
    $env:Seed__AdminEmail = "production-$taskId@nextgen.invalid"
    $env:Seed__AdminPassword = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(30)) + '!Aa9'
    $env:Seed__IncludeDemoData = 'false'
    $env:DataProtection__KeyPath = Join-Path $taskFolder 'api-keys'
    foreach ($taskCase in @(@{key='Jwt__Secret';value='CHANGE_THIS_SECRET'},@{key='Seed__AdminPassword';value='short'},@{key='Seed__IncludeDemoData';value='true'},@{key='Api__ProxyKey';value='short'})) {
        $taskSaved = [Environment]::GetEnvironmentVariable($taskCase.key)
        [Environment]::SetEnvironmentVariable($taskCase.key,$taskCase.value)
        $taskRejected = Start-Process dotnet -ArgumentList @('NextGenCoaching.Api.dll','--urls','http://127.0.0.1:5113') -WorkingDirectory (Join-Path $taskRoot 'publish/readiness/Api') -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $taskFolder ($taskCase.key+'.log')) -RedirectStandardError (Join-Path $taskFolder ($taskCase.key+'-error.log'))
        [Environment]::SetEnvironmentVariable($taskCase.key,$taskSaved)
        if (!$taskRejected.WaitForExit(10000)) { Stop-Process -Id $taskRejected.Id -Force; throw 'Unsafe configuration was not rejected.' }
        if ($taskRejected.ExitCode -eq 0) { throw 'Unsafe configuration unexpectedly exited successfully.' }
        $taskResults.Add(@{name=('Production rejects unsafe '+$taskCase.key);passed=$true})
    }
    $taskApi = Start-Process dotnet -ArgumentList @('NextGenCoaching.Api.dll','--urls','http://127.0.0.1:5113') -WorkingDirectory (Join-Path $taskRoot 'publish/readiness/Api') -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $taskFolder 'api.log') -RedirectStandardError (Join-Path $taskFolder 'api-error.log')
    Confirm-TaskReady 'http://127.0.0.1:5113/health/ready' $taskApi
    $taskMigrationCount = & (Join-Path $PostgresBin 'psql.exe') -h 127.0.0.1 -p 55432 -U nextgen_dev -d $taskDatabase -t -A -c 'SELECT count(*) FROM "__EFMigrationsHistory";'
    if ($LASTEXITCODE -ne 0 -or [int]$taskMigrationCount -ne 2) { throw 'Expected both versioned migrations in the disposable database.' }
    $taskResults.Add(@{name='Fresh Production migration and startup';passed=$true})
    $taskLogin = Invoke-RestMethod 'http://127.0.0.1:5113/api/v1/auth/login' -Method Post -ContentType 'application/json' -Body (@{email=$env:Seed__AdminEmail;password=$env:Seed__AdminPassword}|ConvertTo-Json)
    $taskUsers = Invoke-RestMethod 'http://127.0.0.1:5113/api/v1/admin/users/' -Headers @{Authorization="Bearer $($taskLogin.data.accessToken)"}
    if ($taskUsers.data.totalCount -ne 1) { throw 'Production unexpectedly seeded demo users.' }
    $taskResults.Add(@{name='Only configured administrator seeded';passed=$true})
    $taskTestimonials = (Invoke-RestMethod 'http://127.0.0.1:5113/api/v1/public/testimonials/featured').data
    if ($taskTestimonials.Count -ne 0) { throw 'Production unexpectedly seeded sample testimonials.' }
    $taskResults.Add(@{name='No sample alumni testimonials in Production';passed=$true})
    $taskSwagger = Invoke-WebRequest 'http://127.0.0.1:5113/swagger/v1/swagger.json' -SkipHttpErrorCheck -UseBasicParsing
    if ($taskSwagger.StatusCode -ne 404) { throw 'Swagger should not be public in Production.' }
    $taskResults.Add(@{name='Production Swagger unavailable';passed=$true})
    $env:DataProtection__KeyPath = Join-Path $taskFolder 'web-keys'
    $env:Api__BaseUrl = 'http://127.0.0.1:5113'; $env:Hosting__HttpsRedirect = 'false'
    $taskWebDir = Join-Path $taskRoot 'publish/readiness/Web'
    $taskWeb = Start-Process dotnet -ArgumentList @('NextGenCoaching.Web.dll','--urls','http://127.0.0.1:5114') -WorkingDirectory $taskWebDir -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $taskFolder 'web.log') -RedirectStandardError (Join-Path $taskFolder 'web-error.log')
    Confirm-TaskReady 'http://127.0.0.1:5114/health/live' $taskWeb
    $taskResponse = Invoke-WebRequest 'http://127.0.0.1:5114/' -UseBasicParsing
    if ($taskResponse.Headers['X-Content-Type-Options'] -ne 'nosniff') { throw 'Missing response hardening.' }
    $taskResults.Add(@{name='Published website and security headers';passed=$true})
    $taskBrowserInput = Join-Path $taskFolder 'browser-input.json'
    @{web='http://127.0.0.1:5114';email=$env:Seed__AdminEmail;password=$env:Seed__AdminPassword;storage=(Join-Path $taskFolder 'storage.json');report=(Join-Path $taskFolder 'browser')} | ConvertTo-Json | Set-Content -LiteralPath $taskBrowserInput
    if (!$SkipBrowser) {
        & node (Join-Path $PSScriptRoot 'Verify-ProductionBrowser.cjs') $taskBrowserInput before
        if ($LASTEXITCODE -ne 0) { throw 'Production browser verification failed.' }
    } else {
        $taskCatalogue = (Invoke-RestMethod 'http://127.0.0.1:5113/api/v1/public/courses').data
        Invoke-RestMethod 'http://127.0.0.1:5113/api/v1/public/enquiries' -Method Post -ContentType 'application/json' -Body (@{name="Production QA $taskId";email="qa-$taskId@nextgen.invalid";courseInterestId=$taskCatalogue[0].id;message='Disposable production verification.'}|ConvertTo-Json) | Out-Null
    }
    Stop-Process -Id $taskWeb.Id -Force; $taskWeb.WaitForExit()
    $taskWeb = Start-Process dotnet -ArgumentList @('NextGenCoaching.Web.dll','--urls','http://127.0.0.1:5114') -WorkingDirectory $taskWebDir -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $taskFolder 'web-restart.log') -RedirectStandardError (Join-Path $taskFolder 'web-restart-error.log')
    Confirm-TaskReady 'http://127.0.0.1:5114/health/live' $taskWeb
    if (!$SkipBrowser) {
        & node (Join-Path $PSScriptRoot 'Verify-ProductionBrowser.cjs') $taskBrowserInput after
        if ($LASTEXITCODE -ne 0) { throw 'Production session restore failed.' }
        $taskResults.Add(@{name='Browser enrollment and admin session survive restart';passed=$true})
    } else {
        $taskResults.Add(@{name='Production website restarts successfully (browser session test skipped)';passed=$true})
    }
    & (Join-Path $PostgresBin 'pg_dump.exe') -h 127.0.0.1 -p 55432 -U nextgen_dev -Fc -f (Join-Path $taskFolder 'backup.dump') $taskDatabase
    if ($LASTEXITCODE -ne 0) { throw 'Backup failed.' }
    & (Join-Path $PostgresBin 'createdb.exe') -h 127.0.0.1 -p 55432 -U nextgen_dev $taskRestoreDatabase
    if ($LASTEXITCODE -ne 0) { throw 'Restore database creation failed.' }; $taskRestoreCreated = $true
    & (Join-Path $PostgresBin 'pg_restore.exe') -h 127.0.0.1 -p 55432 -U nextgen_dev -d $taskRestoreDatabase --no-owner (Join-Path $taskFolder 'backup.dump')
    if ($LASTEXITCODE -ne 0) { throw 'Restore failed.' }
    $taskCount = & (Join-Path $PostgresBin 'psql.exe') -h 127.0.0.1 -p 55432 -U nextgen_dev -d $taskRestoreDatabase -t -A -c 'SELECT count(*) FROM leads;'
    if ([int]$taskCount -lt 1) { throw 'Restored enrollment request is missing.' }
    $taskResults.Add(@{name='PostgreSQL backup restored into separate database with enrollment records';passed=$true})
} catch { $taskResults.Add(@{name=$_.Exception.Message;passed=$false}); throw }
finally {
    if ($taskWeb -and !$taskWeb.HasExited) { Stop-Process -Id $taskWeb.Id -Force }
    if ($taskApi -and !$taskApi.HasExited) { Stop-Process -Id $taskApi.Id -Force }
    foreach ($taskCleanup in @(@{created=$taskRestoreCreated;name=$taskRestoreDatabase},@{created=$taskCreated;name=$taskDatabase})) {
        if ($taskCleanup.created -and $taskCleanup.name -match '^nextgen_(ready|restore)_[0-9a-f]{8}$') { & (Join-Path $PostgresBin 'dropdb.exe') -h 127.0.0.1 -p 55432 -U nextgen_dev --force $taskCleanup.name }
    }
    foreach ($taskName in $taskEnvNames) { [Environment]::SetEnvironmentVariable($taskName,$taskPrevious[$taskName]) }
    if (Test-Path -LiteralPath (Join-Path $taskFolder 'browser-input.json')) { Remove-Item -LiteralPath (Join-Path $taskFolder 'browser-input.json') }
    $taskResults | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $taskRoot '.local/production-checks.json')
    Write-Host "Production checks: $($taskResults.Where({$_.passed}).Count) passed, $($taskResults.Where({!$_.passed}).Count) failed."
}
