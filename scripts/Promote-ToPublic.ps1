# Publish the current private dev tree onto public main as one commit.
# Public history keeps each promotion. Private wip stays on dev.
# Does not force-push and will not push the dev branch to public.
# Usage (clean tree):  powershell -File scripts/Promote-ToPublic.ps1 -Message "What a stranger should see."

param(
    [Parameter(Mandatory = $true)]
    [string]$Message
)

$ErrorActionPreference = "Stop"

function Get-RemoteUrl([string]$name) {
    (git remote get-url $name 2>$null)
}

function Get-Pat {
    if ($env:GITHUB_PAT) { return $env:GITHUB_PAT }
    return [Environment]::GetEnvironmentVariable("GITHUB_PAT", "User")
}

function Push-Ref([string]$remoteName, [string]$refspec) {
    $url = Get-RemoteUrl $remoteName
    $pat = Get-Pat
    if ($pat -and $url -match "github\.com[/:]([^/]+)/([^/.]+)") {
        $owner = $Matches[1]
        $repo = $Matches[2]
        git -c credential.helper= push "https://x-access-token:${pat}@github.com/$owner/$repo.git" $refspec
        if ($LASTEXITCODE -ne 0) { throw "git push $remoteName $refspec failed" }
        return
    }
    git push $remoteName $refspec
    if ($LASTEXITCODE -ne 0) { throw "git push $remoteName $refspec failed" }
}

$public = Get-RemoteUrl "public"
$origin = Get-RemoteUrl "origin"
if (-not $public) { throw "Remote 'public' is missing." }
if (-not $origin) { throw "Remote 'origin' is missing." }
if ($public -notmatch "github\.com[/:]PuffinProof/PuffinProof(\.git)?$") {
    throw "Remote 'public' must be PuffinProof/PuffinProof, got $public"
}
if ($origin -notmatch "github\.com[/:]gregorylejeune/puffinproof-dev(\.git)?$") {
    throw "Remote 'origin' must be gregorylejeune/puffinproof-dev, got $origin"
}

git update-index -q --refresh
if (git status --porcelain) {
    throw "Working tree is dirty. Commit or stash before promoting."
}

git fetch public
if ($LASTEXITCODE -ne 0) { throw "git fetch public failed" }

git checkout main
git merge --ff-only public/main
if ($LASTEXITCODE -ne 0) { throw "main could not fast-forward to public/main" }

$devTree = (git rev-parse "dev^{tree}").Trim()
$mainTree = (git rev-parse "main^{tree}").Trim()
if ($devTree -eq $mainTree) {
    git checkout dev
    throw "Nothing to promote. main already matches the dev tree."
}

$parent = (git rev-parse main).Trim()
$new = (git commit-tree $devTree -p $parent -m $Message).Trim()
if (-not $new) { throw "git commit-tree failed" }
git update-ref refs/heads/main $new
Push-Ref "public" "main:main"

git checkout dev
git merge -s ours --no-edit -m "Record public promotion." main
if ($LASTEXITCODE -ne 0) { throw "could not record promotion on dev" }
Push-Ref "origin" "dev:dev"
Push-Ref "origin" "main:main"

Write-Host "Public main is now $(git rev-parse main)"
Write-Host "Private dev recorded the promotion."
