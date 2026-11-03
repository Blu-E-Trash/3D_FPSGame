# 23·24장 실습 보고서: 조명·포스트 프로세싱·최적화

> 프로젝트: 3D_FPSGame (Unity 6000.3.22f1, **Built-in 렌더 파이프라인**) / 씬: SampleScene(공장 맵)
> 작성: Claude(AI). 측정·적용은 Claude가 에디터 도구를 만들어 직접 실행했고, 수치는 도구가 저장한 파일(`Docs/Perf/*.md`)에서 옮겼다.

## 0. 요약

| 단계 | 한 일 | 시점 A FPS | 시점 B FPS | 시점 B Shadow Casters |
|---|---|---|---|---|
| Before | 아무것도 하지 않은 상태 | 185 | 140 | 612 |
| 1. 조명 | 해 Mixed, 맵 Baked, Shadowmask 굽기, 프로브, 그림자 거리 30 | 230 | 191 | 150 |
| 2. 포스트 프로세싱 | PPv2: Color Grading, Bloom, Vignette, AO, SMAA | 212 | 166 | 139 |
| 3. 최적화 후(최종) | 매 프레임 로그 제거, 좀비 스키닝 2본·일부 그림자 끔, 드럼통 텍스처 1024 | 255 | 167 | 143 |

- 가장 큰 개선은 **조명 단계**에서 나왔다. 씬에 실시간 그림자를 그리는 Directional Light가 두 개 있었고, 하나를 Baked로 바꾸고 그림자 거리를 줄이자 **그림자를 그리는 물체가 612 → 150개(시점 B)** 로 줄었다.
- 포스트 프로세싱은 화면 품질을 위해 **CPU 약 0.6ms, FPS 약 40**을 썼다(같은 프레임에서 켜고 끈 비교).
- 최적화 단계에서는 **씬 텍스처 메모리 83.7 → 63.7MB(−24%)**, **카메라 스크립트의 매 프레임 GC 할당 1.7KB → 0B**를 확인했다. 좀비 관련 GPU 최적화는 좀비 위치가 매번 달라 장면 단위 수치로는 효과를 가려내지 못했다(아래 분석).

## 1. 측정 방법

- **같은 조건**: Game 창 최대화, 1920x1080, 시작 지점 (10, 1, 0)에서 방향만 고정한 두 시점에서 쟀다.
  - 시점 A: Yaw 180 (넓은 골목, 먼 건물이 많다)
  - 시점 B: Yaw 90 (창고 정면, 좀비 스폰 방향)
- **도구**: `Assets/02.Scripts/Editor/PerfCapture.cs`를 만들어 F5~F8로 시점을 고정(마우스 회전 스크립트를 끄고 위치·방향을 맞춤)하고, F9/F12로 **120프레임 평균**을 `Docs/Perf/*.md`와 화면 PNG로 저장했다. 값은 `ProfilerRecorder`의 Render 카운터(Batches, SetPass, Draw Calls, Triangles, Shadow Casters)와 Main Thread 시간, Memory 카운터다.
- **한계**: 에디터 Play 모드 측정이라 에디터 비용이 섞인다. 좀비 AI가 매번 다르게 움직여 시야에 보이는 좀비 수가 달라지고, 같은 상태를 두 번 재도 FPS가 ±10% 정도 흔들렸다(2단계 PP와 3단계 opt_before는 같은 상태인데 시점 A FPS가 212와 269로 달랐다). 그래서 작은 차이는 효과로 판단하지 않았다. GPU Frame 값은 에디터에서 믿기 어려워 참고만 했다.

📷 `01_before_viewA_stats.jpg` / `02_before_viewB_stats.jpg` / `03_before_profiler.jpg` (Before)

## 2. 조명 (23장)

### 2.1 Before 상태 진단 (`Tools/Lighting/1. Analyze`)

| Light | Mode | 그림자 | 세기 |
|---|---|---|---|
| Directional Light (씬 루트) | Realtime | Soft | 2 |
| Map/Map_v1/Light/Directional Light | Mixed | Soft | 1.5 |

- 해가 두 개라 **실시간 그림자를 두 번** 그리고 있었다.
- Lighting Settings가 없어 **한 번도 구운 적이 없었다**. 맵 렌더러 402개는 모두 라이트맵 UV(UV2)와 Contribute GI가 있어 바로 구울 수 있었다.
- 그림자 거리 40m, Cascade 2.

### 2.2 적용한 것 (`Tools/Lighting/2. Apply Setup`, `3. Bake`)

