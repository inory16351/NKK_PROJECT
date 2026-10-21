# 메이드 쥐 그림 (2026-10-20)

사용자 참고 그림(은발 메이드, `Sheets/ref_maid_concept.png`)은 아이디어만, 그림체는 게임 쥐 플랫(Codex). 시간 능력자라는 게 바로 보이게 **큰 금색 회중시계**가 핵심 소품 (사용자).

| id | 쥐 | 그림 |
|---|---|---|
| 10088 | 메이드 쥐 `maid` (신화 · 7훈장 · 공격력 472) | 파츠 `../Parts/maid/` (흰 털 · 하얀 머리 장식 + 금색 시계 장식 · 파란 메이드 옷 · 꼬리 리본 + 금색 시곗바늘 장식 · **뒷다리 허벅지에 큰 금색 회중시계**) |

- 리깅은 다른 쥐와 똑같은 5조각 방식 (사용자: "기존의 다른 리깅 방식과 동일하게"). 게임 리그는 뒷다리가 몸통 뒤쪽 절반 위에 그려져서, 몸통에 그린 시계는 가려짐 → 시계를 **뒷다리 허벅지(치마 허리춤에 매단 모양)** 로 옮김. 미리보기 `_메이드쥐_리그_미리보기.png` (게임 그리는 순서 그대로 조립 · 아래 작은 것 = 게임 크기).
- 옛 시안: `Sheets/_v1_small_watch`(은회색 · 작은 시계) · `_v2_side_watch`(시계가 몸통 옆, 뒷다리에 가려짐) · `_v3_torso_watch`(몸통 가운데 시계 — 기존 리그 순서에선 가려짐).

| 파일 | 내용 |
|---|---|
| `Sheets/maid_parts.png` | 파츠 시트 (2줄 = 쓴 것: 시계 더 큼) → `python Tools/slice_rat_parts.py <시트> 3 -,maid,-` (scipy 필요: `uv run --no-project --with scipy --with numpy --with pillow python …`) → 몸통 폭 412px 로 맞춤 → `python Tools/clean_part_edges.py Parts/maid/*.png`(흰 털 둘레 자홍 테두리 제거) → `python Tools/gen_sprite_pivots.py` |
| `Sheets/maid_icons.png` → `Icons/` | rs_20088 완벽한 뒷정리 · rs_21088 티 타임 · ult_30036 살인 인형 · ach_40036 업적 (+ 안 쓰는 ult_30036_alt · maid_headdress) — `python Tools/slice_grid_all.py <시트> 3 2 Icons <이름들> 256` |
| `Sheets/maid_props.png` → `Props/` | maid_knife(칼끝 오른쪽) · maid_clock(바늘 없는 시계판) · maid_hand_long/short(위를 가리킴) · maid_teatray(티 타임 스티커) · maid_clockring — 필살기 `UltTimeStop` 소품 |

유니티 사본: `Assets/Art/Rats/Parts/maid` · `RatSkillIcons` · `UltIcons` · `AchvIcons` · `NewRats/maid_*`.
초필살기 '메이드의 세계' 그림 = `../SuperUlt/maid/` (README 는 SuperUlt 쪽).
