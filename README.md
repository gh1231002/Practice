# Practice

Unity로 제작한 3D 액션 RPG 프로젝트입니다. 플레이어 전투와 무기 장비, 몬스터 및 보스 전투, NPC 퀘스트, 인벤토리와 상점 등 RPG의 주요 시스템을 구현했습니다.

## 프로젝트 정보

- **Unity 버전:** 6000.3.9f1 (Unity 6)
- **개발 언어:** C#
- **장르:** 3D 액션 RPG
- **주요 기술 및 패키지:** Unity Input System, Cinemachine, AI Navigation, Timeline, Universal Render Pipeline (URP), TextMeshPro, ScriptableObject

## 주요 기능

### 플레이어 조작 및 전투

- 이동, 걷기, 점프, 회피, 웅크리기 등 기본 액션
- 무기 타입에 따른 공격 및 애니메이션 컨트롤러 전환
- 피격, 넉백, 무적 시간, 슈퍼 아머와 체력 처리
- Cinemachine 기반 카메라 입력 및 타깃 연결

관련 스크립트: `Player_CC.cs`, `WeaponController.cs`, `CinemachineInputController.cs`, `CinemachineTargetAssigner.cs`

### 무기, 장비 및 아이템

- 무기 해금과 장착, 장착 상태 전환
- ScriptableObject 기반 아이템·무기·포션 데이터 관리
- 인벤토리 아이템 표시, 툴팁, 드래그 앤 드롭
- 퀵슬롯에 아이템을 배치하고 단축키로 사용
- 상점 UI를 통한 아이템 구매

관련 스크립트: `EquipmentSystem.cs`, `PlayerInventory.cs`, `WeaponData.cs`, `ItemData.cs`, `PotionData.cs`, `InventoryUi.cs`, `QuickSlotManager.cs`, `ShopUi.cs`

### 몬스터 및 보스 전투

- 몬스터 감지, 순찰, 추적, 공격과 피격 처리
- NavMeshAgent를 이용한 몬스터 이동
- 보스 상태 관리와 복수 공격 패턴
- 공격 범위 표시와 공격 판정, 보스 체력 UI
- 보스 진입 컷신 및 전투 시작 처리

관련 스크립트: `MonsterAIController.cs`, `MonsterDetect.cs`, `MonsterCombat.cs`, `MonsterStats.cs`, `BossAIController.cs`, `BossCombat.cs`, `BossCutsceneManager.cs`

### NPC, 대화 및 퀘스트

- NPC 상호작용과 대화 진행
- 선택지 대화와 ScriptableObject 기반 대화·퀘스트 데이터
- 퀘스트 수락, 진행도 갱신, 완료 및 보상 처리
- 퀘스트 목표 추적 UI

관련 스크립트: `InteractNpc.cs`, `InteractSensor.cs`, `QuestManager.cs`, `QuestData.cs`, `DialogueData.cs`, `QuestUi.cs`, `RewardManager.cs`

### 게임 UI 및 월드 기능

- 플레이어·몬스터·보스 체력 UI와 데미지 표시
- 캐릭터 정보, 인벤토리, 상점, 텔레포트, 퀵슬롯 UI
- 낮과 밤에 따른 조명·하늘 변화
- 마을, 묘지, 보스 지역 간 씬 이동과 로딩 화면
- 미니맵 카메라 및 보스 전투 연출

관련 스크립트: `UiManager.cs`, `DayNightCycle.cs`, `LoadingSceneManager.cs`, `Portal.cs`, `MiniMapCameraManager.cs`, `DamageTextManager.cs`

## 조작 방법

| 동작 | 키 |
|---|---|
| 이동 | `W` `A` `S` `D` 또는 방향키 |
| 카메라 회전 | 마우스 이동 |
| 기본 공격 | 마우스 왼쪽 버튼 |
| 보조 공격 | 마우스 오른쪽 버튼 |
| 점프 | `Space` |
| 회피 | `Left Shift` |
| 걷기 | `Left Ctrl` |
| 웅크리기 | `C` |
| 상호작용 | `E` |
| 전투 상태 전환 | `Tab` |
| 캐릭터 정보 | `P` |
| 인벤토리 | `I` |
| 상점 | `B` |
| 퀵슬롯 | `1` `2` `3` `4` |
| 커서 표시 전환 | `Left Alt` |

입력은 프로젝트의 Input System 액션 맵에 설정된 기본 키 기준입니다. 일부 입력은 게임패드 바인딩도 포함합니다.

## 실행 방법

1. 저장소를 클론합니다.

   ```bash
   git clone https://github.com/gh1231002/Practice.git
   ```

2. Unity Hub에서 클론한 프로젝트 폴더를 엽니다.
3. Unity `6000.3.9f1` 버전을 선택해 프로젝트를 실행합니다.
4. Build Settings에 등록된 시작 씬 `Assets/Resources/Scenes/Town.unity`를 엽니다.
5. 에디터에서 Play 버튼을 눌러 플레이합니다.

## 프로젝트 구조

```text
Assets/Scripts/
├── Animation/  # 애니메이션 상태 동작 및 Root Motion 처리
├── Enum/       # 퀘스트, 몬스터, 보스 등 게임 열거형
├── Manager/    # UI, 퀘스트, 보상, 컷신, 로딩, 카메라 관리
├── Monster/    # 몬스터·보스 AI, 감지, 능력치 및 전투
├── Npc/        # NPC 순찰 및 상호작용
├── SoData/     # ScriptableObject 데이터 에셋
├── SoScript/   # 아이템, 무기, 대화, 퀘스트 데이터 정의
└── Ui/         # 인벤토리, 퀘스트, 상점, 체력 등 UI
```

## 빌드 씬

- `Assets/Resources/Scenes/Town.unity` — 시작 마을
- `Assets/Resources/Scenes/Loading.unity` — 씬 전환 로딩
- `Assets/Resources/Scenes/Grave.unity` — 묘지 지역
- `Assets/Resources/Scenes/Boss.unity` — 보스 지역

## 저장소

[GitHub 저장소](https://github.com/gh1231002/Practice)
