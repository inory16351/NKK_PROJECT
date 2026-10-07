# 치즈 이펙트 (2026-10-07)

치즈 퐁듀 쥐 필살기 "회전회오리" (UltCheeseSpin · 30033): 돌면서 몸에서 치즈 덩어리를 사방으로 던짐 → 착지 폭발 → 웅덩이 (밟은 쥐는 미끄러져 기절).
코드: `Assets/Scripts/Ults/CheesePuddles.cs` (UltimateManager 소품 이름으로 찾음 → 넣은 뒤 **Fill Props** 다시 실행)

| 파일 | 쓰임 |
|---|---|
| `cheese_splash.png` | 착지 치즈 폭발 (확 커졌다 사라짐) |
| `cheese_chunk.png` | 날아가는 치즈 덩어리 (오른쪽 위로 나는 모양, 꼬리 왼쪽 아래 → 진행 방향으로 돌림) |
| `cheese_crown.png` | 바닥에서 솟는 치즈 왕관 (옆모습) |
| `cheese_glob.png` | 튀는 치즈 방울 |
| `Sheets/fx_cheese.png` | Codex 원본 시트 (자홍 2×2) · `prompt_cheese.txt` · `log_cheese.txt` · 참고 `ref_*.png` (운석 폭발 · 치즈 웅덩이 · 치즈 방울) |
| `Sheets/slice_cheese.py` | 자르기 (key_magenta + 작은 잡티 제거) + Unity 사본·.meta 생성 |
| `_미리보기.png` | 잘라낸 결과 모아 보기 |

- 웅덩이는 기존 `NewRats/cheese_puddle.png`, 낙하 지점 표시는 `FX_Meteor/meteor_target.png` (노랗게 물들임) 재사용.
- Unity 사본: `Assets/Art/Rats/FX_Cheese/` + `Assets/Art/Rats/UltProps/` (Fill Props 대상 폴더)
