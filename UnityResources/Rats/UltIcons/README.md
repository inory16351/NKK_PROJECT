# 필살기 아이콘 (2026-10-06)

필살기 게이지가 다 차면 화면 아래에 뜨는 버튼 아이콘. 쥐 캐릭터 테이블 Ultimate 시트의 `ult_icon` 칸 = `UltIcons/ult_<필살기 id>`.

| 파일 | 내용 |
|---|---|
| `Sheets/ult_icons.png` | Codex 원본 시트 (자홍 배경 6×6, 1536²) — **지금 쓰는 판** |
| `Sheets/prompt_ulticons.txt` · `log_ulticons.txt` | 프롬프트 · 로그 (칸 순서가 프롬프트에 적혀 있음) |
| `Sheets/ult_icons_v1.png` 외 `_v1` | 1차 시트 (투명 배경이라 부스러기가 많아 폐기) |
| `ult_30001.png` ~ `ult_30033.png` | 잘라낸 아이콘 256² (칸 1~33 = 필살기 30001~30033) |
| `cs_autoult.png` | 공용 스킬 "필살기 자동 사용" 아이콘 (칸 34, `../SkillIcons/` 에도 복사) |
| `spare_star.png` · `spare_cheese.png` | 여분 (칸 35·36) |
| `_v1_지저분/` | 1차에서 잘랐던 아이콘 (보관용) |
| `_미리보기.png` | 잘라낸 결과 모아 보기 (파일 이름 순) |

- 다시 자르기: `python Tools/slice_icon_grid.py UnityResources/Rats/UltIcons/Sheets/ult_icons.png 6 6 UnityResources/Rats/UltIcons ult_30001,…,ult_30033,cs_autoult,spare_star,spare_cheese`
  (칸마다 가장 큰 덩어리만 남겨서 떨어진 부스러기는 자동으로 버림)
- Unity 사본: `Assets/Art/Rats/UltIcons/`, `Assets/Art/Rats/SkillIcons/cs_autoult.png`
