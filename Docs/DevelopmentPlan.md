# 가드 vs 마네킹 — 개발 계획서

> 원본 기획서: `가드 vs 마네킹.txt`
> 이 문서는 **개발 순서와 구현 기준**을 정의합니다. 작업은 반드시 Phase 순서대로 진행하고, 각 Phase의 **완료 기준**을 만족한 뒤 다음 Phase로 넘어갑니다.

---

## 0. 개발 원칙

### 0.1 환경
| 항목 | 값 |
|---|---|
| Unity | 6000.4.2f1 |
| 렌더 파이프라인 | URP (**Forward+ 렌더러**) |
| 네트워크 | FishNet (개발 중: **Tugboat** / 출시: **FishySteamworks**) |
| 캐릭터 컨트롤러 | ECM2 |
| 입력 | Input System |
| AI | AI Navigation (NavMesh) |
| Steam | Steamworks.NET (개발 중 AppID `480`) |

### 0.2 코딩 규칙
- 변수명·필드명은 **camelCase** (private 필드는 `_camelCase` 없이 `camelCase` 그대로 사용, 상수는 `PascalCase`)
- 모든 public/protected/private **메서드에 XML 주석(`/// <summary>`)** 작성
- **SOLID 원칙 준수**
  - **S**: 한 클래스는 한 가지 책임 (예: `GunTool`은 사격만, 소음은 `NoiseEmitter`가 담당)
  - **O**: 도구·상호작용 대상은 인터페이스(`ITool`, `IInteractable`)로 확장
  - **L**: 파생 클래스가 기반 계약을 깨지 않도록 (예: 모든 `ITool`은 쿨타임 규칙을 동일하게 따름)
  - **I**: 작은 인터페이스로 분리 (`IDamageable`, `INoiseSource`, `ITransformable`)
  - **D**: 게임플레이 로직은 구체 클래스가 아닌 인터페이스/ScriptableObject 설정에 의존
- **서버 권한 원칙**: 판정·상태 변경은 서버(`[Server]`, `[ServerRpc]`)에서만. 클라이언트는 입력 전송과 연출만 담당
- 하드코딩된 수치 금지 → 모든 호스트 설정값은 `GameSettings`에서 읽음

### 0.3 폴더 구조
```
Assets/_Project/
├─ Scripts/
│  ├─ Core/          # GameStateMachine, GameSettings, 이벤트 버스, 공통 인터페이스
│  ├─ Network/       # 접속, 트랜스포트 전환, Steam 로비
│  ├─ Player/        # 공통 플레이어, 역할, 체력, 입력
│  │  ├─ Guard/
│  │  └─ Mannequin/
│  ├─ Tools/         # 총, 탐지기, 손전등
│  ├─ Props/         # 변신 가능 사물, PropDatabase
│  ├─ Objectives/    # 전원 장치
│  ├─ AI/            # 가짜 마네킹
│  ├─ Map/           # 대기실 문, 조명 제어
│  ├─ Audio/         # 소음 시스템
│  ├─ Cameras/       # UnityEngine.Camera와 이름 충돌을 피하기 위해 복수형 사용
│  └─ UI/
├─ ScriptableObjects/
├─ Prefabs/
├─ Scenes/           # Lobby, Room, Game_<맵이름>
├─ Art/  Audio/  VFX/
```
- 네임스페이스: `GuardVsMannequin.<폴더명>`

### 0.4 게임 상태 흐름
```
Waiting ─▶ Preparation ─▶ Playing ─▶ Hunting ─▶ Ended
(접속 대기)  (가드 대기)     (탐색)      (사냥 시간)   (결과)
                              │                     ▲
                              └── 가드 승리 조건 ────┘
```

---

## 1. 확정된 게임 규칙 (기획서 보완 사항)

기획서의 모호한 부분에 대한 확정 사항입니다. 구현 시 이 표를 우선합니다.

| # | 항목 | 확정 규칙 |
|---|---|---|
| 1 | **공동 전원 장치 파괴 속도** | 참여 인원 `n`일 때 **총 속도 = 1.4^(n−1)** (1인 = 1.0, 2인 = 1.4, 3인 = 1.96, 4인 = 2.744). 초당 진행도 = `1.4^(n−1) / 파괴 소요 시간`. 개인 기여분 = 총 속도 / n. 한 명이라도 상호작용이 끊기면 **그 사람은 이탈**하고, **참여자가 0명이 되면 진행도 초기화** |
| 2 | **목표 수행 횟수 0회** | `Playing` 단계 진입(대기실 문 열림) **20초 후 자동으로 사냥 시간** 시작 (가드 보호 목적). HUD의 목표 칸에 **가드 보호 시간과 사냥 시간 시작까지 남은 시간**을 표시한다 (`HuntTiming`) |
| 3 | **가드 대기 시간** | 가드는 맵과 연결된 **별도의 대기실**에서 스폰. 대기 시간 동안 문은 닫혀 있고, 대기 시간이 끝나면 **문이 열려** 맵으로 나갈 수 있음 |
| 4 | **사냥 시간 중 가드 무기** | 사냥 시간에도 **총 사용 가능** (쿨타임 동일). 마네킹 체력 배율은 **호스트 설정값 `huntHealthMultiplier`** (기본값 10) |
| 5 | **변신 불가 사물** | 천장, 바닥, **조명 기구**, **전원 장치**, **전원 장치 주변 구조물** |
| 6 | **조명 방식** | **실시간 조명** + URP **Forward+** 렌더러. 전구는 Emission 머티리얼과 Light 컴포넌트를 함께 끄고 켬 |

---

## 2. 핵심 데이터 정의

### 2.1 `GameSettings` (호스트 설정, 네트워크 동기화)
| 필드 | 타입 | 기본값 | 범위/비고 |
|---|---|---|---|
| `mapId` | int | 0 | |
| `preparationTime` | float | 20 | 15~30초 (가드 대기 시간) |
| `playTime` | float | 180 | 120~240초 (제한 시간) |
| `huntTime` | float | 60 | 사냥 시간 |
| `guardCount` | int | 1 | 1 ~ 전체 인원 / 2 |
| `objectiveCount` | int | 3 | 0 ~ (마네킹 수 + 2) |
| `generatorCount` | int | 5 | 반드시 `objectiveCount`보다 많아야 함 |
| `generatorDestroyTime` | float | 10 | 1인 기준 파괴 소요 시간 |
| `gunCooldown` | float | 3 | |
| `detectorCooldown` | float | 10 | |
| `guardMaxHealth` | int | 3 | |
| `mannequinMaxHealth` | int | 1 | |
| `huntHealthMultiplier` | float | 10 | 사냥 시간 마네킹 체력 배율 |
| `mannequinAttackRange` | float | 2 | |
| `guardMoveSpeed` | float | 2 | 가드 이동 속도(m/s) |
| `mannequinMoveSpeed` | float | 1.5 | 마네킹 기본 이동 속도(m/s). 사냥 시간에는 × 1.5 (`GameConstants.MannequinHuntSpeedMultiplier`) |

