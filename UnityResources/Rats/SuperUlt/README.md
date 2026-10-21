# 초필살기 레이어 컷신 그림 (2026-10-17, Codex)

메이플 오리진 스킬식: 배경 · 캐릭터 포즈 · 이펙트 · 밈 소품을 **따로** 그려서 컷신에서 각각 움직임 (`Ults/SuperUltLayer.cs` 키프레임).
쥐마다 폴더 `<code>/` — 원본·프롬프트·로그·참고 그림 = `<code>/Sheets/`. 미리보기 GIF = `preview/`.

## 만드는 법
1. `<code>/Sheets/prompt_*.txt` (공통: 그림체 = 뽑기 컷 그림 `ref_style` · 캐릭터 = `ref_zapham`/`ref_parts`, 글자 없음)
   - `bg_*.png` 배경 1536×1024 (불 켜짐 → 같은 그림을 참고로 넣어 '불 꺼짐' 편집본을 뽑으면 구도가 같아 전환에 씀)
   - `*_pose_*.png` 캐릭터 한 포즈 1024×1536, 자홍 배경
   - `fx_bolts.png` 검은 배경 3×2 빛 효과 (밝기 = 투명도)
   - `props.png` 자홍 배경 소품 여러 개
2. `bash Tools/codex.sh exec --skip-git-repo-check --ephemeral -s workspace-write -C <Sheets> -i ref_style.png -i ref_zapham.png -i ref_parts.png < prompt_X.txt` (여러 개 동시 가능)
   - 안전 필터에 걸리면(예: '전투 함성·주먹') 귀엽고 밝은 쪽으로 문구를 바꿔 다시.
3. `python Tools/slice_superult.py <code>` → 키잉(자홍 + 분홍 번짐 걷기 · 검은 배경) · 자르기 · 소품 자동 분리(`PROPS` 표에 이름·대략 위치) → `Assets/Art/Rats/SuperUlt/<code>/` 복사
4. 씬: Game `HUD/SuperUlt/Cut/Frame/Scenes/<쥐>` 에 레이어 배치 (scratchpad 식 build2.cs 를 MCP execute_code 로), `Styles/<쥐>.scene` 연결

## 큰 배경 + 가상 카메라 (v3, 사용자: 카메라 무빙 · 배경을 크게)
- 세로 파노라마: `pano_a`(하늘) → `pano_b`(도시) → `pano_c`(지붕), 다음 장은 앞 장 아래 400px(`ref_prev_bottom.png`)를 보고 이어 그림 → `python Tools/stitch_vertical.py pano_on.png auto pano_a.png pano_b.png pano_c.png` (겹침 399 · 393).
- 대정전 버전: 조각마다 켜짐 그림을 참고로 '불만 끈' 편집본 `pano_?_off` → **같은 겹침 값**으로 이어 붙여 `pano_off` (구도가 같아 켜짐↔꺼짐 전환).
- 씬: 묶음마다 `Rig`(줌·기울기·충격 kicks) > `Track`(위치 = -보는 곳) 안에 파노라마·캐릭터·이펙트를 파노라마 좌표로 둠 → 카메라 키만 바꾸면 무빙. 속도선·배터리·클로즈업·밈 글은 화면 좌표.
- 클로즈업 컷 `zap_face_close` (1536×1024 전체 화면).

## zapham (찌릿 햄찌 · 도시 대정전)
- pano_on / pano_off (세로 파노라마, v3) · bg_city_on / bg_city_off (v2, 안 씀)
- zap_pose_absorb (플러그를 하늘로 · ②) · zap_pose_dash (날아오르기 · ③) · zap_pose_finish (하늘 가리키며 윙크 · ④)
- fx_bolt_big · fx_bolt_fork · fx_ring_shock · fx_streak · fx_starburst · fx_aura
- prop_bill (전기요금 고지서) · prop_battery (스마트폰 배터리 틀, 채우기는 UI) · prop_bulb (터지는 전구) · prop_socket · prop_roof (앞 지붕)
- 밈: 배터리 충전 1% → 100% (② 전기가 빨려 듦) · "※ 이번 달 전기세는 연구소가 냅니다" + 날아가는 고지서 (④)

