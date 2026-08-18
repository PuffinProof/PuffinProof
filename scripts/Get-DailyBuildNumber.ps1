# Next build number for today (UTC). Restarts at 1 each new UTC day.
# Prints a single integer. Windows versions are numeric only (no hex digits).
param(
    [int]$Year = 0,
    [int]$Month = 0,
    [int]$Day = 0
)

$utc = [DateTime]::UtcNow
if ($Year -le 0) { $Year = $utc.Year }
if ($Month -le 0) { $Month = $utc.Month }
if ($Day -le 0) { $Day = $utc.Day }

$prefix = "v$Year.$Month.$Day."
$max = 0

function Consider([string]$tag) {
    if (-not $tag.StartsWith($prefix)) { return }
    $rest = $tag.Substring($prefix.Length)
    $n = 0
    if ([int]::TryParse($rest, [ref]$n) -and $n -gt $script:max) {
        $script:max = $n
    }
}

try {
    git tag -l "$prefix*" 2>$null | ForEach-Object { Consider $_ }
} catch { }

if (Get-Command gh -ErrorAction SilentlyContinue) {
    try {
        gh release list --limit 200 --json tagName 2>$null |
            ConvertFrom-Json |
            ForEach-Object { Consider $_.tagName }
    } catch { }
}

Write-Output ($max + 1)
