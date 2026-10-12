# 쥐 고유 컷씬 (뽑기, 전설 이상) — 2026-10-12: 아이스 퀸 시범 → 신화 15종

사용자: 전설 이상은 쥐마다 고유 컷씬 (컷 3장 · 약 3.5초, 처음 뽑았을 때만 `firstOnly`). 신화 먼저, 전설 11종은 나중에 결정.
코드 `Lobby/GachaIntro.cs`(틀) · `Lobby/GachaIntroCuts.cs`(컷 그림 방식) · `Lobby/CutFx.cs`(공통 전환 `PushOut`/`PunchIn` · 전환 값 `CutTransition` · 벡터풍 입자 `Blizzard`).
씬 Lobby `UI/GachaShow/Root/Intros/<이름>` (꺼 둠: `Cut` · `Fx`(`Dot` · `Flake` 템플릿, 꺼 둠) · `Flash`) + `GachaShow.intros` 에 등록. 입자는 런타임에 템플릿을 복제 (코드로 GameObject 안 만듦).

흐름: 하수구 공개 중 그 쥐 차례 → 하수구가 앞으로 빨려 들며 하얗게 (`GachaShow.introCut`) → 컷마다 [하얀 화면에서 크게 시작해 자리 잡음 → 천천히 확대·이동 → 줌 인 + 플래시] (`GachaIntroCuts.trans`) → 마지막 하얀 화면에서 카드 번쩍 공개 (`introRevealFlash`).
컷 구성 (모든 쥐 같은 틀): ① 배경·상황 (뒷모습 넓은 컷) ② 대표 기술 장면 ③ 클로즈업 포즈 + 상징 후광.

## 만드는 법 (새 쥐)
1. `Tools/gen_gacha_intro_prompts.py` 의 `RATS` 에 캐릭터 묘사 · 컷 3장 장면 추가 → `python Tools/gen_gacha_intro_prompts.py <code>` (참고 그림 `ref_style` = 뽑기 컷 그림체, `ref_parts` = 게임 파츠 한 줄)
2. `bash Tools/run_gacha_intro_codex.sh 4 <code...>` (동시 4개, 있는 그림은 건너뜀) → `<code>/Sheets/<code>_c1~3.png`
3. `<code>/` 와 `Assets/Art/Rats/GachaIntro/<code>/` 에 복사
4. `Tools/gen_gacha_intro_scene.py` 의 `RATS` 에 입자(바람 · 색/팔레트 · 크기 · 줄무늬 · 흔들림) · 컷2 효과음 · 입자 수 · 돌풍 → `python Tools/gen_gacha_intro_scene.py <출력.cs> <code...>` → 출력 코드를 Unity MCP `execute_code`(codedom)로 실행 (같은 이름은 지우고 다시 만듦, 씬 저장까지)

## 쥐별 입자
| 쥐 | 입자 |
|---|---|
| 아이스 퀸 | 눈보라 (흰 줄무늬 + 눈꽃) |
| 찌릿 햄찌 | 노란 스파크 (위로 · 크게 흔들림) |
| 쥐랜드 관광쥐 | 색종이 (5색, 천천히) |
| 쥐 황제 | 금빛 불꽃 가루 (아래로) |
| 파라오 생쥐 | 모래바람 (옆으로 빠르게) |
| 기사 쥐 | 빛 먼지 (천천히 위로) |
| 술탄 햄스터 | 금 반짝이 (아래로) |
| 외계인 생쥐 | 초록 광선 입자 (위로) |
| 스트리머 쥐 | 네온 점 (분홍·하늘·보라, 위로) |
| 다람쥐 | 빗줄기 (거의 수직, 길게) |
| 줴리 | 장미 꽃잎 (빨강·분홍) |
| 슈퍼 요리사 쥐 | 김 (흰색, 크게 천천히 위로) |
| 찍찍 탐정 | 밤비 (가늘게) |
| 치즈 퐁듀 쥐 | 치즈 방울 (노랑·주황) |
| 폭탄마 쥐 | 불티 (밝은 노랑·흰빛, 위로 — 주황 배경에 묻히지 않게) |

그림 원본·프롬프트·로그 = `<code>/Sheets/` (공통 문구 `_common.txt`).