## icequeen v2 (2026-10-19, 사용자: 플랫 배경 · 쥐와 배경 따로 · 마지막 컷신 제거 · 5초)
- 그림 다시 뽑음 (Codex): 배경 = **로비 배경(`UI/lobby_bg.png`) 그림체의 플랫**(외곽선·광택·붓 질감 없음) 세로 파노라마 `pano_a~c` + 얼어붙은 판 `pano_?_alt` · 클로즈업 배경 `face_bg` / 쥐 = 자홍 배경에 따로 `pose_intro` · `pose_act` · `face_close` (클로즈업도 쥐만 → 키잉, `face_bg` 위에 같은 키로 겹침).
  원본·프롬프트·로그 = `icequeen/Sheets/v2/` (러너 = scratchpad 식 run_iq.sh: 쥐 3 · face_bg · pano a→b→c 이어 그림 + alt 편집). 옛 그림(그림 같은 칠) = `icequeen/Sheets/_v1_painted/`.
- 연출: ④ 피니시 컷(파노라마로 돌아가는 카메라 · 피니시 포즈) 없음 → ③ 본 연출(띠 + 고드름 다단)이 4.5초까지 이어지고 마지막 일격(큰 고드름 · 파편 · 밈 글)은 같은 화면에서 → 5.0초까지 걷힘. `pose_finish` 안 씀.
  코드 = `SuperUltStyle.noFinishCut` · `actEnd` (매니저가 ③ 끝 = 일격 = 걷힘 시작으로 씀) · `Tools/superult_story.py` `NO_FINISH` 표 (다른 쥐도 여기 넣고 story 를 같은 식으로 바꾸면 됨).
- (같은 날 2차) **띠 없이 화면 전체 컷신**: ③ 도 전체 화면(얼어붙은 호수 + 활강), 고드름 자리마다 실제 피해 숫자 · 누적 피해 · 화면 얼음에 금(`fx_crack1~3`) → 일격에 산산조각(`fx_shard1~3`) → 게임이 드러나며 적 꽁꽁. 금·파편 = `Sheets/v2/crack.png`.
- (3차) **셀 애니 프레임**: 리그 파츠(`Sheets/v2/rig_*` 고해상도, `ref_rig.png`)를 참고로 동작 프레임 시트 `anim_*.png`(자홍 가로 3칸) → `anim_<동작>_1~3`. 컷 8개로 전환을 늘리고, 이펙트·소품도 플랫(`fx_flat.png` · `props_flat.png`). 러너 = `Sheets/v2/run_rig.sh` · `run_anim.sh`.

## ramjui v2 = 람쥐썬더 밈 몽타주 (2026-10-20 초안, 사용자: 람쥐썬더 밈 짤을 게임 그림체 몽타주 초필살기 컷신으로)
- 밈 출처 = 나무위키 '람쥐썬더' (2007 디시 던파갤 제목낚시 · 번개 다람쥐 짤 · 찌리찌리판 · 토르 램지 랜딩 'Bring me Acorn!' = 사실 겨드랑이 긁기 · 수고람쥐썬더). 원본 사진은 쓰지 않고 구도 · 상황만 Codex 로 플랫 그림체 재현.
- 그림: `ramjui/Sheets/v2/` (`gen_prompts.py` · `run_v2.sh` · 프롬프트 · 로그) → Sheets 로 복사 → `slice_superult.py ramjui`. 옛 칠 그림 = `ramjui/Sheets/_v1_painted/` · `ramjui/_v1_painted/`.
- 연출: `Tools/superult_story.py` `ramjui` (옛 것 = `ramjui_v1`) — 컷 6개: ① 낡은 PC 게시글 "얘들아 이거 빨리 봐라" 클릭 → ② 두 손 번개 "람쥐썬더!!!" → ③ 찌리찌리 → ④ 도토리 망치 슈퍼히어로 랜딩 → ⑤ 사실 겨드랑이 긁는 중 → ⑥ 윙크 경례 "수고람쥐썬더!!!" → 4.3초 걷힘 → 게임 화면 번개. 자막 = `ramjui/Sheets/story.json` captions.

