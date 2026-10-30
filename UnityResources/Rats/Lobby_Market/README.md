# 지하 도시 (쥐구멍 암시장 · 비밀 실험실) — 2026-10-12

아지트 맨홀 뚜껑 → 아래 하수구 방(지하 도시). 상점 기획서 · 뽑기 기획서 (기획서/20261012_*_Ver02.pptx).
그림체: 배경·소품 = 로비 배경(`UI/lobby_bg.png`) 그림체 — **단순한 큰 면 · 무광 · 질감/녹/리벳 없음** (사용자 2026-10-12: 질감 많은 건물은 로비와 안 맞음. 프롬프트에 로비 물건 잘라 낸 `Sheets/v4_city/ref_lobby_objects.png` 를 붙이고 "이것과 똑같이" 로 고정) / 브로커 쥐 = 게임 쥐·튜토리얼 초상화와 같은 플랫.

## 지금 쓰는 판 (v4 지하 도시, 2026-10-12)

| 파일 | 내용 |
|---|---|
| `lm_bg.png` | 지하 도시 배경 (`Sheets/v4_city/city_bg.png`): 위 = 로비와 같은 들보·배관, 천장 가운데 맨홀 빛 · 가운데 = 흐릿한 지하 도시 · 62~74% = 빈 돌 보도 (물건·쥐가 서는 곳) · 아래 = 물길 |
| `lm_stall` · `lm_ladder` · `lm_crates` · `lm_sign_up` · `lm_lantern`(숟가락 가로등) · `lm_labbench`(안 씀, 실험대) | 기물 (`Sheets/v4_city/city_props.png` 3×2) |
| `lm_lab.png` · `lm_lab_blown.png` | **비밀 실험실 건물** (뽑기, 피벗 = 가운데 — 프로젝트 임포터가 피벗을 덮어써서 바닥 기준으로 놓음, 터진 그림으로 바꿀 땐 코드가 바닥선을 맞춤) · 뽑기 연출 때 터진 모습 (`Sheets/v4_city/city_lab.png`) |
| `NightMarket/nm_house_milk` · `nm_house_can` · `nm_tent_teapot` · `nm_stall_noodle` · `nm_lanterns` · `md_pennants` · `md_poster_cat` · `md_loot` | 장식 (`Sheets/v4_city/city_decor.png` 4×2), 같은 이름으로 덮어써서 씬 연결 유지 |
| `_old_nightmarket/` | 바꾸기 전 야시장 v3 그림 (배경·기물·장식) |
| `lobby_manhole_closed/open.png` | 아지트 바닥 맨홀 (그대로) |
| `lm_broker_*` · `../TutoPortraits/tuto_broker_*` | 브로커 쥐 (그대로) |
| `lm_blender` · `lm_lid` · `lm_shard` · `lm_splash` · `lm_capsule` | 옛 믹서기 연출 (안 씀, 연출은 `../GachaLab/`) |

씬 Lobby `Alley` (아지트 아래 y -10.24, 로컬 x -7.68~7.68, 보도 바닥 y ≈ -1.72): 노점 x -3.4 · 브로커 -1.75 · 사다리 0 · 숟가락 가로등 1.45 · **비밀 실험실 3.5 (`Alley/SecretLab`, 자식 `Door` = 빨려 드는 곳)** · 찻주전자 5.3 · 우유곽 집 6.35 · 상자 7.25 · 왼쪽 깡통 가게 -5.75 · 분식 -7.0. 누르는 칸 `Home/Hots/Alley/Hot_market` · `Hot_gacha` 는 기물 크기에 맞춤. 팻말 이름 '비밀 실험실' (뽑기 화면 제목은 '유전자 추출기' 그대로).
배치 다시 할 땐 바닥 기준(아래 가운데 x, 바닥 y, 높이)으로 놓을 것 — 그림마다 여백이 달라 크기만 맞추면 떠 보임.

## 미니 게임기 (2026-10-30)

`lm_arcade.png` = 실험실 · 증강 연구소와 같은 칠한 그림체 (`Sheets/arcade/v2_city/arcade.png` 키잉, 높이 860px). 처음 플랫 그림 = `Sheets/arcade/lm_arcade_flat_old.png`. 씬 `Alley/Arcade` x −5.48 · 바닥 −1.73 · 높이 1.8.
