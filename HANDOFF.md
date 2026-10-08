# 찍!찍!!찍!!! — 유니티 이식 인수인계 (요약본)

웹게임 `Proto_Game/rat-uprising.html`(약 1만 줄 JS)을 유니티로 옮기는 작업. 이 문서는 **지금 상태와 다음 할 일**만 정리한 요약본입니다.
날짜별 상세 기록(측정 결과·결정 경위·옛 수치)은 `HANDOFF_LOG.md` (§9-1 ~ 9-14).
마지막 갱신: 2026-10-08 저녁 (보스 스킬)

---

## 0. 꼭 지킬 규칙 (사용자 지시)

- **설명은 항상 한국어** (코드 주석도). 게임 이름 **"찍!찍!!찍!!!"**. PC(Windows) · 기준 1920×1080. 씬 `Lobby.unity`(빌드 0) → `Game.unity`(빌드 1), `SampleScene` 에는 아무것도 넣지 말 것.
- **결정할 게 생기면 바로 사용자에게 질문**하고 진행.
- **수치는 엑셀 테이블**(`Data_Table/`, 3장 형식). 고친 뒤 `python Tools/xlsx2json.py`.
- **모든 오브젝트는 하이라키에 직접**(MCP 로 씬에 생성, 인스펙터에서 값 조정). C# 빌더 스크립트로 씬을 만들지 말 것.
- **UI 글자는 코드에 넣지 말 것**: 씬 TMP 글 또는 인스펙터 문자열, 바뀌는 값만 `{n}` `{floor}` 같은 자리표시를 코드가 채움. 내부 id 는 화면에 안 보이게.
- **그림은 Codex CLI 로만 생성** (코드로 그리기 금지, 자르기·키잉은 됨). 한 장에 여러 개 그려 잘라 쓰기. 먼저 `UnityResources/Rats/` 에 쓸 만한 그림이 있는지 찾아 재사용. **실사 다람쥐 `rsp_*` 금지.**
  - 실행: `bash Tools/codex.sh exec --skip-git-repo-check --ephemeral -s workspace-write -C <폴더> -i <참고.png> < prompt.txt` (자홍 #FF00FF 배경 → `Tools/slice_rat_parts.py` 의 `key_magenta` 로 키잉)
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

**보스** (5층마다, 30층 뒤 반복): 계단 방에서 대기(어두운 방에 실루엣 + 층 진입 때 카메라가 잠깐 비춤) → 계단 방이 열리면 전투. **보스를 잡기 전엔 어떤 경로로도 계단 못 씀** (`Boss.FloorCleared`, 보스가 사라졌으면 다시 불러 전투). 테스트 "층 클리어" 버튼도 보스가 있으면 보스전을 시작함. 체력 70%·35% 에서 필살 패턴, 그 뒤 25% 확률로 섞어 씀. 로그 `[Boss]` (Editor.log 검색).

| 보스 | 일반 공격 (두 번째 40%) | 쿨타임 스킬 (12초, 밈) | 발악(필살) 1 · 2 (체력 70% · 35%, 밈) |
|---|---|---|---|
| 5 경비대장 | 내려찍기 · 진압봉 | 퉁퉁퉁 사후르 | 무야호 3단 점프 · 관짝소년단 |
| 10 광기의 수석 연구원 | 플라스크 · 독가스 구름 | 이븐하게 익혀드릴게요 (불판) | 약품 대방출 파티 · 오히려 좋아 (거대화) |
| 15 연구소장 | 휘두르기 + 경비원 · 서류 가방 | 중꺾마 (피해 감소) | 전액 현금 매입 · 긴급 비상 회의 |
| 20 실험체 제로(메인쿤) | 덮치기 · 헤어볼 | 해피해피해피 (점프) | 한강 고양이 질주 · 바나나 고양이 |
| 25 대마법사 마녀 고양이 | 불덩이 · 덮치기 | 무한동력 버터 고양이 | 트랄랄레로 (상어 머리) · 발레리나 카푸치나 (커피잔 머리) |
| 30 우주 고양이 사령관 | 무중력 · 덮치기 | 맥스웰 고양이 낙하 (착지 뒤 춤) | 홈랜더 눈 레이저 · STAY (블랙홀 → 컷씬 9종 랜덤) |

**고양이** (2층부터 35~60초 뒤, 그 뒤 55~85초 간격, 한 번에 한 마리, 보스전 중엔 안 나옴): **체력 0 까지 안 떠남**. 품종 스킬(하악질 등) + **무리 스킬**: 쥐가 가장 많이 모인 곳 → 바닥 빨간 경고 원 + 머리 위 느낌표 → 고양이마다 다른 범위 기절 (코숏 대폭격 · 러시안 블루 연타 · 턱시도 급소 · 페르시안 뱃살 · 먼치킨 볼링 · 벵갈 순간이동 · 스핑크스 레이저 · 메인쿤 지진 · 마녀 운석 · 우주복 블랙홀). 공통 수치 = CatManager 인스펙터 "무리 스킬 공통", `logCrowd` 로 발동 기록.

**사람** (`ItemManager`): 층 시작 1~2명 · 방이 열릴 때 1~2명 · 18~30초마다 보충, 돌아다니는 사람이 최소 수(2 + 층÷6)보다 적으면 2.5~4.5초 빠른 보충. 상한 min(12, 4 + 층×0.4).

**쥐**: 번식(확률 = 마리 수/최대 비율 곡선, 번식 때 하트) · 클릭 총공격(쿨 3초, 같은 대상 연타 감쇠) · 승급(필요 = 올림(1.5 × 1.3^k), 일괄 승급은 최대의 절반 남김) · 필살기(쥐 개체별 게이지, Lv 7 해금, 하나 끝난 뒤 쿨 25초) · 슈퍼 점프(초당 1/480 확률) · 묘기 5종(공용 스킬로 해금). 최대 마리 = 30 + 둥지 노드(최대 175).

**성장·경제**: 치즈(판에서 벌고 저장) · 연구자료 · 조각(쳇바퀴 훈련 Lv). 공용 스킬 = 훈장마다 트리 하나(치즈 + 연구자료). **훈장 승급 = 연구자료 + 최고 층 + 조각 합 + 지금 훈장까지 스킬 트리 전부**.

**HUD (Game 씬 `HUD`)**: 왼쪽 위 치즈·찍찍!!(전투력/적정)·테스트 패널 / 오른쪽 위 층·방 · 제한시간(초시계 + 나무 게이지) · 쥐 마릿수 / 가운데 위 보스 체력바(엠블럼·줄무늬·깎인 자국) / 아래 필살기 버튼 / 왼쪽 아래 승급 / **오른쪽 아래 미니맵**(열린 방·옆방·계단·보스·쥐 점·카메라 테두리).

**로비**: 아지트(쥐 생활·소품 버튼) · 작전 회의(층 길 가로 스크롤 · 구역·방 · 출동 멤버·등급 확률 · 출발) · 찍찍!! 훈장(사다리·조건·친구·업적) · 쳇바퀴 훈련 · 치즈 창고(스킬 트리) · 탭: 친구들·낮잠 침대는 **아직 없음**.

**테스트 패널** (`HUD/TestPanel`, 출시 때 끄기): 필살기 고르기·발동 · 슈퍼 점프 · 층 클리어(보스 있으면 보스전 시작) · 게임 오버 · 저장값 초기화 · **◀ 보스 ▶ + 보스 소환 · ◀ 기술 ▶ + 보스 기술 발동** (일반·두 번째·스킬·발악 아무거나 강제).

---

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
3. **로비 남은 페이지**: 친구들(도감, `RatPortrait` 재사용) · 낮잠 침대(기록·저장). 탭 아이콘·지도 등 로비 소품을 플랫으로 다시 만들지 사용자 결정.
4. 코드에 박힌 팝업 글(Rat.Action "찌릿!!", ItemManager.ZapChain, 고양이 "냥!" 등) → 테이블/인스펙터.
5. 사운드 (웹은 WebAudio 합성 → WAV 로 뽑기).
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