## fondue v2 = 트레비 치즈 분수 (2026-10-20, 사용자: 인게임 그림체 · 아델 오리진식 트래킹 줌 아웃 · 아이스 퀸식 게임 화면 마무리)
- 그림: `fondue/Sheets/v2/` (`gen_prompts.py` · `run_v2.sh`) → Sheets 로 복사 → `slice_superult.py fondue`. 옛 그림 = `fondue/Sheets/_v1_painted/` · `fondue/_v1_painted/`.
  - 클로즈업 연속 프레임 `cu_snap1~3` · `cu_face1~4` = 1번을 참고로 넣어 Codex '편집' (같은 틀 → 그림만 바꿔 끼움, 자홍 키잉만 · 자르지 않음, `face_bg` 위)
  - `wide_on` = 광장 롱샷 (첫 분수 그림 `v2/pano_on` 을 1/3 크기로 가운데 두고 Codex 아웃페인팅) → 분수 부분을 잘라 고해상도로 다시 그린 `v2/fountain_on` = Sheets `pano_on` (분수 전체 · 줌 아웃 끝 프레이밍). 롱샷 판은 그 아래 같은 축척으로 깔림 (story.json `wide` = 자른 자리 scale · x · y). 러너 `run_wide.sh`
  - `story.json maskBlur` {그림: [타원 · 흐림 · 어둡게]} → `story.json zoomBlur` → `pano_zoom1·2` (사자후식 방사형 줌 블러: 폭발 점에서 바깥으로 뻗는 흐림 · 가운데 선명, 두 세기를 0.05초마다 번갈아 떨림 · 충격파 3번 · 바깥으로 뻗는 속도선 · 하얀 번쩍 없음). `maskBlur`(어둡게 흐리는 타원 블러)는 안 씀
- 연출 `superult_story.fondue` (옛 것 = `fondue_v1`), 1배속 · 5.35초: ① 아래→위 익스트림 클로즈업 틸트 ② 핑거 스냅 ③ 윙크 → 선글라스 ④ 분수 꼭대기 쥐 얼굴에서 0.45초 줌 아웃 (배율 기하급수 · 블러 없음 · 살짝 더 빠졌다가 분수 전체에 딱 멈춤) ⑤ 1초 프레임 스탑 ⑥ 도시보다 높은 치즈 기둥 3개 + 터지는 순간 크롭 마스크 블러 + 카메라 살짝 다가감 ⑦ 치즈 벽이 위아래로 덮음 → 게임 화면 치즈 비 (hailFall = 치즈 방울 · 조각, 철퍽 3프레임) · 글 없음 (NO_TEXT: 제목 띠 · 자막 · 히트 수 · 밈 글)
- 매니저: worldPhase 쥐는 `actEnd` 가 t_end(5.0) 보다 길어도 자르지 않음 (람쥐 5.39 도 이제 끝까지 나옴).
- 미리보기 `preview/fondue_v2_caps.png` = 편집 모드 캡처 (Play 안 들어감: 레이어 Apply(t) → 임시 카메라로 HUD 렌더).

## starchef v2 = 라따뚜이 시식 (2026-10-20, 사용자: 게임 그림체 그대로 · Codex CLI)
- 그림: `starchef/Sheets/v2/` (`gen_prompts.py` · `run_v2.sh`, 참고 = 게임 파츠 `ref_rig`(쥐) · `ref_cook`(요리사 `Humans/Parts/cook` · `cook_mount`) · 로비 배경 · 파라오 셀 · 퐁듀 클로즈업) → Sheets 로 복사 → `slice_superult.py starchef`. 옛 칠 그림 = `starchef/Sheets/_v1_painted/` · `starchef/_v1_painted/`.
  - 컷마다 배경까지 그린 한 장 `shot_*` (자르지 않음) · 연속 프레임 `shot_taste2·3` · `shot_ride2·3` · `shot_v2` = 1번 '편집' (같은 틀). `cu_dish` = 자홍 → `face_bg` 위.
  - `story.json motionBlur` {그림: 길이 px} → `<그림>_mblur` (가로 블러 사본, 컷 전환 가림막)
- 연출 `superult_story.starchef` (옛 것 = `starchef_v1`), 1배속 · 5.4초: ① 무릎 꿇고 고개 숙여 바치는 요리사 → 카운터 위 거만한 쥐까지 카메라 몸체가 올라감 (페데스탈 · 앞 냄비 시차) ② 벌벌 떨리는 손 위 라따뚜이 ③ 눈 감고 손가락으로 찍어 맛봄 → 얼굴로 급줌 → 눈 번쩍 ④ 블러 휙 → 머리 위에 올라탄 쥐 · 모자 날아감 ⑤ 머리카락 뜯기 → 대머리 ⑥ 기절한 요리사 위 브이 (로우 앵글) → 게임 화면: 대상마다 머리카락 · 기절 별.
- 글 없음 (`NO_TEXT`: 밈 글 · 제목 띠 · 자막 · 히트 수, 2026-10-20 사용자).
- (2차, 사용자) ② 손 컷 = ①과 같은 측면 시점 (`cu_dish` 자홍 + 같은 주방 `bg_dish`, 손만 떨림) · ④ 올라타기 = 쥐 · 사람 따로: 딱 컷 → 쥐 없는 사람 `shot_ride0` → 블러 쥐 `pose_leap_mblur`(`motionBlur`, 투명 그림도 됨)가 휙 지나감 → 쥐 판 `cu_ratride`(ride1 과 같은 틀)가 머리 위로 떨어져 착지 → `shot_ride1` 로 바꿔 끼움. 화면 전환 블러는 안 씀. 미리보기 `preview/starchef_v2_ride.png`.
- 그림 속 자리 상수 = `OFFER_RAT` · `TASTE_FACE` · `RIDE_HEAD` · `V_DIZZY` · `V_HAND` (그림을 다시 뽑으면 다시 잼). 미리보기 `preview/starchef_v2_caps.png`.