- 기본값은 `Resources/Data/default_game_settings.json`에, 룸에서 조정하는 단위·범위·표시 순서는 `Resources/Data/game_settings.csv`에 둡니다. 런타임 값은 `SyncVar`로 동기화하는 구조체로 관리합니다.
- 값 검증(범위 제한)은 `GameSettingsValidator`가 담당합니다 (SRP).

### 2.2 고정 상수 (`GameConstants`)
| 이름 | 값 |
|---|---|
| `GuardMoveSpeed` | 2.0 |
| `MannequinMoveSpeed` | 1.5 |
| `MannequinHuntMoveSpeed` | 2.2 |
| `Damage` | 1 |
| `UntransformCooldown` | 5초 (Preparation 단계에서는 0) |
| `FakeMannequinPurgeTime` | 남은 시간 20초 |
| `ZeroObjectiveHuntDelay` | 20초 |
| `CoopDestroySpeedBase` | 1.4 |
| `MaxPlayers` | 10 |

### 2.3 주요 인터페이스
```csharp
public interface IDamageable { void TakeDamage(int amount, NetworkObject attacker); }
public interface IInteractable { bool CanInteract(PlayerRole role); void BeginInteract(NetworkConnection conn); void EndInteract(NetworkConnection conn); }
public interface ITool { bool IsReady { get; } void Use(); }
public interface INoiseSource { void EmitNoise(NoiseType type); }
public interface ITransformable { int PropId { get; } }
```

---

## 3. 개발 Phase

각 Phase는 **작업 목록 → 완료 기준** 순서로 구성됩니다. 체크박스를 채워가며 진행합니다.

### Phase 0 — 네트워크 기반

**목표:** 두 개의 클라이언트가 접속해 각자의 역할로 이동할 수 있다.

- [x] `_Project` 폴더 구조 생성, 어셈블리 정의(asmdef) 구성
- [x] URP 렌더러를 **Forward+**로 변경 (`PC_Renderer.asset`)
- [x] 테스트용 `Game_Test` 씬: 바닥, 벽, 대기실, 사물 몇 개
- [x] `NetworkManager` + Tugboat 설정, 테스트용 호스트/참가 버튼 (임시 UI)
- [x] `PlayerRole` (Guard / Mannequin / Spectator) 및 서버에서 역할 배정 (`RoleAssigner`: 가드는 최대 인원 / 2)
- [x] 역할별 플레이어 프리팹 스폰 (`PlayerSpawner`: 가드는 **대기실 스폰 지점**, 마네킹은 맵 스폰 지점)
- [x] ECM2 기반 이동: 가드 1인칭(속도 2), 마네킹 3인칭(속도 1.5)
- [x] `PlayerInputReader`: Input System 액션을 게임 입력 이벤트로 변환 (로컬 소유자만)
- [x] `GameStateMachine` (서버 권한, 상태 SyncVar) + 상태별 타이머
- [x] `GameSettings` 구조체 및 기본 프리셋 (하드코딩 값으로 시작)

**상태:** ✅ 검증 완료 (2026-09-24, 에디터 호스트 + 빌드 게스트)

**완료 기준:** 에디터 + 빌드 2개로 접속 → 역할이 배정되고 각자 올바른 시점으로 이동 → 상태가 `Preparation → Playing`으로 넘어가며 모든 클라이언트에 표시됨.

**구현 메모**
- 테스트 환경은 메뉴 `GuardVsMannequin > 테스트 환경 생성`(`TestEnvironmentSetupTool`)으로 생성한다. 다시 실행하면 프리팹과 씬을 새로 만든다. (Phase 1부터 Phase 0 툴을 대체)
- 이동은 **소유자 권한(client-authoritative) NetworkTransform**으로 동기화한다. 원격 아바타는 ECM2 시뮬레이션을 끈다. 지인용 게임이라 예측(prediction)보다 단순함을 우선하며, 사격·공격·체력 등 **판정은 계속 서버 권한**으로 처리한다.
- 게임 상태 규칙은 `IGameStateHandler` 구현체로 분리되어 있다. Phase 1 이후에는 `GameStateMachine.RegisterHandler`로 핸들러를 교체해 승패 조건 등을 추가한다.
- 입력은 `IPlayerInput`으로 추상화되어 있다. 액션 이름은 `GameInput.inputactions`의 Player 맵을 따른다.
- ESC로 커서 잠금을 전환한다. (임시 HUD 조작용이며, Phase 6에서 설정 메뉴로 대체)

---

### Phase 1 — 핵심 루프 (최소 플레이 가능 버전) ⭐

**목표:** 숨고, 쏘고, 전원 장치를 파괴하고, 승패가 난다.

#### 1-A. 가드 대기실
- [x] `WaitingRoomDoor` (NetworkBehaviour): `Preparation` 동안 닫힘, `Playing` 진입 시 서버가 열고 클라이언트에 동기화
- [x] 문이 닫혀 있는 동안 물리적으로 통과 불가 (콜라이더)

#### 1-B. 변신 시스템
- [x] `PropDatabase` (ScriptableObject): PropId ↔ 메시/머티리얼/콜라이더 크기 매핑
- [x] `TransformableProp` 컴포넌트: 변신 가능한 사물에 부착, PropId 보유
- [x] 변신 불가 대상 처리: 천장, 바닥, 조명 기구, 전원 장치, 전원 장치 주변 구조물에는 `TransformableProp`을 **붙이지 않음** (에디터 검증 툴로 확인)
- [x] 벽 변신: 벽에서는 **고정 크기 벽 조각** PropId로 변신
- [x] `MannequinTransformer`: 우클릭 → 조준한 사물 레이캐스트 → `ServerRpc(propId)` → 서버 검증 → `SyncVar<int> currentPropId` → 모든 클라이언트에서 외형 교체
- [x] 변신 중: 이동만 가능 (상호작용·감정 표현 입력 차단)
- [x] 변신 해제 쿨타임 5초 (`Preparation` 중에는 0초)
- [x] 여러 마네킹이 같은 사물로 변신 가능

#### 1-C. 가드 사격
- [x] `GunTool : ITool`: 좌클릭 → `ServerRpc(origin, direction)` → 서버 히트스캔
- [x] 히트스캔 사거리 > 맵 천장 높이 (맵마다 `MapInfo.ceilingHeight`로 검증)
- [x] 쿨타임 `gunCooldown` (탐지기와 **공유하지 않음**)
- [x] 명중 시 `IDamageable.TakeDamage`
- [x] 사냥 시간에도 사용 가능

#### 1-D. 체력·사망
- [x] `Health` (서버 권한, SyncVar): 최대 체력은 역할과 `GameSettings`에 따라 결정
- [x] 사망 시 플레이어 오브젝트 제거 → Spectator 역할로 전환 (관전은 Phase 4에서 구현, 여기서는 자유 카메라 임시 처리)

#### 1-E. 전원 장치
- [x] `Generator : IInteractable` (서버 권한)
  - 참여자 목록 관리, 매 틱마다 `progress += 1.4^(n−1) / generatorDestroyTime * deltaTime`
  - 참여자가 0명이 되면 `progress = 0`
  - `progress >= 1` → 파괴 상태, 이후 상호작용 불가
