# 필살기 소품 추가분 (2026-10-06)
에이전트가 이식하며 "필요한 그림"으로 보고한 것. Codex 시트 `Sheets/ult_props.png` (자홍 3×3) · `prompt_ultprops.txt` · `log_ultprops.txt` · 참고 `ref_*.png`.

| 파일 | 쓰임 |
|---|---|
| `ult_pot.png` | 슈퍼 요리사 쥐 대왕 치즈 요리 냄비 |
| `ult_afro.png` | 찌릿 햄찌 과충전 뒤 탄 아프로 |
| `heaven_stairs.png` | 천사 생쥐 천국의 계단 |
| `excalibur_blade.png` | 기사 쥐 바위 없는 검 |
| `rat_knot.png` | 쥐왕 엉킨 꼬리 덩어리 |
| `ult_error_window.png` | 우주 친칠라 빅뱅.exe 오류 창 (글자 없음) |
| `ult_spotlight.png` | 줴리 핀 조명 |
| `egg_splat.png` | 줴리 야유 계란 얼룩 |
| `ult_gauge_bar.png` | 게이지 막대 틀 |

Unity 사본: `Assets/Art/Rats/UltProps/` (UltimateManager Fill Props 대상)

## 록 무대 (2026-10-06, 록스타 쥐 모시 피트 UltMosh)
Codex 시트 `Sheets_rock/rock_stage_sheet.png` (자홍 3×3) · `prompt_rockstage.txt` · `log_rockstage.txt` · 참고 `ref_*.png` · 자르기 `slice_rock.py` · `_미리보기_rock.png`.

| 파일 | 쓰임 |
|---|---|
| `rock_stage.png` | 무대 (나무 데크 + 검은 막 + 발조명) |
| `rock_speaker.png` | 스피커 스택 (좌우) |
| `rock_amp.png` | 기타 앰프 |
| `rock_truss.png` | 조명 트러스 (색 조명 4개) |
| `rock_drums.png` | 드럼 세트 |
| `rock_guitar_broken.png` | 박살 난 기타 |
| `rock_mic.png` | 마이크 스탠드 |
| `rock_lamp.png` | 스포트라이트 등 (예비) |
| `rock_pyro.png` | 불기둥 (박자·박살 때) |

# 줴리 감사합니다 무대 소품 (2026-10-06)
Codex 시트 `Sheets_jw/jw_stage.png` (자홍 2×2) · `prompt_jw_stage.txt` · `log_jw_stage.txt` · 참고 `ref_*.png` · 자르기 `slice_jw.py` (key_magenta + 연결 영역) · `_미리보기.png`

| 파일 | 쓰임 |
|---|---|
| `jw_dark_hole.png` | 핀 조명 어둠 (가운데 타원 구멍. 구멍 폭 0.49 · 높이 0.225) |
| `jw_dark.png` | 어둠 판 (구멍 둘레를 덮는 조각) |
| `jw_bubble.png` | 관객 말풍선 (글자 없음, 아래 꼬리) |
| `jw_iris_hole.png` | 아이리스 아웃 (가운데 원 구멍. 폭 0.388 · 높이 0.406) |

- 2026-10-06 2차: `jw_dark_hole`·`jw_iris_hole` 은 코드에서 더 안 씀 (어둠은 `jw_dark` 판 + SpriteMask(`dot`) 로 바꿈). `jw_bubble` 은 9칸 늘이기로 씀.
- `Sheets_jwtux/` : 줴리 턱시도 파츠(몸통·앞다리·뒷다리) Codex 요청 — 사용량 한도로 실패 (log_jw_tux.txt). 프롬프트·참고 그림은 남겨 둠, 한도 풀리면 재실행.

# 줴리 턱시도 파츠 (2026-10-07)
Codex 시트 `Sheets_jwtux/jw_tux.png` (자홍 3칸) · `prompt_jw_tux.txt` · `log_jw_tux.txt` · 참고 `ref_*.png`(줴리 원래 파츠) · 자르기 `slice_jwtux.py` (가장 큰 덩어리만, 원래 파츠와 같은 픽셀 크기로 맞춤) · `_미리보기.png` (위: 원래, 오른쪽: 턱시도)

| 파일 | 쓰임 |
|---|---|
| `jwt_torso.png` | 줴리 감사합니다 중 몸통 (재킷 + 흰 셔츠) |
| `jwt_front.png` | 앞다리 (소매 + 흰 소맷부리) |
| `jwt_back.png` | 뒷다리 (바지) |
Unity 사본: `Assets/Art/Rats/UltProps/` (.meta 는 원래 파츠 것을 복사 → 피벗 같음). 코드가 필살기 동안 리그 그림만 바꿔 끼우고 끝나면 되돌림.
