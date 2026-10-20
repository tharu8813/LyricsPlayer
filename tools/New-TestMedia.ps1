param([Parameter(Mandatory=$true)][string]$FFmpeg, [string]$Output = (Join-Path $PSScriptRoot '../artifacts/test-media'))
$ErrorActionPreference = 'Stop'
$Output = [IO.Path]::GetFullPath($Output)
New-Item -ItemType Directory -Force -Path $Output | Out-Null
$video = Join-Path $Output '01 - Evening light.mp4'
& $FFmpeg -hide_banner -loglevel error -y -f lavfi -i 'testsrc2=size=640x360:rate=30' -f lavfi -i 'anullsrc=r=44100:cl=stereo' -t 18 -c:v libx264 -preset ultrafast -pix_fmt yuv420p -c:a aac $video
if ($LASTEXITCODE -ne 0) { throw 'MP4 fixture creation failed' }
& $FFmpeg -hide_banner -loglevel error -y -i $video -c copy (Join-Path $Output '03 - Window seat.mkv')
if ($LASTEXITCODE -ne 0) { throw 'MKV fixture creation failed' }
& $FFmpeg -hide_banner -loglevel error -y -i $video -c:v wmv2 -b:v 600k -c:a wmav2 (Join-Path $Output '04 - After hours.wmv')
if ($LASTEXITCODE -ne 0) { throw 'WMV fixture creation failed' }
& $FFmpeg -hide_banner -loglevel error -y -f lavfi -i 'anullsrc=r=44100:cl=stereo' -t 18 -c:a libmp3lame (Join-Path $Output '02 - Quiet room.mp3')
if ($LASTEXITCODE -ne 0) { throw 'MP3 fixture creation failed' }
@'
[00:00.00]A little closer to the music
[00:02.00]Evening light on the window
[00:04.00]긴 가사도 자연스럽게 여러 줄로 이어지고, 지금 듣는 한 줄에 머무릅니다.
[00:06.00]Every word finds its own time
[00:08.00]音楽と歌詞が同じ場所で出会う
[00:10.00]A quiet moment between the lines
[00:12.00]이전과 다음 사이에 흐르는 음악
[00:14.00]The music stays with you
[00:16.00]One more line, one more moment
'@ | Set-Content (Join-Path $Output '01 - Evening light.lrc') -Encoding utf8
@'
1
00:00:00,000 --> 00:00:02,000
The first line
그리고 두 번째 줄

2
00:00:08,000 --> 00:00:10,000
After a long pause

3
00:00:12,000 --> 00:00:17,500
끝까지 같은 타임라인
'@ | Set-Content (Join-Path $Output '02 - Quiet room.srt') -Encoding utf8
Write-Output $Output