- [x] 마네킹 좌클릭 유지 = 상호작용 (변신 중에는 불가)
- [x] 진행도 SyncVar → 월드 공간 진행도 바 (마네킹에게만 표시)
- [x] `GeneratorSpawner`: 맵의 후보 위치 중 `generatorCount`개 무작위 배치
- [x] `ObjectiveTracker`: 파괴 횟수 카운트, `objectiveCount` 도달 시 사냥 시간 요청

#### 1-F. 승패 판정 (`WinConditionEvaluator`)
- [x] 가드 승리: ① 마네킹 전멸 ② `Playing` 제한 시간 종료 시 목표 미달성 ③ 사냥 시간 종료 시 가드 1명 이상 생존
- [x] 마네킹 승리: 사냥 시간 중 모든 가드 제거
- [x] 목표 수행 횟수 0회: `Playing` 진입 20초 후 사냥 시간 시작

#### 1-G. 최소 HUD
- [x] 남은 시간, 남은 마네킹 수, 달성 횟수 / 목표 횟수 (전원), 목표 안내 (마네킹만), 내 체력

**상태:** ✅ 검증 완료 (2026-09-24)

**완료 기준:** 가드 1명 + 마네킹 1~2명으로 한 판이 시작부터 승패 판정까지 오류 없이 진행된다. → **지인 테스트 플레이 진행**

**구현 메모**
- 대기실 문(`WaitingRoomDoor`)은 네트워크 오브젝트가 아니다. 이미 동기화된 게임 상태를 보고 각 피어가 같은 위치로 움직인다.
- 아바타 프리팹은 씬 오브젝트를 직렬화 참조할 수 없으므로, `GameServices.State`(`IGameStateProvider`)로 게임 상태를 조회한다.
- 승패와 사냥 시간 진입 조건은 `MatchRules`(`IMatchRules`)에 모여 있다. 상태 핸들러는 이 인터페이스에만 의존한다.
- 피해 가능 여부는 `DamageRules`가 판정한다. (가드→마네킹: 탐색/사냥, 마네킹→가드: 사냥 시간만)
- 변신 시 충돌체는 사물 경계에 맞춘 **캡슐로 근사**한다. (반지름 0.15~1.0m) 판정이 어색하면 Phase 2 이후 박스 충돌체로 교체를 검토한다.
- 전원 장치 상호작용은 **탐색(Playing) 단계에서만** 가능하다. (가드가 대응할 수 없는 준비 시간에 파괴되는 것을 방지)
- 사망 시 아바타를 제거하고 씬 카메라로 전환한다. 관전 모드는 Phase 4에서 구현한다.
- HUD는 IMGUI 임시 구현(`GameHud`)이며, 정식 UI는 Phase 6에서 교체한다.
- 변신 불가 대상 검증: 메뉴 `GuardVsMannequin > 변신 가능 사물 검증`

---

### Phase 2 — 속이기와 탐지

**목표:** 가짜 마네킹과 소음으로 심리전이 성립한다.

- [x] **감정 표현** 1~6번 키: `ServerRpc(emoteId)` → `ObserversRpc` 애니메이션 재생 (변신 중 불가)
- [x] **가짜 마네킹 AI** (서버 전용 로직, NetworkAnimator로 동기화)
  - 실제 마네킹과 **같은 프리팹 외형과 애니메이션** 사용
  - 행동 루프: 일정 시간 동안 무작위 방향 NavMesh 이동 또는 감정 표현
  - 게임 시작 시 무작위 수만큼 생성, 일정 간격으로 수 감소
  - 남은 시간 20초 또는 사냥 시간 진입 시 **전부 소멸** (실제 마네킹의 변신은 유지)
  - 가드의 총에 맞으면 소멸 (피격 반응은 실제 마네킹과 동일하게)
- [x] **소음 시스템**
  - `NoiseEmitter : INoiseSource` + `NoiseProfile` ScriptableObject (클립, 거리, 볼륨)
  - 서버에서 발생 → `ObserversRpc`로 3D 사운드 재생
  - 대상: 총 발사, 탐지기에 걸린 마네킹, 변신 상태의 주기적 소음, 전원 장치 파괴
- [x] **탐지기** `DetectorTool : ITool`: 우클릭 → 서버에서 범위 내 마네킹(변신 여부 무관) 검색 → 각자 소음 발생. 쿨타임 `detectorCooldown`
- [x] **벽타기** (변신 상태): ECM2 중력 방향 변경으로 벽면 부착
- [x] **마네킹 3인칭 카메라**: 벽에 붙었을 때 회전 각도 제한

**상태:** ✅ 검증 완료 (2026-09-25)

**완료 기준:** 가짜 마네킹과 실제 마네킹을 외형·움직임만으로 구분하기 어렵고, 소음을 듣고 위치를 추적할 수 있다.

**구현 메모**
- 감정 표현은 애니메이션 에셋이 없어 **절차적 움직임**(`ProceduralEmoteView`: 점프/회전/흔들기/인사/떨기/웅크리기)으로 임시 구현했다. 에셋 도입 시 `IEmoteView`의 Animator 구현으로 교체한다. 실제/가짜 마네킹은 같은 `EmoteController` + 뷰를 사용한다.
- 가짜 마네킹은 NetworkAnimator 대신 **서버 권한 NetworkTransform + 감정 표현 RPC**로 동기화한다. 외형은 실제 마네킹과 같은 생성 함수로 만든다.
- 가짜 마네킹 수는 6~12 무작위(`GameConstants`). 탐색 단계 동안 **남은 시간 20초까지 고르게 줄어들도록** 한 마리씩 제거한다.
- NavMesh는 서버가 **준비 단계 첫 진입 시 런타임으로 굽는다**(`NavMeshSurface`, 맵 충돌체 기준). 이때 대기실 문이 닫혀 있어 대기실은 분리된다. 전원 장치는 `NavMeshObstacle`로 구멍을 낸다.
- 소음은 `NoiseLibrary`(ScriptableObject)로 관리한다. 클립이 비어 있으면 `ProceduralNoiseClips`의 **임시 합성음**을 사용하므로, 사운드 에셋은 라이브러리에 지정만 하면 교체된다.
- 탐지기는 `PlayerIdentity`를 가진 **실제 마네킹만** 소음을 내게 한다. (가짜 마네킹이 반응하면 탐지기가 의미 없어짐) 반경은 `GameConstants.DetectorRadius` = 10m.
- 변신 중 주기적 소음 간격은 8~14초 무작위.
- 벽타기: 캐릭터를 **회전시키지 않고** ECM2 Flying 모드(중력 없음)로 면을 따라 이동한다. 바닥→벽(이동 방향 앞 벽), 벽→천장(위로 이동 중 머리 위 천장), 천장→벽, 벽→바닥(아래로 이동 중 발밑 바닥) 순으로 전환하며, 붙은 면이 사라지면 떨어진다. 벽에서는 W가 위로 오르기다. 이동/카메라는 `IMovementPlaneProvider`로 현재 면을 조회한다.
- 변신 중에는 이동해도 몸 방향이 바뀌지 않고 **Q/E로만 좌우 회전**한다(`DisguiseRotationAbility`, 120°/초). 마네킹의 회전 모드는 이 컴포넌트만 변경한다. (E는 가드 손전등 키와 같지만 역할이 겹치지 않음)
- 천장이 개방된 맵은 **투명 천장(충돌체만)** 을 둔다. 벽을 타고 오른 마네킹이 맵 밖으로 나가지 않게 한다. (투명 천장 위 실루엣은 기획상 재미 요소로 유지)
- 벽에 붙었을 때 카메라는 벽을 바라보는 방향 기준 좌우 75°, 천장에서는 아래에서 올려다보는 각도로 제한한다.

