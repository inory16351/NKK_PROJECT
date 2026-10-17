# 마우스 포인터 (2026-10-17, Codex)

쥐 머리 화살표 (흰 실험쥐, 코끝 = 클릭 지점). 원본·프롬프트·로그 = `Sheets/` (참고 그림 ref_uikit · ref_labrat).
`python Tools/slice_cursor.py 48` → 키잉 · 두 장 코끝 맞춤 · 48px 로 줄임 → `Assets/Art/Cursor/` 에 복사.

- cursor_normal.png 평소 포인터 (눈 뜸)
- cursor_pressed.png 누르고 있을 때 (눈 감고 살짝 눌림)
- `*_big.png` 줄이기 전 큰 그림 (크기를 바꿀 때 다시 자르지 않아도 됨)

유니티: 텍스처 형식 Cursor · Read/Write 켬 · 압축 없음. 설정 = `Assets/Resources/CursorSkin.asset` (그림 · 끝점 hotspot (1, 1) · forceSoftware), 코드 = `Scripts/Core/CursorSkin.cs` (게임이 켜지면 저절로 붙음, 모든 씬).
