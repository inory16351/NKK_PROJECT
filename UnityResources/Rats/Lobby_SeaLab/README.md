# Lobby_SeaLab — 수중 연구소 아지트 배경 (11훈장부터)

61~75층 인게임이 수중 연구소(구간 70008, 가구 테마 Sea)라서, 11훈장(연구소 점령자, 조건 최고 64층)을 달면 아지트 배경이 수중 연구소 모습으로 바뀝니다. (4~5 고양이 연구소 · 6~8 지구과학 연구소 · 9~10 공사 현장)

| 파일 | 내용 |
|---|---|
| `lobby_sealab_bg.png` | 1536×1024. `UI/lobby_bg.png` · 고양이 연구소판과 크기·배치가 같음 (쥐구멍 = 바다가 보이는 둥근 창 · 슬레이트 보드 + 바다 생물 사진 · 빨간 대야 탁자 · 부표 의자 · 방수 노트·해마 표본병 · 쳇바퀴 빈자리 · 철 선반 치즈·불가사리 병 · 조개 침대 · 위아래 배관·밸브·둥근 창 너머 산호와 모래) → `Assets/Art/Rats/UI/lobby_sealab_bg.png` (임포트 설정은 lobby_bg 를 복사) |
| `Sheets/prompt_lobby_sealab.txt` · `log_lobby_sealab.txt` | 쓴 판(v2) 프롬프트 · 로그 — **고양이 연구소판을 그대로 두고 다시 칠하기(reskin)** 로 지시 |
| `Sheets/lobby_sealab_bg_v1.png` · `prompt_lobby_sealab_v1.txt` · `log_lobby_sealab_v1.txt` | 안 쓴 첫 판: 화풍은 좋았지만 바닥(갑판)이 원래보다 위 · 둥근 창이 10% 위 → 쥐가 갑판 앞면을 걷게 돼서 버림 |
| `Sheets/ref_*` | 배치 참고(원래 아지트 · 고양이 연구소판) · 테마 참고(인게임 수중 연구소 물건·벽) |

연결: Lobby 씬 `Home` → `LobbyHome.tierLooks` (4 catlab · 6 earthlab · 9 buildsite · **11 sealab**).
다음 구간 배경도 배치가 어긋나면 v2 처럼 "고양이 연구소판 reskin" 프롬프트가 잘 맞음.
