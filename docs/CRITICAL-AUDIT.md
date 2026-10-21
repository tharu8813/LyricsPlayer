# LyricsPlayer critical engineering audit

검증일: 2026-10-21 (Asia/Seoul). 기준 및 원격 `main`:
`7d3e855f08a126aa21ebee39f8fa259a516a8d4f`.
작업 브랜치: `codex/critical-quality-overhaul`.

판정: **핵심 수정과 아래 회귀 검증을 수행했다. 전체 출시 품질 게이트는 아직 부분 충족이다.**
실제 1시간 연속 실행, 4K/HDR, 혼합 DPI/다중 모니터, 특수 코덱 및 native hang 복구는
완료했다고 주장하지 않는다. 이번 실행에서 프로세스 충돌이나 메모리 손상은 관찰되지
않았지만, 단기간 통과는 네이티브 안전성의 절대 증명이 아니다.

## A. Critical audit report

문서의 이전 성공 횟수는 근거로 재사용하지 않았다. 현재 Core, Parsers, Services,
ViewModels, Views, 진입점, manifest, 프로젝트/게시 설정, 검사 코드를 읽고 재실행했다.
기존 Graphify 그래프는 WinForms 파일을 가리키는 오래된 지도여서 현재 구현 증거에서 제외했다.
P0 충돌/손상은 재현하지 못했다. 아래의 코드 경로 입증과 런타임 재현을 구분한다.

| ID | 우선순위 | 파일/위치 | 문제 | 근거 | 상태 |
|---|---|---|---|---|---|
| A01 | P1 | MainViewModel.PlayFileAsync / 명령 조건 | 선택적 분석 완료까지 IsLoading이 유지되어 재생 조작을 막음 | 기존 await/명령 조건 추적; 지연 분석 주입으로 pause/stop/next/previous 검사 | 수정·검증 |
| A02 | P1 | MediaPlaybackService.PlayAsync | 작업 시작 후 취소된 요청이 취소 검사보다 Stop을 먼저 호출할 수 있음 | 원래 실행 순서로 입증; 특정 명령 사이 취소 경합은 결정적으로 재현하지 않음 | Stop 전후 취소 검사 보강 |
| A03 | P1 | MainViewModel.DisposeAsync / InitializeAsync | RunAsync 밖에서 직접 시작한 작업이 종료 대기 목록에 없고 초기화 도중 종료 수명이 불명확 | 직접 호출 후 종료/초기화 직후 종료 검사 | 직접 작업 추적 및 로컬 엔진 해제 검증 |
| A04 | P1 | MainViewModel.DisposeAsync | 초기화가 목록 refresh를 기다리는 동안 목록 취소보다 pending 대기를 먼저 하면 느린 전체 스캔 종료를 기다림 | await 의존 순서로 입증; 수천 파일의 종료 지연 시간은 미측정 | 목록 취소/해제를 먼저 수행 |
| A05 | P1 위험 | MusicReactionService PCM callbacks | 공유 realloc 버퍼의 크기/포인터 검증 부족, 중첩 콜백 및 해제 순서 방어 부족 | 관리 측 경계 검사와 실제 취소 스트레스; 기존 메모리 손상은 미재현 | 소유권·제한·직렬화 보강 |
| A06 | P1 | PlaybackClock / Seek | 200ms 뒤에는 탐색이 성공했다고 간주하여 목표 가사가 먼저 보이거나 이전 시각으로 복귀 가능 | 기존 목표 Reset 경로; 지연/실패/역방향 탐색 순수 검사 | 네이티브 응답 확인 후 재정렬 |
| A07 | P2 | StopAsync / LyricsView | 정지 뒤 이전 가사 목록이 남음 | 목록 보존 코드; stop 뒤 Lines 및 offset 변경 검사 | 정지 시 숨김 정책 구현 |
| A08 | P2 | MainWindow.ShowPip | Owner 연결 때문에 메인 최소화가 PiP에도 영향 | Owner 코드; 실제 메인 최소화 뒤 PiP 가시성 검사 | 독립 창 및 종료 시 명시 해제 |
| A09 | P2 | PlaylistService.WatcherError | 오류 후 Refresh만으로 감시 인스턴스가 복구된다고 가정 | 오류 이벤트 경로; overflow 주입 후 새 watcher의 실제 파일 생성 이벤트 검사 | 재생성 및 시작 실패 재시도 |
| A10 | P2 | PlayFileAsync | 동일 파일도 매번 전체 디코딩 | 호출 경로; 완료 결과 재생 시 분석 호출 횟수 검사 | 8MiB/16개 LRU 캐시 |
| A11 | P2 | MusicReactionTimeline | NaN/Infinity가 전체 곡선을 오염시키고 미세 노이즈를 곡별 정규화할 수 있음 | 비정상 에너지/미소 입력 순수 검사 | 유한값 정리·곡 전체 노이즈 하한 |
| A12 | P2 | LyricsView.Center / 렌더링 | 먼 행 실체화 직후 이전 offset에서 다시 애니메이션, 분석 활성화/재개 시 배율 급변 가능 | ScrollIntoView 뒤 이전 offset 사용 경로; 중앙 정렬/가상화/배율 검사 | 먼 점프에서 새 offset부터 짧게 정렬·활성화 fade |
| A13 | P2 | SrtParser.TryParseTimeSpan | 매우 큰 유효 정수 시간이 TimeSpan 범위를 넘으면 예외, 역방향 cue 허용 | 2147483647시간 입력 및 역방향 cue 회귀 검사 | 잘못된 시간 건너뜀 |
| A14 | P2 | MainWindow / 설정 / 배포 | 좁은 창의 3열 고정, 설정 미유지, 의존성 고지 누락 | XAML/패키지 및 실제 렌더 검사 | 좁은 창 목록 접기, 최소 설정, 고지/게시 검사 추가 |
| A15 | P1 | MainViewModel.PlaybackError | native 오류 이벤트를 UI에서 처리할 때 상태가 이미 Ended이면 실패를 완료로 오인 | 실제 4바이트 MP3 재현: native=Ended, 표시=재생 완료 | terminal Ended 오류도 수락, 세대 무효화 및 로드/분석 취소 |

