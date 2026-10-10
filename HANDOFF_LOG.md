# 찍!찍!!찍!!! — 작업 기록 (2026-10-06 ~ 10-08 상세)

> 2026-10-08 에 HANDOFF.md 를 요약본으로 정리하면서, 그때까지의 상세 인수인계 문서를 그대로 옮겨 둔 것. 최신 상태는 HANDOFF.md 가 우선. 여기 적힌 수치 중 나중 날짜 항목이 앞 항목을 덮어씀.

# 찍!찍!!찍!!! — 유니티 이식 진행 상황 (인수인계)

웹게임 `Proto_Game/rat-uprising.html`(약 1만 줄 JS)을 유니티로 옮기는 작업. 이 문서만 보고 다음 작업자가 이어갈 수 있게 정리함.
마지막 갱신: 2026-10-08

---

## 0. 꼭 지킬 규칙 (사용자 지시)

- **설명은 항상 한국어.** (코드 주석도 한국어)
- **게임 이름: "찍!찍!!찍!!!"** ("쥐들의 반란" 아님)
- **빌드 타깃: PC (Windows) · 기준 해상도 1920×1080.**
- **모든 오브젝트는 하이라키에 직접 만들어서** 인스펙터에서 값을 바꿀 수 있게. (층마다 달라지는 방처럼 어쩔 수 없는 건 프리팹 + 매니저가 생성)
- **씬:** `Lobby.unity`(빌드 0) → `Game.unity`(빌드 1). `SampleScene` 에는 아무것도 넣지 말 것.
- **작업 단계마다 결정할 게 생기면 바로 사용자에게 질문**하고 진행.
- **수치 데이터는 엑셀 테이블로 관리** (`Data_Table/`, 아래 3장 형식).
- **생성 담당:** `ItemManager` = 물건·사람 생성(주기·상한) / `StageManager` = 쥐덫·고양이 등장 확률.
- **판 시작 쥐**: 티어 테이블 6·7·8·8·9·10·11·12 + 시작 쥐 노드 합 +12 (8훈장 다 찍으면 24마리, 2026-10-08 사용자 결정)
- **특수 능력(패시브)은 처음부터 켜짐**, **특수 액션 = 쳇바퀴 훈련 Lv 3**, **필살기 = 쳇바퀴 훈련 Lv 7** (조각으로 해금, 2026-10-07 사용자 결정), **공용 스킬 묘기는 공용 스킬로 해금**.
- **이미지가 필요하면 Codex CLI 로 생성**, 토큰을 아끼려고 **한 장에 여러 개를 그려서 잘라 쓸 것** (5장).
- **화풍: Untitled Goose Game 식 플랫** (외곽선·광택·그라데이션 없음, 쥐 파츠 그림과 같게). 참고 이미지 `-i UnityResources/Rats/Sheets/ballerina.png,UnityResources/Rats/ArtSheets/art_01_item_flask.png`
- **만든 그림은 유니티 적용과 별개로 `UnityResources/Rats/<분류>/` 에 PNG + `Sheets/`(원본 시트·프롬프트) + README·미리보기로 정리**
- **스킬 id 등 내부 id 는 유저 화면에 보이지 않게**
- **UI 글자는 코드에 넣지 말 것**: 버튼·팻말·라벨 글은 하이라키 TMP 텍스트에 직접 (MCP 로 씬에 생성). 바뀌는 값만 글 안의 자리표시 `{n}` `{floor}` `{tier}` `{tierName}` 를 코드가 채움. 씬 오브젝트는 C# 빌더 스크립트가 아니라 MCP 로 직접 만들 것

---

## 1. 폴더

| 경로 | 내용 |
|---|---|
| `Proto_Game/rat-uprising.html` | 원본 웹게임 (로직 참고용, 이미지는 base64 로 박혀 있음) |
| `UnityResources/Rats/` | 원본 이미지 (README.md 에 폴더 설명). 유니티로는 쓰는 것만 복사함 |
| `Data_Table/*.xlsx` | 데이터 테이블 (원본) |
| `Tools/xlsx2json.py` | 엑셀 → `Assets/Data/*.json` 변환. **엑셀 고친 뒤 꼭 실행** |
| `Tools/gen_sprite_pivots.py` | 파츠 피벗·리그 메타 생성 (`sprite_pivots.json`, `*RigMeta.json`) |
| `Tools/codex.sh` | Codex CLI 실행 래퍼 |
| `Unity_Making/NKK_Project/` | 유니티 프로젝트 (Unity 6000.3.19f1, URP) |

유니티 안:
- `Assets/Art/Rats/` 게임에서 쓰는 이미지 (임포트 규칙 `Assets/Editor/RatSpriteImporter.cs`: 전부 Sprite, PPU 100, 파츠는 관절 피벗 자동 적용)
- `Assets/Art/Generated/` 코드로 만든 임시 그림 (바닥 타일, 그림자, 고리, 별, 조각 등)
- `Assets/Data/` 테이블 JSON + 아트 라이브러리 에셋(`RatArtLibrary`, `CatArtLibrary`, `HumanArtLibrary`)
- `Assets/Fonts/` Gowun Dodum · IBM Plex Sans KR · **Jua** (OFL) + TMP SDF (한글 2,504자, `charset_ko.txt`). 게임 UI 제목·팝업·버튼 = Jua (빠진 기호는 Gowun Dodum 대체), 긴 설명 = Gowun Dodum. 머티리얼 `Jua-Regular SDF Outline/Popup/Plank`. Jua 만 다시 굽기 = 메뉴 `NKK/Bake Font (Jua)` (Bake Fonts 는 에셋을 새로 만들어 연결이 끊김)
- `Assets/Art/UI_Kit/` UI 조각 30종 (원본 `UnityResources/Rats/UI_Kit/`), 9-슬라이스 = 메뉴 `NKK/Apply UI Kit Borders`
- `Assets/Prefabs/` Rat · Item · Room · Human · Cat · Trap
- `Assets/Scripts/` (아래 4장)

에디터 메뉴 `NKK/`: Build Rat/Cat/Human Art Library, Bake Fonts, Reimport Art Sprites.

---

## 2. 유니티 MCP

