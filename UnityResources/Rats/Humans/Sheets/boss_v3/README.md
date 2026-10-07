# 보스 파츠 v3 (2026-10-07)

- `boss_mad_parts.png` : 10층 보스 "광기의 수석 연구원" (3x2, 자홍 배경) · 프롬프트 `prompt_boss_mad.txt` · 로그 `codex_log_mad.txt`
- `boss_director_parts.png` : 15층 보스 "연구소장" · `prompt_boss_director.txt` · `codex_log_director.txt`
- `boss_props.png` : 보스 투사체 (플라스크 · 헤어볼 · 초록 얼룩, 3x1) · `prompt_boss_props.txt` → `../../../BossProps/`
- 참고 그림: `ref_humans.png`(사람 파츠) · `ref_boss_chief.png`(경비대장 v2 시트, 배치·화풍 기준)
- 경비대장 v2 와 같은 규칙: 몸통에 팔 없음, 머리 3종(보통·화남 angry·겁먹음 scared). 예전 웹 파츠는 `old_web_parts/`
- 자르기: `python Tools/slice_human_parts.py <시트> boss_mad|boss_director` → `python Tools/gen_sprite_pivots.py` → 유니티 NKK/Reimport Art Sprites · NKK/Build Human Art Library
- 투사체: 자홍 빼고 `python Tools/slice_sheet.py <시트> 3 1 UnityResources/Rats/BossProps flask,hairball,splash_green --pad 6`
