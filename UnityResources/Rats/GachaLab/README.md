# 유전자 뽑기 연출 — 비밀 실험실 컷씬 (2026-10-12, Codex)

믹서기 연출(잔인하다는 의견)을 바꾼 것. 코드 `Lobby/GachaShow.cs`, 씬 Lobby `UI/GachaShow/Root`.

흐름: ① 로우앵글 `lab_c1` (고글 쓴 쥐들이 실험 용기 안을 내려다봄, "실험 개시!!") → ② 스포이드 `lab_c2` + 물방울 `lab_drop` 맺힘 · 반짝 `glow_glint` → ③ 용기 속 `lab_c3` 로 줌 블러 (잔상 사본) + 떨어지는 물방울 →
④ **지하도시 화면**: 비밀 실험실 건물(`../Lobby_Market/lm_lab.png`)이 부르르 → 펑 (`boom_teal`, 건물 → `lm_lab_blown`) → 돌아다니던 지하 쥐들 + 고글 쓴 실험실 쥐 3마리(`lab_goggles`)가 빙글빙글 건물 문으로 빨려 듦 (건물이 화면에 없으면 실험대 컷 `lab_c4` 로 대신) →
⑤ **세로로 긴 한 장 `lab_shaft`** (1024×3808 → 화면 1920×7140, 세로 3장 이어 붙임): 실험실 플라스크 바닥 → 흙·뿌리 → 묻힌 물건 → 돌 기초 → 벽돌 벽·등불 → 하수구 출구까지 같은 배관 하나로 이어짐. 위→아래로 천천히·빠르게·천천히 + 세로 잔상 (사용자: 컷이 따로 놀아 연결이 안 됨 → 한 장으로) →
⑥ 같은 그림 맨 아래 출구로 살짝 당겨 옴 → 뚜껑 `lab_lid_closed`(구리빛으로 칠함) 덜컹덜컹 (유니크 이상이면 틈으로 등급 색 빛 `glow_orb` · `glow_leak`) → **"눌러서 열기!" 클릭해야 열림** (사용자) →
열림 `lab_lid_open`: 유니크 이상 = 빛 폭발 + 빛기둥 `glow_pillar` + 반짝이 튐 (전설 금빛 · 신화 흰빛) · 에픽 이하 = 파리 `lab_fly` →
⑦ 전부 한 마리씩 (가챠 공개식): 검은 실루엣 + 모이는 빛 (좋은 등급은 빛기둥·더 길게) → 번쩍 `glow_flash` → 색이 돌아오며 폭발 (보통 `glow_rays` 등급 색 · 전설 `burst_gold` · 신화 `burst_rainbow`) + 충격파 고리 `glow_ring` + 반짝이 → 클릭/스페이스 = 다음, 건너뛰기 = 결과 → 결과 칸.

| 파일 | 내용 |
|---|---|
| `lab_c1~c4.png` | 컷 그림 1536×1024 — 오프닝 컷씬(`../Story/`)과 같은 그림체 |
| `lab_shaft.png` | ⑤⑥ 세로 한 장 1024×3808 = `Sheets/shaft_a`(실험실 바닥 → 흙·뿌리) + `shaft_b`(깊은 흙·묻힌 물건 → 돌 기초) + `shaft_c`(돌 → 벽돌 → 하수구 출구) 를 `python Tools/stitch_vertical.py <출력> auto <a> <b> <c>` 로 이어 붙임 (각 장 위 띠를 앞 장 아래 400px `ref_prev_bottom.png` 를 보고 이어 그려서 겹침 400px 자동). 출구 구멍 중심 = 높이 87.7% · x 507, 지름 ≈ 폭 29%. 뚜껑·빛은 씬에서 이 그림의 자식 (x -9, y -2694). 유니티 Max Size 4096. 옛 짧은 그림 = `Sheets/lab_shaft.png` |
| `lab_c5` · `lab_c6` | 옛 배관·출구 컷 (안 씀, `lab_shaft` 참고 그림으로만) |
| `glow_*` · `boom_teal` · `burst_gold` · `burst_rainbow` | **그린 빛 효과** (`Sheets/lab_glow.png`, 검은 배경 4×3 → `python Tools/slice_glow_grid.py` = 밝기를 투명도로). 흰빛은 게임에서 등급 색으로 칠함 (사용자: 벡터 같은 효과는 별로 → 직접 그린 그림으로) |
| `lab_lid_closed/open` · `lab_fly` · `lab_drop` · `lab_goggles` | `Sheets/lab_fx.png` 4×2 (`python Tools/slice_grid_all.py`). `lab_rays` · `lab_burst` · `lab_boom` 은 옛 벡터풍 (안 씀) |
| `Sheets/` | 원본 · 프롬프트(`prompt_lab_*.txt` · `prompt_shaft_a~c.txt`, 공통 `_common.txt` · `shaft_common.txt`) · 로그 · 참고 그림 |

유니티 사본 `Assets/Art/Rats/GachaLab/`. 배경음악 = `Audio/BGM/BGM_Gacha.mp3` (SceneMusic.gachaClip, 결과 칸 전까지 = `GachaShow.Cutscene`, 결과가 뜨면 `gachaFadeOut` 2.5초 동안 줄며 지하 곡으로).

**컷 전환 (2026-10-12)**: 모든 컷 사이 = 줌 인 + 흰 플래시 → 다음 컷 펀치 인 (`Lobby/CutFx.cs`), 배관은 위로 스치는 흰 속도선. 전설 이상 쥐 고유 컷씬은 `../GachaIntro/README.md` (시범 = 아이스 퀸).
