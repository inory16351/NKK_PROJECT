# 박쥐 (쥐 친구 95161) — 컷아웃 리그 파츠

Codex 생성 (2026-11-04) · 원본·프롬프트·로그 = `Sheets/` (bat_parts.png, prompt_parts.txt, log_parts.txt, ref_*.png)
자르기 `Sheets/slice_bat.py` (자홍 키잉 = Tools/slice_rat_parts.key_magenta) · 조립 미리보기 `Sheets/compose_bat.py`

캐릭터는 왼쪽을 봄. 피벗 = 이미지 좌상단 기준 0~1.

| 파츠 | 파일 | 피벗 |
|---|---|---|
| 머리 | head.png | 목 단면 (0.67, 0.93) · 크기 0.62 |
| 몸통 | torso.png | 부착점: 목 (0.30, 0.14) · 어깨 (0.58, 0.16) · 엉덩이 (0.62, 0.90) |
| 가까운 날개 (펼침, 뒤쪽으로 뻗음) | wing.png | 어깨 (0.05, 0.86) · 크기 1.3 |
| 먼 날개 (펼침, 뒤쪽으로 뻗음) | wing_far.png | 어깨 (0.07, 0.86) · 크기 1.3 |
| 접은 날개 (매달림) | wing_fold.png | 어깨 (0.52, 0.05) |
| 발 | feet.png | 엉덩이 (0.5, 0.02) |

- 매달림 = 바로 선 자세로 조립(발·몸통·머리·접은 날개) 후 위아래 뒤집기, 발끝이 천장에 닿음.
- 날기 = 날개가 몸 뒤쪽(오른쪽)으로 뻗어 +20°(위) ~ -75°(아래, 시계 방향) 사이로 퍼덕임, 먼 날개 -10°. (2026-11-04 사용자: 날개가 앞쪽으로 움직여 보임 → Codex 원본 날개를 좌우 반전해 뒤쪽으로)
- 게임 적용 (2026-11-04): `Assets/Art/Rats/RatFriends/Bat/` (스프라이트 피벗 = 위 표, 유니티 좌하단 기준으로 변환) · 스크립트 `Scripts/Lobby/AlleyBat.cs` · Lobby 씬 `LobbyPan/Alley/Bat` (천장 (-1.4, 2.97), Visual 크기 0.2). 도토리 상점에서 95161 을 사면 보임, 누르면 떨어지며 몸을 뒤집고 4.5초 날다가 제자리로 돌아와 매달림.
- 피벗 수정 (2026-11-04): 처음 넣을 때 `RatSpriteImporter` 가 sprite_pivots.json 에 없는 그림을 가운데 피벗으로 덮어써서 날개가 그림 가운데를 축으로 돌았음 (앞쪽을 덮음) → `pivots.json`(pivotUnity) 추가 + `Tools/gen_sprite_pivots.py` 가 `RatFriends/<이름>/pivots.json` 을 읽게 함. 퍼덕임 초당 3.5 (미리보기 GIF 와 같은 빠르기).
