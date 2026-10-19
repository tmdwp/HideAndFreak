> **공개 브랜치 안내 (`public`)**
> 이 브랜치에는 직접 만든 코드·에셋(`Assets/_Project`, 문서, 프로젝트 설정)과 공개 라이선스 라이브러리(FishNet, Steamworks.NET)만 들어 있습니다.
> Unity 에셋 스토어 등 재배포할 수 없는 외부 에셋(Easy Character Movement 2, 맵·캐릭터·사운드·이펙트 팩, Mixamo 애니메이션, Ghoul 모델 등)은 제외했습니다.
> 따라서 이 브랜치만으로는 컴파일·실행되지 않으며, 사용한 에셋 목록은 [Docs/AssetLicenses.md](Docs/AssetLicenses.md)를 참고해 각자 구입·임포트해야 합니다.

# Hide&Freak

(개발 코드 이름: 가드 vs 마네킹 / GuardVsMannequin)

시설을 지키는 **가드**와 시설을 탈출하려는 **마네킹**의 비대칭 멀티플레이 게임입니다.
모티브: 메챠 카멜레온 + 데드 바이 데이라이트

- 인원: 2~10명 (가드는 최대 인원의 절반)
- 마네킹은 가짜 마네킹 사이에 섞이거나 사물로 변신해 숨고, 전원 장치를 파괴해 **사냥 시간**을 여는 것이 목표입니다.
- 가드는 총과 수류탄(맞은 마네킹의 위치를 외곽선으로 드러내고 느리게 함)으로 마네킹을 찾아 제거합니다. 사냥 시간에는 조명이 꺼지고 공수가 뒤바뀝니다.

## 개발 환경
| 항목 | 내용 |
|---|---|
| 엔진 | Unity 6000.4.2f1, URP (Forward+) |
| 네트워크 | FishNet 4.7.3 (개발: Tugboat / 출시 예정: FishySteamworks) |
| 캐릭터 | Easy Character Movement 2 (ECM2) |
| 입력 | Input System |
| AI | AI Navigation (런타임 NavMesh) |
| Steam | Steamworks.NET (개발 중 AppID 480) |

## 진행 상황
자세한 계획과 구현 메모는 [Docs/DevelopmentPlan.md](Docs/DevelopmentPlan.md)를 참고하세요.

| Phase | 내용 | 상태 |
|---|---|---|
| 0 | 네트워크 기반 (접속, 역할 배정, 이동, 상태 머신) | ✅ 검증 완료 |
| 1 | 핵심 루프 (대기실 문, 변신, 사격, 체력, 전원 장치, 승패, HUD) | ✅ 검증 완료 |
| 2 | 속이기와 탐지 (감정 표현, 가짜 마네킹 AI, 소음, 탐지기, 벽타기, 변신 회전) | ✅ 검증 완료 |
| 3 | 사냥 시간 (조명 소등, 손전등, 마네킹 1인칭·공격·제한 시야) | ✅ 검증 완료 |
| 4 | 관전 모드, 저체력 시야, 결과 화면 | ✅ 검증 완료 |
| 5 | Steam 로비 / 룸 씬 (방 만들기·코드·초대, 준비, 호스트 설정) | 🧪 테스트 중 |
| 6 | 설정(언어·키·감도·음량), ESC 메뉴, 크레딧, uGUI HUD, 다국어(한/영), 이동 속도 설정 / 정식 맵·밸런스는 예정 | 🧪 테스트 중 |
| 7-A | 게임플레이 개편: 가드 대기 시간 제거(시작 준비 3초), 가드 무작위 스폰·출발 보호, 탐지기 → 수류탄(외곽선·감속), 공용 외곽선 | 🧪 테스트 중 |

## 테스트 방법
1. Unity에서 프로젝트를 엽니다.
2. 메뉴 **GuardVsMannequin → 테스트 환경 생성**을 실행합니다. 로비/룸/게임 씬, 프리팹, 데이터 에셋이 만들어지고 빌드 순서(Lobby → Room → Game_Systems → 맵 씬)가 설정됩니다. 테스트 씬 Game_Test는 게임에 넣지 않습니다(모드 SDK 시험용).
3. **`Lobby` 씬에서 Play** 하거나 빌드를 실행합니다. (게임 씬에는 NetworkManager가 없어 단독 실행되지 않습니다)
4. 한 PC에서 테스트: 한쪽은 **직접 방 만들기**, 다른 쪽은 **IP로 참가**(비우면 localhost)를 누릅니다.
   Steam 테스트: PC 2대(Steam 계정 2개)에서 호스트가 **방 만들기** → 게스트가 **방 코드** 입력 또는 친구 초대 수락.
5. 룸에서 게스트는 **준비**, 호스트는 설정을 조정한 뒤 **게임 시작**을 누릅니다. 결과 후 룸으로 돌아옵니다.

> 네트워크 오브젝트나 프리팹이 바뀌면 호스트와 게스트가 반드시 같은 빌드여야 합니다.

## 조작
기본 키입니다. 로비의 **설정** 또는 게임 중 **ESC → 설정**에서 바꿀 수 있습니다.

| 키 | 가드 | 마네킹 |
|---|---|---|
| WASD / 마우스 | 이동 / 시점 (1인칭) | 이동 / 시점 (3인칭) |
| 좌클릭 | 사격 | 전원 장치 파괴 (유지) / 사냥 시간: 공격 |
| 우클릭 | 수류탄 | 변신 / 해제 |
| Shift | 달리기 (스태미나 소모) | 달리기 (스태미나 소모) |
| Space | 점프 | 점프 |
| Q / E | – | 변신 중 좌우 회전 |
| 1~6 | – | 감정 표현 |
| E | 손전등 (사냥 시간) | – |
| ESC | 메뉴 (계속하기 / 설정 / 방 나가기) | 메뉴 (계속하기 / 설정 / 방 나가기) |
| ← / → | 관전 대상 전환 (사망 후) | 관전 대상 전환 (사망 후) |

## 폴더 구조
```
Assets/_Project/     게임 코드와 에셋 (Scripts, Editor, Input, Prefabs, Scenes, ScriptableObjects)
Assets/_Project/Resources/   메모장·엑셀로 편집하는 데이터 파일
    Localization/strings.csv        화면 문장 (열 = key, ko, en … — 열을 추가하면 언어가 늘어남)
    Credits/credits.<언어>.txt      크레딧
    Data/default_game_settings.json 게임 설정 기본값
    Data/game_settings.csv          룸 설정 항목의 순서·조정 단위·범위
    Data/game_tuning.csv            밸런스 수치 (이동 배율, 스태미나, 시야, 쿨타임 등 — id,value,설명)
Docs/                개발 계획서
Assets/FishNet, ECM2, com.rlabrecque.steamworks.net, Plugins   서드파티
```

## 서드파티 에셋 라이선스
- **ECM2는 유료 에셋(Unity Asset Store)** 입니다. 이 저장소는 비공개로 유지해야 하며, 공개로 전환할 경우 ECM2 폴더를 저장소에서 제외해야 합니다.
- FishNet, Steamworks.NET, DOTween은 각 라이선스를 따릅니다.
