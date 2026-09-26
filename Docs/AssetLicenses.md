# 사용 에셋 및 라이선스

게임 내 크레딧(`Assets/_Project/Resources/Credits/credits.<언어 코드>.txt`)의 "이용한 에셋" 목록은 이 문서를 기준으로 관리합니다.
에셋을 추가하거나 제거할 때는 **이 문서와 모든 언어의 크레딧 파일을 함께 갱신**하세요.

| 에셋 | 제작 | 버전 | 라이선스 | 위치 | 비고 |
|---|---|---|---|---|---|
| FishNet: Networking Evolved | FirstGearGames | 4.7.3 | FishNet License (`Assets/FishNet/LICENSE.txt`) | `Assets/FishNet` | 네트워크 |
| FishySteamworks | FirstGearGames | 4.1.1 | MIT License | `Assets/FishNet/Plugins/FishySteamworks` | FishNet용 Steam 트랜스포트 |
| Steamworks.NET | Riley Labrecque | 2025.164.1 | MIT License (`Assets/com.rlabrecque.steamworks.net/LICENSE.md`) | `Assets/com.rlabrecque.steamworks.net` | Steam API 래퍼 |
| Easy Character Movement 2 | Oscar Gracián | 1.4.3 | Unity Asset Store EULA | `Assets/ECM2` | **유료 에셋. 저장소는 비공개로 유지** |
| DOTween | Demigiant | - | DOTween License (`Assets/Plugins/Demigiant/DOTween/readme.txt`) | `Assets/Plugins/Demigiant` | 현재 코드에서 미사용 |
| Universal Render Pipeline, Input System, AI Navigation, uGUI | Unity Technologies | 패키지 | Unity Companion License | Package Manager | 엔진 패키지 |

## 사운드 / 그래픽
- 현재 모든 사운드는 코드로 만든 **임시 합성음**(`ProceduralNoiseClips`), 모든 모델은 Unity 기본 도형입니다. 외부 에셋이 없습니다.
- 사운드·모델·폰트 에셋을 추가하면 위 표와 크레딧 파일에 추가하세요. (특히 폰트는 게임 배포 허용 여부를 확인)

## 주의
- ECM2는 Unity Asset Store EULA에 따라 **저장소를 공개로 전환할 경우 반드시 제외**해야 합니다.
- FishNet은 자체 라이선스(`Assets/FishNet/LICENSE.txt`)를 따릅니다. 출시 전에 원문의 재배포·사용 조건을 확인하세요.
