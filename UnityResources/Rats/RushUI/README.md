# 총공격 UI (2026-11-05, Codex)

클릭 총공격 쿨타임 게이지(마우스 포인터를 둘러싼 원) + 클릭 위치 표시(화살표 4개가 모여듦, 1안).
원본·요청 글·로그 = `Sheets/` (참고 그림 ref_uikit · ref_cursor · ref_stopwatch).
`python Tools/slice_rush_ui.py` → 키잉 · 자르기 (채움 원은 빈 원의 크림색 띠 모양으로 잘라 겹침) → `Assets/Resources/RushUI/` 로 복사.

- rush_ring_frame 빈 원 (갈색 테두리 + 크림 띠) · rush_ring_fill 밝은 초록 채움 (2026-11-05 사용자: 가시성, Codex 주황 그림 색만 바꿈 · 주황 원본 = rush_ring_fill_orange) · rush_ring_ready 다 참 (치즈색)
- 클릭 표시는 1초(`markTime`)만 보이고, 휠로 넓게 봐도 화면에서 같은 크기. 게이지는 클릭 0.5초(`gaugeDelay`) 뒤 빈 원으로 나와 남은 쿨타임 동안 참.
- rush_knob 진행 끝 점 · rush_arrow 화살표 (왼쪽을 가리킴, 코드에서 4방향으로 돌림) · rush_center 가운데 원

코드 = `Scripts/Rats/RushUI.cs` (RatManager.Start 에서 붙음, 수치는 RushUI 인스펙터). 임포트 = Sprite · 압축 없음.
