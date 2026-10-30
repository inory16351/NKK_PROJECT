# 찍!찍!!찍!!! — 유니티 이식 인수인계 (요약본)

웹게임 `Proto_Game/rat-uprising.html`(약 1만 줄 JS)을 유니티로 옮기는 작업. 이 문서는 **지금 상태와 다음 할 일**만 정리한 요약본입니다.
날짜별 상세 기록(측정 결과·결정 경위·옛 수치)은 `HANDOFF_LOG.md` (§9-1 ~ 9-14).
마지막 갱신: 2026-10-30 (**§5-39 저작권 검토 19줄 고침 (사용자 검토 시트대로)**) / 2026-10-30 (**§5-38 줴리 초필살기 v3 = 무성영화 앵콜 (턱시도 고양이 · 세피아 · 자막 카드)**) / 2026-10-30 (**§5-37 미니 게임기 = 쥐돌깨기 창 (웹 프로토타입을 C# 으로, 수치 = 미니게임 테이블)**) / 2026-10-29 (**§5-34 훈장별 아지트 배경: 4훈장 = 고양이 연구소 · 6훈장 = 지구과학 연구소 · 9훈장 = 공사 현장 · 11훈장 = 수중 연구소 · 13훈장 = 2차대전 박물관 · 16훈장 = 우주 연구소 (`LobbyHome.tierLooks`)**) / 2026-10-29 (**§5-33 Windows 빌드 화면 비율 16:9 고정 · 21:9/4:3 은 검은 띠 · 활성 플랫폼 Windows 로 전환 · 테스트 빌드 확인 · 커밋 안 함**) / 2026-10-29 (**§5-32 새 쥐 콩쥐팥쥐(180° 맞붙은 쌍둥이) · 나비쥐(머리만 날아다님) · 커밋 안 함**) / 2026-10-26 (**§5-31 1시간 단위 페이스: 시간당 층이 점점 줄게 · 훈장 승급 배율 rank_mul · 실측 보정 시뮬·곡선 · 커밋함 2191ba26**) / 2026-10-23 저녁 (**§5-30 증강 구름 = 맵에 소환되어 공격 · 화면 고정 그림 없앰 · 이펙트·아이콘 32개 플랫으로 다시 · 스케이터 생쥐 롤러스케이트 그림·동작 + 웹 미리보기**) / 2026-10-23 오후 (**§5-29 새 보스 쥐 히드라: 리깅·웹 확인·유니티 에셋·GIF 끝 · 사용자 OK · 커밋함**) / 2026-10-23 낮 (**§5-28 잘린 그림 다시 자르기 · 치즈 창고 흰 네모 아이콘**) / (**§5-27 증강 Ver.04: 아이콘 3차 적용 · 상시 그림/기차/칼날 투명 버그 고침**) / 2026-10-22 오후 (**§5-26 보스 리깅 고치기 2차: 통 그림 비교 · 팔 여러 개/목 원인 · 머라이언 · 차력거인 팬티·포탑 사격** / 2026-10-21 밤 **§5-25 필살기 쌓임 해결 · 컷인 대신 로그** / **§5-24 엔딩 컷씬 그림 5장 · 씬 연결 · Play 확인 · 커밋 끝** / §5-23 증강·필살기·암시장·뽑기 묶음 / §5-22 적정 찍찍 숨김 / §5-21 딜 미터기·쥐 목록) / 2026-10-21 (**§5-18 시작 층 상한 완화: 최고 클리어 −3 · 스킵 노드 · 조각 조건 · 31층~ 곡선 다시 뽑음**) / 2026-10-19 (**§5-16 초필살기 v2 아이스 퀸: 마지막 컷신 제거 · 플랫 배경 · 쥐/배경 따로** / **§5-13 증강 · 튜토리얼 여부 창 · 필살기 쿨 제거 · 밸런스 곡선 재설계 · 제한시간 120초 · 엑셀 시뮬레이터** / **100층 확장: 데이터·코드·그림·유니티 연결·Play 확인** §5-12) / 2026-10-17 — **§5-11 일괄 승급 6마리 · 마우스 포인터 · 쳇바퀴 훈련 필살기 대사 · 초필살기 계획** / 2026-10-15 — **§5-10 튜토리얼 라운드(1·2층) · 씹힘 방지 · 로비 슬로우 모션 · 치즈 창고 훈장 버튼 · 벽 체력 숫자 숨김** / 2026-10-13 밤 — **PV(홍보 영상) 작업은 `PV/HANDOFF_PV.md`** / **§5-8 브랜치 안내 먼저** / (뒷골목 숨김·조각 1개 · **스트링 테이블 + 영어 번역** §5-5) / 이전: 2026-10-12 밤 (아이스 퀸·5G 그림 · 5G 휴대폰 속 쥐 · 지하 도시 v4 · 뽑기 = 비밀 실험실 컷씬 · 지하/뽑기 배경음악 · **하수구 긴 세로 그림 · 아이스 퀸 고유 컷씬 · 컷 전환 통일(줌 인·플래시·벡터 눈보라)** §5-4 — 사용자 확인 · 커밋 남음)

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

**쥐**: 번식(확률 = 마리 수/최대 비율 곡선, 번식 때 하트) · 클릭 총공격(쿨 3초, 같은 대상 연타 감쇠) · 승급(필요 = 올림(1.5 × 1.3^k), 일괄 승급은 승급 뒤 6마리 남김 — 2026-10-17) · 필살기(쥐 개체별 게이지, Lv 7 해금, 하나 끝난 뒤 쿨 25초) · 슈퍼 점프(초당 1/480 확률) · 묘기 5종(공용 스킬로 해금). 최대 마리 = 30 + 둥지 노드(최대 175).

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

## 5-8. 브랜치 (2026-10-13) ★

- `main-game` (여기) = 메인 개발 · `mvp` = Firebase 랭킹·기록 + itch.io 웹 빌드 전용 (테스트 패널 없음) · `main` = 원격 기본, 그대로. 게임 수정은 여기서 하고 `mvp` 에 `git merge main-game`. **MVP 서버 진행 상황·다음 할 일은 `mvp` 브랜치의 HANDOFF.md §5-8 과 `서버 계획.md`.**
- 브랜치를 바꾸면 유니티에 "씬 다시 불러올까요?" 팝업 → 사용자에게 Reload 요청.

## 5-10. 튜토리얼 라운드 1·2층 (2026-10-15) ★

사용자 표(CBT 변경)대로 첫 안내들을 1·2층에 고정. **'튜토리얼 라운드'** = `Progress.TutoRoundSteps` (g_intro · g_wall · g_birth · g_promote · g_cat · g_floor) 를 다 볼 때까지 (`Progress.InTutoRound`, 예전 흐름으로 l_first 를 본 저장은 끝난 것으로). 판이 끝나도(게임 오버 · ESC 포기) 본 단계만 기록되고 라운드는 다음 판으로 이어짐 → **라운드를 다 봐야 로비 l_first(치즈 창고)가 나옴** (조건 `RoundDone`).

| 안내 | 지금 |
|---|---|
| 조작(g_intro) | 라운드 1층 시작 (조건 InRound) |
| 벽 파괴 + 맵(g_wall) | 1층 첫 벽 붕괴. 예전 g_floor 의 맵 대사(Line 5)를 여기로 옮김 → 대사 48 → 49(맵, 옛 5) → 91(옛 49) |
| 제한시간(g_floor) | 2층부터 새 발동 `Hurry`: 남은 시간 30초 이하 **또는** 계단 방 벽(가장 깎인 것) 30% 이하·계단 방 열림 (`StageManager.StairsWallRatio`) 중 먼저, 한 번만 |
| 번식(g_birth) | 라운드 동안 1층 번식 없음 (`RatManager.Breed`) → 2층 첫 번식 |
| 승급(g_promote) | ~~2층부터 10마리~~ → 2026-10-21 라운드에서 빠지고 최대 마릿수 도달과 함께 (§5-19) |
| 고양이(g_cat) | 안내 전엔 1층 고양이 없음 · 2층부터 층 들어오고 30초(`StageManager.tutoCatDelay`, 게임 시간) 뒤 반드시 등장, 못 보고 넘기면 다음 층에서 또 30초 |
| 슈퍼 점프 | 첫 판 · 라운드 동안 저절로 안 나옴 (`SuperJumpManager`, 테스트 버튼은 됨) |
| 쉬운 벽 | 1층 시작 방 옆 쉬운 벽(`firstRunWallMul`)은 첫 판 + 라운드 동안 유지 |
| 인구(g_pop · g_pop_f5) | 그대로 |

- 그 뒤 흐름: l_first(로비, 치즈 창고) → 다음 판 시작 g_power (조건 RunMin 2 → `Done l_first`) → 그 다음 로비 l_rats (조건 `Done g_power`).
- **씹힘 방지**: ① `TutorialBox` 연타 막기 — 줄이 뜨고 `clickGuard` 0.35초 클릭 무시 · 글이 다 나오고 `nextDelay` 0.45초 · 한 줄 최소 `minLineTime` 1초 (그때 '계속' 표시). ② FloorStart 창(6초)은 다른 연출·대사로 막혀 있던 시간을 안 셈 (`TutorialManager.freeT`). ③ 판 조건(RunMax 1) 대신 상태 조건이라 놓친 안내는 다음 판에 다시 기회.
- **치즈 창고 훈장 버튼**: 바로 훈장을 달지 않고 찍찍!! 훈장 페이지로 (`SkillPage.RankUp`). 훈장 기능이 잠겨 있으면 아지트로 나감 → l_rank(CanRankUp) 대사 → 끝나면 행동 `OpenPage rank`. 버튼 글 "찍찍!! 훈장 보러 가기!" / "Go to SQUEAK!! Rank!" (스트링 8000175).
- **로비 슬로우 모션**: `Time.timeScale` 이 씬을 넘어 남아서 역경직(0.05배)·필살기 정지 중에 로비로 가면 로비가 느려짐 → `FxManager` 가 씬이 바뀔 때마다 timeScale 1 · Paused/UltFreeze/WorldFreeze 끔.
- **벽 체력바**: 숫자 글 숨김 · 크림색 판 끔 · 빨간 막대만 (`WallBar.showLabel` · `hpColors` 둘 다 꺼 둠, 사용자: 벽은 빨강으로 구별).
- 테이블: 튜토리얼 테이블 Step(g_intro · g_wall · g_floor · g_birth · g_promote · g_cat · g_power · l_first · l_rats · l_rank) · Line(5·49) · Trigger_Type(Hurry) · Cond_Type(RoundDone · InRound), 스트링 테이블 8000175. 백업 `Data_Table/_backup_20261015`.
- **확인함 (Play, 테스트 슬롯 4)**: 1층 조작 안내 → 첫 벽 붕괴 안내(맵 포함) · 1층 번식·고양이 없음 → 2층 첫 번식 안내 → 10마리 승급 → 2층 30초 고양이 → 남은 30초 타이머 안내 → 라운드 끝(InTutoRound false) · 계단 벽 25% 일 때 StairsWallRatio 0.25 · 로비로 갈 때 timeScale 0.05 → 1 · 훈장 버튼 → 훈장 페이지. **안 본 것**: 계단 벽 30% 로 실제 대사가 뜨는 장면 · 훈장 잠김 상태에서 버튼 → l_rank → 훈장 페이지 · 라운드 중 게임 오버 뒤 로비(치즈 창고 안 열림) · 실제 마우스 연타.

## 5-11. 일괄 승급 · 마우스 포인터 · 대사 · 초필살기 계획 (2026-10-17)

- **일괄 승급** (사용자: 6마리만 남김): 예전엔 max(6, 최대 마릿수 × 0.5) 를 남겨 최대 30 이면 15마리였고, 실패 배너는 '6마리'로 잘못 표시 → `promoteKeepRatio` 0 (코드 기본값 · Game 씬), 배너 `{keep}` = 실제 남길 수(`PromoteKeepCount`). 낮은 등급이 남길 수를 넘으면 필요 수가 적은 윗등급을 봄 (`NextBulkGrade`). **일괄 승급 버튼은 남길 수를 지키며 할 수 있을 때만 켜짐** (`RatManager.CanPromoteAll`, 예전엔 개별 승급 가능 여부로 켜져서 눌러도 실패).
- **쳇바퀴 훈련 대사**: "필살기... 기억나지?"(대사 41)를 새 단계 `l_rats_ult`(After l_rats_page, 조건 Done g_boss_ult)로 → 첫 보스 필살기 시연을 못 본 플레이어에겐 안 나옴. 백업 `Data_Table/_backup_20261017`.
- **마우스 포인터** (`UnityResources/Rats/Cursor/README.md`): 쥐 머리 화살표 평소/누름 2장 (Codex) → `Tools/slice_cursor.py` → `Assets/Art/Cursor/` (텍스처 형식 Cursor, 48px). 설정 `Assets/Resources/CursorSkin.asset` · 코드 `Core/CursorSkin.cs` (게임 시작 때 저절로 붙음, 모든 씬, 왼쪽/오른쪽 버튼 누르는 동안 누름 그림). 유니티 꺼진 상태에서 meta 를 직접 써서 넣음 → **유니티 열어 임포트·Play 확인 필요**.
- **초필살기 1단계 (사용자: 계획서 제안대로)**: 테이블 쥐 캐릭터 `Super_Ult`(15종 이름·자막·motif) · `Super_Config`(unlock_level 10 · stack_need 5 · hit 10×4 · finish 20 · boss_cap 0.35 · 타임라인 0.4/1.9/3.9/4.6/5.0초), 스트링 42~44xxxxx(영어 번역 끝). 코드 `Ults/SuperUltManager`(스택 = UltimateManager.Stop 에서 · 종별 판당 1회 · 수동 · 화면 범위 피해 벽 제외 · 보스 상한 · 컷신 중 일반 필살기 대기 · 프레임당 0.05초 제한) · `SuperUltStyle`(쥐별 그림·색·입자·소리) · `SuperUltLayer`(레이어 키프레임). 씬 Game `HUD/SuperUlt`(SuperBar · Cut · Styles) + 루트 `SuperUltManager` + 테스트 패널 '고른 쥐 초필살기'. 튜토리얼 발동 `SuperReady` 코드만(단계·대사 아직).
  - **햄찌 예시 = 레이어 컷신** (사용자: 뽑기 컷 재활용 말고 메이플 오리진처럼 배경·움직이는 물체 따로 + 밈): 그림 `UnityResources/Rats/SuperUlt/` (README), 씬 `Cut/Frame/Scenes/ZapHam` (Intro · Act(띠 Mask) · Finish). 미리보기 GIF `SuperUlt/preview/zapham_super_v3_camera.gif` (v3 = 큰 세로 파노라마 + 가상 카메라 Rig/Track · 충격 kicks · 얼굴 클로즈업, 씬 코드 scratchpad 식 build3.cs). Style `titleOut` = 제목 띠 빠지는 시각. 사용자 확인 대기 → 나머지 14종 · 튜토리얼 · 쳇바퀴 표시 · 씬 새 글 스트링 등록(`import_scene_strings.py`).
  - **15종 진행 (2026-10-18, 유니티 꺼진 동안 코드·그림)**: 연출 틀 = `초필살기 구현 계획.md` §4. 그림 `UnityResources/Rats/SuperUlt/<code>/` (14종 Codex 일괄, `_run_log.txt`). 코드: TrainPage 초필살기 카드(superCard · sName · sDesc · sLock · superPathTag) · 튜토리얼 `g_super`(대사 92·93, 강조 id `super`).
    **2026-10-18 새벽 진행**: 사용자 — 그림체는 게임의 플랫한(언타이틀드 구스 게임) 그림체 유지(햄찌·뽑기 컷씬 수준, 1차 관광쥐 등 디즈니풍 → `Sheets/_old_v1`), 셀 애니식 합성 · 그림체만 통일 · **카메라 · 액션 · 이펙트는 쥐마다 개별**. → 프롬프트에 햄찌 승인 그림(`ref_zap_style/props/pano_style.png`) 첨부 · 가로 파노라마 7종(parkrat · knight · sultan · streamrat · jwerry · starchef · jjdetective) · 쥐별 연출 = `Tools/superult_story.py`(패턴: rain · rise · sweep · shower · spiral · bounce · burst_ring · stomp · converge · pops · trail, 얼굴 컷 full/slide/zoom/tilt). Codex 한도(02:48 풀림) → `_run_log3.txt` 로 자동 재개. 시험 적용: parkrat · emperor(임시 파노라마 a+b). 햄찌 ③ 땅 번개 · SuperBar 강조 id `super` · 쳇바퀴 초필살기 카드 · 번역 연결(`Tools/hook_superult_strings.py`) 끝.
    **15종 적용 끝 (2026-10-18 새벽 3시)**: 14종 그림 완성(Codex 한도 뒤 재개) → 쥐마다 씬 적용(코드가 5만 자 제한이라 한 마리씩 MCP) → Play 캡처로 15종 전부 처음~끝 확인 · 번역 연결(게임 19 · 로비 4, 밈 15종 영어). 남은 확인: 사용자 눈으로 연출 피드백 · 스트리머 상패가 유튜브 골드 버튼과 비슷함(바꿀지 결정) · ③ 바닥 고리가 화면 아래 한 줄로 늘어섬(취향).
    적용 = scratchpad 식 `apply.sh <code...>`(자르기 → 새로고침 → gen_superult_scene → MCP) · 확인 = `cap.sh <code...>`(Play 캡처).
    **유니티 열면 할 일 (순서)**: ① `python Tools/slice_superult.py <code>` 전부 → ② 새로고침 → ③ `python Tools/gen_superult_scene.py out.cs zapham <14종>` → MCP execute_code (Game 씬) → ④ SuperBar 에 TutorialTarget id `super` → ⑤ Lobby 쳇바퀴 훈련 카드 복제해 superCard 연결 · 글 → ⑥ 새 씬 글(밈 · {n}명 · 초필살기!! · 카드) `import_scene_strings.py` + 영어 → ⑦ 15종 Play 캡처 확인.
  - 테스트: 슬롯 5 + `SkipTutorial`, 유니티 창을 앞으로(뒤에 있으면 게임 시간이 거의 안 흐름), 캡처 = EditorApplication.update + ScreenCapture (컷신 시간 cur.t 를 파일 이름에).
- 참고: 유니티 없이 컴파일 확인 = Assembly-CSharp.csproj 를 복사해 Compile 을 `Assets/Scripts/**/*.cs` 로 바꾸고 출력 경로를 scratchpad 로 → `dotnet build` (오류 0 확인함).
- 이 수정은 `main-game` 브랜치만 (사용자: mvp 는 더 이상 업데이트 안 함).
## 5-12. 100층 확장 (2026-10-19) — 상세 기록 `기획서/100층 확장 구현 기록.md`

- **사용자 결정**: 16~30층 고양이 연구소로 교체 (25 해피 고양이 · 30 팝 고양이, 마녀·우주 사령관 floor 0) · 초월 승급 안 함 (새 친구 55종도 이번엔 없음) · 91~100 보스 = 94·97·100층 · 보스마다 새 밈 기술 (수정하기 쉽게 테이블 + 기록) · 훈장·스킬 임의 배분 · 밸런스 임의 · 그림 전부 Codex 시트.
- **데이터·코드**: 스테이지 100층 (31층~ ×1.12) · 구간 6 (70005 고양이 연구소 ~ 70010 우주 연구소, Zone `themes` 가구 테마) · 물건 108 (62xxx · 가구 63xxx) · 사람 24 (Human `to_floor`) · 보스 17 (95007~95023) · 밈 기술 54 (`Hazards/Boss.Meme.cs` 범용 패턴 17종, Atk_Type `pattern` 칸) · 훈장 9~16 (조건 4칸, Dex_Count · Boss_Clear) · 공용 스킬 9~16훈장 (자동 승급 · 승급 필요 수 감소) · 영어 채움. 내용 원본 = `Tools/expansion_content.py` → `gen_expansion_data.py`.
- **그림 (Codex 23장, 실패 0)**: `UnityResources/Rats/Expansion100/` (Sheets = 프롬프트·원본·로그, Items · Memes · Rank · Stage) + Humans/Cats Parts. 순서 `gen_expansion_prompts → run_expansion_codex.sh 4 → slice_expansion → gen_sprite_pivots`. 손본 것: 공룡탈 알바 팔(옆 칸 조각 섞임 → 다시 자름) · 포탑 고양이 (`CAT_DILATE=4` 로 잘라야 5조각).
- **유니티 연결 끝**: 사람/고양이 아트 라이브러리 (사람 37 · 고양이 24) · Game: ItemManager 그림 168 · Boss `memeSprites` 36 · StageManager `lastFloor` 100 · `zoneLooks` 70005~70010 · 승급 패널 자동 승급 토글 (`HUD/Promote/Panel/AutoToggle`, 패널 +70) · SceneMusic `finalBossFloor` 100 (Title·Lobby·Game) · Lobby: 훈장 그림 16 · 스킬 탭 16개 + 훈장 사다리 = 가로 스크롤 (`TabsView` · `LadderView`) · 조건 4줄 (`RankPanel/Cond4`) · 새 조건 라벨 (ReqDex/ReqBoss · CDex/CBoss) · 새 씬 글 스트링 키.
- **Play 확인 (2026-10-19, 슬롯 5 · 튜토리얼 건너뜀)**: 테스트 패널 방식으로 새 보스 17마리 × 두 번째 공격·스킬·발악 1·2 강제 발동 = 오류 0 · 16/31/46/61/76/91층 들어가기 = 구간·가구 테마·바닥/벽·물건·사람 정상. 캡처로 해피·팝·원시인·공룡탈·빨간 우주복·그럼피 · 46·91층 확인.
- **이 PC 에서 유니티 MCP**: HTTP 세션 대신 stdio 서버(`uvx --from mcpforunityserver==10.3.0 mcp-for-unity --transport stdio`)가 유니티 브리지 6400 에 붙음. `execute_code` 는 `manage_tools activate scripting_ext` 뒤 `action: execute`. 파이썬: 기본 python 에 scipy 없음 → 스크래치 venv (uv) 로 자르기.
- **로비 Play 확인 (2026-10-19, 슬롯 5 를 10훈장으로)**: 훈장 사다리·치즈 창고 탭이 고른 훈장으로 자동 스크롤 (`Lobby/ScrollTo.cs`, RankPage · SkillPage) · 훈장 조건 5줄(연구자료 + 4) 겹침 → ReqTemplate 높이 50→42 · 치즈 창고 승급 창 높이 +30, 승급 창 훈장 그림이 씬 고정(치즈) → 고른 훈장 그림으로 (`SkillPage.DrawRank`).
- **main-game 으로 옮김 (2026-10-19)**: 처음엔 이 PC 가 `main` 만 받아서 거기서 작업 → `expansion-100` 브랜치에 커밋 후 `main-game` 에 머지. 엑셀·JSON·씬은 main-game 것 + 생성기·연결 스크립트 다시 실행 (스트링 접두사: 초필살기 42~44 → 밈 팝업은 **45**). 건너뛰기 상한은 main-game 엑셀 값(20~25층 15→20) 유지하게 gen_stage_table 에 넣음. 자동 승급은 `CanPromoteAll`(번식용 6마리 남김) 기준.
- **마우스 포인터 고침 (§5-11)**: `RatSpriteImporter` 가 Art 아래 PNG 를 전부 Sprite·압축으로 바꿔서 커서가 안 바뀌었음 (로그 "Failed to set the cursor … not CPU accessible" · "Invalid texture used for cursor") → `Assets/Art/Cursor/` 는 Cursor 형식 · 읽기 가능 · 비압축 · 밉맵 없음. Play 에서 오류 사라짐 확인.
- **남음**: ① 사용자 플레이 피드백 (밈 기술 세기·연출) ② 밸런스 측정 (40층대부터 막힐 가능성 큼) ③ 새 구간 배경음악 (지금 16층~ 은 20층 곡) ④ 커밋.

## 5-13. 증강 · 튜토리얼 여부 창 · 필살기 쿨 제거 · 밸런스 측정 (2026-10-19)

- **사용자 결정** (메모리 `nkk-augment-balance-decisions`): 클리어 10~15시간(클릭 적극 10h / 거의 안 함 15h) · 허들(누적) 16층 2h · 30층 4h · 31층 증강 4h · 50층 6.5h · 70층 9h · 100층 12.5h · 측정 = 시뮬레이터 + 대표 층.
- **필살기 사이 쿨타임 없앰**: `UltimateManager.ultCooldown/ultCooldownMin` 0 (코드 기본값 + Game 씬). 쓸모없어진 공용 스킬 `Ult_CD` 노드는 `Ult_Gauge_Pct` 로 (gen_skill_tree).
- **튜토리얼 여부 창**: 타이틀 '처음부터' → 오프닝 끝 → `UI/TutoAsk` "튜토리얼을 진행하시겠습니까? 찍! (네!) / 찍찍!! (아니오)" (`TitleManager.tutoAsk/tutoYes/tutoNo`). 아니오 = `Progress.SetTutoQuiet` → 대사·행동·튜토리얼 라운드 제한 없이 단계만 조용히 지나감(해금은 원래 조건에서). 스트링 8000472~473.
- **증강** — 테이블 `증강 테이블.xlsx`(Augment 51 · Grade 4 · Offer · Aug_Config · No_Scale, 생성기 `Tools/gen_augment_table.py`) + **`증강 특수 효과 테이블.xlsx`**(Special 12 · Special_Type, `Tools/gen_augment_special.py`). 스트링 접두사 46(이름) · 47(설명) · 48(등급).
  - 등급 = 브론즈 · 실버 · 골드 · 프리즘 (등급마다 풀이 다름). 효과 = 공용 스킬 효과 타입 그대로(장착하면 `CommonSkill` 이 노드처럼 더함) + `Special`(칼바람식: 거대 쥐 · 눈덩이 · 회전 치즈 칼날 · 천둥 번개 · 클릭 폭격 · 세포 분열 · 시간 역행 · 필살기 메아리 · 토네이도 · 연쇄 붕괴 · 승급 폭죽 · 잭팟, 높은 등급 위주). 새 효과 타입 `Research_Pct` · `Aug_Slot`.
  - 흐름: 31층 도달 해금 → 31층 이상 클리어 때 다음 층 시작에 3장 중 1장 (게임 멈춤) → 바로 보관함(`Progress.aug`)에 쌓임 → 슬롯(기본 5 + `Aug_Slot` 노드 10·12·16훈장)이 비면 장착, 꽉 차면 보관함 → 판 중 Tab/HUD 버튼 = 관리 창(교체·빼기·합성, 판 안 합성 재료는 보관함에서도 사라짐) → 층 클리어 때 장착한 증강마다 강화 포인트(클리어 층 × pt_per_floor) → 로비 지하 '증강 연구소'에서 포인트 + 치즈로 강화(Lv 5, 레벨당 +10%) · 합성(같은 등급 N개 → 상승/유지/하락) · 도감(못 얻은 건 ???) · 41층 도달 시 '들고 가기' 1개.
  - 코드: `Core/Augments.cs`(공통) · `Augment/AugmentManager.cs`(판 안 UI) · `AugmentSpecials.cs`(특수 효과, 훅 = ItemManager.OnSmashed · RatManager.ClickRush/Promote · StageManager.BreakWall/ChipWall · RunTimer 0초 · UltimateManager.Stop · Heist · Rat.Damage/LateUpdate) · `AugmentCard.cs` · `AugmentArt.cs`(Resources/AugmentArt.asset, Fill Icons) · `Lobby/AugmentPage.cs`. `FeatureGate.showWhenLocked` 추가.
  - 씬: Game `HUD/Augment`(Bar · Pick · Manage) + `AugmentManager`(+AugmentSpecials) · Lobby `Alley/AugLab`(건물, x 5.9 — 해금 전엔 찻주전자·우유곽 장식이 그 자리) · `Home/Hots/Alley/Hot_aug`(id aug) · `UI/Prep/Body/Page_aug`. GameDatabase 3씬 모두 augmentTable · augmentSpecialTable 연결. FxManager 스티커 `cheese_blade`.
  - 그림 (Codex, 기존 공용 스킬 아이콘·비밀 실험실 그림체에 맞춤): `UnityResources/Rats/Augment/`(아이콘 51 + ??? + 건물 + 칼날, Sheets = 프롬프트·원본, `_v1` = 처음 뽑은 벡터풍) → `Assets/Art/Augment/` · `Art/Rats/Lobby_Market/aug_lab.png` · `Art/FX/fx_cheese_blade.png`.
  - 튜토리얼: `g_aug`(발동 AugPick) · `g_aug_manage` · `l_aug`(해금 aug) · `l_aug_page` · `l_aug_carry`, 대사 94~101, 강조 id augpick · augbar · aug.
  - Play 확인 (슬롯 5): 고르기 창 · 특수 효과(칼날·거대화·토네이도·번개) · 관리 창 교체 · 로비 지하 건물 · 페이지 강화/들고 가기/합성/도감. **실제 31층 첫 클리어 흐름·튜토리얼 대사·영어 화면은 아직 안 봄.**
  - **UI 다듬기 (2026-10-20, 사용자: 증강 슬롯 그림 · 관리 UI)**: Codex `UnityResources/Rats/Augment/Sheets/ui/` (UI 키트 그림체) → `Tools/slice_aug_ui.py` → `Art/Augment/` `aug_frame_1~4`(브론즈 · 실버 · 골드 · 프리즘 테두리) · `aug_slot_empty`(+) · `aug_slot_lock`(자물쇠) · `aug_ui_panel`(관리 창 나무 판) · `aug_ui_shelf`(HUD 선반), 9칸 늘리기 테두리 = `borders.json` (임포트 때 spriteBorder).
    코드: `AugmentArt.emptySlot/lockedSlot` · `AugmentCard.SliceFit`(테두리 두께 = 칸 짧은 변 14%, 최대 34px · 그림이 있으면 Inner 숨김) · `AugmentManager` 잠긴 슬롯 미리보기(증강 설정 `slot_max` 기본 8) · `detailHint` · `storageEmptyNote` · 장착 초록/빼기 빨강(`equipSprite/unequipSprite` = UI_Kit plank). 씬 Game `HUD/Augment`: Manage/Card 판 1600×940 · 리본 머리띠(장착 · 보관함) · 보관함 코르크 · 슬롯 줄 0.86배 · DetailHint · Storage/EmptyNote · Bar/Shelf. 상세 · 고르기 카드 글 폭을 테두리 안으로.
    스트링 8000510~511 (안내 2개, 영어 포함) + 8000512 '총 피해 {n}' = 'Total {n}'. 캡처 `Augment/Sheets/ui/after_v2.png`.
    **레이아웃 정리 (같은 날, 사용자)**: HUD 증강 줄 = 승급 버튼(`HUD/Promote/OpenButton`) 같은 줄 오른쪽, 사이 24. 관리 창 1600×980 · 나무 테두리 안쪽 여백 60 · 칸 사이 24 격자: 머리 줄(제목 · 닫기) / 왼쪽 칸(장착 리본 · 슬롯 0.86배 8칸 · 보관함 리본 · 코르크 보관함 132×156 두 줄) / 오른쪽 칸 400(자세히 · 장착 버튼) / 아래 줄(합성 모드 · 합성하기 · 안내). 버튼 높이 70 · 글 최대 28 자동 줄임(최소 16, 안쪽 여백 좌우 20) · 나무 판(합성하기 · 장착 = 초록). 고르기 카드 사이 48 · 글 폭 = 카드 − 96. 씬 패치 원본 = scratchpad 식 aug_layout.cs. 캡처 `after_v4_layout.png`.
    **윗판 두껍게 (같은 날, 사용자: 제목 · 닫기가 나무판 위에)**: `aug_panel` 을 Codex 로 다시 뽑음(윗변 두 겹 판자 144px · 옆 60 · 아래 72, 옛 판 `Sheets/ui/_old/aug_panel_v1.png`) → 테두리 80 · 90 · 80 · 166. 창 1600×1010 · 제목(크림 굵은 글) · 닫기 = 윗판 가운데(y −78) · 내용 여백 옆 78 · 위 166 · 아래 88 · 슬롯 0.83배. 캡처 `after_v5_board.png`.
    **되돌림 · Tab 정리 (같은 날, 사용자)**: 두꺼운 윗판 시안은 되돌림(처음 판 그림 · 여백 60 · 창 1600×980, 시안 = `Sheets/ui/_old/aug_panel_v2_thick.png`), 제목 '증강 관리' = 윗판(25~81) 가운데 y −53 · 크림 굵은 글. HUD 선반 제거 · 증강 버튼 = 승급 버튼과 같은 크기 · 그림 · 글('증강', 스트링 8000513 'Augments', '(Tab)' 지움) · 닫기 = 둥근 X 버튼(ui_btn_round, 윗판 위 x −150). **관리 창은 게임을 멈추지 않음**(고르기 창만 멈춤). **증강 슬롯 = 2칸 시작 · Aug_Slot 노드 3개(10 · 12 · 16훈장)로 한 칸씩 · 최대 5** (증강 테이블 slot_base 2 · slot_max 5, `Augments.SlotCount` 상한). balance_sim · gen_stage_table `AUG_SLOTS` 도 2~5로 바꿨지만 **스테이지 곡선은 아직 다시 안 뽑음** (증강 배율이 줄어 31층~ 적정이 낮아져야 함). 캡처 `after_v7_tab.png`.
    **나무 1.5배 · 제목 · 닫기 (같은 날, 사용자)**: 관리 창 판 9칸 테두리를 1.5배로(`pixelsPerUnitMultiplier` 1/1.5 — 그림은 처음 것 그대로) · 창 1600×1020 · 안쪽 여백 옆 100 · 위 140 · 아래 104 · 제목 = 윗판 가운데(y −79.5) 크림 이름표(ui_bar_white) 위 진한 갈색 굵은 글 · 닫기 = 다시 나무 표지판(lb_plank, 윗판 위 x −260, 스트링 8000011). 캡처 `after_v9_title.png`.
- **아웃게임 윗줄 (2026-10-20, 사용자)**: Lobby `UI/Prep/TopBar/HomeButton` '아지트로 쪼르르' → **'아지트로'**(스트링 8000514 'To Hideout', 옛 8000037 안 쓰임) · 왼쪽 → **오른쪽 위 끝**(폭 157 = 글 폭 + 60) · 돈 표시(Money)는 그 왼쪽 16 · 탭 줄은 왼쪽 24 로. 모든 준비실 페이지가 같은 윗줄을 씀.
- **밸런스 — 곡선·비용 재설계 (2026-10-19 오후, 시뮬레이터 기준 · 유니티 측정은 아직)** — 상세 `HANDOFF_LOG.md §9-22`. 측정 원본 round9 (`Tools/probe_results/round9_summary.md`).
  - **사용자 결정**: 기준 = 클릭 적극 10h / 거의 안 함 15h · **콘텐츠 해금 직전 허들 → 해금 뒤 성장 체감** · 31층~ 시작 층 = 최고 클리어 −4 (넘은 허들 아래로는 안 감) · 9~16훈장 '제한시간 +' 노드는 다른 효과로 · **기본 제한시간 180 → 120초** + 제한시간 노드 전체 축소 · 9훈장 자동 승급은 그대로.
  - 곡선 (`gen_stage_table.py`): 1~19층 기획서 그대로. 20층~ = 허들 층(30·40·…·90·100 = 다음 훈장 조건 층 −1, 증강·들고 가기·새 훈장 직전) 은 추세 × 1.6, 넘은 다음 층부터 추세로 내려와 전투력/적정 ≈ 1.3배. 추세 앵커 = 훈장 끝 전투력(`probe_results/power_by_tier.json`) × 증강 배율. 허들 층 목표 초 = 그 훈장 끝 제한시간의 70%. 적정: 30층 40만 · 50층 311만 · 70층 1,810만 · 100층 7,140만 (예전 8.1억 · 7.2B · 64B · 1.7T).
  - 제한시간: Game 씬 `RunTimer.baseTime` 120 (코드 기본값·BalanceProbe.BaseTime 도). 1~8훈장 Time_Add 절반(8훈장 합 +60초), 9~16훈장 Time_Add → 생명체 치즈 % · 황금 물건 %. 층 목표 초도 120초 기준 (72·90·108초).
  - 비용: `gen_economy.COST_SCALE` 2~5훈장 ×1.3 · 6~8훈장 ×0.9 · `gen_expansion_data.LATE_COST_SCALE` 9~16훈장 ×1.95. 스킵 노드 9~16훈장 (7,6)·(5,5)… (누적 9훈장 40 → 15훈장 100).
  - **경제 전체 (사용자 "전부 다")**: ① 치즈 — 7훈장~ 치즈가 트리 값의 10~45배 남았음 → `gen_skill_tree` 7훈장~ 노드 치즈 = 노드 연구자료 × 그 훈장 층대 (치즈 수입÷연구자료 수입) × 0.85 × 2 → 승급 때 남는 치즈 0.2~0.7배. ② 상점 — 가격이 층 수입의 0.1% 수준이었음 → `BlackMarket.BC` 에 층 수입 배율 `bc_floor_grow`^(min(층,30)-1) (상점 테이블, gen_economy 가 씀: bc_base 6.76 · grow 1.2213 = 6칸 전부 ≈ 층 수입 3배). ③ 증강 강화 치즈 — 같은 배율(`up_cheese_floor_grow`) + up_cheese_base = 층 수입 0.15배 (`gen_augment_table`). ④ **조각** — 출생마다 +1 이라 조각 합이 훈장 조건의 ~4배 (쥐 성장 '갉기'는 레벨마다 공격 ×1.25 → 측정보다 훨씬 셈) → 사용자 결정 '레벨업 조각 비용 올림': `Tools/gen_shards.py` shard_k ×10 (일반 20 … 신화 5), 뽑기 조각 ×10, 암시장 묶음 ×10 → 승급 때 조각 합 ≈ 조건 1.1~1.4배. ⑤ 보스 — `Tools/gen_boss_hp.py` hp_pow_sec = 예전 값 × (새 보스전 목표 ÷ 예전 목표): 5층 20 · 10층 22 · 20층 28 · 허들 보스 41~65.
  - 생성 순서 (전부): `gen_stage_table → gen_economy → python -c "import gen_expansion_data as g; g.tiers()" → gen_skill_tree → gen_augment_table → gen_boss_hp → gen_shards → gen_string_table → fill_en_by_pattern → xlsx2json` → `balance_sim_xlsx`.
  - 시뮬 결과 (최종): **클릭 10.8h / 미클릭 14.5h** · 클릭 16층 1.8h · 30층 4.2h · 50층 6.4h · 70층 8.4h / 미클릭 2.4 · 6.4 · 9.2 · 11.7h · 후반 허들마다 2~3판 (미클릭 30층 5판) · 판 끝 병목이 치즈·연구자료·조각으로 고루 나뉨.
  - **엑셀 시뮬레이터** `Data_Table/밸런스 시뮬레이터.xlsx` (`python Tools/balance_sim_xlsx.py`): 설정(노란 칸) 고치고 저장 → 실행 → 요약·판 시트. 층별 시트 = 훈장 끝 상태 층별 초 (엑셀 수식, 실패 빨강). 유니티 안 씀.
  - **남음**: ① 유니티 대표 층 측정(31·41·51·71·100층, 120초 기준 벽이 2/3 로 얇아짐) → power_by_tier.json · 시간 모델(TIME_C 등) 보정 ② 가정값 — '거의 안 함' 배율(PASSIVE_SLOW 1.35) · 층당 출생 ≈ 최대 마릿수(BIRTH_PER_POP 1) · 1~9훈장 최대 마릿수 일부 · 30층 뒤 치즈 수입 배율 고정 → 측정 필요 (엑셀 설정에서 바꿔 보기) ③ 16층 뽑기 허들 2판 이상 원하면 6~8훈장 비용 조정 ④ rat_atk_mul(티어 테이블) 은 코드에서 안 쓰임 (훈장 전투력은 트리·돌연변이 배율로만) ⑤ 커밋.

## 5-14. 초반 곡선 · HUD 적정 표시 (2026-10-19 저녁, 커밋 안 함)

- **문제 (시뮬레이터 balance_sim + 전투력·적정 겹친 그래프)**: 7~15층이 층마다 한 번씩 막혀 판마다 1층씩 전진 · 18층(허들)·19층(어려움) 연속 벽(각 약 0.5h) 뒤 20~29층은 19층보다 낮은 평지 · 새 층을 테이블 적정의 약 0.8배에서 깨서 HUD 찍찍!! 이 늘 부족해 보임.
- **곡선 (`gen_stage_table.py` `_early`, 사용자 "고쳐 줘")**: 1~29층 = 기본선 × 허들. 2~4층 ×1.25 · 6~16층 ×`EARLY_G` 1.26 · 17~29층은 30층 추세까지 기하 보간. 허들 = 해금 직전 층만 `EARLY_HURDLE` {5: 1.8, 10: 1.8, 15: 3.0, 19: 3.5} (다음 층에 안 남음 → 넘으면 적정이 크게 내려가 시원하게 뚫림). 16·18층은 6·7훈장 조건 층(도달)과 겹쳐 도달하자마자 승급해 허들이 안 먹어서 뺌. 그 밖 보스 층(20·25) ×1.15. 30층~ 은 그대로. 예전 "1~19층 기획서 그대로" 결정을 이번 요청으로 바꿈.
- 연구자료가 적정에 비례해서 `gen_economy` 가 2~8훈장 비용도 다시 계산함 (승급 연구자료 160·200·432·856·2694·11021 → 105·119·218·525·1302·3223).
- **HUD 적정 표시**: ~~`StageManager.needShowK` 0.8 고정~~ → **층마다 계산 (2026-10-19 밤)**. 고정 0.8배는 실제 '깰 수 있는 선'(시뮬 기준 1~9층 0.61배 · 5층 0.80배 · 제한시간 노드 쌓인 40층~ 0.47배)과 안 맞아 후반엔 충분히 깨는데도 찍찍!! 이 적정의 60% 로 부족해 보였음. 이제 `StageManager.PowNeedShown(f)` = 테이블 적정 × r, r = 이 층 제한시간(`RunTimer.FloorTime`, 공용 스킬 시간 포함) × `needShowTimeFrac` **0.85** (사용자 결정: 적정이면 제한시간 85% 쓰고 깸) 를 목표 초(`target_sec`)로 나눈 시간 배율이 되는 전투력 비 (식 = `balance_sim.time_mul`: 0.8 × r^-1.5 / r^-0.5, 인스펙터 `needShowTimeC`·`needShowPowLo/Hi`). `needShowK` 는 목표 초 없을 때만. 표시만 바뀜 — 벽 감산·보스·고양이 체력은 테이블 적정 그대로. 시뮬: 클릭 플레이어 실패 판은 전부 표시 1.0배 미만(최대 0.9), 처음 깬 층 ≥ 0.9배 · 미클릭은 1.0~1.07배에서도 실패할 수 있음(35% 느린 가정). Play 확인: 테스트 저장(제한 +60초)에서 1층 0.52배 · 5층 0.62배 · 50층 0.77배. (테이블 적정 자체를 낮추고 벽을 두껍게 하는 방식은 시뮬레이터 시간 모델에서 미클릭 플레이어가 5층을 못 넘어 되돌림)
- **결과 (balance_sim)**: 막히는 곳 = 허들 층(5·10·15·19·30…) 위주, 7~9·11~14·16~18층 막힘 0 · 클릭 100층 9.8h (예전 10.8) · 미클릭 11.85h (예전 14.5). 그래프 데이터 = scratchpad 식 overlay (balance_sim 을 층마다 기록).
- **49·50층 연속 벽 · 100층 4~5판 (같은 날 이어서)**: 원인은 곡선이 아니라 **조각 레벨 합 승급 조건** — 트리·연구자료는 다 됐는데 10훈장 '조각 합 240'·15훈장 '690'을 못 채워 승급이 1시간 넘게 밀림 (그동안 이전 훈장 전투력으로 49·50층 / 100층을 상대). `expansion_content.TIERS` 9~16훈장 조각 조건 = 시뮬레이터에서 조건 층에 닿을 때 조각 합(클릭·미클릭 중 작은 값)의 약 95%: 170 · 200 · 310 · 370 · 450 · 510 · 600 · 690 (예전 180 · 240 · 310 · 390 · 480 · 580 · 690 · 810). 결과: 클릭 49층 막힘 3판 → 1판 · 100층 4~5판 → 1판 · 클릭 100층 8.8h · 미클릭 11.8h.
- **19층 적정 (같은 날, 사용자 "19층 적정 이상함")**: 예전 19층 = 기본선 ×3.5 → 일반 층인데 18층의 4.4배 · 20~24층보다 높음 · 벽·연구자료도 혼자 튐 (8훈장 조건이 20층 도달이라 허들을 19층에 뒀던 것). → 8훈장 조건 **21층 도달**(20층 보스 클리어, `gen_economy.TIER_CLEAR[8]` 20) · 허들을 **20층 보스**로 · `EARLY_HURDLE` {5: 1.8, 10: 1.8, 15: 2.0, 20: 2.0} (허들은 앞 층의 2.5배 안쪽, 다음 층은 0.63배). 이제 허들 = 전부 보스 층 (5·10·15·20·30·…·100), 다음 훈장 = 그 보스를 깨고 다음 층 도달. 19층 6.94만 → 1.98만. 결과: 클릭 8.8h · 미클릭 11.6h, 막힘 = 보스 허들 층 1~3판.
- **남은 것**: ① 클릭 적극이 8.8h 로 목표 10h 보다 짧음 (사용자: 그대로 둠) (원하면 허들 배율이나 비용으로 늘리기) ② 미클릭 100층 2판 · 30~90층 허들 2~4판 ③ 유니티 Play 확인 (120초 · 1~29층 실제 플레이 — 표시 적정 계산은 확인함) ④ 백업 `Data_Table/_backup_20261019b` ⑤ 예전 세션의 다른 재밸런싱(옛 브랜치 기준)은 stash `claude-stale-rebal-20261019` · `C:\Project\NKK\rebal_claude_20261019` — 버려도 됨.

## 5-15. 훈장 해금 = 큰 허들 · 보스 = 가벼운 허들 (2026-10-19 밤) ★ — 위 §5-14 곡선 구조를 대체

- **사용자 결정**: 보스는 가벼운 허들 · 훈장 해금 시기를 플레이 시간에 고르게 흩뿌리고 훈장 해금을 허들로 써서 성장 체감 · 최종 훈장(16)은 85층 부근 해금 · 16훈장 모든 콘텐츠(트리) ≈ 92층.
- **한 곳에서 관리 = `Tools/tier_gates.py`** (gen_stage_table · gen_economy · gen_expansion_data · gen_skill_tree 가 읽음): `GATE` 훈장 조건 층(Max_Floor 도달) 2~16 = 6 · 10 · 14 · 19 · 24 · 29 · 35 · 41 · 47 · 53 · 59 · 65 · 71 · 78 · **85** (16훈장 예전 = 100층 클리어) · `T16_FULL` 92 · `RANK_K` 3.6 · `TREE_K` 0.75 · `T16_TREE_K` 0.15 · `T16_R` 1.0 · `POST_G` 1.03 · `FINAL_SPIKE` 1.4 · `SHARD`(조각 조건).
- **곡선 (`gen_stage_table._curve`)**: 훈장 허들 층 = 다음 훈장 조건 층 − 1 (5 · 9 · 13 · 18 · 23 · 28 · 34 · 40 · 46 · 52 · 58 · 64 · 70 · 77 · 84) + 92층(16훈장 트리 다). 허들 층 적정 = 그 훈장 끝 전투력(power_by_tier.json) × 증강 ÷ `HURDLE_R` 1.0, 넘은 뒤 `HURDLE_SPIKE` 1.3 만 내려감 (1.6 이면 이전 훈장으로 6~13층 더 뚫어 승급 체감이 흐려졌음). 보스 층 `BOSS_SPIKE` 1.15 (가벼운 허들), 100층 최종 보스 ×1.4, 92층 뒤 층당 ×1.03. 예전 EARLY_G·EARLY_HURDLE·MID_SPIKE·보스 허들 구조는 없앰.
- **비용 통일**: 2~16훈장 승급 연구자료 = 조건 층 연구자료 × `RANK_K` (예전 2~8훈장 = 기획서 판 일정 방식은 30층까지만이라 조건 층을 못 옮김 — `gen_economy` 는 1훈장 트리 치즈·상점만 판 일정으로) · 트리 연구자료 = 다음 승급 × `TREE_K` (16훈장 = 16훈장 승급 × 2.5 × `T16_TREE_K`) · 노드 치즈 = 층대 치즈/연구자료 수입 비율 (`gen_skill_tree.CHEESE_RATIO_FROM` 7 → 2).
- **조각 조건** = 시뮬에서 조건 층에 닿을 때 조각 합(클릭·미클릭 중 작은 값) × 0.9: 10 · 20 · 30 · 55 · 90 · 125 · 200 · 220 · 275 · 295 · 335 · 370 · 420 · 440 · 495.
- **결과 (balance_sim)**: 클릭 100층 8.8h · 미클릭 11.3h · 승급 0.3~1.0h 마다 (클릭 0.47 · 0.75 · 1.09 · 1.66 · 2.3 · 3.26 · 4.2 · 4.65 · 5.12 · 5.87 · 6.45 · 7.25 · 7.6 · 7.96 · 8.37h) · 막힘 = 훈장 허들 층 0.2~0.8h, 보스 층은 거의 안 막힘 · 16훈장 = 85층 도달 판 뒤 91층에서 승급, 92층 막힘 → 미클릭은 91층에서 트리 다 찍고 넘음, 클릭은 트리 절반으로 넘고 마지막 판에 다 찍음 (92층을 더 높이면 미클릭이 영영 못 넘음).
- **알아 둘 것**: 승급은 조건 층보다 2~6층 뒤 (조건 층을 넘은 판이 몇 층 더 가고 로비에서 승급) · 7→8훈장 간격만 약 1h (6→7훈장 전투력 차 ×3.8 이 커서 8훈장 비용이 튐) · 100층 최종 보스 체력 계수 36 (어려움 유형 보스전 목표) — 더 세게 원하면 gen_boss_hp · 시뮬은 power_by_tier.json(훈장별 전투력 추정) 기준이라 트리 효과 값을 바꿔도 시뮬엔 안 보임 → 실측 필요.
- 생성 순서 그대로: `gen_stage_table → gen_economy → gen_expansion_data.tiers() → gen_skill_tree → gen_augment_table → gen_boss_hp → gen_shards → gen_string_table → fill_en_by_pattern → xlsx2json`.

## 5-16. 초필살기 v2 — 아이스 퀸 · 기사 쥐 (2026-10-19)

- **사용자 결정**: 셀 애니(레이어) 방식은 유지 · **맨 마지막 컷신(④ 피니시 컷 = 파노라마로 돌아가는 카메라 + 피니시 포즈) 제거** · 연출 전체 5초 · 그림은 Codex 로 **쥐와 배경 따로**, 배경도 **플랫**(로비 배경 그림체) · 먼저 아이스 퀸으로 예시.
- 그림: `UnityResources/Rats/SuperUlt/icequeen/Sheets/v2/` (프롬프트·로그·`run_iq.sh`) → Sheets 로 복사 → `slice_superult.py icequeen` (scipy 없으면 `uv run --no-project --with scipy --with numpy --with pillow python …`) → `Assets/Art/Rats/SuperUlt/icequeen/`. 옛 칠 그림 = `Sheets/_v1_painted/`. 클로즈업 = `face_bg`(배경) + `face_close`(쥐만, 키잉).
- 코드: `SuperUltStyle.noFinishCut` · `actEnd`(4.5) — 켜면 ③ 이 actEnd 까지 이어지고 그때 마지막 일격(피해·흔들림·소리만, 피니시 그림·펀치 인 없음) → t_end(5.0)까지 걷힘. 다단 10타는 ③ 길이에 고르게. `Tools/superult_story.py` `NO_FINISH` 표 + icequeen 새 연출(큰 고드름 · 파편 · 밈 글은 화면 아래) · `gen_superult_scene.py` 가 Style 에 값 넣음. 유니티 밖 컴파일 오류 0.
- **씬 적용 · Play 확인 끝 (2026-10-19)**: `gen_superult_scene.py out.cs icequeen` → execute_code (Game 씬 저장). 씬 `Scenes/IceQueen` = Intro · Act 만 (Finish 없음), 그림 빠짐 0. Play(슬롯 5, `TestSuper("icequeen")`) 캡처 `UnityResources/Rats/SuperUlt/preview/icequeen_v2_caps.png`: 컷인 → 클로즈업(배경+쥐) → 띠 + 고드름 10타 → 4.5초 일격(하얀 번쩍) → 걷힘, 끝까지 5.0초(실측 5.28초). 콘솔 오류 없음 (WebAtlas `Rats_SuperUlt_icequeen` 크기 경고는 예전부터).
- **이 PC MCP (2026-10-19)**: Claude 쪽 UnityMCP 가 실패로 남으면 `uvx --from "mcpforunityserver==10.3.0" mcp-for-unity --transport http --http-url http://127.0.0.1:8080 --project-scoped-tools` 로 서버를 띄우고 유니티 MCP 창(Window > MCP for Unity > Toggle MCP Window)에서 Start Session → HTTP 로 직접 호출 (한 세션 안에서 `manage_tools activate scripting_ext` 뒤 execute_code).
- **띠 없앰 → 화면 전체 컷신 (2026-10-19, 사용자: 맵 전체를 덮는 컷신 + 피해가 게임에 들어가는 느낌)**: ③ 도 화면 전체 = 얼어붙은 호수(pano_alt)를 카메라가 따라가며 여왕 활강. 고드름이 꽂힐 때마다 **그 타에 실제로 준 피해 숫자**가 꽂힌 자리에서 튀어 오름 + **누적 피해 '총 피해 {n}'** 이 셈 + 화면(얼음 유리)에 **금**(`fx_crack1~3`, 진한 파랑)이 하나씩 쌓임 → 4.5초 큰 고드름에 **산산조각**(`fx_shard1~3` 사방으로) → 게임 화면이 드러나고 화면 안 고양이·보스·사람 2.5초 꽁꽁.
  - 코드: `SuperUltStyle` hitNumbers · hitPoints(화면 px, 마지막 = 일격) · hitShake · finishFreeze · finishFlash / `SuperUltManager` dmgTemplate · total(자리 {n}) · dmgRise/Life/Pop/FinishScale · `Damage` 가 준 피해 합(`lastDealt`, 보스는 상한 뒤 실제 값) · `FreezeView`. 씬 `HUD/SuperUlt/Cut/DmgNumber`(꺼 둠) · `TotalDamage`.
  - 그림: 금·파편 시트 `icequeen/Sheets/v2/crack.png` (Codex, 자홍 3×2) → `slice_superult.py` `GRID_CRACK`.
  - Play 확인 (캡처 `SuperUlt/preview/icequeen_v2_caps.png`, 콘솔 오류 없음, 실측 5.1~5.3초). 1층 테스트에선 첫 타에 화면 물건이 다 부서져 뒤 타는 피해 0 → 숫자는 대상이 있을 때만 뜸 (누적은 그대로 보임).
- **셀 애니 · 컷 8개 · 플랫 이펙트 (2026-10-19 3차, 사용자)**: 쥐 = **리깅 파츠를 바탕으로 그린 동작 프레임**을 바꿔 끼움 (`anim_land/skate/spin/dash/twirl` 시트 × 3프레임 = 15장, Codex 참고 = 고해상도 리그 파츠 `rig_*` + 승인 포즈). 큰 파노라마 + 카메라 무빙은 유지, 컷 전환 0.4~0.65초마다 8컷 (① 오로라 틸트 ② 발코니 착지·마법 ③ 얼굴 ④ 활강 ⑤ 도약·회전 ⑥ 바닥 클로즈업 돌진 ⑦ 소환 ⑧ 가리킴 → 산산조각). 이펙트·소품도 플랫으로 다시 (`fx_flat.png` · `props_flat.png`, 옛 빛 그림 = `Sheets/_v1_painted/out_*`).
  - 코드: `SuperUltLayer.swapResize`(프레임마다 그림 크기 달라도 같은 픽셀 배율, 피벗 = 발끝) · 첫 그림 되돌리기 · `superult_story.Story.cel` · `slice_superult` anim 시트(덩어리 기준으로 프레임 나눔) · `gen_superult_scene` 코드 압축(5만 자 제한). 제목 띠 0.95초에 빠짐(titleOut).
  - `SuperUltRig`(UI 관절 리그 컴포넌트)와 `rig_*` 고해상도 파츠는 만들어 두었지만 지금 연출에선 안 씀 (셀 프레임 방식으로 바꿈).
  - **인게임은 안 건드림** (사용자): 앞에서 넣었던 '일격 뒤 적 꽁꽁(finishFreeze)'은 뺌. 피해 숫자는 이미 들어간 피해를 읽어 보여 주기만.
- **초필살기 = 얼음성 → 우박 눈보라 (2026-10-19 4~5차, 사용자)**: 렛잇고 필살기의 '얼음성 → 하늘에서 눈보라 광역' 아이디어를 초필살기로. **일반 필살기(렛잇고)는 원래대로** (한때 발레로 바꿨다가 사용자 요청으로 코드·자막·영어 전부 되돌림 — UltRatItGo.cs 는 커밋 상태 그대로, 꽁꽁도 그대로).
  - 컷신 (컷 6개, 컷신 시간 3.4초 × **재생 속도 0.75배** `SuperUltStyle.playRate` → 실제 약 4.5초 + 걷힘): ① 오로라 틸트 ② 언 호수에서 소환 ③ 얼굴 ④ 하늘에서 얼음성(`prop_castle`, Codex `Sheets/v2/castle.png`)이 빈 호수로 쿵 · 세상이 얼어붙음 ⑤ 파노라마 속 성에서 눈보라가 하늘로 ⑥ 하늘에서 **우박**(얼음 조각 `fx_shard2/3` · 비스듬한 줄)이 쏟아져 하얗게.
  - **컷신이 걷히면 게임 화면 우박 3초** (제 속도): 다단 10타 · 대상마다 피해 숫자(월드 팝업) · 바닥에 우박 알갱이가 튐(`hailCount`) · 화면 안 고양이·보스·사람 꽁꽁(`freezeAll`, 끝나고 1.5초 더) → 마지막 일격 '전부 꽁꽁!!'. 전체 실측 약 8.3초.
  - 코드: `SuperUltStyle` worldPhase · cutFade · worldTime · blizzard* · hailCount · freezeAll · playRate / `SuperUltManager` 다단 구간 H0~H1 분리 · `WorldBlizzard` · `WorldNumbers` · `FreezeView` · 화면 전체 우박 오버레이 `worldSnow`(씬 `HUD/SuperUlt/WorldSnow` + Dot · Flake=fx_shard3, 컷신 밖, 바람 거의 수직). `superult_story.WORLD` 표.
  - **우박 이펙트 = Codex 그림** (사용자: 벡터 점 대신 그림체 통일): `Sheets/v2/hail.png` (자홍 3×2) → `fx_hail_fall`(꼬리 달린 알갱이) · `fx_hail_ball` · `fx_hail_chunk` · `fx_hail_splash1~3`(바닥 충돌 3프레임, 칸 크기 그대로 잘라 자리 맞춤). 게임 화면 단계: 떨어지는 우박 = `Blizzard.dotSprites/flakeSprites`(그림 비율 그대로 속도 방향으로, 비우면 예전 벡터 점 — 뽑기 컷씬은 그대로) · 바닥에 우박 = 오버레이 그림 프레임 애니(`SuperUltManager.Splash`, 템플릿 `HUD/SuperUlt/WorldSnow/Splash`) · 대상 타격 · 마지막 일격도 대상 화면 자리에 큰 충돌 그림(`SplashAt`). `SuperUltStyle.imageFx` 면 FxManager 점 파티클 안 씀. `superult_story.IMAGE_FX` 표. 캡처 `SuperUlt/preview/icequeen_v3_hail.png`.
  - 테이블: Super_Ult 31015 finish_line '전부 꽁꽁!!' / 'Freeze, everyone!!' (백업 `Data_Table/_backup_20261019b`).
  - Play 확인 (캡처 `SuperUlt/preview/icequeen_v3_super.png`, 콘솔 오류 없음).
- **기사 쥐 v2 (2026-10-19, 사용자: 아이스 퀸 방식 · 페이트 엑스칼리버 + 메이플 오리진 데몬 어벤저 참고)**: 띠 · 마무리 컷 없음, 컷 6개(0.75배속) → 게임 화면 3초.
  ① 원탁 언덕 안개에서 유령 깃발 · 투구가 솟음 ② 초원: 무릎 꿇고 → 검을 뽑아 치켜듦, 빛 입자가 검으로 ③ 얼굴 ④ 로우앵글 빛 기둥이 하늘을 가르며 위로 틸트 ⑤ 유령 기사단 질주 + 머리 위 거대 유령 검 · 도약 ⑥ 내리침(베기 호 · 유령 검이 땅에 꽂힘) → 하얗게 → **게임 화면: 거대 베기 그림이 대상을 지나며 10타 · 마지막에 성검 빛 물결이 왼→오** · 빛 입자가 위로 떠오름. 꽁꽁 없음.
  - 그림 (Codex 12장, `knight/Sheets/v2/` + `gen_prompts.py` · `run_v2.sh`): 세로 플랫 파노라마 a~c · face_bg + face_close · 리그 파츠(`Parts/knight`) 바탕 동작 시트 anim_raise/swing/slash/ghost × 3 · `fx_flat`(유령 검 · 빛 기둥 · 베기 호 · 빛 입자 · 별 · 고리, 칸 이름 = slice_superult `FLAT_NAMES`) · `fx_hit`(베기 · 빛 물결 · 빛 입자 · 타격 3프레임) · props_flat. 옛 칠 그림 = `Sheets/_v1_painted/`. 속도선은 아이스 퀸 플랫 줄무늬 복사.
  - 코드: `SuperUltStyle` worldWind · worldDotSize · slashSprites/slashSize/slashLife · waveSprite/waveSize/waveTime / `SuperUltManager` SlashFx · WaveFx · MoveImage(오버레이 움직이는 그림) / `superult_story` knight · `WORLD`/`IMAGE_FX` knight / `gen_superult_scene` 값 변환 `cs()` (정수 · 실수 · Vector2 · 그림 · 그림 배열).
  - **v3~v4 다시 짬 (같은 날, 사용자: 돌 제단의 대검을 뽑아 → 차징 → 위에서 아래로 화면을 가르듯 베기 → 그 일직선을 따라 초토화 · 검은 더 크게 · 화면 전체를 가르며 파동)**: 컷 ① 언덕 위 돌 제단(`prop_altar`)에 꽂힌 거대 대검(`prop_greatsword`) + 하늘 빛 ② 손잡이를 잡고 힘껏 → 돌 파편(`prop_debris`)과 함께 뽑아 치켜듦 ③ 얼굴 ④ 어깨에 메고 차징(`fx_charge` 기운 · `fx_sparks` 빨려 듦 · 흔들림이 점점 잦아짐) ⑤ 하늘로 도약 ⑥ **머리 위로 크게 들었다가 한 번 내려찍기 → 화면을 상하로 가르는 가로 베기선 · 파동 한 번** (사용자, `Sheets/v5/anim_cut.png` = 정면 내려찍기 — 프레임 3은 칼날이 거꾸로 보여 안 씀, 가로 휘두르기 안은 `v5/anim_cut_horizontal.png`): 화면 가운데 가로 베기선(`fx_cutline`) + **굵은 파동 띠(`fx_slashwave`)가 선 따라 뻗고 충격파 줄(`fx_ripple`)이 양옆으로** + 선 위 폭발(`fx_boom1~3`) → 하얗게. **v6 (사용자)**: 기사 동작 = 옆모습으로 대검을 머리 한참 뒤로 젖혔다 → 머리 위를 지나 → 몸 앞으로 내려침 (`Sheets/v6/anim_swingdown.png` → `anim_swingdown_1~3`), 컷신 베기선은 번쩍하고 0.25초 안에 사라짐 · 게임 화면에 남는 선 없음(후딜 선 제거, lineOnce 면 선 그림 안 그림). 휘두르기 0.5배속 (3.0~3.28초, 컷신 끝 3.55) · **컷신 글 없음** (제목 띠 · 자막 · 히트 수 · 밈 글 — `SuperUltStyle.hideText`, `superult_story.NO_TEXT`). 캡처 `knight_v6_swing.png`. 게임 화면 단계(1초): 가로 파동이 **한 번** 퍼지며 폭발이 가운데서 양끝으로 (`SuperUltStyle.lineOnce`, 다단 · 마무리 때 추가 폭발 · 파동 없음), 그 1초 동안 다단 10타. 전체 실측 약 6.3초.
    그림: `Sheets/v3`(제단 · 동작 · fx_cut) → `Sheets/v4`(대검 2.5배 동작 3시트 · 제단 · `fx_wave` 파동 3줄) — 쓰는 것은 v4 + v3 fx_cut. 코드: `SuperUltStyle` lineSprite · lineAngle · lineLength · lineThickness · boomFrames · boomSize · boomFinishCount · lineWave · lineRipple · waveThick · rippleDist · rippleTime / `SuperUltManager` LineBoom · LineWave · `MoveImage` 가로 길이 고정(w) · 굵기 배율 따로(sy) · `Splash` 프레임 지정. 유령 검 · 기사단 · 빛 물결(v2)은 안 씀. 캡처 `SuperUlt/preview/knight_v4.png` · 가로 베기 `knight_v5_hcut.png` · 내려찍기 `knight_v5_slam.png`.
  - **WebAtlas 주의**: `Editor/WebAtlasBuilder` 가 가로·세로가 4의 배수가 아닌 Art 그림을 폴더별 아틀라스(최대 4096)에 자동으로 묶는데, 컷신 그림은 커서 아틀라스가 넘쳐 **엉뚱한(옛) 그림이 나왔음** → `slice_superult.pad4` 로 출력 그림을 4의 배수로(투명 여백 위·오른쪽) → 아틀라스에서 빠짐. 그 결과 `_WebAtlas/Rats_SuperUlt_knight` · `_icequeen` 아틀라스는 빌더가 자동으로 지움. 아틀라스에서 빠진 직후 씬에 걸린 그림이 '텍스처 없음(흰색)'이 될 수 있음 → 그림 다시 임포트 후 씬 다시 적용.
  - 안 쓰는 옛 출력 7개 삭제 (knight fx_aura · fx_impact · fx_strike · pose_* · icequeen pose_finish, 원본은 `_v1_painted/out_*`).
  - Play 확인 (캡처 `SuperUlt/preview/knight_v2.png`, 실측 8.3초, 콘솔 오류 없음).
  - **별 모양 이펙트 없앰 (사용자)**: 컷 전환 · 뽑을 때 섬광 · 하얗게 = 둥근 빛(`fx_flash`), 반짝이 = 둥근 빛 알갱이(`fx_dots`), 고리 = 빛 고리(`fx_lightring`) — Codex `Sheets/v7/fx_light.png`. 얼굴 배경도 별 없이 다시(`v7/face_bg.png`). 게임 화면 입자 = 빛 알갱이 · 대상 타격 = 폭발 프레임. (세이버 엑스칼리버 정면샷 `v7/anim_excal.png` 도 만들어 봤지만 사용자가 원래(옆모습 내려치기)로 되돌림 — 안 씀.) 컷 ⑥ 기사 그림이 다단 반복문 안에 들어가 8겹으로 만들어지던 것도 고침.
  - **마지막 컷 = 엑스칼리버식 (2026-10-19 사용자, 유튜브 UBW 세이버 엑스칼리버 참고)**: 정면 전신(대검을 머리 뒤로, `anim_excal_1`) → **트래킹 줌**으로 얼굴까지 → **눈 익스트림 클로즈업**(face_close 확대, 눈 반짝임 별 → 둥근 점으로 Codex 수정) → **휘두르기**: 정면에서 머리 뒤 → 머리 위 → 카메라 쪽으로 내려침(`anim_excal_1~3`), 내려오는 칼을 따라 빛 띠(`fx_slashwave`)가 위→아래로 쓸며 둥근 섬광과 함께 **화면을 가림** → **후경**: 빛 벽 2장이 가운데서 **좌우로 갈라지며** 휘두른 뒤의 기사가 작게 가운데 + 뒤로 빛 폭발(`fx_blast`) · 탄 가장자리 · 방사 줄무늬(`fx_frame`) · 돌 파편 · 잔상 겹침(블러). 그림 = Codex `Sheets/v8/fx_frame · fx_blast · face_close`. 컷신 끝 4.05 → 게임 화면 파동 1초. 캡처 `knight_v9_excalibur.png`. (중간 시안: 옆모습 내려치기 · 정면 내지르기 정지 컷은 안 씀.)
  - **v10~v11 (사용자)**: 순서를 엑스칼리버대로 = ① 제단 ② 뽑기 ③ 차징 ④ 정면(대검을 머리 뒤로, 빛 알갱이가 모임 → 트래킹 줌) ⑤ 눈 클로즈업 ⑥ 휘두르기(빛 띠가 화면을 가림) ⑦ **후경**. 앞쪽 얼굴 컷 · 도약 컷 · 화면을 가로지르던 사선 빛 기둥은 뺌.
    후경: 원근 초원(`rear_bg`, 캐릭터가 딛고 선 땅) · 멀리 선 기사(`rear_knight` = 칼을 **비스듬히** 아래로 내려친 뒤, `Sheets/v11`) · 칼끝에서 카메라 쪽 땅 균열(`ground_crack`) · **화면 양옆 가까이 갈라진 바위·빛 벽**(`split_frame`, 가운데 투명)이 카메라를 가리며 천천히 벌어짐 · 벽은 잔상 2겹으로 블러. 그림 = Codex `Sheets/v10`(배경 · 균열) · `v11`(양옆 벽 · 비스듬한 칼 기사). 컷신 끝 3.75. 캡처 `knight_v11_rear.png`.
  - **v12 = UBW 엑스칼리버 프레임 분석대로 (사용자)**: 영상 20.5~23.8초 = 기 모으기 → 얼굴 클로즈업 · 트래킹 줌 → 휘두른 잔상 줄무늬가 얼굴 위를 지남 → 빛 · 불꽃이 화면을 덮음 → 덮은 이펙트가 갈라지며 틈으로 멀리 내지른 정지 컷 → 갈라진 벽이 양옆으로 벌어지며 카메라를 스쳐 지나감.
    구현: ④ 정면(대검 머리 뒤 · 빛 알갱이) → 트래킹 줌 ⑤ 눈 클로즈업 + **잔상 줄무늬**(`fx_smear`, 3겹 블러)가 칼 각도로 지남 → 덮음(`fx_blast` + 섬광) ⑥ 후경 정지 컷(초원 · 비스듬히 내려친 기사 · 칼끝에서 칼 방향 땅 균열) ⑦ **빛·불꽃 벽(`fx_flamewall` 반쪽, 찢긴 가장자리를 갈라지는 선에 맞춰 둘)이 칼 각도로 갈라져 양쪽으로 벌어지며 커져서(다가옴) 지나감**, 잔상 겹 = 블러.
    **각도 통일**: `superult_story.KNIGHT_LINE = -40` (rear_knight 칼날을 PCA 로 잰 값) — 잔상 · 갈라짐 · 땅 균열 · 게임 화면 파동 전부. 그림 = Codex `Sheets/v12`. 옆 바위 벽(`split_frame`) · 정면 휘두르기 컷은 안 씀. 캡처 `knight_v12_split.png`.
  - **v13 후경 (사용자)**: 레퍼런스처럼 기사를 가깝게(화면 높이 약 40%) · 살짝 **하이앵글** (Codex `Sheets/v13`: `rear_knight` · 하이앵글 초원 `rear_bg` 다시) · 후경을 **큰 세로 파노라마** `pano_rear`(하늘 `pano_top` + 초원 + 가까운 땅 `pano_bot`, README 방법대로 이어 그려 붙임 — 위 이음은 자동 겹침이 언덕을 지워서 위 장 아래 330px 잘라 110px 만 섞음)로 만들고 그 안에서 **Rig(줌) > Track(보는 곳)** 카메라 무빙: 갈라지는 순간 **틸트 다운 + 달리 아웃 반동** → 천천히 되돌아옴. 갈라진 벽은 같은 반동을 시차 1.4배로 받고, 화면 양옆에 크게 오래 머물다 지나감. 게임 화면의 맵 가르는 파동은 그대로 (사용자: 좋음). 캡처 `knight_v13_rear.png`.
  - **v14~v15 (사용자)**: 레퍼런스 22.95~23.20초 프레임으로 '빛 벽이 찢겨 열림' 3프레임(`Sheets/v14/open1~3`, 가이드 그림 `guide_open*` = 열린 자리 대략 위치)도 만들어 봤지만, 사용자가 **v13 의 화면을 가르는 빛·불꽃 벽(칼 각도 -40°)** 으로 되돌림 (open 프레임 안 씀).
    대신: ① 칼끝 땅 균열 · 파편 뺌 ② **크롭형 마스크 블러** = `slice_superult.blur_copy` 로 흐린 사본 — 벽은 `fx_flamewall_blur` 를 크게 2겹, 후경은 `pano_rear_blur`(기사 둘레 타원은 투명, 바깥으로 갈수록 진함)를 파노라마 위에 겹침 ③ **칼 오라 (칼과 크기 딱 맞게 · 노란빛 진하게)**: rear_knight 칼날을 다각형으로 따서(가드 (440,705) → 칼끝 (934,1152), 양 가장자리 직선을 칼날 중간에서 잼) 만든 `rear_knight_glow`(안쪽 밝은 노랑 + 바깥 금빛 노랑 2겹, 기사 뒤) · `rear_knight_shine`(칼날 모양 노란빛, 기사 위, 깜빡임) — 둘 다 기사 그림과 같은 크기 · 자리 (캡처 `knight_v15_aura.png`) ④ 둥근 하얀 섬광(`fx_flash`) 은 후경 · 덮기에서 뺌 (컷 전환 번쩍만 남음). 캡처 `knight_v15_split.png` · `knight_v15_glow.png`.
- **파라오 v2 초안 (2026-10-20, 사용자: 아누비스 · 미라를 잔뜩 소환 → 미라 돌격 · 정면샷 · 기사/아이스 퀸 참고)**: 그림 Codex 15장 `pharaoh/Sheets/v2/` (`gen_prompts.py` · `run_v2.sh`) → Sheets 로 복사 → `slice_superult.py pharaoh` (옛 그림 = `Sheets/_v1_painted/`). 셀 프레임 anim_summon · command(파라오) · anubis · mrise · mrun · mleap(미라) × 3 + `army`(정면 군단 한 장) · fx_flat/fx_hit/props_flat 이름 = slice `FLAT_NAMES` · `HIT_NAMES` · `FLAT_PROPS`.
  연출 `superult_story.pharaoh` (옛 것 = `pharaoh_v1`, 안 씀): ① 노을 태양 → 틸트 다운 ② 계단 아래 정면 소환(소환진) ③ 얼굴 ④ 평원 3줄에서 모래가 터지며 아누비스 · 미라가 솟음 + 뒤에 군단 · 카메라 물러남 ⑤ 지팡이를 카메라로 ⑥ 미라 16마리가 카메라 쪽으로 달려와(달리기 3프레임 반복 · 원근) 지나감 · 양옆 아누비스 호위 · 맨 앞 미라가 덮쳐 모래 먼지로 덮음 → 게임 화면 1.6초 (모래 · 태클 충돌 · 마지막에 `army` 가 왼→오 waveSprite). 콘티 `SuperUlt/preview/pharaoh_v2_board.png`. **2차 (같은 날, 사용자)**: 아누비스는 ④ 에서 **한 마리만** 크게 (⑥ 옆에서 지켜봄), 미라는 잔뜩 · 군단 그림 `army` 를 미라만으로 다시 (옛 것 `Sheets/_v1_painted/army_with_anubis.png`) · **마무리 = 모래바람 대신 미라 쥐들이 게임 화면을 왼→오로 달려 지나감**: 옆모습 달리기 `anim_mside_1~3` (Codex) · 코드 `SuperUltStyle.runFrames/runGap/runCount/runHeight/runSpeed/runFrame` · `worldSnowOff` / `SuperUltManager.RunWave`(MoveFx run = 등속 · 프레임 반복 · 통통 걸음 · 아래쪽이 앞) · 컷 ⑥ 모래 먼지 덮개 · 줄무늬 뺌. 씬 적용 · Play 확인 끝 (UnityMCP 는 Claude 쪽 연결이 실패해도 서버(8080)가 떠 있으면 HTTP 직접 호출 — scratchpad 식 mcp.py, 슬롯 5 로 바꿔 테스트 후 nkk_slot 되돌림). 캡처 `SuperUlt/preview/pharaoh_v3_caps.png`, 콘솔 오류 없음. Art 폴더의 옛 pose_* · fx_aura 등은 안 씀.
- **폭탄마 v2 (2026-10-20, 사용자: 빨간 마법진 여러 겹 → 위에서부터 레이저 → 맵 전체 초토화 폭발)**: 그림 Codex 11장 `bomber/Sheets/v2/` (`gen_prompts.py` · `run_v2.sh`, 옛 그림 `Sheets/_v1_painted/`) — 파노라마(붉은 노을 · 황무지 지평선 · 절벽) · 얼굴 · 정면 동작 anim_cast/anim_fire · `fx_flat`(마법진 · 원근 마법진 · 룬 고리 · 작은 마법진 · 붉은 섬광 · 불티) · `beam`(레이저 · 심 · 꽂힘) · `boom`(폭발 3프레임 · 충격파 고리 · 파편 · 연기) · `mushroom`(버섯구름 3단계, 지금은 안 씀). slice `STRIP3` · `GRID6` · `drop_edge_bits`(옆 칸 부스러기 지움).
  연출 `superult_story.bomber` 컷 5개 (0.75배속): ① 절벽 위 팔짱 ② 두 팔을 하늘로 · 발밑 마법진 ③ 얼굴 ④ 지평선 → 하늘 틸트 업, 마법진 6겹이 아래부터 쌓임 ⑤ 맨 위에서 레이저 → 마법진을 하나씩 꿰뚫으며 아래로, 지평선에 꽂히는 순간 섬광 → 3.05초 컷신 끝.
  **게임 화면 = 큰 폭발 하나** (사용자: 캐릭터 · 버섯구름 컷 없음, 자잘한 폭발 여러 개 말고 맵을 다 덮는 한 번): `SuperUltStyle.blastBeam/blastFrames/blastFlash/blastRing/blastDebris/blastSmoke` · `SuperUltManager.BigBlast · Seq · BlastBurst` — 레이저가 화면 가운데로 내리꽂힘 → 화면을 덮는 섬광 → 폭발 프레임(3400px) → 충격파 고리 2겹 · 파편 16개 → 반투명 연기(불꽃 뒤, 0.6). `mushFrames` 는 비움(빈 목록 = `new Sprite[0]`, gen `cs()`). 캡처 `SuperUlt/preview/bomber_v7_blast.png`, 콘솔 오류 없음.
  남은 의견: 컨셉이 마법사(메구밍) 쪽이라 폭탄마답게 바꾸는 안(A = 폭탄 탑 + 도화선 + 아프로 개그, B = 조준 레이저 + 거대 폭탄)을 사용자에게 설명함 — 결정 안 됨.
- **Play 테스트 방법 (2026-10-20)**: Claude 쪽 UnityMCP 연결이 실패해도 서버(8080)가 떠 있으면 HTTP 직접 호출 (scratchpad 식 `mcp.py`). **Play 확인은 단계마다 따로** (적용 코드 맨 앞에도 `if (isPlaying) return`), `nkk_slot` 을 5로 바꿔 Play → `TestSuper(code)` → 캡처 → 원래 슬롯으로 되돌림. (같은 날 한 번 Play 중에 새로고침 · 씬 빌드가 돌았음 — 저장 단계에서 실패해 씬 · 저장 영향 없음.)
- **사고 기록 (2026-10-19)**: 사용자가 슬롯 1 로 Play 중일 때 MCP 체인이 멈추지 않아 Play 중 재컴파일 · 테스트 단계 `SkipTutorial()` 이 **슬롯 1 저장에 tutoSkip=true 를 씀** · 사용자 Play 를 멈춤. 원래 tutoSkip 값은 모름 (사용자 확인 필요). 이후 테스트 스크립트는 Play 중이면 중단 · 슬롯 5 가 아니면 SkipTutorial 안 함.
- **남음**: ① 사용자 눈 확인 ② ~~새 씬 글 '총 피해 {n}' 스트링 테이블 등록~~ (2026-10-21 §5-17 에서 끝) ③ 나머지 13종도 같은 방식(햄찌는 손으로 만든 장면).
## 5-17. 새 쥐 '메이드 쥐' (2026-10-20, 게임 적용 · Play 확인 끝) — 그림 `UnityResources/Rats/NewRats4/README.md`

- **사용자 결정**: 참고 그림(은발 메이드)을 게임 그림체 쥐로 · 이름 '메이드 쥐' · 신화 7훈장(공격력 472) · 스킬 = 메이드 (패시브 '완벽한 뒷정리' 치즈 ×1.4 = Cheese_Boost · 액션 '티 타임' 20초마다 주변 광란 6초 = Cheer, 찻잔 쟁반 스티커) · 필살기·초필살기 = **시간 정지 + 칼** (시간 정지는 **연출만**, 게임 영향 없음) · 흰 털 · **큰 금색 회중시계가 잘 보이게** · 리깅은 **다른 쥐와 같은 방식** (그리는 순서 그대로 → 시계는 뒷다리 허벅지에).
- 테이블 (백업 `Data_Table/_backup_20261020`): Character 10088 `maid` · Skill 20088 · 21088 · Ultimate 30036 '살인 인형' `UltTimeStop`(게이지 240, 자막 c1~c5) · Super_Ult 31016 '메이드의 세계' · 업적 40036 '그리고 시간은 움직인다'. 스트링 + 영어 채움 → xlsx2json 끝.
- 코드: `Ults/UltTimeStop.cs` (회중시계 · 바늘 → 회색 화면 → 화면 안 대상 옆으로 순간이동하며 공중에 칼 → 재개 순간 일제히 꽂힘) · `Rat.Action` Cheer 에 메이드면 `maid_teatray` 스티커. 유니티 밖 dotnet 빌드 오류 0.
- 초필살기: 그림 Codex 10장 `SuperUlt/maid/Sheets/v2/`(`gen_prompts.py` · `run_v2.sh`) → Sheets 복사 + 멈춘 세계 판 `pano_?_alt`(원본 탈색) → `slice_superult.py maid` 끝 (Art 복사됨). 연출 `superult_story.maid` (컷 6: 달·시계탑 → 회중시계 딸깍 → 얼굴 → 손가락 딱 · 회색 · 하늘에 칼 고리 → 칼 흩뿌림 → 칼이 카메라로) · 게임 화면 = 칼 비 (WORLD/IMAGE_FX maid, 칼 그림 *r = 오른쪽으로 돌린 사본). 씬 코드 `gen_superult_scene.py out.cs maid` (2.8만 자).
- **유니티 적용 끝 (2026-10-20)**: maid 파츠 다시 임포트(피벗) · NKK/Build Rat Art Library (88종) · Game: UltimateManager Fill Props/Icons · FxManager 스티커 `maid_teatray` · AchievementToast Fill Icons · 초필살기 씬 `Scenes/Maid` · LocText(밈 8000517) · Lobby: IconBook Fill Icons. 스트링 import_scene_strings + 영어 (다른 세션의 술탄·람쥐 새 글 2개도 영어 채움).
- **Play 확인 (슬롯 5, 콘솔 오류 0)**: 필살기 = 째깍 → 회색 · 칼이 물건 둘레에 → 재개 때 일제히 꽂혀 피해 (캡처 scratchpad). 처음엔 빈 방에서 칼이 안 나와서 **대상이 6개보다 적으면 바닥 자리에도 칼**(actor 4, 마지막 칼에 작은 충격) · 칼 폭 82 · 회색 0.46 으로 고침. 초필살기 = 컷 6개 → 게임 화면 칼 비 (처음 너무 크고 빽빽 → worldDotSize 34~62 · gap 0.1 · worldSnowScale 0.3), 대상마다 피해 숫자. 편집 모드 컷신 미리보기 = 기존 cap 스크립트(캡처 뒤 그림 되돌림).
- 안 본 것: 티 타임은 스티커 생성만 확인 (특수 액션은 쳇바퀴 Lv 3 해금 뒤라 실제 발동 화면은 아직) · 뽑기 고유 컷씬(GachaIntro)은 없음 → 보통 공개로 나옴 (원하면 따로 제작).

## 5-18. 시작 층 상한 완화 (2026-10-21) ★

- **문제 (사용자 플레이)**: 층별 성장 체감은 좋은데, 로비에서 고를 수 있는 시작 층과 최고 클리어 층 차이가 너무 크고 갈수록 벌어져 불쾌함. 시뮬레이터로 보면 클릭 기준 10~45층에서 차이 2 → 17층.
  원인 ① 1~29층 건너뛰기 상한 = 기획서 26장 표 (17층 클리어해도 10층까지 · 20~29층 15~20) ② §5-15 에서 훈장 조건 층을 앞당겼는데 스테이지 스킵 노드 값은 예전(8훈장까지 누적 27) 그대로라 노드 합이 못 따라옴.
- **바꾼 것**
  - `gen_stage_table.skip_cap` (모든 층 한 규칙): **최고 클리어 − `SKIP_BACK` 3, 단 넘은 훈장 허들 다음 층보다 아래로는 안 감** (예전 30층~ 은 −4). `DOC_SKIP` 표 없앰.
  - `gen_skill_tree` 스테이지 스킵 노드(S1 · S3, 앞 칸이 큼)를 `tier_gates.GATE` 에서 계산: 훈장 t 트리 누적 = 다음 훈장 조건 층과 그다음 조건 층의 가운데 − 1 → 승급 직후 새 노드를 안 찍어도 다음 허들까지 반쯤 따라옴. 누적 1~16훈장 = 7 · 11 · 15 · 20 · 25 · 31 · 37 · 43 · 49 · 55 · 61 · 67 · 73 · 80 · 91 · 99.
  - `tier_gates.SHARD` (조각 레벨 합 조건) 다시 맞춤: 판이 짧아져 판당 출생(조각)이 줄어 7→8 · 9→10훈장이 조각 때문에 1.5h 씩 밀렸음 → 같은 규칙(조건 층 도달 때 조각 합 × 0.9, 클릭·미클릭 중 작은 값)으로 12 · 14 · 30 · 45 · 65 · 80 · 110 · 145 · 160 · 205 · 255 · 285 · 335 · 380 · 410.
  - `gen_stage_table._curve` 증강 배율: 표에 없는 1~8훈장 기본 슬롯 5 → **2** (7·8훈장 허들 34·40층이 증강 5칸 기준이라 미클릭은 34층을 영영 못 넘었음). 곡선을 다시 뽑으면서 §5-13 의 증강 2~5칸 변경도 같이 반영됨 → **31층~ 적정이 약 0.85배로 내려감** (29~33층은 0.97~0.85배, 1~28층 그대로).
  - 생성 순서대로 전부 다시 실행 + `balance_sim_xlsx`. 바뀐 JSON = StageTable · CommonSkillTable · TierTable (나머지는 줄바꿈만).
- **결과 (balance_sim)**: 시작 가능 층 차이 = 모든 판 **3층 이하** (허들 넘은 직후 0~1). 클릭 **8.0h** · 미클릭 **9.8h** (바꾸기 전 클릭 9.4h · 미클릭 34층에서 막혀 못 깸 / §5-15 기록 8.8 · 11.3h). 앞쪽 몸풀기 층이 줄어 판이 10~20분으로 짧아지고 전체 시간도 줄어듦 — 더 길게 원하면 `tier_gates.RANK_K`(승급 비용)나 `SKIP_BACK` 4 로.
- **확인 안 함**: 유니티 Play (데이터만 바뀜 → 유니티 창을 앞으로 가져오면 JSON 다시 읽음). 이미 산 스킵 노드는 같은 id 라 기존 저장에도 새 값이 바로 적용됨. 시작 층이 앞당겨져 판 첫 층에서 쥐 수가 적은 채로 높은 층을 만나는 손해(시뮬 RAMP_SEC 25초 가정)는 실제 플레이로 볼 것.

## 5-19. 승급 튜토리얼 = 최대 마릿수 도달과 함께 (2026-10-21)

- **사용자**: 승급 안내는 최대 마릿수 도달 안내와 함께 나오는 게 흐름상 맞음 → 초반엔 승급이 없으니 최대 마릿수까지 채우도록 유도.
- **바뀐 흐름** (§5-10 표의 '승급 = 2층 10마리' 를 대체)
  | 경우 | 단계 |
  |---|---|
  | 처음 꽉 참 (몇 번째 판이든) | `g_pop`(인구수 설명) → 바로 `g_pop_promote`(새, After g_pop · Locked promote): "걱정 마! 꽉 찼을 땐 승급" + 승급 해금 · 설명 · 주의 · 일괄 승급 |
  | 4층까지 못 채우고 5층 | `g_pop_f5`: 인구수 설명 + "꽉 차면 승급을 알려 줄게" 예고 (승급은 안 열림) |
  | 예고를 들은 뒤 꽉 참 | `g_promote`(발동 RatCount 10 → **PopFull**, 조건 Done g_pop_f5·g_power_pop + Locked promote): "드디어 꽉 찼어! 약속대로 승급" + 해금 · 설명 |
  | 두 번째 판 g_power 때 승급 잠김 | `g_power_promote_new`(승급 해금) 없앰 → `g_power_pop`(인구수 잠겨 있으면, 이제 g_power 바로 뒤) → `g_power_hint`(새): "적정이 모자라면 꽉 채워 봐 → 승급이 열려" |
  | 승급이 이미 열림 | `g_power_promote` 그대로 |
- `Progress.TutoRoundSteps` 에서 g_promote 뺌 (안 빼면 꽉 채우기 전엔 라운드가 안 끝나서 치즈 창고 l_first 가 안 열림).
- 대사: Line 9 · 11 · 29 · 30 · 90 문구 바꿈(키 그대로, 영어 다시 씀) · 52 · 66 · 67 지움(스트링은 '안 쓰임') · 102~107 새로(g_pop_promote, 103~107 은 g_promote 대사와 같은 키 + 새 키 3900102). 백업 `Data_Table/_backup_20261021`.
- **확인함**: 바뀐 TutorialTable.json 으로 단계 고르기 흉내(조건·After 이어짐) — 위 다섯 경우 모두 의도한 순서. **확인 안 함**: 유니티 Play.

## 5-20. 초반 계단 벽 완화 (2026-10-21)

- **사용자**: 초반 계단 벽이 체감상 너무 빡세서 불쾌함 → 구간별 체력을 낮추거나 적정 감산 공식을 바꿔 조금 너프.
- 원인: 전투력이 적정보다 낮으면 계단 벽 피해 = (전투력÷적정)^1.5 (일반 벽은 ^0.5) + 경로 벽 합의 절반이 계단 벽 하나에 몰림 → 첫 판(쥐 30마리, 적정의 0.6~0.75배)에서 일반 벽은 뚫리는데 계단 벽만 거의 안 깎임.
- **바꾼 것** (`StageManager`, 테이블은 그대로): 1층 → `earlyEndFloor`(10층) 사이 고르게
  - 계단 벽 감산 지수 `earlyGateStairs` 0.5 → `wallGateStairs` 1.5 (5층 ≈ 0.94)
  - 계단 벽 체력 배율 `earlyStairsHp` ~~0.7~~ → **0.4** → 1 (사용자 플레이: 0.7 도 여전히 빡셈 → 같은 날 더 낮춤. 2층 0.47 · 3층 0.53 · 4층 0.6 · 5층 0.67 · 7층 0.8 · 9층 0.93)
- 효과 (시간 모델, 제한 120초 안에 계단까지 가려면 필요한 전투력÷적정): 1~4층 0.78 → **0.56~0.64** (0.7 일 때 0.64~0.69) · 5층(허들) 0.87 → 0.75 · 6~8층 0.78 → 0.69~0.74 · 9층 0.87 → 0.85 · 10층~ 그대로. 계단 벽 하나만 보면 2층 체력이 예전의 약 절반. 적정일 때 1~9층은 계단 벽이 얇아진 만큼 조금 빨라짐.
- 두 값 모두 Game 씬에 저장돼 있지 않아 코드 기본값이 쓰임 (씬을 저장하면 그때 값이 씬에 들어감 → 그 뒤로는 인스펙터에서 조정).
- **계단 방이 바로 옆일 때 (같은 날, 사용자)**: 계단 거리 d = 1 이면 경로 벽 합(wall_path) 전체가 계단 벽 하나에 들어가 d ≥ 2 때의 두 배로 튀었음 (예: 3층 1.5만 vs 0.76만). → d = 1 도 계단 벽 = 합 × `stairsShare`(0.5) (`StageManager` 층 지형 계산 + `gen_stage_table.walls()`). 이제 계단 벽 체력은 계단 거리와 상관없이 같음. 대신 d = 1 지형은 경로 벽 합이 절반이라 그 층이 더 빨리 끝남 (운 좋은 지형). 스테이지 테이블 다시 생성 → 바뀐 것 = 1 · 3층 `wall_stairs`(wall_path 가 0 일 때만 쓰는 예비 값) 뿐. 백업 `Data_Table/_backup_20261021/스테이지 테이블.xlsx`.
- `balance_sim.py` 는 층 구분 없는 시간 곡선(`TIME_C · TIME_LO`)이라 이 완화는 안 들어감 (초반 1~9층만 영향).
- **확인함**: `Tools/check_compile.py` 오류 0. **확인 안 함**: 유니티 Play (초반 판 체감).

## 5-21. 딜 미터기 · 쥐 목록 창 (2026-10-21)

- **사용자 결정**: 롤체 딜 미터기처럼 이번 층(층마다 0부터) 쥐 **종별** 피해 상위 5개를 화면 오른쪽에, 순위가 바뀌면 줄이 위아래로 움직이며 자리 바꿈 (유튜브 순위 영상처럼 밀고 내려오는 연출) · 대상 = 물건·가구·벽·사람·고양이·보스 전부 · 쥐 목록 창 = HUD 버튼 + 단축키(R), 열면 게임 멈춤.
- **집계** `Core/DamageMeter.cs` (정적): `Add(쥐, 피해, 맞기 전 체력)` = 실제로 깎인 체력만 · 날아간 물건/사람이 맞힌 피해 = 날린 쥐(`By`) 몫. 훅 = `Item.Damage/SkillHit` · `Human.Damage/Blast` · `Cat.Damage` · `Boss.Damage` · `StageManager.DamageWall`, 층 시작(`EnterFloor`)에 `Clear`.
- **딜 미터기** `Rats/DamageMeterUI.cs` · 씬 `HUD/DmgMeter` (Icon · Title · Total{n} · Rows/Row0~6 = 줄 7개 돌려 씀: Rank{rank} · Medal · Up · Portrait(RatPortrait) · Name · Value{n} · Bar/Fill). 1~3등 = 왕관·은·동 메달, 막대 = 등급 색, 피해 0 이면 숨김.
  - **자리 바꾸기 연출 (사용자: 유튜브 순위 영상처럼 밀고 내려오게)** — 바 차트 레이스 방식 참고: 올라가는 줄 = 커지며(riseScale) 왼쪽으로 떠서 맨 앞에 그려지고 easeOutBack 으로 도착 + 노란 번쩍(Flash) + ▲ · 밀려난 줄 = pushDelay 만큼 늦게 출발해 목표보다 더 내려갔다 돌아옴(pushOvershoot) + 작아짐·흐려짐 + ▼ · 새 종 = 아래에서 떠오름 · 빠진 종 = 가라앉으며 사라짐 · 숫자는 올라가며 셈 · 순위 숫자/메달 통통. 시간·세기 전부 인스펙터 '자리 바꾸기 연출'. 줄 자식에 Flash · Down 추가, Rows 의 RectMask2D padding 은 좌우·위로 넓힘(커져도 안 잘림).
- **쥐 목록 창** `Rats/RosterPanel.cs` · 씬 `HUD/RosterButton`(쥐 마릿수 아래) · `HUD/Roster/Panel`(Card: Title · Summary{n}{max}{kinds}{dmg} · 정렬 등급순/마릿수순/피해순 · 닫기 · Scroll/Viewport/Content/CardTemplate: Frame(등급 색) · Rank{rank}위 · Portrait · Name · Grade{grade} · Count{n} · Damage{n}). 멈춤 = `FxManager.Halt` 에 `RosterPanel.Pausing`. 임시 소환 쥐 제외.
  - **휠 (2026-10-21 밤, 사용자: 목록 창이 떠 있으면 휠로 뒤 화면이 줌되지 말고 목록만 위아래로)**: 창이 열려 있으면 `CameraController` 가 휠 줌·끌기를 안 받음 · 휠은 마우스 위치와 상관없이 `RosterPanel.Wheel` 이 목록을 `wheelPixels`(120) 만큼 스크롤 (ScrollRect 자체 휠은 `scrollSensitivity` 0 으로 꺼서 두 번 안 움직임). `check_compile.py` 오류 0, **Play 확인 안 함**.
- **그림** `UnityResources/Rats/HUD_DmgMeter/` (Codex 1장 3×2 → 유니티에서 키잉·자르기) → `Assets/Art/HUD_DmgMeter/`.
- **튜토리얼 (사용자 결정: 세 번째 판)**: `g_meter` = 발동 FloorTime 20 · 조건 RunMin 3 + Done g_power · 해금 `meter` (Line 108~110, 강조 meter, 순서 110) → `g_roster` = After g_meter · 해금 `roster` (Line 111~112, 강조 roster, 순서 111). 잠겨 있으면 `TutorialManager` 의 FeatureGate 가 `HUD/DmgMeter` · `HUD/RosterButton` 을 숨기고, R 키도 막음(`RosterPanel.feature`). Unlock_Id 시트에 meter · roster 추가.
- **스트링**: 새 씬 글 8000512 · 8000518~8000531 (원격 머지 뒤 다시 받은 키, + 기존 '닫기' 8000011) 에 LocText (자리표시 글 = template) · 대사 3900108~3900112 · 영어 채움. 초필살기 '총 피해 {n}'(8000512 = Total {n}) 도 같이 연결 (§5-16 남음 ② 끝).
- **원격 머지 (2026-10-21)**: 컴퓨터가 꺼져 머지가 중간에 끊김 → git index 깨짐 → index 다시 만들고 잔여물은 `git stash` ("중단된 머지 잔여물") 로 치운 뒤 다시 머지. Game 씬은 원격 기준 + 딜 미터기 UI 를 스크립트로 다시 만듦 (scratchpad `rebuild_meter.cs`), 튜토리얼 · 스트링 엑셀은 원격 기준에 내 단계 · 대사 다시 넣음.
- **슈퍼 점프 → 보스 (2026-10-21)**: 예전엔 보스에 피해 0 (물건 · 사람 · 고양이 · 벽만). 이제 화면 안 · 싸우는 중인 보스에 박살 피해 × `bossDamageK`(3), 상한 = 최대 체력 × `bossCap`(0.15) — 보스 피해 배율(공용 스킬)로 넘친 만큼 되돌림. 로그 `[SuperJump] 보스 피해`. Play 확인: 5층 보스 747 (상한 747). 슈퍼 점프 벽 부수기(`SuperBreakWall`)도 딜 미터기에 셈.
- **보스 머리 위 소품 (2026-10-21, 사용자: 스킬 쓸 때 입에 뭘 물고 있음)**: `ShowProp(..., aboveHead)` 가 키 × 0.75 + 앞으로 30 자리 → 고양이 보스는 입 자리. 이제 머리 꼭대기 + `propGap`(14) 위에 소품 아래쪽이 오게, 고양이는 머리 쪽으로 `catPropAhead`(키 × 0.3). 대상 = 밈 패턴 Shout · Buff · Spin · Dash(고양이) · Global · 약품 · 긴급 회의. 사람 보스가 손에 쥐는 소품(PlaceHeld)은 그대로. 노조 고양이 확성기로 확인.
- **확인**: 테이블 로드(단계 44 · 대사 키) · 영어 JSON. **튜토리얼 흐름 자체는 Play 확인 안 함** (슬롯 1 은 tutoSkip 이라 다 열림 · 테스트 슬롯 5 를 3번째 판 상태로 만들어야 함).

## 5-22. 적정 찍찍 화면에서 숨김 (2026-10-21)

- **사용자 결정**: 허들 층(훈장 해금 층 ×1.3 · 보스 층 ×1.15) 때문에 낮은 층 적정이 바로 위층보다 높아 보여 헷갈림 → **적정은 화면에 안 보이고 우리가 조절하는 내부 값으로만**, HUD 는 지금 찍찍!!(전투력)만 보여 줘서 '강해졌다'는 감각만 주기.
- HUD `PowerText` = `찍찍!! {power}` (코드 글 9000001 · 씬 글 8000330 ko/en 바꿈, 키 그대로). 적정(`PowNeed` · `PowNeedShown`)은 벽 피해 감산 · 보스 체력 등 내부 계산에 그대로 씀.
- 튜토리얼 `g_power` 대사 23~25 = '적정' 설명 빼고 "찍찍!! = 우리 힘, 쥐가 많아지고 세질수록 오름 → 막히면 키워서 다시 도전" · `g_power_hint` 29 = "더 강해지고 싶을 땐 꽉 채워 봐". 영어 다시 씀. 백업 `_backup_20261021/* (적정 숨김 전).xlsx`.
- Play 확인: HUD '찍찍!! 26.9K', 바뀐 대사 로드.

## 5-23. 증강 · 필살기 · 암시장 · 뽑기 묶음 (2026-10-21, 사용자 11개 요청 중 1~10)

1. **증강 고르기 20초 자동 선택**: `AugmentManager.pickAutoSec`(20) · 고르기 창 아래 `Pick/Timer` "{n}초 뒤에 가장 좋은 증강을 자동으로 골라요" (실제 시간, 게임 멈춤 중). 가장 높은 등급, 같으면 왼쪽 카드(Card1 = offer[0]).
2. **같은 증강 장착 불가**: `Augments.SameEquipped` — Equip · Swap 이 거절. 관리 창 버튼 = "같은 증강 장착 중"(못 누름, `sameLabel`). 고르기·자동 선택으로 받은 중복은 보관함으로.
3. **강화 = 같은 증강 소모 + 치즈** (강화 포인트 · `GivePoints` 없앰): 레벨 0→1 … 4→5 에 같은 증강 `up_copy_1~5` = 1 · 1 · 1 · 2 · 2 개 (증강 테이블 Aug_Config, 사용자: 점점 늘게 + 치즈로 적정선). 재료 = 장착 · 들고 가기 아닌 같은 id, 낮은 레벨 먼저. 로비 글 "강화 (같은 증강 {have}/{need} · 치즈 {cheese})" · 카드 "같은 증강 {pts}개 보유"(로비 자세히만). pt_per_floor · up_pt_* 는 '안 씀'.
4. **판 안 보관함 분리**: `Augments.RunItems` = 이번 판에 얻은 것(고르기 · 판 안 합성 · 장착에서 뺀 것). 관리 창 보관함은 이것만. 판이 끝나면 그대로 아웃게임 보관함(증강 연구소)에 남음.
5. **필살기 시간 정지 없앰**: 컷인 중 `UltFreeze`(timeScale 0) 안 씀 · `RunTimer` 가 필살기 중에도 흐름. 초필살기는 원래 안 멈춤.
6. **동시 필살기**: `UltimateManager.concurrentTier`(8훈장) 부터 대표(컷인 · 카메라) 1 + 컷인 없는 동시 필살기 `extras`, 최대 `maxConcurrent` 3. 그 전 훈장은 예전처럼 하나씩. `UltBase.ActorT` 필살기마다.
7. **Y 키 = 필살기 카메라 고정 켜고 끄기** (`nkk_ult_cam` 저장, 배너 "필살기 카메라 고정 켬/끔").
8. **암시장 조각 가격**: 유료 리롤마다 × `goods_reroll_growth` 1.2 (리롤 가격 ×1.5 와 함께, 자동 입고 때 원래대로).
9·10. **조각 수 = 훈장 비례**: 암시장 `BlackMarket.PackSize` · 뽑기 `GeneGacha.ShardsOf` = 테이블 값 × 훈장 ÷ `shard_full_tier` 16 (최소 1). 8훈장 = 절반.
- 백업 `_backup_20261021/* (11개 요청 전).xlsx`. 새 스트링 8000532~8000537 + 영어.
- **Play 확인 (슬롯 5)**: 자동 선택 · 중복 거절 · 판 안 보관함 1개만 · 동시 필살기 3개 · 필살기 중 제한시간 흐름 · Y 토글 · 암시장/뽑기 절반 · 조각 가격 ×1.44(리롤 2) · 강화 재료 소모 Lv 0→1.

## 5-24. 엔딩 컷씬 (2026-10-21, 끝 — 커밋 be3c2a0)

- **사용자 결정**: 엔딩은 오프닝처럼 간단하게 (컷 + 자막). 대본 = 아래 5컷 7줄 (사용자에게 보여 줌, 고칠 말 없었음).
  1 쓰러진 그럼피 · 빛 새는 마지막 문 "찍...! (마지막 문이... 열렸어.)" "찍찍! (그 너머엔 처음 보는 빛이 쏟아지고 있었어.)" / 2 해 뜨는 옥상 "찍찍찍!! (바깥이다!! 진짜 하늘이야!!)" / 3 빈 철창 · 손가락 "찍~ (이제 손가락의 은총 같은 건 필요 없어.)" / 4 들판으로 "찍찍... (치즈는 이제 우리 힘으로 찾을 거야.)" "찍!찍!!찍!!! (모두 함께라면 어디든 갈 수 있으니까!!)" / 5 쿠키 · 다른 연구소 불빛 "찍...? (그런데... 저 멀리, 또 다른 연구소의 불빛이...)"
- **끝난 것 (코드 · 표, 컴파일 오류 0)**
  - 튜토리얼 테이블 Story 시트에 `story` 칸 (비움 = 오프닝 · `ending`) + 엔딩 줄 13~19 (이미지 ending_01~05, 키 4000013~4000019, 영어 채움). `TutoStoryRow.story`.
  - `StoryPlayer.Play(onDone, which)` = story 칸으로 골라 재생 (오프닝은 그대로 12줄).
  - `Progress.EndingSeen / MarkEnding()` (저장 `endingSeen`). `StageManager.endingScene`("Title"): 마지막 층 클리어 → 처음이면 `TitleManager.PendingEnding = true` 로 타이틀 씬 → `TitleManager.Start` 가 메뉴 숨기고 엔딩 재생(건너뛰어도 본 것으로) → 로비. 두 번째부터는 예전처럼 바로 로비.
- **그림 (끝)**: Codex 사용량 한도로 **ChatGPT 웹(사용자 계정, 앱 내장 브라우저) 한 대화** "Story Cutscene Illustration" 에서 5장을 이어서 그림 (같은 대화라 화풍이 이어짐). `UnityResources/Rats/Ending/ending_01~05.png`(README · 원본 `Sheets/chatgpt/` · 5컷 요청 글 `Sheets/prompt_ending_05_chatgpt.txt`) → `Assets/Art/Rats/Story/` (1536×1024 = 4의 배수라 WebAtlas 에 안 묶임, 임포트 설정은 story_01 과 같게) → Title 씬 `UI/Story` `StoryPlayer.cuts` 에 ending_01~05 추가 · 씬 저장.
  - **ChatGPT 그림 내려받는 법**: 그림이 `blob:` + ChatGPT CSP 가 localhost fetch 를 막고, 내장 브라우저는 window.open 이 같은 탭을 덮어씀 → `Tools/recv_image.py <폴더> 8765` 띄우고, ChatGPT 페이지 JS 에서 blob → `File` → `DataTransfer` 로 `<input type=file>` 에 넣어 `<form method=POST enctype=multipart/form-data action=http://127.0.0.1:8765/upload>` 제출 (form-action 은 CSP 에 없음). 받은 뒤 서버 끄기 — **예전 세션 서버가 같은 포트에 남아 있으면 요청이 그쪽으로 감** (Windows). 내장 브라우저 ChatGPT 는 로그아웃될 수 있음 → 사용자가 로그인.
- **Play 확인 (끝, 2026-10-21)**: 슬롯 5 + `TitleManager.PendingEnding = true` → Title 씬 다시 열기 → 메뉴 숨고 엔딩 7줄 (컷 1·1·2·3·4·4·5, 자막 맞음) → 마지막 줄 뒤 로비 · `EndingSeen` 저장됨 · 콘솔 오류 0. 오프닝 줄 수 12 그대로. 확인 뒤 `nkk_slot` 1 로 되돌림. (100층 실제 클리어 경로·두 번째 클리어 때 안 나오는 것은 코드만 확인 — `StageManager` 518줄.)
- **남은 것**: 없음 (main-game 커밋 be3c2a0, mvp 병합은 따로).
- **Codex 실행 주의**: 원격 커밋 62b0d4b 로 들어온 프로젝트 `.codex/config.toml`(UnityMCP url) 이 전역 설정(stdio)과 부딪혀 **프로젝트 폴더 안에서 codex 가 시작을 못 함** ("url is not supported for stdio"). 작업 폴더(-C)를 프로젝트 밖(scratchpad 등)으로 두고 결과만 복사.

## 5-25. 필살기 쌓임 해결 · 컷인 대신 로그 (2026-10-21 밤)

- **사용자 요청**: 필살기가 계속 쌓임 → 일정 훈장부터 필살기 쿨 없애고, 카메라 고정(Y)을 끄면 쓰는 쥐 쪽 컷인·확대 없이 딜 미터기 아래 로그로.
- 원인: 8훈장 동시 필살기도 ① 대표 컷인(1.4초)이 끝나야 다음 것 시작 ② 동시 최대 3개 ③ 프레임마다 하나씩만 꺼냄 → 대기열이 쌓임.
- `UltimateManager`: `concurrentTier`(8) 부터 대기열을 **기다림 없이 그 프레임에 전부** 시작 (쿨 · 대표 컷인 기다림 무시) · `maxConcurrent` 0 = 제한 없음 (씬 0). 그 전 훈장은 예전처럼 하나씩. **카메라 고정 끔**이면 대표도 컷인 · 확대 · 화면 번쩍 없이 바로 상황극 (훈장 상관없이). 컷인 없이 시작한 필살기(동시 필살기 · 고정 끔)는 `log.Add`.
- `Ults/UltLog.cs` · 씬 `HUD/UltLog` (딜 미터기 바로 아래, 오른쪽 위 기준 (-24, -686), 줄 Row0~3: 배경 ui_card · Icon(필살기 아이콘) · Text `{name}의 필살기! {ult}` LocText 8000538 template / 영어 "{name}'s Ultimate! {ult}"). 새 줄은 맨 위로 밀려 들어오고 4초 뒤 사라짐, 멈춤 중엔 안 사라짐. 필살기 이름 = 필살기 색 (옅은 색은 진하게 `minSat`/`maxVal`).
- **Play 확인 (슬롯 5 = 8훈장, 콘솔 오류 0)**: 고정 켬 = 대표 컷인 1 + 나머지 4개 동시에 바로 · 로그 4줄 / 고정 끔 = 컷인 없이 4개 동시 · 로그 4줄. 확인 뒤 `nkk_slot` 1 · `nkk_ult_cam` 원래대로.
- 참고: 딜 미터기는 씬에서 **화면 오른쪽 위**에 있어서 로그도 오른쪽(미니맵 위)에 있음.

## 5-26. 보스 개편 — 그림 · 리깅 (2026-10-22, 진행 중 · 커밋 안 함) ★ — 명세 `UnityResources/Rats/BossRework/README.md`

- **사용자 결정**: 보스 설계를 전부 바꿈. 지금은 **그림 + 리깅 움직임 확인만**, 행동(평타 1·2 · 쿨타임 스킬 8초 · 발악 70%·35%)은 사용자가 나중에 전달. 5·10·15·20층 = 기존 그림 재사용 · 100층 = 예전 우주복 고양이(boss_commander). 층별 외형 결정은 README 표. 패러디는 실루엣·의상만.
- **그림**: Codex 시트 `Tools/gen_boss_rework_prompts.py` → `bash Tools/run_boss_rework_codex.sh <동시수> [시트]` (프로젝트 안에서 돌리면 `.codex/config.toml` UnityMCP url 이 전역 설정과 겹쳐 실패 → 임시 폴더에서 그리고 옮김) → 자르기 `python Tools/slice_boss_rework.py [시트]` (덩어리 '가운데가 칸 안' 기준으로 통째로 → 칸 경계에서 안 잘림. 사용자가 잘림 지적했던 것 고침) → `gen_sprite_pivots.py` → 유니티 Build Human/Cat/Rat Art Library · **NKK/Build Boss Part Art**.
  - 사람 리그: 버티컬 마우스 · 공룡탈 알바 · 랩터 사육사 · 굴착기 기사 · 심해 잠수부 · **좋았쓰 고양이(소환물, 두 발로 서서 가리킴 → 사람 리그)**.
  - 고양이 리그 (+ 두 번째 머리 `head2`): 팝 캣(옆모습, 입 보일 만큼만 돌림, 늘 뻐끔) · 노조 고양이 · 괴렘 · 총통 고양이 · 그렘린 고양이.
  - 전용 리그 `Hazards/BossPartRig.cs` + `BossPartArt.cs` (에셋 `Assets/Data/BossPartArt.asset`, 기본값 `Editor/BossPartArtBuilder.cs`): 거미 고양이 · 상어 고양이 · 물고기 고양이 · 차력거인(낮게 기는 네 발, 등에 고양이 포탑) · 항공모함 고양이(관제탑 = 기계 고양이 머리, 눈 = 창문) · 우주복 쥐(정체 = 위턱·아래턱 경첩) · 굴착기(탈것).
  - 시트 `_v1`·`_v2` = 사용자가 바꾸라 한 예전 판 (보관).
- **코드**: `Boss.cs` 보스 테이블 `rig` = `Part` 면 PartRig (Game 씬 `Boss/PartRig` 자식 + `partArt` 연결, 저장함) · 고양이 `head2` = 팝 캣은 `popHeadInterval` 마다 번갈아, 나머지는 공격·맞을 때 · `TableRows.BossRow.IsPart`.
- **보스 테이블**: `Tools/gen_boss_rework_data.py` → 95024~95041 (floor 0 = 층에 안 나옴, **테스트 패널 ◀ 보스 ▶ 로만**). 평타 = 리그별 기본, 쿨타임 스킬·발악 = 같은 층 지금 보스 것 그대로(사용자 지시). 소환물(좋았쓰·굴착기) 스킬 없음. 그렘린은 알·일반 고양이 단계 없이 바로 그렘린. 백업 `Data_Table/_backup_20261022/`.
- **확인**: 에디터에서 리그 조립 + 걸음 위상 4단계 · 두 번째 그림을 카메라로 찍어 봄 (Play 는 아직). Title 씬에 저장 안 된 변경이 있어 Game 씬은 추가로 열어 고치고 닫음.
- **리깅 고치기 2차 (2026-10-22 오후, 사용자 "합쳐진 것 이상한 게 많다 → 통 그림과 비교해서 확실하게")**: 상세 `BossRework/README.md` 끝 · LOG §9-23.
  - 도구: **`Editor/BossRigPreview.cs`** (`BossRigPreview.Shoot(폴더, "코드,코드")` = Game 씬 보스 리그 복제 → 대기·걸음 2·공격 4컷 PNG, 씬 안 바꿈. execute_code 에서는 `System.Type.GetType("BossRigPreview, Assembly-CSharp-Editor")` 로 부름) · **`Tools/match_parts.py`** (파츠를 통 그림 위에서 위치·각도로 찾음 + `rig_values` = BossPartArt at/pivot/angle 계산) · **`Tools/slice_boss_fix.py`** (cr5 tt5 hr4~hr9 cx4 cx5 자르기, `head2` = 고양이 두 번째 머리 관절점). 바뀌기 전 파츠 `BossRework/Parts/_old/`.
  - 원인 → 고침: 좋았쓰·팝 캣 관절점 안 들어감(재임포트) · **고양이 head2 관절점 없음 → 머리 바뀔 때 튐** · **사람 몸통이 정면이라 소매·팔이 그려져 팔 3~4개** → 옆모습 몸통 + 어깨 = 몸통 가운데(`SHOULDER`, 등 쪽 끝에 붙이면 안쪽 팔처럼 보임 — 사용자 지적) · 사람 리그 **먼 팔 별도 그림 `arm2`** (HumanArtLibrary·HumanRig, 사육사 반대쪽 손 · 원시인 쥐 빈손) · 사육사 목 가는 머리 · 팝 캣 목 달린 몸통 · 거미 머리 몸통 위 + **용접 목 받침** · 70층 = **머라이언 고양이**(새 그림, 공격 때 물줄기) · 80층 차력거인 = 사용자 스케치 자세 통 그림 + **팬티** + 상반신/엉덩이 분리 + 정강이/발 분리 + **포탑 고양이 사격 깜빡·반동 (`BossPartArt.Move.Recoil`, BossPartRig `recoilRate`·`recoilTilt`)**.
  - Codex 안전 필터: 차력거인 맨살 팔다리 확대 시트는 '선정성'으로 막힘(tt6 2번·tt7 1번, 플러시 버전은 그림이 달라 안 씀) → tt2 팔다리 그대로.
  - **관절 원 가리기 (사용자: 겹침 순서로)**: 위에 오는 조각 끝 원만 잘라 냄(`slice_boss_fix.py discs`, DISC_CUT: 차력거인 윗팔·허벅지 아래끝, 머라이언 몸 오른쪽 끝, 원본 `<파츠>_full.png`) + 아래 조각 관절을 그 몸 안쪽에 → 원이 뒤로 숨음. 차력거인 가까운(바깥) 팔은 몸통 위 + 어깨 원은 그 색만 키잉(`DISC_KEY`, 밑의 같은 살색 몸통이 비침). 상어 고양이 = **새 그림**(cx6_full 네 발 통 그림 → cx6: 허벅지가 몸에서 이어지는 다리), 다리는 몸통 뒤, 대각선 짝 걸음, 겹치던 지느러미 뺌. 머라이언 얼굴 0.85배 · 꼬리 1.3배(몸 끝 굵기에 맞춤). 차력거인 허리 = 몸통이 엉덩이 위(배가 팬티 허리선을 덮음).
- **차력거인 파츠 tt8 (관절 원 없음)** 적용 (`slice_boss_fix.py tt8`, 값 = `Tools/giant_rig_calc.py`). 엉덩이가 몸통 위 → 경계 = 팬티 허리선. 사람 보스 목 = 머리 목 앞선 ↔ 몸통 목 단면 앞선 (`slice_boss_fix.py necks`).
  - **끝 (ChatGPT 웹, Codex 한도)**: 상반신 = tt9 (`Sheets/chatgpt/tt9.png`, 어깨 관절 덩어리 없음, 0.68배) · 엉덩이 = tt8 (사용자: tt9 엉덩이 이상) · 윗팔 = tt8 을 팔꿈치(62%)에서 잘라 어깨~팔꿈치 (`slice_boss_fix.py limbs`, 사용자: 팔꿈치 두 개) · 머리 목 = 몸통 왼쪽 위 목 단면 · 앞 포탑 안장 0.32 (머리와 안 겹치게).
  - ChatGPT 웹 쓰는 법 (내장 브라우저): 참고 그림은 PowerShell `Clipboard.SetImage` 로 복사 → **사용자가 입력창에 Ctrl+V** (자동 붙여넣기·JS base64 첨부는 안 됨/깨짐) → 요청 글 입력 → 결과는 `Tools/recv_image.py <폴더> 8765` + 폼 POST (§5-24 방법, 이름 필터를 영소문자·숫자·밑줄 .png 로 넓힘).
- **차력거인 3차 (저녁)**: 웹 미리보기 `BossRework/rig_web/giant.html` 로 사용자 확인 후 유니티 반영 — 값 `Tools/giant_rig_v3.py` (팔다리 배율 가까운·먼 쪽 같게, 먼 팔다리는 어깨·엉덩이 관절 옆에서 IK, 엉덩이 관절 = 팬티 다리 구멍 선) · 새 파츠 ChatGPT tt10a~c (정강이·발 분리, 허리까지 늘린 엉덩이, 먼 쪽 주먹·발 따로) · 엉덩이 `Move.Wiggle` (멈출 때 한 번 출렁 + 공격 중 사격 박자). 상세 README 끝. BossRigPreview 로 확인 (Play 는 아직).
- **팝 캣 목 · 상어 고양이 다리 (밤)**: 웹 미리보기(`rig_web/giant.html?code=…`, 고양이 리그도 흉내)로 확인 후 유니티 — 팝 캣 머리 낮춤 + 목 짧게 자름 · 상어 다리 관절 몸 안쪽. README 끝.
- **보스 행동 구현 1차 (밤)**: 명세 `BossRework/boss_spec_20261022.md` (원문 + 질문으로 정한 것 + 이름 한/영). 데이터 `Tools/gen_boss_skills.py` → `gen_string_table.py` → `gen_boss_skills.py --english` → `xlsx2json.py` (다른 표는 다시 저장만 되니 git checkout). 25~97층 = 개편 보스 95024~95039, 100층 = 우주 고양이 사령관(95006, 스킬 = 버터 고양이 '잼 식빵 고양이 회전'), 예전 95007~95023 floor 0.
  - 코드: `Hazards/Boss.Rework.cs` 새 패턴 Fan · Parade · Orbit · Path · Rise · Slam · Smash · Swallow · Grow + Atk_Type 새 칸 `p3` · `opt`(head/head_keep/body/hide/ride/explode/near/all/red/shake/around/sticky/morph/mask/intro). 쥐 `Rat.Boss.cs` 삼켜짐(인구 유지)·멋대로 걷기·방독면. 발악 둘 다 쓴 뒤 번갈아.
  - 그림 없는 기술(br_*)은 Boss.placeholderSprite (FX_CatWarn/cat_target) 로 보임 → 그림은 나중에 한꺼번에.
  - **기술 그림 br_* 34장 끝 (2026-10-22 저녁)**: Codex 시트 3장 `Tools/gen_boss_fx_prompts.py` → `bash Tools/run_boss_rework_codex.sh 3 ef1 ef2 ef3` → `python Tools/slice_boss_fx.py` → `BossRework/FX/br_*.png`(+ `_preview.png`) → `Assets/Art/Rats/BossMeme/` → Game 씬 Boss **Fill Meme Sprites** (70장, 저장함). ef1 = 소품 18(HUH 글자·우는 빵 4·탈·산탄·마우스 운석·철근 가시·전자레인지·빨간 버튼·방독면·그림 도구 4) · ef2 = 변신 6(괴렘 봤구나 머리·괴렘 식빵·허 고양이 머리·노조 확성기/전화·공룡탈 담배) · ef3 = 철근 거미줄·꼬마 머라옹·새끼 그렘린·수염. 재사용 6 = club · jet2 · rg_siren · smoke · emperor fx_bullet(뒤집음) · vmouse 머리. 머리 교체 2장은 원래 머리 픽셀 크기로 줄이고 관절점 `BossRework/FX/pivots.json` → `gen_sprite_pivots.py` 가 읽음. 표의 그림 이름 132행 전부 찾아짐. **Play 확인 (같은 날)**: 테스트 보스(체력 1e15)로 실험체 봤구나 머리·식빵 변신+빵 낙하 · 허 고양이 HUH 글자 · 공룡탈 담배 · 노조 전화 · 거미줄 낙하 · 그림 도구 던지기 · 새끼 그렘린 · 쥐 전투기 · 빨간 버튼(specialProp) 확인, 에러 없음. **철근 거미줄 = 떨어진 자리에 2초 남았다 사라짐** (사용자, R_RebarWeb p1 5 · p2 2 · size 600→720 = 경고 원 지름, `gen_boss_skills.py` 도 같게 · Play 확인) · 보스가 반투명해 보이는 건 맞을 때 흰 번쩍임(기존). 확인 요령: `FxManager.BaseTimeScale` 를 0.1~0.4 로 낮추면 빠른 낙하도 찍힘 (끝나면 1).
  - 아직: Play 확인 · 94 그렘린 알→일반 고양이→변신 흐름 · 97 우주복 쥐 동료 합류 흐름 · 35 공룡 꼬리·몸 기울이기 · 새 그림.
- **97층 우주복 쥐 다시 (2026-10-23)**: 사용자 "어몽어스 패러디 이상함, 스킬도 블랙홀 복붙" → 웹 미리보기 `BossRework/rig_web/crewmate.html` (리그 + 쥐 40마리 + 합류→변장→변신→모든 기술 버튼, 값 고치기 → C# 줄) 에서 동작 확인 후 유니티. 원인: 머리를 alt(공격·맞을 때 그림)로 바꿔 맞을 때마다 괴물 입이 깜빡 · 촉수 삼키기 = 블랙홀 소용돌이 + 쥐 순간 사라짐 (촉수 그림 안 씀) · 긴급 회의 = 화면 깜빡뿐. 고친 것: 파츠 `show`(Always/Closed/Open)·`open`(열림 각도) + `BossPartRig.open` → 머리 = 아래턱 + 위턱 경첩, 등장·촉수·덮치기(공중)·뱉기 때만 열림 · `Boss.Crew.cs` 촉수 6갈래(br_crew_tongue)가 잡아 입으로 → 한 마리씩 앞으로 뱉음(우물우물) · 새 패턴 Meeting = 빨간 버튼 폴짝 → 긴급 회의 화면(얼굴 br_crew_face) → 맵 전체 기절 · 사이렌 opt alarm (붉게 깜빡) · 쥐 `Grab`(촉수에 잡힘). BossPartArt.asset 은 YAML 로 직접 고침(빌더와 같은 값). 같은 날 발 뜸 고침 (몸통 0.95→0.817, 앞다리 1.1배). **Play 확인 (테스트 보스, 체력 1e15)**: 컴파일 OK · 등장 촉수(머리 열림→촉수→삼킴→뱉기 전 입 열림) · 사이렌 4개 + 붉은 깜빡 · 긴급 회의(버튼 → 회의 화면 괴물 얼굴 → 125/125 기절) · 오류·경고 0.
- **남음**: 사용자가 Play(테스트 패널)로 움직임 확인 → 값 조정은 `BossPartArtBuilder.cs`(다시 만들면 에셋 값 덮어씀) · 보스 행동 명세 받으면 행 정리(실제 층 배치) · 94 그렘린/97 우주복 쥐 연출 · 소환물 스킬 연결 · 커밋.

## 5-27. 증강 기획서 Ver.04 반영 (2026-10-23, 진행 중 · 커밋 안 함) — 결정 `기획서/증강 기획서 Ver.04 반영 결정.md`

- **사용자 결정**: 증강 51개 → 기획서 32개로 교체 (모래시계 줍기는 삭제), 수치는 대충(나중에 밸런스 한꺼번에), 세이브의 없어진 증강 → 같은 등급 새 증강 (`Aug_Config replace_<옛 id>`, `Augments.Migrate`), 확률표 다시 (브론즈 4개뿐). 동작 결정은 결정 문서.
- **표**: `Tools/gen_augment_table.py` (A 32 · OFFER · REPLACE) · `Tools/gen_augment_special.py` (19: 칼날 확률형 · 클릭 폭격 확률형 · 천둥 20초 · 새 Midas · Tower · CheeseRain · Unify ×5 · Clones · Meteor · Train) → gen_string_table → `gen_augment_table.py --en` → xlsx2json. 백업 `Data_Table/_backup_20261023/`. 새 팝업 글 8000539~8000549 (씬 키 연결함).
- **코드**: `AugmentSpecials.cs` (다시 씀: 예고 원 WarnThen · 칼날 = 때린 쥐 둘레 잠깐 · 클릭마다 폭발 · 분신 · 운석 · 치즈 비 · 기차 · 연쇄 붕괴 글자 · 승급 폭죽 3발 · 잭팟 슬롯) · `AugmentSpecials.Rats.cs` (등급 통일 · 쥐 탑 · 상시 표시) · `Rat.Augment.cs` (탑 쌓기 그림 · 공 굴리기 · 다다다다 잔상) · 훅 Rat(Bump·RunSpeed·MultiHit·Pose) · RatManager(RollSpecies·SpeciesOfGrade·Replace·승급에서 탑 빼기·클릭) · ItemManager(미다스·치즈 비) · Item(황금 반짝) · 패티 최대 · 5G 전체 전송. 눈덩이·세포 분열·시간 역행·필살기 메아리 코드는 뺌.
- **그림 (Codex, `Tools/run_aug_v04_codex.sh` → `Tools/slice_aug_v04.py`)**: 아이콘 11 (`Art/Augment`, AugmentArt Fill Icons) · 스티커 9 (`Art/FX/aug`, Game FxManager stickerSprites) · 양복 대머리 쥐 `Parts/skater_suit` (Build Rat Art Library). 사용자 "그림체가 너무 벗어남" → 실제 게임 그림을 모은 참고 그림(`Sheets/v04/ref_*_game.png`, `ref_rat_parts.png`)으로 다시 (처음 것 `_v1/`).
- **Play 확인 (테스트로 Augments.Equipped 에 직접)**: 쥐 46 → 탑 5개(4마리씩 합침) · 일반 쥐 17 → 골든 햄스터 공 · 분신 4 · 거대화 · 오류 0. 테스트로 넣은 증강 20개는 세이브에서 지움(보관함 0 · seen 비움). 먹구름 등 상시 그림이 화면 위 밖에 있던 것 고침(높이 0). 맘스땃쥐 6~8층은 그림 늘림(새 그림 없음).
- **이어서 (2026-10-23 낮)**: 아이콘 3차(무광·흐리게, `Sheets/v04/aug_icons_e.png`, 2차 시트 = `_v2/`) 잘라 넣음 → 기존 게임 아이콘과 나란히 비교해 질감 맞음. **상시 그림·기차·칼날이 안 보이던 버그**: 매 프레임 `sticker.life = 99999` 로 되돌려 FxManager 나타나기 투명도 `(max - life)/0.15` 가 늘 0 (에디터가 느릴 때만 흐릿하게 보였음) → `Look()` · `UpdateTrains` · `UpdateBlades` 에서 그 줄 뺌. 상시 먹구름은 기획서 '상시 = 작게'대로 `stormLookSize` 260 (예전 520 이 층 클리어 글자를 가림, 큰 `cloudSize` 는 치즈 비 발동용). Play 확인: 먹구름·슬롯머신·5G 신호·발자국·기차·운석 예고 원 보임, 오류 0. 테스트 증강·seen 은 세이브에서 지움.
- **남음**: 사용자 눈 확인 (아이콘 3차 · 상시 그림 크기·위치) → 커밋 (§5-26 보스 작업과 함께 아직 커밋 안 함).

## 5-28. 잘린 그림 · 치즈 창고 흰 네모 아이콘 (2026-10-23)

- **흰 네모 원인**: 치즈 창고(훈장별 스킬 트리) `SkillPage.iconSprites` 를 손으로 채워 왔는데 100층 확장 때 새 아이콘 4장(`cs_autopromote` · `cs_needdown` · `cs_promote2` · `cs_skip2`)이 안 들어가 그 노드가 흰 네모. → `SkillPage` 컴포넌트 메뉴 **Fill Icons (SkillIcons · rk_*)** 추가 (SkillIcons 폴더 전부 + rk_1~ 끝까지) → Lobby 씬 실행·저장 (54 → 58장). **새 스킬 아이콘을 넣으면 이 메뉴 다시.** 훈장 배지 배열(RankPage·SkillPage)은 16장 다 있었음.
- **잘린 그림 찾기**: `Assets/Art` 전체 PNG 가장자리에 불투명 픽셀이 닿은 것 → 눈으로 확인. 원인 = 시트를 칸으로 딱 잘라 칸 밖으로 나온 부분이 잘리거나 옆 칸 부스러기가 묻음.
- **새 도구 `Tools/reslice_blobs.py`**: 시트 전체에서 덩어리를 찾아 '가운데가 있는 칸' 기준으로 통째로 자름 (`<시트> <열> <행> <칸번호>=<저장경로,…> [--join=0]`, 위아래 그림이 붙어 있으면 `--join=0`).
- **다시 자름**: 훈장 rk_9~16 + 새 스킬 아이콘 4 (`Expansion100/Sheets/badges.png` 4×3, join 0) · 보스 소품 boss_butter_cat · boss_maxwell (`boss_skill` 3×2) · boss_money · boss_shark (`boss_meme` 4×2) · boss_pallbearer_a/b (`boss_meme2` 2×2, 관 뚜껑 잘렸던 것) · rock_stage · rock_speaker (`UltProps/Sheets_rock` 3×3) · 증강 스티커 magic_circle · slot_machine (`Augment/Sheets/v04/aug_fx` 3×3). UnityResources·Assets/Art 둘 다 덮어씀, 예전 것은 git 기록.
- **안 고친 것**: `Rats/FX/cheese_wheel`(치즈 바위 필살기) 위·오른쪽 몇 픽셀 평평 — 원본 시트 기록 없음, 필요하면 Codex 로 새로. `Lobby_Market/lm_*`(옛 믹서기, 안 씀) 잘림·부스러기 그대로. 쥐·사람·고양이 리그 파츠는 관절에서 끊긴 게 정상이라 이번 검사에서 뺌.
- **100층 확장 물건 31개도 잘려 있었음** (사용자: 공룡 뼈 `f_trex` 꼬리 등): z1~z6 물건 시트(6×3)를 덩어리 방식(join 0)으로 다시 잘라 지금 그림보다 커지는 것만 교체 — 티렉스 화석 · 지프 · 굴착기 · 탄약 선반 · 책상류 · 캣타워 · 츄르 자판기 · 상자 · 노트북 · 실뭉치 · 호박 속 모기 · 황금 프라이팬 · 모노리스 등. `slice_expansion.py` 의 `grid()` 도 같은 방식으로 바꿈 (다시 돌려도 안 잘림). join 6 은 `f_snack_counter`·`f_ammo_depot` 이 옆 물건과 묶여서 안 씀.
- **등급 통일 쥐 크기 (사용자)**: 변신한 쥐 크기 × `Aug_Config unify_scale` 1.4 (장착 중만, 공 모양 포함) · 쥐별 덮어쓰기 `unify_scale_<character_id>` → 맘스땃쥐 `unify_scale_10085` = 1 (원래 잘 보임). 코드 `AugmentSpecials.RatScale` × `UnifyScale`. Play 확인: 골든 햄스터 1.4 · 맘스땃쥐 1.
- **튜토리얼 쉬운 벽 체력 2.5배 (사용자: 너무 바로 깨짐)**: `StageManager.firstRunWallMul` 0.3 → 0.75 (Game 씬 저장 + 코드 기본값). 첫 판·튜토리얼 라운드 1층 시작 방 옆 벽 하나.
- 표 다시 만들 때 주의: `gen_augment_table.py` 만 돌리면 이름·설명 `_sk` 키가 빠짐 → 스크립트 첫 줄 순서대로 4단계 다 (gen_string_table 은 다른 xlsx·StringTable.json 순서만 바꿔 놓으니 git checkout).
- **남음**: 사용자 눈 확인 (치즈 창고 9~16훈장 트리 · 보스 소품 · 확장 물건 · 변신 쥐 크기 · 튜토리얼 벽) → 커밋.

## 5-29. 새 보스 '쥐 히드라' (2026-10-23 오후, 모델 끝 · 커밋함) — 명세 `UnityResources/Rats/BossRework/README.md` 끝

- **사용자 결정**: 쥐 몸이 끔찍하게 뭉친 히드라. 목 7개 · 머리 = 차력거인 쥐 머리(tt10) 계열 · 덩어리 몸통 + 쥐 다리 8 + 꼬리 4 · 징그러움 중간 (피·내장 없음). 작업 중 추가: 몸통에 쥐 머리 더 많이 · **박힌 각도 제각각** · **위 목 머리 7개도 제각각 움직임**. 이번 지시: 리깅 → 웹 확인 → **유니티엔 모델만 넣어 두기** (보스 테이블·층·행동은 아직) → GIF.
- **그림**: Codex 시트 hy1_full(통)·hy2·hy3·hy4·hy5 → `BossRework/Parts/boss_hydra/` (= `rig_web/hydra/`). 몸통 = **hy5 `body_chaos`** (머리 각도 제각각, 사용자 요청에 맞춤 · hy4 `body` 는 웹 미리보기 '몸통' 고르기로 비교 가능).
- **리그 값 = `Tools/hydra_rig.py`** → `rig_web/hydra_parts.json` (+ `--cs` = BossPartArtBuilder 줄, 확인 그림 경로 주면 PNG). 원래 목 마디(neck1~3)는 길고 굵기가 달라 이음새마다 배 줄무늬가 어긋남 → **neck2 하나로 짧은 마디 `neck1s~3s`** (왼쪽 둥근 끝 + 오른쪽 둥근 끝 50px 이어 붙임, 자르기만) · 마디 순서 몸 쪽이 위 → 둥근 끝끼리 겹쳐 관절 안 보임. **목 각도 = `solve_neck`**: 몸통 자리 → 머리 관절 목표 · 머리 각도 · 뒤집기 · 모양(S = 한 번 꺾였다 반대로 뒤틀림 / C = 한쪽으로 휨)을 주면 각도·길이를 찾음 (표 `NECKS`). **머리 방향 제각각** (사용자 "한 방향 말고 여러 각도로 뒤틀려"): 0·2 고개 숙임 · 1 듦 · 3 하늘 보고 울부짖음 · 4·5·6 뒤(오른쪽)를 봄. 뒤를 보는 목은 **사슬째 첫 마디(neck1 flipX)에서 뒤집음** (사용자 "목 리그도 방향 맞춰서" — 머리만 뒤집으면 목이 180° 돌아 배 줄무늬가 위로 감), IK 는 몸통 좌표 실제 각도로 바꿔 풀고 되돌림. **목 움직임 = IK** (사용자 "목 움직임이 이상함" → 예전엔 마디마다 각도를 흔들어 몸 쪽부터 쌓여 목 전체가 와이퍼처럼 휘청·머리 까딱): 머리 관절 목표 = 쉬는 자리 + 떠다님(8자, 목마다 빠르기·위상 다름) + 물기(뒤·위로 wind 70px → 앞·아래로 strike 230px) → CCD 로 마디 3개 (쉬는 각도 ±40°), 머리는 쉬는 절대 각도 + 끄덕임 → 거의 수평. 값은 첫 마디(amp = 떠다님 px · speed · phase · wind · strike), 머리 amp = 끄덕임 °. 파이썬 `neck_ik` · 웹 `neckIK` · 유니티 `BossPartRig.NeckIK` 세 곳 같은 계산 (각도 소수 셋째 자리까지 같음 확인). 박힌 머리 6 = Twitch (각도·좌우 뒤집기 제각각). 다리 8 (먼 4 어둡게, 1.25배) · 꼬리 4 부채꼴.
- **웹 미리보기 `rig_web/hydra.html`** (launch.json `boss-rig-web` → `http://localhost:8772/rig_web/hydra.html`): 대기·걸음·물기(한 목씩)·일제히 물기·맞음, 몸통 고르기(body_chaos/body), 좌우 뒤집기(flipX) 지원, C# 줄 = 빌더 `P(..., flip: true, neck:, wind:, strike:, stretch:)` 형식. 확인함 (콘솔 오류 0).
- **GIF `rig_web/hydra.gif`** (`Tools/hydra_gif.py`, 14.5초 7.9MB: 걸음 → 대기 → 한 목씩 물기 7번 → 일제히 물기 → 맞음, 공통 팔레트 96색).
- **유니티 (모델만)**: 그림 19장 `Assets/Art/Rats/BossParts/boss_hydra/` · `BossPartArt.Move` 에 **Neck · Twitch** + Part 칸 `neck` `wind` `strike` · `BossPartRig` = Neck(`NeckIK`, 공격(alt) 중 `biteOrder` 차례 목이 물기 + 찌르는 동안 head_bite, 인스펙터 `neckLimit` 40 · `neckIters` 8) · Twitch(가끔 몸부림 + 표정 바뀜) · 인스펙터 `biteTime` 0.9 · `biteWind` 0.3 · `biteStrike` 0.55 · `biteOrder` · `BossPartArtBuilder.Hydra()` (length 300 · height 380) + **`AddHydra()`** = 다른 보스 값은 두고 히드라 줄만 에셋에 넣음 (execute_code: `System.Type.GetType("BossPartArtBuilder, Assembly-CSharp-Editor").GetMethod("AddHydra").Invoke(null, null)`). `BossPartArt.asset` 에 들어감 (47파츠, 그림 빠짐 0) · 컴파일 오류 0 · 에디터에서 리그 조립해 대기·걸음·공격 찍어 봄 (웹과 같음). 씬은 안 건드림.
- **시도 후 되돌림 (같은 날 저녁)**: 목·머리를 쥐 여러 마리가 엉겨 붙은 그림으로 바꿔 봄 (Codex `hy6` 목 마디 6종 `ratneck_a~f` · `hy7` 머리 2종×3표정 `ratheadA/B`, 목 굵기 230 · 몸통 1.45배까지) → 사용자 "그냥 이전 모습으로" → 매끈한 목(neck1s~3s)·머리(head*) 그대로. 그림·프롬프트는 `BossRework/Sheets/hy6·hy7` · `Parts/boss_hydra/ratneck_*·rathead*` 에 보관만 (유니티·웹에는 안 넣음).
- **남은 일**: 보스 테이블 행(95042~?)·층·행동은 사용자에게 (`BossRow.rig = Part`, code `boss_hydra`) · 맞음 그림 `head_angry` 는 웹 전용 (유니티 BossPartRig 엔 맞음 입력이 없음).

## 5-30. 증강: 화면에 붙는 그림 없앰 → 맵에 소환되어 공격 · 이펙트 그림 플랫으로 (2026-10-23 저녁, 커밋함 4b7d1f0)

- **사용자**: "화면 위에 먹구름 같은 거 붙이지 말고 맵에 일정 시간마다 소환되어서 공격하는 걸로" → 그다음 "그림체가 달라서 플랫한 느낌으로 다시 · 그런 느낌인 것들 다".
- **코드** (`AugmentSpecials.cs` · `.Rats.cs`): 상시 그림(`Look`·`looks`·`stormLookSize`) 삭제. **천둥 번개** = p1 초마다 대상(보스 → 고양이 → 화면 속 물건 → 열린 방) 근처 맵에 먹구름 소환(`SummonCloud`, 높이 `cloudHeight` 300 · 좌우로 `cloudDrift` 떠다님 · 바닥 범위 원 `stormRingColor`) → p4 초 동안 반경 p5 안 대상에 번개 p2 번 (`Strike`, 번개는 구름 높이에서). **치즈 비** = p1 초마다 맵에 치즈 구름 → p2 초 동안 반경 p5 안에 치즈 조각 + 0.25초마다 피해 (초당 평균 공격력 × p4, `RainHit`) · 그동안 치즈 +p3 (예전처럼 전체). 층이 바뀌면 구름 정리(`EndClouds`). 가랜드 = 탄생 자리에 잠깐(0.6초에 하나) · 발자국 = 거대 쥐 지나간 자리(0.7초마다, 1.6초) · 슬롯머신 = 층 클리어 때 계단 옆 · 5G 신호 = 5G 쥐 머리 위 가끔. 크기 인스펙터 `garlandSize`·`slotSize`·`signalSize`.
- **테이블**: `gen_augment_special.py` Lightning p4 3 (구름 머무는 시간) · p5 400 (번개 범위) / CheeseRain p4 1.5 (초당 피해) · p5 280 (비 범위) → xlsx · JSON. 증강 설명 스트링은 그대로.
- **Play 확인**: 컴파일 오류 0 · 먹구름이 맵 위에 뜨고 바닥 원 안 물건에 번개 (캡처함) · 치즈 구름 생성 확인. 테스트 증강 4개·seen 은 세이브에서 지움. (세이브가 강해 층이 몇 초 만에 넘어가 구름이 층 바뀜에 정리되는 걸 봄 — 정상.)
- **증강 아이콘 32개 플랫으로 다시** (사용자: "증강도 그림체가 다름"): 내용 참고 = 지금 아이콘을 같은 격자로 모은 `Sheets/v04/ref_icons_fa·fb·fc.png` + 그림체 참고 `ref_flat_game.png` → Codex 시트 `aug_icons_fa`(12) · `fb`(12) · `fc`(8, 4×2) → `slice_aug_v04.py` `FLAT` 목록 → `Art/Augment/aug_*.png` 덮어씀. 부드러운 판 = `Sheets/v04/_v3/aug_icons_e.png` · git 기록. 스케이터 아이콘 = 손발 롤러스케이트.
- **스케이터 생쥐 = 겟 투 워크 패러디** (사용자: "스케이트를 안 타고 있음 · 팔 다리에 스케이트 · 움직임도 스케이트 타는 것처럼 · 웹에서 고치고 넣어"): 그림 `rat_suit` 다시 (앞발 = 손에 끈으로 묶은 롤러스케이트, 뒷발 = 롤러스케이트, 머리·몸통·꼬리 그대로, 이전 판 `_v2/rat_suit.png`) → `Parts/skater_suit` · 피벗 = `sprite_pivots.json` · `RatRigMeta.json` 의 skater_suit 값만 고침 (gen_sprite_pivots.py 를 돌리면 **삭제한 메이드 쥐가 다시 들어가니** 통째로 쓰지 말 것 — UnityResources pivots.json 에 maid 가 남아 있음) → Build Rat Art Library.
  - **동작** (`Rat.Augment.cs SkatePose` · `SkateStop` · `SkateCoast`, `Rat.cs` 훅): 걷기 대신 — 달릴 땐 뒷다리 번갈아 뒤로 차기(새 돌진마다 다른 다리부터) · 관성 활주(네 발 바닥) · 멈출 때 바로 서지 않고 미끄러짐(마찰 `friction`) + 브레이크(몸 젖히고 앞발 듦, 불꽃·먼지) · 벽에 튕겨 미끄러짐 · 총공격 = 슈퍼맨 자세 + 방향이 천천히 바뀜(가속 3, 일반 8) · 제자리 = 바퀴 위에서 앞뒤로. **바닥 맞추기** `RatRig.GroundBob` (가장 낮은 바퀴가 바닥에 닿게 bob 자동, 웹과 소수 셋째 자리까지 같음 확인).
  - **값 = AugmentSpecials 인스펙터 `skate` (SkateTune)** = 웹 미리보기와 같은 이름. **웹 `UnityResources/Rats/Augment/rig_web/skater.html`** (launch.json `rat-rig-web` → `http://localhost:8773/Augment/rig_web/skater.html`): 자동(게임 흐름)·밀기·관성 활주·브레이크·쭉 미끄러짐·제자리 · 슬라이더 · 바닥 타일 흐름 · JSON 복사. 웹 JSON 넣기 (Play 밖, Game 씬): `UnityEngine.JsonUtility.FromJsonOverwrite(json, UnityEngine.Object.FindFirstObjectByType<NKK.AugmentSpecials>().skate)` → 씬 저장.
  - **몸통이 안 보이던 버그 (사용자: "계단에서 몸통이 없음")**: 새 torso 가 276×152 (4의 배수) 라 `WebAtlasBuilder` 가 웹 아틀라스에서 뺐는데, Play 중 그 스프라이트 텍스처가 null (예전 아틀라스 묶임이 남음) → torso 다시 임포트(ForceUpdate) + `SpriteAtlasUtility.PackAllAtlases` 로 고침, Play 확인. **파츠 그림 크기를 바꿨는데 Play 에서 한 파츠만 안 보이면 이것부터 의심.**
  - 겟 투 워크 자료 (Steam·namu.wiki): 대머리 회사원 · 팔·다리 네 곳 롤러블레이드 · 점프 없이 관성·속도 · 붙잡기. 자세한 애니메이션 자료는 못 찾음 → 동작은 위처럼 직접 설계.
- **그림 플랫으로 다시**: 증강 이펙트 시트 `aug_fx` (먹구름·치즈 구름·그을음·기차·마법진·가랜드·발자국·슬롯머신·5G 신호)가 광택·입체 → 참고 그림 `Sheets/v04/ref_flat_game.png` (게임 바닥 물건 캡처 + UI 키트) 로 Codex 다시 (`bash Tools/run_aug_v04_codex.sh aug_fx` → `python Tools/slice_aug_v04.py`). 광택 판 = `Sheets/v04/_v2/aug_fx.png`.

## 5-31. 1시간 단위 페이스: 1시간에 깨는 층이 점점 줄게 (2026-10-26, 커밋함 2191ba26) ★

- **사용자**: "1시간 단위로 잘라서 점점 1시간에 깨는 층이 줄어드는 구조로 · 인크리멘탈 게임 난이도 설계와 비슷한 방향". 그 전 검토 = `기획서/성장 요구량 기획서 Ver.04 검토 결정.md` (그 문서의 단계 모델 조정안은 이 방식으로 대체).
- **문제 (바꾸기 전 balance_sim)**: 클릭 시간당 새 층 `[14, 8, 11, 12, 6, 12, 13, 24]` — 평평하고 **마지막 시간이 가장 빠름** (92층 뒤 적정 ×1.03 평지 · 후반 훈장 전투력 차 ×1.3~1.4 뿐이라 허들 3~4판 막힘 → 승급하면 한 판에 13층 질주).
- **목표** `tier_gates.HOUR_FLOORS` = 클릭 `[21, 18, 15, 13, 11, 9, 7, 6]` (8h · 100층). 인크리멘탈 원칙: 요구치(지수) > 생산 → 저절로 느려짐 · 마일스톤 배율로 잠깐 앞지름 · 다음 목표까지 시간이 기하로 늘어남.
- **바꾼 것**
  - `tier_gates.py`: `GATE` 6·12·18·25·31·37·44·50·57·64·70·76·81·86·91 (승급 = 1~2시간째 3번 → 3~5시간째 2번 → 6~7시간째 1번) · `T16_FULL` 92 → **100** (16훈장 트리가 90층대 페이스, 최종 보스 = 트리 전부) · 비용 배율 훈장별 `RANK_K_T` · `TREE_K_T` (`rank_k(t)` · `tree_k(t)`, 14~16훈장 승급 ×5.4) · `T16_TREE_K` 0.043 · `SKIP_BACK` 70층까지 3 · 71층~ 5.
  - **훈장 승급 배율 (새 칸)** `RANK_MUL_LO/HI` 1.115 → 1.48: 승급마다 모든 쥐 공격력 × (2훈장 1.115 … 16훈장 1.48), 누적 16훈장 ×47. 트리 공격력 % 는 더하기라 후반 승급 체감이 줄어서 곱하기 마일스톤으로. 티어 테이블 `rank_mul` 칸 (gen_expansion_data.tiers 가 씀) → **코드** `TierRow.rank_mul` · `CommonSkill.RankMul` · `Rat.Damage` 에 곱함. 기존 `rat_atk_mul` 은 그대로 (안 쓰임).
  - `gen_stage_table.py`: 허들 아닌 층 목표 초 × `TIME_RAMP` 0.8 → 1.534 (1층 → 100층, 초반 벽 얇게 · 후반 두껍게) · 추세 앵커 = 훈장 끝 전투력 × `rank_mul` · `skip_back(f)`.
  - `balance_sim.py`: 무리 전투력 × `TG.rank_mul(훈장)`. **새 도구 `Tools/hourly_check.py`** = 시간당 새 층 · 승급 시각 · 판 기록(`--runs`).
  - 숫자는 좌표 탐색으로 맞춤 (점수 = 누적 층 오차 + 시간당 오차 + 앞 시간보다 늘면 벌점).
- **결과 (balance_sim)**: 클릭 **8.29h** `[22, 13, 13, 13, 11, 11, 6, 6, 5]` · 미클릭 **10.62h** `[14, 15, 9, 10, 10, 10, 10, 5, 5, 5, 7]`. 승급 클릭 0.45 · 0.67 · 0.99 · 1.24 · 1.67 · 2.29 · 2.85 · 3.21 · 3.68 · 4.37 · 4.89 · 5.4 · 6.38 · 7.21 · 8.08h (간격 0.2h → 0.9h). 적정 100층 1.22억 → 42억.
  - 2시간째(13층)가 목표 18보다 처짐: 판 길이(15~25분)와 30층 허들이 겹치는 구조 — 6·7훈장 비용을 낮추면 4·8시간째가 대신 튐 (1.6~2.2 시험).
- **확인**: 유니티 컴파일 오류 0. 측정(BalanceProbe)을 다시 하면 power_by_tier.json 에 승급 배율이 섞이니 rank_mul 로 나눠 넣을 것.
- **Play 측정 round10 (같은 날, 클릭·필살기 O, `Tools/probe_results/round10_hourly.txt`)**
  - 승급 배율 붙음: 16훈장 레어 쥐 Damage 3,122만 = (18 + 고정 2,879) × 트리 67.1 × 성장 × **RankMul 46.978**.
  - **초반 = 시뮬보다 어려움**: 트리 전부인데 1훈장 5층 · 2훈장 10층 · 5훈장 25층 실패 (0.27~0.39배), 6훈장 31층 시작 → 32층 실패 (25층 시작이면 31층까지 통과 — 판 안 쥐 수 차이).
  - **후반 = 시뮬보다 쉬움**: 15훈장 트리 전부 86~91층(90층 허들 포함) 25~77초 · 16훈장 **트리 0** 으로 91→100층 한 판 · 트리 전부는 91~99층 17~38초. 13훈장 트리 전부는 80층 허들 실패(설계대로).
  - 원인: 실제 판 쥐 수 10~25마리 (시뮬 = 최대 마릿수 400~525 포화 가정) · 후반은 적정의 0.15~0.4배로도 깸 (필살기·증강·벽 피해 % 등 전투력 밖 피해) → **balance_sim 시간 모델을 이 측정으로 보정해야** 시간당 층 수가 믿을 만함.
  - BalanceProbe 버그: 10~15훈장 승급 조건 '트리 전부'가 cond4 라 `MakeMeta` 가 못 봄 → 노드 -1 이면 트리 0개. 지금은 노드 99 로 넘겨서 잼. 16훈장 100층 클리어 = 엔딩 → 로비로 넘어가 '끝' 이 안 찍힘. → **아래에서 고침**.
- **실측 보정 · 재튜닝 (같은 날 이어서, 사용자 "round10 측정으로 balance_sim 시간 모델 보정하고 다시 맞춰 줘 · cond4 버그도")** ★ 위 결과 숫자를 대체
  - **BalanceProbe** (`Core/BalanceProbe.cs`): ① `MakeMeta` 승급 조건 cond1~**4** 를 봄 (노드 -1 = 트리 전부 맞게) ② RowsCsv 칸 추가 `tier,tree,start,n,rats,pow,need` (뒤에 붙임, update_spu 영향 없음) ③ 마지막 층 클리어로 씬이 바뀌면 `OnDestroy` 에서 마무리하고 다음 측정 ④ `FakeRuns` (>0 = 판 횟수, 첫 판 '쉬운 방' 없이 잼). 16훈장은 다음 훈장이 없어 노드 -1 = 트리 0 → 트리 전부는 노드 99.
  - 추가 측정: round11 (3~14훈장 허들, 시작 = 허들 −4) · round12 (1~3훈장 트리 0/절반/전부, FakeRuns 5) · round13 (새 곡선 확인) → `Tools/probe_results/round1x_*.txt/csv`.
  - **알게 된 것**: 판 안 쥐 수는 훈장과 거의 무관하게 **판 첫 층 ~9마리 → 층당 +1.1마리** (시뮬 예전 가정 = 최대 마릿수 포화 32~420 → 후반 전투력을 10~20배 과대). 13~16훈장 쥐 한 마리는 power_by_tier 추정보다 2.5~8배 셈. 후반은 필살기·증강 등으로 적정의 0.2~0.5배에도 깸. 1층 실제 벽 = 테이블의 2% (초반 계단 벽 완화). **최대 마릿수 상한(후반 400~525)은 실제로 거의 안 닿음 → 마릿수 노드는 체감이 없을 수 있음 (따로 볼 것)**.
  - **새 도구 `Tools/calib_from_probe.py`** → `probe_results/sim_calib.json`: ① 찍찍!! = 시뮬 한 마리(power_by_tier × rank_mul ÷ 최대 마릿수×0.8) × exp(k·[1, 1−트리, max(0,훈장−12)/4]) × 쥐 수(a0 + a1·n) ② **층 시간 = exp(c·[1, log 경로 벽 합(wall_path), log(적정/찍찍), 보스, 층/100])** (벽 체력 ÷ 피해 꼴, 실패 층 = 제한 × 2 로 같이 맞춤). 측정 130층 중 118층 성공/실패 맞춤. 쓰는 법: `python Tools/calib_from_probe.py round10_hourly.txt round11_calib.csv round12_early.csv round13_verify.csv --write`.
  - `balance_sim.py`: sim_calib.json 이 있으면 위 식 (판 안 쥐 수 · 층 시간), 없거나 `--nocal` 이면 예전 식. 검증: 예전 time_mul 식은 1층 0.62배 10초를 164초로 봤음.
  - **`gen_stage_table.py` 곡선 = 실측 보정 곡선 `_curve_calib`** (sim_calib.json 이 있으면): 층마다 '그 층에 처음 닿는 플레이어'(그 층 훈장 · 훈장 안 트리 진행률 0 → 허들 1 · 판에서 지난 층 n = 4 / 71층~ 6, 넘은 허들 뒤로는 줄어듦)가 **설계 초**에 깨는 적정을 거꾸로 계산. 설계 초 `DESIGN_T` 35 → 60.7초 (지수 0.706) · 보스 ×`BOSS_T` 1.55 · 훈장 허들 = 트리 전부·제한시간의 `HURDLE_T_FRAC` 0.464. 예전 `_curve`(power_by_tier 기준)는 실측 찍찍!!의 3~10배 적정이라 보정 시뮬에선 4~10층에서 영영 막혔음. 1층 적정 ≤ 2층 ÷ 1.3.
  - 튜닝 (보정 시뮬): `RANK_K_T` 2~5 3.456 · 6~10 1.62 · 11~13 3.456 · 14~16 5.4 / `TREE_K_T` 2~5 0.938 · 6~10 1.124 · 11~13 0.75 · 14~15 0.6 / `T16_TREE_K` 0.274. 적정 1층 96 · 30층 1.95만 · 50층 35.9만 · 70층 900만 · 90층 12.9억 · 100층 48억.
  - **결과 (보정 시뮬)**: 클릭 **7.36h** `[23, 19, 18, 14, 8, 7, 7, 4]` (목표 `[21, 18, 15, 13, 11, 9, 7, 6]`) · 한 벽 최대 3판 · 미클릭 **9.0h** `[23, 19, 18, 13, 5, 5, 5, 5, 7]` (82·87·92층 4~5판, 마지막 판 91→100 질주). 승급 클릭 0.47 · 0.76 · 1.04 · 1.59 · 1.82 · 2.35 · 2.67 · 2.86 · 3.44 · 4.25 · 4.52 · 5.51 · 6.38 · 7.17h.
  - **round13 확인 (새 곡선 Play)**: 6·12·15훈장 허들 · 13훈장 트리 0 막힘 = 예상대로. **2훈장 10층 보스 · 9훈장 56층 허들은 실제가 더 어려움** (예상 통과 → 실패), 7훈장 트리 0 은 예상보다 3층 더 감. 층 시간 오차 ×1.5 라 허들 근처는 판마다 뒤집힐 수 있음 → 사람 플레이 확인 필요. round13 은 보정에 넣었고 그 뒤 재튜닝한 곡선은 Play 로 다시 안 잼.
  - 튜닝 손잡이에서 뺀 것: `NEED_TILT`(만들었다 지움) · `TIME_RAMP`(벽 두께, 0.8 → 1.534 그대로).
- 백업 `Data_Table/_backup_20261026` (xlsx · json · economy_params).

## 5-32. 새 쥐 콩쥐팥쥐 · 나비쥐 (2026-10-29, 게임 적용 · Play 확인 · 커밋 안 함) — 그림 `UnityResources/Rats/NewRats5/README.md`

- **사용자 지시**: 둘 다 **필살기·초필살기 없음** · 메인 게임(main-game)에만 · 그림 Codex · 테이블 연동 · 콩쥐팥쥐 외국어는 쌍둥이 느낌(Hanbok Twins) · Rare · 훈장 8 · 효과는 연출 + 착지 충격 / 나비쥐 비행은 연출만.
- **콩쥐팥쥐 `kongpat`(10088)**: 한복 두 벌(콩쥐 노랑·산호 · 팥쥐 민트·남색) 쥐 5조각 두 벌 → **한 마리를 180° 돌려 위아래로 맞붙임** (사용자: "머리가 겹치지 않게 따로 · 리그가 걍 180도로 딱 붙어있는 느낌 · 위아래로 한 장씩"; 처음 시도 = 위 쥐를 세로로 뒤집기 → 머리가 겹쳐서 폐기). `Rat.Pair.cs TwinSplit`: 위 쥐 = 같은 값에 rot+π (쌍의 가운데 높이 `twinGap` 축). 서 있는 쪽이 pose, 거꾸로 선 쪽은 바둥(`TwinFlail`). 특수 액션 `Jump_Slam` 3번 = 점프마다 쌍이 가운데 축으로 반 바퀴 (`twinP`, 끝나면 `twinBase` 반올림). 반 바퀴 자세 Play 확인.
- **나비쥐 `butterfly`(10089)**: 사용자 "나비 몸통 부분이 쥐 머리로 바뀐 느낌 · 몸통 없이 · 귀가 날개처럼 변해 머리가 날아다님 · 그림 아예 다시(참고 스케치 그대로)" → `RatArtLibrary.Entry.headOnly` (머리 + 꼬리 + 큰 귀 날개 둘 + 작은 아래 날개 둘 `wing2`, 몸통·다리 안 그림, 시트 `butterfly_parts.png` 4열×2줄 → `Tools/slice_butterfly.py`). RatPortrait 도 headOnly 처리. `RatRig.BuildHeadOnly/ApplyHeadOnly/ApplyWings` (날개는 머리 자식, `wingSpread` 로 좌우 벌어짐, 펄럭 = Pose.wing), `Rat.Pair.cs ButterflyPose/ButterflyTransform` (flyHeight 둥실 · flap*). 첫 시안(일반 쥐 몸통에 날개)은 `NewRats5/Sheets/_v1_torso_wings`.
- **스킬 (사용자 확인 필요)**: 20088 둘이서 두 번 = Double_Bite 30% / 21088 공중제비 쌍둥이 = Hit_Item 4% → Jump_Slam 3번 · 2.6 · 반경 99.5 / 20089 나비 효과 = Crit_Boost +10% ×3.7 / 21089 비늘가루 소나기 = 10초마다 Savory_Smell(반경 300 · 5초 · +30% 피해). 공격력 콩쥐팥쥐 88 (Epic, 같은 훈장 에픽 최고치 광부 저빌 88 에 맞춤) · 나비쥐 46 (Rare).
- **웹 미리보기** `UnityResources/Rats/NewRats5/rig_web/pair.html` (launch.json `rat-rig-web` → `http://localhost:8773/NewRats5/rig_web/pair.html`): 두 마리 한 화면, 슬라이더 + JSON (이름 = RatManager twin*·fly*·flap* · RatRig wingSpread·smallDown · 리그 메타 wing_*·wing2_*). 웹에서 고친 JSON 을 주면 유니티 필드·RatRigMeta 에 넣음 (아직 자동 붙여넣기 코드는 없음).
- 도구: `Tools/add_twin_butterfly_rats.py`(테이블) · `Tools/add_rats_to_unity.py`(파츠 → Assets · 리그 메타). 백업 `Data_Table/_backup_20261029`. 스트링 + 영어 채움 · xlsx2json 끝.
- **Codex 가 이 PC 에서 안 돌 때**: `~/.codex/config.toml` 에 UnityMCP·unityMCP 두 항목이 겹쳐 "url is not supported for stdio" 로 바로 종료 → 설정은 안 건드리고 `CODEX_HOME=<임시폴더>`(auth.json 만 복사)로 실행, 저장이 막히면 `<임시폴더>/generated_images/<세션>/` 에서 복사. 끝나면 임시 폴더 삭제.
- **안 한 것 / 남음**: ① 웹 미리보기(슬라이더)는 안 만들고 바로 유니티에 넣음 — 값은 RatManager 인스펙터 · RatRig `wingSpread` ② 쥐 도감·뽑기 풀·로비 초상화 확인 안 함 (RatPortrait 가 head 만 쓰는 쥐에서 어떻게 보이는지) ③ 이번에 table xlsx 재저장으로 다른 테이블 xlsx·json 줄바꿈만 바뀐 것 있음 (되돌리지 않음) ④ Play 에서 특수 액션 전체 흐름·착지 충격 숫자는 안 봄.

## 5-33. Windows 빌드 화면 비율 16:9 고정 (2026-10-29, 커밋 안 함)

- **점검 (Play 중 로비, Game 창 해상도만 바꿔 캡처)**: 16:9 정상 · 16:10 정상 · 4:3 좌우 잘림(낮잠 침대 간판 반) · **21:9(2560×1080) 문제** = 위쪽 훈장 줄 잘림 · 치즈 창고 간판이 재화 표시에 가림 · 하수구 뒷골목 설명이 아래 안내 띠와 겹침. 원인 = 로비 카메라가 배경으로 화면을 꽉 채우며(cover) 위아래를 자름 + CanvasScaler(1920×1080, 가로세로 반반 0.5)라 넓은 화면에서 UI가 커짐.
- **사용자 결정**: 모든 비율 16:9 고정 + 남는 곳 검은 띠 (넓은 화면 = 좌우, 16:10·4:3 = 위아래). 21:9 꽉 채우기 지원은 안 함.
- **코드 `Core/ScreenAspectLock.cs`** (씬 오브젝트 없음, CursorSkin 처럼 시작 때 숨은 오브젝트): Windows 빌드에서만(`UNITY_STANDALONE && !UNITY_EDITOR`) 동작. 전체 화면 = 모니터 해상도 안의 가장 큰 16:9로 `Screen.SetResolution` (2560×1080 → 1920×1080 · 3440×1440 → 2560×1440 · 1920×1200 → 1920×1080 · 1440×1080 → 1440×810), 유니티가 늘이지 않고 검은 띠. 창 모드 = 창 크기를 바꾸면 0.3초 멈춘 뒤 16:9로 다시 맞춤 · Alt+Enter 전환도 다시 맞춤. **에디터 · 웹(mvp) 빌드는 그대로.**
- 확인: dotnet 컴파일(에디터 정의 · Windows 빌드 정의 둘 다) 이 파일 오류 0 · 유니티 컴파일 오류 0.
- **테스트 빌드 (같은 날)**: 활성 플랫폼 WebGL → **Windows 빌드 프로필로 전환함** (Mono) → `Unity_Making/NKK_Project/Builds/Windows_ResTest_20261029/NKK.exe` 성공 (Builds 는 git 제외). 실행해 창 안쪽 크기 잼: 창 1600×1000 → **1600×900** · 창 1600×686 → **1220×686** · 전체 화면(16:9 모니터) → 1920×1080 그대로. 타이틀 화면 정상.
- 남음: ① 이 PC 모니터가 16:9 라 **전체 화면 검은 띠 · 띠 있을 때 클릭 위치는 직접 못 봄** (21:9·16:10 모니터에서 한 번 확인 필요) ② 빌드 폴더의 `찍!찍!!찍!!!_BurstDebugInformation_DoNotShip` 은 배포 때 빼기 ③ 테스트 실행으로 이 PC 의 빌드용 저장(레지스트리 HKCU\Software\<회사>\<제품>)이 새로 생김 — 에디터 저장과는 따로.

## 5-34. 훈장별 아지트 배경: 4훈장 고양이 연구소 · 6훈장 지구과학 연구소 (2026-10-29, 커밋함 21bb322a) · 9훈장 공사 현장 (커밋함) · 11훈장 수중 연구소 (커밋함) · 13훈장 2차대전 박물관 (커밋함) · 16훈장 우주 연구소 (커밋함) — 그림 `UnityResources/Rats/Lobby_CatLab/README.md`

- **사용자**: 16~30층이 고양이 연구소라 4티어(탈출 설계자, 최고 18층) 해금 시 로비 배경도 연구소 컨셉으로. **새 그림 화풍은 인게임과 맞출 것** (로비 그림체 X → 플랫).
- 그림: Codex 1장 `lobby_catlab_bg.png` (1536×1024, 원래 `lobby_bg` 배치 그대로 — 쥐구멍·화이트보드·통조림 탁자·책·쳇바퀴 빈자리·치즈 선반·침대·바닥 높이, 화풍 = 인게임 고양이 연구소 소품·벽 참고). → `Assets/Art/Rats/UI/lobby_catlab_bg.png` (임포트 설정 lobby_bg 복사, 크기 15.36×10.24 같음).
- 코드: `LobbyHome.tierLooks` (훈장 fromTier → 배경 그림, 높은 것 우선, 없으면 처음 배경) · `Refresh` 에서 교체 (로비 들어올 때 · 페이지에서 돌아올 때 · 훈장 승급 뒤 RankPage). 씬 Lobby `Home` 에 fromTier 4 = lobby_catlab_bg 연결. 하수구 AlleyRats 는 목록 비어 있어 그대로. 타이틀 씬 lobby_bg(UI)는 그대로.
- 확인: Play (슬롯 16훈장) 캡처 = 간판·쥐 바닥·쳇바퀴 위치 맞음 · 메모리에서 훈장 1·3 = 원래 배경 / 4·5 = 연구소 (저장 안 함) · 콘솔 에러 0.
- 남음: 위에 얹는 로비 소품(쳇바퀴·탁자 위 치즈·찻잔·맨홀·훈장)은 원래 로비 그림체 그대로 → 연구소 배경에서 플랫으로 바꿀지 사용자 결정.
- **지구과학 연구소 배경 (같은 날, 사용자)**: 31~45층(구간 70006 지구과학 연구소, Dino 테마)에 맞춰 **6훈장(반란 지휘관, 최고 31층)부터** 지구과학 연구소 배경 (사용자 처음 요청은 "4티어"였으나 4훈장은 고양이 연구소라 확인 → 6훈장 선택). 그림 `UnityResources/Rats/Lobby_EarthLab/` (Codex 1장, 배치 = lobby_bg · 화풍 = 고양이 연구소 로비판 + 인게임 화석·사암벽) → `Assets/Art/Rats/UI/lobby_earthlab_bg.png`. Lobby `Home.tierLooks` = 4 → catlab · 6 → earthlab. Play 확인: 메모리에서 훈장 1·3 원래 / 4·5 고양이 / 6·7 지구과학 (저장 안 함) · 콘솔 에러 0.
- **공사 현장 배경 (같은 날, 사용자)**: 46~60층(구간 70007 연구소 확장 공사 현장, Build 테마)에 맞춰 **9훈장(지하 제국 총독, 최고 50층)부터**. 그림 `UnityResources/Rats/Lobby_BuildSite/` (Codex 1장, 배치 = lobby_bg · 화풍 = 고양이 연구소 로비판 + 인게임 합판 벽·경고 줄무늬·비계·안전모) → `Assets/Art/Rats/UI/lobby_buildsite_bg.png`. Lobby `Home.tierLooks` = 4 catlab · 6 earthlab · 9 buildsite. Play 확인: 메모리에서 3 원래 / 4 고양이 / 6·8 지구과학 / 9·12 공사 현장 (저장 안 함) · 캡처 = 간판·바닥·쳇바퀴 맞음, 훈장 줄이 철골 들보 위 · 콘솔 에러 0.
- **수중 연구소 배경 (같은 날, 사용자)**: 61~75층(구간 70008 수중 연구소, Sea 테마)에 맞춰 **11훈장(연구소 점령자, 최고 64층)부터**. 그림 `UnityResources/Rats/Lobby_SeaLab/` → `Assets/Art/Rats/UI/lobby_sealab_bg.png`. 첫 판은 갑판·둥근 창이 위로 어긋나(쥐가 갑판 앞면을 걷게 됨) 버리고(`_v1`), **고양이 연구소판을 그대로 두고 다시 칠하기(reskin)** 프롬프트로 다시 뽑아 배치 맞춤. Lobby `Home.tierLooks` = 4 · 6 · 9 · 11. Play 확인: 메모리에서 3 원래 / 4 고양이 / 6 지구과학 / 9·10 공사 / 11·16 수중 (저장 안 함) · 캡처 = 간판·바닥·쳇바퀴·둥근 창 맞음 · 콘솔 에러 0.
- **2차대전 박물관 배경 (같은 날, 사용자)**: 76~90층(구간 70009 2차대전 박물관, War 테마)에 맞춰 **13훈장(쥐 공화국 의장, 최고 76층)부터**. 그림 `UnityResources/Rats/Lobby_WarMuseum/` (고양이 연구소판 reskin 프롬프트로 한 번에 배치 맞음 · 국기·마크·무기·사람 없음) → `Assets/Art/Rats/UI/lobby_warmuseum_bg.png`. Lobby `Home.tierLooks` = 4 · 6 · 9 · 11 · 13. Play 확인: 메모리에서 3 원래 / 4 고양이 / 6 지구과학 / 9 공사 / 11·12 수중 / 13·16 박물관 (저장 안 함) · 캡처 = 간판·바닥·쳇바퀴·쥐구멍 맞음 · 콘솔 에러 0.
- **우주 연구소 배경 (같은 날, 사용자)**: 91~100층(구간 70010 우주 연구소, Space 테마)에 맞춰 **16훈장(찍찍!! 신화, 최고 91층)부터**. 그림 `UnityResources/Rats/Lobby_SpaceLab/` (고양이 연구소판 reskin, 한 번에 배치 맞음) → `Assets/Art/Rats/UI/lobby_spacelab_bg.png`. Lobby `Home.tierLooks` = 4 · 6 · 9 · 11 · 13 · 16 (구간 6개 전부). Play 확인: 메모리에서 3 원래 / 4 고양이 / 6 지구과학 / 9 공사 / 11 수중 / 13·15 박물관 / 16 우주 (저장 안 함) · 캡처 = 간판·바닥·쳇바퀴·쥐구멍 맞음 · 콘솔 에러 0.

## 5-35. 뒷골목 미니 게임기 = 10훈장 해금 (2026-10-30, Play 확인 · 커밋 안 함) — 그림 `UnityResources/Rats/Lobby_Market/Sheets/arcade/`

- **사용자**: 미니게임은 처음부터가 아니라 2~3시간 진행 뒤 → **10훈장 해금 시 하수구 뒷골목에 미니게임을 할 게임기** · 위치 = **암시장 왼쪽** · 그림 = **인게임 화풍**(플랫) · 해금되면 안내 튜토리얼. (시뮬 기준 10훈장 = 클릭 2.86h · 미클릭 2.94h. main-game 전용 — 50f 는 40층 공개라 10훈장에 못 감)
- **그림**: Codex 1장 `Sheets/arcade/arcade.png` (참고 = 인게임 소품 `ref_style_props/items` · 담배 상자 몸통 청록 오락기 + 압정 조이스틱 · 병뚜껑 버튼 · 치즈 동전 구멍 · 실패 의자) → 키잉 `Lobby_Market/lm_arcade.png` → `Assets/Art/Rats/Lobby_Market/lm_arcade.png`. 첫 시도(로비 그림체 프롬프트)는 `Sheets/arcade/_v1_lobby_style` (저장 실패, 그림 없음).
- **씬 Lobby**: `Alley/Arcade` (높이 1.75 · 바닥 −1.725 · x −5.11 = 노점 바로 왼쪽, `ArcadeMachine`) · `Alley/ArcadeBubble`(브로커 말풍선 복제) · `Home/Hots/Alley/Hot_arcade` (Hot_aug 복제: LobbyHot id `arcade` · FeatureGate `arcade` — 잠김 = 게임기·팻말·강조 숨김 + **깡통 가게 `Decor/HouseCan` 보임**, 열림 = 깡통 가게 자리에 게임기 · TutorialTarget `arcade`). 팻말 '미니 게임기 / 쉬어 가는 한 판' (8000553 · 8000552), 누르면 대사 2개 (8000550~551, LocScene 연결), 영어 채움.
- **코드**: `Lobby/ArcadeMachine.cs` (누르면 통 튀기 + 말풍선, `bubbleLift` 로 위 팻말 피함) · `LobbyManager.OnHot("arcade")` (잠김 = 자물쇠 반응, 열림 = `ArcadeMachine.Poke`). **미니게임 화면은 아직 없음** → 생기면 `Poke` 에서 열면 됨.
- **튜토리얼** (튜토리얼 테이블, 백업 `Data_Table/_backup_20261030`): `l_arcade` (발동 TierAtLeast 10 · 조건 Unlocked alley · 해금 `arcade` · sort 330, 아지트에서 시궁쥐 대사 113~114 → 맨홀 강조) → `l_arcade_hot` (뒷골목 도착 · Done l_arcade · sort 331, 안경 쥐 115~116 · 브로커 117, 게임기 강조). Unlock_Id 시트에 `arcade`. 스트링 3900113~117 + 영어. 튜토리얼 안 함(quiet) 저장도 해금은 됨.
- **Play 확인 (슬롯 3 = 9훈장을 메모리에서 10훈장으로, 끝나고 저장값 되돌림)**: 아지트 대사 2줄 · 맨홀 강조 → 뒷골목 도착 대사 3줄 · 게임기 노란 강조 · 깡통 가게 사라지고 게임기 · 누르면 말풍선. 콘솔 에러 0. 캡처 `Unity_Making/NKK_Project/Captures/arcade_*.png`.
- 참고: `gen_string_table.py` 가 다른 테이블 xlsx 도 다시 저장해서 git 에 여러 xlsx 가 바뀐 것으로 뜸 (JSON 은 스트링·튜토리얼만 내용 변경).
- **남음**: ① 미니게임 내용·화면 (지금은 '준비 중' 말풍선) ② 영어 화면 확인 ③ 커밋.
- **(2026-10-30 밤, 사용자) 그림체 · 위치 바꿈**: 암시장 · 비밀 실험실 · 증강 연구소와 같은 칠한 그림체로 다시 그림 (Codex, 참고 = 실험실 · 지하 도시 소품 · 증강 연구소, 프롬프트 = `Sheets/arcade/v2_city/`, 처음 플랫 그림은 `Sheets/arcade/lm_arcade_flat_old.png`). 위치 x −5.11 → **−5.48** (분식 −7.0 과 노점 −3.4 사이 가운데, 바닥 −1.73 · 높이 1.8) · 말풍선 · `Hot_arcade`(누르는 칸 0.95×1.8 · 강조 · 팻말 높이) 같이 옮김. Play 확인: 16훈장 슬롯에서 게임기 보임 · 깡통 가게 숨김 · 누르는 칸이 게임기에만 닿음, 에러 0.
  - Codex 저장이 '읽기 전용'으로 막힘 → 임시 CODEX_HOME 의 `generated_images` 에서 가져옴 (§ Codex 메모와 같음).

## 5-36. 튜토리얼 대사 5초 자동 넘김 (2026-10-30, Play 확인 · 커밋 안 함)

- **사용자**: 튜토리얼 대사가 뜨면 넘길 때까지 게임이 멈춰서 방치 중엔 진행이 안 됨 → 오프닝 컷씬처럼 **5초 뒤 저절로 다음 대사**.
- `TutorialBox.autoNext` 5 (인스펙터, 0 = 끔): 한 줄이 뜨고 5초가 지나면 클릭 없이 다음 줄 · 마지막 줄이면 대사창이 닫히고 다음 단계로. 글이 다 나오고 기존 넘김 조건(nextDelay·minLineTime)을 채운 뒤에만 넘어감. 클릭으로 넘기는 건 그대로. Game · Lobby 씬 둘 다 같은 컴포넌트라 같이 적용 (오프닝 컷씬 `StoryPlayer.autoNext` 10초는 그대로).
- **Play 확인 (로비, 대사 3줄 l_arcade_hot 을 클릭 없이)**: 줄 바뀜 0 → 4.9 → 9.9초 · 14.9초에 창 닫힘 · timeScale 1 · 콘솔 에러 0 (저장값 손대지 않음, 테스트 뒤 되돌림). 게임 씬은 같은 컴포넌트·같은 닫힘 경로(TutorialManager.Finish → Pausing 끔)라 따로 안 봄.
- 참고: 방치 중 멈추는 다른 창 = 증강 고르기 창(31층~, 게임 멈춤 · 고를 때까지 대기)은 이번에 손대지 않음.

## 5-37. 미니 게임기 = 쥐돌깨기 창 (2026-10-30, Play 확인 · 커밋 안 함)

- **사용자**: 게임기(§5-35)를 누르면 `쥐돌깨기.html`(웹 프로토타입, Shooting Ballz 식 벽돌깨기)을 인게임 창으로. **밸런스·조작은 나중에 사용자가 안내** → 지금 값 = 웹 기본값 그대로.
- 유니티엔 Windows 용 웹뷰가 없어서 **C# 으로 옮김** (규칙 · 화면 배치 · 키 그대로, 판 좌표 = 웹 1280×720 픽셀). 코드 `Scripts/Lobby/JudolGame.cs` (로직 + 매 프레임 풀에서 다시 놓는 그리기) · `JudolRatArt.cs` (원작 파츠를 웹 drawQuad 규칙으로 조립해 2048 텍스처에 칸마다 미리 그림, 등급 오라 포함, 셰이더 `Art/Minigame/JudolBake.shader`·`.mat` 패스 0 덮어쓰기/1 겹치기) · `JudolPress.cs` (판 끌기 · 뽑기 버튼 꾹/Shift).
- **수치** = 새 엑셀 `Data_Table/미니게임 테이블.xlsx` → `MinigameTable.json` (`xlsx2json` NAMES 추가): Judol_Config(경기장·속도·뽑기 가격·천장·증강 비용·벽돌 체력 곡선 등 39개) · Judol_Grade(피해·스킬 확률·오라·색) · Judol_Aug(16종, 이름·아이콘 = 증강 테이블 augment_id, 값 = min(cap, v1×단계)) · Judol_Tier(3장 등급 가중치) · Judol_Band(벽돌 색) · Judol_Shape(고유 스킬 범위 = 원작 액션 스킬 effect_type). 쥐 뽑기 등급 확률 = 뽑기 테이블 Gacha_Grade, 등급별 쥐 = 파츠 그림이 있는 쥐 전부.
- **씬 Lobby** `UI/ArcadeWindow` (튜토리얼 바로 아래, 꺼 둠, `JudolGame`): Board(Field 마스크 · 템플릿 · Left/Right 패널 · ScreenParts · Floats · Close) · OvTitle/OvAug/OvPause/OvOver. `Alley/Arcade` ArcadeMachine.game 연결 → 누르면 창 열림 (말풍선 대신). 열려 있는 동안 아지트 입력 막음 (`LobbyManager` inputBlocked), 배경음악 = HTML 안 음악 `Audio/BGM/BGM_Arcade_Judol.mp3` (`SceneMusic.arcadeClip`, M = 끄기). 닫기 버튼 · Esc(시작/끝 화면) = 닫기, 게임 중 닫았다 열면 일시정지로 이어 함. 최고 턴 = 저장 슬롯 `judolBest` (Progress). 시간 = unscaled (로비 슬로 모션 영향 없음).
- **글**: 씬 TMP + 인스펙터(증강 설명 augTexts · 치명!/잭팟! 등) → 스트링 8000554~8000603 (+ 기존 닫기·처음부터) · LocText 41 · LocScene fields 22 · 영어 채움 (제목 Rat Breaker). 폰트에 ↑ 가 없어 '유니크 이상'.
- **Play 확인**: 창 열기 · 시작 · 뽑기 40/250번 · 발사(283마리 동시, 약 200fps) · 회수 · 턴 넘김 · 증강 3장 고르기 · 끝 창 · 닫기 · 영어 화면, 콘솔 에러 0. 마우스 끌기 발사는 직접 해 보지 않음 (코드 경로만). 테스트 뒤 저장값·언어 되돌림.
- **남음**: 밸런스·조작 안내 받으면 엑셀/코드 조정 · 보상(본게임 치즈 등) 없음 · 50f 브랜치엔 없음(게임기 자체가 main-game 전용).
- **(2026-10-30 저녁, 사용자) 뽑기 키**: **왼쪽 Shift = 쥐 1마리 뽑기 (꾹 = 연속, 뽑기 버튼 꾹과 같은 hold_delay · hold_repeat)** · **왼쪽 Ctrl = 일괄 뽑기** (치즈가 되는 만큼, 한 번에 최대 `Judol_Config.bulk_max` 100 — 예전 Shift = 10마리 `ten_pull` 을 바꿈) · 뽑기 버튼은 누름 = 1마리 · 꾹 = 연속 · Ctrl + 누름 = 일괄 · Q = 1마리 그대로. 안내 글 8000577 '{price} 치즈 · Shift 뽑기 · Ctrl 일괄 · 천장 {pity}' (넘치면 자동 크기) · 8000580 시작 화면 조작 줄 (영어 채움). Play 로 확인 (Shift 톡 +1 · 꾹 연속 · Ctrl 치즈 455 → 45마리, 저장값 되돌림).
- **(같은 날, 사용자) 빠르게 버튼**: 버튼 글이 '다음에 누르면 될 배속'(1배속인데 ×2)을 보여 줘서 실제 배속과 어긋남 → **지금 배속 X1 · X2 · X4** 만 (글 '빠르게' 뺌, 씬 Label = `X{n}` · 28 · LocText 없음, 스트링 8000570 '안 쓰임', 아래 단축키 안내 'F' 는 그대로). Play 로 X2 → X4 → X1 확인.
- **(같은 날, 사용자) 조준 = 날리고 싶은 방향으로 끎** (예전 = 새총처럼 뒤로 끎): `JudolGame.OnPress` aim = 지금 - 누른 자리. 안내 글 3줄 (8000555 시작 화면 설명 · 8000560 경기장 안내 · 8000580 조작 줄, 한글 · 영어) 고침. Play 로 오른쪽 위 · 왼쪽 위 끌기 → 같은 방향 조준선 · 발사 확인.
  - 주의: 편집 모드에서 `GameDatabase.Load()` 를 부르면 상점·뽑기 테이블 값이 씬에 직렬화되어 저장됨 → import_scene_strings 가 상점 상품 이름을 씬 글로 잡음. 이번에 로비 씬 값을 비우고 잘못 생긴 스트링 6줄(8000607~612) 지움.
- **(같은 날, 사용자) 밸런스 1차 = 판 길이 그대로, 전투 증강 2배**: 실제 `JudolGame` 로직을 리플렉션으로 돌리는 자동 플레이(프로젝트 밖 임시 스크립트, 판마다 치즈 전부 뽑기 · 쥐가 증강 비용 2배 넘으면 증강 · 조준 = 한 마리 경로로 가장 많이 맞는 각도)로 비교.
  - 바꾼 값: `Judol_Config.hp_grow` 1.075 → **1.0775** · `Judol_Aug` **전투 12종 v1 ×2 · 확률 상한(cap) ×2 (최대 100)** = dmg 2 · crit 20/100 · multi 30/100 · zap 20/100 · domino 2 · storm 6 · giant 4 · train 6 · clone 2 · meteor 2 · blades 4 · legend 100. **경제 4종(gold · jackpot · rain · birth)은 그대로** — 2배로 하면 치즈 → 쥐 → 화력 눈덩이(무작위로 고르면 쥐 4000마리 · 한 턴 85초).
  - 결과 (중앙값, 현행 → 바꾼 뒤): 높은 등급만 고르기 63.5 → 66.5턴 · 무작위 증강 73 → 76.5턴 · 무작위 조준 52 → 59턴 · 4배속 약 5.8 → 6.3분. **증강 값어치 = 증강 안 받은 판 대비 +6.5턴 → +13턴**.
  - **(같은 날, 사용자) 9번째 증강부터 비용 증가 완화**: `JudolGame.AugCost` = aug_base × aug_mul^n, **`aug_late_from` 9 번째부터 `aug_mul_late` 1.3** (Judol_Config 두 줄 추가). 비용 1~8번째 그대로(10 … 171), 9번째부터 256 · 384 · 577 · 865 → **222 · 289 · 375 · 488** (13번째 634 · 14번째 825). 시뮬레이션: 9~12번째 받는 턴 54 · 62 · 70 · 82 → 50 · 57 · 63 · 72, 판 길이 그대로(높은 등급 66.5턴).
  - 아직: 초반 20턴 심심함(설정값으로 안 바뀜, 구조 손봐야) · 쥐 140마리 넘으면 `launch_min` 때문에 발사 시간이 쥐 수 × 0.02초로 늘어남(전체 발사 시간 고정은 코드 수정) · 높은 등급(골드·전설) 증강이 실버 경제 증강보다 약함(무작위로 고르는 쪽이 더 오래 버팀).

## 5-38. 줴리 초필살기 v3 = 무성영화 앵콜 (2026-10-30, Play 확인 · 커밋 안 함)

- **사용자**: 초필살기 '줴리 감사합니다 (앵콜)' 연출이 톰과 제리 그 자체(파란 고양이 · 집 · 문 · 쥐구멍 · 프라이팬 · 카펫) → **턱시도 고양이와 찰리 채플린 패러디 슬랩스틱**으로. 고양이는 게임 그림체(톰 연상 X) · 실제 영화 장면 X · 다른 초필살기처럼 다양한 구도와 전환. 결정: 채플린은 무성영화 장치 위주(떠돌이 차림 X) · 세피아 · 자막 카드 2~3장 · 컷신 쥐는 게임 그림체로 새로. **초필살기만** (줴리 쥐 본체 그림 · 일반 필살기 '줴리 감사합니다'는 안 건드림).
- 그림 23장 Codex (`UnityResources/Rats/SuperUlt/jwerry/Sheets/v3/`) → `slice_superult.py` (새 `SEPIA` 단계) → `Assets/Art/Rats/SuperUlt/jwerry/` (옛 톰 그림 49장 지움, 원본은 `_v2_tomjerry/` 보관). 연출 = `Tools/superult_story.py jwerry` (옛 것 `jwerry_v2`), 자세한 컷 = `SuperUlt/README.md` jwerry v3.
- 씬 Game `HUD/SuperUlt/Cut/Frame/Scenes/Jwerry` 다시 만듦 (레이어 90) · Style 컷신 입자 0. 자막 카드 글 = 스트링 8000606 '오늘의 앵콜 공연' / Tonight's Encore · 8000605 '웨이터는 단단히 화가 났다!' / The waiter is NOT amused! · 8000604 '끝' / The End (LocText, 생성기에 키 고정).
- **확인**: 편집 모드 캡처 + Play(`TestSuper("jwerry")`) 캡처 — 컷신 7.2초 → 게임 화면 크림 파이 비 · 철퍼덕, 콘솔 에러 0. 저장값 되돌림. 컷신 중 보이는 노란 점 = 뒤에서 계속 도는 게임의 치즈 획득 표시 (원래 동작, 다른 초필살기도 같음).
- 남음: 초필살기 이름·대사(줴리 감사합니다 (앵콜) · 커튼이 오른다… 앵콜! · 줴리 감사합니다!!)는 사용자가 저작권 검토 중이라 그대로 (컷신엔 안 보임). 줴리 쥐 본체 그림 · 일반 필살기는 따로.
- **(같은 날 2차, 사용자) 자막 카드 = 언어 상관없이 영어 고정** (옛날 영화 느낌): Tonight's Encore · The waiter is NOT amused! · The End, LocText 없음 (스트링 8000604~8000606 은 '안 쓰임').
- **영어 로비 팻말 · 말풍선 글자가 팻말보다 훨씬 크게 나오던 문제**: `LocText.Fit`(영어일 때 넘치면 자동 크기)이 최소 크기를 무조건 8 로 잡아서, 크기 1.5~2.4 인 월드 글자(TextMeshPro: 로비 팻말 · 브로커 · 게임기 · 쥐 수다 말풍선)가 오히려 몇 배로 커졌음 → 월드 글자는 바닥 8 없이 원래 크기 × 0.55 까지, 최소가 원래 크기를 넘지 않게. 영어 Play 로 아지트 · 뒷골목 팻말 · 말풍선 확인 (캡처 지움, 언어 · 저장값 되돌림).

## 5-39. 저작권 검토 19줄 고침 (2026-10-30, 커밋 안 함)

- **사용자**: 검토 시트(`Desktop/저작권 검토 목록_2026-10-30.xlsx`)에서 고칠 줄만 남김 (218 → 19줄) → '바꿀 문구 예시'대로.
- 고친 것 (한글·영어 둘 다 17 · 영어만 2): 찍찍 탐정 영어 Detective Squeakan → Detective Squeaks · 요리사 쥐 설명(라따뚜이 암시) · 닌자 쥐 설명(피자) · 찌릿 햄찌 설명(전기 쥐 코스프레) · 쥐랜드 관광쥐 설명(동그란 귀 머리띠 · 원조) · 폭탄마 쥐 설명(하루 한 발 · 안대) · 아이스 퀸 설명(엘사 · 렛잇고) · 스킬 폭렬 외길 → 폭탄 외길 · 그림자 분신술 → 그림자 쥐떼 (설명 포함) · 건전지 백만 볼트 → 건전지 대방전 (설명 포함) · 필살기 천본앵 난무 → 벚꽃 일섬 · 대사 '백만 볼트…!' → '충전 완료…!' · '내 이름은 찍구밍!…' → '이 한 방에 전부 건다! 폭탄 마법, 발사!' · 자막 영어 Stairway to heaven~ → Stairs to heaven~ · 귀멸 자막 뇌의 호흡 / 제1형 / 벽력일섬 → 번개의 기운 / 첫 번째 칼 / 번개 일섬.
- 방법: 원본 엑셀 17칸 → `gen_string_table` (키 그대로) → 스트링 영어 · 상태 '번역 완료' · 메모 '2026-10-30 저작권 고침 (사용자 검토)' → `xlsx2json`. 백업 `Data_Table/_backup_20261030/저작권_19줄_전/`. 유니티 GameDatabase 로드 확인 · 콘솔 에러 0.
- 참고 (목록에 없어 그대로): 폭탄마 필살기 개발 설명 1530034 에 옛 스킬 이름 '폭렬 외길' / 'Explosion or Bust' 가 남아 있음 · 코드 주석 `UltExplosion.cs` '찍구밍'.

## 5-40. 깨진 저장 슬롯 방어 (2026-10-30, Play 확인 · 커밋 안 함)

- **증상 (사용자)**: 타이틀 이어하기 · 처음부터가 안 열림. 원인 = 테스트 저장값 복구 실수로 슬롯 2~5 에 JSON 대신 Base64 글이 들어감 → `Progress.SlotSummary` 의 JsonUtility 예외 → `TitleManager.Refresh` 에서 멈춰 슬롯 창이 안 뜸. 저장값은 망가지기 전 백업으로 되돌림 (데이터만, 코드 아님).
- **방어 코드**: `Progress.Parse` (깨진 JSON = null) · `SlotSummary` 깨진 슬롯 = 빈 슬롯 (false) · `Load` 깨진 슬롯 = 처음 상태로 시작 + 원본을 `키_broken` 에 보관 + 경고 로그 · `TitleManager.Start` 이어 하기 버튼 = 읽히는 슬롯이 있을 때만.
- Play 확인 (슬롯 5 일부러 깸): 이어하기 창 = 슬롯 1~4 정상 · 5 빈 칸 못 누름 / 처음부터 창 = 5 누를 수 있음 / `UseSlot(5)` 멈춤 없음 · `_s5_broken` 보관 · 경고 1줄 · 에러 0. 뒤에 저장값 되돌리고 `_broken` 키 지움.

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