---

### Phase 3 — 사냥 시간

**목표:** 조명이 꺼지고 공수가 뒤바뀐다.

- [x] `LightController` (맵 단위): 등록된 모든 `MapLight`(Light + Emission 렌더러)를 일괄 On/Off
- [x] 사냥 시간 진입 연출: **텍스트·알림 없이** 모든 전구가 꺼짐 + 전원 차단 효과음
- [x] 사냥 시간 동안 환경광, 스카이박스 조정 (URP Volume 전환)
- [x] 가드 손전등 `FlashlightTool`: E키 On/Off, 배터리 무제한, 상태 SyncVar (다른 플레이어에게도 보임)
- [x] 마네킹 사냥 시간 변화
  - 체력 = `mannequinMaxHealth × huntHealthMultiplier` (현재 비율 유지 또는 가득 채움 중 선택, 기본은 **가득 채움**)
  - 이동 속도 2.2
  - 카메라 **1인칭 전환**
  - 진입 시 강제 변신 해제, 이후 다시 변신 가능 (Phase 6에서 변경)
  - 좌클릭 = 공격 (`attackRange` 내 가드에게 서버 판정, 변신 중에는 불가)
- [x] 사냥 시간 타이머 → 종료 시 승패 판정

**상태:** ✅ 검증 완료 (2026-09-25)

**완료 기준:** 목표 달성 또는 0회 설정 시 사냥 시간이 발동하고, 양측 승리 조건이 모두 정상 동작한다.

**구현 메모**
- 조명 연출(`HuntingLightingController`)은 대기실 문과 같이 **동기화된 게임 상태만 보고** 각 피어가 똑같이 수행한다. 조명은 0~0.5초 사이로 어긋나게 꺼지고, 전원 차단음은 2D로 전원에게 재생된다.
- 환경광은 URP Volume 대신 `RenderSettings`(환경광 Flat, 반사 0, 노출 0 스카이박스)로 어둡게 한다. 결과 화면까지 어둡게 유지하고, 다음 판 대기/준비 단계에서 되돌린다.
- HUD는 기획대로 사냥 시간 진입을 **텍스트로 알리지 않는다.** (탐색/사냥 시간 모두 단계 이름을 숨김) 역할별 조작 안내만 바뀐다.
- 원격 가드의 손전등이 실제 시선 방향을 비추도록 상하 시선을 `PlayerViewSync`로 동기화한다. (0.1초 간격, 1° 이상 변할 때만. Phase 4에서 관전용 좌우 시선까지 포함하도록 일반화)
- 마네킹 사냥 모드(`MannequinHuntingMode`): 서버는 강제 변신 해제와 체력 배율 적용(가득 채움), 소유자는 1인칭 전환과 속도 2.2를 적용한다. 1인칭에서는 마우스 좌우가 몸을 회전시킨다.
- 마네킹 시야(`HuntVisionEffect`): **본인 화면에만** 개인 조명(반경 8m, 그림자 없음)과 검은 선형 안개(2~9m)를 적용해 가까운 주변만 보이게 한다. 빌드에서 안개 셰이더가 제거되지 않도록 Graphics 설정의 Fog 스트리핑을 Custom(Linear 유지)으로 둔다.
- 마네킹 공격(`MannequinAttackTool`): 쿨타임 1초, 반경 0.35m 구체 판정, 사거리는 호스트 설정 `mannequinAttackRange`.
- 탐색 단계 좌클릭(전원 장치 파괴)과 사냥 시간 좌클릭(공격)은 게임 상태로 구분한다.

---

### Phase 4 — 사망 후 경험과 결과

- [x] **관전 모드**: 좌우 화살표로 생존 플레이어 순환, 관전 대상 이름 표시, 대상 사망 시 자동 전환
- [x] **저체력 시야 효과**: 잔여 체력 비율에 따라 URP Volume Vignette 강도·어둡기 증가 (자신에게만)
- [x] **결과 화면**: 승패, 역할별 결과 → 일정 시간 후 자동 또는 버튼으로 룸 씬 이동 (룸 씬은 Phase 5, 지금은 대기 상태로 복귀)
- [x] 킬 로그는 **표시하지 않음**, 다른 플레이어 체력도 **표시하지 않음**

**상태:** ✅ 검증 완료 (2026-09-25)

**완료 기준:** 사망한 플레이어가 끝까지 게임을 관전하고, 결과 후 룸으로 돌아간다.

**구현 메모**
- 관전: 아바타가 없을 때(사망 또는 미참가) 켜지는 씬 카메라를 `SpectatorController`가 생존자의 시야로 옮긴다. 생존자 목록은 클라이언트에 스폰된 아바타(`ClientPlayerList`)이며, 사망하면 아바타가 제거되므로 자동으로 다음 대상으로 넘어간다.
- 대상 시야: `PlayerViewSync`가 모든 플레이어의 카메라 시선(Pitch, Yaw)을 0.1초 간격으로 동기화하고, `SpectatorViewPoint`가 역할에 맞게 1인칭(가드, 사냥 시간 마네킹) 또는 3인칭(마네킹) 카메라 위치를 계산한다. 사냥 시간에 마네킹을 관전하면 그 마네킹의 시야 효과(`IViewEffect`: 개인 조명 + 안개)를 관전자 화면에도 똑같이 적용한다.
- 저체력 효과: `LowHealthVignette`가 런타임에 전역 URP Volume(비네트 최대 0.6 + 노출 최대 −1.5EV)을 만든다. 플레이어 카메라의 Post Processing을 켜 둔다.
- 결과: 참가자 목록은 서버에만 보관하다가 **판이 끝날 때 공개**한다(`MatchResult.Participants`). 진행 중 사망 정보가 네트워크로 새지 않게 하기 위함이다. 결과 공개 시점에 아바타가 남아 있으면 생존, 사망·접속 종료는 탈락으로 표시한다.
- 결과 화면은 10초 뒤 자동으로 대기 상태로 돌아가며, 호스트는 "바로 다음 판 준비" 버튼으로 즉시 돌아갈 수 있다. 결과 화면에 들어서면 버튼을 누를 수 있게 커서 잠금이 풀린다.

---

### Phase 5 — Steam 연동과 로비/룸 씬

