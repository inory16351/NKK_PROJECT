# 보스 파츠 v2 (2026-10-07)

- `boss_chief_parts.png` : 5층 보스 "경비대장 강철수" Codex 시트 (3x2, 자홍 배경). 프롬프트 `prompt_boss_chief.txt`, 참고 그림 `ref_humans.png`, 로그 `codex_log.txt`.
- 예전 웹 시트(`../sheet_boss_chief+boss_mad+boss_director.png`)는 몸통에 팔이 그려져 있어 따로 움직이는 팔과 합쳐지면 팔이 4개로 보였음 → 몸통은 팔 없이(어깨에서 잘림) 다시 그림.
- 파츠: 머리 · 화난 머리(angry, 공격할 때) · 겁먹은 머리(scared, 쓰러질 때) · 몸통 · 팔 · 다리 → `../../Parts/boss_chief/`
- 자르기: `python Tools/slice_human_parts.py <시트> boss_chief` (Parts/pivots.json 갱신) → `python Tools/gen_sprite_pivots.py` → 유니티 메뉴 NKK/Reimport Art Sprites · NKK/Build Human Art Library
- 미리보기 `_미리보기.png`
