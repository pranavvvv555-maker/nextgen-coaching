param(
    [string]$PostgresBin = 'E:\postgresql\bin',
    [int]$DatabasePort = 55432,
    [switch]$DatabaseOnly,
    [switch]$NoBuild
)
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path $PSScriptRoot -Parent
$taskLocal = Join-Path $taskRoot '.local'
$taskData = Join-Path $taskLocal 'postgres'
$taskConfigPath = Join-Path $taskLocal 'backend.json'
if (!(Test-Path (Join-Path $PostgresBin 'initdb.exe'))) {
    throw 'PostgreSQL tools were not found. Pass -PostgresBin with the folder containing initdb.exe, pg_ctl.exe and psql.exe.'
}
New-Item -ItemType Directory -Path $taskLocal -Force | Out-Null
function New-LocalSecret {
    $taskBytes = New-Object byte[] 36
    $taskRng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    try { $taskRng.GetBytes($taskBytes) } finally { $taskRng.Dispose() }
    return [Convert]::ToBase64String($taskBytes)
}
if (!(Test-Path $taskConfigPath)) {
    $taskPassword = New-LocalSecret
    $taskConfig = @{
        ConnectionStrings = @{ DefaultConnection = "Host=127.0.0.1;Port=$DatabasePort;Database=nextgen_coaching;Username=nextgen_dev;Password=$taskPassword" }
        Jwt = @{ Secret = (New-LocalSecret) }
        Seed = @{
            IncludeDemoData = $true
            AdminEmail = 'admin@nextgencoaching.local'; AdminPassword = ((New-LocalSecret) + '!Aa1')
            StudentEmail = 'student@nextgencoaching.local'; StudentPassword = ((New-LocalSecret) + '!Aa1')
            InstructorEmail = 'instructor@nextgencoaching.local'; InstructorPassword = ((New-LocalSecret) + '!Aa1')
            HrEmail = 'hr@nextgencoaching.local'; HrPassword = ((New-LocalSecret) + '!Aa1')
        }
    }
    $taskConfig | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $taskConfigPath -Encoding UTF8
}
$taskConfig = Get-Content -LiteralPath $taskConfigPath -Raw | ConvertFrom-Json
@"
NEXTGEN LOCAL DEVELOPMENT ACCESS
Keep this file private. These accounts belong to the isolated development database.

Website (default launcher): http://localhost:5102
Admin sign-in: http://localhost:5102/login
API documentation: http://localhost:5101/swagger

Admin email: $($taskConfig.Seed.AdminEmail)
Admin password: $($taskConfig.Seed.AdminPassword)

