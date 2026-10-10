# 효과음 (2026-10-10, 코드 합성)
`python Tools/gen_sfx.py` 로 생성 (웹 프로토 `rat-uprising.html` 의 WebAudio 합성 효과음 tone · noise 를 파이썬으로 옮김 + 새 소리).
44.1kHz · 16bit · 모노 WAV, 모두 최대 0.8 로 맞춤 → 실제 크기 · 겹침 제한은 유니티 `SfxManager` 항목마다.
- 이름 끝 `_1 _2 _3` = 같은 소리 변주 (랜덤 재생): rat_squeak · cat_meow
- 분류: ui_* (클릭 · 열기 · 닫기 · 구매 · 거절 · 잠김 · 안내) · tuto_* (화자별 '찍' blip · 넘기기 · 강조) · story_page · slot_select
  · jgl_* 징글 (해금 · 층 클리어 · 게임 오버 · 훈장 승급 · 업적 · 보스 경고/처치 · 시간 경고 · 발악 · 필살기 컷인)
  · rat_* (울음 · 갉기 · 탄생 · 총공격 함성 · 돌진 · 승급 · 기절 · 점프 · 착지 · 높은 등급 탄생) · ult_ready · superjump_*
  · hit_* / smash_* (유리 · 나무 · 금속) · crit · combo_02~12 · combo_word · cheese · spill · wall_crack · wall_blast · stairs_open · delivery_drop · trap_snap · meteor · laser · throw
  · human_fly · guard_siren · cat_* (울음 · 하악 · 경고 · 덮치기 · 퇴치) · boss_* (피격 · 내려찍기 · 휘두르기 · 던지기)
- 목소리 계열(사람 비명 · 진짜 쥐/고양이 소리)은 합성이라 어색 → 나중에 녹음/생성 소리로 바꿀 것 (같은 이름으로 덮어쓰면 됨)
