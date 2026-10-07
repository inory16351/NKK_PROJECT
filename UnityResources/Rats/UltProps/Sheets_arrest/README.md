# 쥐랜드 체포 소품

2026-10-07, Codex 내장 image_gen으로 생성한 **단일 4열 × 2행 시트**. 기존 `lawyer_rat_a.png`, `guard_rat_a.png`를 스타일 참고로 사용했다. CLI/API 생성은 사용하지 않았다.

- 원본: `arrest.png` (1774 × 887, 자홍 배경, 글자 없음)
- 실제 생성 프롬프트: `prompt_arrest.txt`
- 투명 배경 결과 미리보기: `_preview.png`
- 재현 스크립트: `slice_arrest.ps1`

| 칸 | 출력 이름 | 내용 |
|---|---|---|
| 위 1 | police_rat_a | 왼쪽을 보는 방패·곤봉 경찰 |
| 위 2 | police_rat_b | 같은 경찰의 달리기 |
| 위 3 | police_rat_cuff | 앞으로 수갑을 내미는 경찰 |
| 위 4 | lawyer_rat_point | 손가락으로 가리키는 변호사 |
| 아래 1 | lawyer_rat_doc | 빈 서류를 든 변호사 |
| 아래 2 | prop_handcuffs | 수갑 소품 |
| 아래 3 | police_tape | 글자 없는 노란 통제선 |
| 아래 4 | police_siren | 빨강·파랑 경광등 |

## 분할

`Tools/slice_rat_parts.py::key_magenta`의 자홍 거리, 60~120 알파 전이, RGB 탈색 계산을 C#으로 옮겨 PowerShell/System.Drawing에서 실행한다. 가장자리 자홍 잔색은 인접 내부 색으로 복구한다. Python 이미지 라이브러리가 없는 환경에서도 실행 가능하다.

8방향 연결 영역을 찾아 중심점이 속한 칸에 **전체 영역**을 배정한다. 면적 8픽셀 이상인 조각을 모두 보존하고 6픽셀 여백을 둔다. 따라서 아래 첫 칸의 꼬리처럼 격자 경계를 조금 넘은 부분도 잘리지 않는다. 캐릭터 5종은 442 × 408 공통 캔버스와 같은 바닥선을 사용한다.

실행:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File UnityResources/Rats/UltProps/Sheets_arrest/slice_arrest.ps1
```

PNG 8종은 상위 `UnityResources/Rats/UltProps/`에 저장하고 `Unity_Making/NKK_Project/Assets/Art/Rats/UltProps/`로 복사한다. Unity 메타는 기존 `lawyer_rat_a.png.meta`의 Sprite 설정을 복제하고 각 파일에 새 무작위 32자리 GUID를 부여했다. 재분할 시 GUID는 유지한다.

Unity에서 사용할 때는 `UltimateManager > Fill Props`로 목록을 갱신한다. 에디터 실행·조작은 하지 않았다.
