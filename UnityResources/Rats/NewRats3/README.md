# 새 쥐 2종 그림 (2026-10-12) — 아이스 퀸 쥐 · 5G

로직·수치는 이미 있음 (HANDOFF §5-3 '새 쥐 2종'). 기획서 그림(`Sheets/ref_*_concept.jpg`)은 아이디어만, 그림체는 게임 쥐 플랫(Codex).

| id | 쥐 | 그림 |
|---|---|---|
| 10086 | 아이스 퀸 쥐 `icequeen` | 파츠 `../Parts/icequeen/` (얼음 왕관 · 얼음 망토) |
| 10087 | 5G `fiveg` | **휴대폰 속 쥐** (사용자 2026-10-12): 겉 = 휴대폰 한 장짜리 `../Parody/rat_fiveg.png` (다리·꼬리 없음, 컴퓨터 마우스 쥐처럼 움직임) · 화면 배경 `../Parody/rat_fiveg_screen.png` · 화면 속 파란 쥐 = 파츠 `../Parts/fiveg/` |

- 5G 동작: `RatRig.screenRig`(Single 아래 자식 RatRig) 가 화면 칸 안에서 겉 자세를 따라 함 (걷기·공격·행동·기절). 가만히 있으면 화면 안을 혼자 서성임. **멀리서 빠르게 달리다 멈추면 화면에 얼굴 쿵** (앞으로 미끄러짐 → 납작 → 벌러덩 → 어질어질, `bumpSpeed/bumpDist/bumpTime`). UI(도감·뽑기 카드)는 `RatPortrait` 가 화면 칸에 배경 + 쥐를 넣음.
- 화면 칸 = 휴대폰 그림에서 비어 있던 자리, 좌하단 기준 (0.0721, 0.1123, 0.8559, 0.7536) → `RatArtLibraryBuilder.ScreenRectFiveG`. 휴대폰 그림을 바꾸면 다시 잴 것.
- 미리보기 `_5G_미리보기.png` (합성 확인용).

| 파일 | 내용 |
|---|---|
| `Sheets/newrats3_parts.png` | 파츠 시트 (1줄 아이스 퀸 · 2줄 5G) → `python Tools/slice_rat_parts.py <시트> 3 icequeen,fiveg,-` 후 몸통 폭 412px 로 맞춤 → `python Tools/gen_sprite_pivots.py` |
| `Sheets/fiveg_phone.png` | 휴대폰(화면 칸 자홍) · 화면 배경 · 신호 막대. 다리·꼬리는 잘라 낸 뒤 지움 |
| `Sheets/newrats3_icons.png` → `Icons/` | rs_20086 서리 축적 · rs_21086 얼음 미사일 · rs_20087 집결 범위 · rs_21087 데이터 전송 · ult_30035 렛잇고 · ach_40035 업적 (`python Tools/slice_grid_all.py <시트> 3 2 <폴더> <이름들> 256`) |
| `Sheets/newrats3_props.png` → `Props/` | ice_castle (렛잇고) · ice_missile (RatManager.iceMissileSprite) · signal_wave · snowflake · ice_block · frost_crystal · data_chip · ice_tiara |

유니티 사본: `Assets/Art/Rats/Parts/{icequeen,fiveg}` · `Parody/rat_fiveg(_screen).png` · `RatSkillIcons` · `UltIcons` · `AchvIcons` · `NewRats/` (소품).