- [x] Steamworks.NET(2025.164.1), FishySteamworks(4.1.1) 설치 (중복 steam_api 플러그인 정리)
- [x] 트랜스포트 선택: Multipass(Tugboat + FishySteamworks). Steam 방은 두 트랜스포트로 서버를 열고, 직접 방(개발/LAN)은 Tugboat만 사용 (`TransportConnector`)
- [x] **로비 씬**: 방 만들기(Steam 로비 생성 + 호스트 시작), 방 코드 참가(코드 = 로비 ID), 친구 초대/친구 방 참가, 직접 접속(개발용)
- [x] **룸 씬**
  - 호스트: 시작 버튼 (전원 준비 + 인원 조건 충족 시 활성화)
  - 게스트: 준비 / 준비 해제
  - 게임 설정 UI (호스트만 편집, 전원 열람): 맵, 대기 시간, 제한 시간, 사냥 시간, 가드·마네킹 수, 목표 수행 횟수(0 ~ 마네킹 수 + 2), 도구 쿨타임, 체력, **사냥 시간 체력 배율**
- [x] 호스트 이탈 시 모든 게스트를 로비 씬으로 퇴출
- [x] 게임 종료 후 같은 방으로 룸 씬 복귀

**상태:** 🧪 한 PC(직접 방) 테스트 완료 (2026-09-25) / Steam 2PC 테스트 대기

**완료 기준:** 서로 다른 PC의 Steam 친구끼리 초대/코드로 접속해 설정을 바꾸고 한 판을 끝까지 진행한다.

**구현 메모**
- 씬 흐름: `Lobby`(시작, 오프라인) → 서버 시작 시 FishNet `DefaultScene`이 `Room`을 전역 로드 → 호스트가 시작하면 `RoomState`가 `Game_Test`를 전역 로드 → 결과 후 `Room`으로 복귀. 연결이 끊기면 `DefaultScene`이 `Lobby`를 불러온다.
- `Lobby` 씬의 **NetworkRoot**(NetworkManager, Multipass, DefaultScene, Steam/세션 서비스)는 DontDestroyOnLoad로 유지된다. 로비로 돌아왔을 때 생기는 중복은 NetworkManager가 제거하고, 서비스는 "먼저 등록된 것이 유지" 규칙으로 등록한다. **Play는 반드시 Lobby 씬에서** 시작한다. (게임 씬에는 NetworkManager가 없음)
- `SessionService`(`ISessionService`): Steam 로비와 연결을 묶는다. Steam 방 = 로비 생성 → 서버(Tugboat + Steam) 시작 → 로컬 클라이언트는 Tugboat localhost로 접속. 참가 = 로비 입장 → 로비 데이터의 호스트 SteamID로 FishySteamworks 접속. 게임 실행 중 초대 수락(`GameLobbyJoinRequested_t`)과 초대로 실행(`+connect_lobby`)을 모두 처리한다.
- `RoomState`: 전역 네트워크 오브젝트. 참가자(이름·준비·호스트), 게임 설정을 동기화한다. 설정은 `GameSettingDefinitions`(항목·단위·범위)로 조정하고 인원 변화에 맞춰 `GameSettingsValidator`로 다시 보정한다. 게임 중에는 Steam 로비 참가를 막는다.
- 게임 씬은 `IRoomInfo`/`ISceneFlow`(Core 인터페이스)로만 룸에 접근한다. `MatchSessionBridge`가 모든 플레이어의 씬 로드를 기다려(최대 15초) 판을 시작하고, 결과 후 룸으로 돌려보낸다.
- NetworkManager의 `ObserverManager`에 **SceneCondition**을 기본 조건으로 둔다. 씬 오브젝트가 그 씬을 불러온 클라이언트에게만 스폰되게 해, 게스트가 게임 씬 로딩 중에 스폰 정보를 받아 `GameSystems`를 찾지 못하는 문제(SceneId not found)를 막는다. 전역 오브젝트(`RoomState`)는 조건에서 제외된다.
- 닉네임: 각 클라이언트가 접속 후 자신의 Steam 이름을 서버에 알린다. Steam이 없으면 "Player N".
- UI는 **uGUI**(에디터 툴로 씬에 생성, 인스펙터에서 편집 가능). 기본 폰트에 한글이 없어 `OsFontApplier`가 실행 시 OS 한글 폰트(맑은 고딕)를 적용한다. (Phase 6에서 TMP + 한글 폰트 에셋으로 교체 검토)
- 한 PC에서는 같은 Steam 계정으로 호스트/게스트를 동시에 실행할 수 없으므로, 한 PC 테스트는 **직접 방**으로, Steam 테스트는 PC 2대 + Steam 계정 2개로 한다.

---

### Phase 6 — 마무리

- [x] 설정: 키 리바인딩(Input System Rebinding API, 저장/불러오기), 마우스 감도, 사운드(마스터/BGM/효과음)
- [x] 게임 중 ESC 메뉴 (계속하기 / 설정 / 방 나가기) — 룸과 게임 씬
- [x] 크레딧: 기획자, 개발자, 이용한 에셋 (`Docs/AssetLicenses.md` 기반)
- [x] HUD를 IMGUI 임시 구현에서 uGUI로 교체
- [x] 다국어 (한국어/영어, CSV 열 추가로 언어 확장) — 설정 화면에서 언어 선택
- [x] 룸 설정에 가드/마네킹 이동 속도 추가 (마네킹 사냥 시간 속도 = 기본 × 1.5)
- [x] 사람이 관리하기 쉬운 데이터를 텍스트 파일로 분리 (문장, 크레딧, 게임 설정 기본값·범위)
- [x] 사냥 시간에도 마네킹 변신 허용 (진입 시 강제 해제는 유지, 변신 중 공격 불가)
- [x] 보안 보강: Steam 방 Tugboat 루프백 바인딩, 접속 검증(`RoomAuthenticator`), 로비 공개 범위 설정, 닉네임 정리, 서버 이동 검증(`MovementValidator`)
- [x] 텍스트 데이터 파일을 빌드 후에도 수정 가능하게 (빌드 폴더 `GameData`, 없거나 깨지면 내장 원본 → 코드 기본값)
- [x] 캐릭터 외형: 마네킹 3종(더미·슬라임·유령, 맵마다 선택), 가드(Ethan + 더미 애니메이션)
- [x] 가드 리볼버(1인칭·3인칭)와 총구에서 나가는 총알·명중·총구 화염 이펙트
- [x] 맵 4종 자동 생성 도구 (Asylum, Industrial, Hospital, Alchemist House) — 생성 후 배치 확인·조정 필요
- [ ] 정식 맵 다듬기: 자동 배치된 스폰·전원 장치·출발 지점·조명 위치 조정 (천장 개방 맵은 투명 천장, 프롭 실루엣 문제는 의도된 재미로 유지)
- [ ] 밸런스 조정, 최적화 (실시간 조명 개수, 네트워크 전송량)
- [ ] (검토) TextMeshPro + 한글 폰트 에셋으로 UI 폰트 교체

**상태:** 설정·ESC 메뉴·크레딧·HUD·로비 패널 분리·다국어·이동 속도 검증 완료 (2026-09-26) / 데이터 파일 분리·사냥 시간 변신·보안 보강·외부 데이터 파일·캐릭터 외형·총·맵 4종 검증 전

