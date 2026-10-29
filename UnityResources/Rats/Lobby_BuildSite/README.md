# Lobby_BuildSite — 공사 현장 아지트 배경 (9훈장부터)

46~60층 인게임이 연구소 확장 공사 현장(구간 70007, 가구 테마 Build)이라서, 9훈장(지하 제국 총독, 조건 최고 50층)을 달면 아지트 배경이 공사 현장 모습으로 바뀝니다. (4~5훈장 = 고양이 연구소 `Lobby_CatLab/` · 6~8훈장 = 지구과학 연구소 `Lobby_EarthLab/`)

| 파일 | 내용 |
|---|---|
| `lobby_buildsite_bg.png` | 1536×1024. `UI/lobby_bg.png` 와 크기·배치가 같음 (쥐구멍 = 굴착기·비계가 보임 · 청사진 보드 + 공사 사진 · 전선 드럼 탁자 · 페인트통 의자 · 청사진 두루마리·서류철·흙손 · 쳇바퀴 빈자리 · 비계 치즈 선반(콘·벽돌) · 안전모 침대 · 위 철골 들보·철근 / 아래 콘크리트 기초·배관·시멘트 포대) → `Assets/Art/Rats/UI/lobby_buildsite_bg.png` (임포트 설정은 lobby_bg 를 복사) |
| `Sheets/prompt_lobby_buildsite.txt` · `log_lobby_buildsite.txt` | Codex 프롬프트 · 로그 |
| `Sheets/ref_layout_lobby_bg.png` | 배치 참고 (원래 아지트) |
| `Sheets/ref_flat_lobby_catlab.png` | 플랫 아지트 참고 (고양이 연구소판, 화풍·배치 맞춤용) |
| `Sheets/ref_style_build_props.png` · `ref_style_build_wall.png` | 테마 참고: 인게임 공사 현장 물건·벽 |

연결: Lobby 씬 `Home` → `LobbyHome.tierLooks` (4 → catlab · 6 → earthlab · **9 → buildsite**).
참고: 윗쪽 철골 들보가 원래 배경보다 조금 위(13~19%)에 있어 훈장 줄이 들보 위에 걸림 — 화면 확인함.
