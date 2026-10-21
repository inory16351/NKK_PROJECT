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

## alien v2 = 모선 강림 · 쥐 도시 폐허 (2026-10-21 초안, 사용자: 크고 웅장한 UFO 모양 우주선이 땅에 레이저 · 카메라가 위에서 아래로 레이저와 함께 · 쥐 도시가 폐허로)
- 그림: `alien/Sheets/v2/` (`gen_prompts.py` · `run_v2.sh`) → Sheets 로 복사 → `slice_superult.py alien`. 옛 그림 = `alien/Sheets/_v1_painted/` · `alien/_v1_painted/`.
  - 세로 파노라마 `pano_a~c` (밤하늘 · 구름 → 구름 아래 · 먼 쥐 도시 → 쥐 도시 광장) + 폐허 편집본 `pano_?_alt` → `pano_on` · `pano_alt` (겹침 419 · 397)
  - 거대 모선 `ufo1` → 주포 충전 `ufo2` (편집, 자홍 · 자르지 않음) · 조종석 `face_bg` + `cu_face1~3` (내려다봄 → 버튼 쾅 → 초록 빛 웃음, 편집) · 셀 `anim_end_1~3` (원반 위 팔짱 → 총구 후 → 브이 윙크)
  - `beam.png` → `fx_beam · beamcore · beamhit` (+ 아래 퍼진 끝을 뺀 마디 `fx_beamseg`) · `boom.png` → `fx_boom1~3 · shockring · debris · smoke` · `fx_flat` = charge · lightring · flash · fire · cloud(라벤더로 다시 칠함, `TINT`) · streak
- 연출 `superult_story.alien` (옛 것 = `alien_v1`), 1배속 · 6.5초 · 글 없음 (2차, 사용자: 멀쩡한 도시에 레이저를 뿌리고 터진 뒤 폐허를 **대비**해서): ① 구름 위로 모선이 묵직하게 내려와 화면 폭을 채움 ② 조종석 클로즈업 3프레임
  ③ **BEFORE** 불빛 반짝이는 멀쩡한 쥐 도시 (`CITY` 구도) ④ 주포 충전 ⑤ 발사 → 레이저 끝을 따라 하늘 → 구름(갈라짐) → 멀쩡한 도시까지 틸트 다운 → 레이저를 도시 위로 좌우로 쓸며 뿌림(피벗 = 주포) · 줄기 7개 더 · 연쇄 폭발
  → 굵은 마지막 일격 · 섬광 · 먼지 → **AFTER** 먼지가 걷히면 ③ 과 같은 구도의 폐허 (`cam swap` → pano_alt) → 원반 탄 쥐 → 게임 화면: 폭탄마 v2 와 같은 blast (초록 레이저 → 큰 폭발)
- 생성 .cs 약 4.9만 자 (MCP 5만 자 한도 가까움 — 더 넣으려면 술탄처럼 파일을 읽어 컴파일).
- 그림 속 자리 = `UFO_W · UFO_Y · CANNON · CLOUD_Y · PLAZA · CITY · GROUND_Y · SWEEP · BUTTON · ANTENNA`.
- 미리보기 = `Tools/superult_preview.py <code> <폴더>` (Unity 없이 SuperUltLayer 규칙으로 PIL 렌더 · 글 · 띠 마스크 없음) → `preview/alien_v2_caps.png` · `alien_v2_preview.gif`.