**구현 메모**
- 설정 화면은 탭으로 나뉜다: **그래픽**(화면 모드: 전체 화면(창)/창 모드/최대화 창, 해상도(누르면 펼쳐지는 스크롤 목록), 언어) · **사운드**(음량) · **조작 키**(마우스 감도, 키 설정). 코드는 틀(`SettingsPanelView`: 탭·열기/닫기·모두 기본값)과 설정 묶음(`ISettingsSection`: `DisplaySettingsSection`, `LanguageSettingsSection`, `SoundSettingsSection`, `ControlSettingsSection`)으로 나뉘며, 새 설정은 묶음 컴포넌트를 페이지에 두기만 하면 자동으로 연결된다.
- 룸 게임 설정 범위 확대(2026-09-29): 대기 15~180초, 탐색 120~1800초, 사냥 30~900초, 체력 가드 1~100·마네킹 1~500, 사냥 체력 배율 1~100, 공격 범위 1~30m, 이동 속도 0.5~20. 피해량 설정 추가(가드 총·마네킹 공격, 1~500, 기본 1). `GameSettingId`에 새 항목은 끝에만 추가한다(UI 프리팹이 번호로 저장).
- 룸 게임 설정(맵 제외)은 -/+ 버튼 외에 값을 눌러 키보드로 입력할 수 있다(호스트만). 누르면 단위 없는 숫자로 바뀌고 Enter/다른 곳 클릭으로 적용되며, 서버(`RoomState.SetSettingValue`)가 항목 범위와 인원 조건으로 보정한다. 숫자가 아니면 원래 값으로 돌아간다.
- 넘칠 수 있는 목록은 스크롤: 룸의 참가자·게임 설정 목록, 크레딧, 결과 화면 참가자, 키 설정.
- 게임 이름: **Hide&Freak** (로비 제목·크레딧·창 제목). 코드 네임스페이스와 저장소 이름은 GuardVsMannequin/GuardVsMonster 유지. 창 제목(productName)이 바뀌면 PlayerPrefs 저장 위치도 바뀌어 개인 설정이 한 번 초기화된다. 화면 설정은 `DisplaySettings`가 `Screen.SetResolution`으로 바로 적용하고, 해상도 목록은 모니터 지원 해상도(가로×세로 중복 제거)다. 플레이어가 한 번이라도 고르면 다음 실행 때도 적용된다. (에디터에서는 적용하지 않음) "모두 기본값으로"는 언어와 화면 설정을 유지한다.
- 개인 설정(`GameOptions`: 감도 배율, 전체/배경 음악/효과음 음량, 화면 크기·모드)과 키 바인딩은 `OptionsStore`가 **PlayerPrefs**(이 PC)에 저장한다. 호스트가 정하는 게임 설정(`GameSettings`)과는 별개다.
- 키 바인딩: 입력 에셋 복제본을 만드는 곳(`PlayerInputReader`, `SpectatorController`, `PauseMenuView`, 설정 화면)마다 `OptionsStore.ApplyBindings`로 저장된 바인딩을 적용하고, 게임 중 바꾸면 `BindingsChanged`로 즉시 반영한다. 바꿀 수 있는 키 목록은 `KeyBindingCatalog`.
- 음량: Unity에는 AudioMixer 에셋을 코드로 만드는 공개 API가 없어, 지금은 **전체 = `AudioListener.volume`, 효과음/배경 음악 = `AudioVolumes` 배율**로 적용한다. 사운드 에셋 도입 시 AudioMixer 그룹으로 교체할 수 있도록 음량 조회를 `AudioVolumes` 한곳에 모았다. (배경 음악은 아직 없음)
- ESC: 기존의 "커서 잠금 전환"을 메뉴로 교체했다(입력 액션 `ToggleCursor` → `Menu`). 커서 잠금은 `GameCursor`가 관리하며, 잠겨 있을 때만 이동·시점·클릭 입력을 받는다. 키 설정 중에는 ESC가 키 입력 취소로 쓰이므로 메뉴가 반응하지 않는다.
- 크레딧 내용은 `Resources/Credits/credits.<언어 코드>.txt` 텍스트 파일에서 편집한다. (기획·개발: 정승제) 현재 언어 파일이 없으면 ko 파일을 보여준다.
- **텍스트 데이터 파일** (모두 `Assets/_Project/Resources/` 아래, 메모장·엑셀로 편집. UTF-8로 저장)

  | 파일 | 내용 |
  |---|---|
  | `Localization/strings.csv` | 화면의 모든 문장. 열 = `key, ko, en, …`. `#` 줄은 메모, 값의 쉼표는 큰따옴표로 감싸고 줄바꿈은 `\n` |
  | `Credits/credits.ko.txt`, `credits.en.txt` | 언어별 크레딧 |
  | `Data/default_game_settings.json` | 방을 만들 때의 게임 설정 기본값 |
  | `Data/game_settings.csv` | 룸 설정 항목의 표시 순서, 조정 단위(step), 범위(min~max), 단위 문구 key |

- **다국어**: `Loc.Get(key)` / `Loc.Format(key, 값…)`로 문장을 얻는다. 고정 문구는 `LocalizedText` 컴포넌트(에디터 툴이 key로 만든 텍스트에 자동 부착), 상황에 따라 바뀌는 문구는 각 화면 코드가 채운다. 언어를 바꾸면 `Loc.LanguageChanged`로 즉시 갱신된다. 첫 실행 언어는 시스템 언어(한국어면 ko, 그 외 en)이며, 선택한 언어는 `GameOptions.languageCode`로 저장된다.
  - 새 언어 추가: `strings.csv`에 열 추가(머리글 = 언어 코드, `language.name` 줄에 그 언어로 쓴 이름) → 설정 화면 언어 목록에 자동 표시. 크레딧은 `credits.<코드>.txt` 추가. 번역이 비어 있는 칸은 ko로 표시된다.
  - 한국어·영어 외에 한자/가나 등을 쓰는 언어는 `OsFontApplier`의 폰트가 글자를 지원하는지 확인해야 한다.
