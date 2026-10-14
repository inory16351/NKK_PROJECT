# 찍!찍!!찍!!! — 유니티 이식 인수인계 (요약본)

웹게임 `Proto_Game/rat-uprising.html`(약 1만 줄 JS)을 유니티로 옮기는 작업. 이 문서는 **지금 상태와 다음 할 일**만 정리한 요약본입니다.
날짜별 상세 기록(측정 결과·결정 경위·옛 수치)은 `HANDOFF_LOG.md` (§9-1 ~ 9-22).
마지막 갱신: 2026-10-15 새벽 — **§5-9 (itch.io 페이지·크롬 작업 남음)** · 2026-10-13 저녁 — **§5-8 (브랜치 역할 · MVP 서버 · 다음 = WebGL 빌드) 부터 읽을 것** / (튜토리얼 UI 크기 0 수정 §5-8 · 뒷골목 숨김·조각 1개 · **스트링 테이블 + 영어 번역** §5-5) / 이전: 2026-10-12 밤 §5-4

---

## 0. 꼭 지킬 규칙 (사용자 지시)

- **설명은 항상 한국어** (코드 주석도). 게임 이름 **"찍!찍!!찍!!!"**. PC(Windows) · 기준 1920×1080. 씬 `Title.unity`(빌드 0, 2026-10-09 추가) → `Lobby.unity`(빌드 1) → `Game.unity`(빌드 2), `SampleScene` 에는 아무것도 넣지 말 것.
- **결정할 게 생기면 바로 사용자에게 질문**하고 진행.
- **수치는 엑셀 테이블**(`Data_Table/`, 3장 형식). 고친 뒤 `python Tools/xlsx2json.py`.
- **모든 오브젝트는 하이라키에 직접**(MCP 로 씬에 생성, 인스펙터에서 값 조정). C# 빌더 스크립트로 씬을 만들지 말 것.
- **UI 글자는 코드에 넣지 말 것**: 씬 TMP 글 또는 인스펙터 문자열, 바뀌는 값만 `{n}` `{floor}` 같은 자리표시를 코드가 채움. 내부 id 는 화면에 안 보이게.
- **그림은 Codex CLI 로만 생성** (코드로 그리기 금지, 자르기·키잉은 됨). 한 장에 여러 개 그려 잘라 쓰기. 먼저 `UnityResources/Rats/` 에 쓸 만한 그림이 있는지 찾아 재사용. **실사 다람쥐 `rsp_*` 금지.**
  - 실행 (codex.sh 는 이 PC 사용자 폴더에서 codex.exe 를 찾음): `bash Tools/codex.sh exec --skip-git-repo-check --ephemeral -s workspace-write -C <폴더> -i <참고.png> < prompt.txt` (자홍 #FF00FF 배경 → `Tools/slice_rat_parts.py` 의 `key_magenta` 로 키잉)
  - 화풍: 게임 안 = **Untitled Goose Game 식 플랫**(외곽선·광택·그라데이션 없음, 참고 `UnityResources/Rats/UI_Kit/_미리보기.png`), 로비 소품 = 로비 배경(`UI/lobby_bg.png`) 그림체.
  - 만든 그림은 `UnityResources/Rats/<분류>/` 에 PNG + `Sheets/`(원본·프롬프트·로그) + README 로 보관 후 `Assets/Art/...` 에 복사.
- **Unity 테스트는 짧게, 끝나면 Play 바로 멈추기.** 몇 분 걸리는 BalanceProbe 측정은 사용자가 원할 때만, 미리 시간을 말하고 (2026-10-08 사용자: "유니티가 계속 돌아가고 있었음").
- **사용자가 에디터에서 테스트 중이면 Unity 건드리지 말 것.**
- 정해진 게임 규칙 (사용자 결정): 특수 능력(패시브) 처음부터 켜짐 · 특수 액션 = 쳇바퀴 훈련 Lv 3 · 필살기 = Lv 7 · 공용 묘기는 공용 스킬로 해금 · 판 시작 쥐 = 티어 테이블 6~12 + 시작 쥐 노드(최대 +12) · 특별 고양이(마녀·우주복)와 고양이 보스는 둘 다 유지.

---

## 1. 폴더

| 경로 | 내용 |
|---|---|
| `Proto_Game/rat-uprising.html` | 원본 웹게임 (로직 참고) |
| `UnityResources/Rats/` | 그림 원본 (분류별 폴더 · README) |
| `Data_Table/*.xlsx` | 데이터 테이블 원본 (`_backup_YYYYMMDD/` = 고치기 전 백업) |
| `Tools/` | `xlsx2json.py`(엑셀→JSON, 자료형 틀린 칸은 위치 알려 주고 그 JSON 안 씀) · `gen_stage_table.py`(Stage 시트 생성) · `gen_skill_tree.py`(공용 스킬 트리 생성) · `update_spu.py`(측정 → SPU 보정) · `slice_*.py`(시트 자르기) · `codex.sh` · `probe_results/`(측정 CSV) |
| `Unity_Making/NKK_Project/` | 유니티 프로젝트 (Unity 6000.3.19f1, URP) |
| `HANDOFF_LOG.md` | 날짜별 상세 기록 |

유니티 안: `Assets/Art/`(게임 그림, `Rats/` 아래 분류별 · `UI_Kit` · `HUD_Gauge` · `FX_CatWarn` · `FX_Heart` · `Stage` …) · `Assets/Data/`(테이블 JSON + `RatArtLibrary`·`CatArtLibrary`·`HumanArtLibrary`) · `Assets/Fonts/`(Jua = 제목·팝업·버튼, Gowun Dodum = 긴 글, 이모지 없음 → 쓰지 말 것) · `Assets/Prefabs/` · `Assets/Scripts/`.
에디터 메뉴 `NKK/`: Build Rat/Cat/Human Art Library · Bake Font (Jua) · **Add Missing Chars (연결 유지)** · Apply UI Kit Borders · Reimport Art Sprites.
- **폰트 글자 추가는 `NKK/Add Missing Chars (연결 유지)`** (charset_ko.txt 에 있는데 구운 SDF 에 없는 글자만 덧붙임). `Bake Font(s)` 는 에셋을 지우고 다시 만들어 씬·머티리얼 연결이 끊기니 쓰지 말 것. 구운 폰트에 없는 글자가 화면에 나오면 `Jua-Regular Dynamic SDF` 가 Play 중 글자를 만들어 파일이 바뀜 (그래서 git 에 뜸).
`Assets/_Recovery/0 (1).unity` = 유니티 복구 씬 (필요 없음, 커밋 안 함).

---

## 2. 유니티 MCP 요령

- 패키지 `com.coplaydev.unity-mcp` (`#v10.3.0` 고정), 인스턴스 `NKK_Project`. 끊기면 에디터 창을 한 번 클릭.
- `execute_code` 는 CodeDom(C# 6): 로컬 함수·`out var`·튜플 해체 안 됨 → `System.Func`, `UnityEngine.Object` 로 명시.
- **스크립트 고친 뒤 컴파일은 Play 밖에서** (Play 중 도메인 리로드 → GameDatabase null·StunStars 에러 대량).
- MCP 가 명령을 두 번 보낼 때가 있음 (`Suppressed duplicate command`) → 상태를 바꾸는 코드는 두 번 실행돼도 괜찮게.
- 에디터가 뒤에 있으면 Game 화면 캡처가 몇 프레임 늦거나 게임이 느림 → 상태는 코드로 읽어서 확인, 캡처는 참고만.
- 씬을 바꿀 땐 `EditorSceneManager.SaveScene` 후 `OpenScene` (저장 안 된 변경이 있으면 load 가 거부됨).

---

## 3. 데이터 테이블 (`Data_Table/` → `Assets/Data/*.json`)

형식: 1행 한글명 / 2행 키 / 3행 자료형(int·float·string·enum, `-` = 설명 칸) / 4행~ 데이터. 효과 = `effect_type` + `value_01~06`(의미는 같은 파일 타입 정의 시트), 조건 = `cond1_type` + 값. 공통 로직은 캐릭터 칸에 넣지 않음.

| 파일 | 주요 시트 |
|---|---|
| 쥐 캐릭터 | Character 79 · Skill 158(패시브+액션) · Ultimate 33 · Ult_Caption · Ult_Charge |
| 쥐 등급 · 쥐 성장 | Grade(크기·탄생 가중치·skill_power·승급 promote_*) / Growth_Node · Growth_Order · Action_Awaken |
| 고양이 캐릭터 | Character 10 (`skill` 품종 스킬 · **`crowd_skill` 무리 스킬**, life_time 안 씀) · Skill 20 (50001~ 품종, **50011~ 무리 `Crowd_*`**) · Effect_Type |
| 물건 | Item (물건·가구·쏟아짐 Spill 16) · Zone 5 · Furniture_Layout |
| 티어 | Tier 8 (연구자료 비용 · 조건 Max_Floor · Shard_Level_Sum · **Skill_Tree_Complete** · 시작 쥐 · 탄생 배율) |
| 사람 | Human 4 · Human_Line |
| 공용 스킬 | Common_Skill (훈장 8 × 트리, `gen_skill_tree.py` 로 생성) · Branch · Common_Effect_Type |
| 업적 | Achievement 33 · Achv_Cond_Type |
| 스테이지 | Stage 1~50층 (`gen_stage_table.py` 생성: 방 수·적정 전투력·물건 체력·치즈·wall_path·추가 시간) |
| **튜토리얼** | Speaker 4 (해설 쥐) · Step 18 (발동·조건·해금·행동) · Line 47 · Story 12 (오프닝 초안) · 설명 시트 Trigger_Type · Cond_Type · Unlock_Id · Action_Type |
| **보스** | Boss 6 (공격·두 번째 공격·체력 hp_pow_sec·special1/2_type·special_chance·**skill_type·skill_cd·skill_first**) · Boss_Line (Intro/Attack/Hit/Down/Special/**Skill**) · Atk_Type 29 (반경·기절·개수·웅크림·길이·화면 이름) |

새 테이블: `xlsx2json.py NAMES` 추가 → `Data/TableRows.cs` 행 클래스 → `Data/GameDatabase.cs` 로드.

---

## 4. 코드 구조 (`Assets/Scripts/`)

좌표: 로직은 웹과 같은 게임 단위(x 오른쪽, y 아래, z 높이, 방 1280×800), 화면에 놓을 때만 `World.ToUnity` (1유닛 = 100, TILT 0.85).

| 폴더 | 주요 파일 |
|---|---|
| `Core/` | `GameManager`(층·치즈·콤보·HUD·배너·페이드) · `CameraController`(줌·끌기·흔들림·**Peek 미리보기**) · `FxManager`(파티클·팝업·고리·얼룩·코인·체력바·레이저·번개·**하트**) · `Progress`(저장·스킬·훈장·업적) · `CommonSkill`(공용 스킬 효과 합) · `RunTimer` · `GameOver` · `Heist`(층 탈취 연출) · `Research` · `AchievementToast` · `BalanceProbe`(측정) · `QuitMenu` |
| `Stage/` | `StageManager`(랜덤 지형·벽·계단·쥐덫·고양이 등장·**Climb/TestClear**) · `Room` · `WallBar` · `WallFx`(벽 폭파) · **`Minimap`** |
| `Rats/` | `Rat`(+ `.Passive` `.Trick` `.Action` `.Ult` `.Mount`) · `RatManager`(번식·총공격·승급) · `RatRig` · `PromotePanel` · **`PopCounter`** |
| `Hazards/` | `Boss`(대기·전투·공격 10종·필살 패턴 12종·**쿨타임 스킬 6종**) · `Cat`(품종 스킬 + **무리 스킬**) · `CatManager`(등장·**무리 찾기·경고 원·낙하물·블랙홀**) · `Trap` |
| `Items/` · `Humans/` | 물건 물리·생성·택배·운석, 사람 생성(`ItemManager`) / 사람 리그·행동 |
| `Ults/` | `UltimateManager` · `UltBase` + 필살기 33종 `Ult*.cs` · `SuperJumpManager` · `UltTestPanel`(테스트 패널) |
| `Lobby/` | `LobbyManager`(페이지·출발) · `LobbyHome`(아지트) · `RunPage`(작전 회의) · `RankPage`(찍찍!! 훈장·업적) · `TrainPage`(쳇바퀴 훈련) · `SkillPage`(치즈 창고 = 공용 스킬 트리) · `RatTreePopup` · `RatPortrait` · `IconBook` |

---

## 5. 지금 게임이 돌아가는 방식 (요약)

**판 흐름**: 로비 작전 회의에서 시작 층(1 + 스테이지 스킵 노드) → 층마다 판 시드 랜덤 지형(방 최대 9, 계단 = 거리 4 이내 가장 먼 방, 벽 체력은 경로 합 wall_path 를 나눔) → 벽을 갉아 방 확장 → 계단 방 열고 계단에 닿으면 탈취 연출(연구자료 = 8 × 1.25^(층-1), 보스 층 ×3) → 다음 층. 제한시간 180초(보스 층 +30, 노드로 늘어남), 0초면 게임 오버(경비원·고양이 습격 → 결과 창 → 로비).

**보스** (5층마다, **30층이 마지막** — 2026-10-12 사용자: 30층 보스를 잡고 계단에 닿으면 탈취 연출 뒤 저장하고 로비로. `StageManager.lastFloor` · 작전 회의 층 길·시작 층도 30까지): 계단 방에서 대기(어두운 방에 실루엣 + 층 진입 때 카메라가 잠깐 비춤) → 계단 방이 열리면 전투. **보스를 잡기 전엔 어떤 경로로도 계단 못 씀** (`Boss.FloorCleared`, 보스가 사라졌으면 다시 불러 전투). 테스트 "층 클리어" 버튼도 보스가 있으면 보스전을 시작함. 체력 70%·35% 에서 필살 패턴, 그 뒤 25% 확률로 섞어 씀. 로그 `[Boss]` (Editor.log 검색).

| 보스 | 일반 공격 (두 번째 40%) | 쿨타임 스킬 (12초, 밈) | 발악(필살) 1 · 2 (체력 70% · 35%, 밈) |
|---|---|---|---|
| 5 경비대장 | 내려찍기 · 진압봉 | 퉁퉁퉁 사후르 | 무야호 3단 점프 · 관짝소년단 |
| 10 광기의 수석 연구원 | 플라스크 · 독가스 구름 | 이븐하게 익혀드릴게요 (불판) | 약품 대방출 파티 · 오히려 좋아 (거대화) |
| 15 연구소장 | 휘두르기 + 경비원 · 서류 가방 | 중꺾마 (피해 감소) | 전액 현금 매입 · 긴급 비상 회의 |
| 20 실험체 제로(메인쿤) | 덮치기 · 헤어볼 | 해피해피해피 (점프) | 한강 고양이 질주 · 바나나 고양이 |
| 25 대마법사 마녀 고양이 | 불덩이 · 덮치기 | 무한동력 버터 고양이 | 트랄랄레로 (상어 머리) · 투명 망토 (해리포터) |
| 30 우주 고양이 사령관 | 무중력 · 덮치기 | 맥스웰 고양이 낙하 (착지 뒤 춤) | 홈랜더 눈 레이저 · STAY (블랙홀 → 컷씬 9종 랜덤) |

**고양이** (2층부터 35~60초 뒤, 그 뒤 55~85초 간격, 한 번에 한 마리, 보스전 중엔 안 나옴): **체력 0 까지 안 떠남**. 품종 스킬(하악질 등) + **무리 스킬**: 쥐가 가장 많이 모인 곳 → 바닥 빨간 경고 원 + 머리 위 느낌표 → 고양이마다 다른 범위 기절 (코숏 대폭격 · 러시안 블루 연타 · 턱시도 급소 · 페르시안 뱃살 · 먼치킨 볼링 · 벵갈 순간이동 · 스핑크스 레이저 · 메인쿤 지진 · 마녀 운석 · 우주복 블랙홀). 공통 수치 = CatManager 인스펙터 "무리 스킬 공통", `logCrowd` 로 발동 기록.

**사람** (`ItemManager`): 층 시작 1~2명 · 방이 열릴 때 1~2명 · 18~30초마다 보충, 돌아다니는 사람이 최소 수(2 + 층÷6)보다 적으면 2.5~4.5초 빠른 보충. 상한 min(12, 4 + 층×0.4).

**쥐**: 번식(확률 = 마리 수/최대 비율 곡선, 번식 때 하트) · 클릭 총공격(쿨 3초, 같은 대상 연타 감쇠) · 승급(필요 = 올림(1.5 × 1.3^k), 일괄 승급은 최대의 절반 남김) · 필살기(쥐 개체별 게이지, Lv 7 해금, 하나 끝난 뒤 쿨 25초) · 슈퍼 점프(초당 1/480 확률) · 묘기 5종(공용 스킬로 해금). 최대 마리 = 30 + 둥지 노드(최대 175).

**성장·경제**: 치즈(판에서 벌고 저장) · 연구자료 · 조각(쳇바퀴 훈련 Lv). 공용 스킬 = 훈장마다 트리 하나(치즈 + 연구자료). **훈장 승급 = 연구자료 + 최고 층 + 조각 합 + 지금 훈장까지 스킬 트리 전부**.

**HUD (Game 씬 `HUD`)**: 왼쪽 위 치즈·찍찍!!(전투력/적정)·테스트 패널 / 오른쪽 위 층·방 · 제한시간(초시계 + 나무 게이지) · 쥐 마릿수 / 가운데 위 보스 체력바(엠블럼·줄무늬·깎인 자국) / 아래 필살기 버튼 / 왼쪽 아래 승급 / **오른쪽 아래 미니맵**(열린 방·옆방·계단·보스·쥐 점·카메라 테두리).

**로비**: 아지트(쥐 생활·소품 버튼) · 작전 회의(층 길 가로 스크롤 · 구역·방 · 출동 멤버·등급 확률 · 출발) · 찍찍!! 훈장(사다리·조건·친구·업적) · 쳇바퀴 훈련 · 치즈 창고(스킬 트리) · 친구들!!(도감) · 낮잠 침대(저장하고 타이틀로).

**테스트 패널** (`HUD/TestPanel`, 출시 때 끄기): 필살기 고르기·발동 · 슈퍼 점프 · 층 클리어(보스 있으면 보스전 시작) · 게임 오버 · 저장값 초기화 · **전부 해제** (지금 슬롯: 튜토리얼·기능 전부 열림 · 마지막 층까지 클리어 · 최고 훈장 · 공용 스킬 노드 전부 · 치즈·연구자료 무제한(1e15, 써도 다시 채움, 저장 `infinite`) → 로비로. `Progress.UnlockEverything`, "저장값 초기화"로 원래대로) · **◀ 보스 ▶ + 보스 소환 · ◀ 기술 ▶ + 보스 기술 발동** (일반·두 번째·스킬·발악 아무거나 강제).

---

## 5-1. 튜토리얼 (2026-10-09, 진행 중) — 기획 `듀토리얼 계획.md`

- **사용자 결정**: 자동 저장 + 저장 슬롯 5개(낮잠 침대 = 저장하고 타이틀로) · 대사 중 게임 멈춤 + 클릭/스페이스로 넘김 · 해설 쥐 4마리(흰 실험쥐·안경 쥐·시궁쥐·슈퍼 생쥐) × 표정 4종 = Codex 새 그림 · 대사는 엑셀 `튜토리얼 테이블` · 오프닝 6~8컷(대본 초안 → 사용자 확인 → Codex) · 추가 안내: 고양이 첫 등장·계단/제한시간·첫 보스(슈퍼 생쥐가 "여기선 내가 도와주지!!" 하며 필살기 시연 → 필살기 설명)·필살기 처음 참 · 컷씬만 건너뛰기.
- **스위치 방식**: 단계마다 한 번 발동하면 저장 파일(슬롯)에 기록 → 그 슬롯에선 다시 안 뜸 (`Progress.TutoDone/MarkTuto`, 발동 순간 기록). 해금 기능도 슬롯별 (`Progress.IsUnlocked/Unlock`, 처음부터 열림 = run·rec). **튜토리얼 이전 저장(ver 0)은 다 본 것으로** (`tutoSkip`). 테스트 패널 "저장값 초기화" 뒤엔 튜토리얼이 처음부터 나옴. BalanceProbe 는 `SkipTutorial()`.
- **슬롯**: `Progress.Slot`(PlayerPrefs `nkk_slot`) · 슬롯 1 = 예전 키 `nkk_progress_v1`, 2~5 = `_s2`… · `UseSlot/NewGame/DeleteSlot/SlotSummary` (타이틀 화면용, 아직 타이틀 없음).
- **코드** `Scripts/Tutorial/`: `TutorialManager`(씬마다 하나, Step 을 sort 순으로 확인 → unlock → 대사 → action → After 단계) · `TutorialBox`(대사창 UI, unscaled) · `TutorialTarget`(강조 id) · `FeatureGate`(잠금: 숨기기/자물쇠/버튼 막기). 대사 중 정지 = `FxManager.Halt`(= Paused ∥ TutorialManager.Pausing). `RatManager.Births`, `LobbyManager.OpenPage` 는 잠긴 기능이면 안 열림.
- **씬**: Game `HUD/Tutorial`(Box·Highlight·Arrow) + 루트 `TutorialManager`(FeatureGate: promote·power·pop) · 강조 대상 promote/power/pop/timer/minimap/ult. Lobby `UI/Tutorial` + `TutorialManager` · 아지트 팻말·위 탭마다 자물쇠(`Lock`) + FeatureGate + 강조 `tab_<id>` · `research`.
- **그림**: `UnityResources/Rats/TutoPortraits/`(`Tools/slice_tuto_portraits.py`) · `TutoUI/`(ui_frame·ui_arrow·ui_lock, `slice_tuto_ui.py`, 9-슬라이스 = Apply UI Kit Borders).
- **확인함**: 첫 판 대사·HUD 숨김·시계 강조 · 로비 잠금·치즈 창고 해금·연구자료 강조 · 잠긴 페이지 안 열림 · 첫 보스 슈퍼 생쥐 필살기 → 설명.
- **타이틀 · 오프닝 (끝)**: `Title.unity` = `UI/Menu`(이어 하기 · 처음부터 · 끝내기) · `UI/Slots`(기록 1~5, 글 자리표시 {slot} {tier} {tierName} {floor} {runs} {time}) · `UI/Confirm`(덮어쓰기) · `UI/Story`(`StoryPlayer`: 컷 두 장 겹쳐 바꾸기 + 천천히 확대 · 자막 · 건너뛰기) + `TitleManager`. 처음부터 = `NewGame` → 오프닝 → runs+1 → 바로 1층 / 이어 하기 = `UseSlot` → 로비 (runs 0 이면 오프닝부터). 컷 그림 `UnityResources/Rats/Story/`(Codex 7장) → `Assets/Art/Rats/Story/`. 낮잠 침대(`Page_rec`) = "저장하고 타이틀로" 버튼 (`LobbyManager.saveExitButton/SaveAndExit`).
- **튜토리얼 가림 수정 (2026-10-12)**: 가리키는 곳(노란 테두리)이 대사 판·초상화에 가리면 대사창이 화면 위로 옮겨 감(`TutorialBox.dodge`) · 가리키는 UI 1.25배 확대(`targetZoom`, 대상별 `TutorialTarget.zoom`) · 첫 탄생 = 갓 태어난 쥐에 강조 id `baby`(Line 7·8) + 카메라가 비추며 확대(`TutorialManager.worldFocusZoom`) + 하트. 플레이 확인은 아직.
- **첫 판 추가 안내 (2026-10-12)**: `g_pop`(첫 판 최대 마릿수 도달 = 발동 PopFull → 인구수 표시 pop 해금) · 두 번째 판 `g_power` 는 power 만, pop 설명은 `g_power_pop`(After 쉼표 여러 단계 지원, pop 잠겨 있을 때만) · 첫 판 1층 시작 방 옆 방 하나 = 쉬운 벽(`StageManager.firstRunWallMul` 0.3, 감쇠 없음, 계단 쪽 방 우선) · `g_wall`(발동 RoomOpen, g_floor 뒤) "해냈다!! 어서 계단을 찾아서…". 플레이 확인은 아직.
- **슈퍼 점프 첫 안내 (2026-10-12)**: `g_superjump`(발동 SuperJump = `SuperJumpManager.Jumps`, 연출 끝난 뒤) — 신비한 힘이 깨어나 점프한다는 설정 4줄. 판 상관없이 처음 한 번.
- **다른 PC에서 보스 안 나옴 수정 (2026-10-12)**: `HumanArtLibrary` 클래스가 `HumanRig.cs` 안에 있어 에셋의 m_Script 가 비어 있었음(이 PC 는 캐시로 동작, 새 PC 는 "[Boss] 그림 없음") → `Humans/HumanArtLibrary.cs` 로 분리 + 에셋 연결. **ScriptableObject·MonoBehaviour 는 파일 이름 = 클래스 이름으로 둘 것.**
- **로비 간판 · 치즈 창고 따라 하기 (2026-10-12)**: 훈장 판자 오른쪽 1.2(제목 카드 피함) · 치즈 창고 팻말 0.2 아래(돈 표시줄 피함). 치즈 창고 = `l_skill_pick`(추천 스킬 = 열린 칸 중 가장 싼 것, 강조 `skill_next` 를 SkillPage 가 붙임) → `l_skill_buy`(SkillSelect, 강조 `skill_buy` = BuyButton) → `l_skill_done`(SkillBuy). 치즈 모자라면 `l_skill_poor` → 다음에 열 때 `l_skill_pick2`. `skill_` 강조는 다음 단계까지 유지(`holdPrefix`). Done·NotDone 쉼표 여러 개. 오프닝 컷씬 10초 자동 넘김(`StoryPlayer.autoNext`).
- **승급 주의 대사 (2026-10-12)**: 승급을 너무 많이 하면 쥐가 적어져 번식이 거의 안 된다는 주의 + '일괄 승급'은 번식용 쥐를 남겨 준다는 안내. `g_promote` 끝(Line 63~65) · 두 번째 판에서 승급을 처음 배우는 `g_power_promote_new` 끝(Line 66~67). 같은 뜻으로 Line 28(승급 다시 알림)·52(인구 꽉 참) 문구도 "번식할 쥐만 남기고" 로 맞춤. 소환 쥐(Summon 스킬 11종 = 임시 쥐 `temp` 6초, 필살기용 임시 쥐 15초)는 승급·인구·번식·전투력에서 빠짐 (확인함). 개별 승급은 2마리까지 줄일 수 있음(`CanPromote`), 일괄 승급은 `PromoteKeepCount` 만큼 남김.
- **25층 마녀 발악 2 → 해리포터 '투명 망토' (2026-10-12, 진행 중)**: 발악 두 개가 둘 다 머리 바꾸기(트랄랄레로 상어 머리 · 발레리나 카푸치나 커피잔)라 겹침 → 상어는 유지, `special2_type` 을 `Frog_Curse` → 새 `Invis_Cloak` 으로. 사용자 선택 (레비오우사는 30층 무중력과 겹쳐 뺌, 패트로눔·디멘터·폴리주스 분신 중 투명 망토).
  - 흐름: 망토가 위에서 내려와 덮음(windup) → 사라짐(`Hidden`: 피해 안 받음) → 쥐 무리 쪽으로 몰래 이동(반짝임·발자국 먼지만 살짝) → 나타나기 `cloakWarn` 초 전 경고 원 → 경고 원 자리에 나타나며 덮침(`SpecialHit`) + 망토를 벗어 던짐 → `cloakRest` 쉼 → Atk_Type count 만큼 반복.
  - **끝**: 그림 `UnityResources/Rats/BossProps/boss_cloak.png`(덮어쓰기) · `boss_cloak_fly.png`(벗어 던짐), 원본·프롬프트·로그 `BossProps/Sheets/invis_cloak/` → `Assets/Art/Rats/BossProps/` 복사. (Codex 가 저장을 못 해서 `~/.codex/generated_images/<세션>/` 에서 직접 복사함.) `Boss.cs`: 필드(cloakSprite·cloakFlySprite·cloakSize·cloakFlyTime·cloakAlpha·cloakSparkle·cloakMove·cloakWarn·cloakRest·cloakHidePopup·cloakShowPopup) · `Hidden` · `Damage` 에서 Hidden 이면 무시 · Spawn/EndSpecialFx 초기화 · BeginSpecial 에서 망토 표시 · UpdateSpecial `case "Invis_Cloak"`.
  - **끝 (2026-10-12 오후)**: `Boss.LateUpdate` 숨으면 반투명(cloakA)·그림자 · `PlaceCloak`(덮어쓰기 내려옴 / 벗어 던짐 날아감) · `ShowProp` 색 되돌림 · `Rat` 숨은 보스는 안 쫓고 안 부딪힘 · 보스 테이블 `Invis_Cloak` 행 + 95005 special2 + 96107 문구.
  - **남음**: 씬(§5-2 목록) · 테스트 패널로 마녀 '투명 망토' 확인 · `BossProps/README.md` 줄 추가.
- **사람 몸 전체 피격 (2026-10-13)**: 예전엔 발밑 원(반지름 26)만 맞음 (키 150) → 고양이와 같은 방식 `Human.Body` · `Human.BodyDist` (그림 범위, `ItemManager.humanBodyShrink` 0.12). 적용: 쥐 박치기(발밑 = 밀어냄, 몸 = 튕기기만, bite 쿨타임 그대로) · 날아온 물건 · 폭발(BlastActors·범위 피해) · 라이브 방송 총알 · 역병 분사 · 사람 몸 클릭 = 총공격 지점을 사람 발밑으로. Play 로 확인 (머리 쪽 점: 예전 112~146 떨어짐 → 0).
- **고양이 몸 전체 피격 (2026-10-12)**: `Cat.Body`(그림 범위 → 게임 좌표, `CatManager.bodyHitShrink`) + `Cat.BodyDist` — 쥐 박치기(발밑 원 = 밀어냄, 몸 = 튕기기만) · 날아온 물건 · BlastActors · 라이브 방송·역병 분사 · 고양이 몸 클릭 = 총공격 지점을 고양이로.
- **친구들 도감 (끝)**: Lobby `Page_dex` = 쳇바퀴 훈련 페이지 틀 복제 + `Lobby/DexPage.cs`. 쥐 79종 전부 (등급 순), 등급 칩 {n}/{total}. 만난 친구(`Progress.Seen`) = 그림·이름·등급·설명·쳇바퀴 Lv·특수 능력·특수 액션·필살기 / 못 만남 = 실루엣·'???' / 훈장 부족(unlock_rank) = 실루엣·'{tier}훈장부터'. 글 = `Page_dex/Words`. 첫 훈장 승급 뒤 튜토리얼 l_dex 로 해금.
- **잠긴 기능 반응 (끝)**: Lobby `LockedFeedback`(Tutorial/LockedFeedback.cs) — `LobbyManager.OnLocked` → 그 기능 자물쇠 흔들림 + 아래쪽 말풍선 `UI/LockToast`("아직 잠겨 있어요, 찍!" + 기능별 힌트 = 인스펙터). 위쪽 탭은 버튼을 막지 않고 OpenPage 가 막음.
- **문서**: 튜토리얼 기획서 pptx (정승운, 코어 루프 기획서 레이아웃) = `OneDrive/Desktop/문서용/20261009_프로젝트 NKK_튜토리얼 기획서_정승운.pptx` · 사운드 목록 초안 = `사운드 목록.md`.
- **배경음악 (2026-10-10)**: Gemini Pro 음악 만들기(Lyria, 연주곡 · 표준 3분)로 11곡 = `UnityResources/Audio/BGM/`(README) → `Assets/Audio/BGM/`(스트리밍 · Vorbis). 재생 = `Core/MusicManager`(씬 넘어가도 하나, 겹쳐 바꾸기, 튜토리얼 대사 · 포기 창 중 볼륨 줄임, 볼륨 PlayerPrefs `nkk_bgm_vol`) + 씬마다 `SceneMusic`(Title = BGM_Title, 오프닝 컷씬 중 BGM_Story_Epic · Lobby = BGM_Lobby · Game = 1층 Lab · 5층 Breeding · 10층 Pharma · 15층 Admin · 20층~ Director + 보스전 BGM_Boss · 30층 이상 보스 BGM_Boss_Final + 남은 시간 30초 이하 BGM_Hurry).
  - Gemini 음악: 내장 브라우저에서 사용자 로그인 → 도구 '음악 만들기' · 모드 Pro · 길이 표준 · 보컬 연주곡. 다운로드는 '트랙 다운로드 → 오디오만(MP3)', 받은 파일이 Downloads 에 `.tmp` 로 생김 (사라지기도 함) → 크기가 멈추면 바로 복사. 긴 프롬프트·특정 프롬프트는 '생성 중'에서 멈출 때가 있음 → 중지 후 짧게 다시.
- **효과음 (2026-10-10)**: `Tools/gen_sfx.py` 코드 합성 81개 = `UnityResources/Audio/SFX/`(README) → `Assets/Audio/SFX/`. 재생 = `Core/SfxManager`(씬마다, 이름으로 `SfxManager.Play/PlayAt`, 이름별 볼륨·간격·동시 수, 화면 밖 무시, UI 버튼 클릭 자동, 볼륨 `nkk_sfx_vol`, 목록 = 컴포넌트 메뉴 Fill From Folder). 게임 곳곳 연결(물건 타격/박살 재질별 · 콤보 · 치즈 · 번식 · 총공격 · 보스 · 고양이 · 벽 폭파 · 층 클리어 · 게임 오버 · 튜토리얼 · 로비 구매 등). 목소리 계열은 합성이라 어색 → 나중에 같은 이름으로 교체.
- **볼륨 설정 (2026-10-10)**: `Core/VolumeSettings`(배경음악 · 효과음 슬라이더, 숫자 글 {n}%, 효과음 움직이면 미리 듣기) = Game `HUD/QuitMenu/Panel/Card/Volume`(ESC 포기 창 아래) · Lobby 낮잠 침대 `Page_rec/Card/Volume` · Title '설정' 버튼 → `UI/Settings` 창 (`TitleManager.settingsButton/Panel/Close`).
- **남음**: 사용자 플레이 피드백 (대사 · 타이밍 · 소리 크기).

- **미니맵 점 가림 수정 (2026-10-12)**: 방이 새로 열려 나중에 복제된 방 칸이 쥐 점 위에 그려짐 (넓은 층일수록 잘 보임) → `Minimap.Get` 에서 새 칸은 칸 틀 바로 뒤에 끼움.

## 5-2. 새 쥐 6종 · 보스 기절 ×1.3 (2026-10-12) — 그림 `UnityResources/Rats/NewRats2/README.md`

- **사용자 결정**: 기획서 `캐릭터상세기획서모음.pptx` 중 게임에 없는 쥐 6종 추가 (그림은 참고만, 게임 그림체) · 폭탄마법사 쥐 → '폭탄마'(사용자 참고 그림: 안대·폭탄 조끼) · 폭탄 쥐 = 그냥 폭탄(얼굴 없음), 쥐포 = 다리 없는 쥐포 그대로 (둘 다 컴퓨터 마우스처럼 한 장짜리) · 새 6종만 웃긴 이름 · 폭탄마는 기획서대로(필살기 올인 + 반동 기절), 나머지 세부 수치는 임의 · 흰 갑주 필살기 없음 · 보스 기절 시간 전부 ×1.3 (보스 테이블 Atk_Type stun, 백업 `_backup_20261012`).
- 쥐 (이름은 2026-10-12 사용자가 엑셀에서 바꿈): 10080 폭탄마 쥐(bomber, 신화 t6, 425) · 10081 쥐4(jwipo, 에픽 t1) · 10082 고쳐쓰쥐(mechanic, 일반 t2) · 10083 제국군 쥐(trooper, 유니크 t4) · 10084 폭탄이쥐(bombrat, 에픽 t4) · 10085 맘스땃쥐(burger, 에픽 t2). 공격력 = 등급×훈장 기존 공식 값.
- 스킬 (쥐 테이블 Skill 20080~21085, Effect_Type 시트에 뜻): Ult_Power(필살기 ×2.5, 뒤 4초 기절) · Grill_Stack + Savory_Smell(물건 받는 피해 +30%, `Item.tenderT/tenderMul`) · Part_Collect + Tune_Up(가장 센 동료 공격력 +30% 15초) · Aim_Assist(70%, 빗나갈 때 +10%) + Blaster_Shot(3발, 다 맞으면 +50%) · Burning_Fuse(30초, 한 장짜리 그림 3단계) + Self_Destruct(쳇바퀴 훈련 없이, 막힌 벽으로 돌진해 폭발 후 사라짐 `RatManager.RemoveRat`) · Patty_Stack(5장, 두꺼워짐) + Patty_Slam.
- 필살기 30034 익스플로전 `Ults/UltExplosion.cs` (게이지 480, 자막 c1~c5, 소품 magic_circle · explosion_big, 업적 '폭발은 예술이다!').
- 코드: `Rats/Rat.NewRats.cs`(전부 여기) + 훅(Rat.Passive 공격력·부순 물건 · Rat.Tick · Rat.Action CanAct/Step/Pose/Begin · RatRig.tailLen · UltimateManager.UltDamage × UltPowerMul · RatManager 총알 align). EffectType 은 enum 끝에만 붙임. 유니티 없이 Roslyn 으로 컴파일 확인함 (에러 0).
- **씬 작업 1~4 끝 (2026-10-12 저녁, MCP)**: 아트 라이브러리 85종 · Game 씬 RatManager 팝업 글·그림 · FxManager 스티커 smell/explosion_big · UltimateManager Fill Props/Icons · Boss 망토 그림·팝업 · Lobby IconBook Fill Icons. **남음 = 5 (플레이 확인)**.
- (옛 목록)
  1. 메뉴 NKK/Build Rat Art Library (bomber·mechanic·trooper·burger 파츠 + 한 장짜리 bombrat·jwipo, 빌더에 추가함).
  2. Game 씬 RatManager '새 쥐 6종': 팝업 글(missPopup '빗나감!' · blasterAllHitPopup '전탄 명중!!' · tunePopup '튜닝 완료!' · pattyPopup '패티 +{n}' · pattySlamPopup '빅버거 박치기!!' · smellPopup '고소한 냄새~' · fusePopup '치익… 치익…' · selfDestructPopup '펑!!') · blasterBoltSprite = NewRats/blaster_bolt · pattySprite = patty · fuseSprites = bombrat_fuse1~3 · grilledSprite = jwipo_grilled.
  3. UltimateManager: Fill Props · Fill Icons. IconBook(로비·게임) Fill Icons. FxManager 스티커에 `smell` · `explosion_big` 추가.
  4. Boss 인스펙터 투명 망토 (§5-1).
  5. 플레이 확인: 테스트 패널로 새 쥐 소환 수단이 없으면 BalanceProbe/코드로 스폰해서 각 스킬 화면 확인. 쥐한폭탄 자폭으로 인구가 줄어드는 것 = 의도.

## 5-3. 허들 기획서 · 경제 · 상점 · 뽑기 (2026-10-12, 진행 중) — 기획서 `기획서/20261012_*_Ver02.pptx` 3개

- **사용자 결정**: 보스는 5층마다 그대로 + 난이도는 허들 기획서대로 · 층 건너뛰기 상한은 기획서 표(최고 클리어 층 → 최대 시작 층, 최대 15), 올리는 건 지금처럼 스킬 노드 · 훈장 조건 층 기획서에 맞춰 재배치 (클리어 5·8·10·13·15·17·19층 → Max_Floor 6·9·11·14·16·18·20) · 측정(BalanceProbe)은 나중에 · 30층 보스 = 끝 (로비로) · 체력바 깎인 자국은 계속 맞아도 따라 줄어듦 · 로비 아래 '지하 하수구'(맨홀 뚜껑으로 들어감)에 암시장·유전자 추출기 · 뽑기 연출 = 믹서기 ("다들 들어가!!! 빨리!!" → 갈갈갈 → 조각) · 상점에 브로커 쥐 상인 (플랫 그림체) · 상점·뽑기 튜토리얼.
- **난이도** `Tools/gen_stage_table.py` (기획서 수치): 층 유형(도입·쉬움·보통·어려움·허들) · 적정 찍찍 배율 (×1.25 / 5층 ×1.8 / 6~15 ×1.32 / 16 ×1.75 · 17 ×1.7 · 18 ×2.1 · 19 ×2.2 · 20 ×2.4, 21~30 = 16~20 반복) · 방 수 2→9 · 목표 돌파 108/135/162초 (보스 층은 그중 40% 가 보스전 → 보스 테이블 hp_pow_sec 비율로 줄임) · 제한 180초 통일 (RunTimer.bossExtra 0) · 새 칼럼 floor_type · target_sec · boss_sec · research(층 클리어 연구자료 = 8 × 난이도, Heist 가 읽음) · skip_cap · player_pow(기획서 기준 전투력, 측정 비교용).
- **경제** `Tools/gen_economy.py` (새): 기획서 판 일정(판별 최고 층)대로 번 연구자료·치즈로 훈장 승급 비용(티어 테이블)과 스킬 트리 비용(`Tools/economy_params.json` → gen_skill_tree.py)을 정함. 16층~ 은 뽑기·상점과 나눠 쓰게 50%. 순서: `gen_stage_table → gen_economy → gen_skill_tree → xlsx2json`. **플레이어 힘(공격력 성장)은 손대지 않음 → 측정으로 맞춰야 함.**
- **상점 입고 = 탈출 시간 15분** (사용자 2026-10-12: 실시간 30분 대신, RunTimer 가 판에서 보낸 시간만 셈 → `Progress.shop.playLeft`)
- **상점 (쥐구멍 암시장)** `Core/BlackMarket.cs` + 상점 테이블 (Shop_Config · Shop_Slot · Shop_Goods · Shop_Floor) · **뽑기 (유전자 추출기)** `Core/GeneGacha.cs` + 뽑기 테이블 (Gacha_Config · Gacha_Grade). 저장 = Progress.shop / gacha. 해금 = `Progress.IsUnlocked`: alley·gacha 16층 클리어, market 17층 (Progress 인스펙터 testMarket 로 테스트).
  - 아지트 맨홀 = `Home/Hots/Hot_alley` (2026-10-12, 아지트 바닥 **가운데** x 0). 그림 `lobby_manhole_closed/open.png` (LobbyPan.manhole 이 열림/닫힘 바꿈 + manholeOpenOffset). 팻말 TutorialTarget id = `alley_arrow` (튜토리얼 대사 69).
  - 하수구 방 `Alley` 는 씬에서도 아지트 바로 아래 (0, -10.24). 배경 `lm_bg.png` = **야시장 v3** (메이플 리버스 시티 느낌, 천장 맨홀 구멍 가운데) · 사다리(가운데) · 상자 · 올라가기 버튼 팻말. (랜턴은 꺼 둠, 옛 배경 `Sheets/bg_v3_old`)
  - **하수구 쥐**: `Alley/AlleyRats` = 두 번째 LobbyHome (fitCamera 끔, main = 아지트 Home, 자리(slot) 없음 → 걷기·수다·누르면 점프·끌어 던지기). 바닥 floorU 0.08~0.92 · floorV 0.655~0.69, 5마리, 대사는 인스펙터(야시장 대사). 물건 버튼은 어느 층이든 쥐를 먼저 누름 (`LobbyHome.AnyRatAt`).
  - **야시장 꾸미기** `Alley/Decor` (2026-10-12): 우유곽 집 · 깡통 가게 · 찻잔 점집 · 분식 노점 · 젓가락 문(사다리 앞) · 거꾸로 매달린 집 · 등불 줄 · 깃발 · 고양이 포스터 · 장물 더미 · 금고 · 자루. 저울 · 치즈 네온은 꺼 둠. 그림 `UnityResources/Rats/Lobby_Market/NightMarket/` (nm_* = 야시장 시트, md_* = 암시장 소품 시트).
  - **맘스땃쥐 새 그림** `UnityResources/Rats/NewRats2/Burger/` (윗빵=얼굴 · 치즈 · 양상추 · 토마토 · 패티 2종 · 아랫빵=발·꼬리 · 완성본). 쌓기 = 아래부터 아랫빵 · [고기 · 토마토 · 양상추 · 치즈] × (1 + 패티 수) · 윗빵, 미리보기 `_맘스땃쥐_쌓기_미리보기.png`. **게임 적용됨**: 한 장짜리 쥐 `Parody/rat_burger.png`(패티 0) + 미리 합친 `Art/Rats/NewRats/burger_stack0~5.png`(폭 같음·발끝 맞춤, 최대 크기 4096) → `RatManager.burgerStack`, `Rat.NewRatSprite` 가 패티 수로 바꿔 끼움 (박치기 → 0). 옛 파츠 `Parts/burger` 는 빌더가 건너뜀. 다시 합치기 = scratchpad 식 burger_stack.py (층 순서·겹침 값은 이 줄 참고).
  - 쥐 활동 자리(카드 놀이·낚시 등)는 놀이 소품(`prompt_market_decor.txt` ②) 오면.
  - **새 쥐 2종 (2026-10-12, `기획서/캐릭터상세기획서모음.pptx` 9·10쪽) — 로직만, 그림은 Codex 돌아오면**:
    - 10086 아이스 퀸 쥐 `icequeen` (신화 t5, 378): 패시브 20086 서리 축적 `Frost_Stack` (생명체 칠 때마다 빙결 스택: 스택당 6% 느려짐, 10이면 2초 꽁꽁, 4초 안 맞으면 풀림) · 액션 21086 얼음 미사일 `Ice_Missile` (20초, 고드름 5개 유도 → 때리던 생명체 > 가까운 생명체 > 물건) · 필살기 30035 렛잇고 `UltRatItGo` (게이지 300 — 사용자 "수치는 알아서" 2026-10-12, 기획서 2000 은 다른 필살기(100~480)와 단위가 안 맞아 조정. 맵 생명체 전부 꽁꽁 + 물건 서리 충격) · 업적 40035 Rat It Go!
    - 10087 5G `fiveg` (에픽 t5, 48 = 기획서 '낮음'): 패시브 20087 `Rush_Range_Up` (클릭 총공격 집결 범위 +0.3, 한 번만) · 액션 21087 데이터 전송 `Data_Boost` (주변 250에 5마리 이상, 15초마다 → 반경 쥐 공격력 +25% 8초)
    - 빙결 = `Rats/Frost.cs`, Cat/Boss/Human 에 `frost` (Tick 의 dt 에 배율, 꽁꽁 = 0). 얼음 미사일 = `RatManager.FireIce/UpdateIce`, 목표 공용 `FoePos/FoeHit`. 팝업 글 = RatManager 인스펙터 freezePopup · iceMissilePopup · dataPopup (Game 씬).
    - ~~임시 그림~~ → **그림 끝 (2026-10-12 밤, §5-4)**. 빌더의 빌려 쓰기 코드는 진짜 파츠가 있으면 건너뜀.
    - 기획서 맘스땃쥐 쪽은 지금 구현과 다름 (기획서: 물건 10개마다 패티 · 액티브 20초마다 벽 박치기) → 사용자 확인 전 그대로.
  - ~~믹서기 연출~~ → **비밀 실험실 컷씬으로 바꿈 (2026-10-12 밤, §5-4)**.
  - LobbyHome.FitCamera 는 카메라 x·크기만 (y 는 LobbyPan) — 예전엔 y 를 아지트로 되돌려서 하수구 클릭이 아지트 물건에 걸렸음.
- **로비**: `Lobby/LobbyPan`(하수구로 카메라 이동, 맨홀 = 물건 버튼 id alley → LobbyManager.OnHot) · `MarketBroker` · `MarketPage`(Page_market) · `GachaPage`(Page_gacha) · `GachaShow`(UI/GachaShow 연출). 씬: Lobby `Alley`(방) · `Home/Hots/Alley`(Hot_market · Hot_gacha) · `LobbyPan` · `UI/ToHome`.
- 튜토리얼 테이블: 화자 broker · 단계 l_alley · l_gacha_hot · l_gacha_page · l_market_lobby · l_market · l_market_page · l_wish (조건 WishOpen · 발동 AlleyEnter 추가).
- **플레이 확인함 (2026-10-12, 슬롯 5에서)**: 뽑기 화면 · 무료 10회 · 믹서기 연출(쥐들 뛰어듦 → 갈갈갈 → 조각 → 결과 10칸) · 암시장 라인업 · 첫 방문 선물. UI 겹침 정리 (연출 막 불투명 — 선형 색공간이라 반투명 검정이 옅게 보임 · 결과 칸/버튼 자리 · 등급 띠 = ui_bubble · 채우기 Image 는 그림이 있어야 동작).
- **주의**: 시험할 땐 `Progress.UseSlot(5)` 후 `PlayerPrefs nkk_slot` 을 1로 되돌릴 것. (2026-10-12 실수로 슬롯 1 에 테스트 값이 저장됨 → 튜토리얼 기록으로 훈장 1 · 최고 층 5 · tutoSkip false 로 되돌림, 치즈·연구자료는 원래 값을 몰라 0)
- **남음 (옛 목록, ①② 는 §5-4 에서 끝)**: ① Codex 한도(오후 6:34 이후) 풀리면 `UnityResources/Rats/Lobby_Market/Sheets/prompt_sewer.txt` · `prompt_manhole.txt` 다시 실행 → 하수구 배경(lm_bg 교체) · 맨홀(닫힘/열림) ② Lobby 씬: `Alley` 방을 아지트 아래(y -10.24)로 옮기고 노점·믹서기·브로커를 하수구 바닥(빈 자리 27% · 73%)에 · `Home/Hots` 에 Hot_alley(맨홀, id alley, FeatureGate alley, TutorialTarget alley_arrow) · LobbyPan.manhole/manholeClosed/manholeOpen 연결 ③ 튜토리얼 대사 l_alley · l_market_lobby 문구 '오른쪽 화살표' → '맨홀 뚜껑' 으로 ④ 밸런스 측정.

## 5-4. 아이스 퀸·5G 그림 · 5G 휴대폰 속 쥐 · 지하 도시 v4 · 뽑기 비밀 실험실 컷씬 (2026-10-12 밤)

- **아이스 퀸 쥐 · 5G 그림** (`UnityResources/Rats/NewRats3/README.md`): 기획서 그림은 참고만, 게임 쥐 플랫 그림체 (사용자). 파츠 `Parts/icequeen` · `Parts/fiveg`, 아이콘 rs_20086/21086/20087/21087 · ult_30035 · ach_40035, 소품 ice_castle · ice_missile 등 → `Art/Rats/NewRats/`. 아트 라이브러리 다시 만듦.
  - 씬 연결 끝: RatManager.iceMissileSprite = NewRats/ice_missile · UltimateManager Fill Props/Icons · AchievementToast Fill Icons (Game) · IconBook Fill Icons (Lobby).
- **5G = 휴대폰 속 쥐** (사용자): 겉 = 휴대폰 한 장짜리 `Parody/rat_fiveg.png` (다리·꼬리 없음, 컴퓨터 마우스 쥐처럼 움직임) · 화면 = `rat_fiveg_screen.png` + 화면 속 파란 5G 쥐 리그. `RatArtLibrary.Entry.screen/screenRect/screenBg` (빌더 `ScreenRectFiveG`) · `RatRig.screenRig/screenBg` (Rat·LobbyRat 프리팹 Single 아래 `ScreenBg` · `ScreenRat`) · UI 는 `RatPortrait` 가 화면 칸에 배경 + 쥐 복제본을 넣음.
  - 화면 속 쥐는 겉 자세(걷기·공격·행동·기절)를 따라 하고, 가만히 있으면 화면 안을 서성임. **멀리서 빠르게 달리다 멈추면 화면에 얼굴 쿵** (미끄러짐 → 납작 → 벌러덩 → 어질어질, `RatRig.bumpSpeed/bumpDist/bumpTime`, 효과음 hit_glass).
  - 크기 (사용자): 화면 속 쥐가 다른 쥐와 같은 길이가 되게 휴대폰을 키움 (`RatRig.Build` single 분기, 길이 = 화면 폭 × screenRatLen).
- **지하 도시 v4** (`UnityResources/Rats/Lobby_Market/README.md`): 로비 그림체 고정 (단순·무광, 질감 없음 — 사용자: 처음 뽑은 건물들은 로비와 안 맞음). 배경·기물·장식 새로, 같은 이름으로 덮어써 씬 연결 유지. 배치 = 보도 바닥 기준. 뽑기 기물 = **비밀 실험실 건물** `Alley/SecretLab` (팻말 '비밀 실험실', 뽑기 화면 제목은 그대로). 옛 Gate·매달린 집·금고·자루는 꺼 둠.
- **뽑기 연출 = 비밀 실험실 컷씬** (`UnityResources/Rats/GachaLab/README.md`, `Lobby/GachaShow.cs` 다시 씀, 씬 `UI/GachaShow/Root` 다시 구성): 로우앵글 고글 쥐들 → 스포이드 물방울 반짝 → 낙하 줌 블러 → **지하도시 화면에서** 비밀 실험실 폭발 + 돌아다니는 지하 쥐들·고글 쥐 3마리가 전부 건물로 빨려 듦 (사용자, `LobbyHome.suspended` 로 걷기 멈춤, 끝나면 `RestoreWorld` 로 원래대로) → 배관 타고 아래로 (가속 + 잔상) → 하수구 뚜껑 덜컹덜컹 → 최고 등급 유니크 이상 = 등급 색 빛 · 에픽 이하 = 파리 → **전부 한 마리씩 강조** (하수구 장면 뒤, 클릭/스페이스 = 다음, 건너뛰기 = 결과) → 결과 칸.
  - **2차 다듬기 (사용자 피드백)**: 빛·반짝·강조 효과를 벡터풍 → **Codex 로 그린 빛 효과** (`GachaLab/glow_*`, 검은 배경 → `Tools/slice_glow_grid.py`) · 배관~출구를 **세로로 긴 한 장 `lab_shaft`** 로 이어 붙여 연결 (뚜껑·빛은 그 그림의 자식) · 덜컹덜컹 뒤 **"눌러서 열기!" 클릭해야 열림** · 공개 = 실루엣 → 번쩍 → 색 + 등급 폭발(전설 금빛·신화 무지개) · 빛기둥 · 반짝이 · 결과 칸이 뜨면 뽑기 곡 2.5초 페이드 아웃 (`MusicManager.Play(clip, fade)`, `SceneMusic.gachaFadeOut`, `GachaShow.Cutscene`).
  - 건물이 화면에 없으면(지하 밖에서 열림) 실험대 컷 `lab_c4` 에서 결과 쥐들이 빨려 드는 것으로 대신. 연출 중 LobbyPan '올라가기' 버튼 숨김 (`GachaShow.Busy`).
  - 확인함: Play 에서 가짜 결과 6개로 처음부터 끝까지 (지하 폭발·빨려 듦·배관·뚜껑·강조·결과 칸), 콘솔 에러 없음. 실제 뽑기(저장값 사용)로는 아직 안 돌림.
- **배경음악** (Gemini 음악 만들기, 연주곡 · 표준): `BGM_Alley` (지하 도시, SceneMusic.alleyClip — LobbyPan.AtRoom 일 때) · `BGM_Gacha` (뽑기 연출, SceneMusic.gachaClip — GachaShow.Busy 일 때).
- **UnityMCP 가 안 붙을 때**: 유니티 허브가 uv 설치 전에 켜져 있으면 PATH 에 uvx 가 없어 서버를 못 띄움 → 허브를 완전히 껐다 켜거나, 터미널에서 `uvx --from "mcpforunityserver==10.3.0" mcp-for-unity --transport http --http-url http://127.0.0.1:8080 --project-scoped-tools` 로 직접 띄운 뒤 유니티 MCP 창에서 Start Session.

### 5-4 이어서 할 일 (2026-10-12 밤, 세션 넘김 시점)

사용자 요청: ① 빛 효과를 원래 그림체(동화풍 칠)로 → **끝** (`GachaLab/Sheets/lab_glow2.png` → `glow_*`·`boom_teal`·`burst_*` 같은 이름으로 교체, 옛 사진풍은 `GachaLab/_glow_v1/`, 씬 Image 는 preserveAspect) ② 비밀 실험실이 물 위에 떠 있음 → **끝** (임포터가 피벗을 가운데로 덮어써서 바닥 기준으로 다시 놓음, 터진 그림 바꿀 때 바닥선 유지 코드) ③ **하수구를 더 긴 세로 그림으로 + 블러 속도감 → 끝 (2026-10-12 밤, Play 확인함)**:
- 세로 3장 `Sheets/shaft_a~c.png` (각 1024×1536, 다음 장은 앞 장 아래 400px `ref_prev_bottom.png` 를 보고 이어 그림) → `python Tools/stitch_vertical.py <출력> auto a b c` (auto = 겹침 자리 자동 찾기, 둘 다 400px) → `lab_shaft.png` 1024×3808 (같은 이름으로 덮어써 씬 연결 유지, 옛 짧은 그림은 `Sheets/lab_shaft.png`). 텍스처 Max Size 4096.
- 씬 (유니티 꺼진 상태에서 YAML 직접): `PipeImage` 1920×7140 · 자식 HoleGlow·Pillar·Lid·FlyTemplate (x -9, y -2694) · Rays (-9, -2734) · GachaShow `pipeTime` 3.2 · `pipeBlur` 0.025 (긴 그림이라 잔상 간격 줄임).
- **쥐 고유 컷씬 (전설 이상)**: `UnityResources/Rats/GachaIntro/README.md` (만드는 법 4단계 · 쥐별 입자). **신화 15종 끝 (2026-10-12 밤)**: 아이스 퀸 + 14종 Codex 42장, 씬 `Intros/` 15개 · `GachaShow.intros` 15. Play 로 다람쥐·관광쥐·폭탄마 연속 공개 확인. 전설 11종은 사용자 결정 대기. `Lobby/GachaIntro.cs`(틀) · `GachaIntroCuts.cs`(컷 + `trans` 전환 값 + `snow` 입자) · `GachaShow.intros`/`introFrom`. 씬 `UI/GachaShow/Root/Intros/<이름>` = `Tools/gen_gacha_intro_scene.py` 가 만든 코드를 MCP execute_code 로.
- **컷 전환 통일 (사용자: 벡터풍 눈보라 · 줌 인 · 플래시 전환, 다른 컷도 비슷하게 이어지게)**: `Lobby/CutFx.cs` = `PushOut`(컷 끝 줌 인 + 하얗게) · `PunchIn`(하얀 화면에서 크게 시작해 자리 잡음) · `Blizzard`(흰 원을 속도 방향으로 늘린 벡터풍 입자, 씬 템플릿 Image 복제) · `CutTransition`(전환 시간·배율). 하드코딩 정리 (사용자): 전환 값 = GachaShow `labCut`·`dropCut`·`fallCut`·`benchCut`·`worldCut`·`pipeCut`·`introCut` · `introSfx` · `introRevealFlash` · `resultFlash*`, 배관 속도선 = `pipeLines`(템플릿 `Stage/CutPipe/SpeedDot`) · `speedDots/Mul/Base`, 지하 고글 = `Alley/GogglesTemplate` (전엔 new GameObject). 뽑기 연출 전 구간에 적용: 실험실 → 스포이드 → 낙하 → 지하(시작·끝 플래시) / 실험대 컷 → 배관(펀치 인 + 위로 스치는 속도선 `speedDot` = `Art/Generated/circle.png`) → 하수구 → 공개 → (고유 컷씬) → 결과 칸 살짝 번쩍.
- **확인함 (2026-10-12 밤)**: Play 에서 가짜 결과(흰 실험쥐 + 아이스 퀸 NEW)로 처음부터 결과 칸까지, 12프레임마다 캡처해 전환·눈보라·속도선 확인, 콘솔 에러 없음. 테스트 = `scratchpad` 식: Play → `LobbyPan.AtRoom` true → `GachaShow.Play` + `EditorApplication.update` 로 캡처·뚜껑 자동 누름.
- **남음**: 사용자 눈으로 확인 (전환 속도 · 눈보라 양 · 컷씬 길이 약 3.5초) → 커밋 (이번 작업 전체 아직 커밋 안 함).
- **뽑기 수정 (2026-10-12 밤, 사용자)**: ① 뚜껑이 뽑을 때마다 옆으로 밀림 → 처음 자리를 한 번만 기억 (`lidHome0`) ② 빨려 드는 장면(지하, Prep 숨김)에서 지하 클릭됨 → `LobbyManager` inputBlocked 에 `GachaShow.Busy` ③ 건너뛰기 2단계: 뚜껑 앞까지는 1번째 = 뚜껑 열기 전으로 (`JumpToSewer`), 그 뒤 = 결과. 처음 얻은 신화(`mustIntroFrom`)는 건너뛰어도 고유 컷씬을 보여 주고 결과 (그동안 건너뛰기 버튼 숨김·입력 무시). Play 자동 테스트로 확인.
- **뒷골목 숨김 · 조각 1개 (2026-10-13, 사용자)**: ① 하수구 뒷골목(맨홀)은 16층 클리어 전엔 **아예 안 보임**, 열릴 때 튜토리얼 `l_alley` 가 시작되는 순간 통 튀어나옴. `Progress.IsUnlocked("alley")` = 16층 클리어 **+ `Progress.alleyRevealStep`(l_alley) 시작함**, `MarkTuto` 가 UnlockVersion 을 올려 FeatureGate 가 다시 그림. 튜토리얼 테이블 l_alley 조건 = `Unlocked gacha` (alley 로 두면 서로 기다림). `FeatureGate` 에 `disableWhenLocked`(콜라이더 끄기) · `revealTime/revealOvershoot/revealSfx`(보는 중 열리면 튀어나옴), `LobbyHot.Contains` 는 콜라이더 꺼져 있으면 false. 씬 연결 끝: Lobby `Home/Hots/Hot_alley` FeatureGate → hideWhenLocked = Manhole · Highlight · Tag, disableWhenLocked = BoxCollider2D, revealSfx = jgl_unlock. Play 확인 (잠김 = 맨홀·팻말 숨김·클릭 안 됨 → 열림 = 튀어나옴, 콘솔 에러 없음). 실제 16층 첫 클리어 흐름으로는 아직 안 봄. 커밋 안 함. ② 뽑기 조각 = 등급 상관없이 캡슐당 **1개** (뽑기 테이블 Gacha_Grade shard_count 전부 1, 백업 `_backup_20261013`). 상점 묶음 수량(Shop_Goods pack_size)은 그대로.
- **MCP**: 세션 시작 때 유니티가 꺼져 있으면 Claude 쪽 UnityMCP 가 '실패'로 남음 → `/mcp` 에서 Reconnect. 안 하면 서버(127.0.0.1:8080/mcp)에 HTTP 로 직접 붙어도 됨 (initialize → tools/call).
- 참고: 이 세션에서 MCP 서버는 Claude 백그라운드로 띄운 것이라 세션이 끝나면 꺼짐 → 유니티 허브를 완전히 껐다 켜면 유니티가 직접 띄움 (§5-4 'UnityMCP 가 안 붙을 때').

---

## 5-5. 스트링 테이블 · 영어 번역 (2026-10-13)

- **`Data_Table/스트링 테이블.xlsx`** (String 시트: string_key · ko · en · 출처 · 상태 · 번역 메모 / Glossary 시트 = 용어집) → `StringTable.json`. **2,102개 전부 영어 번역 끝** (자리표시 `{n}` · `<color>` 태그 · 대화 구분 `|` 검사 통과).
- **사용자 결정**: 다른 테이블의 한글은 그대로 두고(사람이 읽는 용), 글 칸 옆에 `<칸>_sk` int 키 칸을 둠 (관리·코드용) · 플레이어가 보는 글만 (desc·dev 메모·테스트 패널 제외) · 밈은 영어권 말장난으로 현지화 · 이번엔 테이블+영어만, **게임 안 언어 전환 코드는 다음 단계**.
- 키 = 칸 번호 × 100000 + 행 id (예: 쥐 스킬 이름 12 → 1220086). 10~41 = 테이블 칸 (`Tools/gen_string_table.py` 의 COLS), **80xxxxx = 씬 글** (Title·Lobby·Game 의 TMP m_text + 인스펙터 문자열, 같은 한글은 키 하나), **90xxxxx = 코드에 박힌 글** (팝업·배너·로비 쥐 수다 등, 자리표시 이름 정리함: `{Format(power)}` → `{power}`). 
- 도구: `python Tools/gen_string_table.py` (키 칸 채우기·한글 바뀌면 상태 '한글 바뀜'·새 글 '번역 필요'·사라지면 '안 쓰임', 여러 번 돌려도 키 그대로) · `python Tools/import_scene_strings.py` (씬 저장 후, 새 씬 글 추가). **순서: gen_stage_table → gen_economy → gen_skill_tree → gen_string_table → xlsx2json** (gen_skill_tree 가 공용 스킬 시트를 다시 만들어 _sk 칸이 지워져도 같은 한글이면 같은 키로 다시 채움).
- 용어 (사용자 확정): 게임 제목 Squeak! Squeak!! Squeak!!! · 찍 = Squeak · 찍찍!!(힘) = SQUEAK!! · 적정 = Rec. · 훈장 = Rank · 조각 = Shard · 연구자료 = Research · 친구 = Buddy/Buddies!! · 탈출 = Escape · 맘스땃쥐 = McSqueak's · 쥐4 = Rat Jerky · 줴리 = Jerreh · 흐에~ 스트리머 = 영어권 방송 밈 (쥐커드 = Squeak Shuffle) · 근육 쥐 = Mouscle · 5G = Mi-Fi. 나머지는 Glossary 시트.
- 한국어 아재개그(줴리 필살기 자막 쥐포·킹콩 등)는 영어 말장난으로 바꿈 (Hide and squeak 등).
- **언어 설정 (2026-10-13)**: 타이틀 `UI/Settings/Card/Language` (한국어 · English 버튼, `LanguageSettings`). 코드 = `Core/Loc.cs` (PlayerPrefs `nkk_lang`, `Loc.Get(키, 원래 글)`, `Loc.Changed`) · `Core/LocText.cs` (씬 TMP 에 키, template 켜면 코드가 `LocText.Tpl` 로 틀만 가져감) · GameDatabase.stringTable. **타이틀 씬만 연결 끝** (메뉴·저장 칸·확인 창·설정·오프닝 자막 Story text_sk·훈장 이름 tier_name_sk). Play 로 영어 확인 (로고 자동 크기, 저장 칸 설명 자동 크기). 로비·게임 씬은 아직 (GameDatabase.stringTable 도 로비·게임 씬엔 아직 안 넣음).
- **로비·게임 연결 (2026-10-13)**: ① 테이블 글 = 행 클래스에 `<칸>_sk` int 필드 + `GameDatabase.Load` 끝에서 `Loc.ApplyAll` (string 칸 X 옆 X_sk 가 있으면 지금 언어로 바꿔 끼움, 언어 바꾸면 Load 다시) ② 씬 글 = 루트 `Localization` 오브젝트의 `LocScene` (실행 순서 -10000, 씬 열 때 제일 먼저): 씬의 LocText 전부(Lobby 222 · Game 54, 전부 template) + 인스펙터 문자열 `fields`(컴포넌트+경로+키, Lobby 102 · Game 100). 테스트 패널은 뺌 ③ 코드 글 = `Loc.T(키)` · `Loc.Pick(키…)` (한국어도 테이블 ko 칸에서, 코드에 원문으로 찾지 않음 — 사용자), 자리표시는 .Replace. 남은 한국어 리터럴 = 씬 값이 덮어쓰는 인스펙터 기본값·디버그 로그뿐. ④ 영어일 때 LocText 글은 칸을 넘치면 자동 크기 (최소 55%, `LocText.Fit`). 번역이 없으면 한국어 대신 `[Loc] … 글 없음` 경고. 영어로 로비 페이지들·게임 HUD Play 확인. 씬 다시 연결 = scratchpad 식 hook 코드 (TMP ko → 80 키, SerializedObject 문자열 → fields).
- **튜토리얼 (2026-10-13)**: 최대 마릿수 = g_pop (닿으면, 몇 번째 판이든) 또는 g_pop_f5 (4층까지 한 번도 안 닿았으면 5층 시작) — 둘 중 하나 보면 끝 (서로 NotDone + g_power_pop). 치즈 창고: SkillSelect = 강조된 추천 칸을 골랐을 때만, SkillBuy = 그 칸을 활성화했을 때만 (`SkillPage.BoughtGuided`, 로비 들어올 때 0). 강조 테두리는 글자(TMP)면 실제 글자 범위 (`TutorialTarget.fitText`) — 적정 찍찍 강조가 칸 전체로 커지던 것.
- **남음**: ① ~~로비·게임 연결~~ 끝 (위) ② 영어 글이 길어서 칸 넘침 확인 ③ 튜토리얼 대사 70·74 · 하수구 대사 '믹서기' 는 지금 뽑기(비밀 실험실)와 안 맞음 → 한글부터 고칠지 사용자 확인 ④ 확률 정보 '조각 {s}개' 는 이제 늘 1개.

## 5-6. 초반 적정 찍찍 (2026-10-13)

- 기획서 배율(1~4층 ×1.25 · 5층 ×1.8 · 6~15층 ×1.32 · 16~20층 …)·방 수·목표 시간은 기획서와 같음 (지수 × 400).
- 측정 round8 (`Tools/probe_results/round8_fresh_f1-5.txt`, 새 저장 · 1훈장 · 스킬 0 · 총공격 O · 승급 1): 1층 5~14초 통과, **2층 4판 모두 실패** (30마리 꽉 차도 힘 300~380 = 적정 500 의 0.6~0.7배, 벽 0~1개). 전체 기준 260 으로 낮춘 측정(round8b): 2·3층 통과 · 4층 0.9배 근처 → 사용자 "너무 쉬움".
- **사용자 결정: 1층만 300, 2층부터는 예전(400 기준) 그대로** → `gen_stage_table.py` `POW_FIX = {1: 300}`. 2층 이후는 이전 값과 같음 (확인함).
- 참고: 측정상 새 저장 첫 판은 여전히 2층이 첫 벽 (1층 값은 2층 난이도에 영향 거의 없음).

## 5-7. 서버 (계획만, 2026-10-13)

- `서버 계획.md`: 목적 = 랭킹 · 기록 · 내부 지표 (저장은 로컬 그대로) · Firebase · itch.io 윈도우 + WebGL → Unity SDK 대신 **REST (UnityWebRequest)** · 익명 로그인 · Firestore `players` / `runs` · 닉네임 + 로비 랭킹. Firebase 프로젝트는 사용자가 만들 예정 (아직 없음). 구현 전에 정할 것 = 문서 9장.
- 해적 쥐 영어 이름 = **Pirat** (사용자).

- **뽑기 스포이드 컷 (2026-10-13)**: `lab_c2` 물방울 없는 그림으로 다시 뽑음 + 연출 물방울을 스포이드 끝에 매달림 (GachaLab README).

- **치즈 창고 지도 고정 (2026-10-13)**: 끌기·휠 확대 끔 (`SkillPage.allowPan` · `allowZoom`, 트리 전체가 보이게 맞춘 상태 그대로). 안내 글 = "스킬을 찍으면 이어진 칸이 드러나요" (8000201).

- **신화 고유 컷씬 (2026-10-13)**: 신화는 이미 가진 쥐여도 항상 컷씬 (건너뛰기 중이어도). 저장 `gacha.introSeen` (끝까지 본 쥐) — 처음 보는 컷씬은 못 넘기고, 본 컷씬은 화면 클릭(스페이스·엔터·ESC) 한 번으로 그 컷씬만 넘김 (`GachaShow.introTapSkip`). 전설은 그대로 처음만.

- **뽑기 하수구 뚜껑 (2026-10-13)**: 지난 뽑기에서 열린 채 남은 뚜껑을 하수구 장면 시작 때야 닫던 것 → 배관을 내려가기 전부터 닫힌 뚜껑을 출구에 붙임 (`GachaShow.CloseLid`, Pipe · JumpToSewer · Sewer, 원래 자리는 Awake 에서 기억). Play 로 두 번 연속 뽑기 확인. 참고: 지하 도시 빨려 듦 장면은 지하에서 뽑기를 열 때만 (아지트에서 열면 실험대 컷으로 대신, 원래 설계).

- **로비 말풍선 프리팹 (2026-10-13)**: `Assets/Prefabs/LobbyBubble.prefab` (배경 · 꼬리 · 글자를 SortingGroup 으로 한 덩어리, 묶음 안 배경·꼬리 0 · 글자 1). 씬 `Home/Fx/BubbleTemplate` = 이 프리팹 인스턴스 (아지트 Home · 지하 AlleyRats 둘 다 씀). 말풍선마다 순서 `bubbleOrder` + 순번 → 겹치면 새 말풍선이 배경째로 위 (예전엔 전부 9000/9001 이라 뒤 글자가 앞 말풍선 위로 비침).

## 5-8. 브랜치 · MVP 서버 (Firebase) — 2026-10-13 저녁 세션 넘김 시점 ★ 여기부터 이어서

**브랜치** (사용자 결정, 세 브랜치 모두 원격에 푸시됨 · 저장소는 비공개·팀원만):
- `main-game` = **메인 개발**. 게임 수정은 여기서 커밋 → `mvp` 에서 `git merge main-game`.
- `mvp` = **itch.io 출시용** (Firebase 랭킹·기록 · WebGL · 테스트 패널 없음). MVP 전용 작업만 여기서.
- `main` = **기본 틀** (원격 기본 브랜치, 건드리지 않음). 팀원 `pdw` 브랜치도 원격에 있음.
- 팁: `main-game` 을 고칠 땐 브랜치를 바꾸지 말고 `git worktree add ../NKK_maingame_wt main-game` 에서 고치고 커밋 → `git worktree remove` → mvp 에서 merge (유니티 "씬 다시 불러올까요?" 팝업 안 뜸, 스크립트만 다시 컴파일). 브랜치를 직접 바꾸면 팝업 → 사용자에게 Reload 요청.
- 합칠 때 주의: mvp 가 공용 파일도 고침 (`Progress.cs` · `LobbyManager.cs` · Title/Lobby/Game 씬). 특히 **Lobby 씬 상단 탭줄 · 낮잠 침대 창**을 main-game 에서 고치면 씬 충돌 가능 → 합칠 때 확인.

**MVP 서버 지금 상태** (단계별 자세한 기록 = 루트 `서버 계획.md` 10장 · HANDOFF_LOG §9-22):
- Firebase 프로젝트 `nkk-mvp` (사용자 계정 caddman771144@gmail.com, 작업은 Claude in Chrome 탭에서) · 익명 로그인 · Firestore 서울 · 웹 앱 `nkk-web`.
- 코드 `Scripts/Net/`: `ServerConfig`(에셋 `Assets/Data/ServerConfig.asset`, `sendInEditor` 기본 끔) · `FirebaseClient`(익명 로그인·토큰 갱신·Create/Set·runQuery) · `Telemetry`(훈장 달 때 `rankups` 기록, 오프라인 큐) · `Leaderboard`(`players/{uid}`, 상위 100 · 5분 캐시, 닉네임 2~12자 + `Assets/Data/NickBanned.txt`). 세 씬 `Server` 오브젝트에 다 붙어 있음 (타이틀 것이 끝까지 살아남음).
- 로비: 상단 **랭킹 탭** (`Tab_lb`, 아이콘 `lb_rank_board.png`) · `Page_lb`(`LeaderboardPage`) · `UI/NickPopup` · 낮잠 침대 `ServerRows`(`ServerSettings`: 익명 통계 보내기 토글 · 닉네임 바꾸기). 랭킹 = 최고 층 ↓ → 그 층에 처음 닿았을 때 시도 횟수 ↑ (`Progress.floorRuns`), 시즌 없음, 닉네임 중복 허용.
- 보안 규칙 `Firebase/firestore.rules` 게시됨 (rankups 만들기만 · players 본인만, 최고 층·훈장 감소 불가, 목록 100). 색인 players (bestFloor ↓ · runs ↑). 거절 시험 통과.
- **기능 해금 기록** (2026-10-14, `974b043`): 쳇바퀴 훈련(`rats`)이 열릴 때 `milestones` 에 기록 1개 (uid · feature · 훈장 · 탈출 시도 · 최고 층 · 플레이 시간 · 버전 · 빌드 · 언어) — 훈장 전 초반 유저 수집. `Progress.FeatureUnlocked` → `Telemetry.logFeatures`(기본 rats, 늘리면 규칙의 `feature in [...]` 도). 훈장 기록과 같은 큐라 **규칙에 없는 기록은 큐 맨 앞에서 막혀 훈장 기록까지 못 나감** → 규칙 먼저 게시. 규칙 콘솔 게시함 (2026-10-14, 실제 쓰기 시험은 안 함).
- **API 키 2개** (2026-10-14 바꿈): `ServerConfig.Key` 가 **페이지 주소로** 고름 — itch.io(`itch.zone`)에서 연 WebGL = 출시용 `Browser key` (= `apiKey`, itch.io·itch.zone 주소로만) · 그 밖(에디터·윈도우·사무실 localhost 에서 연 WebGL) = 개발용 `nkk-dev (editor/win)` (= `devApiKey`, Identity Toolkit·Token Service·Firestore API 만). 빌드 하나로 itch.io 와 사무실 둘 다 됨. 윈도우 빌드는 출시 안 함 (itch.io = 웹만). 개발용 키를 **사무실 공인 IP `59.9.223.133` 으로 제한하기로 함 — 콘솔 설정은 아직** (사용자). 팀원은 같은 사무실·같은 공유기.
- WebGL 설정: Gzip + decompression fallback · 데이터 캐시 · 기본 창 1280×720. **에디터 플랫폼 = WebGL** (2026-10-14 전환). Development Build 는 압축이 안 돼 itch.io 파일 하나 200MB 제한에 걸림 → 항상 일반 빌드.
- **웹 아틀라스** (2026-10-14, main-game `50aec23` → mvp): 가로·세로가 4의 배수가 아닌 스프라이트는 DXT 압축이 안 돼 무압축으로 들어감 (.data 224MB) → `Editor/WebAtlasBuilder.cs` 가 그런 스프라이트만 폴더별 아틀라스(`Assets/Art/_WebAtlas`, 133개, DXT5)로 묶음. 원본 PNG·크기·피벗 그대로. 그림 넣거나 지우면 자동으로 다시 맞춤 (메뉴 `NKK/Rebuild Web Atlases`, 설정 초기화 `NKK/Reset Web Atlas Settings`). Sprite Packer = Sprite Atlas V2. 압축 빌드 .data 132MB · zip 144MB. (투명 픽셀 덧붙이기는 `sprite.bounds` 로 크기 맞추는 코드 때문에 최대 9% 작아 보여서 안 씀.)
- 에디터에서 서버 시험할 때: Play 중 `ServerConfig.sendInEditor = true` (메모리) → 끝나면 **false 로 다시 저장** (Play 중 ScriptableObject 변경은 남음).

**바로 다음 할 일**
1. **WebGL 빌드 끝** (2026-10-14, 빌드 약 5분): `Unity_Making/NKK_Project/Builds/WebGL.zip` (Builds 는 git 제외) → **사용자가 itch.io Restricted 페이지에 직접 업로드**, 팀원 공유 (비밀번호 또는 관리자 추가). 로컬 확인: 타이틀·저장 칸·인트로·게임 화면 그림 정상. 다시 빌드 = BuildPipeline WebGL, BuildOptions.None, 출력 `Builds/WebGL`.
2. itch.io 에서 확인: 익명 로그인 · 훈장 기록 · 랭킹 쓰기/조회 · **한글 닉네임 입력** (WebGL 의 TMP 입력 칸은 한글 조합(IME)이 안 될 가능성 큼 → 안 되면 WebGL 입력 플러그인 검토) · 저장(브라우저 저장소) · 소리 · 화면 크기.
3. 남은 정할 것: 개인정보 안내 문구 (익명 ID 만 씀, itch.io 페이지 설명).
- 서버 시험 문서 2개(`players/Z6x5…` '에디터시험' · `rankups/874V…`)는 사용자가 콘솔에서 지우기로 함 (Claude 는 영구 삭제 못 함). 이 PC PlayerPrefs 시험값은 지움.
- 커밋 안 한 것 (예전부터): `ProjectSettings/QualitySettings.asset` · `UnityConnectSettings.asset` 변경 여부 확인 필요 · `Unity_Making/NKK_Project/Captures/` (MCP 스크린샷, 커밋 안 함).

**튜토리얼 수정 (2026-10-13, main-game `c57aaef` → mvp)**: 다른 PC(처음 하는 저장)에서 "대사는 나오는데 UI 가 안 보임" — 기능이 열리며 튀어나오는 UI(`FeatureGate.Reveal`, 크기 0→1) 를 대사가 가리키면 `TutorialTarget` 이 0 부근 크기를 원래 크기로 기억해 굳음 (찍찍!! 수치·마릿수·승급 버튼·맨홀 팻말). 고침: 튀어나오는 중엔 확대를 미룸 · 크기 0 은 원래 크기로 안 씀 · 튀어나오다 꺼지면 원래 크기로. 다른 PC 에서 처음부터 하는 흐름으로는 아직 직접 확인 안 함.

## 5-9. itch.io 페이지 · 화면 비율 · butler 업로드 (2026-10-15 새벽) ★ 크롬 작업 여기서 이어서

itch.io 프로젝트 = **Squeak!Squeak!!Squeak!!!** (`parksongho.itch.io/squeak-squeak`, 편집 `itch.io/game/edit/5148827`, DRAFT, 계정 INORYW82-1). 크롬(Claude in Chrome)이 중간에 끊겨서 브라우저 작업이 남음.

**한 것**
- 화면 비율: 예전 빌드는 Default 템플릿이라 캔버스가 1280×720 **고정** → 640×360 칸에서 잘렸음. 새 WebGL 템플릿 `Assets/WebGLTemplates/Fit/index.html` = 창(게임 칸) 크기에 맞춰 16:9 유지하며 꽉 채움(남는 쪽 검은 띠), 푸터·유니티 로고 없음. `PlayerSettings.WebGL.template = PROJECT:Fit`.
- itch.io 임베드 설정 저장됨: **960×540 · 전체화면 버튼 켬** (모바일·자동 시작·스크롤바·SharedArrayBuffer 끔).
- **butler** (itch 공식 업로드 도구) 설치 `%USERPROFILE%\tools\butler\butler.exe`, 로그인 완료(키 `~/.config/itch/butler_creds`). 업로드: `butler push "C:/Dev/NKK_PROJECT/Unity_Making/NKK_Project/Builds/WebGL" parksongho/squeak-squeak:html5 --ignore capture.html` · 상태 `butler status parksongho/squeak-squeak`. 140MB 올리는 데 10분 이상 걸림.
- **버그 고침** `Scripts/Core/LocText.cs` `Fit()`: 영어일 때 자동 크기 최소값을 무조건 8 로 잡아서, 크기 1.5~2.7 인 **월드 TextMeshPro(로비 간판)가 3~5배로 커져 겹침** → 최소 8 은 UGUI 글에만, 원래 크기보다 커지지 않게. 고친 빌드로 로비 정상 확인함.
- 스크린샷 (1920×1080 JPG, 영어): `Builds/ItchShots/itch_01_title_en.jpg` · `itch_02_story.jpg` · `itch_04_promote.jpg` · `itch_05_swarm.jpg` · `itch_07_hideout.jpg` (원본 PNG 같은 폴더). 캡처 방법: `Builds/WebGL/capture.html`(preserveDrawingBuffer + 1920 렌더) 을 로컬 서버(스크래치 `shotserver.py`, 포트 8765, POST `/save?name=` 로 PNG 저장)로 열고 `canvas.toDataURL`.

**남은 일 (크롬에서)**
1. butler 업로드 확인: html5 채널이 버전 2(고친 빌드)가 됐는지 `butler status` 로 확인 (세션 넘길 때 버전 2 업로드 진행 중).
2. 편집 페이지 Uploads: 새 html5 업로드에 **"This file will be played in the browser"** 체크, 옛 수동 업로드 `WebGL.zip`(2026-10-14) 은 체크 해제 또는 삭제 (사용자 확인 후). 저장.
3. 스크린샷 5장 업로드 (편집 페이지 Screenshots → Add screenshots, 파일 인풋에 `file_upload`. 한 번에 10MB 제한이라 각 300KB 는 문제없음). 순서: 타이틀 → 스웜 → 승급 → 아지트 → 스토리.
4. 실제 페이지에서 Run game → 960×540 칸에 잘림 없이 맞는지 · 전체화면 버튼 확인.
5. (선택) 표지 이미지(cover, 630×500) 없음 · 커스텀 테마는 이미 있음.

**커밋 안 한 것 (mvp 브랜치, 지금 체크아웃 상태)**: `Assets/WebGLTemplates/` (+ .meta) · `LocText.cs` · `ProjectSettings.asset`(템플릿 = Fit) · 폰트 SDF 2개(Play 중 글자 추가로 바뀐 것). `LocText.cs` 수정은 main-game 에도 필요 → main-game 에서 고치고 merge 하거나 cherry-pick.

## 6. 밸런스 측정 (BalanceProbe)

- 목표 (사용자): 전투력 = 적정일 때 일반 층은 제한시간 180초의 **60~90%**(75% 쪽으로), 보스 층은 210초의 **약 90%**(길 117초 + 보스전 72초, 보스전은 Boss `hp_pow_sec` 로 맞춤).
- 쓰는 법 (Play 중, 몇 분 걸림 → 사용자에게 먼저 알리기):
```
NKK.BalanceProbe.Results.Clear(); NKK.BalanceProbe.Rows.Clear(); NKK.BalanceProbe.Queue.Clear();
NKK.BalanceProbe.Calib = true; NKK.BalanceProbe.UseUlt = true; NKK.BalanceProbe.UseRush = true;
NKK.BalanceProbe.Queue.Add(new float[]{ 티어, 시작층, 끝층, 600, 20, -1, -1, 2 });   // 노드 -1 = 다음 훈장 조건(트리 전부) · 조각 -1 = 조건 · 마지막 = 일괄 승급
NKK.BalanceProbe.RunQueue();   // 결과: NKK.BalanceProbe.Results / RowsCsv() → Tools/probe_results/roundN.csv
```
  `Calib` = 적정 전투력을 무리 전투력으로 덮어씀 ("적정일 때 몇 초"), 층 줄에 `└ 보스전 N초` · `└ 사람 등장·평균·최대` · `└ 기준 180초의 N%`. 저장은 `nkk_probe` 키만 씀.
- 결과 → `python Tools/update_spu.py <csv...> --write` → `python Tools/gen_stage_table.py && python Tools/xlsx2json.py`. 랜덤 지형이라 **층당 4회 이상**, 구간 평균으로 볼 것. 측정 중 스크립트 수정 금지, 유니티 창을 앞에.
- 현재: 라운드 7 까지 130번 중 70번 목표 범위 (`Tools/probe_results/round5_7_merged.csv`). **다음 측정은 사용자가 성장 곡선을 준 뒤** (사용자 2026-10-08: 측정보다 수정 먼저). 그 뒤로 바뀐 것(보스 필살 패턴·고양이 무리 스킬·훈장 조건 트리 전부)이 시간을 늘릴 수 있음 → 다시 재야 함.

---

## 7. 남은 일 (순서 제안)

0. ~~보스 테이블 분리 + 두 번째 일반 공격 + 쿨타임 스킬(밈) 6종~~ → 끝 (LOG §9-17). 사용자가 직접 보스전을 해 보고 세기·연출 피드백.
1. **사용자 플레이 피드백 반영**: 보스 필살 패턴 12종·쿨타임 스킬 6종·고양이 무리 스킬 10종의 화면 연출·세기 (데이터 발동은 확인, 화면은 일부만 봄). 보스 건너뜀이 또 생기면 Editor.log `[Boss]` 확인.
2. **밸런스**: 사용자 성장 곡선 → `gen_stage_table.py`(POW_GROW·HP_GROW·벽)·`gen_skill_tree.py`(값·비용) 조정 → 측정 (§6). 보스전 시간(필살 패턴 포함)·고양이 방해 정도도 같이. 높은 층(10층+) 사람 수도 아직 안 잼.
3. ~~로비 남은 페이지 (친구들 · 낮잠 침대)~~ → 끝 (§5-1). 탭 아이콘·지도 등 로비 소품을 플랫으로 다시 만들지 사용자 결정.
4. 코드에 박힌 팝업 글(Rat.Action "찌릿!!", ItemManager.ZapChain, 고양이 "냥!" 등) → 테이블/인스펙터.
5. 사운드: 목록 초안 `사운드 목록.md` (배경음악 · 징글 · 효과음, 우선순위). 제작 방식 결정 필요.
6. 템플릿 잔여물(TutorialInfo · Readme.asset · SampleScene) 삭제 여부 — 사용자 답 대기.
7. 안 쓰는 새 그림: `FX_CatWarn/cat_target`·`stun_stars`, `BossProps/boss_anger`, FX_New 일부(연기·불꽃·독가스·땀·Zzz·흙더미).
8. 출시 준비: 테스트 패널 끄기 · PC 빌드 테스트.

---

## 8. 작업 기록 색인 (상세는 `HANDOFF_LOG.md`)

| 날짜 | 내용 |
|---|---|
| 10-06 | 이식 시작, 리그·물건·층·사람·고양이·패시브·특수 액션, 이펙트 그림 재제작(§5), 로비 시작 |
| 10-07 | 필살기 33종·슈퍼 점프(§7) · 제한시간 · 게임 오버 · 층 탈취 · 보스 6종 · 공용 스킬 트리·훈장 · 쳇바퀴 훈련 · 층 벽 밸런스 1차 (§9-1~9-8) |
| 10-08 오전 | 찍찍!! 훈장·업적 · 쥐 스킬 아이콘 · 고양이 보스 그림 · 측정 라운드 5~7 · 랜덤 지형 · 쏟아짐 · 벽 폭파 · 경제 개편 (§9-9~9-11) |
| 10-08 오후 | 보스 건너뜀 수정·미니맵·쥐 마릿수·HUD 게이지 (§9-12) · 고양이 무리 스킬·사람 보충 (§9-13) · 훈장 = 트리 전부·번식 하트·보스 필살 패턴·층 길 스크롤 (§9-14) |
| 10-08 저녁 | 보스 테이블 분리 · 두 번째 일반 공격 3종 · 보스 쿨타임 스킬(밈) 6종 (§9-17) · 진압봉 손에 · 맥스웰 춤 · 발악 패턴 밈 12종 · STAY 컷씬 · 테스트 패널 보스 고르기 (§9-18) |
