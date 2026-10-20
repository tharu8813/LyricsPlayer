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
$audioFolder = Join-Path $Output 'audio-reaction'
New-Item -ItemType Directory -Force -Path $audioFolder | Out-Null
& $FFmpeg -hide_banner -loglevel error -y -f lavfi -i 'aevalsrc=0.35*sin(2*PI*1000*t)*lt(t\,3)+0.55*exp(-15*mod(t-3\,0.5))*sin(2*PI*65*t)*between(t\,3\,6):s=44100:d=12' -c:a libmp3lame (Join-Path $audioFolder 'Reaction.mp3')
if ($LASTEXITCODE -ne 0) { throw 'Audio reaction fixture creation failed' }
& $FFmpeg -hide_banner -loglevel error -y -f lavfi -i 'aevalsrc=0.35*exp(-8*mod(t\,0.5))*sin(2*PI*90*t)+0.08*sin(2*PI*330*t)*(0.5+0.5*sin(2*PI*0.5*t)):s=44100:d=36' -c:a libmp3lame (Join-Path $audioFolder 'Rhythm.mp3')
if ($LASTEXITCODE -ne 0) { throw 'Rhythmic audio fixture creation failed' }
@'
[00:00]베이스가 칠 때 커지는 가사
[00:06]리듬을 따라 천천히 제자리로
[00:12]한 줄의 가사와 함께 듣는 음악
[00:18]A little closer to the music
[00:24]같은 음악, 같은 가사
[00:30]천천히 원래 크기로 돌아오는 순간
'@ | Set-Content (Join-Path $audioFolder 'Rhythm.lrc') -Encoding utf8
@'
[00:00]고음에는 크기가 그대로
[00:03]베이스가 칠 때 커지는 가사
[00:06]천천히 원래 크기로
[00:09]같은 가사, 같은 음악
'@ | Set-Content (Join-Path $audioFolder 'Reaction.lrc') -Encoding utf8
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
