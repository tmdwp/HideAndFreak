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
| 2 | **목표 수행 횟수 0회** | `Playing` 단계 진입(대기실 문 열림) **20초 후 자동으로 사냥 시간** 시작 (가드 보호 목적) |
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

- 기본값은 `GameSettingsPreset` ScriptableObject에 저장하고, 런타임 값은 `SyncVar`로 동기화하는 구조체로 관리합니다.
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

**상태:** 코드 작성 완료 / 완료 기준 검증 전

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

- [ ] `LightController` (맵 단위): 등록된 모든 `MapLight`(Light + Emission 렌더러)를 일괄 On/Off
- [ ] 사냥 시간 진입 연출: **텍스트·알림 없이** 모든 전구가 꺼짐 + 전원 차단 효과음
- [ ] 사냥 시간 동안 환경광, 스카이박스 조정 (URP Volume 전환)
- [ ] 가드 손전등 `FlashlightTool`: E키 On/Off, 배터리 무제한, 상태 SyncVar (다른 플레이어에게도 보임)
- [ ] 마네킹 사냥 시간 변화
  - 체력 = `mannequinMaxHealth × huntHealthMultiplier` (현재 비율 유지 또는 가득 채움 중 선택, 기본은 **가득 채움**)
  - 이동 속도 2.2
  - 카메라 **1인칭 전환**
  - 강제 변신 해제 및 변신 불가
  - 좌클릭 = 공격 (`attackRange` 내 가드에게 서버 판정)
- [ ] 사냥 시간 타이머 → 종료 시 승패 판정

**완료 기준:** 목표 달성 또는 0회 설정 시 사냥 시간이 발동하고, 양측 승리 조건이 모두 정상 동작한다.

---

### Phase 4 — 사망 후 경험과 결과

- [ ] **관전 모드**: 좌우 화살표로 생존 플레이어 순환, 관전 대상 이름 표시, 대상 사망 시 자동 전환
- [ ] **저체력 시야 효과**: 잔여 체력 비율에 따라 URP Volume Vignette 강도·어둡기 증가 (자신에게만)
- [ ] **결과 화면**: 승패, 역할별 결과 → 일정 시간 후 자동 또는 버튼으로 룸 씬 이동
- [ ] 킬 로그는 **표시하지 않음**, 다른 플레이어 체력도 **표시하지 않음**

**완료 기준:** 사망한 플레이어가 끝까지 게임을 관전하고, 결과 후 룸으로 돌아간다.

---

### Phase 5 — Steam 연동과 로비/룸 씬

- [ ] Steamworks.NET, FishySteamworks 설치 (FishNet 버전 호환 확인)
- [ ] `TransportSelector`: 에디터/개발 빌드는 Tugboat, 릴리스는 FishySteamworks (DIP: 접속 로직은 트랜스포트에 의존하지 않음)
- [ ] **로비 씬**: 방 만들기(Steam 로비 생성 + 호스트 시작), 방 코드 참가(코드 = 로비 ID), 친구 초대/친구 방 참가
- [ ] **룸 씬**
  - 호스트: 시작 버튼 (전원 준비 + 인원 조건 충족 시 활성화)
  - 게스트: 준비 / 준비 해제
  - 게임 설정 UI (호스트만 편집, 전원 열람): 맵, 대기 시간, 제한 시간, 사냥 시간, 가드·마네킹 수, 목표 수행 횟수(0 ~ 마네킹 수 + 2), 도구 쿨타임, 체력, **사냥 시간 체력 배율**
- [ ] 호스트 이탈 시 모든 게스트를 로비 씬으로 퇴출
- [ ] 게임 종료 후 같은 방으로 룸 씬 복귀

**완료 기준:** 서로 다른 PC의 Steam 친구끼리 초대/코드로 접속해 설정을 바꾸고 한 판을 끝까지 진행한다.

---

### Phase 6 — 마무리

- [ ] 설정: 키 리바인딩(Input System Rebinding API, 저장/불러오기), 마우스 감도, 사운드(마스터/BGM/효과음, AudioMixer)
- [ ] 크레딧: 기획자, 개발자, 이용한 에셋 (`Docs/AssetLicenses.md` 기반)
- [ ] 정식 맵 제작 (천장 개방 맵은 투명 천장, 프롭 실루엣 문제는 의도된 재미로 유지)
- [ ] 밸런스 조정, 최적화 (실시간 조명 개수, 네트워크 전송량)

---

## 4. 필요 에셋 체크리스트

에셋을 추가할 때마다 `Docs/AssetLicenses.md`에 **이름 / 출처 / 라이선스 / 사용처**를 기록합니다 (크레딧용).

### 4.1 플러그인
- [x] FishNet
- [x] ECM2
- [ ] Steamworks.NET
- [ ] FishySteamworks
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
