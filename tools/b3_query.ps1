# B.3 Query Script - count typeof/GetOperandType distribution
# Usage: powershell -ExecutionPolicy Bypass -File tools\b3_query.ps1 [-Detail]
# -Detail: show per-line detail for typeof(long)

param([switch]$Detail)

$EmueraDir = Join-Path $PSScriptRoot "..\Emuera"
if (-not (Test-Path $EmueraDir)) {
    Write-Error "ERROR: Emuera dir not found at $EmueraDir"
    exit 1
}

Write-Host '========================================' -ForegroundColor Cyan
Write-Host ' B.3 Codebase Query - typeof / GetOperandType' -ForegroundColor Cyan
Write-Host '========================================' -ForegroundColor Cyan
Write-Host ''

# === typeof(long) ===
Write-Host '--- typeof(long) ---' -ForegroundColor Yellow
$typeofLong = Get-ChildItem -Path $EmueraDir -Recurse -Filter *.cs | Select-String 'typeof\(long\)'
$typeofLongByFile = $typeofLong | Group-Object Path | Sort-Object Count -Descending

$totalLong = 0
foreach ($g in $typeofLongByFile) {
    $relPath = $g.Name.Replace($EmueraDir + '\', 'Emuera\')
    $msg = '{0,4}  {1}' -f $g.Count, $relPath
    Write-Host "  $msg"
    if ($Detail) {
        foreach ($m in $g.Group) {
            $trimmed = $m.Line.Trim()
            Write-Host "         L$($m.LineNumber): $trimmed" -ForegroundColor DarkGray
        }
    }
    $totalLong += $g.Count
}

Write-Host ''

# === typeof(string) ===
Write-Host '--- typeof(string) ---' -ForegroundColor Yellow
$typeofStr = Get-ChildItem -Path $EmueraDir -Recurse -Filter *.cs | Select-String 'typeof\(string\)'
$typeofStrByFile = $typeofStr | Group-Object Path | Sort-Object Count -Descending

$totalStr = 0
foreach ($g in $typeofStrByFile) {
    $relPath = $g.Name.Replace($EmueraDir + '\', 'Emuera\')
    $msg = '{0,4}  {1}' -f $g.Count, $relPath
    Write-Host "  $msg"
    $totalStr += $g.Count
}

Write-Host ''

# === GetOperandType ===
Write-Host '--- GetOperandType ---' -ForegroundColor Yellow
$getOp = Get-ChildItem -Path $EmueraDir -Recurse -Filter *.cs | Select-String 'GetOperandType\(\)'
$getOpByFile = $getOp | Group-Object Path | Sort-Object Count -Descending

$totalOp = 0
foreach ($g in $getOpByFile) {
    $relPath = $g.Name.Replace($EmueraDir + '\', 'Emuera\')
    $msg = '{0,4}  {1}' -f $g.Count, $relPath
    Write-Host "  $msg"
    $totalOp += $g.Count
}

Write-Host ''

# === Summary ===
Write-Host '========================================' -ForegroundColor Cyan
Write-Host "  typeof(long)  : $totalLong" -ForegroundColor White
Write-Host "  typeof(string): $totalStr" -ForegroundColor White
Write-Host "  GetOperandType: $totalOp" -ForegroundColor White
Write-Host '  ---' -ForegroundColor DarkGray

$totalAll = $totalLong + $totalStr + $totalOp
$uniqueFiles = @($typeofLongByFile.Name) + @($typeofStrByFile.Name) + @($getOpByFile.Name) | Select-Object -Unique
Write-Host "  Total to process: $totalAll items" -ForegroundColor Green
Write-Host "  Files involved  : $($uniqueFiles.Count) files" -ForegroundColor Green
Write-Host '========================================' -ForegroundColor Cyan
