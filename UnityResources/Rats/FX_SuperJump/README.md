# 슈퍼 점프 이펙트 (2026-10-06)

슈퍼 점프(SuperJumpManager) 전용. 웹 drawSuperJump/updateSuperJump 와 똑같이 보이게 다시 만듦 (2차).
Codex 원본: `Sheets/` — `fx_sj2.png`(자홍 3×2) · `sj_band_sheet.png` · `sj_lines_a/b.png` · `sj_vignette.png`(흑백 → 밝기를 알파로) · `prompt_sj2_a/b.txt` · `log_sj2_a/b.txt`. 1차 시트 `fx_superjump.png` 도 보관.
자르기: `Tools/slice_rat_parts.py` 의 `key_magenta` + 칸별 crop.

| 파일 | 쓰임 |
|---|---|
| `sj_band.png` | 컷인 만화 칸 (노랑 + 망점 + 굵은 테두리) → HUD `CutIn/Band` |
| `sj_lines_a.png`, `sj_lines_b.png` | 집중선 (0.05초마다 바꿔 깜빡) → `cutLineFrames` |
| `sj_vignette.png` | 기 모으기 가장자리 빛 (가운데 투명) → HUD `ChargeGlow` |
| `sj_dot.png` | 흰 동그라미: 기 모으기 불꽃 · 발사/낙하 꼬리 · 발사 자국 → `dotSprite` |
| `sj_star5.png` | 흰 별: 착지 때 튀는 별 → `starSprite` |
| `sj_sparkle.png` | 흰 4각 반짝: 하늘 끝 반짝 → `sparkleSprite` |
| `sj_line.png` | 흰 직선: 낙하 속도선 · 컷인 발밑 선 → `lineSprite` |
| `sj_crater.png` | 착지 바닥 자국 (움푹 + 금 9줄) → `stampSprite` |
| `sj_spiky.png` | '쿠과과광!!!' 뒤 만화 터짐 → HUD `ImpactText/Burst` |
| `sj_puff.png`, `sj_ring.png`, `sj_impact.png`, `sj_twinkle.png`, `sj_star.png`, `sj_streak.png` | 여분 (지금 코드에서 안 씀) |

Unity 사본: `Assets/Art/Rats/FX_SuperJump/`

- 2026-10-07: `sj_star5`·`sj_sparkle`·`sj_twinkle`·`sj_star` 를 게임 그림체로 다시 그림 → `../FX_Stars/README.md` (옛 그림 `../FX_Stars/old/`)
