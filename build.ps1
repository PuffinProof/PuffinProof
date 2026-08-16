$ErrorActionPreference = "Stop"

$utc = [DateTime]::UtcNow
$build = powershell -NoProfile -ExecutionPolicy Bypass -File "$PSScriptRoot\scripts\Get-DailyBuildNumber.ps1"
if (-not $build) { $build = 1 }

Write-Host "Version $(($utc).Year).$($utc.Month).$($utc.Day).$build"

dotnet test JustSpell.slnx -c Release --filter "FullyQualifiedName~JustSpell.Tests"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$verArgs = "-p:CalVerYear=$($utc.Year);CalVerMonth=$($utc.Month);CalVerDay=$($utc.Day);BuildNumber=$build"

dotnet publish src/JustSpell.Stub/JustSpell.Stub.csproj -c Release -r win-x64 --self-contained false /p:PublishSingleFile=true /p:DebugType=none $verArgs -o dist\stub
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$env:CalVerYear = "$($utc.Year)"
$env:CalVerMonth = "$($utc.Month)"
$env:CalVerDay = "$($utc.Day)"
$env:BuildNumber = "$build"
powershell -NoProfile -ExecutionPolicy Bypass -File "$PSScriptRoot\installer\JustSpell.Msix\pack.ps1"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

New-Item -ItemType Directory -Force -Path dist | Out-Null
Copy-Item "dist\stub\JustSpellSetup.exe" "dist\JustSpellSetup.exe" -Force
Copy-Item "dist\stub\feed.json" "dist\feed.json" -Force

Get-ChildItem dist -File | ForEach-Object {
    "{0}  {1}" -f (Get-FileHash $_.FullName -Algorithm SHA256).Hash, $_.Name
} | Set-Content dist\SHA256SUMS.txt

Write-Host ""
Write-Host "Stub EXE:       dist\JustSpellSetup.exe"
Write-Host "MSIX:           dist\JustSpell.msix"
Write-Host "App Installer:  dist\JustSpell.appinstaller"
