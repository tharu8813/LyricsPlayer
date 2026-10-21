param([Parameter(Mandatory=$true)][string]$Folder)
$ErrorActionPreference = 'Stop'
$Folder = [IO.Path]::GetFullPath($Folder)
$required = @('LyricsPlayer.exe', 'LyricsPlayer.dll', 'LICENSE', 'THIRD-PARTY-NOTICES.md',
    'licenses/LibVLC-LGPL-2.1.txt', 'licenses/VLC-GPL-2.0.txt', 'licenses/DotNet-LICENSE.txt',
    'licenses/DotNet-THIRD-PARTY-NOTICES.txt', 'licenses/WindowsDesktop-LICENSE.txt',
    'hostfxr.dll', 'coreclr.dll', 'PresentationFramework.dll',
    'libvlc/win-x64/libvlc.dll', 'libvlc/win-x64/libvlccore.dll',
    'libvlc/win-x64/plugins/stream_out/libstream_out_smem_plugin.dll',
    'libvlc/win-x64/plugins/stream_out/libstream_out_transcode_plugin.dll')
foreach ($file in $required) {
    if (-not (Test-Path -LiteralPath (Join-Path $Folder $file) -PathType Leaf)) { throw "Missing portable dependency: $file" }
}
if (Test-Path -LiteralPath (Join-Path $Folder 'libvlc/win-x86')) { throw 'Unexpected x86 runtime in x64 distribution' }
$config = Get-Content -LiteralPath (Join-Path $Folder 'LyricsPlayer.runtimeconfig.json') -Raw | ConvertFrom-Json
if (-not $config.runtimeOptions.includedFrameworks) { throw 'Runtime config is not self-contained' }
$plugins = @(Get-ChildItem -LiteralPath (Join-Path $Folder 'libvlc/win-x64/plugins') -Recurse -File -Filter '*.dll')
if ($plugins.Count -lt 100) { throw "Incomplete native plugin tree: $($plugins.Count)" }
$files = @(Get-ChildItem -LiteralPath $Folder -Recurse -File)
[pscustomobject]@{ Folder=$Folder; Files=$files.Count; Bytes=($files | Measure-Object Length -Sum).Sum; NativePlugins=$plugins.Count; SHA256=(Get-FileHash -LiteralPath (Join-Path $Folder 'LyricsPlayer.dll')).Hash }
