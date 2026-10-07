# 새 쥐 (2026-10-06) — 찍찍 탐정 · 치즈 퐁듀 쥐 (+ 쥐록 홈즈 옷 입힘)

기획서 `기획서/20261006_프로젝트 NKK_캐릭터기획서_김규장.pptx` 의 신화 쥐 2종.

## v2 (현재 사용) — 사람 같던 v1 을 다시 그림
v1 은 다리가 사람 팔다리(소매·손가락·바지)로 나오고 몸통 대비 다리가 길어(앞발 = 몸통 폭 × 0.8) 키가 크고 사람처럼 보였음.
v2 는 기존 쥐 파츠를 참고 시트로 넣고 "네 발 쥐, 짧은 앞발 + 큰 허벅지 뒷발, 분홍 쥐발" 비율을 지정해서 Codex 로 다시 생성.

| 폴더/파일 | 내용 |
|---|---|
| `Sheets/newrats_parts_v2.png` | Codex 원본 시트 (자홍 배경, 5열×3행: 1줄 찍찍 탐정 · 2줄 치즈 퐁듀 · 3줄 쥐록 홈즈 트위드 코트판) |
| `Sheets/prompt_parts_v2.txt` · `log_parts_v2.txt` | 프롬프트 · 생성 로그 |
| `Sheets/ref_std_rats.png` · `ref_std_rats2.png` | 참고로 넣은 기존 쥐 파츠 (police·chef·blackrat / detective·mailman·nerd 를 자홍 배경에 원래 비율로 배치) |
| `../Parts/jjdetective/`, `../Parts/fondue/`, `../Parts/detective/` | 잘라낸 파츠. 몸통 폭을 기존 쥐 중앙값 412px 로 맞춤 (전 파츠 같은 배율) |
| `_미리보기.png` | 잘라낸 파츠 + 소품 |
| `_조립비교_v2.png` | 리그로 조립한 모습 비교 (police·chef·blackrat·mailman·detective·jjdetective·fondue, 같은 몸길이) |
| `Props/` | soccer_ball · cheese_bullet · cheese_puddle · magnifier · bowtie (v1 시트에서 자른 것 그대로) |
| `Sheets/v1/` | v1 보관: 원본 시트·프롬프트·로그·미리보기·잘랐던 파츠(`parts_*`)·`pivots_v1.json` |
| `../Parts_unused/detective_v1/` | 쥐록 홈즈 옛 파츠 (옷 없음) |

비율 (몸통 폭 대비, 기존 쥐 ≈ 머리 0.75~0.86 · 앞발 0.52~0.64 · 뒷발 0.66~0.8):
v2 찍찍 탐정 머리 0.74 · 앞발 0.56 · 뒷발 0.67 / 퐁듀 0.75 · 0.54 · 0.65 / 쥐록 홈즈 0.75 · 0.50 · 0.72.

- 종 코드: `jjdetective`(찍찍 탐정, 10078), `fondue`(치즈 퐁듀 쥐, 10079), `detective`(쥐록 홈즈, 10048).
- 같은 시트 사본: `../Sheets/group_jjdetective+fondue+detective_v2.png`
- 다시 자르기: `python Tools/slice_rat_parts.py UnityResources/Rats/NewRats/Sheets/newrats_parts_v2.png 3 jjdetective,fondue,detective`
  → 그 다음 파츠를 몸통 폭 412px 로 일괄 확대(LANCZOS, 피벗은 0~1 비율이라 그대로) → `python Tools/gen_sprite_pivots.py` → `Assets/Art/Rats/Parts/<id>/` 로 복사.
- 소품만 다시 자르기 (v1 시트): `python Tools/slice_rat_parts.py UnityResources/Rats/NewRats/Sheets/v1/newrats_parts_v1.png 3 -,-,- --props 2 soccer_ball,cheese_bullet,cheese_puddle,magnifier,bowtie`
