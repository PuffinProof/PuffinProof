$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$now = [DateTime]::UtcNow
$year = if ($env:CalVerYear) { [int]$env:CalVerYear } else { $now.Year }
$month = if ($env:CalVerMonth) { [int]$env:CalVerMonth } else { $now.Month }
$day = if ($env:CalVerDay) { [int]$env:CalVerDay } else { $now.Day }
$build = if ($env:BuildNumber) { [int]$env:BuildNumber } elseif ($env:GITHUB_RUN_NUMBER) { [int]$env:GITHUB_RUN_NUMBER } else { 1 }
$display = "$year.$month.$day.$build"
$makeappx = Get-ChildItem "C:\Program Files (x86)\Windows Kits\10\bin\*\x64\makeappx.exe" |
    Sort-Object FullName -Descending |
    Select-Object -First 1 -ExpandProperty FullName
if (-not $makeappx) { throw "MakeAppx.exe not found. Install the Windows 10/11 SDK (Microsoft)." }

$payload = Join-Path $root "installer\payload"
dotnet publish (Join-Path $root "src\PuffinProof\PuffinProof.csproj") `
    -c Release -r win-x64 --self-contained false `
    /p:DebugType=none /p:DebugSymbols=false `
    -o $payload
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$layout = Join-Path $PSScriptRoot "layout"
if (Test-Path $layout) { Remove-Item $layout -Recurse -Force }
New-Item -ItemType Directory -Path (Join-Path $layout "Assets") | Out-Null
Get-ChildItem $payload -File | Where-Object { $_.Extension -ne ".pdb" } | Copy-Item -Destination $layout
if (Test-Path (Join-Path $payload "Dictionaries")) {
    Copy-Item (Join-Path $payload "Dictionaries") (Join-Path $layout "Dictionaries") -Recurse
}
if (Test-Path (Join-Path $payload "Assets")) {
    Copy-Item (Join-Path $payload "Assets\*") (Join-Path $layout "Assets") -Force
}
$manifest = Get-Content (Join-Path $PSScriptRoot "AppxManifest.xml") -Raw
$manifest = $manifest -creplace 'Version="1.0.0.0"', "Version=`"$display`""
$utf8 = New-Object System.Text.UTF8Encoding $false
[System.IO.File]::WriteAllText((Join-Path $layout "AppxManifest.xml"), $manifest.Trim(), $utf8)

Add-Type -AssemblyName System.Drawing
$iconSource = Join-Path $root "branding\app-icon.jpg"
if (-not (Test-Path $iconSource)) {
    $iconSource = Join-Path $root "branding\mascot-puffin.jpg"
}
function Save-Png([int]$size, [string]$name) {
    $bmp = New-Object System.Drawing.Bitmap($size, $size)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.Clear([System.Drawing.Color]::FromArgb(247, 244, 239))
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    if (Test-Path $iconSource) {
        $src = [System.Drawing.Image]::FromFile($iconSource)
        $g.DrawImage($src, 0, 0, $size, $size)
        $src.Dispose()
    }
    else {
        $brush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(196, 69, 54))
        $g.FillEllipse($brush, [int]($size * 0.2), [int]($size * 0.55), [int]($size * 0.6), [int]($size * 0.18))
        $brush.Dispose()
    }
    $g.Dispose()
    $bmp.Save((Join-Path $layout "Assets\$name"), [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
}
Save-Png 50 StoreLogo.png
Save-Png 44 Square44x44Logo.png
Save-Png 150 Square150x150Logo.png

$outDir = Join-Path $root "dist"
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
$msix = Join-Path $outDir "PuffinProof.msix"
if (Test-Path $msix) { Remove-Item $msix -Force }
& $makeappx pack /d $layout /p $msix /o
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$appinstaller = Join-Path $outDir "PuffinProof.appinstaller"
$uri = [Uri]::new((Resolve-Path $msix).Path).AbsoluteUri
@"
<?xml version="1.0" encoding="utf-8"?>
<AppInstaller Version="$display" xmlns="http://schemas.microsoft.com/appx/appinstaller/2018">
  <MainPackage Name="PuffinProof" Publisher="CN=PuffinProof" Version="$display"
               ProcessorArchitecture="x64" Uri="$uri" />
  <UpdateSettings>
    <OnLaunch HoursBetweenUpdateChecks="24" ShowPrompt="true" />
  </UpdateSettings>
</AppInstaller>
"@ | Set-Content -Path $appinstaller -Encoding UTF8

Write-Host "MSIX:         $msix"
Write-Host "AppInstaller: $appinstaller"
Write-Host "Try: Add-AppxPackage -Path `"$msix`" -AllowUnsigned"
