# 엔딩 컷씬 (2026-10-21)

마지막 층(100층)을 처음 깨면 타이틀 씬 StoryPlayer 로 엔딩 → 로비. 자막 = 튜토리얼 테이블 Story 시트 `story = ending` 줄 (line 13~19, 키 4000013~4000019).
오프닝(`Story/`)과 같은 1536×1024 한 장짜리 컷 5장, 아래 25% 는 자막 판이 덮음.

| 컷 | 파일 | 장면 |
|---|---|---|
| 1 | ending_01.png | 쓰러진 그럼피(우주 사령관) · 빛이 새는 마지막 문 · 올려다보는 쥐들 |
| 2 | ending_02.png | 해 뜨는 옥상으로 쏟아져 나오는 쥐 떼 |
| 3 | ending_03.png | 빈 철창에 손을 넣는 '손가락' · 당황한 연구원 |
| 4 | ending_04.png | 언덕 너머 들판으로 함께 걸어가는 쥐들 |
| 5 | ending_05.png | (쿠키) 멀리 다른 연구소 창문 속 갇힌 쥐들 |

- 프롬프트 = `Sheets/prompt_ending_0*.txt` (Codex 형식), 참고 그림 `Sheets/ref_*.png`.
- 그림은 Codex 사용량 한도로 **ChatGPT 웹(사용자 계정) 한 대화**에서 5장을 이어서 그림 (같은 대화라 화풍이 이어짐). 1~4컷 · 5컷 모두 2026-10-21 (`Sheets/prompt_ending_05_chatgpt.txt`).
- 받은 원본 = `Sheets/chatgpt/` (1536×1024 PNG). 이 폴더의 ending_0*.png 와 같음 → `Assets/Art/Rats/Story/` 복사.
- 내려받는 법: 그림이 `blob:` 이고 ChatGPT CSP 가 localhost fetch 를 막음 → `Tools/recv_image.py <폴더>` 띄우고 ChatGPT 페이지에서 JS 로 blob 을 File 로 만들어 `<form method=POST enctype=multipart action=http://127.0.0.1:8765/upload>` 로 제출 (form-action 은 안 막힘). 받은 뒤 서버 끌 것.