### 재현·수정·검증 근거

**A01, A03, A07, A10.** Tests/AuditChecks의 분석 delegate를 미완료 Task로 유지한 채
실제 LibVLC 재생을 시작한다. 재생 준비 뒤 IsLoading=false, ReactionState=Analyzing인 상태에서
pause/stop/next/previous를 실행한다. 기존에는 PlayFileAsync가 분석을 await했다.
수정 후 재생 준비 및 가사 로드 Task와 분석 Task를 분리했다. 모든 분석 결과는 취소 여부,
세대, 현재 전체 경로를 검사한 뒤 적용한다. 취소를 무시하는 가짜 분석을 20회 교체한 뒤
이전 결과부터 완료해도 최신 분석 상태를 덮지 않는다. 일반 분석 예외도 반응만 Unavailable로
전환한다. 직접 호출 작업, 초기화 작업, 분석 작업을 모두 종료 대기 목록에 넣는다.

상태는 플레이어의 VLCState/기존 표시 상태와 독립적인 ReactionStatus
(Unavailable/Analyzing/Ready)를 사용한다. IsLoading은 미디어 준비에만 해당한다.
Playing/Paused/Buffering은 native 상태에서 읽는다. 선택적 분석 실패를 PlaybackFailed로
매핑하지 않는다. Paused는 가사/위치를 유지하며 음악 확대를 release한다. Stopped는
위치·활성 행·표시 가사·음악 반응을 초기화하고 같은 파일 재생을 허용한다. 마지막 곡
Ended에서는 마지막 가사를 유지하고 자동 반복하지 않으며 재생 버튼으로 처음부터 다시 연다.

