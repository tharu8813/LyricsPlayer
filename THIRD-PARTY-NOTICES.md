# Third-party components

LyricsPlayer is distributed under the GPL v3 text in `LICENSE`. The portable
folder also includes these unmodified dependencies:

| Component | Version | Package license / source |
|---|---|---|
| LibVLCSharp / LibVLCSharp.WPF | 3.10.1 | LGPL-2.1-or-later; https://code.videolan.org/videolan/LibVLCSharp |
| VideoLAN.LibVLC.Windows | 3.0.21 | Package declares LGPL-2.1-or-later; https://code.videolan.org/videolan/libvlc-nuget |
| VLC native runtime and plugins | 3.0.21 | VLC contains LGPL and GPL components; https://code.videolan.org/videolan/vlc/-/tree/3.0.21 |
| .NET / Windows Desktop runtime | 8.0.x (see included runtime configuration) | MIT and component notices; https://github.com/dotnet/runtime and https://github.com/dotnet/wpf |

`licenses/` contains the upstream LGPL 2.1, GPL 2.0, .NET license and third-party
notices, and Windows Desktop license. The package license does not imply that
every native plugin has the same license. Native DLLs and plugins remain separate
and replaceable in the portable folder.

Source locations: https://download.videolan.org/pub/videolan/vlc/3.0.21/ and
https://code.videolan.org/videolan/LibVLCSharp/-/tags/3.10.1 . These upstream links
are provenance references, not a substitute for fulfilling corresponding-source
obligations when redistributing. Audit native plugin and codec obligations and
retain the corresponding sources before publishing a public binary release.
