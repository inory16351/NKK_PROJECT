# Lobby_CatLab — 고양이 연구소 아지트 배경 (4훈장부터)

16~30층 인게임이 고양이 연구소라서 4훈장(탈출 설계자, 최고 18층)을 달면 아지트 배경이 고양이 연구소 모습으로 바뀝니다.

| 파일 | 내용 |
|---|---|
| `lobby_catlab_bg.png` | 1536×1024. `UI/lobby_bg.png` 와 크기·배치가 같음 (쥐구멍·화이트보드·통조림 탁자·책·쳇바퀴 빈자리·치즈 선반·침대·판자 바닥 높이) → `Assets/Art/Rats/UI/lobby_catlab_bg.png` (임포트 설정은 lobby_bg 를 복사) |
| `Sheets/prompt_lobby_catlab.txt` · `log_lobby_catlab.txt` | Codex 프롬프트 · 로그 |
| `Sheets/ref_layout_lobby_bg.png` | 배치 참고 (원래 아지트) |
| `Sheets/ref_style_catlab_props.png` · `ref_style_catlab_wall.png` | 화풍 참고: 인게임 고양이 연구소 (Untitled Goose Game 식 플랫) |

연결: Lobby 씬 `Home` → `LobbyHome.tierLooks` (fromTier 4 → lobby_catlab_bg). `LobbyHome.Refresh` 에서 교체 (로비 들어올 때 · 페이지에서 돌아올 때 · 훈장 승급 뒤). 다음 구간(31층~ 등) 배경도 같은 목록에 줄을 추가하면 됩니다.
