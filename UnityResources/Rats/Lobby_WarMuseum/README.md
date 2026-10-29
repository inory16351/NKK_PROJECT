# Lobby_WarMuseum — 2차 세계대전 박물관 아지트 배경 (13훈장부터)

76~90층 인게임이 2차대전 박물관(구간 70009, 가구 테마 War)이라서, 13훈장(쥐 공화국 의장, 조건 최고 76층)을 달면 아지트 배경이 박물관 모습으로 바뀝니다. (4~5 고양이 연구소 · 6~8 지구과학 연구소 · 9~10 공사 현장 · 11~12 수중 연구소)

| 파일 | 내용 |
|---|---|
| `lobby_warmuseum_bg.png` | 1536×1024. `UI/lobby_bg.png` · 고양이 연구소판과 크기·배치가 같음 (쥐구멍 = 지프·낙하산 전시실이 보임 · 초록 칠판 + 철모·무전기·비행기·낙하산 사진과 지도 · 올리브 전투식량 깡통 탁자 · 깡통 의자 · 야전 노트·지도 가방·훈장 · 쳇바퀴 빈자리 · 나무 진열 선반 치즈·비행기 모형·상자 · 철모 침대 · 위 나무 들보·매달린 비행기 모형·말린 천막 / 아래 배관·나무 상자) → `Assets/Art/Rats/UI/lobby_warmuseum_bg.png` (임포트 설정은 lobby_bg 를 복사) |
| `Sheets/prompt_lobby_warmuseum.txt` · `log_lobby_warmuseum.txt` | Codex 프롬프트 · 로그 — 고양이 연구소판 reskin 방식. 실제 국기·부대 마크·무기·사람은 넣지 않게 지시 |
| `Sheets/ref_*` | 배치 참고(원래 아지트 · 고양이 연구소판) · 테마 참고(인게임 2차대전 박물관 물건·벽) |

연결: Lobby 씬 `Home` → `LobbyHome.tierLooks` (4 catlab · 6 earthlab · 9 buildsite · 11 sealab · **13 warmuseum**).
