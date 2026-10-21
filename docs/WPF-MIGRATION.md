# WPF migration: architecture and verification

> 아래는 이전 구현과 당시 실행 결과의 기록입니다. 현재 분석 분리·탐색 응답 확인·설정·배포 및 미검증 범위는 [Critical Audit](CRITICAL-AUDIT.md)을 기준으로 확인하세요.

기준: `44fb0d4d9774c7e38061052467ab54f288a76b96`. 전환 브랜치: `codex/wpf-migration`.
검증일: 2026-10-20, Windows x64. 이 문서는 소스 검사, 자동 실행 검사, 화면 확인, 아직 실행하지 않은 항목을 구분합니다.

## 기준 분석

기존 MainForm은 LibVLC/MediaPlayer, 파일 검색, 메타데이터, 재생 명령, PiP를 모두 소유했습니다. MediaLyricsSync는 WinForms Timer와 구체적인 LyricsRenderer 목록을 직접 다뤘고, LyricsRenderer는 GDI 텍스트 측정과 스크롤 타이머 두 개를 소유했습니다. 전체화면은 VideoView의 부모를 바꾸었습니다. PiP는 별도 엔진 없이 같은 동기화 객체에 렌더러를 등록했습니다.

LrcParser/SrtParser와 LyricLine의 파싱 의미는 유지했습니다. 기존 README의 MP4 전용/상위 Videos 설명은 실제 코드의 MP4·MP3·WMV·MKV/Windows Videos 특수 폴더 하위 LyricsPlayer와 달랐으며 수정했습니다.

기준 분석용 Graphify 결과는 로컬 `graphify-out/`에 있으며 Git과 앱 빌드에서 제외합니다. 323노드/508관계/16커뮤니티이며, AST 미해결 endpoint 23개와 동일 노드 간 관계 병합 60개가 있어 정확한 동작 판단에는 실제 원문을 사용했습니다.

## 최종 구조

```text
Program: STA + 기존 이름의 단일 인스턴스 Mutex
  App / MainWindow
    MainViewModel (메인과 PiP가 공유하는 한 세션)
      MediaPlaybackService → 한 LibVLC, 한 MediaPlayer
      LyricsSynchronizationService → 기존 Parsers → LyricTimeline
      PlaylistService → FileSystemWatcher + 디바운스 + 비동기 Parse
    VideoView (공식 WPF HwndHost)
    LyricsView (가상화/재활용 ListBox + 대체 가능한 스크롤 애니메이션)
    PlaybackControlBar / LyricsPipWindow
```

Core와 Parsers는 Window/Control/Timer를 참조하지 않습니다. MVVM 명령과 상태는 MainViewModel 한 개로 유지했습니다. 영상 출력 연결, 창 모드, 키보드 입력, 마우스 이동·크기 변경은 View 계층에 있습니다. 기능별 ViewModel을 억지로 분리하지 않았습니다.

재생/정지는 SemaphoreSlim으로 직렬화하며 native Stop을 UI 스레드에서 기다리지 않습니다. 가사 로드는 CancellationToken과 세대 번호로 이전 곡 결과를 차단합니다. 폴더 목록은 백그라운드에서 스캔하고 기존 항목을 재사용합니다. LibVLC Parse는 비동기 API 자체를 사용합니다. 완료 후 Media 해제를 Dispatcher에서 실행하여 VLC 콜백 스택 안의 네이티브 해제를 피합니다. 종료는 감시·타이머·이벤트를 중단하고 진행 중인 작업을 기다린 뒤 플레이어와 엔진을 해제합니다.

재생 타임라인은 재생 중 20ms·일시정지 중 50ms DispatcherTimer와 단조 시계 보간으로 계산합니다. 대기/정지 상태에서는 타이머를 중단합니다. 음악 반응은 사전 분석한 곡선을 재생 시각에 맞춰 읽으며, 해당 가사 View가 재생 중일 때만 CompositionTarget.Rendering을 구독합니다. 일시정지·정지·Unloaded 시 구독을 해제합니다. 상세 기록은 [음악 반응 및 시계 개선](AUDIO-REACTION.md)에 있습니다.

## 기능 보존 체크리스트

아래는 분석 단계의 동일한 기능 목록에 전환 후 결과를 붙인 것입니다. 자동 검사는 `Tests/Program.cs`, 미디어 생성은 `tools/New-TestMedia.ps1`에 있습니다.

