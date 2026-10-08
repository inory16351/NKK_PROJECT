# 고양이 보스 파츠 (2026-10-08)

20·25·30층 고양이 보스 전용 그림 (예전엔 일반 고양이 gym·fire·space 를 3.4배로 키워 씀).
- `boss_cat_parts.png` : Codex 원본 (3줄 × 5칸, 자홍 배경) · 프롬프트 `prompt_boss_cats.txt` · 로그 `log_boss_cats.txt`
  - 1줄 `boss_zero` 실험체 제로 (거대 메인쿤 실험체: 전극·흉터·번호표 목줄·붕대 꼬리)
  - 2줄 `boss_witch` 대마법사 마녀 고양이 (별 모자·보라 망토·금 부적)
  - 3줄 `boss_commander` 우주 고양이 사령관 (안테나 헬멧·견장·빨간 띠·망토·장갑 다리)
- 참고 그림 `ref_cat_parts.png` = 기존 고양이 gym·fire·space 파츠 (배치·관절 기준)
- 자르기: `python Tools/slice_cat_parts.py <시트> 3 boss_zero,boss_witch,boss_commander` → `../../Parts/<id>/` + `Parts/pivots.json` → `python Tools/gen_sprite_pivots.py` → `Assets/Art/Rats/Cats/Parts/<id>/` 복사 → 유니티 NKK/Reimport Art Sprites · NKK/Build Cat Art Library
- 스테이지 테이블 Boss 시트 `code_id` 가 이 id 를 가리킴