- **이동 속도**: `MoveSpeedController`(가드/마네킹 프리팹)가 동기화된 이번 판 설정으로 `maxWalkSpeed`/`maxFlySpeed`(벽타기)를 맞추고, 마네킹은 사냥 시간에 1.5배로 바꾼다. 가짜 마네킹(NavMeshAgent)도 마네킹 기본 속도를 따른다.
- **사냥 시간 변신**: `MannequinTransformer.CanTransformIn`에 `Hunting`을 추가했다. 진입 순간의 강제 해제(`MannequinHuntingMode`)는 그대로 두어 사냥 시간 시작 시 모두 본모습이 드러난다. 변신 중에는 탐색 단계의 상호작용처럼 공격도 막는다(`MannequinAttackTool`). 1인칭에서 사물 메시가 시야를 가리지 않도록 본인 화면에서는 변신 외형도 그림자만 그린다(`PropDisguiseView.SetDisguiseShadowsOnly`). 해제 쿨타임 5초는 동일하다.
- **보안** (호스트 = 서버인 P2P 구조라 호스트의 조작과 위치 정보를 이용한 월핵은 막을 수 없다. 게스트 쪽 부정행위와 무단 접속을 줄이는 것이 목표)
  - Steam 방에서는 Tugboat를 `127.0.0.1`/`::1`에만 연다(`TransportConnector`). 호스트 자신의 로컬 접속용이며, 방 코드 없이 IP로 들어오는 경로를 막는다. 직접 방은 인스펙터의 바인드 주소를 그대로 쓴다.
  - `RoomAuthenticator`(FishNet Authenticator, `SessionService`가 실행 시 등록): 판 진행 중 접속, 인원 초과, Steam 방의 비로비 SteamID·외부 IP 접속을 거부한다. 로비 멤버 목록 갱신이 늦을 수 있어 최대 5초 기다린다. `RoomState`는 **인증을 통과한 연결만** 참가자로 등록한다.
  - 로비 공개 범위는 `SteamLobbyService.lobbyType`(기본 FriendsOnly). Public은 코드를 아는 누구나 들어올 수 있고 AppID 480에서는 로비 검색에 보일 수 있다. **정식 AppID로 교체 전까지 FriendsOnly 권장.**
  - 닉네임은 서버에서 `<`, `>`, 제어 문자를 제거한다(리치 텍스트 태그로 UI를 깨뜨리는 것 방지).
  - `MovementValidator`(가드·마네킹 프리팹): 소유자 권한 이동을 서버가 검증한다. 최대 속도 × 1.3으로 이동 예산을 쌓고(최대 1.5초분 + 1m), 수평 이동·상승이 예산을 넘으면 마지막 정상 위치로 되돌린다(`TargetRpc`). 가드 대기 시간에는 가드가 `WaitingRoomArea`(대기실 오브젝트, 로컬 상자) 밖으로 나가면 되돌린다. 낙하는 검사하지 않는다. 새 맵에는 대기실에 `WaitingRoomArea`를 붙여야 한다.
- **빌드 후 편집 가능한 데이터 파일**: `DataFiles`가 ① 빌드 폴더 `<게임>_Data/StreamingAssets/GameData/…` → ② 내장 원본(`Resources`) → ③ 코드 기본값 순서로 읽는다. 파일이 없거나, UTF-8이 아니거나(깨진 글자 포함), 해석에 실패하면 다음 순서로 넘어간다. 원본은 계속 `Assets/_Project/Resources`에서 편집하며, 빌드가 끝나면 `DataFilesBuildProcessor`가 `Localization`, `Credits`, `Data` 폴더와 안내문(README.txt)을 빌드 폴더로 복사한다. (Windows/Linux 빌드)
- **캐릭터 외형**: `CharacterSkin`(모델 + 애니메이터) 에셋을 `CharacterSkinView`가 아바타에 붙인다. 맵의 `MapInfo`에 마네킹/가드 외형을 지정하면 그 맵에서만 바뀌고(가짜 마네킹 포함), 비우면 프리팹 기본 외형(마네킹=더미, 가드=Ethan)을 쓴다. 애니메이션은 위치 변화로 속도를 계산해 `Speed`, `MoveX`, `MoveZ` 중 컨트롤러에 있는 파라미터만 설정한다. 몸체 렌더러는 `CharacterSkinView.BodyRenderers`로 변신 숨김·1인칭 그림자 처리에 쓰인다.
  - 외형·무기 모델은 메뉴 `GuardVsMannequin > 캐릭터·무기 에셋 생성`(테스트 환경 생성에 포함)이 만든다: 원본에서 스크립트·충돌체를 빼고, 발을 원점에 맞추고 키를 맞추고(사람 1.8m, 슬라임 1.3m, 유령 1.6m), 머티리얼을 URP Lit으로 변환해 `Assets/_Project/Art/Skins`에 저장. 휴머노이드(더미·Ethan)는 Kevin Iglesias 8방향 걷기·달리기 2D 블렌드 컨트롤러를 공유한다.
  - 새 외형 추가: `Create > GuardVsMannequin > Character Skin`으로 에셋을 만들고 모델 프리팹(발 원점, 정면 +Z)과 컨트롤러를 지정 → 맵의 `MapInfo`에 넣는다. 정면이 다르면 `yawOffset`으로 보정.
- **가드 총**: `GuardWeaponView`가 소유자에게는 카메라 앞 1인칭 리볼버, 다른 플레이어에게는 오른손 위치에서 조준 방향을 향하는 3인칭 리볼버를 보여준다(휴머노이드는 `HandAimIK`로 오른손을 조준 방향으로 뻗음). 판정은 그대로 서버 히트스캔이고, `ShotTracerView`가 보는 사람 기준 총구에서 총알 이펙트(`BulletEffect`)를 끝점까지 날리고 명중·총구 화염 이펙트를 재생한다. (MasterStylizedProjectiles의 OrangeGunShot)
- **맵 자동 생성** (`GuardVsMannequin > 맵 생성`, `MapCatalog`의 순서 = 맵 번호): 원본 데모 씬을 가져와 정리(프리팹 연결 해제, 카메라·UI·에셋 스크립트 제거, 물리 고정) → 머티리얼 URP 변환(`UrpMaterialConverter`, 원본 에셋 머티리얼을 직접 바꿈) → 충돌체 없는 메시에 MeshCollider → 크기·이름 규칙으로 변신 사물/벽 조각 지정(맵 전용 `PropDatabase_<씬>`) → 임시 NavMesh로 플레이 가능한 바닥(실내 맵은 천장 있는 곳, 가장 넓게 이어진 영역)을 분석해 마네킹 스폰 10·전원 장치 후보 12·가드 출발 지점 5를 고르게 배치 → 조명을 실시간 MapLight로 바꾸고 부족하면 추가 → 투명 경계·천장, 맵 아래 닫힌 대기실 → 게임 시스템·HUD → 빌드 설정과 RoomState 맵 목록 등록.
  - 새 맵의 가드 대기실은 맵과 떨어져 있어, 탐색 단계 시작 시 `GuardReleaseTeleporter`가 가드를 `GuardReleasePoint`로 옮긴다(`MovementValidator.ServerTeleport`). 테스트 맵은 출발 지점이 없어 기존처럼 문이 열린다.
  - 빌드 설정에 없는 맵을 고르면 `RoomState`가 테스트 맵으로 대신한다.
