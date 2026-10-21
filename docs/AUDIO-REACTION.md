# Playlist Canvas 기준 음악 반응과 가사 시계

> 아래는 이전 구현과 당시 실행 결과의 기록입니다. 현재 분석 분리·탐색 응답 확인·설정·배포 및 미검증 범위는 [Critical Audit](CRITICAL-AUDIT.md)을 기준으로 확인하세요.

기준 저장소: [Playlist Canvas](https://github.com/tharu8813/Playlist-Canvas), 커밋 `8cd38d42c10b3ca6f18b4d7f7e0b6760e548816e`. 2026-10-21 로컬 체크아웃의 origin·HEAD와 GitHub main이 같은 커밋임을 확인하고 실제 구현을 읽었습니다.

## 채택한 동작

- `app/renderer/python_visualizer.py`의 음악 반응: 8kHz mono, 512 Hann 창, 24개 로그 주파수 대역 중 가장 낮은 4개 대역 평균, 곡별 20/95 percentile 정규화, 60ms attack·400ms release. 이 시간은 90% 변화에 도달하는 시간입니다.
- `app/animation/curves.py`의 bass_scale: `1 + level * strength`. 사용자 요청으로 **강도는 기본 0.25의 절반인 0.125**를 사용합니다. 음악의 추가 확대 상한은 12.5%입니다.
- `app/animation/lyrics.py`와 Source 기본값: 가사 이동 360ms Cubic EaseInOut, 현재 줄 기본 1.08배, 주변 줄 opacity 0.34. 확대 중 줄바꿈과 레이아웃을 다시 계산하지 않습니다.

참조의 현재 줄 음악 확대와 기본 가사 전환을 맞췄습니다. Canvas의 편집기, 여러 방향 이동, 바운스·기울기·단어별 노래방 편집 기능 전체를 옮긴 것은 아닙니다. 기존 LyricsPlayer의 LRC/SRT 의미, +200ms 보정, 클릭 탐색과 수동/자동 스크롤은 보존합니다. 외부 소스 파일이나 리소스를 프로젝트에 복사하지 않고 C#/WPF로 구현했습니다.

## 구현과 지연 개선

`Services/MusicReactionService.cs`는 기존 LibVLC 엔진에 분석 전용 MediaPlayer를 잠시 만듭니다. `transcode → smem`으로 오디오만 8kHz PCM으로 받아 빠르게 분석하고 해제합니다. `no-time-sync`로 재생 속도에 묶이지 않으며 `no-sout-video`로 영상 스트림을 차단합니다. 실제 재생 MediaPlayer의 소리·영상 출력을 교체하지 않습니다. 메인과 PiP는 여전히 같은 재생 세션을 공유합니다. 분석은 취소 가능하고 최신 파일의 결과만 반영합니다. 분석 실패 시 기존 재생과 가사는 계속됩니다.

`Core/MusicReactionTimeline.cs`는 전체 PCM 대신 512개 샘플의 순환 버퍼와 30fps 저음 값만 보관합니다. 필요한 4개 저역 FFT bin의 DFT를 직접 계산해 추가 패키지를 피했습니다. 대역 내 실제 최대치와 대역당 5개 보간 probe를 함께 사용합니다. 완료된 반응 곡선은 재생 시각으로 읽으며 WPF 프레임 사이에는 보간합니다. 실시간 감지 결과를 나중에 실행하는 큐가 없고, 다른 앱의 소리나 마이크에 영향을 받지 않습니다. Windows process loopback 코드는 제거되어 Windows 11 전용 API 제한도 없습니다.

`Core/PlaybackClock.cs`는 LibVLC의 드문 시간 갱신 사이를 단조 시계로 보간합니다. 100ms 이하의 작은 보고 오차는 250ms에 걸쳐 흡수해 가사가 순간적으로 뒤로 돌아가지 않게 합니다. 입력이 멈췄을 때 계속 앞서가지 않도록 마지막 native 갱신 후 300ms로 예측을 제한합니다. 탐색 시에는 목표 시각부터 바로 진행하고, 200ms 동안 이전 native 값을 무시합니다. 일시정지와 속도 변경에서도 시계를 재조정합니다. 기본 +200ms를 일괄 증가시키지는 않았습니다.

`Views/LyricsView.xaml(.cs)`는 현재 줄의 기본 강조와 음악 확대를 별도 Transform으로 합성합니다. 재생 중 저음 반응이 있는 View만 CompositionTarget.Rendering을 구독합니다. 50ms 앞의 곡선도 확인해 타격 직전에 구독을 시작하며 무음에서는 해제합니다. 일시정지·정지·Unloaded에서 구독을 해제하고 일시정지 시 음악 확대는 기본 배율로 가라앉습니다. 이는 정지한 화면을 편안하게 유지하기 위한 WPF 동작입니다. Windows 클라이언트 애니메이션 설정도 반영합니다.

## 수치 비교 및 실제 검사

`Tests/Fixtures/canvas-reaction-reference.json`은 실제 Playlist Canvas 함수를 실행해 만든 기준값입니다. `tools/Compare-CanvasReaction.py`로 참조 저장소를 수정하지 않고 재생성할 수 있습니다. 앱 실행이나 .NET 검사는 Python 설치를 요구하지 않습니다.

```powershell
# 선택: Playlist Canvas의 Python 환경으로 기준값 재생성
C:/path/to/Playlist-Canvas/.venv/Scripts/python.exe -B tools/Compare-CanvasReaction.py C:/path/to/Playlist-Canvas Tests/Fixtures/canvas-reaction-reference.json

./tools/New-TestMedia.ps1 -FFmpeg C:/tools/ffmpeg.exe
dotnet build Tests/LyricsPlayer.Checks.csproj -c Release
dotnet Tests/bin/Release/net8.0-windows/win-x64/LyricsPlayer.Checks.dll --audio-runtime
```

2026-10-21 최종 실행 결과:

- Release 빌드: 경고 0·오류 0. Windows x64 self-contained 게시 성공.
- 순수 검사 33개: 기존 LRC/SRT·타임라인, 동일 입력의 참조 곡선, 스트리밍 DFT, 무음, 보간 시계, 정지·재개·속도·탐색·정체와 작은 시계 오차 처리.
- 동일 합성 PCM에서 Playlist Canvas FFT/정규화/감쇠 결과와의 최대 오차 **3.78e-8**. 다른 디코더가 처리한 실제 파일의 출력까지 bit-identical하다는 의미는 아닙니다.
- 실제 LibVLC/WPF/PiP 포함 **총 61개 성공**, 종료 코드 0. 마지막 native player 해제 포함. 로그: 로컬 `artifacts/canvas-motion-checks.log`.
- 12초 테스트 음원의 사전 분석·재생 준비 약 142ms. 음악 확대 측정 배율 1.121, 상한 1.125 이내. PiP 동기화, 일시정지 시 프레임 구독 해제와 배율 복귀, 무음 탐색 시 과거 타격 미재생을 확인했습니다.
- 실제 재생에서 native 시간 갱신 사이에도 가사가 진행됐습니다. 1450ms에서 탐색한 뒤 +200ms 보정을 포함한 다음 가사 경계(1800ms)를 약 407ms 후 통과했습니다. 2배속에서는 벽시계 260ms 동안 약 504ms 진행했습니다. 이 결과는 소리 출력부터 디스플레이까지의 하드웨어 지연 측정이 아닙니다.
- MP4·MP3·MKV·WMV의 오디오 전용 분석 성공. 분석 중 취소, 빠른 곡 교체 시 이전 결과 배제, 분석과 메인 영상 출력의 공존 확인.
- 1만 줄 중 5개 행만 생성. 탐색 중 WPF 렌더링 표본 중앙 간격 3.4ms·p95 11.0ms는 GPU FPS 보장이 아닙니다.
- WPF 렌더링 미리보기로 가사 대비와 배치를 확인했습니다. `--hold`로 생성 음원을 재생하면서 60초간 시각 확인 창을 유지할 수 있습니다. 실제 데스크톱의 외부 캡처를 검증 근거로 사용하지 않았습니다.

LibVLC는 테스트 창의 thumbnail clip, 빠르게 삭제되는 테스트 파일 메타데이터의 cannot peek 및 급속 탐색 중 timestamp/no reference clock 메시지를 출력했으나 검사는 정상 종료됐습니다. 경고가 장시간·특수 미디어에서 미치는 영향은 별도 관찰이 필요합니다. 이번 명령은 전체화면 idle·다중 모니터 검사를 포함하지 않으며 이전 전체 회귀 실행의 입력 간섭 문제를 성공으로 보고하지 않습니다.

## 배포와 남은 한계

새 실행본: `artifacts/publish/canvas-motion-win-x64/LyricsPlayer.exe`.
배포: `artifacts/LyricsPlayer-WPF-canvas-motion-win-x64.zip`의 **폴더 전체**를 풉니다. 기존 audio-reactive/audio-visual/bass-pulse 실행본과 구분해 새 폴더를 사용하세요. LibVLC의 stream_out smem·transcode 플러그인도 필요하며 게시 폴더에 포함합니다.

BPM·악기 분리는 아니며 곡의 저역 에너지에 반응합니다. 매우 긴 파일의 분석 시간, 손상된 미디어, 특수 코덱과 장시간 실행은 추가 검증이 남아 있습니다. 분석이 실패하거나 제한 시간 2분을 넘으면 음악 확대를 생략하고 재생·가사를 유지합니다. Bluetooth·출력 드라이버·가사 원본 파일의 지연은 자동으로 추정하지 않으므로 기존 시간 보정으로 조절해야 할 수 있습니다. 다중 DPI/모니터와 전체화면 체감 비교는 미검증입니다.
