# B.3 Verify Script - build + residual check
# Usage: powershell -ExecutionPolicy Bypass -File tools\b3_verify.ps1 [-Batch name]
# Example: powershell -ExecutionPolicy Bypass -File tools\b3_verify.ps1 -Batch 1a

param(
    [string]$Batch = ""
)

$ProjectRoot = Split-Path $PSScriptRoot -Parent
$Csproj = Join-Path $ProjectRoot "Emuera\Emuera.csproj"

if (-not (Test-Path $Csproj)) {
    Write-Error "ERROR: csproj not found at $Csproj"
    exit 1
}

$baseline = @{
    typeofLong = 336
    typeofStr  = 410
    getOpType  = 194
}

Write-Host '========================================' -ForegroundColor Cyan
if ($Batch) {
    Write-Host " B.3 Verify - Batch $Batch" -ForegroundColor Cyan
} else {
    Write-Host ' B.3 Verify - Full Check' -ForegroundColor Cyan
}
Write-Host '========================================' -ForegroundColor Cyan
Write-Host ''

# === Step 1: Build ===
Write-Host '[1/3] Building...' -ForegroundColor Yellow
$buildOutput = & dotnet build $Csproj 2>&1
$buildResult = $LASTEXITCODE

if ($buildResult -eq 0) {
    Write-Host '       BUILD OK (0 errors)' -ForegroundColor Green
} else {
    Write-Host "       BUILD FAILED (exit code $buildResult)" -ForegroundColor Red
    $errors = $buildOutput | Where-Object { $_ -match 'error CS\d+:' }
    if ($errors) {
        Write-Host ''
        Write-Host 'Build errors:' -ForegroundColor Red
        foreach ($e in $errors) {
            Write-Host "  $e" -ForegroundColor Red
        }
    }
    Write-Host ''
    Write-Host '========================================' -ForegroundColor Cyan
    Write-Host ' Result: FAIL (build errors)' -ForegroundColor Red
    Write-Host '========================================' -ForegroundColor Cyan
    exit 1
}

Write-Host ''

# === Step 2: typeof(long) residuals ===
Write-Host '[2/3] Counting typeof(long) residuals...' -ForegroundColor Yellow
$typeofLong = Get-ChildItem -Path "$ProjectRoot\Emuera" -Recurse -Filter *.cs | Select-String 'typeof\(long\)'
$typeofLongCount = @($typeofLong).Count

$clr = if ($typeofLongCount -lt 400) { 'Green' } else { 'Yellow' }
Write-Host "       typeof(long): $typeofLongCount remaining" -ForegroundColor $clr

$typeofLongByFile = $typeofLong | Group-Object Path | Sort-Object Count -Descending
if ($typeofLongByFile.Count -gt 0 -and $typeofLongByFile.Count -le 10) {
    foreach ($g in $typeofLongByFile) {
        $relPath = $g.Name.Replace($ProjectRoot + '\', '')
        Write-Host "         $($g.Count)  $relPath" -ForegroundColor DarkGray
    }
}

Write-Host ''

# === Step 3: typeof(string) residuals ===
Write-Host '[3/3] Counting typeof(string) residuals...' -ForegroundColor Yellow
$typeofStr = Get-ChildItem -Path "$ProjectRoot\Emuera" -Recurse -Filter *.cs | Select-String 'typeof\(string\)'
$typeofStrCount = @($typeofStr).Count

$clr = if ($typeofStrCount -lt 450) { 'Green' } else { 'Yellow' }
Write-Host "       typeof(string): $typeofStrCount remaining" -ForegroundColor $clr

$typeofStrByFile = $typeofStr | Group-Object Path | Sort-Object Count -Descending
if ($typeofStrByFile.Count -gt 0 -and $typeofStrByFile.Count -le 10) {
    foreach ($g in $typeofStrByFile) {
        $relPath = $g.Name.Replace($ProjectRoot + '\', '')
        Write-Host "         $($g.Count)  $relPath" -ForegroundColor DarkGray
    }
}

Write-Host ''

# === Summary ===
Write-Host '========================================' -ForegroundColor Cyan
$totalResidual = $typeofLongCount + $typeofStrCount
$totalOriginal = $baseline.typeofLong + $baseline.typeofStr
$reduced = $totalOriginal - $totalResidual
Write-Host "  typeof residual: $totalResidual / $totalOriginal (cleared $reduced)" -ForegroundColor $(if ($reduced -gt 0) { 'Green' } else { 'White' })
Write-Host '  Build: PASS' -ForegroundColor Green
Write-Host '  Result: PASS' -ForegroundColor Green
Write-Host '========================================' -ForegroundColor Cyan