- 패키지 `com.coplaydev.unity-mcp` 설치됨. 인스턴스 이름 `NKK_Project`.
- 연결이 끊기면 에디터 창을 한 번 클릭(포커스)하면 다시 붙음.
- `execute_code` 는 CodeDom(C# 6) → 로컬 함수·`out var` 쓰지 말 것 (`System.Func` 사용), `Object` 는 `UnityEngine.Object` 로.

---

## 3. 데이터 테이블 (`Data_Table/`)

형식: 1행 한글명 / 2행 키 / 3행 자료형(int·float·string·enum, `-` = 설명 칸) / 4행~ 데이터.
스킬·효과는 **로직 이름 하나(`effect_type`) + `value_01~06`**, 값 의미는 같은 파일의 타입 정의 시트. 조건은 `cond1_type` + `cond1_value_01/02`.
공통 로직(필살기 발동 확률, 고양이 덮치기 등)은 캐릭터 칼럼에 넣지 않음.

| 파일 | JSON | 시트 |
|---|---|---|
| 쥐 캐릭터 테이블 | RatTable | Character(77) · Skill(154: 패시브+액션) · Ultimate(31) · Cond_Type · Effect_Type |
| 쥐 등급 테이블 | RatGradeTable | Grade(크기·속도·탄생 가중치·스킬 위력·조각 계수) |
| 쥐 성장 테이블 | RatGrowthTable | Growth_Node(9) · Growth_Order(29) · Action_Awaken · Growth_Effect_Type |
| 고양이 캐릭터 테이블 | CatTable | Character(10) · Skill(10) |
| 물건 테이블 | ItemTable | Item(물건 24 + 가구 12) · Zone(층 구간 5) · Furniture_Layout(32) |
| 티어 테이블 | TierTable | Tier(8: 비용·승급 조건·탄생 등급 배율·시작 마릿수) |
| 사람 테이블 | HumanTable | Human(4) · Human_Line(대사 34) |
| 공용 스킬 테이블 | CommonSkillTable | Common_Skill(35, id 92001~) · Branch(가지 7) · Common_Effect_Type · Column_Desc |
| 업적 테이블 | AchievementTable | Achievement(33, id 40001~ · 조건 cond_type+target_id+need · 아이콘) · Achv_Cond_Type |

새 테이블을 만들면 `Tools/xlsx2json.py` 의 `NAMES` 에 이름 추가 → `TableRows.cs` 에 행 클래스 → `GameDatabase.cs` 에서 로드.

---

## 4. 코드 구조 (`Assets/Scripts/`)

좌표: 로직은 웹과 같은 **게임 단위**(x 오른쪽, y 아래, z 높이), 화면에 놓을 때만 `World.ToUnity` (1유닛 = 100 게임 단위, 3/4 탑다운 TILT 0.85).

| 파일 | 역할 |
|---|---|
| `Core/World.cs` | 좌표 변환·상수 |
| `Core/GameManager.cs` | 층·티어·치즈·콤보·HUD·배너·페이드 |
| `Core/CameraController.cs` | 휠 줌, 끌어서 이동, 화면 흔들림 |
| `Core/FxManager.cs` | 이펙트 풀 (파티클·팝업 글자·고리·얼룩·치즈 코인·체력바·레이저·번개·분사·역경직) |
| `Core/ShearShadow.cs` | 물체 모양대로 비스듬히 드리우는 그림자 |
| `Core/Progress.cs` | 종별 조각·성장 레벨 저장(PlayerPrefs), 성장 노드 자동 찍기 |
| `Data/*` | enum · 테이블 행 · `GameDatabase` |
| `Stage/StageManager.cs` | 층 생성(시드)·방 열기·벽 체력·계단·적정 전투력·쥐덫·고양이 등장 |
| `Stage/Room.cs` | 방 그림 (바닥·벽) |
| `Items/Item.cs`, `ItemManager.cs` | 물건 물리(밀림·날아감·저글링·충돌·박살)·생성·택배 웨이브·폭탄·사람 생성 |
| `Rats/Rat.cs` (+ `.Passive` `.Trick` `.Action`) | 쥐 이동·갉기·번식 연동·기절·도망 / 특수 능력 / 묘기·잠 / 성장·특수 액션 |
| `Rats/RatManager.cs` | 무리·번식·클릭 총공격·오라·총알·소환·드랍 소품 |
| `Rats/RatRig.cs`, `RatArtLibrary.cs` | 5파츠 컷아웃 리그 (고양이도 재사용) |
| `Humans/*` | 사람 리그·행동 |
| `Hazards/*` | 고양이·고양이 매니저·쥐덫 |

---

## 5. 이펙트 이미지 정리 (진행 중)

**사용자 판단: 웹게임에서 코드로 그리던 이펙트가 너무 별로 → Codex 로 이미지를 새로 만들어 정리하고, 이미지를 다듬는 작업을 우선 진행할 것.**

### 끝난 것 (2026-10-06)
- Codex 시트 3장 → `UnityResources/Rats/FX_New/Sheets/` (프롬프트 `prompt_*.txt` 같이 보관, 참고 이미지 `-i ArtSheets/art_01_cat.png`)
  - `fx_tint.png` (4×4, **흰색** → 코드에서 색 입힘): 파편 3·종이·별·반짝이·먼지·동그라미·고리·얼룩 2·하트·소용돌이·부스러기·베기·치즈
  - `fx_anim.png` (6프레임×4줄): explosion · poof · hit · zap
  - `fx_color.png` (4×4, 색 있음): 코인 2·총알·불덩이·레이저 2·번개 2·회오리·연기·불꽃·독가스·땀·Zzz·하트·흙더미 (+ `beam_white` = 레이저 흰색판)
- 자르기: `python Tools/slice_sheet.py <시트> <열> <행> <출력폴더> <이름,...> [--keep-cell] [--pad N]`
- 유니티 `Assets/Art/FX/Tint|Anim|Color/` (자동 Sprite 임포트). `FxManager` 변경:
  - 파티클 3종 + 새 `FX_Spray`(분사) = Texture Sheet Animation(Sprites 모드, 무작위 한 장). 크기 배율 `shardSizeK` 등 (웹은 반지름 기준이라 ×2.4~2.8)
  - `Anim(name, x, y, z, scale)` 프레임 애니메이션 (`anims` 목록, `AnimTemplate`) → 물건 박살·폭탄·불덩이·사람 펑·고양이 덮치기·번개·소용돌이 폭발에 연결
  - `AddSticker(name, …)` 일정 시간 붙는 그림 (`StickerTemplate` = 부모(바닥 눌림)+자식(회전), `stickerSprites` 목록) → 소용돌이(바닥 회전)·회오리(따라다님)
  - 얼룩 `spillSprites` 무작위, 레이저/번개 `FX_Beam.mat`(흰 빛줄기, `beamWidth`·`boltWidth`), 코인·총알·드랍 별·총공격 표시(`rushMarkerSize0/1`) 교체
- Play 확인 완료, 콘솔 에러 없음. 테스트 팁: Play 중 `Time.captureDeltaTime = 1/60` 주고 `FxManager.I.Anim(...)` 호출 후 스크린샷 (에디터가 백그라운드면 프레임이 띄엄띄엄이라 금방 사라짐)

- **바닥·벽 (층 구간 5종)**: Codex `stage_floor.png`(3×2, 512² 이음새 없는 타일) · `stage_wall.png`(띠 5줄, 자홍 선 구분) → `Assets/Art/Stage/floor_*.png`(640×544, TILT 미리 눌림) · `wall_*.png`(높이 156, 무늬 주기 맞춰 잘라 가로 이음새 없음). `Art/Stage/` 는 임포터에서 **PPU 200**.
  `StageManager.zoneLooks`(zone_id → 바닥·뒷벽·벽 윗면 색) → `MakeRoom` 에서 `Room.ApplyLook`. 1·15·20층 Play 확인.
- **글자 팝업**: 전용 머티리얼 `Fonts/IBMPlexSansKR-Bold SDF Popup.mat`(외곽선 0.28 + 그림자 underlay), `FxManager.popInTime·popOvershoot·popTilt` (easeOutBack 통통 + 무작위 기울기)

### 남은 것
- 체력바 위치 확인
- 아직 안 쓰는 새 그림: 연기·불꽃·독가스·땀·Zzz·하트·흙더미·번개 그림·불덩이(고양이 불덩이는 기존 원본 유지) → 잠(Zzz)·흙 파기(dig)·광란 등에 붙이면 됨

예전 목록 (참고):
- 파편 조각, 반짝이 별, 먼지 뭉게, 바닥 고리, 바닥 얼룩, 그림자 타원
- 레이저 줄, 번개 줄, 총알, 브레스·분사 입자, 소용돌이, 회오리
- 총공격 표시 원, 드랍 소품 대체(별), 체력바 위치
- 바닥(실험실 격자 타일)·벽(단색 사각형) — 웹도 코드로 그렸던 것
- 묘기·액션 글자 팝업 스타일

**Codex 사용법 (토큰 절약):**
```bash
bash Tools/codex.sh exec --skip-git-repo-check --ephemeral -s workspace-write -C <작업폴더> "<프롬프트>"
```
- 한 번에 한 장만 그리지 말고, **시트 한 장(예: 4×4 격자, 투명/단색 배경)에 여러 이펙트를 그리게** 한 뒤 Python(PIL)으로 잘라서 `Assets/Art/FX_New/` 등에 넣을 것.
- 원본 웹게임 그림체(파스텔, 굵은 갈색 외곽선 없음, 부드러운 평면)에 맞추기. 참고: `UnityResources/Rats/FX/`, `Items/`.
- 프레임 애니메이션이 필요한 것(번개·폭발·먼지)은 한 줄에 4~6프레임으로 그리게.
- 잘라 넣은 뒤 `FxManager` 템플릿·파티클 머티리얼·`CatManager` 레이저/불덩이 템플릿의 스프라이트만 교체하면 됨.

---

## 6. 진행 상황

### 끝난 것 (Game 씬에서 Play 확인, 콘솔 에러 없음)
- 빌드 설정(PC, 1920×1080, 제품명), 리소스 임포트·관절 피벗, 엑셀→JSON, TMP 한글 폰트
- 쥐: 5파츠 리그, 돌진·멈칫·갉기·발구르기, 번식(웹 공식), 클릭 총공격, 그림자
- 물건·가구: 피격 밀림, 날아감, 저글링·헤딩, 공중 충돌, 가구 쏟아짐, 박살 연쇄, 체력바
- 층: 시드 방 배치, 벽 체력·게이트, 방 확장, 계단 → 다음 층(페이드)
- 사람 4종: 걷기·기겁·날아감·벽 철퍼덕·통통·펑, 볼링, 대사
- 고양이 10종(품종 스킬) · 쥐덫, 쥐 기절·도망·데굴데굴
- 특수 능력(패시브) 24종, 묘기 4종, 잠, 택배 로켓배송
- 조각·종별 성장(로비 대신 임시로 판 중 자동 강화 `Progress.autoUpgradeInRun`), 특수 액션 17종
- 이펙트 매니저(파편·고리·얼룩·코인·흔들림·역경직·팝업)

### 남은 것 (순서 제안)
1. ~~이펙트 이미지 Codex 재제작·정리~~ (5장, 거의 끝 — 남은 소소한 것만)
2. **공용 스킬 트리 (진행 중)** — 웹 `RSKILLS` 35종
   - 끝: 테이블 `Data_Table/공용 스킬 테이블.xlsx` (위치·선행·해금 티어(웹 SKILL_RANK)·비용 `ceil(cost_base × cost_grow^L)` 치즈·effect_type + value) → `CommonSkillTable.json` → `GameDatabase.CommonSkills*`
   - 끝: 아이콘 35종 + 자물쇠 `Assets/Art/Rats/SkillIcons/cs_<code_id>.png` (256², 원본·시트·프롬프트 `UnityResources/Rats/SkillIcons/`). 시트 자르기 `slice_sheet.py --blobs` (칸 경계 넘은 그림도 덩어리째)
   - 끝: `Progress` 에 공용 스킬 레벨 저장 · `StateOf`(Owned/Open/Hint/TierLock/Hidden, 웹 mapState) · `CanBuySkill`/`BuySkill` · 인스펙터 `testSkillLevels`(테스트용 덮어쓰기)
   - 끝: `Core/CommonSkill.cs` 효과 수치 계산 (AtkMul·CheeseMul·묘기 확률 등, 테이블 식 그대로)
   - 끝: 게임 로직 연결 (사용자 결정: 효과 먼저, 구매 재화 = **치즈**, 판 밖에서만 구매) — 갉는 힘·크리·전기 이빨(`ItemManager.ZapChain`)·연쇄 폭발·가구·치즈 운석(`RatManager.UpdateMeteor`, 그림 cs_meteor)·저글링/충돌 치즈·고양이 피해/체력/겁·속도·멈칫·인구(`PopCap`)·번식·쌍둥이·돌연변이·탄생 축제·치즈·생성 속도/묶음/상한(`RoomCap`)·콤보·로켓배송·황금 물건·공용 묘기(`Rat.RollCommonTrick`, 백덤블링 기본 `RatManager.baseFlipChance` 4%)·벽·덫(치즈만 쏙)·총공격(`RushTime`·`RushDamage`). 전부 Play 확인
   - 아직 효과 없음 (시스템이 없음): 필살기 연습·슈퍼 점프 연습(필살기·슈퍼 점프 만들 때), 야행성(오프라인 수입 없음). 웹의 날아차기·배치기 묘기(2.5%·2%)도 아직 없음. 광란 배율은 코드 1.5 고정(테이블 Birth_Frenzy value_03 = 1.5 와 같음)
   - 남음: 로비 스킬 지도 UI (방사형, 가지 색 = Branch 시트, `Progress.StateOf`·`BuySkill`), 판 사이 치즈 저장
3. 성장 "특수 강화" 노드의 패시브 세기 반영 (테이블 값이 0레벨 위력이 곱해진 상태라 계산 방식 결정 필요 → 사용자에게 질문)
4. 필살기 31종 (`ULT_ENG`, 웹 약 2,500줄, 종별 연출 스크립트 `UltXxx`) + 필살기 앞모습 리그(FrontRig)
5. 보스 (5층마다: 경비대장·광기의 연구원·연구소장·고양이 보스 3종)
6. **로비 씬 (진행 중)** — 사용자 결정: 웹 구조 전부 순서대로 · 아지트는 웹처럼 살아 있게 · 판 끝은 ESC 포기만 먼저
   - 끝: `Lobby.unity` (처음엔 빌더로 만들었고, 사용자 지시로 빌더 스크립트는 삭제 → 이제 씬을 MCP 로 직접 고침). `Scripts/Lobby/`: `LobbyManager`(팻말 글·페이지·출발) · `LobbyHome`(아지트: 배경 cover·쥐 생활·수다·빼꼼·훈장 줄·말풍선·마우스 누르기/던지기) · `LobbyRat`(웹 HOME actor 상태·자세) · `LobbySlot`(활동 자리) · `LobbyHot`(물건 버튼+판자 팻말) · `RunPage`(작전 회의: 층 길·구역·방·출동 멤버·등급 확률·기록·출발)
   - 끝: `Progress` 에 치즈·연구자료·티어·최고 층·탈출 횟수 저장, `StartFloorCap`, `PendingStartFloor` → `GameManager.Awake` 가 층·티어·치즈 받음, 5초마다 치즈 저장. 게임 ESC = `QuitMenu`(포기 창, `FxManager.Paused`) → 로비. 로비 ESC = 아지트로. 로비↔게임 왕복·치즈 이월 Play 확인
   - 끝: 쳇바퀴 = 받침대 `WheelStand` + 도는 바퀴 `WheelRing` (새 플랫 그림 `UnityResources/Rats/Lobby_New/`), 쥐가 안에서 달리면 `LobbyHome.wheelSpinSpeed` 로 회전
   - 끝: 치즈 창고(SkillPage) · 쳇바퀴 훈련(TrainPage, §9-7)
   - 끝: 찍찍!! 훈장 페이지 + 업적 (RankPage, §9-9)
   - 남음 (순서): 친구들(도감) → 친구들(도감) → 낮잠 침대(기록·저장). 탭 아이콘·지도·쳇바퀴 등 로비 소품(`Rats/Lobby/lb_*`)은 예전 그림체 → 플랫으로 다시 만들지 결정
7. 로그라이크 메타(층 제한시간, 게임 오버, 연구자료), 저장
8. 사운드: 웹은 WebAudio 로 합성 → 같은 합성을 WAV 로 뽑기 (BGM 만 필요하면 Gemini)
9. 손에 든 소품 그림(ACT_HOLD), 슈퍼 요리사 쥐 탈것, 컴퓨터 마우스 쥐(한 장 그림) 확인
10. PC 빌드 테스트

### 알아둘 점
- 테스트할 때 `RatManager.startRats`(코드 id 목록)·`startCount` 로 시작 쥐 지정 가능. **테스트 후 비워 둘 것.**
- 진행도 초기화: `Progress` 컴포넌트 우클릭 → "진행도 초기화" (또는 PlayerPrefs 키 `nkk_progress_v1` 삭제).
- 웹 원본 수치를 그대로 옮겼으니 밸런스 조정은 테이블/인스펙터에서.

## 7. 필살기 · 슈퍼 점프 · 이펙트 (2026-10-06~07)
- **필살기 게이지** `Scripts/Ults/UltimateManager.cs` (Game 씬 `UltimateManager`): **쥐 개체마다 게이지** (`Rat.ultGauge`, 2026-10-07 사용자 결정 — 같은 종이 여럿이면 모은 그 쥐가 씀). 충전 조건 = 쥐 테이블 `Ult_Charge` 시트(조건을 한 그 쥐만 참, 층 통과는 전원), 요구량 = `Ultimate.ult_gauge`, 충전량 × 공용 스킬 필살기 연습. 다 차면 그 쥐 머리 위 반짝이(`readyMark` = `FX/Tint/twinkle`, 필살기 색) + `HUD/UltBar` 에 **그 쥐 버튼 하나**(최대 `maxButtons` 8, 버튼에 마우스 → 그 쥐 반짝이 커짐) → 대기열(한 번에 하나). 공용 스킬 `Ult_Auto` 면 자동. 찍찍 탐정 단서도 개체별 (`Rat.clues`).
  - 흐름: 컷인(`HUD/UltCutIn`) → 상황극(`UltXxx : UltBase`, 33종 `Scripts/Ults/Ult*.cs`, 테이블 `Ultimate.script` = 클래스 이름) → 업적(`HUD/UltAchievement`, 오른쪽 위). 업적은 `Progress` 저장 — **로비 표시는 찍찍!! 훈장과 나중에 연동**.
  - 글 = `Ult_Caption` 시트 (키 c1…, `{n}`). 소품 = `UltimateManager.props` (컴포넌트 메뉴 **Fill Props** — 새 그림 넣으면 꼭 다시 실행), 아이콘 = Fill Icons.
  - 도우미 `UltBase`, 쥐 `Rat.Ult.cs` (UltOn·UltPose·HideBody·좀비·기절 별), 물건 `Item.Held/SkillHit/Fling/Survive`.
  - 웹과 다른 보강: 찍찍 탐정 = 공 5개 핀볼, 산타·마법사 = 돌아다님, 쥐.D = 록 무대, 바이킹 강화, 스트리머 = 앞발에 쌍권총, 드래곤 = 입에서 브레스, 람쥐썬더 = 웹 구조(옆모습 꼭두각시 + 앞모습 히어로 랜딩 파츠) + 전기 연출 강화. **실사 다람쥐 `rsp_*` 그림 금지.**
- **슈퍼 점프** `Scripts/Ults/SuperJumpManager.cs`: 웹 drawSuperJump 1:1 (만화 칸 `sj_band`, 깜빡이는 집중선, 컷인 속 쥐 = 전용 카메라→RenderTexture→`HUD/SuperJumpFx/CutIn/BandRat`, 만화 글씨 재질 `Assets/Fonts/SJ Comic *.mat`). 진행 중 `FxManager.WorldFreeze`.
- **테스트 패널** `HUD/TestPanel` (왼쪽 위): `< >` 필살기 고르기 · 필살기 발동 · 슈퍼 점프. 출시 때 끄기.
- **이펙트**: FxManager Beam/Bolt/BoltLine(Zap 핸들, 살아 있는 동안 다시 꺾임)/Spark/Slash/BigBeam/Crackle (`Art/Rats/FX_Beam`). 운석 = `ItemManager.Meteor.cs` (`FX_Meteor`). 별 = 그림체 맞춘 새 별 (`UnityResources/Rats/FX_Stars`).
- **벽**: 웹 drawWallSeg 방식 — 열린 방 사이 벽 없음, 위·아래 벽 = 벽 면, 옆 = 윗면 띠, 정렬로 물건 안 가림. 벽 체력바 = `WallBar.cs` 템플릿 (FxManager worldCanvas 아래, StageManager.wallBarTemplate). 카메라 배경 #3d4a45.
- **새 쥐** jjdetective·fondue (파츠 v2, 몸길이 덮어쓰기 36/40), 쥐록 홈즈 코트 파츠. **슈퍼 요리사 쥐** 탈것 = `Rat.Mount.cs` (cook_mount 사람 리그, Rat 프리팹 메뉴 Fill Mount Parts). **줴리** 필살기: 턱시도 파츠 `jwt_*`, 마스크 어둠, 말풍선.
- **폰트**: 정적 SDF + 동적 보조 3종(`Assets/Fonts/* Dynamic SDF`)을 대체 목록·TMP 전역 대체에 연결. 이모지·╯·⚠ 는 어떤 폰트에도 없음 → 쓰지 말 것.
- **판정 순서**: 물건에 부딪히면 특수 액티브 먼저, 안 터지면 공용 묘기.
- **Git**: `.gitignore` = Unity Library/Temp/Logs/UserSettings/csproj/Screenshots 만 제외 (UnityResources 포함). 다른 PC 에서 열면 Library 자동 재생성.

## 8. 작업 방식 (에이전트 인수인계)
- 그림은 **Codex 로만 생성** (`bash Tools/codex.sh exec ... < prompt.txt`, `-i` 참고 그림 여러 개, 자홍 배경 시트 → `Tools/slice_rat_parts.py` key_magenta 로 자르기). 코드로 그림 그리지 말 것. **먼저 웹 코드 그림 목록과 UnityResources 에서 기존 그림을 찾아 재사용.** 새 그림은 `UnityResources/Rats/<폴더>/` 에 시트·프롬프트·로그·README·_미리보기 와 함께 보관 후 `Assets/Art/Rats/<폴더>/` 복사.
- 웹게임(`Proto_Game/rat-uprising.html`)과 같은 방식으로 만들 것 — 다르면 사용자가 지적함. 연출은 단조롭지 않게.
- 병렬 작업: 서브에이전트는 맡은 파일만 고치고 Unity 조작 금지(임시 csproj 로 dotnet build 확인), Unity 반영(컴파일·씬 연결·Fill Props)은 메인이 마지막에. 사용자가 에디터에서 테스트 중이면 Unity 건드리지 말 것.
- 남은 일: 코드에 직접 들어간 팝업 글(Rat.Action "찌릿!!/콰릉!", ItemManager.ZapChain "찌릿!", 웹 요리사 말 팝업 등) → 테이블/인스펙터로 · 로비 업적(훈장 연동) · 벽 금 간 자국(웹은 선) · 사용자 플레이 피드백.

## 9. 다음 작업 (2026-10-07 기준, 다른 에이전트 이어서) — 시작 전에 §0 규칙·§7·§8 꼭 읽기
### 9-1. 필살기 게이지 — 이미 완료 (확률 발동 아님)
- 필살기는 **게이지 방식**으로만 발동: `UltimateManager.Charge` 로 쥐 개체별 게이지가 차고(`Ult_Charge` 시트), 다 차면 하단 버튼(또는 공용 스킬 `Ult_Auto` 자동) → 대기열. 웹의 `ULT_CHANCE`(확률 발동)는 옮기지 않았음. 코드 안 `Random` 은 테스트용 쥐 소환 위치·컷인 제목 흔들림뿐.
- 확률로 저절로 터지는 건 **슈퍼 점프**(`SuperJumpManager.chancePerSec` = 1/480 초당, 쿨 120초) — 웹과 같음.
- 차오르는 과정 UI 는 **만들지 않음** (사용자 결정 2026-10-07).

### 9-2. 층별 제한시간 — 완료 (2026-10-07)
- `Core/RunTimer.cs` (Game 씬 `RunTimer`): 웹 floorTime = `baseTime 190 + perRoom 35 × 방 수 + (보스 층 bossExtra 90)`. `StageManager.FloorEntered` 이벤트로 층마다 다시 채움.
- 필살기(`UltimateManager.Busy`)·슈퍼 점프·`FxManager.WorldFreeze/Paused`·층 이동 페이드(`StageManager.Climbing`)·게임 오버 중엔 멈춤. 인스펙터 `testFreeze` = 시간 안 줄어듦, 컴포넌트 메뉴 "테스트: 12초 남기기".
- 60·30·10초 경고 배너(글 = 인스펙터 `warnTitle {n}`·`warnSub`) + 빨간 번쩍, 30초 아래 빨갛게 깜빡, 10초 아래 1초마다 글이 통 튐. 0초 → `GameOver.Begin(Time)`.
- HUD: `HUD/TimeText` (글 "남은 시간 {m}:{s}") · `HUD/TimeBar/Fill` (가로 앵커로 줄어듦).

### 9-3. 게임 오버 — 완료 (2026-10-07)
- `Core/GameOver.cs` (Game 씬 `GameOver`, 정적 `GameOver.Active`): 웹 startGameOver/updateGameOver/drawGameOverFx/showGameOver 1:1.
  - 경비원 `min(24, 12 + 쥐/4)` (사람 프리팹 + `Human.BeginRaid/RaidStep`, Items.Humans 목록엔 안 넣음) · 고양이 `min(10, 5 + 쥐/10)` 실제 품종 (`Cat.BeginRaid/RaidStep`, 체력바 숨김) — 화면 사방에서 가장 가까운 쥐로. 잡힌 쥐 = 기절 999 + `Held` + 철창(`GameOverCages/CageTemplate`, 웹 그림 `Rogue/rg_cage`). 남은 쥐는 360 안의 습격자 반대로 도망.
  - 경비원 대사 = 사람 테이블 Human_Line 새 상황 `Raid`·`Raid_Catch` (Situation_Type 에도 추가). 잡힐 때 팝업·배너 글 = 인스펙터.
  - 화면: `HUD/GameOverFx` (붉은 비네트 `sj_vignette` 맥박 · 2.5초부터 어두워짐 · 양쪽 경보등 `rg_siren` + 빙글 빛줄기 `sj_streak` · "일망타진!!!" 3.2초까지).
  - 4.5초 뒤(다 잡히면 2.2초) + 0.6초 → `HUD/GameOver/Panel` 결과 창 (이유 WhyTime/WhyBoss, 성과 `{floor} {start} {research} {best} {n}`, 아지트로 버튼 → 치즈 저장 후 Lobby).
  - 게임 오버 중엔 총공격 클릭·필살기 요청·슈퍼 점프·ESC 포기 창·고양이 등장·계단·쥐덫 멈춤.
  - `GameManager.StartFloor`(이번 판 시작 층) · `RunResearch`(이번 판 연구자료, 아직 0 — 층 탈취 연출 만들 때 채울 것). 보스 패배(`Why.Boss`)는 보스 만들 때 `GameOver.Begin(GameOver.Why.Boss)` 호출.
  - 테스트: GameOver 컴포넌트 메뉴 "테스트: 게임 오버 (시간 초과)".

### 9-4. 층 클리어 연출 — 완료 (2026-10-07)
- `Core/Heist.cs` (Game 씬 `Heist`, 정적 `Heist.Active`): 웹 startHeist/updateHeist/drawHeist 1:1. 쥐(임시·필살기 중 제외)가 계단에 닿으면 `StageManager.Climb()` → 탈취 연출 → 2.2초에 페이드 → 다음 층, 2.7초에 연출 끝.
- 연구자료 = `round(6 × 1.45^(층-1))` (보스 층 ×3) → `Progress.research`(저장) + `GameManager.RunResearch`(결과 창 "이번 판 연구자료"). 화면 속 쥐는 계단으로 총공격 돌진, 계단에 종이 파편·별.
- 화면 `HUD/HeistFx`: 빨간 테두리 깜빡 · 경보등 2개 · "연구 자료를 훔쳤다!!!" / "빨리 도망가!!!"(0.45초~) / "연구자료 +{n}"(0.8초~) · 자료 뭉치 `Rogue/rg_docs` 통통 · 종이 12장 `Rogue/rg_paper` 가로지름. 연출 중엔 필살기 대기열·슈퍼 점프 멈춤, 제한시간도 멈춤(Climbing).
- 테스트 패널에 **층 클리어 · 게임 오버** 버튼 추가 (`UltTestPanel.clearButton/gameOverButton`).
- 연구자료 드랍 — 완료: `Core/Research.cs` (Game 씬 `Research`, `Research.I.Earn`). 웹 researchDrop: 가구 12% (1~3) · 물건 1.2% (1), `ItemManager.OnSmashed` 에서. 화면에 보이면 종이 아이콘(`Rogue/rg_paper`, 템플릿 `Research/PaperIconTemplate`)이 통 튀어 오르고 "연구자료+{n}"(인스펙터 글). 층 클리어도 `Research.Earn` 으로 저장 (`Progress.research` + `GameManager.RunResearch`). 컴포넌트 메뉴 "테스트: 화면 가운데에 연구자료 +2".

### 9-5. 5층 보스 — 완료 (2026-10-07)
- `Hazards/Boss.cs` (Game 씬 `Boss`, 정적 `Boss.Current`): 웹 makeBoss/startBossFight/updateBoss/bossStompLand/bossDown 이식. 사람 리그(`Boss/Rig`, 키 = 사람 × scale) + 그림자.
- 데이터 = 새 **스테이지 테이블** (`Data_Table/스테이지 테이블.xlsx` → `StageTable.json`, GameDatabase.stageTable — Game·Lobby 씬 둘 다 연결): `Boss`(층·공격 타입·크기·체력 = 적정 전투력 × hp_pow_sec·치즈·속도·공격 간격/반경/기절) · `Boss_Line`(Intro/Attack/Hit/Down 대사).
- 흐름: 보스 층 진입 → 계단 방에서 대기(배너 부제 "보스 층! 계단 방에 {name}") → 계단 방 벽이 무너지면 전투(배너·대사·번쩍·카메라) → 쥐 쪽으로 걸어옴, 3~4.5초마다 내려찍기(웅크림 0.5초·화난 얼굴 → 점프 → 착지 반경 300 쥐 기절 2초·데굴, 물건 날아감, 쿠웅!) → 체력 0 → 하늘로 빙글 → 격파 배너·치즈·코인. 살아 있는 동안 계단 막힘, 고양이 안 나옴.
- 피해: 쥐 들이받기(`Rat.BumpBoss`, 보스전 중 45% 확률로 보스 쪽으로 돌진) · 필살기/슈퍼 점프(`ItemManager.BlastActors`) · 충격파(`Aoe`). 맞을 때마다 그 쥐 필살기 게이지 `Hit_Boss`. HUD `HUD/BossBar`(이름·체력·맞을 때 흰 번쩍).
- 그림: Codex 새 디자인 `UnityResources/Rats/Humans/Sheets/boss_v2/` (몸통에 팔 없음 — 예전 시트는 팔이 4개로 보였음). 사람 리그에 **화난 머리**(`angry`, Pose.angry) 추가.
- 테스트 패널 **보스 소환**: 화면 가운데에 불러 바로 전투 (체력·치즈는 지금 층 기준, 이 층 진짜 보스가 대기 중이었으면 끝난 뒤 되돌림).
- 글은 Boss 인스펙터(floorSub·fightSub·downTitle·downSub·stompPopup·cheesePopup). 10·15층 보스 등은 아직 없음.

### 9-6. 훈장별 공용 스킬 트리 · 승급 · 스테이지 테이블 (2026-10-07, 진행 중)
**끝난 것 (커밋됨)**
- 공용 스킬 = 찍찍!! 훈장(티어 1~8)마다 트리 하나, 노드 38개(시작점 포함). 노드는 **한 번 활성화**, link_1/2 중 하나라도 활성화되면 열림. 같은 효과는 값을 **더함**(공격력 % 와 피해량 % 는 각각 더한 뒤 둘만 곱함). 생성기 `Tools/gen_skill_tree.py` (칸 배치 SLOTS · 훈장별 효과 T[t] · 비용 CHEESE/RESEARCH · 아이콘 ICON) → `공용 스킬 테이블.xlsx` → xlsx2json.
  - 비용: 치즈 + 연구자료(2훈장부터). 묘기 해금: 1훈장 백덤블링 · 2 윈드밀 · 3 트리플 악셀 · 4 쥐 대포알 (해금 전엔 안 나옴, `CommonSkill.TrickChance`).
  - 효과 계산 `Core/CommonSkill.cs` (Progress.SkillVersion 캐시). 효과 목록·의미 = 테이블 Common_Effect_Type 시트.
  - `Progress`: 노드 저장 `nodes`(예전 레벨식 skills 는 무시), `StateOf`(Owned/Open/Locked/TierLock), `BuySkill`, **훈장 승급** `CanRankUp/RankUp`(티어 테이블 연구자료 + 조건: Max_Floor · Shard_Level_Sum · **Skill_Node_Count**), 시작 층 = 1 + 스테이지 스킵 노드. 테스트: 인스펙터 `testSkills`(노드 id) · `testSkillTier`(그 훈장 이하 전부).
  - 로비 `Lobby/SkillPage.cs`: 훈장 탭 8개(`MapCard/Tabs`) · 탭 열면 트리 전체 맞춤 · 못 단 훈장 탭 = 승급 패널(`MapCard/RankPanel`) · 상세 카드 비용 2줄(치즈·연구자료). 글은 씬 `DetailCard/Words`.
  - 묘기 성공 = 특수 액션처럼 필살기 게이지 +5 (`Action_Use`) × 묘기 게이지 노드.
- 물건: 물건 테이블 `from_floor`·`to_floor`·`unlock_skill` — 층·스킬 해금에 따라 나오는 물건이 늘어남 (구간 Zone 의 물건 목록 칸은 삭제, 바닥 그림용만). 새 물건 8종(Codex, `UnityResources/Rats/Items_New/`) 훈장별 "신규 물건" 노드로 해금. 새 아이콘 16종 `SkillIcons/Sheets/skill_icons_c.png`.
- 승급(게임 화면): `RatManager.Promote/PromoteAll/PromoteNeed`(기본 10마리, 노드로 감소, 최소 4, 일괄 때 6마리 남김) + `Rats/PromotePanel.cs` (HUD 왼쪽 아래 `HUD/Promote`).
- 스테이지 테이블 **Stage 시트**(층 1~30: 방 수 · 적정 전투력 · 물건 체력 배율 · 치즈 배율 · 계단/일반 벽 배율 · 추가 시간). 생성기 `Tools/gen_stage_table.py` (POW0 400 · POW_GROW 1.38 · HP_GROW 1.38 · CHEESE_GROW 1.5 · 벽 계단 3+0.6(f-1) 최대 10 · 일반 1.2). 30층 넘으면 마지막 두 층 비율로 이어서. 코드: `StageManager.PowNeed/ItemHpK/CheeseK/TimeAdd`, 물건·사람·고양이·보스·제한시간이 이걸 씀. (예전 수식은 ×3.3/×3.6 이라 새 덧셈식 스킬로 못 따라감)

**밸런스 측정 (BalanceProbe, Game 씬 컴포넌트 켜 둠 — 측정 아닐 땐 스스로 꺼짐)**
- 사용: Play 중 `NKK.BalanceProbe.Results.Clear(); NKK.BalanceProbe.Run(티어, 시작층, 끝층, 포기초, 20, 노드수, 조각합);` → `NKK.BalanceProbe.Report()` 로 읽기. 저장은 `nkk_probe` 키만 씀. 제한시간은 재기만 함(멈춤), 쥐가 꽉 차면 5초마다 일괄 승급, 클릭 총공격 없음(최소 성능).
- **유니티 창이 뒤에 있으면 배속이 크게 느려짐** (실시간의 0.5~3배). 측정 중엔 에디터를 앞에 두기 권장.
- 결과 (현재 곡선): 1훈장 노드 0 → 1층 197초/295 (전투력 0.84배) · 2층 242초/295 (0.62배) · 3층 시간 초과 예상. 1훈장 37노드 → 1층 73초 (1.66배) · 2층 78초 (1.61배) · 3층 154초/338 (1.19배) · 4층 250초 넘어도 계단 못 엶 (0.8배). 치즈 1~3층 약 20K.
- 4훈장(노드 100·조각 40) 7층부터 측정 중 끊음: 시작 쥐 12마리로 번식이 느려 7층 130초에 방 3/6, 전투력 970 / 적정 2760 → **중반 이후 곡선이 너무 가파를 가능성** (시작 쥐·번식 속도·POW_GROW 확인 필요).
- 측정 중 NullReferenceException = 측정이 카메라를 꺼서 `FxManager.Coin` 의 Camera.main 이 null → 고침 (카메라 없으면 건너뜀).

**2026-10-07 오후 — 승급 개편 · 공격력 1.8배 · 번식 비율 · 총공격 제한 (커밋 전)**
- 측정 버그: 카메라를 꺼서 `Rat.OnScreen`(승급 정렬)·고양이 등장(`Camera.main`)이 깨짐 → 이전 측정은 "승급 0·고양이 없음"이었음. 이제 메인 카메라는 켜 두고 cullingMask 0.
- 시작 쥐 (사용자 결정): 티어 테이블 start_rat_count 6/8/12/18/25/32/40/48, 스킬 시작 쥐 노드 마릿수 늘림 (2·4·6훈장 G8 = 시작 쥐).
- **승급** (사용자 결정, 메모리 nkk-promote-decisions): 필요 마릿수 = 올림(promote_base 1.5 × promote_grow 1.3^k), k = 이번 판 그 등급 승급 횟수 → promote_soft 8 넘으면 promote_grow2 1.05 로 완만 (쥐 등급 테이블 칸). 일괄 승급은 최대 마리 수 × promoteKeepRatio(0.5) 남김. 공용 스킬 Promote_Need 삭제 → **Promote_Double**(승급 때 2~4% 확률로 2마리, 팝업 `promoteDoublePopup` 씬 인스펙터).
- **공격력** (사용자 결정): 쥐 공격력은 등급 비례 유지, 등급 간 1.8배 → atk_base 10/18/32/58/105/189, 쥐 캐릭터 atk = atk_base × 티어 rat_atk_mul(unlock_rank) 로 다시 계산. 액션·필살기·슈퍼 점프 피해 = `Rat.SkillDamage` = 공격력 × 등급 skill_power(1/0.83/0.66/0.52/0.42/0.34 = 예전 위력 유지값, 측정하며 조정).
- **번식** (사용자 결정): 확률 = 1/(1+((마리 수÷최대 마리 수)/breedHalfRatio 0.5)^breedRatioExp 3), 5마리 이하 100%.
- **총공격** (사용자 결정): 같은 대상이 rushStackWindow 0.5초에 rushStackMax 12번 넘게 맞으면 rushStackOverMult 20%. 클릭 쿨타임 rushCooldown 3초(돌진 끝난 뒤, 최소 1초), `RatManager.ClickRush`. 공용 스킬 **Rush_CD** -0.5초 (2훈장 S6 · 5훈장 S6 · 8훈장 S5).
- 측정 도구: 대기열 `BalanceProbe.Queue` + `RunQueue()` (인자 8번째 = 승급 방식 0/1/2), `UseUlt`(필살기 차면 바로, `UltimateManager.ForceAuto`), `UseRush`(쿨타임마다 보스→계단 벽→약한 벽 총공격), 시간 = `RunTimer.Used/LastUsed`(필살기·연출 제외), 제한 넘으면 실패.
- 측정 결과 (일괄 승급·필살기·총공격 사용): 3훈장 7층 63% · 8층 실패 (목표대로, 총공격 넣기 전). 4훈장 7~11층 전부 통과 (10층 보스 27%, 11층 0.71배인데 84%). 5훈장 10층 21% · 11층 실패. 6훈장 13~17층 통과 (16층 0.56배인데 16%). 7훈장 16~20층 통과 (20층 보스 0.58배로 12%). **총공격·필살기가 너무 셈** — 고훈장일수록 전체 시간의 절반 이상이 필살기, 보스가 허들 역할 못 함. 계단 방 벽(적정 × 3+0.6(층-1))은 총공격 없으면 못 뚫음.
- `Assets/_Recovery/0 (1).unity` = 컴퓨터 꺼질 때 유니티가 만든 복구 씬(필요 없음).

**2026-10-07 저녁 — 이어서 바뀐 것 (커밋 전, 전부 Play 확인·콘솔 에러 없음)**
- 총공격 너프 (사용자 결정): rushDamageMult 1.5 · rushStackMax 6 · rushStackOverMult 0.1 (씬 값도).
- 필살기 (사용자 결정): 게이지 2배 (Ultimate.ult_gauge 100~240) · ultDamageK 13 · ultItemK 4.5 · **필살기 쿨타임** `UltimateManager.ultCooldown` 25초(최소 8, 하나 끝난 뒤 다음까지, 게이지는 계속 참) · 공용 스킬 **Ult_CD** (4훈장 S2 -3 · 6훈장 S6 -3 · 8훈장 S2 -4초).
- 보스 체력 감소 노드(Boss_Hp_Down) 완전 삭제 (사용자: 없애기로 했던 것). 실제로는 1훈장 -50% 가 모든 보스에 걸려 있었음 → 보스 체력 2배가 됨. S4 자리 = **보스에게 주는 피해 +10~25% · 보스 층 시간 +3~4초** (Boss_Dmg_Pct value_02 = 보스 층 추가 초, `CommonSkill.BossTimeAdd`).
- **새 보스 5종** (사용자 결정: 그림 Codex 새로, 보스전 별도 시간 없음): 스테이지 테이블 Boss 6행(5 경비대장 · 10 광기의 수석 연구원 · 15 연구소장 · 20 거대 메인쿤 · 25 마녀 고양이 · 30 우주 고양이, 그 뒤 반복 `GameDatabase.BossOf`) · 칸 rig(Human/Cat)·atk2_type·atk2_chance · **Atk_Type 시트**(공격별 radius·stun·count·windup·dur, 보스 행 atk_radius/atk_stun 은 삭제) · Boss_Line 새 대사.
  - `Hazards/Boss.cs`: 공격 Stomp·Pounce·Flask·Hairball·Fireball·Swing(guardEvery 번마다 경비원 count 명, `ItemManager.SpawnHumanAt`)·Gravity, 투사체(`Boss/BossShotTemplate` 루트 오브젝트, flask/hairball/fireball 스프라이트), 고양이 보스 = `Boss/CatRig`(Cat 프리팹 리그 복사, 크기 = catLength × scale 3.4, 웹은 ×4.2), 테스트 보스 소환 = 누를 때마다 다음 보스. 팝업 글 = 인스펙터(pouncePopup 냥냥펀치!! · swingPopup 퍽!! · gravityPopup 무중력!! · hairPopup 털뭉치! · guardCall 경비! 경비이!!).
  - 그림: `UnityResources/Rats/Humans/Sheets/boss_v3/`(연구원·연구소장 시트·프롬프트·README, 예전 웹 파츠 old_web_parts/) → Parts/boss_mad·boss_director · `UnityResources/Rats/BossProps/`(flask·hairball·splash_green) → `Assets/Art/Rats/BossProps/`.
- **훈장 승급 조건** (사용자 결정): Skill_Node_Count = **지금 훈장 트리에서** 찍은 수 (`Progress.SkillCountIn(tier)`), 티어 테이블 2→8훈장 15·18·20·22·24·26·28.
- **제한시간** (사용자 결정): 모든 층 **180초** (방 수 무관, `RunTimer.perRoom` 0) · 보스 층 +30 · 제한시간 노드 G2·G6 16개 합 +120초 (3~12초) · 보스 노드 합 +30초 → 다 찍으면 300 + 보스 60. 방 수는 최대 9 그대로.
- 측정 도구: `BalanceProbe.MakeMeta` = **이전 훈장 트리 전부 + 지금 트리 nodes 개**(싼 것부터, -1 = 다음 훈장 조건 수, 99 = 전부) · 조각 shards(-1 = 조건) · `Calib`(적정 고정: `StageManager.PowOverride` = 무리 전투력 → "전투력 = 적정일 때 몇 초" 측정, 제한시간 넘어도 계속) · 보스전 시간 줄(`└ 보스전 N초`). **측정 중엔 스크립트 수정 금지** (플레이 중 도메인 리로드로 GameDatabase null·StunStars 에러 대량 발생).

**측정 결과 요약**
- 최소 상태(이전 트리 전부 + 승급 조건만큼, 제한시간 개편 전): 다음 훈장 조건 층을 17~49% 시간에 통과, 전투력 1.3~1.7배 → 너무 쉬움.
- 적정 고정(전투력 = 적정, 제한 180+노드): 2훈장 2·3·4·5층 16·18·54·106% · 3훈장 4~8층 24·74(보스)·57·71·169% · 4훈장 7~11층 27·96·92·84(보스)·75% · 5훈장 10~14층 45(보스)·131·145·69·44% · 6훈장 13·14·15층 91·40·106(보스)%. **층마다 편차 큼** (방 배치·시작 방 거리·계단 벽 배율 3+0.6(층-1)).

**다음 할 일 (사용자 목표)** — 1번은 §9-8 에서 진행 (1차 반영, 재검증 필요)
1. ~~전투력 = 적정일 때 제한시간의 70~80%~~ → §9-8
2. 그다음 **최소 상태(이전 트리 전부 + 지금 트리 승급 조건만큼)는 다음 훈장 조건 층을 아슬아슬하게(85~100%)**, **최대 상태(지금 트리까지 전부, 조각 1.5배)는 수월하게(40~60%)** — 적정 전투력 곡선(POW0·POW_GROW) 또는 노드 너프. 사용자에게 "허들 올리기 vs 노드 너프" 다시 물어볼 것 (직전 질문은 답 없이 넘어감).
3. 승급이 판 후반(일반 40회쯤) 막힘: 일괄 승급이 최대 마리 수 절반을 남기는데 필요 수(완화 1.05)가 그보다 커짐 → promote_grow2·promote_soft 검토.

### 9-7. 쳇바퀴 훈련 (조각 강화) — 완료 (2026-10-07)
- 사용자 결정: 성장은 웹처럼 **정해진 순서 자동** (훈련 = 조각으로 Lv+1, 노드는 Growth_Order 순서), **Lv 3 = 특수 액션 해금 · Lv 7 = 필살기 해금**, 패시브는 처음부터 켜짐.
- 쥐 성장 테이블: 새 노드 10 `필살기 해금`(ultUnlock, effect `Ult_Unlock`) · Growth_Order 고정 구간 25개로 다시 짬 (선행 조건이 다 맞게 — 예전 순서는 특수 강화·급소 갉기가 선행 미달이라 엉뚱한 노드로 대체되고 있었음). 각성은 Lv 24. 원본 백업 `Data_Table/_backup_20261006/쥐 성장 테이블_before_ultunlock.xlsx`.
- 코드: `Progress.NodeAt(L)`(레벨 L 에 찍히는 노드) · `UnlockLevel(effect)` · `UltUnlocked(code)` · `UpgradeAll()` · `UpgradableCount`. `Rat.UltUnlocked` → `UltimateManager.Need(Rat)` 가 해금 전엔 0 (게이지 안 참·버튼 안 뜸). 테스트 패널 필살기 발동은 해금과 상관없이 됨.
- `Progress.autoUpgradeInRun` 기본 꺼짐 (Lobby·Game 씬 값도 끔) → 판 중엔 조각만 모이고 강화는 로비에서.
- 로비 `Page_rats` (`Lobby/TrainPage.cs`, 씬에 MCP 로 직접 만듦): 왼쪽 ListCard = 등급 필터 칩(강화 가능 수 배지) · 다 같이 훈련 · 카드 그리드(그림·이름·Lv·조각 막대·훈련 버튼, 강화 가능 먼저) / 오른쪽 DetailCard = 그림·등급·Lv·설명·"다음 훈련으로 배우는 것"(다음 6레벨 노드, 해금 노드 강조) · 특수 능력 · 특수 액션(Lv 3 전엔 흐림 + "Lv {lv}에 해금") · 필살기(아이콘, Lv 7 전 흐림, 없는 종은 NoUlt) · 훈련하기. 글은 전부 씬(`Page_rats/Words` 포함). 팻말 알림 점 `LobbyManager.Alert("rats")`.
- UI 쥐 그림 `Lobby/RatPortrait.cs`: RatRig 와 같은 관절 배치로 UI Image 8개(하이라키에 있음)를 조립해 칸에 맞춤 — 도감 등에서도 재사용.
- 필살기 아이콘 = TrainPage 컴포넌트 메뉴 **Fill Ult Icons**.
- **밸런스 영향**: 이제 필살기는 Lv 7 종만 씀. `BalanceProbe.MakeMeta` 는 조각 레벨을 일반·레어 종에만 나눠 줌 → 측정 때 필살기가 거의 안 나옴. 다음 측정 전에 "티어별로 필살기 해금된 종 수" 가정을 정해서 MakeMeta 에 반영할 것.

### 9-8. 층 벽 밸런스: 적정 전투력이면 제한시간 70~80% (2026-10-07, 1차 반영 · 재검증 필요)
사용자 목표: **전투력 = 적정일 때 180초(보스 층 +30 = 210초)의 70~80% 로 클리어** (노드 추가 시간 빼고 기준).

**알게 된 것 (BalanceProbe 적정 고정 · 1~8훈장 1→25층)**
- 층 지형은 층 번호가 시드라 항상 같음 → 층마다 "시작 방 → 계단 방 최단 경로의 벽 체력 배율 합"(S)이 4.5~28 로 들쭉날쭉했던 게 편차의 큰 원인. 예전 일반 벽 거리 가중(1 + 0.25 × 거리)이 긴 경로를 더 키움.
- 걸린 시간 ≈ S × (배율 1당 초 SPU). SPU 는 **지형마다 크게 다름** (2~35초): 쥐는 돌진하다 부딪힌 벽만 갉으므로, 바깥벽이 많은 복도형 지형은 엉뚱한 벽에 부딪혀 느림. 같은 층은 회차가 달라도 대체로 비슷하게 느리거나 빠름 (11·15·17·21층 느림, 16·22·24층 빠름). 한 층 안의 회차 편차도 ±50% 정도로 큼 → 층마다 2회 이상 재야 함.
- 초반(1~4층)은 쥐가 적어 전투력 대비 벽을 훨씬 빨리 뚫음 + 벽을 키워도 시간이 덜 늘어남(판 안에서 번식으로 전투력이 커짐).
- 측정 도구 버그 2개 고침: 총공격이 계단과 상관없는 약한 벽을 노림 → **계단 쪽 길의 벽**을 노리게. 계단 방이 열린 뒤에도 남은 벽만 노려서 먼 계단에 늦게 닿음 → **계단 방이 열리면 계단으로 총공격** (이 수정 뒤로는 아직 못 잼).

**바꾼 것**
- `Tools/gen_stage_table.py`: 층마다 StageManager 와 같은 지형을 계산(SeededRandom 이식, maxRow 3)해서 S = 목표 초 ÷ (SPU[f] × 공용 스킬 벽 체력 배율) → 계단 벽 = S × 50%, 일반 벽 = 나머지 ÷ 경로 일반 벽 수 (최소 0.8). 목표 초 = 135 (보스 층 157 − 보스전 40). `SPU` 표 = 층별 측정값 (없는 층 10). 50층까지 생성.
- `StageManager.wallDistK` (인스펙터, 0) = 일반 벽 거리 가중 (예전 0.25 고정).
- 스테이지 테이블 Boss 시트 `hp_pow_sec`: 5층 20 · 10층 28 · 15층 40 · 20/25/30층 36 (보스전 측정 5층 55~88초 → 40초 목표).
- BalanceProbe: 층마다 `└ 기준 180초의 N% · 계단 거리 · 경로 벽 배율 합` 줄, `Rows`/`RowsCsv()`(층·사용 초·기준·거리·S·방·성공·보스전 초), `BaseTime 180`·`BossBaseTime 30`.

**측정 결과 (3회차 = 지금 SPU 표 직전 값, 층별 2~4회, 계단 총공격 수정 전)**: 전체 중앙값 63%, 64번 중 25번이 60~90%.
- 1·2·3·4층 60·55·34·59% · 5(보스) 79 · 6 78 · 7 62 · 8 100 · 9 71 · 10(보스) 83 · 11 90 · 12 111 · 13 68 · 14 67 · 15(보스) 150 · 16 34 · 17 91 · 18 67 · 19 67 · 20(보스) 56 · 21 163 · 22 37 · 23 71 · 24 45 · 25(보스) 64.
- 그 뒤 60% 아래였던 층(1~4·16·20·22·24)만 SPU × 측정%/75 로 보정해서 지금 테이블에 반영 (1층 S 94 · 3층 79 · 16층 72 등 — 쥐 적은 초반은 벽을 크게 늘려도 시간이 덜 늘어서).
- 100% 넘던 층(8·12·15·21)은 계단 총공격 버그 영향이 커서 그대로 둠 → 다시 재면 빨라질 것.

**다음**: 같은 큐로 다시 재기 (Play 중 아래 코드) → 층별 중앙값으로 `SPU[f] × 측정%/75` 갱신 → `python Tools/gen_stage_table.py && python Tools/xlsx2json.py` → 반복. 측정 중엔 **유니티 창을 앞에** (뒤에 있으면 실시간보다 느림, 에디터 Interaction Mode 는 효과 없음).
```
NKK.BalanceProbe.Calib = true; NKK.BalanceProbe.UseUlt = true; NKK.BalanceProbe.UseRush = true;
int[][] q = { new[]{1,1,2}, new[]{2,2,4}, new[]{3,4,7}, new[]{4,7,10}, new[]{5,10,13}, new[]{6,13,16}, new[]{7,16,20}, new[]{8,20,25} };
for (int rep = 0; rep < 2; rep++) foreach (var a in q) NKK.BalanceProbe.Queue.Add(new float[]{ a[0], a[1], a[2], 600, 20, -1, -1, 2 });
NKK.BalanceProbe.RunQueue();   // 끝나면 NKK.BalanceProbe.RowsCsv() / Report()
```
- 필살기: 측정 쥐의 조각 레벨은 일반·레어 종에만 → Lv 7 필살기 해금 종이 거의 없음 (필살기 없이 잰 값). 실제로 필살기가 열리면 더 빨라짐.
- 26~50층은 SPU 미측정(10). 초반 층 벽이 매우 두꺼워졌으니 실제 플레이(전투력이 적정보다 낮을 때 벽 피해 감산)로도 한번 확인할 것.


### 9-9. 찍찍!! 훈장 · 업적 · 쥐 스킬 아이콘 · 쥐별 스킬 트리 · 글자 정리 (2026-10-08)
- **시작 쥐 노드** (사용자 결정): 공용 스킬 Start_Rat = 마릿수만 더함, 등급은 티어 시작 쥐처럼 지금 훈장의 등급 확률(`RatManager.RollSpecies`) — `CommonSkill.StartRatAdd`. 마릿수도 줄임 (훈장별 합 3·4·2·6·2·7·3·3, `gen_skill_tree.py` G1·G3·G8). 로비 출동 멤버 수에도 더함.
- **쥐 스킬 아이콘 158개** (`UnityResources/Rats/RatSkillIcons/`, README): 10-06 에 만들어 두고 안 쓰던 Codex 시트 재사용 + 너무 비슷한 22개 다시 그림. 쥐 캐릭터 테이블 Skill `skill_asset` = `RatSkillIcons/rs_<id>`.
- **성장 노드 아이콘**은 새로 안 그림 (사용자: 같은 효과면 재사용): 쥐 성장 테이블 Growth_Node 새 칸 `node_icon`(@action·@passive·@ult = 그 쥐의 아이콘 / 공용 스킬 아이콘) · `node_desc`(화면 설명, {v1}·{p1}) · `pos_x`·`pos_y`(트리 창 칸).
- `Lobby/IconBook.cs` (Lobby 씬 `IconBook`): 아이콘 모음 (RatSkillIcons·SkillIcons·UltIcons·AchvIcons), 컴포넌트 메뉴 Fill Icons. `Skill/Ult/Node/Get`.
- **쳇바퀴 상세 카드**: 특수 능력·특수 액션·필살기 카드와 "다음 훈련으로 배우는 것" 칩에 아이콘. **스킬 트리 보기** 버튼 → `Lobby/RatTreePopup.cs` (`Page_rats/TreePopup`): 성장 노드 트리(끈·아이콘·지금/최대·다음 훈련 Lv), 노드 누르면 오른쪽에 설명 + 그 쥐의 스킬 설명 + 오르는 훈련 Lv 목록. 해당 없는 노드(필살기 없는 종의 필살기 해금)는 흐리게. ESC = 창 먼저 닫기.
- **찍찍!! 훈장 페이지** `Lobby/RankPage.cs` (`Page_rank`, 웹 renderRank): 위 = 훈장 사다리 8칸(지금·자물쇠) / 왼쪽 = 고른 훈장 배지·상태·효과(시작 쥐·윗등급 배율, 지금 → 그 훈장)·승급 조건 막대·훈장 달기 / 가운데 = 그 훈장에 오는 친구들(안 만난 친구는 그림자 + ???) / 오른쪽 = **업적**. 팻말 알림 점 = `CanRankUp`.
- **업적** (사용자: 나중에 확장 → '업적' 으로): 새 `업적 테이블`(Achievement: achv_id·이름·설명·cond_type·target_id·need·achv_icon·정렬, Achv_Cond_Type). 지금은 필살기 33종 완주 = `Ult_Use`(Progress 의 필살기 기록). `Progress.AchvProgress/AchvDone`. 아이콘 33개 = 같은 메달 틀 + 상징 (`UnityResources/Rats/AchvIcons/`, README). 게임 중 업적 토스트는 아직 필살기 테이블 ult_achv 를 씀 (이름은 같음) — 조건 타입을 늘릴 때 토스트도 업적 테이블로 옮길 것.
- **글자 정리**: UI 조각 그림의 음영(카드 아래 18px 띠 · 판자 아래 13px 띠와 양쪽 못 · 타일 아래 36px 띠 · 팻말 위 끈) 위에 글이 겹치던 것을 앞면 안으로 옮김 (작전 회의 층 타일·정보 줄·기록 칸, 쳇바퀴 카드·필터·성장 길·능력 카드, 훈장 효과 줄·업적 줄·친구 칸, 트리 노드). 판자 버튼 라벨은 전부 아래 음영·양쪽 못 여백 + 한 줄 자동 크기. 넘치던 글(카드 Lv·이름·능력 제목·출동 멤버 설명·치즈 창고 상세 이름)은 칸 높이/자동 크기. 점검은 Play 중 TMP textBounds 를 그림 앞면과 비교하는 스크립트로 함 (Badge 숫자 점은 일부러 밖).

### 9-10. 2026-10-08 오후 — 업적 알림·스위치 · 고양이 보스 그림 · 고양이 다리 입체 · 시작 쥐 · 계단 거리 (일부 미커밋)
- **업적 알림** `Core/AchievementToast.cs` (Game 씬 `HUD/AchievementToast` → 패널 `HUD/UltAchievement`, 메달 아이콘 `Icon` 추가, 위치 y -215): 업적 테이블 기준. UltimateManager 의 예전 업적 패널 코드는 삭제, 필살기 끝나면 `Progress.OnUltUsed(ultId)`.
- **업적 스위치** (사용자 결정): 저장 파일마다 `SaveData.achvOn`(달성한 업적 id). `Progress.CheckAchv` 는 스위치가 꺼져 있고 조건(AchvProgress ≥ need)을 넘을 때만 켜고 `AchvGot(row)` 한 번. `AchvDone` = 스위치. 다시 알림 없음.
- **고양이 보스 전용 그림** 20 `boss_zero` · 25 `boss_witch` · 30 `boss_commander` (`UnityResources/Rats/Cats/Sheets/boss_cats/` README, 새 도구 `Tools/slice_cat_parts.py --boss` = 자홍 테두리 제거 + 다리를 몸통 중간 높이에). 스테이지 테이블 Boss code_id 교체, CatArtLibrary 다시 빌드함. 리그 미리보기 `boss_cats/_rig_preview.png`.
- **고양이 다리 입체** (사용자 결정): 가까운 다리 = 몸통 앞, 먼 다리만 뒤 → `Cat.cs`·`Boss.cs`·`UltThankYou.cs` 의 `rig.Build(..., legsBehind: false)`. **코드만 고침, 아직 컴파일·Play 확인 안 함** (측정 Play 중이라). 다음 작업자: 컴파일 후 일반 고양이 10종·보스 3종 모습 확인.
- **시작 쥐**: 티어 테이블 start_rat_count 6·7·8·8·9·10·11·12, 공용 스킬 Start_Rat = 각 훈장 G1 +1 · 짝수 훈장 G3 +1 (합 12), 빠진 자리(G3 홀수 훈장·G8)는 Breed_Chance +5%. 노드 마릿수는 등급 무관, 훈장 확률로 뽑음.
- **최대 마리 수**: 기본 popCap 30 + Pop_Cap 노드 합 145 = 175.
- **계단 거리 상한** (사용자 결정): `StageManager.stairsMaxDist` 4 = 계단 방은 거리 4 이내 중 가장 먼 방 (긴 복도 끝 계단은 쥐가 안 모여 벽을 얇게 해도 170~250% 걸렸음). `gen_stage_table.py STAIRS_MAX_DIST` 와 같게.

**층 밸런스 측정 — 다음 작업자가 이어서 (목표: 전투력 = 적정일 때 180초 · 보스 층 210초의 70~80%)**
- 라운드 4 (계단 총공격 수정 후, 상한·시작 쥐 변경 전): 중앙값 68%, 61번 중 25번이 60~90%. 결과 `Tools/probe_results/` 에 없으면 HANDOFF §9-8 표 참고.
- 그 결과로 `gen_stage_table.py` SPU 갱신(지형이 안 바뀐 층만 `SPU × 측정%/75`, 0.6~1.6배 제한), 계단 거리 상한으로 지형이 바뀐 층(8·11·13·15·17·18·21·25·26·28·30)은 SPU 빠짐(기본 10). 테이블 재생성 완료.
- 라운드 5 를 돌리던 중 중단 (시작 쥐·계단 상한·새 SPU 반영 상태). **할 일**: ① 유니티 컴파일 (고양이 다리 코드) ② Play 후 §9-8 의 큐 코드로 측정 2회 ③ 층별 중앙값으로 SPU 갱신 → `python Tools/gen_stage_table.py && python Tools/xlsx2json.py` ④ 다시 측정, 70~80% 될 때까지 반복 ⑤ 커밋.
- 측정 주의: 유니티 창을 앞에 (뒤면 느림), 측정 중 스크립트 수정 금지, BalanceProbe 는 화면을 안 그려서 측정 중엔 게임 캡처 불가.
- 초반 층(1~4)·16·22·24층은 벽을 크게 늘려도 시간이 잘 안 늘어남 (번식으로 전투력이 커짐) — SPU 갱신이 수렴 안 하면 그 층들은 제한시간 대신 목표를 조정할지 사용자에게 물을 것.

### 9-11. 2026-10-08 — 측정 라운드 5·6 · 보스 층 허들 · 저장값 초기화 버튼
- **오류 원인 기록**: 측정 Play 중에 유니티 창에 포커스가 가면 밀려 있던 스크립트 변경이 자동 컴파일 → 도메인 리로드 → BalanceProbe 상태가 날아가고 `RatManager.GradeOpen` NullReference · `Rat.StunStars` IndexOutOfRange 대량 발생. **측정 전엔 꼭 컴파일 끝난 상태에서 Play 시작.**
- **허용 범위** (사용자 결정): 일반 층은 **60~90% 면 OK** (83~88% 도 괜찮음) → 그 범위 밖인 층만 75% 쪽으로 SPU 보정.
- **보스 층 = 허들** (사용자 결정): 210초의 **약 90%(189초)** 로 아슬아슬하게. 길 뚫기 117초(일반 층과 비슷) + **보스전 72초** (`gen_stage_table.py BOSS_TOTAL_SEC 189 · BOSS_FIGHT_SEC 72`). 보스전 시간은 Boss 시트 `hp_pow_sec` 로 맞춤.
- **적정 찍찍(적정 전투력) 값은 시간에 맞춰 바꿔도 됨** (사용자 2026-10-08). 단 Calib 측정에선 적정 = 무리 전투력으로 덮어써서(화면 값이 계속 바뀌는 이유) 영향 없음 → 실제 전투력 측정(§9-6 다음 할 일 2번) 때 POW0·POW_GROW·층별 보정으로 조정.
- 새 도구 `Tools/update_spu.py <csv...> [--write]`: BalanceProbe `RowsCsv()` 결과 → 일반 층 SPU, 보스 층은 길 뚫기(SPU)·보스전(hp_pow_sec) 따로 보정. 결과 파일은 `Tools/probe_results/roundN.csv`.
- 라운드 6 (`round6.csv`) 뒤: SPU 안 바뀐 층은 라운드 5 와 합쳐(`round5_6_merged.csv`) 판단 — 92번 중 49번 목표 범위, 일반 층 대부분 60~90%. 남은 층 11·12·17·18·21 · 보스 층. hp_pow_sec 5층 33.4 · 15층 70.7. 층당 회차 편차 ±50% 라 2회로는 흔들림 → 4회 이상 합쳐 볼 것.
- 라운드 5 (`round5.csv`, 65번): 목표 범위 29번. 느림 9·11·15(보스 145%)·17층, 빠름 12·13·14·16·21·23층, 25층 보스 50%. 보스전 34~55초 (목표 72) → hp_pow_sec 5층 26.3 · 10층 56 · 15층 57.8 · 20층 47.3 · 25층 71.6 (30층 36 그대로, 미측정). 테이블 재생성 완료 → 라운드 6 측정.
- **윈드밀 버그 수정**: 몸을 뒤집은(sy -1) 채 회전축을 그대로 둬서 몸이 축에서 떨어져 큰 원(바퀴)을 그리며 돌았음 → 웹처럼 몸 중심 축 (`Rat.Trick.cs TrickTransform`, 축 높이 0.6hh·발 축 위 0.5hh) + 칠 때마다 흰 고리. 대포알도 축 보정.
- **새 공용 묘기 5 "쳇바퀴 돌기"** (사용자: 바퀴처럼 도는 게 웃겨서 전용으로): 예전 윈드밀 버그 모습(뒤집혀 큰 원, 5바퀴) + 진행 방향으로 굴러가며 0.12초마다 반경 50 · 공격력 0.6배 (`TrickType.Wheel`, 속도 `RatManager.wheelTrickSpeed` 260, 1.6초). 해금 = **5훈장 K1** `Trick_Unlock 5` (예전 모든 묘기 +1%), 확률 = 6훈장 K7 · 8훈장 K3 `Trick_Chance 5 +2%` (`gen_skill_tree.py` TRICK 5). 아이콘 `cs_wheelspin` (기존 그림 합성, 로비 SkillPage iconSprites 에 추가). 팝업 글 3종은 다른 묘기처럼 `Rat.Trick.cs Tricks` 표.
- **테스트 패널 "저장값 초기화"** 버튼 (`HUD/TestPanel/ResetButton`, `UltTestPanel.resetButton`): `Progress.ResetAll()` 후 저장 없이 Lobby 로 (GameManager 를 꺼서 5초 저장 막음). Play 확인 완료.

- 라운드 7 (`round7.csv`, 합친 판단 `round5_7_merged.csv`): 130번 중 70번 목표 범위. 11층 SPU 41 · 12층 13(편차 커서 직접) · 13층 4.76 · 15층 32.4 · 25층 3.08, 보스 hp_pow_sec 20층 56.1 · 25층 53.9. **층 측정은 사용자가 성장 곡선을 준 뒤 다시** (2026-10-08 사용자: 측정보다 수정 먼저).
- **윈드밀 회전축 2차**: 높이 어림(pivotH)으로는 종마다 몸 비율이 달라 축이 어긋남 → `RatRig.Apply(..., bodyPivot)` = 몸통 스프라이트 중심을 축으로, 몸통 반 높이 + lift 2 에 놓음 (Rat.LateUpdate 가 윈드밀일 때 켬).
- **다른 PC 클론 문제** (사용자: "멀티 플레이어로 열림"): Unity 6 템플릿 패키지 `com.unity.multiplayer.center` 가 처음 열 때 Multiplayer Center 창을 띄움 → manifest 에서 삭제. unity-mcp 는 `#main`(계속 바뀜) → `#v10.3.0` 태그로 고정 (Library 의 지문 da7b9ae… 은 커밋 해시가 아님, 그걸로 고정하면 패키지 해석 실패). `.gitattributes` (유니티 YAML LF 고정 · 바이너리). `Assets/Editor/OpenLobbyOnStart.cs` = 처음 켤 때 빈 씬/SampleScene 이면 Lobby 열기. 남은 템플릿 잔여물(TutorialInfo · Readme.asset · SampleScene)은 삭제 여부 사용자 답 대기.
- **스킬 노드 획득/미획득 구분** (사용자: 구별이 힘듦): 획득 = 뒤 금빛 고리 `Glow`(돌며 깜빡) + 반짝이 배지 `Badge` + 원래 색 아이콘 + 진한 이름 / 열림 = 판·아이콘 흐리게 / 잠김 = 더 어둡게. 색·속도는 SkillPage 인스펙터 "노드 상태 구분". Glow·Badge 는 노드·핵심 노드 템플릿 자식 (씬).

- **판마다 랜덤 지형** (사용자 결정: 배치만 랜덤 — 어느 벽을 뚫을지가 곧 길 선택): `StageManager.RunSeed`(GameManager.Awake 에서 판마다 새로) · `FloorSeed(f)` → 방 배치·계단·가구 배치. 인스펙터 `randomLayout` 끄면 예전 층 번호 시드. 벽 체력은 **생성된 길 기준 자동 계산**: 스테이지 테이블 새 칸 `wall_path`(경로 벽 배율 합 목표) 를 계단 거리 d 로 나눔 (일반 = max(0.8, 합×0.5÷(d-1)), 계단 = 나머지, `stairsShare`·`minNormalWall`). `gen_stage_table.py` 는 SPU 를 앞뒤 2층 중앙값(`spu_smooth`)으로 매끄럽게 해서 wall_path 계산 (층별 SPU 는 그 층 지형 하나에 맞춘 값이라). wall_stairs·wall_normal 은 wall_path 0 일 때만. **다음 측정은 랜덤 지형이라 층당 4회 이상** 재고, SPU 갱신도 층별이 아니라 구간 평균으로 볼 것.
- **쏟아짐** (사용자: 큰 물건도 부서질 때 쏟아지게 + 물건에 맞는 새 물체, 금고 = 돈): 새 물건 16종 (60033~60048, 분류 **Spill** = 평소 생성 안 됨, `ItemRow.IsSpill`, 부술 수 있고 치즈 줌). 그림 Codex `UnityResources/Rats/Items_Spill/` (README 에 어디서 쏟아지는지 표). 큰 물건 8종 + 가구 전부에 drop_01~04 지정 (원본 백업 `Data_Table/_backup_20261008/`). `ItemManager.OnSmashed` 는 가구만이 아니라 drop 있는 물건 전부 쏟음. 물건 그림 = ItemManager 메뉴 Fill Item Sprites (Game 씬 반영함).

- **인간형 보스 3종 몸통·팔·다리 옆모습으로 새로** (사용자: 리깅 이상 — 정면 몸통 + 옆모습 머리, 팔이 몸통에 묻힘): `UnityResources/Rats/Humans/Sheets/boss_v4/` (README). 목살을 머리 목 폭·피부색에 맞춤. `HumanArtLibrary.Entry.front`(정면 몸통이면 팔 양옆·다리 벌림·목 가운데) 코드는 남겨 둠 — pivots.json 에 "front": 1 일 때만 (지금은 전부 0).
- **보스는 계단 방에서 대기** (2026-10-08 수정 2번째 — 사용자: 5층에서 보스가 안 나옴. 실제론 대기 중이었지만 멀리 있는 계단 방이 안 그려져 숨겨 둠): 보스 층은 계단 방을 처음부터 어둡게 그림(`StageManager.BossWaitRoom`) + 보스는 그 안에 실루엣(`Boss.waitDarkTint`) → 계단 방이 열리면 원래 색 + 전투. 보스를 잡아야 계단 사용(`Boss.Blocking`). 필살기는 버튼을 눌러야 발동 (자동 = 6훈장 Ult_Auto 노드 · 측정 ForceAuto 뿐, Play 로 확인).

- **보스 v5**: 경비대장 머리 3종·보스 팔 3종 다시 (목 뒤 튀어나옴·팔 단면) — `Humans/Sheets/boss_v5/` README. 머리 크기 `head_scale`(pivots.json headScale → HumanRigMeta → HumanArtLibrary.Entry.headScale): 경비대장 0.775 · 연구원·연구소장 0.85.
- **로켓 배송**: 상자가 막힌 벽 두께 안에 떨어지면 물건이 벽에 가려짐 → 착지 위치를 방 바닥 안쪽으로.
- **총공격 범위** 공용 효과 `Rush_Range` (1·3·5·7훈장 S3, +15/15/20/20%): 화면 밖으로 화면 크기 × 합 만큼 쥐도 모임 (`RatManager.StartRush`). 자리를 내준 스테이지 스킵은 같은 트리 S1 에 더해 합 유지.
- **쥐 벽 넘어감** (사용자: 벽 통과하는 스킬들): 측정 중 감지(`Rat.WallEscapeCheck`, 에디터 전용 · BalanceProbe 결과에 `[벽 밖]`) 56건 → 거의 전부 총공격 등으로 몰려 쥐끼리 미는 힘이 Confine 뒤에 적용돼 벽 안으로 밀린 것 (+ 벽 틈 질주·윈드밀 각 1). **`Rat.KeepInside`** = LateUpdate 에서 한 번 더 Confine, 닫힌 방이면 마지막 안전 위치로.
- **벽 파괴 연출**: 금 자국 3단계 (`Room.crackTemplate/crackSprites/crackAt`) + **`Stage/WallFx.cs`** (Game 씬 `WallFx`): 벽 선 따라 연쇄 폭발(explosion 애니) → 벽돌·콘크리트가 새로 열리는 방 쪽으로 부채꼴로 쏟아짐(큰 덩어리 slabCount) · 먼지 밀려 나감 · 충격파 · 역경직 · 흔들림 · "콰광!!". v2 (사용자 참고 사진: 폭발 + 방사형 잔해 줄기 + 바깥으로 뿜는 연기): 잔해 발사 속도 1300·낮게·부채꼴 0.75, 빠른 잔해 뒤 흙먼지 꼬리(trail*), 연기 기둥 smokeCount 6 이 열리는 방 쪽으로 뿜어져 부풀어 오름(smoke*). `StageManager.BreakFx`. 그림 `UnityResources/Rats/FX_Wall/`.
- **경제 개편** (사용자: 치즈가 남아돎 → 연구자료 부족 즈음 치즈도 바닥, 훈장마다 1훈장 약 2판 → 8훈장 약 6판, 성장 곡선은 나중에 다시): 측정 `Tools/probe_results/round8_econ.csv`(층별 치즈·연구자료, BalanceProbe Rows 에 cheese·research 칸 추가). 연구자료 = `Heist` 8 × 1.25^(층-1) (예전 6 × 1.45, 보스 ×3). 노드 연구자료 `gen_skill_tree.py RESEARCH`, 승급 티어 테이블 research_cost 36·40·140·460·940·2460·8650, 노드 치즈 = 노드 연구자료 × `CHEESE_PER_RES`(층대 치즈÷연구자료 수입 × (트리+승급)÷트리), 1훈장 트리 치즈 합 21,300. 예전: 7·8훈장은 한 판도 안 돌고 넘어감(0.24·0.03판).

### 9-12. 2026-10-08 — 보스 건너뜀 버그 · 미니맵 · 쥐 마릿수 · 보스바/시간 게이지 그림
- **보스 건너뜀 버그** (사용자: 보스전 없이 바로 다음 층, 한 번 깬 보스가 다시 들어가면 안 나옴): 로비→재입장 흐름도 보스는 정상 대기였음. 재현된 건 층 이동 예약(탈취 연출 2.2초 → 페이드 → `EnterFloor(Game.Floor + 1)`)이 늦게 오면 그때 층 기준으로 +1 해서 **4층 → 6층** 으로 한 층 더 넘어가는 것 + 테스트 "층 클리어" 버튼이 보스를 무시하고 넘어감. 수정:
  - `StageManager.Climb`: 보스 살아 있으면 거부, 예약된 층(from+1)에만 들어감 (`Game.Floor == from` 일 때만).
  - 안전장치 `Boss.DefeatedThisFloor`·`FloorCleared(f)`: 보스 층에서 이 층 보스를 안 잡았으면 어떤 경로로도 계단 못 씀. 계단 방이 열렸는데 보스가 Off/Dead 면 `Boss.OnStairsOpened()` 가 다시 불러 전투 (그림이 없어 못 부르면 에러 로그 + 통과 허용).
  - 테스트 "층 클리어" = `StageManager.TestClear()`: 보스가 기다리면 계단 방까지 벽을 열어 보스전 시작, 아니면 다음 층.
  - 로그: `[Boss] N층 보스 … → Wait` · `보스전 시작` · `격파 · 전투 N초` · `계단 막음` · `다시 불러옴` → 다시 생기면 Editor.log 에서 `[Boss]` 검색.
- **보스 미리보기**: 보스 층 진입 0.7초 뒤 카메라가 계단 방의 보스 실루엣을 잠깐 비추고 돌아옴 (`CameraController.Peek` peekIn/Hold/Out, `Boss.peekOnEnter/peekDelay`, 측정 중엔 안 함).
- **미니맵** `Stage/Minimap.cs` (Game 씬 `HUD/Minimap`, 오른쪽 아래, 웹 drawMinimap): 열린 방(ui_tile × 구간 색) · 부술 수 있는 옆방(ui_tile_lock 어둡게) · 보스 층이면 안 열린 계단 방도 붉게 + 보스 아이콘(전투 중엔 보스 위치) · 계단 아이콘 · 쥐 점(최대 200) · 카메라 테두리. 칸·점·아이콘은 `Area` 자식 틀(인스펙터에서 크기·색). 제목 `{floor}층 · {zone}`.
- **쥐 마릿수** `Rats/PopCounter.cs` (`HUD/PopCount`, 제한시간 게이지 아래): `쥐 {n} / 최대 {max}` (RealCount / PopCap), 최대면 주황, 바뀌면 통통.
- **보스 체력바·제한시간 게이지 그림** (Codex `UnityResources/Rats/HUD_Gauge/` README): 보스바 = 엠블럼 + 틀 + 홈(`Groove`) 안 채움(줄무늬, 보스 색) + 깎인 자국(`Trail`, `Boss.barTrail` trailDelay/Speed) + 맞을 때 번쩍·엠블럼 흔들림. 제한시간 = 초시계 아이콘 + 나무 틀 + 홈 안 채움.
- 아직: 이전 요청 "일정 층수 이상 올라가면 사람들이 안 나옴" 은 조사만 (층 진입 땐 21층도 2명 나옴, 보충 humanRefill · HumanCap 확인 필요).

### 9-13. 2026-10-08 — 고양이 무리 스킬 10종 · 고양이 안 떠남 · 사람 보충
- **고양이는 체력 0 이 될 때까지 안 사라짐** (사용자): life_time 퇴장 삭제 (고양이 테이블 칸은 "체류 시간 (안 씀)").
- **무리 스킬** (사용자: 쥐가 많은 곳을 확실히 인식해 범위 기절로 방해 · 고양이마다 다르게 · 빨리): 고양이 테이블 Character 새 칸 `crowd_skill` → Skill 50011~50020, Effect_Type `Crowd_*` (value_01 범위 · 02 기절 · 03 웅크림 · 04~06 효과별, 쿨타임 = cond1 Interval). 흐름: 쥐가 가장 많이 모인 곳(`CatManager.FindCrowd`, 반경 crowdScanRadius 안 마릿수) → 바닥 빨간 경고 원(채움이 차오름 + 테두리 깜빡) + 머리 위 느낌표 → 발동.
  - 코숏 냥냥 대폭격(도약 내려찍기) · 러시안 블루 냥펀치 연타(작은 내려찍기 3연속, 매번 새 무리) · 턱시도 급소 찌르기(좁고 빠름, 3.2초 기절) · 페르시안 뱃살 프레스(반경 300, 물건 날림) · 먼치킨 식빵 볼링(굴러가며 길 위 기절 + 끝에서 쿵) · 벵갈 그림자 습격(사라졌다 무리 한가운데) · 스핑크스 레이저 폭격(제자리, 2곳 하늘 레이저) · 메인쿤 지진 내려찍기(충격파 3겹) · 마녀 운석 마법(3곳) · 우주복 블랙홀(빨아들인 뒤 띄워 기절).
  - 공통 수치는 CatManager 인스펙터 "무리 스킬 공통"(첫 발동 1.2초 · 달리기 400 · 도약 거리 650 · 원거리 사거리 1100 · 팝업 `{skill}!! {n}마리 기절` · 고양이 말 · 등장 배너 `{crowd}`), 경고 그림은 `CatManager/CatWarn` 자식 (README `UnityResources/Rats/FX_CatWarn/`). `logCrowd` 켜면 발동이 콘솔에 남음. 기존 품종 스킬(하악질 등)은 그대로 사이사이에.
- **특별 고양이 vs 고양이 보스** (사용자 결정 2026-10-08: 지금처럼 둘 다 둠): 마녀 모자·우주복 고양이(25층부터 25%, 무리 스킬 운석·블랙홀)와 25층 보스 대마법사 마녀 고양이·30층 우주 고양이 사령관(Boss 시트, 불덩이·무중력)은 별개. 보스 층에서 보스 대기 중엔 특별 고양이도 나올 수 있음.
- **사람 안 나옴**: 측정(티어 4, 1~8층) 결과 층마다 평균 2~4명은 나옴. 높은 층은 층을 15~25초에 깨서 보충(18~30초)이 한 번도 안 돌아 시작 2명만 잠깐 보였음 → 걷거나 도망치는 사람이 최소 수(2 + 층÷6, 상한 이하)보다 적으면 2.5~4.5초 빠른 보충 (`ItemManager.humanMin*`, `humanQuickRefill`). BalanceProbe 층 줄에 `사람 등장 · 평균 · 최대`.
- 측정 주의: 이번 측정은 사용자 요청으로 8층에서 중단 (Unity 점유). 높은 층(10층+) 사람 수는 아직 측정 안 함.

### 9-14. 2026-10-08 — 훈장 조건 = 트리 전부 · 번식 하트 · 보스 필살 패턴 12종 · 층 길 스크롤
- **훈장 승급 조건** (사용자): 스킬 개수 대신 **지금 훈장까지 공용 스킬 트리 노드 전부** (`Skill_Tree_Complete`, 티어 테이블 cond3, 값은 안 씀). `Progress.CondNeed` 가 트리에서 개수 자동 계산 → 찍찍!! 훈장 페이지·스킬 페이지 승급 칸 `{have}/{need}`. BalanceProbe 는 이 조건이면 트리 전부 찍고 측정.
- **번식 하트** (Codex `UnityResources/Rats/FX_Heart/`): 번식할 때 화면 안이면 `FxManager.Hearts` 4개 (RatManager.birthHearts) — 통 튀어나와 흔들리며 솟고 뒤 절반에 사라짐. 크기·속도·수명은 FxManager 인스펙터 "하트".
- **보스 필살 패턴** (사용자: 보스전 더 재밌게, 뇌절 컨셉): 스테이지 테이블 Boss 새 칸 `special1_type`(체력 70%) · `special2_type`(35%) · `special_chance`(둘 다 쓴 뒤 공격마다 확률), Atk_Type 12행 + 새 칸 `atk_name`(배너 이름), Boss_Line 상황 `Special` 대사. 발동 = 배너 + 대사 + 번쩍. 경고 원·낙하물·블랙홀은 CatManager 것을 같이 씀.
  - 경비대장: 3단 공중 내려찍기 / 안전 고깔 폭격 · 수석 연구원: 약품 대방출 파티(나선 투척 14개) / 자가 실험 거대화(6초 1.6배·빨라짐·충격파) · 연구소장: 결재 서류 폭탄 / 긴급 이사회 소집(충격파 + 경비원 6) · 메인쿤: 새벽 3시 우다다(질주 5번) / 상자 들어가기(무적 → 폭발) · 마녀: 운석 소나기 / 개구리 저주(머리 위 개구리) · 우주 사령관: 궤도 레이저 포격 / 초거대 블랙홀.
  - 소품 그림 Codex `UnityResources/Rats/BossProps/` (고깔·서류·상자·개구리·약병·화남 표시). Boss 인스펙터 "필살 패턴" (specialProp · markTemplate = 씬 루트 `BossSpecialProp` · `BossFrogMark`).
- **작전 회의 층 길 스크롤** (사용자: 층 타일이 칸을 넘어 다른 UI 뒤로): `PlanCard/FloorPath` = ScrollRect(가로) + Viewport(RectMask2D) + Content(HorizontalLayoutGroup + ContentSizeFitter) + Scrollbar(ui_bar_bg/yellow). `RunPage.tileScroll`, 고른 층이 안 보이면 그쪽으로 옮김. 휠·끌기 됨.

**남은 일 (순서 제안)**
1. 중·후반 측정: 티어 T 마다 `Run(T, 그 티어 Max_Floor, 다음 티어 Max_Floor+1, 450, 20, 다음 티어 Skill_Node_Count, 다음 티어 Shard_Level_Sum)`. 목표: 다음 훈장 조건 층(티어 테이블 Max_Floor)은 제한시간의 60~80% 로 통과, 그 다음 층은 빠듯하거나 실패.
2. 결과로 `gen_stage_table.py` 곡선(POW_GROW·HP_GROW·벽 배율·방 수)과 `gen_skill_tree.py` 값·비용(치즈 수입 대비 훈장 트리 1개 ≈ 판 3~5번), 보스 `hp_pow_sec`(지금 40), 티어 테이블 조건(연구자료·Skill_Node_Count 15/40/70/100/130/160/195) 조정 → 다시 생성·xlsx2json·측정.
3. 10·15층 보스 등 나머지 §6.

### 9-15. 2026-10-08 — 로비 훈장 배지 위치 · 하트 크기
- 아지트 훈장 배지 8개(`Home/Badges`)가 "쳇바퀴 훈련" 팻말 뒤에 가려짐 → 윗쪽 들보(찍찍!! 훈장 팻말 오른쪽, 화면 y≈112)로 한 줄 이동, 1.25배. 못 단 훈장 색 = `LobbyHome.lockedBadgeColor` (더 진하게).
- 번식 하트 크기 26~38 · 솟는 속도 130~210 · 시작 높이 55 (쥐 몸에 가려지던 것). 캡처 `Captures/` (git 제외).

### 9-16. 2026-10-08 — 다음 작업 기록
- 사용자: 보스 테이블을 따로 만들고(스테이지 테이블에서 분리), 일반 공격이 하나뿐인 인간형 보스 3명에게 두 번째 일반 공격 추가 → 다른 에이전트가 진행. 작업 내용은 HANDOFF.md §7 0번.

### 9-17. 2026-10-08 — 보스 테이블 분리 · 두 번째 일반 공격 · 쿨타임 스킬(밈) 6종
- **보스 테이블 분리**: `Data_Table/보스 테이블.xlsx` (JSON `BossTable`) = Boss · Boss_Line · Atk_Type · Situation_Type · Column_Desc(보스 칸). 스테이지 테이블은 Stage · Column_Desc 만. 백업 `_backup_20261008/스테이지 테이블_보스분리전.xlsx`.
  - `TableRows.cs` `BossTableFile` · `GameDatabase.bossTable` (Game·Lobby 두 씬 연결) · `xlsx2json.py NAMES` · `update_spu.py` (hp_pow_sec 은 보스 테이블) · `gen_stage_table.py` (스타일을 Stage 시트에서 복사).
- **두 번째 일반 공격** (atk2_chance 0.4): 경비대장 `Baton` 진압봉(앞쪽 반원 날림) · 수석 연구원 `Gas_Cloud` 독가스 구름(플라스크 2개 → 5초 구름, 안에 0.5초 머문 쥐 기절) · 연구소장 `Briefcase` 서류 가방(쥐 무리에 던짐, 물건 날림·서류 흩날림).
- **쿨타임 스킬** (사용자: 밈 넣기, 2020년 뒤 유명한 밈으로 골라 질문해서 결정): Boss 새 칸 `skill_type · skill_cd(12) · skill_first(6)`, Atk_Type `[스킬]` 6행, Boss_Line 상황 `Skill` 대사(+ 새 두 번째 공격 Attack 대사). 필살 패턴과 따로 돎 (필살 대기 중이면 필살 먼저).
  - 5 경비대장 = **퉁퉁퉁 사후르**(2025 이탈리안 브레인롯): 진압봉 퉁 ×3 (충격파 커짐, 짧은 기절) → 사후르!! 큰 기절.
  - 10 수석 연구원 = **이븐하게 익혀드릴게요**(2024 흑백요리사): 토치 → 쥐 무리 2곳에 지글지글 불판 6초, 안에 1초 머문 쥐 기절.
  - 15 연구소장 = **중꺾마**(2022): 금빛 오라 폭발(주변 밀어냄) + 6초 받는 피해 ×0.4 · 넉백 없음 · 금빛 깜빡.
  - 20 메인쿤 = **해피해피해피**(2023 고양이 밈): 통통 점프 6번 쥐 쪽으로, 착지마다 작은 충격파.
  - 25 마녀 = **무한동력 버터 고양이**(사용자 지정: 잼 바른 식빵 + 고양이 = 무한동력): 잼 식빵 붙인 고양이 4마리가 가로축으로 빙글빙글(세로 뒤집힘) 떠서 쥐 무리 쪽으로 날아감, 열린 방 안에서 튕김, 4초. 닿은 쥐 기절.
  - 30 우주 사령관 = **맥스웰 고양이 낙하**(2023): 경고 원 6곳 → 빙글 도는 맥스웰이 떨어짐 (CatManager.AddStrike 재사용).
  - 사용자가 고른 기준: 무명·최신 소소한 밈은 거절, 누구나 아는 밈 선호. 처음 제안(무궁화·호루라기·논문 리젝·라떼는 등)은 "별로".
- 그림 Codex `UnityResources/Rats/BossProps/` (`Sheets/boss_skill.png` 3×2): 진압봉 · 서류 가방 · 토치 · 버터 고양이 · 맥스웰 · 불판 자국. 독가스 = 기존 `FX/Color/toxic`. Boss 인스펙터 "두 번째 공격 · 쿨타임 스킬" (그림·크기·팝업 글·노출 시간·중꺾마 배율·버터 고양이 속도/회전).
- 확인: Play 에서 테스트 보스 6명 차례로 불러 스킬·두 번째 공격 강제 발동 → 모두 동작 (불판·가스 구역 기절, 중꺾마 피해 100→40, 서류 가방 10마리 기절, 점프 6번, 버터 고양이 4마리, 맥스웰 6개), 콘솔 에러 없음. 실제 보스 층에서 보스전 시간이 늘었을 수 있음 → 다음 측정 때 같이.

### 9-18. 2026-10-08 — 진압봉 손에 쥐기 · 3단 점프 무한 반복 · 맥스웰 춤 · 발악 패턴 밈 12종 · STAY 컷씬 · 테스트 패널 보스 고르기
- **진압봉·토치·서류 가방을 손에 쥠** (사용자: 막대기 이상하게 듦): `HumanRig.Hand(out dir)` = 가까운 팔 끝 위치·방향, `Boss.ShowHeld` → `PlaceHeld` (진압봉은 손잡이를 손에, 팔 방향으로 뻗음, `batonArtAngle`·`batonGrip`).
- **3단 점프 무한 반복 수정**: 착지 때 일반 착지(`Land`)가 air 를 꺼서 Mega_Stomp 가 같은 목표로 계속 뜀 → `Land` 는 Stomp/Pounce 만.
- **맥스웰 착지 뒤 춤** (사용자가 크로마키 영상 지정): 떨어지는 것 공통 시스템 `AddDrop`/`UpdateMaxwells` (kind 0 좌우 흔들흔들 걷기 · 1 제자리 빙글(세로축) · 2 빙글 돌며 원 · 3 통통 튀며 좌우 · 4 관 운구 댄스 · 5 춤 없음). 맥스웰은 0~3 돌아가며, 춤 중 닿은 쥐 기절. 인스펙터 "맥스웰".
- **발악(필살) 패턴 밈** (사용자 결정, Atk_Type id 는 그대로 · atk_name·desc·Special 대사만 밈): 5 무야호 3단 점프 / 관짝소년단(관 낙하 → 운구 댄스) · 10 약품 파티 그대로 / 오히려 좋아(머리 펑 → 거대화) · 15 전액 현금 매입(돈다발 낙하) / 긴급 비상 회의(비상 버튼 + 빨간 경보 + 경비원) · 20 한강 고양이(꽁냥이 질주 + 물보라) / 바나나 고양이(상자 대신 우는 바나나 고양이 + 눈물) · 25 트랄랄레로(운동화 상어 낙하 → 맞은 쥐 머리 = 상어) / 발레리나 카푸치나(빙글 → 쥐 머리 = 커피잔) · 30 홈랜더 눈 레이저(보스 눈에서 빔) / STAY.
  - 쥐 머리 표시는 옆모습(사용자 요청) `boss_shark_head`·`boss_cup` (머리 자리에 바라보는 방향으로, 쥐 하나에 표시 하나). `Boss.headMarkFwd/Up/Scale`.
- **STAY 컷씬** (사용자 아이디어): 초거대 블랙홀이 웅크림(3초) 동안 **모든 쥐**를 빨아들임 → `HUD/StayCutscene` (`BossCutscene`: 플래시 인 → 인터스텔라 책장 너머 과거의 자신을 찍찍!! 말리는 장면 랜덤 → 번쩍 → 세계 정지 풀림) → 쥐가 사방으로 튕겨 나옴. 장면 9개(그림·외침·자막 인스펙터): 상한 치즈 · 쥐덫 · 고양이 꼬리 · 전선 · 끈끈이 · 쥐약 · 고양이 밥그릇 · 로봇청소기 · 컴퓨터 마우스 쥐(USB 꼬리를 토스터에). BalanceProbe 중엔 컷씬 건너뜀. 그림 `UnityResources/Rats/BossCutscene/`.
- **테스트 패널**: ◀ 보스 ▶ + 보스 소환(고른 보스, 테스트 보스 싸우는 중이면 바꿔 부름) · ◀ 기술 ▶([일반]·[두 번째]·[스킬]·[발악 1/2]) + 보스 기술 발동 (`Boss.ForceAttack`, 다음 공격으로).
- 소품 그림 Codex `BossProps/Sheets/boss_meme.png`(관·돈다발·비상 버튼·바나나 고양이·상어·물보라) · `boss_meme_side.png`(옆모습 상어 머리·커피잔).

### 9-19. 2026-10-08 — 발악 패턴 밈 다듬기 (사용자 피드백)
- **관짝소년단**: 관이 떨어진 뒤 관 멘 운구단(`boss_pallbearer_a/b`, 두 포즈를 박자마다 번갈아)이 옆걸음 춤 (Boss 인스펙터 운구단: 크기·박자·속도·시간). 관 개수 10 → 4.
- **진압봉이 휘두르는 중 사라짐**: 맞히는 순간 숨기던 것 → 손에 쥔 소품은 공격이 끝날 때 내려놓음 (퉁퉁퉁도 끝까지 쥠).
- **바나나 고양이 = 소환**: 보스는 안 숨고 엉엉 울다가 우는 바나나 고양이 4마리 소환 → 가까운 쥐에게 아장아장 (눈물·엉엉) → bananaArm 초 뒤 쥐에 닿거나 시간이 다 되면 펑 (Atk_Type Box_Fit count·radius·dur).
- **꽁꽁 얼어붙은 한강**: 보스 둘레 실제 얼음 바닥(`boss_ice`, iceRadius) — 안의 쥐가 가끔 미끄러져 넘어짐 ("미끌!"), 그 위로 질주.
- **불판**: 불꽃(FX flame)·연기(FX smoke)가 피어오름 (`Puff`), 불판 위 쥐는 빨갛게 (`Rat.Burn` → RatManager.burnTint, 쥐가 남은 시간을 셈).
- **긴급 비상 회의**: 어몽어스식 긴급 회의 컷씬 (`HUD/StayCutscene/Meeting`: 배경 `meeting_bg` + 가운데 보스 화난 얼굴(사람 그림 모음 angry) 쾅 + "긴급 회의 소집!!", 플래시 인·아웃, 2초, 세계 정지) → 비상 버튼·경보·충격파·경비원.
- **트랄랄레로·발레리나**: 쥐 머리 위에 덧씌우던 표시 대신 **머리 그림 자체를 바꿈** (`Rat.SwapHead` → `RatRig.headOverride`, 옆모습 그림, 기절 시간 + 1.5초). 시간은 쥐가 직접 세서 보스가 사라져도 반드시 되돌아감. 그림 한 장짜리 쥐만 예전처럼 위에 표시.
- **멘트**: Atk_Type 새 칸 `line` (전용 멘트). 배너·스킬 이름 칸에 이름 대신 멘트 (사용자: 스킬 이름 말하지 말고 멘트를 이름 칸에) — 꽁꽁 얼어붙은 한강 위로 고양이가 걸어다닙니다 / 난 너희들보다 강하고, 똑똑하고, 더 낫다. 난 더 낫다고! / 중요한 건 꺾이지 않는 마음!!. 배너 글자 자동 크기(30~64).
- **홈랜더 눈 레이저**: 모으고 쏘는 동안 머리를 빨간 눈 버전으로 (`RatRig.headReplace`, `Boss.laserHeadSprite`), 빔은 머리 그림의 눈 픽셀(`laserEyePx` 102,139)에서 정확히 나감, 목표 쪽을 봄, 굵은 빔 한 줄 + "얌념~". 눈에서 튀던 파편(깃털처럼 보임) 제거.
- **기절 팝업** (사용자): 범위 공격마다 뜨던 "{스킬}!! N마리 기절" 묶음 팝업 제거 (CatManager.slamPopup 빈칸) → 새로 기절한 쥐마다 머리 위에 작은 "기절!!" (`Rat.Stun` → `RatManager.StunPopup`: 0.5초 이상 기절일 때만, 한 프레임 12개까지, 글·색·크기 인스펙터).

### 9-20. 2026-10-08 — 폰트 빠진 글자
- `Jua-Regular Dynamic SDF.asset` 이 Play 때마다 바뀌던 원인: charset_ko.txt 에 나중에 들어간 한글 자모(ㅋ 등)·기호가 구운 SDF 에 없어서 예비(Dynamic) 폰트가 그때그때 글자를 만듦 (로비 대사 "ㅋㅋㅋ 완전 놀람").
- `FontBaker` 새 메뉴 **NKK/Add Missing Chars (연결 유지)**: 기존 에셋에 빠진 글자만 덧붙임 (GUID 그대로). 실행 결과 GowunDodum·IBMPlex Medium/Bold +109자, Jua +51자. Jua 폰트 파일 자체에 없는 기호(①♪▶—「」 전각 문장부호 등)는 못 넣음 → 쓰지 말 것.