| 항목 | 값 | 까닭 |
|---|---|---|
| 해(루트) | **Mixed**, Soft Shadows | 직접광·그림자는 실시간, 간접광은 굽는다 |
| 맵 안의 보조 해 | **Baked** | 실시간 계산과 두 번째 그림자 패스를 없앤다. 빛은 라이트맵에 남는다 |
| Lighting Settings | FactoryLighting: Baked GI, **Shadowmask**, Progressive GPU, 해상도 20, 최대 2048, Non-Directional | 30m 밖 그림자는 구운 그림자로 대신한다 |
| 맵 렌더러 402개 | Receive GI = **Lightmaps** | 정적 물체는 라이트맵으로 빛을 받는다 |
| Light Probe Group | **926개** (NavMesh 위 6m 격자, 높이 0.5m·2.5m) | 움직이는 좀비·플레이어가 구운 빛을 받는다 |
| Reflection Probe | Baked, 128, 맵 전체 | 실시간 반사(카메라 6대 비용)는 쓰지 않는다 |
| Quality(모든 단계) | Shadow Distance **40 → 30**, Distance Shadowmask | 가까운 그림자만 실시간으로 그린다 |
| 굽기 | 약 2분 30초 | |

Built-in 파이프라인이라 교안의 APV(Adaptive Probe Volume)는 쓸 수 없어서 Light Probe Group을 NavMesh 기준으로 자동 배치했다.

### 2.3 결과

| 항목 | 시점 A Before → 조명 | 시점 B Before → 조명 |
|---|---|---|
| CPU Main (ms) | 5.42 → **4.34** | 7.12 → **5.23** |
| FPS | 185 → **230** | 140 → **191** |
| Shadow Casters | 353 → **126** (−64%) | 612 → **150** (−75%) |
| Batches | 234 → 294 (**증가**) | 591 → **274** |
| SetPass | 185 → 197 | 247 → **187** |
| Triangles | 92.8k → 81.9k | 246.5k → **90.5k** |

📷 `04_light_viewA_stats.jpg` / `05_light_viewB_stats.jpg` / `06_compare_before_light.jpg` (왼쪽 Before, 오른쪽 조명 후)

**분석**
- 그림자 패스가 하나 줄고 그림자 거리도 줄어 Shadow Casters와 Triangles(그림자 패스의 삼각형 포함)가 크게 줄었고, 이것이 FPS 상승의 주원인이다.
- 시점 A에서 Batches가 늘었다. 라이트맵이 여러 장으로 나뉘면 같은 머티리얼이라도 라이트맵 인덱스가 달라 정적 배칭으로 합쳐지지 못하기 때문이다. 그래도 그림자 비용 감소가 더 커서 FPS는 올랐다.
- 화면이 조금 어두워졌다. 실시간 환경광(하늘)은 모든 면을 고르게 밝혔지만, 구운 간접광은 가려진 곳이 실제로 어둡게 계산되기 때문이다. 2단계에서 노출로 보정했다.

## 3. 포스트 프로세싱 (24장)

### 3.1 방식

Built-in 파이프라인이라 URP Volume 대신 **Post Processing v2 패키지(com.unity.postprocessing 3.5.1)** 를 설치했다. `Tools/Post Processing/Apply Setup`이 다음을 만든다.
- `PostProcessing` 레이어, 씬의 빈 `Global Volume` 오브젝트에 **PostProcessVolume(Global)** + `FactoryPostProfile`
- Main Camera에 **PostProcessLayer**(SMAA Medium). MinimapCamera에는 붙이지 않아 미니맵에는 효과가 들지 않는다.
- F11로 Play 중 효과를 껐다 켜며 같은 프레임에서 비교했다.

콘셉트 한 줄: **"빛바랜 폐공장"** — 살짝 바랜 색, 은은한 대비, 가장자리를 조금 어둡게.

### 3.2 설정값과 튜닝 과정

| 효과 | 1차(직접 적용) | 2차(최종, 튜닝 후) | 바꾼 까닭 |
|---|---|---|---|
| Color Grading – Tonemapper | ACES | **Neutral** | ACES는 중간 밝기까지 눌러 화면이 눈에 띄게 어두워졌다 |
| Post Exposure | 0.35 | **0.45** | 굽기 뒤 어두워진 화면을 되돌린다 |
| Contrast / Saturation | 12 / −12 | **8 / −10** | 그늘이 뭉개지지 않게 |
| Temperature | 4 | 4 | 아주 살짝 누렇게 |
| Bloom | Intensity 1.2, Threshold 1.1, Fast Mode | 같음 | 하늘·밝은 금속만 번지게, 가벼운 모드 |
| Vignette | 0.28 | **0.22** | 조준에 방해되지 않게 |
| Ambient Occlusion(MSVO) | 0.6 | **0.35** | 라이트맵에 이미 그늘이 있어 AO를 겹치면 너무 어둡다 |
| Anti-aliasing | SMAA | SMAA | FXAA보다 또렷하고 TAA처럼 잔상이 없다 |
| Motion Blur / Depth of Field | 사용 안 함 | 사용 안 함 | FPS에서 겨누기 어렵고 어지럽다 |

