# 쥐 스킬 아이콘 (특수 능력 · 특수 액션)

쥐 79종의 특수 능력(패시브, 20001~)·특수 액션(21001~) 아이콘 158개. 256×256 투명 PNG, 화풍 = 쥐 그림과 같은 플랫(Untitled Goose Game 식).
유니티: `Assets/Art/Rats/RatSkillIcons/` 에 같은 파일 → 쥐 캐릭터 테이블 Skill 시트 `skill_asset` = `RatSkillIcons/rs_<스킬 id>`, 로비 `IconBook` 이 읽음.

- `Sheets/rat_skill_icons_1~7.png` : Codex 원본 시트 (6×4, 2026-10-06) · `prompt_N.txt` · `log_N.txt` · `sheet_ids.json`(칸 순서 = 스킬 id)
- `Sheets/rat_skill_icons_redo.png` : 너무 비슷했던 22개 다시 그림 (2026-10-08, `prompt_redo.txt` · `names_redo.txt`)
- 시트 6 은 Codex 가 칸 순서를 바꿔 그려서 20070·20071·20072·21070·21071 은 파일 이름을 바로잡음 (자르기 다시 하면 주의)
- 자르기: `python Tools/slice_sheet.py <시트> 6 4 <폴더> <이름,...> --blobs --pad 0` → 알파 24 미만 지우고 220px 로 맞춰 256 캔버스 가운데
- 성장 노드 아이콘은 새로 안 그림: 쥐 성장 테이블 `node_icon` (@action · @passive · @ult = 그 쥐의 아이콘, 나머지 = 공용 스킬 아이콘)