## alien v3 = 인디펜던스 데이 패러디 (2026-10-21 사용자 3차 · 지금 쓰는 것, 옛 것 = `alien_v2` · `alien_v1`)
- 그림: `alien/Sheets/v3/` (`gen_prompts.py` · `run_v3.sh`) → Sheets 로 복사 → `slice_superult.py alien` (v2 그림도 Sheets 에 그대로 있음).
  - ① `bg_chat` (떠드는 쥐들, `softBlur` → `bg_chat_soft` 포커스 아웃) · `fg_wall` (화면 1/3 앞 건물, 자홍 · 오른쪽 모서리 피벗) · 셀 `anim_spy_1~3` (트렌치코트 · 중절모 · 선글라스)
  - ② `bg_hand` + `cu_hand1→2` (손목 스파이 시계 버튼, 편집) ③ `bg_space` (지구) · `ship_far`
  - ④⑤⑦ `shot_descend` (세로 롱샷: 불타는 구름 구멍 → 하늘 → 쥐 도시) → `shot_descend_dmg` (가운데만 부서짐, 편집) · `zoomBlur` → `shot_descend_dmg_zoom1·2` (크롭 줌 블러)
    · 거대 우주선 `ship` → `ship2` (주포 충전, 편집) — 롱샷이라 sizeDelta 로 세로만 납작하게 (`SHIP_FLAT`)
    · (2차, 사용자: 도시와 거리를 훨씬 벌리고 위쪽을 덮는 느낌) 우주선 = 화면 폭 1.6배 · 아주 납작(0.22) · 화면 위에 붙인 천장 / 도시 = `SHOT_END` 0.56 으로 화면 아래로 내림 (아래 땅은 잘림) → 사이에 하늘이 넓게
  - ⑥ `bg_runway` (로우 앵글 · 소실점) · `jet_back1→2` (애프터버너, 편집) · `motionBlur` → `jet_back2_mblur`
  - ⑧ 셀 `anim_cry_1~3` · `story.json inset` → `ui_inset` · `ui_insetbg` (PIL 로 만든 흰 둥근 창 — 수어 통역 창 패러디) · `fx_v3.png` → `fx_signal · jetdot · afterburn · shard · panel · tear`
- 연출 `superult_story.alien`, 1배속 · 8.0초 · 글 없음: ① 0.4 스파이 엿보기 (건물 · 쥐 · 배경 시차) ② 1.45 버튼 ③ 1.98 지구 신호 → 우주선 접근 ④ 2.85 구름 구멍에서 우주선이 나오며 카메라도 같이 내려감
  ⑤ 4.0 충전 → 4.18 프레임 스탑 → 4.5 레이저 · 흔들림 · 크롭 줌 블러 (섬광 아래서 가운데만 부서진 도시로) ⑥ 4.85 전투기 우르릉 → 5.1 애프터버너 → 5.38 블러와 함께 소실점으로 (빨려 드는 속도선)
  ⑦ 5.7 점 같은 전투기가 주포로 → 6.25 진입 → 레이저가 위로 걷히며 멈춤 → 6.6 멈칫 ⑧ 6.85 연쇄 폭발 → 7.1 거대 폭발 · 파편 + 오른쪽 아래 우는 쥐 창
  → 게임 화면: 우주선 파편 비 (`hailFall` = shard · panel, 쾅 = boom1~3).
- (3차, 사용자: 부딪치는 장면이 작아 안 보임 · 전투기가 너무 빨라 0.7배) `JSPD` 0.7 = 발진(소실점까지 0.2초) · 접근(0.67초) 모두 느리게 → 8.3초.
  ⑦ 은 카메라 묶음 `Cam` (E) 안에서 주포 쪽으로 2.4배까지 따라 들어감 · 작은 전투기 = `jet_back2` 뒷모습을 줄여 씀 (날개 · 불꽃이 보임, 점 그림 `fx_jetdot` 안 씀) · 주포에 닿으면 0.12초 히트 스톱 → 폭발 · 충격파 · 불꽃 · 하얀 번쩍 → 카메라가 빠지며 레이저가 멈춤.
- 그림 속 자리 = `SPY_DX · WATCH · SIGNAL · SHOT_END · SHIP_W/Y/FLAT · SHIP_CANNON · IMPACT · VANISH · JET_Y · NOZZLES · INSET`. 미리보기 `preview/alien_v3_caps.png` · `alien_v3_preview.gif` · Unity 편집 모드 캡처 `alien_v3_unity_caps.png`.
- 긴 C# 은 `Tools/run_unity_cs.py <파일.cs>` (에디터 안에서 파일을 읽어 컴파일 → 5만 자 한도 없음). v3 생성 .cs 는 약 3.2만 자라 그냥 execute_code.