📷 `07_pp_v1_on_too_dark.jpg` / `08_pp_v1_off.jpg` (1차: 효과 켬/끔) → `09_pp_v2_on.jpg` / `10_pp_v2_off.jpg` (2차: 효과 켬/끔)

### 3.3 비용 (시점 A, 같은 프레임에서 F11로 켬/끔)

| 항목 | PP 끔 | PP 켬 |
|---|---|---|
| FPS (Stats) | 278 | 237 |
| CPU main (ms) | 3.6 | 4.2 |
| SetPass | 195 | 208 |

화면 전체에 효과를 덧입히는 만큼 SetPass가 13 늘고 프레임당 약 0.6ms를 더 썼다. 효과 중 AO가 가장 무거운 편이라 세기를 낮췄고 Bloom은 Fast Mode를 썼다.

📷 `11_pp_viewA_stats.jpg` / `12_pp_viewB_stats.jpg` / `13_pp_profiler_memory.jpg`

## 4. 최적화

### 4.1 AI가 제안한 방법 (CPU / GPU / RAM)

먼저 측정 도구로 후보를 조사했다(`Tools/Perf/Report Skinned Meshes`, `Report Memory`, 스크립트 전수 검색).

| 분야 | 제안 | 조사 결과 | 선정 |
|---|---|---|---|
| CPU | 매 프레임 Debug.Log 제거 | `CameraRotate.LateUpdate`가 매 프레임 `Mouse Y` 로그를 남겨 **프레임당 1.7KB GC, 0.23ms(프레임의 4.4%)** | ✅ 적용 |
| CPU | 로그 문자열을 조건부 컴파일로 | `Zombie.ChangeState`가 `Debug.Log`를 직접 호출(상태가 바뀔 때마다) | ✅ 적용(GameLogger.Log로) |
| CPU | Update 호출·Find·GetComponent 정리 | Update는 대부분 가볍고 Find/GetComponent는 Awake에서만 쓴다 | ❌ 효과 작음 |
| CPU | 오브젝트 풀링 | 좀비·폭탄은 이미 ObjectPool을 쓴다 | ❌ 이미 적용 |
| GPU | 실시간 빛·그림자 줄이기 | 조명 단계에서 해결(Shadow Casters −75%) | ✅ 2장에서 적용 |
| GPU | 좀비 Skinned Mesh 비용 | 좀비 1마리 = **조각 14개**(절단 연출용), 7마리면 98개를 매 프레임 스키닝하고 각각 그림자를 그린다 | ✅ 적용(스키닝 2본, 손·아래팔 그림자 끔) |
| GPU | 포스트 프로세싱 비용 | 3.3 측정대로 0.6ms. Fast Bloom, AO 세기 낮춤 | ✅ 3장에서 반영 |
| GPU | 오클루전 컬링 | 22장에서 이미 굽기 완료 | ❌ 이미 적용 |
| GPU | GPU Instancing | 맵 대부분이 정적 배칭되어 효과가 작다 | ❌ |
| RAM | 작게 보이는 소품의 큰 텍스처 줄이기 | 씬 텍스처 1~2위가 **드럼통(3개뿐) 2048 텍스처**(10.7MB씩) | ✅ 적용(1024) |
| RAM | 오디오 Load Type 정리 | 불러온 오디오 클립 0개 | ❌ 해당 없음 |
| RAM | 모델 Read/Write, 메시 압축 | 메시 메모리 31MB로 작다 | ❌ 효과 작음 |

### 4.2 적용 내용

| 분야 | 파일 | 변경 |
|---|---|---|
| CPU | `02.Scripts/Camera/CameraRotate.cs` | 매 프레임 `GameLogger.Log($"Mouse Y: {mouseY}")` 삭제 |
| CPU | `02.Scripts/Monster/Zombie.cs` | `Debug.Log` → `GameLogger.Log` (빌드에서 `ENABLE_LOG`를 빼면 문자열 생성까지 사라진다) |
| GPU | `Zombie/Prefabs/Zombie1.prefab` (`Tools/Optimization/Apply`) | Skinned Mesh 14개 Skin Weights **4 → 2 Bones**, 손·아래팔 4조각 **Cast Shadows Off** |
| RAM | `Sector Warzone/Post-Apocalyptic Barrels/Textures` 9개 | Max Size **2048 → 1024** |