## sultan v2 = 오일머니 금화 비 (2026-10-20, 사용자: 석유로 큰돈 버는 건 짧게 → 궁전에서 나와 마법 양탄자를 타고 금화를 비처럼 뿌림 · 2차: 석유 컷은 석유만 · 유정 작게 · 궁전에 돈 가득 · 궁전 밖에서 바로 양탄자로 하늘까지 · 양탄자 밑으로 금화를 땅까지 쭉)
- 그림: `sultan/Sheets/v2/` (`gen_prompts.py` · `run_v2.sh`, 1차 그림 일부 = `v2/_old`) → Sheets 로 복사 → `slice_superult.py sultan`. 옛 그림 = `sultan/Sheets/_v1_painted/` · `sultan/_v1_painted/`.
  - `shot_oil1→2→3` (작은 유정 → 땅이 들썩 → 검은 석유 분출, 편집 · 금화 없음) · `face_bg` + `cu_face1→2` (흐뭇 → 금화 눈) · `shot_rise` (세로 한 장: 아래 = 금화 산이 쌓인 궁전 · 위 = 하늘) · `pano_on` (도시 위 하늘, 가로)
  - 셀 프레임 `anim_ride_1~3` (궁전 앞 떠오름) · `anim_pour_1~3` (자루를 기울여 쏟음 — 양탄자 밑 금화는 slice 에서 잘라냄, 발끝 = 양탄자 바닥) · `pour.png` → `fx_pour1~3` (금화 줄기 루프, 칸 크기 그대로)
  - `fx_flat` = coin · coin2 · sparkle · coinburst · gem · oil / `fx_hit` = coinfall · sack · wall · clink1~3
- 연출 `superult_story.sultan` (옛 것 = `sultan_v1`), 1배속 · 4.15초: ① 0.4 유정 → 0.56 들썩 → 0.68 분출 ② 0.98 금화 눈 ③ 1.42 금화 궁전 앞 양탄자 → 1.68 떠올라 카메라가 하늘까지 틸트 ④ 2.4 하늘: 2.62부터 양탄자 밑으로 금화 줄기를 땅까지 쏟아부음 (줄기가 닿는 지붕마다 짤랑 · 밈 글) + 2.45부터 화면 전체 금화 비(뒤 130 · 앞 170, 점점 거세짐) → 덮는 벽 없이 그대로 걷혀 게임 화면 금화 비로 이어짐 (3차, 사용자: 비처럼 막 쏟아짐 · 인게임에서도 지속)
- (4차) 하늘에서 밑으로 붓기 대신 `anim_ride_1~3` 으로 금화를 퍼서 손으로 하늘 높이 흩뿌림 (0.22초마다, 손에서 9닢이 부채꼴로 솟구쳐 화면 위로) → 그 금화가 화면 위에서 비로 쏟아짐 · 보석 없이 순수 금화만 (`fx_pour` · `anim_pour` 안 씀)
- (5차) 카메라 무빙: ① 옆으로 흐르며 다가감 → 분출 순간 줌인 + 위로 틸트 ② 기울어 들어와 흔들림 · 금화 눈 줌인 ③ 줌인 상태로 돌며 시작 → 좌우로 흔들리며 위로 휙 ④ 아래에서 양탄자를 따라 휙 올라와 가까이(1.6배) → 넓게 빠져(1.1배) 도시 위 금화 비 → 끝에 다시 밀고 들어감
- (6차) 떠오르는 동안 쥐 화면 자리 · 크기 고정 (양탄자 바닥 FEET = -420px · 키 540px, ④ 첫 부분도 줌 ZF · FOLLOW 로 같은 자리) = 머리 위 여백 일정, 상승감은 배경 틸트 · 속도선 / 던지기 0.7배속 (TSPD, 0.31초마다) · 던질 때마다 트래킹 줌 (양탄자 쪽으로 1.16배 확 다가갔다 원래 카메라로)
- (7·8차) 트래킹 줌 = 넣었다 빼지 않고 던질 때마다 1.08배씩 계단식으로 더 들어감 (1.2 → 약 1.96배) · 양탄자는 제자리 (0.5, 0.64) · 왼쪽(anim_ride_2) · 오른쪽(anim_ride_3)으로 한 번씩 번갈아 뿌림, 금화 부채꼴도 그쪽으로 · 카메라도 그쪽으로 살짝 기울임
- 게임 화면: worldTime 4초 · worldSnowScale 1.4 (금화 굵게 · 빠르게 바람 (-90, -950)) · `SuperUltStyle.worldSnowFade` 1.2초 = 끝 무렵 금화 비가 서서히 걷힘 (매니저 SnowAlpha, 다른 쥐는 0 = 그대로)
- 그림 속 자리 = `superult_story.py` GUSH · EYES · RIDE_H · POUR_DX/DY · POUR_LEN. 미리보기 `preview/sultan_v2_caps.png`.
- 씬 적용: 생성 .cs 가 MCP 5만 자에 가까워서, execute_code 안에서 파일을 읽어 CSharpCodeProvider 로 컴파일해 실행함 (참조는 필요한 어셈블리만 — 전부 넣으면 명령줄이 너무 길어 실패).

