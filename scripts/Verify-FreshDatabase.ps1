param([string]$PostgresBin = 'E:\postgresql\bin')
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path $PSScriptRoot -Parent
$taskLocal = Join-Path $taskRoot '.local'
$taskCfg = Get-Content (Join-Path $taskLocal 'backend.json') -Raw | ConvertFrom-Json
$taskConnection = New-Object System.Data.Common.DbConnectionStringBuilder
$taskConnection.set_ConnectionString($taskCfg.ConnectionStrings.DefaultConnection)
if ($taskConnection['Host'] -ne '127.0.0.1' -or [int]$taskConnection['Port'] -ne 55432) { throw 'Only the isolated local cluster is supported.' }
if (Get-NetTCPConnection -LocalPort 5111 -State Listen -ErrorAction SilentlyContinue) { throw 'Verification port 5111 is already in use.' }
$taskDbName = 'nextgen_seed_check_' + [Guid]::NewGuid().ToString('N').Substring(0,8)
$taskOldPassword = $env:PGPASSWORD
$taskOldConnection = $env:ConnectionStrings__DefaultConnection
$taskOldEnvironment = $env:ASPNETCORE_ENVIRONMENT
$taskCreated = $false
$taskApi = $null
try {
    $env:PGPASSWORD = [string]$taskConnection['Password']
    & (Join-Path $PostgresBin 'createdb.exe') -h 127.0.0.1 -p 55432 -U nextgen_dev $taskDbName
    if ($LASTEXITCODE -ne 0) { throw 'Could not create a fresh verification database.' }
    $taskCreated = $true
    $taskConnection['Database'] = $taskDbName
    $env:ConnectionStrings__DefaultConnection = $taskConnection.get_ConnectionString()
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    $taskApi = Start-Process -FilePath (Join-Path $taskRoot 'src/NextGenCoaching.Api/bin/Debug/net10.0/NextGenCoaching.Api.exe') -ArgumentList @('--urls','http://127.0.0.1:5111') -WorkingDirectory (Join-Path $taskRoot 'src/NextGenCoaching.Api') -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $taskLocal 'fresh-database.log') -RedirectStandardError (Join-Path $taskLocal 'fresh-database-error.log')
    $env:ConnectionStrings__DefaultConnection = $taskOldConnection
    $env:ASPNETCORE_ENVIRONMENT = $taskOldEnvironment
    $taskReady = $false
    for ($taskTry = 0; $taskTry -lt 50; $taskTry++) {
        try { if ((Invoke-RestMethod 'http://127.0.0.1:5111/health/ready' -TimeoutSec 2).status -eq 'ready') { $taskReady = $true; break } } catch { }
        if ($taskApi.HasExited) { throw 'Fresh database startup failed. See .local/fresh-database.log.' }
        Start-Sleep -Milliseconds 300
    }
    if (!$taskReady) { throw 'The fresh database API did not become ready.' }
    $taskCourses = (Invoke-RestMethod 'http://127.0.0.1:5111/api/v1/public/courses').data
    if ($taskCourses.Count -ne 6 -or $taskCourses[0].id -ne '11111111-1111-1111-1111-111111111111' -or ($taskCourses | Where-Object {$_.mode -ne 'live-online'})) { throw 'Fresh catalogue did not match the approved seed.' }
    foreach ($taskCourse in $taskCourses) {
        $taskDetail = (Invoke-RestMethod "http://127.0.0.1:5111/api/v1/public/courses/$($taskCourse.slug)").data
        if ($taskDetail.journeySteps.Count -eq 0 -or $taskDetail.tools.Count -eq 0) { throw 'A fresh course is missing curriculum relationships.' }
    }
    $taskOldApiUrl = $env:NEXTGEN_API_URL
    try {
        $env:NEXTGEN_API_URL = 'http://127.0.0.1:5111'
        & node (Join-Path $taskRoot 'scripts/Verify-Backend.mjs')
        if ($LASTEXITCODE -ne 0) { throw 'Fresh database integration checks failed.' }
        Copy-Item (Join-Path $taskLocal 'backend-checks.json') (Join-Path $taskLocal 'fresh-database-checks.json')
        $taskOldWorkflow = $env:NEXTGEN_ISOLATED_WORKFLOW
        try {
            $env:NEXTGEN_ISOLATED_WORKFLOW = 'true'
            & node (Join-Path $taskRoot 'scripts/Verify-Workflow.cjs')
            if ($LASTEXITCODE -ne 0) { throw 'Isolated learning workflow checks failed.' }
        } finally { $env:NEXTGEN_ISOLATED_WORKFLOW = $taskOldWorkflow }
    } finally { $env:NEXTGEN_API_URL = $taskOldApiUrl }
    Write-Host 'Fresh database migrations, canonical catalogue, curriculum relationships and API integration checks passed.'
} finally {
    $env:ConnectionStrings__DefaultConnection = $taskOldConnection
    $env:ASPNETCORE_ENVIRONMENT = $taskOldEnvironment
    if ($taskApi -and !$taskApi.HasExited) { Stop-Process -Id $taskApi.Id -Force; $taskApi.WaitForExit(5000) | Out-Null }
    if ($taskCreated -and $taskDbName -match '^nextgen_seed_check_[a-f0-9]{8}$') {
        # Only the uniquely named database created by this invocation is removed.
        & (Join-Path $PostgresBin 'dropdb.exe') -h 127.0.0.1 -p 55432 -U nextgen_dev $taskDbName
        if ($LASTEXITCODE -ne 0) { Write-Warning "Could not clean up verification database $taskDbName." }
    }
    $env:PGPASSWORD = $taskOldPassword
}
