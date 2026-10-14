# 배경음악 (2026-10-10, Gemini Pro · 음악 만들기(Lyria), 연주곡 · 표준 길이)
목록·우선순위 = `사운드 목록.md`. 유니티 복사본 = `Assets/Audio/BGM/` (재생 = MusicManager + 씬마다 SceneMusic).

| 파일 | 쓰는 곳 | 길이 | 프롬프트 요지 |
|---|---|---|---|
| BGM_Title.mp3 | 타이틀 · 슬롯 고르기 · 오프닝 | 2:59 | 장난스러운 오케스트라, 피치카토 · 마림바 · 글로켄슈필 · 바순 · 휘파람, 카툰 하이스트, 120 BPM |
| BGM_Lobby.mp3 | 로비 (아지트) | 2:56 | 아늑한 어쿠스틱, 우쿨렐레 · 기타 · 글로켄슈필 · 브러시 드럼, 90 BPM |
| BGM_Floor_Lab.mp3 | 1~4층 지하 비밀 실험실 | 2:58 | 코믹 소동, 스타카토 금관 · 콘트라베이스 · 실로폰, 140 BPM |
| BGM_Boss.mp3 | 보스전 | 3:02 | 우스꽝스럽지만 긴박한 오케스트라 + 록 드럼, 160 BPM |
| BGM_Story_Epic.mp3 | 오프닝 컷씬 | 2:57 | 낮은 현악 · 팀파니 · 허밍 합창 → 금관 영웅 테마, 130 BPM |
| BGM_Floor_Breeding.mp3 | 5~9층 실험동물 사육동 | 2:58 | 밴조 · 피치카토 · 클라리넷 · 우드블록, 145 BPM |
| BGM_Floor_Pharma.mp3 | 10~14층 약품 연구동 | 2:56 | 보글보글 신스 아르페지오 · 첼레스타 · 뮤트 트럼펫, 130 BPM |
| BGM_Floor_Admin.mp3 | 15~19층 전산·관리동 | 2:59 | 레트로 칩튠 + 금관 · 슬랩 베이스, 135 BPM |
| BGM_Floor_Director.mp3 | 20층~ 연구소장 층 | 2:50 | 어두운 오케스트라 · 파이프 오르간 · 팀파니 · 금관, 140 BPM |
| BGM_Hurry.mp3 | 남은 시간 30초 이하 (보스전 제외) | 2:38 | 째깍째깍 · 빠른 피치카토 · 다급한 금관, 175 BPM |
| BGM_Boss_Final.mp3 | 30층 이상 보스전 (우주 고양이 사령관) | 2:59 | 우주풍 오케스트라 · 합창 · 레트로 신스, 160 BPM |
| BGM_Alley.mp3 | 로비 지하 도시 (2026-10-12, SceneMusic.alleyClip) | 3:01 | 피치카토 · 아코디언 · 글로켄슈필 · 클라리넷 · 브러시 드럼, 살랑 스윙 |
| BGM_Gacha.mp3 | 유전자 뽑기 비밀 실험실 연출 (2026-10-12, SceneMusic.gachaClip) | 2:59 | 보글보글 긴장 → 신나는 팡파르, 피치카토 · 신스 플럭 · 글로켄슈필 · 스네어 롤 · 금관 |

- 곡 끝이 반복용 이음매가 아니라 끝맺음이 있음 → 지금은 처음부터 다시 재생 (나중에 반복 구간 편집 필요할 수 있음)
- Gemini 는 한 대화에서 여러 곡을 뽑으면 '생성 중'에서 멈출 때가 많음 → 새 대화에서 짧은 프롬프트로
- 모든 파일에 SynthID 워터마크(들리지 않음). 상업 사용 가능 여부는 협약 기업 확인 필요
- 받은 파일(.tmp)은 Downloads 에서 몇 초 만에 사라짐 (2026-10-12 확인) → 다운로드 누르기 전에 복사 감시를 먼저 걸어 둘 것 (크기가 1MB 넘으면 바로 복사)
- `BGM_PV.mp3` (2026-10-13) — itch.io 홍보 영상용. Gemini 음악 만들기 (Pro · 길이 짧게 · 연주곡), 1:05. 프롬프트: "30-second game trailer music for a cute comedy heist game where lab rats escape. Playful pizzicato strings and bouncy tuba, a sneaky start that builds with faster and faster drums, big brass hits, ending on one loud final hit. Upbeat, cartoonish, energetic." 마지막 쾅 = 61.03초 (PV 는 33.03초부터 30초).

- `BGM_PV2.mp3` (2026-10-14, 사용자가 준 곡 `Operation_Cheddar.mp3`) — PV v6 용. 1:02 · 120 BPM · 31.5초 정적 · 55.04초 마지막 쾅. PV 에는 9.05~34.10초 + 46.50~58초를 이어 붙여 씀 (`PV/music_v6.wav`, 명령 = `Tools/pv/make_pv2.py` MUSIC_CMD).