## emperor v2 = 총병 앞으로 · 장전 · 격발 (2026-10-20, 사용자: 에픽세븐 릴리아스 3스킬 참고 · 총 든 쥐 병사를 잔뜩 소환 → 일제 난사 · 총알이 맵을 덮음 · 왼 → 오)
- 그림: `emperor/Sheets/v2/` (`gen_prompts.py` · `run_v2.sh`, 참고 = 황제 게임 파츠 `ref_rig` · 로비 배경 · 파라오 셀 · 퐁듀 클로즈업) → Sheets 로 복사 → `slice_superult.py emperor`. 옛 칠 그림 = `emperor/Sheets/_v1_painted/` · `emperor/_v1_painted/`.
  - 병사 셀 프레임 `anim_soldier_1~3` (행진 · 조준 · 격발) = `ANIM_SAMEBOX` 로 세 프레임 같은 틀 (격발 프레임 총구 섬광 때문에 프레임마다 여백을 자르면 옆으로 튐). 총구 섬광 `fx_muzzle` 은 `FLIP_X` 로 좌우 뒤집음 (Codex 가 왼쪽으로 그림).
- 연출 `superult_story.emperor` (옛 것 = `emperor_v1`), 1배속 · 5.25초 · 글 없음: ① 칼을 높이 듦 ② 궁궐 앞 들판에 병사 세 줄 소환 ③ 장전 (공이치기 철컥 → 조준) ④ 칼을 앞으로 내리침 ⑤ 일제 사격 4번 (줄마다 조준 ↔ 격발 · 섬광 · 연기 · 총알) + 예광탄 80발 ⑥ 총알이 화면을 덮음 → 게임 화면: `worldWind` (2600, -40) 으로 총알 그림이 왼 → 오로 맵을 가로지르고 곳곳 탄착.
- (2차, 사용자: 측면 → 정면 일제 사격 · 총알 속도감 카메라) 정면 그림 `bg_front` · `anim_front_1~3` (준비 · 카메라 조준 · 정면 섬광, 같은 틀) · `cu_aimfront` · `cu_empfront` · `fx_front.png` → `fx_fmuzzle · fbullet · fbullet3q · smokering · hole · radial` (`EXTRA_GRID`). 연출: 병사 · 배경을 카메라 묶음 `Cam` 에 넣고 소실점 기준 확대 = 쏠 때마다 쾅 밀고 들어감 → 마지막 사격 뒤 대열로 돌진 (2.4배) · 총알이 소실점에서 커지며 화면 가장자리로 스쳐 감 → 탄흔이 화면(렌즈)을 덮음. 게임 화면 단계 (4차, 사용자): 게임 화면 이펙트 없음 (`hailSplash` 빈 목록 · `worldSnowOff`) — 컷신 4.45초부터 화면을 덮는 탄흔 묶음 `Hold`(루트 바로 아래)만 컷신이 걷혀도 게임 화면 위에 1.5초(`worldTime`) 남았다가 0.45초(`holdFade`) 동안 사라짐. 매니저 기능 = `SuperUltStyle.holdGroup` (걷히기 시작할 때 그 묶음을 컷신 밖 SuperUlt 아래로 옮김 → 끝나면 제자리).
- 병사 · 총알 · 예광탄은 생성 코드에서 C# 반복문 (숫자 표) — 펼치면 MCP 실행 길이 제한(5만 자)을 넘음. 미리보기 `preview/emperor_v2_caps.png`.
