# Lobby_EarthLab — 지구과학 연구소 아지트 배경 (6훈장부터)

31~45층 인게임이 지구과학 연구소(구간 70006, 가구 테마 Dino)라서, 6훈장(반란 지휘관, 조건 최고 31층)을 달면 아지트 배경이 지구과학 연구소 모습으로 바뀝니다. (4~5훈장 = 고양이 연구소 `Lobby_CatLab/`)

| 파일 | 내용 |
|---|---|
| `lobby_earthlab_bg.png` | 1536×1024. `UI/lobby_bg.png` 와 크기·배치가 같음 (쥐구멍 = 공룡 뼈 전시실이 보임 · 칠판 + 화석 사진 · 암모나이트 돌 탁자 · 야외 조사 노트·붓·호박 · 쳇바퀴 빈자리 · 치즈·화석 선반 · 공룡알 껍데기 침대 · 위아래 지층과 묻힌 화석) → `Assets/Art/Rats/UI/lobby_earthlab_bg.png` (임포트 설정은 lobby_bg 를 복사) |
| `Sheets/prompt_lobby_earthlab.txt` · `log_lobby_earthlab.txt` | Codex 프롬프트 · 로그 |
| `Sheets/ref_layout_lobby_bg.png` | 배치 참고 (원래 아지트) |
| `Sheets/ref_flat_lobby_catlab.png` | 플랫 아지트 참고 (고양이 연구소판, 화풍·배치 맞춤용) |
| `Sheets/ref_style_earthlab_props.png` · `ref_style_earthlab_wall.png` | 테마 참고: 인게임 지구과학 연구소 물건·벽 |

연결: Lobby 씬 `Home` → `LobbyHome.tierLooks` (fromTier 4 → lobby_catlab_bg · **fromTier 6 → lobby_earthlab_bg**).
