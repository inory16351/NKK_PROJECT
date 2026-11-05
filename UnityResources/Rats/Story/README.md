# 오프닝 컷씬 (2026-10-09, Codex)
타이틀에서 '처음부터' → 오프닝 7컷 → 바로 1층. 대사는 튜토리얼 테이블 Story 시트 (컷 번호 · 그림 이름 · 대사).
- story_01 평화로운 실험실 우리 (낙원) · story_02 하늘에서 내려온 '손가락'의 은총 · story_03 돌아오지 않는 친구 (밤)
- story_04 연구 노트를 훔쳐보고 실험당하는 걸 깨달음 · story_05 그날, 쥐류는 떠올렸다 (진격의 거인 패러디)
- story_06 철창을 갉아 여는 결의 · story_07 대탈출 (사람·고양이)
- 1536×1024, 아래 25% 는 자막 판이 덮음. 원본·프롬프트·로그 = `Sheets/` (참고 그림 ref_game.png · ref_human.png). `Assets/Art/Rats/Story/` 에 복사

- **story_02 앞발 고침 (2026-11-05 사용자)**: 가운데 흰 쥐 앞발 3개 → 2개. Codex 로 그 쥐 주변(`fix_story02_paws/crop.png`)을 다시 그려 앞발 · 가슴만 원본에 붙임 (`merge.py`). 바로 전 판 = `fix_story02_paws/base.png` (Sheets/story_02.png 는 처음 Codex 원본 그대로).
- **story_06 손 고침 (2026-11-05 사용자)**: 왼쪽 철창 무는 쥐 = 사용자가 빨간 선으로 표시한 손 2개(코 옆 · 철창 오른편)만 지움, 오른쪽 검은 쥐 = 머리 옆 손 · 흰 쥐 옆 주인 없는 주먹 팔 지움. Codex 로 부분 크롭(`fix_story06_hands/red`, `blue`)을 다시 그려 고친 곳만 원본에 붙임 (`merge.py`). 바로 전 판 = `fix_story06_hands/base.png`.
