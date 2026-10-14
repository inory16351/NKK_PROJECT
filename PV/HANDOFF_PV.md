# itch.io 홍보 영상 (PV) — 인수인계 (2026-10-13 밤, 2차 세션)

다음 에이전트는 이 문서부터 읽을 것. 사용자 규칙(한국어 설명, 질문은 바로, Codex/Gemini 그림 등)은 루트 `HANDOFF.md` §0 · 메모리 그대로.

## 1. 정해진 것 (사용자)
- **영어판** PV, 약 **40초**, 1920×1080 60fps. 저작권 걱정되는 밈·패러디는 전부 뺌 (보스 밈 기술, 진격의 거인 컷, 해리포터·홈랜더·스타워즈·디즈니·톰과제리·렛잇고 등. 위험 쥐 목록 = §5 risky).
- 아래쪽 자막 금지 → **만화식 글자를 화면 여기저기에 박기**. 글자는 폰트 말고 **그림(Gemini 레터링)** 으로 (사용자: "텍스트로 하니까 별로").
- 그림체 = 사용자 메인 타이틀 그림(`UnityResources/Rats/PV/Sheets/ref_title_page.png`). 게임 컷씬(story_* 등) 재활용 금지.
- 그림: Codex (오늘 19:05 까지 한도 막힘) → 지금은 **Gemini 이미지(Nano Banana)** 사용 허락받음. 애니메이션이 필요한 장면은 **Gemini 동영상(Veo)** 허락받음.
- 음악 `UnityResources/Audio/BGM/BGM_PV.mp3` (Gemini, 120 BPM, 1:05, 곡 61.03초 = 마지막 쾅). 컷 경계는 0.5초(박) 단위.

## 2. 콘티 v6 (2026-10-14, 사용자 수정 반영 · `SCENES`, 총 35초)
- 음악 = 사용자 곡 `BGM_PV2.mp3`(Operation_Cheddar, 120 BPM) 편집본 `PV/music_v6.wav`: 곡 9.05~34.10 → 영상 0~25 (곡 31.5초 정적 = AND 22.5초) + 곡 46.50~ → 영상 25~ (곡 55.04초 쾅 = 윙크 33.5초). Gemini 음악·동영상은 2026-10-14 계속 실패. 소리만 다시 = `python Tools/pv/make_pv2.py --remix`.
| 시간 | 장면 | 함수 |
|---|---|---|
| 0–3 | 제목 (쥐 헤딩 Squeak!) | o_title |
| 3–5.5 | SMASH! → Codex 달리기 쥐 무리(`smash_run.png`) 화면 밖으로 퇴장 | o_smash |
| 5.5–8 | 인게임 물건 날리기 ×5 (`smash_1~5`) — 컷마다 **좌우 반전 · 줌 인/아웃 · 기울기 번갈아** (리듬) + CRASH/BAM… | o_game_smash |
| 8–10.5 | 두 쥐 껴안기 → SWARM!!! (정면 달리기 윙크 컷 뺌) | s_swarm |
| 10.5–12.5 | 인게임 번식 ×4 + 게임식 하트 합성 | o_game_breed |
| 12.5–15.5 | **UPGRADE!!** (AND 뺌) → 근육 쥐 4단계 (STEAL 장면 뺌) | s_upgrade |
| 15.5–16.5 | 스킬 트리 노드 열림 + LEVEL UP! | s_power6 |
| 16.5–22.5 | 필살기 **4개 × 1.5초**: 파라오 · 산타 · 대멸종 · 람쥐 썬더. 각 = 배너 0.25초 → 첫 타격 → 클라이맥스 (점프컷 3개, 컷마다 줌 펀치 + 흔들림) + ULTIMATES! (`ult_1~4`, `ULT6`) | s_power6 |
| 22.5–25 | **AND.....** (Codex `let_and.png`) 떨리며 올라옴, 점 톡톡 → 1.05초 터져 흩어짐 → 고양이 로우앵글 실루엣(`cat_silhouette_noeyes.png`)이 다가옴 → 가까워지면 원래 눈(`cat_silhouette.png`)이 눈꺼풀처럼 열림 (`eyes_open`) | s_and |
| 25–29 | 고양이 보스 3마리 (메인쿤 · 마녀 · 우주) + BOSS EVERY 5 FLOORS! | s_boss |
| 29–35 | 끝 화면 PLAY NOW + 매달린 쥐 → 아이리스 → 33.5초 윙크(음악 쾅) | s_play |
- 뺀 것(코드는 남아 있음): o_front(정면 달리기) · s_steal · s_cat8(Codex 고양이 쥐 장면) · s_run_veo. 이전 타임라인 백업 = 세션 스크래치(make_pv2_v5_backup.py, 임시).
- **유니티 건드리지 말 것** (사용자 2026-10-14: 다른 작업 중). 인게임 컷은 이미 찍은 `PV/clips/take_*` 만 씀.

