param(
    [string[]]$Case = @(),
    [int]$BudgetSeconds = 300
)
$ErrorActionPreference = "Stop"
$cliDir = Split-Path $PSScriptRoot -Parent
$repoRoot = Split-Path $cliDir -Parent
$project = Join-Path $cliDir "Emuera.ReferenceCli.csproj"
$publishDir = Join-Path $cliDir "bin/smoke-win-x64"

dotnet restore $project -p:Configuration=Debug-NAudio -p:Platform=x64 -p:RuntimeIdentifiers=win-x64 -r win-x64 -p:SelfContained=true -p:NuGetAudit=false
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
dotnet publish $project -c Debug-NAudio -p:Platform=x64 -p:RuntimeIdentifiers=win-x64 -r win-x64 --self-contained true -p:PublishSingleFile=false --no-restore -o $publishDir --nologo
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
python -c 'import ast, pathlib, sys; ast.parse(pathlib.Path(sys.argv[1]).read_text())' (Join-Path $PSScriptRoot "smoke.py")
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
git -C $repoRoot diff --check
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$testArgs = @((Join-Path $PSScriptRoot "smoke.py"), "--exe", (Join-Path $publishDir "Emuera.ReferenceCli.exe"), "--budget-seconds", $BudgetSeconds)
foreach ($name in $Case) { $testArgs += @("--case", $name) }
& python @testArgs
exit $LASTEXITCODE
