# 별·반짝이 다시 그림 (2026-10-07)
사용자 피드백 "별 이미지가 이질적" → 게임 그림체(평면, 외곽선 없음, 둥근 끝, 연한 회색 그림자 한 톤)로 다시 만듦.
Codex 시트 `Sheets/stars_sheet.png` (자홍 3×2) · `prompt_stars.txt` · `log_stars.txt` · 참고 `ref_*.png` · 자르기 `slice_stars.py`
(칸별 가장 큰 덩어리 → 원래 그림의 불투명 영역에 비율 유지로 맞춰 **같은 파일명·같은 픽셀 크기로 덮어씀** → .meta·GUID·참조 그대로).
`_미리보기.png` : 위 = 옛 그림, 아래 = 새 그림. 옛 그림은 `old/` 에 보관. 칸별 원본은 `new/`.

| 새 그림 (new/) | 덮어쓴 Unity 파일 | 쓰임 | 색 |
|---|---|---|---|
| `star_round` | `Assets/Art/FX/Tint/star.png` | FX_Stars 파티클(스프라이트 모드 1번) · RatManager.stunStarSprite(기절 별) · PickupTemplate · FxManager/UltimateManager 스티커·소품 `star` (UltFireworks 등) | 흰색, 런타임 색입힘 |
| `twinkle_chunky` | `Assets/Art/FX/Tint/twinkle.png` | FX_Stars 파티클(2번) · FxManager/UltimateManager `twinkle` | 흰색, 색입힘 |
| `star_round` | `Assets/Art/Generated/star.png` (64×64) | `FX_Stars.mat` 기본 텍스처 (파티클은 스프라이트 모드라 실제론 위 두 장) | 흰색 |
| `star_round` | `Assets/Art/Rats/FX_SuperJump/sj_star5.png` | SuperJumpManager.starSprite (착지 별) | 흰색, 색입힘 |
| `twinkle_tall` | `Assets/Art/Rats/FX_SuperJump/sj_sparkle.png` | SuperJumpManager.sparkleSprite (하늘 끝 반짝) | 흰색 |
| `sparkle_small` | `Assets/Art/Rats/FX_SuperJump/sj_twinkle.png` | 여분 | 흰색 |
| `star_yellow` | `Assets/Art/Rats/FX_SuperJump/sj_star.png` | 여분 | 노랑 |
| `star_burst` | (없음) | 여분, 아직 Unity 에 안 넣음 | 흰색 |

UnityResources 사본(`FX_New/tint/star·twinkle`, `FX_SuperJump/sj_*`)도 같이 바꿈.