You can create your own student account at http://localhost:5102/signup.
Additional seeded development accounts, when enabled, are listed in backend.json.
"@ | Set-Content -LiteralPath (Join-Path $taskLocal 'DEVELOPMENT-ACCESS.txt') -Encoding UTF8
$taskConnection = New-Object System.Data.Common.DbConnectionStringBuilder
$taskConnection.set_ConnectionString($taskConfig.ConnectionStrings.DefaultConnection)
if ([int]$taskConnection['Port'] -ne $DatabasePort -or $taskConnection['Username'] -ne 'nextgen_dev') {
    throw 'The saved local database configuration does not match this launcher. Use its configured port or supply your own connection with environment variables.'
}
$taskPassword = [string]$taskConnection['Password']
if (!(Test-Path (Join-Path $taskData 'PG_VERSION'))) {
    if ((Test-Path $taskData) -and (Get-ChildItem -LiteralPath $taskData -Force | Measure-Object).Count -gt 0) {
        throw 'The development database folder contains files but is not a PostgreSQL cluster. It has been left untouched.'
    }
    $taskPasswordFile = Join-Path $taskLocal 'postgres-init-password.txt'
    try {
        [System.IO.File]::WriteAllText($taskPasswordFile, $taskPassword)
        & (Join-Path $PostgresBin 'initdb.exe') -D $taskData -U nextgen_dev '--auth=scram-sha-256' '--encoding=UTF8' '--locale=C' "--pwfile=$taskPasswordFile"
        if ($LASTEXITCODE -ne 0) { throw 'Could not initialize the isolated local database.' }
    } finally { Remove-Item -LiteralPath $taskPasswordFile -ErrorAction SilentlyContinue }
}
& (Join-Path $PostgresBin 'pg_ctl.exe') status -D $taskData *> $null
if ($LASTEXITCODE -ne 0) {
    if (Get-NetTCPConnection -LocalPort $DatabasePort -State Listen -ErrorAction SilentlyContinue) {
        throw "Port $DatabasePort is already in use by another service. That service has been left untouched."
    }
    $taskDbProcess = Start-Process -FilePath (Join-Path $PostgresBin 'pg_ctl.exe') -ArgumentList @('-D',"`"$taskData`"",'-l',"`"$(Join-Path $taskLocal 'postgres.log')`"",'-o',"`"-h 127.0.0.1 -p $DatabasePort`"",'-w','start') -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $taskLocal 'postgres-start.log') -RedirectStandardError (Join-Path $taskLocal 'postgres-start-error.log')
    if (!$taskDbProcess.WaitForExit(30000)) { throw 'PostgreSQL startup timed out. See .local/postgres-start-error.log.' }
    if ($taskDbProcess.ExitCode -ne 0) { throw 'PostgreSQL could not start. See .local/postgres-start-error.log.' }
}
$taskPreviousPassword = $env:PGPASSWORD
try {
    $env:PGPASSWORD = $taskPassword
    $taskExists = & (Join-Path $PostgresBin 'psql.exe') -h 127.0.0.1 -p $DatabasePort -U nextgen_dev -d postgres -Atc "SELECT 1 FROM pg_database WHERE datname = 'nextgen_coaching'"
    if ($LASTEXITCODE -ne 0) { throw 'Could not connect to the isolated development database.' }
    if ($taskExists -ne '1') {
        & (Join-Path $PostgresBin 'createdb.exe') -h 127.0.0.1 -p $DatabasePort -U nextgen_dev nextgen_coaching
        if ($LASTEXITCODE -ne 0) { throw 'Could not create the development database.' }
    }
} finally { $env:PGPASSWORD = $taskPreviousPassword }
Write-Host "Local database ready on 127.0.0.1:$DatabasePort. Existing PostgreSQL services have not been changed."
if ($DatabaseOnly) { return }
$taskExisting = Get-NetTCPConnection -LocalPort 5101 -State Listen -ErrorAction SilentlyContinue
if ($taskExisting) {
    try {
        $taskReady = Invoke-RestMethod 'http://localhost:5101/health/ready' -TimeoutSec 3
        if ($taskReady.status -eq 'ready') { Write-Host 'Backend already ready on http://localhost:5101.'; return }
    } catch { }
    throw 'Port 5101 is already in use but the backend is not ready. Check the existing process before restarting.'
}
if (!$NoBuild) {
    & dotnet build (Join-Path $taskRoot 'src/NextGenCoaching.Api') --nologo
    if ($LASTEXITCODE -ne 0) { throw 'The API build failed.' }
}
$taskApi = Start-Process dotnet -ArgumentList @('run','--no-build','--project','src/NextGenCoaching.Api','--launch-profile','http') -WorkingDirectory $taskRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $taskLocal 'api.log') -RedirectStandardError (Join-Path $taskLocal 'api-error.log')
$taskApi.Id | Set-Content (Join-Path $taskLocal 'api-launcher.pid')
for ($taskAttempt = 0; $taskAttempt -lt 50; $taskAttempt++) {
    try {
        $taskReady = Invoke-RestMethod 'http://localhost:5101/health/ready' -TimeoutSec 2
        if ($taskReady.status -eq 'ready') {
            Write-Host 'Backend and database ready: http://localhost:5101/swagger'
            Write-Host 'Local development login credentials: .local/backend.json (Seed section). Do not publish this file.'
            return
        }
    } catch { }
    if ($taskApi.HasExited) { throw 'The API stopped during startup. See .local/api.log and .local/api-error.log.' }
    Start-Sleep -Milliseconds 500
}
throw 'The API did not become ready. See .local/api.log and .local/api-error.log.'