**A05.** VLC 3.0.21의 [smem.c SendAudio](https://github.com/videolan/vlc/blob/3.0.21/modules/stream_out/smem.c#L352-L384)는
prerender 결과가 null이면 block을 해제하고 VLC_EGENERIC을 반환하며 memcpy하지 않는다.
따라서 'null 반환이면 반드시 native 쓰기 충돌'이라는 가설은 기각했다.
현재도 smem을 사용하고, 프레임당 최대 16MiB/동시 소유 최대 32MiB를 제한한다.
각 pre 요청은 별도 포인터를 소유하며 post에서 소유권과 정확한 크기를 확인한 후 복사한다.
builder/버퍼 장부 접근은 잠금으로 보호하고 post 완료 후 해당 버퍼를 해제한다.
오류/취소는 비동기 TCS 완료를 통해 분석 worker가 처리하며 native 경계로 예외를 던지지 않는다.
Stop 및 MediaPlayer release가 끝난 뒤 남은 버퍼를 해제하고 delegate를 KeepAlive한다.
동시 분석은 semaphore로 하나로 제한해 곡 교체 시 디코더 폭증을 피한다.

소유하지 않은 포인터, 0/초대형 할당, 크기 불일치, 중첩 버퍼를 직접 검사했다.
30분 WAV 디코딩을 실제 시작하고 50ms 뒤 취소하는 검사를 20회 수행했으며 10초 이내
취소 완료를 확인했다. 중간에 GC도 실행하고 이후 MP3 분석이 성공함을 확인했다.
이는 Stop이 모든 손상 코덱에서 유한 시간 안에 끝난다는 보장이 아니다.
공식 audio callback의 borrowed buffer 방식도 대안이지만 보통 재생 pacing과 연결되므로,
빠른 사전 분석 유지 및 변경 범위를 고려해 검증한 smem 규약을 보강하는 쪽을 선택했다.

**A06.** Seek는 곧바로 목표 위치를 UI clock에 주입하지 않는다. 현재 추정 위치를 잠시
유지하고, target에 가까운 변경된 native 보고를 확인해 다시 정렬한다. 1.5초 내 확인되지
않으면 관측 native 위치로 복귀한다. 그동안 최신 탐색 요청이 이전 요청을 대체한다.
정체 시 300ms 외삽 상한, 0.5/1/2배속, 정지·재개, native=0, 오래된 보고를 검사했다.
이 확인은 디코더의 reported media time이며 DAC/스피커의 실제 소리 도착을 측정하지 않는다.
사용자 +200ms 기본 보정은 그대로다.

**A08, A09, A12.** PiP는 Owner 없이 Topmost/작업 표시줄 항목을 유지하고 메인 종료에서
명시적으로 닫는다. 실제 최소화 및 반복 5회 열기/닫기를 검사했다. 감시 오류 시 기존
인스턴스를 폐기하고 다시 연결한다. 생성 실패 때 5초 뒤 재시도한다. 파일 스캔 중
개별 파일 접근 실패는 나머지 목록 스캔을 중단시키지 않으며 메타데이터 실패는 안내한다.
VLC metadata parse가 파일 이름 변경을 일시적으로 막을 수 있다. 테스트는 파일 작업을
최대 5초 재시도하고 실제 생성/이름 변경/삭제 결과를 확인하며, 잠금을 없앴다고 주장하지 않는다.

스크롤 단위 혼용 가설은 **기각**: Theme.xaml에 이미 Pixel이 있었다.
LyricsView에도 명시했지만 이것을 기존 결함 수정으로 세지 않는다. 실제 검사는 활성 행 중심과
viewport 중심 차이 0px, 1만 줄에서 5개 컨테이너, 100회 탐색이다. 먼 점프는 실체화된 위치에서
짧게 정렬하여 이전 먼 위치로 되돌아가는 애니메이션을 피한다. 휠/스크롤바는 기존 3초 복귀를 유지한다.
가상화는 유지했다. 여러 줄 텍스트의 수직 여백은 최대 합성 확대 1.215배를 위한 높이에 맞춘다.

음악 반응은 현재 행의 Transform을 재사용하며 음량 곡선/시계는 세션에서 공유한다.
분석 적용은 400ms fade, 구독 활성화는 60ms fade를 쓰고 연속 곡선에 또 다른 저역 필터를
넣어 타격을 늦추지 않는다. 메인/PiP는 작은 LevelAt 계산을 각각 수행하므로 완전히 한 번만
샘플링하는 구조는 아니다. 정지/일시정지/Unloaded 구독 해제 및 재활용 행 배율 초기화를 검사했다.
reduced-motion 분기는 사용자 Windows 설정을 바꾸지 않고 직접 실행해 정적 강조값을 확인했다.

## B. Implementation summary

기존 한 세션·한 재생 MediaPlayer와 공식 VideoView HWND를 유지했다. 새 서비스 계층이나
프레임워크를 도입하지 않았다. 분석용 decoder는 별도 player이며 영상 출력을 재연결하지 않는다.

| 영역 | 실제 수정/추가 파일 |
|---|---|
| 상태/취소/종료 | ViewModels/MainViewModel.cs, Services/MediaPlaybackService.cs |
| PCM 안전성/측정 | Services/MusicReactionService.cs |
| 캐시/수치/시간 | Core/MusicReactionCache.cs, Core/MusicReactionTimeline.cs, Core/PlaybackClock.cs |
| 목록/파싱 | Services/PlaylistService.cs, Parsers/SrtParser.cs |
| 렌더/창 | Views/LyricsView.xaml(.cs), Views/MainWindow.xaml(.cs), Views/LyricsPipWindow.xaml |
| 설정 | Core/PlayerSettings.cs |
| 검사 | Tests/Program.cs, Tests/AuditChecks.cs, Tests/BenchmarkChecks.cs, Tests/PublishedExeCheck.cs, Properties/AssemblyInfo.cs |
| 게시/고지/문서 | LyricsPlayer.csproj, tools/Verify-Portable.ps1, .github/workflows/windows.yml, THIRD-PARTY-NOTICES.md, licenses/, README/docs |

캐시는 완료된 곡선만 8MiB/16개 LRU로 보관하며 전체 경로(대소문자 비구분), 크기,
UTC 수정 시각, 알고리즘 버전으로 식별한다. 분석 시작/완료 사이에도 identity를 비교한다.
실패·취소 결과를 저장하지 않는다. 디스크 캐시는 추가하지 않았다. 같은 크기와 mtime을
고의로 보존한 내용 교체는 식별할 수 없으며, 이 사용 사례에는 content fingerprint가 필요하다.
수치 하한은 곡 전체 peak가 0.001 미만일 때만 무반응으로 처리하여 기존 audible 곡선의
참조 정확도(최대 오차 약 3.78e-8)를 보존했다. 자연스러움은 이 수치만으로 판정하지 않는다.

설정은 볼륨·가사 보정·반응 on/off만 저장한다. 반응 off는 분석도 수행하지 않는다.
경로는 `%LOCALAPPDATA%/LyricsPlayer/settings.json`이고 임시 파일을 쓴 뒤 같은 폴더에서
교체한다. 잘못된 JSON/읽기 실패는 기본값으로 복구하고 값 범위를 제한한다. 알 수 없는 필드는
무시한다. 변경한 설정만 종료 시 저장한다. 테스트용 libraryFolder를 준 창은 개인 설정에 쓰지 않는다.
재생속도·창 위치 저장은 추가하지 않아 화면 밖 저장 위치 복원 문제를 새로 만들지 않았다.

좁은 창에서는 목록을 접고 상단 버튼으로 표시할 수 있다. 좁은 창에서 목록을 열면 화면 패널을
접어 가사 폭을 유지한다. 최초 크기는 작업 영역으로 제한한다. 전면 재디자인은 하지 않았다.
WPF RenderTargetBitmap으로 실제 배치를 확인했으며 영상 HWND 표면은 이 이미지에 포함되지 않는다.
실제 혼합 DPI/모니터 간 이동이나 제품 모션의 주관적 감상 전체를 검증한 것은 아니다.

### 성능 측정

같은 PC의 실제 LibVLC, 합성 8kHz mono S16 WAV를 사용한 한 번의 표본이다.
긴 WAV가 일반 사용자 MP3/4K 영상의 성능을 대표하지 않는다. 기준 코드는 HEAD archive로
격리해 같은 WAV 생성/재생/분석 harness로 측정했다. 절대 private memory는 harness 초기화 차이가
있으므로 전후 개선 근거로 쓰지 않는다. 캐시 hit는 파일 identity 확인과 lookup 시간이며 전체 재생 준비 시간이 아니다.

| 길이 | 기준 전체 분석 ms | 수정 전체 분석 ms | PCM 처리 ms | 정규화 ms | cache hit ms | 수정 native 재생 시작 ms | 수정 UI timer p95 ms |
|---|---:|---:|---:|---:|---:|---:|---:|
| 10초 | 139.2 | 132.1 | 1.5 | 0.07 | 0.09 | 54.8 | 32.7 |
| 3분 | 162.9 | 154.2 | 23.1 | 1.35 | 0.08 | 50.9 | 32.7 |
| 30분 | 400.4 | 463.2 | 241.5 | 19.31 | 0.08 | 49.4 | 39.4 |
| 2시간 | 1370.5 | 1413.5 | 975.0 | 18.38 | 0.07 | 50.8 | 34.6 |

원시 분석의 유의미한 속도 향상을 주장하지 않는다. 30분 표본은 오히려 느렸다.
성과는 재생 조작과 분석의 분리 및 재생 시 디코딩 생략이다. 주입 분석으로 채운 캐시의 실제 세션 재생(Stop/Play/가사 로드 포함) 표본은 7.03ms였다. 전체 분석 wall time에서
PCM callback 처리/마지막 정규화를 따로 계측했다. native decode만의 순수 CPU 시간은
분리하지 못했으며 `nativeAndOverheadEstimateMs`는 차이로 계산한 추정이다.
2시간 결과 곡선은 1,727,992 bytes, 관측 private peak는 약 144.9MiB, UI timer 최대 간격은
38.5ms였다. PCM 처리 합계 975ms와 프로세스 CPU 약 3031ms는 서로 다른 측정값이다.
20회 native 취소 표본의 private memory는 285,343,744→239,783,936 bytes였으나 이는
managed/native/allocator를 섞은 값이며 누수 부재의 증명이 아니다.

전체 회귀 표본에서 1만 줄/100회 탐색 중 WPF rendering 간격 중앙값 3.6ms, p95 7.0ms를 측정했다.
이는 UI Rendering 이벤트 간격이고 실제 GPU present FPS/영상 프레임 지연이 아니다.
LibVLC displayed picture 통계는 소형 MP4/MKV/WMV에서 별도로 확인했다.

## C. Verification report

| 항목 | 결과 | 증거/범위 |
|---|---|---|
| Restore | 성공 | audit-restore.log; cached packages, `--ignore-failed-sources` |
| Release Build | 성공 | audit-build.log; 경고 0, 오류 0 |
| Unit | 성공 | 62개; parsers/timeline/clock/numeric/cache/PCM 경계/settings |
| Integration/UI/Stress | 성공 | audit-runtime-final.log; 순수 검사를 포함 187개, 종료 코드 0 |
| Native analysis | 성공 | 4종 형식, 실제 긴 decode 취소 20회, GC 및 이후 재분석 |
| UI | 일부 성공 | 중앙 정렬, 필수 명령, PiP 최소화/반복, HWND 보존, fullscreen idle/복귀, 수동 스크롤, reduced-motion 분기 |
| Long-file benchmark | 성공 | 10/180/1800/7200초 WAV 실제 디코딩; audit-benchmark/results.json |
| 1시간 연속 재생 | 미실행 | 압축 시간 파일 분석과 구분; native leak/장치 회복 장기 판정 불가 |
| Publish | 성공 | self-contained win-x64; audit-publish.log |
| Portable folder/ZIP | 성공 | 895파일, 320 native plugins, x86 없음, 필수 고지/런타임/DLL 및 assembly SHA 일치 |
| 실제 EXE | 성공 | audit-exe-smoke.log; 별도 프로세스 창/MP4 열기/단일 인스턴스/정상 종료/개인 설정 byte 보존 |
| GitHub Actions | 미실행 | workflow 추가 및 동일 명령 로컬 수행; 서버 실행 이력 없음 |
| 1080p/4K/HDR, 다중 DPI/모니터 | 미실행 | 이번 영상 fixture는 640×360; 전 구간 성능 보장 불가 |

로그/측정/이미지는 Git에서 제외한 `artifacts/`에 있다. 첫 실행에서 배율의 추가 저역 필터가
기존 시간 추종 검사를 실패시켜 제거하고 활성화 fade만 유지했다. 파일 감시 검사는 native
메타데이터 잠금 때문에 실패한 뒤 파일 작업을 유한 재시도로 바꿨으며 실패 검사를 삭제하지 않았다.
이전 실패의 잔여 테스트 파일이 다음 검사 개수를 오염시키는 것도 확인해, harness가 이름을 알고
생성한 테스트 파일만 시작 시 정리하도록 했다. 사용자 미디어는 정리하지 않는다.

native D3D11 thumbnail/access, timestamp/no reference clock, mjpeg cannot peek 경고는
숨기지 않고 로그에 남겼다. 검사 성공과 별개로 장기 GPU/코덱 회복 위험은 남는다.

재실행(저장소 루트, Windows, 기존 fixture는 tools/New-TestMedia.ps1로 생성):

```powershell
dotnet restore Tests/LyricsPlayer.Checks.csproj
dotnet build Tests/LyricsPlayer.Checks.csproj -c Release -m:1 -p:UseSharedCompilation=false
dotnet Tests/bin/Release/net8.0-windows/win-x64/LyricsPlayer.Checks.dll
dotnet Tests/bin/Release/net8.0-windows/win-x64/LyricsPlayer.Checks.dll --runtime
dotnet Tests/bin/Release/net8.0-windows/win-x64/LyricsPlayer.Checks.dll --benchmark
dotnet publish LyricsPlayer.csproj -c Release -r win-x64 --self-contained true -o artifacts/publish/audit-win-x64
./tools/Verify-Portable.ps1 -Folder artifacts/publish/audit-win-x64
dotnet Tests/bin/Release/net8.0-windows/win-x64/LyricsPlayer.Checks.dll --published-exe
```

## D. Remaining risks / release engineering

| 위험 | 가능성/영향 | 대응 및 후속 검증 |
|---|---|---|
| native Stop/release가 드라이버/손상 코덱에서 멈춤 | 이번 미관찰, 종료 지연 영향 큼 | 안전하지 않게 버퍼를 조기 해제하지 않음; 별도 프로세스 격리/장기 hang 주입 검토 |
| 1시간 이후 자원 증가·장치 교체/절전 회복 | 미평가, 장기 세션 영향 | CPU/private/handles/출력 통계 1시간 이상 soak |
| 혼합 DPI·4K/HDR·Alt+Tab·모니터 이동 | 실제 매트릭스 미실행 | 100/125/150/200%, 여러 해상도/모니터에서 화면·음성 체감 검사 |
| 같은 크기/mtime를 보존한 파일 교체 | 일반 편집보다 드묾, 오래된 반응 사용 | 재시작하면 세션 캐시 해제; 필요 시 fingerprint |
| metadata read lock / watcher 폴더 접근 거부 | 일시적 이름 변경 실패/목록 지연 | 유한 parse timeout, 오류 안내/감시 재생성·재시도; 실제 buffer flood/폴더 삭제 추가 부하 검사 |
| 수천 개 항목 refresh | 순차 parse 시간이 길 수 있음 | 중간 취소 가능; 수천 파일 측정/검색 지연은 미실행 |
| 사용자 동기화 체감 | 출력 장치 지연 미측정 | 기존 +200ms 사용자 보정; 실제 출력 지연 자동 보정 주장 금지 |
| 비영점 audio 시작 PTS/불연속 타임스탬프 | 가설, 특수 미디어에서 반응 시차 가능 | 현재 곡선은 디코딩 샘플 수 기준; 해당 fixture로 PTS 정합성 추가 검사 |
| 배포 라이선스 전체 충족 | native plugin별 추가 의무 가능 | 출처/원문 고지 추가; 공개 게시 전 corresponding source/codec 의무 확인 |

원격 확인 결과 `main`과 `codex/wpf-migration`은 기준 커밋이고 기본 `master`는
`44fb0d4d9774c7e38061052467ab54f288a76b96`이다. 최신 공개 릴리스 `2.1.0.0`도 master를
대상으로 하며 asset은 LyricsPlayer.zip이다. WPF를 찾기 어려운 상태는 실제로 존재한다.
기본 브랜치는 승인 후 main으로 바꾸고 master는 legacy 보존을 권한다. WPF 전환은 차기
3.0.0-rc.1 → 검증 후 3.0.0처럼 별도 태그/asset으로 내는 것이 적절하다. 기존 assembly
2.1.0.0은 이번 로컬 감사 빌드에서 유지했다. 기본 브랜치·릴리스·태그·원격 코드는 바꾸지 않았다.

[공식 LibVLCSharp NEWS](https://github.com/videolan/libvlcsharp/blob/3.x/NEWS)의 3.10.2는
LibVLC dispose 멱등성, StreamMediaInput 최적화 및 타 플랫폼 변경을 포함한다. 이 앱은
file URI와 자체 직렬화 disposal 경로를 사용한다. 이번에는 검증 변수를 늘리지 않도록
관리 라이브러리 3.10.1/네이티브 3.0.21을 유지했다. native 업그레이드 필요성은 별도 코덱·보안
감사 대상이며 관리 패키지 업데이트와 동일하게 취급하지 않았다.

배포 ZIP: `artifacts/LyricsPlayer-quality-audit-win-x64.zip`.
SHA-256: `7481B76011227421654191BC07086272ED088081AFD1FE7A2ACA5DE9A255478E`.
895 entries / 119,623,256 bytes. 공개 게시하지 않았다.

## E. Final quality gate

| 조건 | 판정 |
|---|---|
| 1. 일반 재생의 필수 컨트롤 | 지연 분석 중 포함 통과 |
| 2. 분석 실패와 재생 실패 분리 | 예외 주입 통과 |
| 3. callback/메모리 수명 검증 | 공식 규약·경계 검사·취소 스트레스 통과, 절대 안전성 보장 아님 |
| 4. 빠른 곡 변경 후 최신 데이터 | 취소 무시 결과 포함 20회 통과 |
| 5. stop/pause/resume | 자동 검사 통과 |
| 6. 스크롤/반응 간 충돌 | 측정 범위 통과, 모든 DPI/주관적 모션 품질 판정은 남음 |
| 7. PiP/fullscreen 반복 | 5회 및 HWND 보존·최소화·idle 통과, 모니터 이동 미실행 |
| 8. 장시간 재생/종료 안정성 | 긴 파일 분석·반복 취소/종료는 통과, 실제 1시간 soak 미실행: 부분 충족 |
| 9. 기존 기능 회귀 없음 | 자동 매트릭스 통과, 모든 개인 미디어/하드웨어 범위 보장은 아님 |
| 10. 실행 가능한 배포 | 폴더/ZIP/실제 EXE 통과, 깨끗한 계정/다른 PC는 미실행 |

따라서 이 산출물을 모든 환경의 안정성을 보증하는 최종 출시 승인으로 취급하면 안 된다.
