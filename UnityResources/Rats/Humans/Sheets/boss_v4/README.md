# 인간형 보스 옆모습 몸통·팔·다리 (2026-10-08)

예전 boss_v2/v3 몸통은 **정면** 그림이라 옆모습 머리와 안 맞고, 팔(몸통과 같은 색)이 몸통 가운데에 묻혀 장갑만 보였음 → 사용자: 몸통을 옆모습으로 다시.
- Codex 시트 `boss_side_parts.png` (3열 경비대장·광기의 수석 연구원·연구소장 × 3행 몸통·팔·다리, 자홍 배경), 프롬프트 `prompt_boss_side.txt`, 참고 `ref_side_guard.png`(경비원 옆모습) · `ref_boss_outfits.png`(보스 머리 + 예전 옷).
- 머리(head·angry·scared)는 예전 그대로. 예전 정면 파츠 = `old_front_*`.
- 머리 크기: 보스 머리가 몸통만큼 커서 `pivots.json headScale` 0.85 (→ HumanRigMeta head_scale → HumanArtLibrary.Entry.headScale, HumanRig 가 목 기준으로 줄임).
- 목 맞춤: 몸통 위 목살 중 (줄인) 머리 목 폭보다 넓은 피부 픽셀은 지우고, 남은 목은 머리 피부색으로 칠함 (머리 목 단면과 굵기·색이 이어지게).
- `Humans/Parts/pivots.json` 관절 다시 계산 → `Tools/gen_sprite_pivots.py` → 유니티 재임포트 + NKK/Build Human Art Library.
- `_미리보기.png` = 게임 화면 (Boss 테스트 소환).