| 기존 기능 | 전환 결과와 검증 |
|---|---|
| 단일 인스턴스 | 실제 배포 EXE를 두 번 실행: 두 번째 종료 코드 0, 기존 PID 유지, 인스턴스 1개 |
| Videos/LyricsPlayer 자동 생성·검색 | Windows 특수 폴더 API 유지, 실제 기존 라이브러리 화면 확인; 자동 검사는 격리된 테스트 폴더 사용 |
| MP4/MP3/WMV/MKV | 생성한 실제 4종 파일 재생; MP4/MKV/WMV의 displayed video frames 검사 |
| 재생·일시정지·정지·탐색 | 엔진 상태, 시간, 정지 초기화, 빠른 반복 탐색 검사 |
| 볼륨·0.5/0.75/1/1.25/1.5/2 배속 | 기존 범위 유지; 실제 엔진의 볼륨·Rate 확인 |
| 이전·다음·자동 다음 | 경계·현재 경로 기준 단위 검사, 검색 중 next/자동 next 통합 검사, 마지막 곡 비순환 검사 |
| 제목 검색·시간·현재 곡 | 대소문자 비구분 검색, 비동기 재생시간, 객체 동일성 및 재생 강조 유지 |
| 폴더 감시·갱신 | 테스트 파일 생성/이름 변경/대문자 확장자/삭제 실시간 감지; 350ms 디바운스 |
| 폴더/파일 위치 열기 | 명령 및 우클릭 연결 소스 확인; Explorer 열기 수동 왕복은 별도 미실행 |
| 파일 열기·드래그 앤 드롭 | 네이티브 WPF 파일 대화상자·FileDrop 입력 연결; 지원 형식/누락 파일 경계 검사. 실제 마우스 드롭은 미검증 |
| LRC/SRT 자동 탐색 | 동일 파일명 선택, LRC 우선, 미존재 시 미디어 단독 재생 유지 |
| LRC 다중·시간·동일 시각 병합 | 기존 파서 재사용 및 회귀 검사 |
| SRT 종료 시각·여러 줄·무번호 | 기존 파서 재사용, 순수 검사 및 실제 재생 검사 |
| 기본 +200ms·이진 탐색 | 원래 값과 경계 검사. 10만 줄에서 10만 번 검색 약 10ms |
| 3초 초과 공백 | 첫 가사 앞 1초/이전 끝+1초의 ... 삽입, 정확히 3초인 경우 미삽입 검사 |
| 가사 클릭 탐색 | 보정 없는 StartTime으로 즉시 이동 및 active index 검사 |
| 수동 스크롤·3초 복귀 | 휠이 자동 이동을 중단하고 약 3초 후 복귀하는 Dispatcher 실행 검사; 스크롤바 Thumb 연결 소스 확인 |
| 여러 줄·긴 가사 | XAML Wrap, 고정 기본 글자 크기+RenderTransform으로 레이아웃 흔들림 방지 |
| 대용량 가사 | 1만 줄 로드, 실제 생성된 행 7~9개. 영상 재생과 빠른 탐색 중 렌더 콜백 계속 발생 |
| 빈 화면·로딩·가사 없음 | XAML 상태와 실제 무가사 재생 검사. 지원하지 않음/누락 파일은 기존 세션 유지+인라인 오류 |
| PiP 항상 위·이동·크기 | 네이티브 WPF WindowChrome, Topmost; 반복 5회 개폐와 공유 active index 검사. 실제 드래그/크기 조절은 수동 QA 잔여 |
| 전체화면·ESC·Space·영상 더블클릭 | 같은 창/VideoView 유지; ESC 검사, Space/더블클릭 이벤트 소스 확인 |
| 전체화면 상태 유지 | 미디어 없는 프로토타입 3회, 재생 세션 일시정지 상태 5회 왕복: HWND·시각·Paused 유지 |
| 전체화면 컨트롤 자동 숨김 | 3초 idle 후 숨김, 입력 후 복귀 검사. 위치가 같은 합성 MouseMove는 활동으로 세지 않음 |
| 종료·해제 | 종료 후 MediaPlayer.NativeReference=0 검사; 취소/타이머/이벤트 해제 소스 확인 |

## 시각 구성과 모션

영상/가사/라이브러리의 3열, 1000×620 최소 크기, 하단 고정 transport. 짙은 녹색 계열 표면과 낮은 채도의 세이지 강조색, 운영체제 기본 Segoe UI 폰트, 시간 표시용 Consolas, 자체 Path 아이콘과 기존 앱 아이콘을 사용합니다. 외부 이미지·폰트는 추가하지 않았습니다. 가사와 검색 결과 목록은 가상화되어 있습니다.

- 가사 자동 이동: 360ms Cubic EaseInOut; 탐색 직후 150ms 빠른 재동기화. 새 입력은 현재 스크롤 위치에서 애니메이션을 교체합니다.
- 가사 강조: 360ms 불투명도 0.34→1과 1→1.08 배율. 음악 확대는 별도의 Transform으로 최대 12.5% 추가하며 FontSize나 줄바꿈을 매 프레임 바꾸지 않습니다.
- 목록: 140/180ms hover와 250ms 재생 강조. 선택 테두리와 실제 재생 강조를 구분합니다.
- 버튼: hover 배경 전환, pressed 0.95 배율, 키보드 포커스 테두리와 disabled 상태.
- 전체화면 컨트롤: 220ms fade와 8px 이동. 레이아웃 높이는 유지하여 영상 출력 영역을 흔들지 않습니다.
- PiP: 200ms 등장 fade/240ms 8px 이동, 140ms 종료 fade. OS에서 클라이언트 애니메이션을 끄면 창·스크롤 모션을 생략합니다.

