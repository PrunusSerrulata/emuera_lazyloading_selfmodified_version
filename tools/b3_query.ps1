# B.3 Query Script - count typeof/GetOperandType/IsInteger/IsString/bitflag distribution
# Usage: powershell -ExecutionPolicy Bypass -File tools\b3_query.ps1 [-Detail] [-Mode All|B15|B16]
# -Detail: show per-line detail
# -Mode:  All  = show all categories (default)
#         B15  = show only B.3-15 relevant (IsInteger/IsString + bit flags)
#         B16  = show only B.3-16 relevant (typeof + GetOperandType)

param(
    [switch]$Detail,
    [ValidateSet('All','B15','B16')]
    [string]$Mode = 'All'
)

$EmueraDir = Join-Path $PSScriptRoot "..\Emuera"
if (-not (Test-Path $EmueraDir)) {
    Write-Error "ERROR: Emuera dir not found at $EmueraDir"
    exit 1
}

Write-Host '========================================' -ForegroundColor Cyan
Write-Host " B.3 Codebase Query - Mode: $Mode" -ForegroundColor Cyan
Write-Host '========================================' -ForegroundColor Cyan
Write-Host ''

function Show-Category {
    param(
        [string]$Label,
        [string]$Pattern,
        [string]$Color = 'Yellow'
    )
    Write-Host "--- $Label ---" -ForegroundColor $Color
    $matches = Get-ChildItem -Path $EmueraDir -Recurse -Filter *.cs | Select-String $Pattern
    $byFile = $matches | Group-Object Path | Sort-Object Count -Descending
    $total = 0
    foreach ($g in $byFile) {
        $relPath = $g.Name.Replace($EmueraDir + '\', 'Emuera\')
        $msg = '{0,4}  {1}' -f $g.Count, $relPath
        Write-Host "  $msg"
        if ($Detail) {
            foreach ($m in $g.Group) {
                $trimmed = $m.Line.Trim()
                if ($trimmed.Length -gt 120) { $trimmed = $trimmed.Substring(0, 117) + '...' }
                Write-Host "         L$($m.LineNumber): $trimmed" -ForegroundColor DarkGray
            }
        }
        $total += $g.Count
    }
    Write-Host ''
    return $total
}

$showB15 = ($Mode -eq 'All' -or $Mode -eq 'B15')
$showB16 = ($Mode -eq 'All' -or $Mode -eq 'B16')

$totalLong = 0; $totalStr = 0; $totalDouble = 0; $totalOp = 0
$totalIsInt = 0; $totalIsStr = 0; $totalIsFloat = 0; $totalBitFlags = 0

if ($showB16) {
    $totalLong = Show-Category 'typeof(long)' 'typeof\(long\)'
    $totalStr = Show-Category 'typeof(string)' 'typeof\(string\)'
    $totalDouble = Show-Category 'typeof(double)' 'typeof\(double\)'
    $totalOp = Show-Category 'GetOperandType' 'GetOperandType\(\)'
}

if ($showB15) {
    $totalIsInt = Show-Category 'IsInteger (B.3-15 target)' '\.IsInteger\b'
    $totalIsStr = Show-Category 'IsString (B.3-15 target)' '\.IsString\b'
    $totalIsFloat = Show-Category 'IsFloat (already correct)' '\.IsFloat\b'
    $totalBitFlags = Show-Category '__INTEGER__/__STRING__ bit flags' '__INTEGER__|__STRING__'
}

# === Summary ===
Write-Host '========================================' -ForegroundColor Cyan
if ($showB16) {
    Write-Host "  typeof(long)   : $totalLong" -ForegroundColor White
    Write-Host "  typeof(string) : $totalStr" -ForegroundColor White
    Write-Host "  typeof(double) : $totalDouble" -ForegroundColor White
    Write-Host "  GetOperandType : $totalOp" -ForegroundColor White
    $totalTypeof = $totalLong + $totalStr + $totalDouble
    Write-Host "  typeof total   : $totalTypeof  (B.3-16 scope)" -ForegroundColor Green
    Write-Host '  ---' -ForegroundColor DarkGray
}
if ($showB15) {
    Write-Host "  .IsInteger     : $totalIsInt" -ForegroundColor White
    Write-Host "  .IsString      : $totalIsStr" -ForegroundColor White
    Write-Host "  .IsFloat       : $totalIsFloat  (already correct)" -ForegroundColor Green
    Write-Host "  __INTEGER__/__STRING__ : $totalBitFlags" -ForegroundColor White
    $totalBinary = $totalIsInt + $totalIsStr
    Write-Host "  IsInt+IsStr    : $totalBinary  (B.3-15 scope)" -ForegroundColor Green
    Write-Host '  ---' -ForegroundColor DarkGray
}
Write-Host '========================================' -ForegroundColor Cyan