- **UI 프리팹과 맵 제작 도구**: 사용법은 `Docs/UiAndMapGuide.md`. UI는 `Prefabs/UI`의 프리팹(설정·크레딧 패널은 중첩 프리팹)으로 관리되어 씬 생성 도구를 다시 실행해도 직접 수정한 내용이 유지된다(`UiPrefabLibrary`). 맵은 "맵 제작 도우미" 창(`MapMakerWindow`)에서 새 맵 뼈대 생성 → 등록(`PropRegistrar` 공용) → 지점 배치 → 경계 → 검사(`MapValidator`) → 최적화 → 맵 목록 갱신(`MapRegistrySync`: 빌드 설정·RoomState·strings.csv·game_settings.csv) 순서로 만든다. 직접 만든 맵은 `Scenes/Custom`에 두면 자동 생성 맵 뒤 번호로 붙는다.
- **맵 크기**: 걸을 수 있는 바닥이 800㎡보다 좁은 맵은 생성 시 환경 전체를 키운다(최대 2.5배, 조명 범위도 함께). `MapCatalog`의 `scale`에 값을 넣으면 그 배율로 고정된다(0 = 자동).
- **전원 장치 배치**: 생성 시 후보 12개를 서로 최대한 멀리 배치하고, 매 판 `GeneratorSpawner`가 후보 중 서로 먼 것을 고른다(가장 먼 상위 3개 중 무작위). 편집은 `GuardVsMannequin > 맵 편집` 메뉴(씬 뷰 중앙에 전원 장치 후보·마네킹 스폰·가드 출발 지점 추가, 전원 장치 후보 넓게 다시 배치)나 씬에서 직접 옮기기·복제로 한다. 후보는 씬 뷰에 빨간 상자와 이름표로 보인다.
- **캐릭터 애니메이션 규칙** (`CharacterAnimatorParameters`): 외형 컨트롤러에 `Speed`/`MoveX`/`MoveZ`(Float), `Attack`/`Shoot`/`Hit`(Trigger), `Emote1`~`Emote6`(Trigger) 또는 `Emote`(Trigger)+`EmoteId`(Int)를 만들면 게임이 자동으로 사용한다. 없는 파라미터는 건너뛰며, 감정 표현 애니메이션이 없으면 절차적 움직임으로 대신한다(`AnimatorEmoteView` → `ProceduralEmoteView`).
- **리볼버 위치**: 모두 매 프레임 적용되므로 Play 중에 맞춘다.
  - 1인칭 총(카메라 기준, 외형과 무관): Guard 프리팹의 `GuardWeaponView`. Play 중 맞춘 뒤 Copy Component → 프리팹에 Paste Component Values.
  - 3인칭 총·조준 IK 손 위치(외형마다 다름): 외형 에셋 `Skin_*.asset`의 **Weapon Grip**. 에셋이라 Play 중 바꾼 값이 그대로 저장되고, '캐릭터·무기 에셋 생성'을 다시 실행해도 유지된다. 외형이 없는 가드는 `GuardWeaponView`의 예비 값(fallbackGrip)을 쓴다.
- **총알 이펙트**: `ShotTracerView`의 총알·명중·총구 화염 밝기와 크기 배율(기본 0.35~0.4 / 0.4~0.6)로 조정한다. 파티클 시작 색의 RGB·알파와 시작 크기에 곱한다.
- **변신 사물 외곽선**: 마네킹 본인 화면에서 조준 중인 변신 가능 사물(변신 판정과 같은 조건)에 `GuardVsMannequin/PropOutline` 셰이더로 화면 기준 일정한 두께의 외곽선을 그린다(`PropHighlighter`). 색·두께는 `Art/Materials/M_PropOutline`에서 조정. 변신 사물은 외곽선이 원래 메시를 써야 하므로 정적 배칭에서 제외한다.
- **맵 최적화** (`GuardVsMannequin > 맵 최적화`, 맵 생성 시 자동 실행): 투명 경계 밖(실내 3m·실외 12m 여유)의 환경 오브젝트와 꺼진 오브젝트를 지우고, 남은 환경에 Occludee·정적 배칭 플래그(1m 이상 불투명 메시는 Occluder)를 준 뒤, 플레이 영역을 덮는 Occlusion Area로 오클루전 컬링을 굽는다. 수동으로 옮긴 스폰·전원 장치 배치는 건드리지 않는다.
- **빌드 최적화 설정** (`GuardVsMannequin > 빌드 최적화 설정 적용`): 셰이더 변형이 4천 개 넘게 나와 첫 빌드가 1시간 이상 걸리던 문제로, PC URP 에셋의 라이트 쿠키·라이트 레이어·LOD 크로스페이드·반사 프로브 블렌딩/박스 투영·부드러운 그림자를 끄고 캐스케이드를 2개로 줄이며, SSAO를 끄고, Windows 그래픽 API를 Direct3D11만 쓰게 한다. (손전등용 추가 조명 그림자는 유지. Mobile 품질 레벨은 원래 PC 빌드에서 제외되어 있음)
- 새로 추가된 외부 에셋 중 Unity 6에서 컴파일되지 않던 옛 스크립트(Standard Assets `Utility` 3개와 에디터 스크립트 2개, Flooded_Grounds PostProcessing 에디터 2개)는 제거된 API만 대체 API로 바꿨다. (파일마다 "GuardVsMannequin 프로젝트 수정" 주석)
- 인스펙터 툴팁·개발용 로그는 한국어로 유지한다. (플레이어에게 보이지 않음)
- HUD(`GameHud`)는 같은 표시 로직을 uGUI Text/패널로 옮겼다. UI는 모두 에디터 툴(`SharedUiBuilder`, `MenuSceneBuilder`)로 씬에 생성되어 인스펙터에서 편집할 수 있다.

---

## 4. 필요 에셋 체크리스트

에셋을 추가할 때마다 `Docs/AssetLicenses.md`에 **이름 / 출처 / 라이선스 / 사용처**를 기록합니다 (크레딧용).

### 4.1 플러그인
- [x] FishNet
- [x] ECM2
- [x] Steamworks.NET
- [x] FishySteamworks
- [ ] 한글 폰트 (Noto Sans KR, TMP 폰트 에셋)
- [ ] (선택) DOTween

### 4.2 3D 모델
- [ ] 마네킹 캐릭터 (휴머노이드 리그)
- [ ] 가드 3인칭 전신 모델 + 1인칭 팔, 총, 손전등
- [ ] 시설 사물 팩 (다양한 크기, 콜라이더 포함)
- [ ] 전원 장치 (정상 / 파괴 상태)
- [ ] 조명 기구 (On/Off Emission 머티리얼)
- [ ] 벽·바닥·천장 모듈 키트, 대기실 문

### 4.3 애니메이션
- [ ] 마네킹: Idle, 걷기, 달리기, 감정 표현 6종, 공격, 피격, 사망
- [ ] 가드: 3인칭 이동·사격·피격·사망, 1인칭 사격·손전등

### 4.4 VFX
- [ ] 총구 화염, 탄착, 변신 연기, 탐지기 펄스, 전원 장치 스파크·폭발

### 4.5 사운드
- [ ] 가드: 총소리, 쿨다운 완료음, 탐지기, 손전등 클릭, 발소리
- [ ] 마네킹: 탐지 소음, 변신 상태 주기 소음, 변신/해제, 공격, 발소리
- [ ] 전원 장치: 파괴 진행 루프, 폭발
- [ ] 사냥 시간: 전원 차단음, BGM
- [ ] 기타: 대기실 문 열림, 환경음, 로비 BGM, UI 클릭, 승리/패배 징글

### 4.6 UI
- [ ] 크로스헤어, 도구 쿨타임 아이콘, 진행도 바, 로비·룸·설정 UI 스킨

---

## 5. 추후 확장 (1차 개발 범위 제외)

- 커스텀 맵 모드: 에셋번들 로딩, 변신 가능 사물 태깅 규칙, 맵 제작 에디터 툴
- 1차 개발 중에도 확장을 고려해 **맵 데이터(`MapInfo`)와 변신 가능 사물 정보(`PropDatabase`)는 씬과 분리된 데이터로 관리**합니다.
