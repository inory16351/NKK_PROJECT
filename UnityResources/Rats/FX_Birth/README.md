# FX_Birth — 번식 탄생 연출 (2026-11-05)

종마다 처음 번식으로 태어날 때 한 번 나오는 연출(`Scripts/Rats/BirthCutscene.cs`, Game 씬 `HUD/BirthCutscene`)에 쓰는 그림.

| 파일 | 내용 |
|---|---|
| `birth_heart.png` | 큰 분홍 하트 (부모가 합쳐진 뒤 등장 → `NKK/UIDissolve` 로 녹듯 사라짐) |
| `birth_rays.png` | 하트 뒤 햇살 (정사각형, 가운데 기준 회전) |
| `birth_sparkle.png` | 반짝이 별 |
| `birth_heart_small.png` | 작은 하트 (반짝이 사이 장식) |

- 원본: `Sheets/birth_fx.png` (Codex, 자홍 배경) · 프롬프트 `Sheets/prompt_birth_fx.txt` · 참고 `Sheets/ref_heart.png` (= FX_Heart/heart_pink) · 로그 `Sheets/log_birth_fx.txt`
- 자르기: `python Tools/slice_birth_fx.py` → 여기 + `Assets/Art/FX_Birth/` 로 복사
- 디졸브 셰이더: `Assets/Art/FX_Birth/UIDissolve.shader` (노이즈는 셰이더 안 계산)

## 웹 미리보기 (2026-11-05)
- `web/birth.html` — 연출만 가볍게 반복 (클릭 = 다른 쥐). `web/birth_tune.html` — 슬라이더로 값 맞추는 판 (이름 = 인스펙터 이름, JSON 복사). 둘 다 유니티 `BirthCutscene` · `RatPortrait` 와 같은 계산 + 같은 노이즈 디졸브.
- 띄우기: `.claude/launch.json` 의 `rat-rig-web` (포트 8773, 루트 = UnityResources/Rats) → `http://localhost:8773/FX_Birth/web/birth.html`
- 쥐 파츠 = `web/parts/` · `web/rats.json` (RatArtLibrary 85마리, Unity 에서 뽑음 — 파츠가 바뀌면 다시 뽑기)