## 3. 남은 일 (순서)
0. 최종본 `PV/out/PV_v3.mp4` (2026-10-14, 1080p60) → 사용자 확인 중.
1. **사용자 확인**: 초안 `PV/out/PV_v3_draft.mp4` → 수정. 확인 대기: 람쥐 썬더(risky 쥐) 넣을지, 사무라이 칼 컷의 대사 팝업(애니 패러디) 괜찮은지, 번식 컷.
2. 최종 렌더 `python Tools/pv/make_pv2.py` (1080p 60fps).
3. 커밋 (아직 아무것도 커밋 안 함. `packages-lock.json` 은 Recorder 설치로 바뀐 것 → main-game 에 커밋 후 mvp 로 합치는 규칙). `PV/clips/take_*.mp4` 는 큼 (합 ~250MB) → 커밋할지 사용자에게 물을 것.
4. itch.io 스크린샷 GIF · 커버 630×500 (계획만).
5. `UnityResources/Rats/PV/README.md` 에 v3·gem 내용 적기.

## 4. 요령
- **Gemini 다운로드**: 받은 파일이 `~/Downloads/<uuid>.tmp` 로 생겼다가 금방 사라짐 → 클릭 **전에** `bash Tools/pv/grab.sh <저장경로>` 를 백그라운드로 띄워 두면 새 파일을 바로 복사 (스크립트 내용: Downloads 새 파일 크기 멈추면 cp, 60초 제한). 다운로드 버튼 = `button[aria-label="원본 크기 이미지 다운로드"]` 의 마지막 것. 생성 중 = `button[aria-label="대답 생성 중지"]` 존재.
- **Gemini 에 그림 올리기**: 내장 브라우저는 로컬 파일 업로드·클립보드 붙여넣기가 안 됨 → **사용자가 직접 끌어다 놓음** (SendUserFile 로 채팅에 띄워 주면 됨). 지금 이미지 채팅엔 참고 3장(style_ref · rat_ref · lettering_ref)이 이미 올라가 있음 → 같은 채팅에 이어서 프롬프트만 넣으면 됨. 비율 16:9, 배경 = 순수 자홍 #FF00FF (키잉).
- **키잉/자르기** `Tools/pv/pvkit.py`: `img(key=True)` · `blobs()`(떨어진 캐릭터, 옆 조각 지움) · `split_letters()`(테두리 붙은 글자도 글자별) · `split_axis(axis=0)`(줄별). Gemini 워터마크(2752×1536 그림 오른쪽 아래 반짝이)는 `unwatermark()` 가 알파 지도(`Tools/pv/gemini_wm_alpha.png`)로 거꾸로 풀어 지움 (글자 위에 겹쳐도 됨).
- **레터링**: `lw(key, (폰트글자,))` = `LINES` 표(파일·줄·단어)의 Gemini 그림을 폰트 글자 자리 크기로. 파일: `let_words1`(SWARM/RUN/PLAY NOW) · `let_words2`(AND UPGRADE/ULTIMATES/LEVEL UP) · `let_words3`(BOSS EVERY 5 FLOORS/+1) · `let_hits`(CRASH BAM POW / CRUNCH KABOOM) · `let_smash`·`let_steal`(글자별).
- **쥐 리그** `K.rat_image(code, K.rat_pose_run(phase), length)` = 게임 RatRig 와 같은 조립 (Parts + RatRigMeta + .meta 피벗).
- **Unity 촬영** (mvp, MCP execute_code, Unity MCP 포트 6400): Play 전 `PlayerPrefs nkk_slot=5` → Play → `NKK.Progress.I.UnlockEverything(); NKK.Loc.SetLang("en"); SceneManager.LoadScene("Game")` → 위험 쥐 걸러내기(`GameDatabase.Instance.Rats` 의 risky code_id `unlock_rank=999`, 화면 쥐 RemoveRat) → HUD 숨김(HUD 자식 CheeseText·ComboText·PowerText·FloorText·TimeText·TimeBar·PopCount·BannerText·BannerSubText·BossBar·Minimap·UltBar·Promote·Tutorial·QuitMenu·AchievementToast·UltAchievement·CoinTemplate·Fade 에 CanvasGroup alpha 0 — `??` 말고 `if (!cg)` 로).
  - **Unity Recorder 쓰지 말 것**: 이 PC(DX12)에서 MP4 는 초당 1프레임으로 멈추고, JPG 연속도 위아래가 다른 순간으로 찢어짐. → **`Time.captureFramerate = 60` + 매 프레임 `ScreenCapture.CaptureScreenshot(dir/f_0000.png)`** (Game 씬은 `rm.Later(0f, shoot)` 를 스스로 다시 거는 방식, 로비는 `EditorApplication.update` 에서 frameCount 바뀔 때만) → `ffmpeg -framerate 60 -i f_%04d.png -crf 15` → PNG 폴더 지움. 6초 = 약 500MB PNG.
  - 장면 부르기: `rm.Later(t, action)`(게임 시간 지연, 0f = 다음 프레임) · `UltimateManager.TestUlt(code)` · `Boss.TestSpawn(i)` + `ForceAttack(type)` (보스 6마리 기술 목록 = `boss.Moves(row)`) · 카메라 `cam.zoom` + `CenterOn(x, y)` 를 매 프레임 다시 걸면 고정 (게임이 층 이동·peek 로 카메라를 옮김). 보스는 zoom 1.1 + `CenterOn(boss.x, boss.y - Height*0.4/0.85)`.
  - 번식: 인구가 최대면 안 됨 → 쥐 7마리만 남기고 두 마리를 맞닿게 Spawn + `breedCD=0, noBreed=0` → 다음 프레임 번식. 게임 하트는 화면에 안 보임(원인 모름, FxManager.Hearts 리스트엔 들어감) → 영상에서 합성.
  - 스킬 트리: `LoadScene("Lobby")` → Progress 의 private `skills`(HashSet) 에서 1훈장 노드 지우기 + `treeCache` Clear → `LobbyManager.OpenPage("skill")` → `SkillPage.ShowTier(1)` → 6프레임마다 열린 노드 중 가장 싼 것 `sel` 설정 + private `Buy()` (SkillPage.Recommend 는 지금 훈장(8) 기준이라 못 씀).
  - **유니티 창이 뒤에 있으면 느림** → PowerShell `SetForegroundWindow` (keybd_event ALT 먼저) 로 앞으로.
  - **끝나면 Play 멈추고 nkk_slot=1, nkk_lang="ko" 로 되돌리기** (이 세션 끝에 되돌려 둠).
  - risky: zapham parkrat jwerry jjdetective streamrat ramjui fondue bomber icequeen rocker plaguerat trooper burger robot
  - 테이크 (`PV/clips`): `take_smash`(smash_1~5: 2.0/5.5/6.2/7.0/7.8초) · `take_breed5`(탄생 0.433/1.683/2.967/4.217초, 화면 위치 = `BREED_AT`) · `take_ult_<code>` 14개(vampire dino alien viking emperor wizard cosmic samurai dragon santa pharaoh knight ratking ramjui; ult_1 = pharaoh 0.33~1.43 통째 · ult_2 = dino 배너 0.33(0.27초)+3.2~ · ult_3 = ramjui 배너 0.33+2.7~ (ffmpeg trim+concat)) · `take_boss_0~5`(0 경비대장 1 연구원 2 소장 3 메인쿤 4 마녀 5 우주; boss_1~3 = 3번 0.4~2.0 · 4번 0.2~1.3 · 5번 1.2~2.8) · `take_skill`(skill_1 = 0.3초부터 4배속, crop 1600×900+0+150).
- 그림 보관: Codex 그림 `UnityResources/Rats/PV/Sheets/v3/`(프롬프트 p_*.txt, 로그) · Gemini 그림 `…/v3/gem/` · Gemini 동영상 `…/v3/veo/`. README(`UnityResources/Rats/PV/README.md`)에 v3 내용은 아직 안 적음.
- 계획 문서 `PV/PV 계획.md` 는 v3 초안 시점 (지금 콘티는 이 문서 §2 가 최신).