## parkrat v2 = 풍선 펑 (2026-10-21 초안, 사용자 콘티: 퍼레이드 몽타주 → 풍선 고민 · 유레카 → 에어 펌프 → 박수 (사선 하이 앵글) → 손 클로즈업 점점 빨리 → 같은 구도 당황 → 커지는 풍선 + 트래킹 줌 아웃 → 프레임 스탑 → 펑 → 풍압 몽타주 → 머쓱 · 데헷)
- 그림: `parkrat/Sheets/v2/` (`gen_prompts.py` · `run_v2.sh`) → Sheets 로 복사 → `slice_superult.py parkrat`. 옛 칠 그림 = `parkrat/Sheets/_v1_painted/` · `parkrat/_v1_painted/` (연출 `parkrat_v1`).
  - 컷 한 장 `shot_parade1~3` · `shot_think1→2` (유레카, 편집) · `shot_crowd1 ↔ 1b` (박수) → `shot_crowd2` (같은 틀 당황) · `shot_hand1 ↔ 2` · `shot_wave_a/b/c 1→2` (성 · 기념품 거리 · 구경꾼, 멀쩡 → 풍압)
  - 광장 `pano_on` (쥐 없음) → 롱샷 `wide_on` (pano_on 을 0.42 배로 가운데 두고 아웃페인팅, story.json `wide`) · 폐허 `pano_alt` (편집, 슬라이스에서 그대로 복사)
  - 셀 `anim_pump_1~3` (`ANIM_SAMEBOX`, 노즐 자리 고정) · `anim_end_1~3` (멍 → 긁적 → 테헤페로) · `props_flat` = balloon · balloon_tight · bulb · flyrat1·2 · ears · `fx_flat` = sparkle · confetti · clap · shockring · wind · streak · `pop.png` (GRID6) = pop1~3 · dome · debris · dustwall
- 연출 `superult_story.parkrat`, 1배속 · 7.1초 · 글 없음: 풍선(매듭 = 노즐 끝, 쥐 뒤)이 커지는 동안 카메라 배율이 기하급수로 줄어 화면 속 풍선 크기는 거의 그대로 = 둘레만 확 넓어짐 → 4.25~4.7 프레임 스탑 → 펑 → 풍압 3컷 (왼 → 오) → 폐허에 떨어져 데헷 → 게임 화면: blast (레이저 없이 pop 프레임 · 충격파 · 파편 · 먼지)
- 그림 속 자리 = `THINK_HEAD · RAT_FEET · RAT_H · NOZZLE · BALLOON_W · HAND_PUMP · TEHE_EYE`. 미리보기 `preview/parkrat_v2_caps.png` · `preview/parkrat_v2/parkrat_preview.gif`. 씬(Game.unity) 적용은 아직.

## jjdetective v2 = 범인은 바로 너야! (2026-10-21, 사용자 콘티: 사방에서 단서 조사 → 골똘히 생각 → 전구와 함께 결론 → 검은 실루엣 고양이 지목 "범인은 바로 너야!" → 도망가는 고양이를 마취총으로 기절 → 밟고 "사건 해결")
- 그림: `jjdetective/Sheets/v2/` (`gen_prompts.py` · `run_v2.sh`) → Sheets 로 복사 → `slice_superult.py jjdetective`. 옛 칠 그림 = `jjdetective/Sheets/_v1_painted/` · `jjdetective/_v1_painted/` (연출 `jjdetective_v1`).
  - 컷 한 장 `shot_clue1~4` (털린 치즈 가게: 발자국 · 할퀸 창틀 · 고양이 털 · 생선 뼈) · `shot_think1→2` (유레카 = 안경 하얗게 번쩍, 편집) · `shot_point` · `shot_culprit1→2` (흠칫 → 도망, 편집) · `shot_aim1→2` (손목시계 마취총 조준 → 발사, 편집) · `shot_win1→2` (기절한 고양이를 밟고 안경 → 엄지 척 윙크, 편집)
  - 추격 골목 `pano_on` (옆에서 본 달밤 골목, 쥐 · 고양이 없음) · 고양이 셀 `anim_catrun_1~3` (달리기) · `anim_catko_1~3` (마취침 맞고 펄쩍 → 비틀 → 코풍선 기절)
  - `props_flat` = pawprint · hair · fishbone · scratch · bulb · dart / `fx_flat` = sparkle · glare · bubble · dust · shockring · streak / `fx_hit` = dartfall(+ `fx_dartfallr` 돌린 사본) · feather · snot · hit1~3
- 연출 `superult_story.jjdetective`, 1배속 · 6.7초 · 대사 2줄만 컷신 글 (`story.json captions` point · solved, 제목 띠 · 자막 · 히트 수는 `NO_TEXT`):
  ① 0.4 단서 몽타주 4컷 (찾는 순간 고리 · 반짝 → 단서 아이콘이 왼쪽 위 수첩 줄로) ② 1.7 수첩 단서들이 머리 둘레를 돌다 머리로 쏙 ③ 2.42 전구 · 안경 번쩍
  ④ 2.78 지목 "범인은 바로 너야!" → 3.4 고양이 흠칫 → 3.72 도망 ⑤ 3.98 골목 추격 → 4.32 시계 조준 → 4.5 발사 → 4.84 엉덩이에 꽂힘 → 5.02 비틀 → 5.2 기절 ⑥ 5.5 밟고 "사건 해결!" → 6.08 엄지 척
  → 게임 화면: 마취침 비 (`hailFall` = dartfallr · feather, 꽂힘 = hit1~3 기절 별).
