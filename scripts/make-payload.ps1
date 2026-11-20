$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$payloadDir = Join-Path $root 'payload'
$stage = Join-Path $payloadDir '.stage'
$zipPath = Join-Path $payloadDir 'du_lieu.zip'

$node = 'node'

& $node (Join-Path $PSScriptRoot 'make-payload.cjs')
if ($LASTEXITCODE -ne 0) { throw "make-payload.cjs thất bại (exit $LASTEXITCODE)" }

if (Test-Path -LiteralPath $zipPath) { Remove-Item -LiteralPath $zipPath -Force }

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

$zip = [System.IO.Compression.ZipFile]::Open($zipPath, 'Create')
try {
  $base = (Resolve-Path -LiteralPath $stage).Path.TrimEnd('\') + '\'
  $items = Get-ChildItem -LiteralPath $stage -Recurse -File
  $i = 0
  foreach ($item in $items) {
    $name = $item.FullName.Substring($base.Length).Replace('\', '/')
    [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $item.FullName, $name, [System.IO.Compression.CompressionLevel]::NoCompression) | Out-Null
    $i++
    if ($i % 25 -eq 0) { Write-Host "  $i/$($items.Count) file" }
  }
}
finally {
  $zip.Dispose()
}

Remove-Item -LiteralPath $stage -Recurse -Force

$size = (Get-Item -LiteralPath $zipPath).Length
Write-Host ("Payload: {0} ({1:N1} MB)" -f $zipPath, ($size / 1MB))