### 4.3 결과

**CPU — CameraRotate (Profiler Hierarchy, 같은 방법으로 15초 Play 녹화)**

| 항목 | Before | After |
|---|---|---|
| CameraRotate.LateUpdate 시간 | 0.23ms (4.4%) | **0.00ms (0.1%)** |
| GC Alloc / 프레임 | 1.7KB | **0B** |

📷 `14_opt_cpu_before_profiler_CameraRotate.jpg` / `15_opt_cpu_after_profiler_CameraRotate.jpg`

**RAM — 씬 렌더러가 쓰는 텍스처 (`Report Memory`)**

| 항목 | Before | After |
|---|---|---|
| 씬 텍스처 합계 | 83.7MB | **63.7MB (−24%)** |
| 2048 텍스처 | 8개 / 48.3MB | **5개 / 21.5MB** |
| Profiler Texture Memory(에디터 전체) | 628.4MB | 608.4MB |

드럼통은 화면에서 1~2m 크기라 1024로 줄여도 차이가 보이지 않았다.

**GPU — 좀비 Skinned Mesh**
- 좀비 1마리당 그림자를 그리는 조각 14 → **10개**, 정점마다 섞는 뼈 4 → **2개**.
- 장면 단위 측정(opt_before → opt_after)에서는 시점 B Triangles가 85k → 120k로 오히려 늘었다. 최적화 뒤 측정 때 **시야에 들어온 좀비가 더 많았기 때문**이고(Visible Skinned Meshes 72 → 84), 좀비 AI가 매번 다르게 움직여 이 항목의 효과는 장면 수치로 가려내지 못했다. 효과를 제대로 재려면 좀비 위치를 고정한 테스트 씬(예: 좀비 7마리를 같은 자리에 세워 둔 씬)에서 비교해야 한다.

### 4.4 최종 전후 비교

| 항목 | 시점 A Before → 최종 | 시점 B Before → 최종 |
|---|---|---|
| CPU Main (ms) | 5.42 → **3.92** | 7.12 → **6.00** |
| FPS | 185 → **255** (+38%) | 140 → **167** (+19%) |
| Batches | 234 → 272 | 591 → **379** |
| SetPass | 185 → 209 | 247 → **202** |
| Draw Calls | 523 → **429** | 881 → **502** |
| Triangles | 92.8k → **70.2k** | 246.5k → **120.0k** |
| Shadow Casters | 353 → **120** | 612 → **143** |

📷 `16_final_viewA_stats.jpg` / `17_final_viewB_stats.jpg` / `18_final_profiler.jpg`
📷 `19_compare_viewA_before_after.jpg` / `20_compare_viewB_before_after.jpg`

## 5. 정리와 다음에 할 것

- **가장 효과가 컸던 것**: 중복된 실시간 해 하나를 Baked로 바꾸고 그림자 거리를 줄인 것. 빛을 굽는 일은 간접광을 싸게 얻는 일이고, 프레임을 줄이는 일은 실시간 빛과 그림자를 줄이는 일이라는 교안의 설명과 같은 결과였다.
- **나빠진 것**: 라이트맵 때문에 시점 A의 Batches가 늘었다. Lightmap 최대 크기를 4096으로 키워 라이트맵 장 수를 줄이면 배칭이 다시 합쳐질 수 있다(메모리와 맞바꿈).
- **포스트 프로세싱**: 품질을 위해 약 0.6ms를 쓰는 선택을 했다. 저사양에서는 AO부터 끄는 옵션을 두면 된다.
- **남은 후보**: 빌드에서 `ENABLE_LOG` 정의 빼기, 좀비 고정 테스트 씬으로 스키닝 최적화 효과 재측정, 실제 빌드(Development Build + Profiler 연결)로 다시 측정.

## 부록: 만든 도구와 파일

| 파일 | 하는 일 |
|---|---|
| `Editor/PerfCapture.cs` | 시점 고정(F5~F8), 120프레임 측정(F9/F12, Shift+F9/F12) → `Docs/Perf/*.md`, `*.png` |
| `Editor/LightingSetupTool.cs` | 조명 진단·적용·굽기 |
| `Editor/PostProcessSetupTool.cs` | PPv2 Volume/Layer/Profile 적용, F11 켬/끔 |
| `Editor/OptimizationReport.cs` | Skinned Mesh·메모리 조사 |
| `Editor/OptimizationApplyTool.cs` | 좀비 프리팹·드럼통 텍스처 최적화 |
| `Docs/Perf/` | 모든 측정 결과(before/after/opt_before/opt_after, 분석 보고서) |
