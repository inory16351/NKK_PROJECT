# 업적 아이콘

업적 테이블(`Data_Table/업적 테이블.xlsx`, Achievement 시트)의 아이콘. 256×256 투명 PNG, 플랫 화풍.
모든 업적이 같은 메달 틀(빨강·크림 리본 + 금테 크림 메달)에 가운데 상징만 다름 → 필살기 아이콘과 구분.
유니티: `Assets/Art/Rats/AchvIcons/ach_<업적 id>.png` → 테이블 `achv_icon` = `AchvIcons/ach_<업적 id>`, 로비 `IconBook` 이 읽음.

- 지금 업적 40001~40033 = 필살기 30001~30033 을 끝까지 쓰기 (조건 `Ult_Use`)
- `Sheets/achv_icons_a.png`(24) · `achv_icons_b.png`(9) : Codex 원본 · `prompt_*.txt` · `names_*.txt` · `log_*.txt` (2026-10-08)
- 시트 b 는 메달이 칸보다 커서 실제 위치(1줄 y 10~325, 2줄 325~630, 가로 256 칸)로 잘랐음. 옅은 번짐(알파 110 미만)은 지움
- 업적을 늘릴 때: 테이블에 줄 추가(조건 타입은 Achv_Cond_Type 시트) → 같은 메달 틀 프롬프트로 상징만 바꿔 생성 → 여기와 유니티에 `ach_<id>.png`
