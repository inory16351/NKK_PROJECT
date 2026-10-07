# UI 키트

Codex 로 만든 UI 조각 (투명 PNG, 쥐 그림과 같은 Untitled Goose Game 식 플랫). 유니티 `Assets/Art/UI_Kit/` 에 같은 파일 복사.

- 늘려 쓰는 것(카드·판자·테이프·막대·말풍선 등)은 유니티에서 9-슬라이스 테두리 적용: 메뉴 `NKK/Apply UI Kit Borders` (`Assets/Editor/UiKitBorders.cs`)
- `ui_bar_white.png` = `ui_bar_yellow` 를 흰색으로 바꾼 것 (코드에서 등급 색을 입힘)
- 말풍선은 늘릴 때 꼬리가 같이 늘어나지 않게 몸통·꼬리를 따로 잘라 둠
- `Sheets/` : Codex 원본 시트(`ui_kit_a.png` 4×3, `ui_kit_b.png` 4×4) · 프롬프트 · 로그. 다시 자르기: `python Tools/slice_sheet.py <시트> <열> <행> <출력> <이름,...> --blobs --pad 2`

| 파일 | 용도 |
|---|---|
| ui_bar_bg.png | 진행 막대 바탕 |
| ui_bar_green.png | 진행 막대 (초록) |
| ui_bar_white.png | 진행 막대 (흰색, 색 입히기) |
| ui_bar_yellow.png | 진행 막대 (노랑) |
| ui_btn_round.png | 둥근 버튼 |
| ui_btn_round_green.png | 둥근 버튼 (초록) |
| ui_bubble.png | 말풍선 몸통 |
| ui_bubble_tail.png | 말풍선 꼬리 |
| ui_card.png | 종이 카드 (페이지 카드) |
| ui_cork.png | 코르크판 |
| ui_dot.png | 알림 점 |
| ui_key.png | 핵심 노드 (마름모) |
| ui_key_on.png | 핵심 노드 (찍음) |
| ui_node.png | 스킬 노드 |
| ui_node_lock.png | 스킬 노드 (잠김) |
| ui_node_on.png | 스킬 노드 (찍음) |
| ui_pill.png | 재화 알약 |
| ui_pin.png | 압정 |
| ui_plank.png | 나무 판자 버튼 |
| ui_plank_gray.png | 회색 판자 (비활성) |
| ui_plank_green.png | 초록 판자 (출발) |
| ui_plank_red.png | 빨강 판자 (위험) |
| ui_ribbon.png | 리본 라벨 |
| ui_rope.png | 끈 |
| ui_sign.png | 매다는 팻말 |
| ui_tape.png | 마스킹 테이프 (탭) |
| ui_tile.png | 층 타일 |
| ui_tile_lock.png | 층 타일 (잠김) |
| ui_tile_on.png | 층 타일 (선택) |
| ui_topbar.png | 상단 나무 띠 |
