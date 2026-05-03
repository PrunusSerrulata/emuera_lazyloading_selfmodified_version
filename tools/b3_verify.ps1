# B.3 Verify Script - build + residual check
# Usage: powershell -ExecutionPolicy Bypass -File tools\b3_verify.ps1 [-Batch name] [-Mode All|B15|B16]
# Example: powershell -ExecutionPolicy Bypass -File tools\b3_verify.ps1 -Batch 15a -Mode B15

param(
    [string]$Batch = "",
    [ValidateSet('All','B15','B16')]
    [string]$Mode = 'All'
)

$ProjectRoot = Split-Path $PSScriptRoot -Parent
$Csproj = Join-Path $ProjectRoot "Emuera\Emuera.csproj"

if (-not (Test-Path $Csproj)) {
    Write-Error "ERROR: csproj not found at $Csproj"
    exit 1
}

Write-Host '========================================' -ForegroundColor Cyan
if ($Batch) {
    Write-Host " B.3 Verify - Batch $Batch (Mode: $Mode)" -ForegroundColor Cyan
} else {
    Write-Host " B.3 Verify - Full Check (Mode: $Mode)" -ForegroundColor Cyan
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

$showB15 = ($Mode -eq 'All' -or $Mode -eq 'B15')
$showB16 = ($Mode -eq 'All' -or $Mode -eq 'B16')

# === Step 2: B.3-15 residuals ===
if ($showB15) {
    Write-Host '[2/3] Counting B.3-15 residuals (IsInteger/IsString/bit flags)...' -ForegroundColor Yellow

    $isInt = Get-ChildItem -Path "$ProjectRoot\Emuera" -Recurse -Filter *.cs | Select-String '\.IsInteger\b'
    $isIntCount = @($isInt).Count
    Write-Host "       .IsInteger: $isIntCount remaining" -ForegroundColor $(if ($isIntCount -gt 0) { 'Yellow' } else { 'Green' })

    $isStr = Get-ChildItem -Path "$ProjectRoot\Emuera" -Recurse -Filter *.cs | Select-String '\.IsString\b'
    $isStrCount = @($isStr).Count
    Write-Host "       .IsString: $isStrCount remaining" -ForegroundColor $(if ($isStrCount -gt 0) { 'Yellow' } else { 'Green' })

    $bitFlags = Get-ChildItem -Path "$ProjectRoot\Emuera" -Recurse -Filter *.cs | Select-String '__INTEGER__|__STRING__'
    $bitFlagsCount = @($bitFlags).Count
    $bitFlagsByFile = $bitFlags | Group-Object Path | Sort-Object Count -Descending
    Write-Host "       __INTEGER__/__STRING__: $bitFlagsCount remaining" -ForegroundColor $(if ($bitFlagsCount -gt 193) { 'Yellow' } else { 'Green' })

    if ($bitFlagsByFile.Count -gt 0 -and $bitFlagsByFile.Count -le 10) {
        foreach ($g in $bitFlagsByFile) {
            $relPath = $g.Name.Replace($ProjectRoot + '\', '')
            Write-Host "         $($g.Count)  $relPath" -ForegroundColor DarkGray
        }
    }

    Write-Host ''
}

# === Step 3: B.3-16 residuals ===
if ($showB16) {
    $stepLabel = if ($showB15) { '[3/3]' } else { '[2/3]' }
    Write-Host "$stepLabel Counting B.3-16 residuals (typeof/GetOperandType)..." -ForegroundColor Yellow

    $typeofLong = Get-ChildItem -Path "$ProjectRoot\Emuera" -Recurse -Filter *.cs | Select-String 'typeof\(long\)'
    $typeofLongCount = @($typeofLong).Count
    Write-Host "       typeof(long): $typeofLongCount remaining" -ForegroundColor Yellow

    $typeofStr = Get-ChildItem -Path "$ProjectRoot\Emuera" -Recurse -Filter *.cs | Select-String 'typeof\(string\)'
    $typeofStrCount = @($typeofStr).Count
    Write-Host "       typeof(string): $typeofStrCount remaining" -ForegroundColor Yellow

    $typeofDouble = Get-ChildItem -Path "$ProjectRoot\Emuera" -Recurse -Filter *.cs | Select-String 'typeof\(double\)'
    $typeofDoubleCount = @($typeofDouble).Count
    Write-Host "       typeof(double): $typeofDoubleCount remaining" -ForegroundColor Yellow

    $getOp = Get-ChildItem -Path "$ProjectRoot\Emuera" -Recurse -Filter *.cs | Select-String 'GetOperandType\(\)'
    $getOpCount = @($getOp).Count
    Write-Host "       GetOperandType: $getOpCount remaining" -ForegroundColor Yellow

    Write-Host ''
}

# === Summary ===
Write-Host '========================================' -ForegroundColor Cyan
Write-Host '  Build: PASS' -ForegroundColor Green
if ($showB15) {
    $binaryTotal = $isIntCount + $isStrCount
    Write-Host "  B.3-15: IsInt=$isIntCount IsStr=$isStrCount total=$binaryTotal bitflags=$bitFlagsCount" -ForegroundColor White
}
if ($showB16) {
    $typeofTotal = $typeofLongCount + $typeofStrCount + $typeofDoubleCount
    Write-Host "  B.3-16: typeof=$typeofTotal GetOp=$getOpCount" -ForegroundColor White
}
Write-Host '  Result: PASS' -ForegroundColor Green
Write-Host '========================================' -ForegroundColor Cyan
