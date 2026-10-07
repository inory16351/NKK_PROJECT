# 공용 스킬 아이콘

Codex 로 만든 공용 스킬 아이콘 원본 (256×256 투명 PNG). 화풍 = 쥐 그림과 같은 Untitled Goose Game 식 플랫 (외곽선·광택 없음). 유니티에는 `Assets/Art/Rats/SkillIcons/` 로 같은 파일이 복사돼 있음.

- 파일 이름 = `cs_<코드 id>.png` → 공용 스킬 테이블 `skill_asset` (`SkillIcons/cs_<코드 id>`)
- `_미리보기.png` : 전체 한눈에 보기 (아래 띠 색 = 가지 색). 스킬 id 는 게임 화면에 보이지 않음 (개발용 표에만)
- `Sheets/` : Codex 원본 시트(`skill_icons_a.png` 6×4, `skill_icons_b.png` 4×3) · 프롬프트 · 로그. `_v1` = 외곽선 있던 첫 버전 (안 씀)
- 다시 자르기: `python Tools/slice_sheet.py <시트> <열> <행> <출력폴더> <이름,...> --blobs --pad 0` 후 정사각형 256 으로

| 스킬 id | 이름 | 가지 | 파일 |
|---|---|---|---|
| 92001 | 반란의 시작 | 반란 | cs_core.png |
| 92002 | 이빨 강화 | 갉기 | cs_teeth.png |
| 92003 | 급소 물기 | 갉기 | cs_critc.png |
| 92004 | 쥐 헬스장 | 갉기 | cs_gym.png |
| 92005 | 전기 이빨 | 갉기 | cs_zap.png |
| 92006 | 연쇄 폭발 | 갉기 | cs_chainx.png |
| 92007 | 이삿짐 센터 | 갉기 | cs_furnd.png |
| 92008 | 치즈 운석 | 갉기 | cs_meteor.png |
| 92009 | 묵직한 한 방 | 갉기 | cs_hitstun.png |
| 92010 | 보스 사냥꾼 | 갉기 | cs_bossd.png |
| 92011 | 날쌘 발 | 무리 | cs_speed.png |
| 92012 | 둥지 확장 | 무리 | cs_nest.png |
| 92013 | 카페인 중독 | 무리 | cs_caffeine.png |
| 92014 | 번식력 | 무리 | cs_breed.png |
| 92015 | 쌍둥이 | 무리 | cs_twins.png |
| 92016 | 돌연변이 유전자 | 무리 | cs_mutate.png |
| 92017 | 탄생 축제 | 무리 | cs_frenzy.png |
| 92018 | 치즈 감별사 | 물량·치즈 | cs_cheese.png |
| 92019 | 실험 재료 반입 | 물량·치즈 | cs_spawn.png |
| 92020 | 콤보 장인 | 물량·치즈 | cs_combo.png |
| 92021 | 물건 사재기 | 물량·치즈 | cs_stock.png |
| 92022 | 로켓배송 | 물량·치즈 | cs_truck.png |
| 92023 | 황금 물건 | 물량·치즈 | cs_goldx.png |
| 92024 | 백덤블링 | 재롱 | cs_flip.png |
| 92025 | 트리플 악셀 | 재롱 | cs_axel.png |
| 92026 | 쥐 대포알 | 재롱 | cs_cannon.png |
| 92027 | 윈드밀 | 재롱 | cs_windmill.png |
| 92028 | 헤딩 저글링 | 재롱 | cs_tumble.png |
| 92029 | 굴착 본능 | 탈출 | cs_dig.png |
| 92030 | 덫 해체 전문가 | 탈출 | cs_trapsafe.png |
| 92031 | 총공격 | 탈출 | cs_rush.png |
| 92032 | 캣닢 뇌물 | 탈출 | cs_catnip.png |
| 92033 | 야행성 | 탈출 | cs_offline.png |
| 92034 | 필살기 연습 | 특별 | cs_ultcd.png |
| 92035 | 슈퍼 점프 연습 | 특별 | cs_sjump.png |
| - | 잠김 (자물쇠) | - | cs_lock.png |

## 추가 아이콘 (2026-10-07, 훈장별 트리 개편)
`Sheets/skill_icons_c.png` (4x4 투명, 프롬프트 `prompt_c.txt`, 노이즈 제거 후 256 정사각형) · 미리보기 `Sheets/_preview_c.png`
cs_clock(제한시간) · cs_skip(스테이지 스킵) · cs_promote(승급) · cs_startrat(시작 쥐) · cs_multihit(연타) · cs_critdmg(치명타 피해) · cs_dmg(피해량) · cs_wallcrack(벽 체력 감소) · cs_bosshp(보스 체력 감소) · cs_newitem(신규 물건) · cs_cheeseitem(물건 치즈) · cs_cheesecreature(생명체 치즈) · cs_trickcheese(묘기 치즈) · cs_trickgauge(묘기 게이지) · cs_trapmulti(다중 쥐덫) · cs_shield(쥐덫 감소)
노드 → 아이콘 연결은 `Tools/gen_skill_tree.py` 의 ICON 표.