- 그림 속 자리 = `JJ_CLUE_PTS · JJ_THINK_HEAD/GLASS · JJ_POINT_TIP · JJ_ROAD_Y · JJ_CAT_H · JJ_CAT_RUMP · JJ_AIM_WATCH/CAT · JJ_WIN_GLASS/THUMB` (`JJ_` = 다른 쥐 상수와 이름 겹침 방지).
- 미리보기 `preview/jjdetective_v2_caps.png` · `preview/jjdetective_v2/jjdetective_preview.gif` · Unity 편집 모드 캡처 `preview/jjdetective_v2_unity_caps.png`. 씬(Game.unity) 적용 완료 (생성 .cs 약 3.1만 자 → execute_code 직접; `run_unity_cs.py` 로더는 이번에 어셈블리 중복 컴파일 오류).

## jwerry v2 = 톰과 줴리 추격전 (2026-10-21 초안, 사용자: 고양이가 쫓아옴 → 지형지물로 반격하며 도망 → 쥐구멍으로 쏙 · 횡방향 추격 · 추격전 카메라 앵글 · 줌)
- 그림: `jwerry/Sheets/v2/` (`gen_prompts.py` · `run_v2.sh`, 참고 = 게임 파츠 `ref_rig` · 게임 고양이 파츠 `ref_cat`(tuxedo, 그림체만) · 로비 배경 · 파라오 셀 · 스트리머 클로즈업) → Sheets 로 복사 → `slice_superult.py jwerry`. 옛 칠 그림 = `jwerry/Sheets/_v1_painted/` · `jwerry/_v1_painted/` (연출 `jwerry_v1`).
  - 가로 집 파노라마 `pano_a`(부엌) → `pano_b`(거실) → `pano_c`(복도 · 오른쪽 끝 쥐구멍): 앞 장 오른쪽 1/3 을 왼쪽에 둔 씨앗 `ref_seed_b/c` 를 오른쪽으로 아웃페인팅 → `stitch_pano` 가로 (겹침 512 · 512) = `pano_on` 3584×1024
  - 컷 한 장 `shot_open1→2` (치즈 → 고양이 그림자) · `shot_catface` · `shot_pan1→2` (프라이팬) · `shot_door1→2→3` (문 쾅 → 납작 → 팔랑) · `shot_rug1→2` (카펫) · `shot_wall1→2` (벽 쾅 → 기절) · `shot_hole1→2` (빼꼼 → 메롱)
  - 셀 `anim_jrun_1~3` · `anim_tomrun_1~3` (`ANIM_SAMEBOX`) · `anim_jmove_1~3` (돌아보며 메롱 · 다이빙 · 출발) · `anim_tommove_1~3` (팬 얼굴 · 김 뿜는 화남 · 덮침, `anim_tomrun.png` 을 참고로 다시 뽑음) · `motionBlur` → `anim_jmove_2_mblur` · `anim_tommove_3_mblur`
  - `props_flat` = pan · cheese · book · plant · chair(앞을 지나가는 가구 시차) · sweat / `fx_flat` = dust · streak · stars · bonk · steam · shockring / `fx_hit` = panfall · pan2 · star · hit1~3
- 연출 `superult_story.jwerry`, 1배속 · 7.3초 · 글 없음: ① 0.4 치즈 → 0.72 그림자 ② 0.98 고양이 얼굴 (로우 · 사선) ③ 1.25 추격 1: 웅크린 쥐에 3.2배로 바짝 → 1.4 튀어 나가며 휙 빠짐 · 고양이 난입 → 1.74 메롱 ④ 2.05 프라이팬 → 2.3 뿅 (히트 스톱)
  ⑤ 2.62 추격 2: 팬 얼굴 고양이 클로즈업 → 쥐로 휙 → 둘 다 넓게 ⑥ 3.4 문 → 3.6 쾅 → 3.84 팔랑 ⑦ 4.05 추격 3: 김 뿜는 고양이 → 쥐 쪽으로 밀고 들어감 ⑧ 4.7 카펫 → 4.88 공중제비
  ⑨ 5.2 바닥에 붙은 로우 트래킹 → 5.45 다이빙 · 5.5 덮침 → 5.62 쥐구멍 쏙 (급줌 3.7배) ⑩ 5.8 벽 쾅 (아코디언) → 6.08 기절 ⑪ 6.35 빼꼼 → 6.78 메롱 윙크 → 게임 화면: 프라이팬 비 (`hailFall` = panfall · pan2, 뿅 = hit1~3)
