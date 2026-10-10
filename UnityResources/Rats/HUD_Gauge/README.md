# HUD 게이지 · 미니맵 아이콘

Codex 로 만든 게임 HUD 조각 (투명 PNG, UI 키트와 같은 플랫). 유니티 `Assets/Art/HUD_Gauge/` 에 같은 파일 복사.
원본 시트·프롬프트·로그: `Sheets/` (`hud_gauge.png`, 참고 그림 = `UI_Kit/_미리보기.png`). 자홍 배경 → 덩어리별로 자름.

| 파일 | 용도 | 9-슬라이스 (L,B,R,T) |
|---|---|---|
| boss_bar_frame.png | 보스 체력바 틀 (홈 x 67~1356, y 26~95) | 80,60,80,60 · 배율 1.94 |
| boss_bar_fill.png | 보스 체력 채움 (흰색 줄무늬, 보스 색 입힘) · 깎인 자국 | 44,38,44,38 · 배율 2.35 |
| time_bar_frame.png | 제한시간 게이지 틀 | 60,52,60,52 · 배율 2.92 |
| time_bar_fill.png | 제한시간 채움 (흰색, RunTimer 가 색 입힘) | 40,34,40,34 · 배율 3.6 |
| hud_boss_emblem.png | 보스 체력바 왼쪽 엠블럼 (맞으면 흔들림) | - |
| hud_stopwatch.png | 제한시간 게이지 왼쪽 초시계 | - |
| mini_stairs.png | 미니맵 계단 | - |
| mini_boss.png | 미니맵 보스 (두근거림) | - |
| mini_view_frame.png | 미니맵 카메라 보는 곳 테두리 | 60,60,60,60 · 배율 6 |

쥐 마릿수 아이콘은 기존 `UI/ui_ratface.png`, 미니맵 판·칸은 UI 키트 `ui_card` · `ui_tile` · `ui_tile_lock` · `ui_dot` 재사용.
