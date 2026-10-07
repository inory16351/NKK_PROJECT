# 찍!찍!!찍!!! — 유니티 이식 진행 상황 (인수인계)

웹게임 `Proto_Game/rat-uprising.html`(약 1만 줄 JS)을 유니티로 옮기는 작업. 이 문서만 보고 다음 작업자가 이어갈 수 있게 정리함.
마지막 갱신: 2026-10-07

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
- **특수 능력(패시브)은 처음부터 켜짐**, **특수 액션은 조각 성장으로 해금**, **공용 스킬 묘기는 공용 스킬로 해금**.
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
   - 남음 (순서): 치즈 창고(스킬 지도) → 쳇바퀴 훈련(조각 강화, 그다음 `Progress.autoUpgradeInRun` 끄기) → 훈장 → 친구들(도감) → 낮잠 침대(기록·저장). 탭 아이콘·지도·쳇바퀴 등 로비 소품(`Rats/Lobby/lb_*`)은 예전 그림체 → 플랫으로 다시 만들지 결정
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
- 남은 일: 코드에 직접 들어간 팝업 글(Rat.Action "찌릿!!/콰릉!", ItemManager.ZapChain "찌릿!", 웹 요리사 말 팝업 등) → 테이블/인스펙터로 · 로비 업적(훈장 연동) · 로비 쳇바퀴 훈련 → `Progress.autoUpgradeInRun` 끄기 · 벽 금 간 자국(웹은 선) · 사용자 플레이 피드백.

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
- 아직 없음: 물건·가구 부술 때 가끔 연구자료 (웹 researchDrop: 가구 12% 1~3, 물건 1.2% 1).

### 9-5. 다음 작업 제안
- 보스 (5층마다), 연구자료 드랍, 로비 치즈 창고(스킬 지도) 등 §6 남은 것.
- 각 단계마다 Unity 컴파일·플레이 확인 → 커밋.
