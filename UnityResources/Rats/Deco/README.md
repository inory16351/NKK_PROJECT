# Deco — 도토리 상점 장식 그림 (숲 · 네온, 2026-11-04)

쥐 친구를 뺀 장식 20개 (테마마다 10개). Codex, 원본 그림을 격자로 늘어놓은 참고 시트(`ref_grid.png`)를 같은 칸 · 같은 크기로 테마만 바꿔 그리게 함 → 칸별 키잉 → 원본과 같은 캔버스 크기로.

| 폴더 (`forest/` · `neon/` 아래) | 원본 → 장식 | 장식 id 숲 / 네온 |
|---|---|---|
| `badges` | 훈장 `Rogue/rk_1~8` | 95031 / 95032 |
| `wheel` | 쳇바퀴 `lb_wheel_ring` · `lb_wheel_stand` | 95061 / 95062 |
| `manhole` | 맨홀 `lobby_manhole_closed/open` | 95081 / 95082 |
| `alley_a` | 미니 게임기 `lm_arcade` · 암시장 `lm_stall` | 95091 · 95101 / 95092 · 95102 |
| `alley_b` | 비밀 실험실 `lm_lab` · 증강 연구소 `aug_lab` | 95111 · 95121 / 95112 · 95122 |
| `alleybg` | 뒷골목 배경 `lm_bg` (reskin, `sheet.png` 통째) | 95131 / 95132 |
| `ui` | UI 버튼 `UI_Kit/ui_plank(+green·red·gray)` · `ui_btn_round(+green)` · `ui_pill` · `ui_key` / 커서 `cursor_normal·pressed_big` | 95141 / 95142 · 커서 95151 / 95152 |

- 폴더마다 `ref_grid.png` · `layout.json` · `prompt.txt` · `log.txt` · `sheet.png`(Codex 원본) · `out/<원본 이름>_<테마>.png` · `check.png`(위 원본 / 아래 새 그림). 네온은 초록 바탕 키잉.
- 도구: `Tools/deco_sheets.py` (ref · cut) · `Tools/deco_run.sh` (여러 폴더 동시).
- 유니티: `Assets/Art/Rats/Deco/{forest,neon}/` · 커서 = `Assets/Art/Cursor/cursor_{normal,pressed}_{forest,neon}.png` (48px, 끝점 = 원본처럼 왼쪽 위). UI 조각 9-슬라이스 테두리는 원본과 같게 맞춤.
- **커서 다시 (2026-11-04, 사용자)**: 숲 = 도토리 모양 · 네온 = 네온사인 화살표. `{forest,neon}/cursor/` (sheet.png · `out_*_big.png` · `out_*_48.png` → `Assets/Art/Cursor/cursor_{normal,pressed}_{테마}.png` 같은 이름으로 덮어씀). 예전 쥐 머리판 = `_old_cursor/`. 미리보기 `_cursor_preview.png`.
- 아는 점: 숲 판자 오른쪽 아래 도토리가 테두리 안쪽까지 들어와 길게 늘이면 조금 늘어남 · 네온 판자 4색은 가운데가 같은 남색 (테두리 색으로 구분).
- **숲 UI 버튼 투명도 3% (2026-11-04 사용자)**: `Assets/Art/Rats/Deco/forest/ui_*_forest.png` 8개 알파 × 0.97 (가장 진한 곳 247/255). 바꾸기 전 = `forest/_alpha100/`.
- **투명도 시안 (2026-11-04 사용자, 게임에는 안 넣음)**: `forest/_alpha95/` (5%) · `forest/_alpha92/` (8%) · `_alpha97/` = 지금 게임(3%) 사본. 비교 `forest/_투명도_비교.png` (처음 아지트 배경 위 0 · 3 · 5 · 8%).
- **투명도 8% 적용 (2026-11-04 사용자)**: `_alpha92/` → `Assets/Art/Rats/Deco/forest/ui_*_forest.png` 8개 (가장 진한 곳 235/255).
