param(
    [Parameter(Mandatory=$true)]
    [string]$PublishDir
)

$ErrorActionPreference = 'Stop'

Write-Host "Checking publish folder: $PublishDir"
if (!(Test-Path $PublishDir)) {
    throw "Publish folder does not exist: $PublishDir"
}

$exe = Join-Path $PublishDir 'LivingKoreaStudio.exe'
if (!(Test-Path $exe)) {
    throw "LivingKoreaStudio.exe was not found in publish folder."
}

$runtimeDir = Join-Path $PublishDir 'runtimes\win-x64\native'
$nativeFiles = @()
if (Test-Path $runtimeDir) {
    $nativeFiles += Get-ChildItem $runtimeDir -File -ErrorAction SilentlyContinue
}
$nativeFiles += Get-ChildItem $PublishDir -File -ErrorAction SilentlyContinue | Where-Object { $_.Extension -eq '.dll' }

$whisperNative = $nativeFiles | Where-Object { $_.Name -match 'whisper|ggml|ggmlbase|ggml-cpu|ggml-base' }
if (!$whisperNative -or $whisperNative.Count -eq 0) {
    Write-Host "Files in publish folder:" -ForegroundColor Yellow
    Get-ChildItem $PublishDir -Recurse | Select-Object FullName, Length | Format-Table -AutoSize
    throw "Whisper native runtime DLL was not found. Check Whisper.net.Runtime and PublishSingleFile=false."
}

$singleFileRisk = Get-ChildItem $PublishDir -Recurse -File | Measure-Object
if ($singleFileRisk.Count -lt 10) {
    throw "Publish output has too few files. It may have been published as SingleFile. Keep PublishSingleFile=false."
}

Write-Host "OK: EXE found." -ForegroundColor Green
Write-Host "OK: Whisper native runtime files found:" -ForegroundColor Green
$whisperNative | Select-Object FullName, Length | Format-Table -AutoSize
Write-Host "OK: Publish validation passed." -ForegroundColor Green