- 추격 카메라 `C(t, x, 줌, 기울기, 이징, 발끝 화면 y)` = 발끝 높이를 화면에 고정 · 판 밖이 안 보이게 붙잡음 (기울기 여유). 달리는 바닥 `JW_FLOOR` (부엌은 조리대 앞이라 아래 → 쥐구멍 쪽 벽 아래).
- 그림 속 자리 = `JW_FLOOR · JW_HOLE · JER_H/TOM_H · OPEN_HEAD · PAN_HIT · DOOR_HIT · WALL_HIT · WALL_HEAD · HOLE_EYE`. 미리보기 `preview/jwerry_v2_caps.png` · `preview/jwerry_v2/jwerry_preview.gif`.

## zapham v2 = 백만볼트 · 또 당했다~ (2026-10-21, 사용자: 냐옹이 닮은 고양이가 괴롭힘 → 노란 전기 백만볼트로 감전 → 몸통박치기 → 로켓단 퇴장처럼 하늘 저편으로 반짝)
- 참고: 유튜브 Netflix Anime 'Team Rocket Blasting Off Again Compilation' — 폭발/충격 → 대자로 빙글빙글 사선으로 날아감 → 하늘 근접 컷(구름) → 점처럼 작아짐 → 반짝 ✦.
- 그림: `zapham/Sheets/v2/` (`gen_prompts.py` · `run_v2.sh`) → Sheets 로 복사 → `slice_superult.py zapham`. 옛 그림 = `zapham/Sheets/_v1_painted/` · `zapham/_v1_painted/` (옛 손으로 만든 장면 · `zapham_ground` 안 씀).
  - Codex: `pano_on` (들판 · 넓은 하늘) · `bg_sky` · `shot_glare1→2` (얼굴 클로즈업, 편집) · `fx_flat` · `fx_hit` · `props_flat`
  - **ChatGPT 웹 (Codex 한도 · 검열)**: 햄쥐 전신(노란 후드 + 빨간 볼 + 전기)은 플랫 그림체에서 '피카츄'로, 이마 금화 크림 고양이는 '냐옹이'로 걸림 → 햄쥐는 '비니 후드 햄스터' · 번개는 그림에서 빼고 이펙트 레이어로, 고양이는 **빨간 반다나 줄무늬 길고양이**로 바꿈. 두 캐릭터를 한 장에 그리는 것도 걸려서 셀 시트를 따로 그려 합성.
    `anim_volt` (충전 · 젖힘 · 발사) · `anim_tackle` (준비 · 박치기 · 브이 윙크) · `anim_hamtease` (씨 먹기 · 볼 눌림 · 노려봄, 같은 틀) / `anim_cattease` (콕 · 씨 뺏고 메롱 · 흠칫) · `anim_shock` (털 곤두섬 ↔ 엑스레이 해골 · 그을림, 같은 틀) · `anim_catfly` (날아감 · 빙글 · 손 흔듦). 옛 Codex 고양이 = `Sheets/v2/_codex_cat/`.
- 연출 `superult_story.zapham`, 1배속 · 6.5초 · 글 없음: ① 들판 괴롭힘 (콕콕 3번 → 씨 뺏기 → 노려봄) ② 얼굴 클로즈업 눈 번쩍 ③ 충전 → 백만볼트 · 카메라가 번개를 따라 휙 → 감전(해골 깜빡) → 그을림 → 몸통박치기 · 히트 별 → 하늘로 ④ 하늘 근접 컷 빙글빙글 ⑤ 롱샷: 점 → 반짝 ✦ → 햄쥐 브이 윙크로 다가감 → 게임 화면: 대상마다 번개 (`fx_boltfall` · `fx_hit1~3`).
- 크기 = 시트 축척 (`ZAP_REF`/`CAT_REF` 선 자세 프레임 높이 → `ZAP_H`/`CAT_H`), 그림 속 자리 = `TEASE_X · TEASE_CHEEK · GLARE_CHEEKS · GROUND_Y · ZAP_X/CAT_X · STAR_AT · WINK`. 미리보기 `preview/zapham_v2/`. Game 씬 적용 · 옛 v1 그림 git rm · Web Atlas 다시 (packed 0).
