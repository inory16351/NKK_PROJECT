# 빔·번개·베기 이펙트 (2026-10-06)
사용자 피드백 "번개 이펙트, 사무라이쥐, 레이저 빔 이상함" 대응. Codex 시트 `Sheets/fx_beam.png` (자홍 3×3) · `prompt_fxbeam.txt` · `log_fxbeam.txt` · 참고 `ref_muzzle.png` · 자르기 `slice_fxbeam.py` (key_magenta + scipy 연결 영역, 몸통은 가운데 세로줄 복제로 완전 균일).
전부 **흰색 + 연회색 두 톤** → 게임에서 색을 곱해서(tint) 씀. 흰 심지는 같은 그림을 얇게 한 장 더 겹침.

| 파일 | 쓰임 |
|---|---|
| `fx_bolt.png` | 하늘 번개 (세로, FxManager.Bolt) |
| `fx_bolt_seg.png` | 짧은 번개 조각 (꺾인 번개 줄에 이어 붙임, FxManager.BoltLine) |
| `fx_spark.png` | 작은 전기 불꽃 (FxManager.Spark) |
| `fx_beam_body.png` | 레이저 몸통 띠 (늘려 씀, FxManager.Beam / BigBeam, 슈퍼 생쥐) |
| `fx_beam_flare.png` | 레이저 총구 번쩍 (오른쪽을 향함) |
| `fx_beam_hit.png` | 레이저 맞은 자리 튐 |
| `fx_slash_arc.png` | 칼 베기 초승달 (FxManager.Slash, 사무라이 쥐) |
| `fx_slash_streak.png` | 일자 베기 |
| `fx_slash_cross.png` | X 베기 |

Unity 사본: `Assets/Art/Rats/FX_Beam/` (FxManager 인스펙터 연결용) + `Assets/Art/Rats/FX/` (UltimateManager Fill Props 소품용)