LibVLCSharp.WPF 3.10.1은 이전 3.9.1 이후 영상 호스트의 흰 배경과 높이 0 처리 수정이 포함된 호환 WPF 패키지로 선택했습니다. LibVLC 3.0.21 재생 엔진 계열은 그대로 유지했습니다. WPF 영상은 HWND 출력이므로 영상 위에 임의의 형제 WPF 요소를 겹치지 않습니다. VideoView.Content의 공식 오버레이는 빈 화면·입력·전체화면 힌트에만 사용하고 가사와 컨트롤은 별도 영역에 배치합니다. 모든 창은 한 MediaPlayer만 공유하며 PiP에 영상 출력은 연결하지 않습니다.

## 빌드와 배포

`dotnet restore`, `dotnet build -c Release`, WPF BAML/XAML 컴파일, `dotnet publish -p:PublishProfile=Portable-win-x64` 실행. Self-contained .NET 8.0.31 및 x64 LibVLC가 포함되는 폴더 배포입니다. 완전 단일 EXE는 선택하지 않았습니다. 최초 게시 출력은 888개 파일/약 261MiB이며 최종 크기는 게시 폴더를 기준으로 확인해야 합니다.

실행 결과:

| 검증 | 결과 |
|---|---|
| `dotnet restore` 및 Tests 복원 | 성공 |
| `dotnet build -c Release` / Tests Release 빌드 | 성공, 경고 0 / 오류 0 |
| WPF XAML/BAML 컴파일 | 성공 |
| 순수 회귀 검사 | 20개 성공 |
| 실제 WPF·LibVLC 통합 검사 | 순수 검사를 포함한 총 58개 성공, 종료 코드 0 |
| self-contained win-x64 게시 | 성공, .NET 8.0.31 포함, libvlc/win-x64 포함 |
| 게시 폴더의 EXE 직접 실행 | 생성한 MP4+LRC 실제 표시, 흰 여백 제거와 어두운 스크롤바/1x 표시 확인 |
| 실제 키보드 입력 | 배포 EXE에서 Space 일시정지, F11 전체화면, ESC 복귀 확인 |
| 단일 인스턴스 | 두 번째 EXE 종료 0, 기존 PID 유지, 남은 인스턴스 1개 |
| 대용량 렌더 표본 | 최종 통합 검사에서 1만 줄 중 9개 행 생성; 1.2초 탐색 표본 289회, 중앙값 3.9ms/p95 9.0ms. GPU FPS 보증은 아님 |

성공한 실행 로그는 로컬 `artifacts/runtime-checks-passed.log`에 보관합니다. 테스트 창 위의 실제 마우스 이동은 idle timer를 정상적으로 재시작하므로, 입력이 계속되는 실행에서는 idle 검사가 시간 초과될 수 있습니다. 이 경우는 진단 로그에서 실제 포인터 이동을 확인했으며 입력이 멈춘 실행에서 전체 검사가 완료되었습니다. 네이티브 디코더 경고는 아래 한계를 참고하세요.

최종 ZIP: `artifacts/LyricsPlayer-WPF-win-x64.zip` (119,573,088 bytes, 약 114MiB). 필수 실행파일, WPF/.NET 런타임, LibVLC DLL, 100개 이상의 VLC 플러그인, 앱 LICENSE를 아카이브에서 직접 확인했습니다. 총 889개 파일이며 x86 LibVLC는 포함하지 않습니다. 빌드 DLL과 게시 DLL의 SHA-256도 일치했습니다.

## 남은 검증과 한계

- 테스트는 이 PC의 Windows/NVIDIA GPU 및 생성된 소형 파일을 기준으로 합니다. 모든 코덱, DRM, 4K/HDR, 장시간 GPU 부하를 보장하지 않습니다.
- 빠른 미디어 교체·탐색·종료 중 LibVLC 3.0.21이 D3D11 thumbnail/일시적인 디코더 버퍼 경고를 출력했습니다. 실행 검사는 완료되지만 장시간 하드웨어 디코딩 회복 검증은 남아 있습니다. 설정 항목을 임의로 추가하거나 GPU 디코딩을 무조건 끄지는 않았습니다.
- 렌더 콜백 간격 표본은 전체 프레임 지연이나 GPU 프레젠테이션 FPS 측정이 아닙니다. 60FPS 전 구간 보증으로 해석하면 안 됩니다.
- 다중 모니터 간 이동, 혼합 DPI(125/150/200%), 절전/복귀, 오디오 장치 교체, 깨끗한 Windows 계정에서의 무설치 실행, 장시간 메모리 측정은 미실행입니다. PerMonitorV2 manifest와 WPF의 현재 모니터 최대화 경로는 구현되어 있습니다.
- 원래 첫 타임스탬프 이전에도 첫 줄을 선택하는 동작 및 단일 LRC 줄의 초기 공백 규칙은 호환성을 위해 유지했습니다. 시간 보정은 실행 중 세션 설정이며 디스크에 저장하지 않습니다.
- 코드 서명, 설치 프로그램, 자동 업데이트는 작업 범위에 넣지 않았습니다.
