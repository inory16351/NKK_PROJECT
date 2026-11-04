# Lobby_Split — 아지트 배경에서 소품 4종 분리 (2026-11-04)

배경 그림에 같이 그려져 있던 **낮잠침대 · 회의 테이블 세트(탁자 + 위 물건 + 의자 2개) · 친구들 책장(책 더미 + 집게 램프 + 옆 병) · 치즈창고(선반 + 아래 상자)** 를 따로 떼어 도토리 상점 장식으로 바꿀 수 있게 함. 훈장은 원래 따로 된 그림(`Home/Badges`)이라 해당 없음.

| 폴더 | 원본 |
|---|---|
| `base` | `UI/lobby_bg.png` (처음 아지트) |
| `catlab` · `earthlab` · `buildsite` · `sealab` · `warmuseum` · `spacelab` | 훈장별 배경 (4 · 6 · 9 · 11 · 13 · 16훈장) |
| `forest` · `neon` | 도토리 상점 로비 배경 95011 · 95012 |
| `park` | 놀이공원 (도토리 테이블에 아직 상품 없음 — 그림만) |

폴더마다: `base.png`(원본) · `prompt_empty/props.txt` · `log_*` · `empty.png`(소품 지운 빈 배경) · `props.png`(소품만 남기고 나머지 자홍) → `out/` = `<이름>_empty.png` · `<이름>_bed/table/books/shelf.png`(키잉·자름) · `rects.json`(원본 위 픽셀 상자) · `check.png`(원본 | 빈 배경+소품 | 차이).
- 네온은 소품에 분홍·보라가 많아 자홍 키잉에 같이 지워져서 **초록(#00FF00) 바탕으로 다시** (`neon/_magenta/` = 첫 판, 자르기는 `green` 옵션).
- 도구: `Tools/lobby_split_prompts.py`(프롬프트) · `lobby_split_run.sh`(Codex 2장 동시) · `lobby_split_cut.py`(키잉·자르기·확인 그림). `pivots.json` = 소품 피벗(배경 가운데) → `Tools/gen_sprite_pivots.py` 가 `sprite_pivots.json` 에 넣음.
- 유니티: `Assets/Art/Rats/UI/LobbySplit/` = `lobby_<이름>_empty.png`(임포트 설정 = lobby_bg) · `lp_<이름>_<소품>.png`(피벗 = 배경 가운데라 `Home/Props/*` 를 원점에 두고 그림만 바꿈).
- 아는 점: 기본 배경은 램프 불빛이 벽에 번지던 게 빈 배경에서 조금 약해짐 · 네온 선반은 기둥이 벽 쪽이라 판만 붙은 모양.
