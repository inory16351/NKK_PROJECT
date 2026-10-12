# 새 쥐 6종 (2026-10-12) — 캐릭터 상세 기획서 `캐릭터상세기획서모음.pptx`

기획서 그림은 참고만, 그림체는 게임 플랫(Untitled Goose Game 식). Codex 생성.

| id | 이름 | 코드 | 등급·훈장 | 그림 |
|---|---|---|---|---|
| 10080 | 폭렬쥐 찍구밍 (기획서 '폭탄마법사 쥐' → 폭탄마, 사용자 참고 그림 `Sheets/ref_bomber_concept.png`) | bomber | 신화 · 6 | 파츠 `../Parts/bomber/` |
| 10081 | 노릇노릇 쥐포 | jwipo | 에픽 · 1 | 한 장짜리 `../Parody/rat_jwipo.png` (+ 구운 그림 `Single/jwipo_grilled.png`) |
| 10082 | 고쳐쓰쥐 (정비공) | mechanic | 일반 · 2 | 파츠 `../Parts/mechanic/` |
| 10083 | 스톰쥐퍼 (흰 갑주) | trooper | 유니크 · 4 (필살기 없음, 사용자 결정) | 파츠 `../Parts/trooper/` (앞발에 블래스터) |
| 10084 | 쥐한폭탄 (폭탄 쥐) | bombrat | 에픽 · 4 | 한 장짜리 `../Parody/rat_bombrat.png` = 그냥 폭탄, 얼굴 없음 (사용자). 심지 3단계 `Single/bombrat_fuse1~3.png` |
| 10085 | 패티추가 맘스땃쥐 | burger | 에픽 · 2 | 한 장짜리 `../Parody/rat_burger.png` = 햄버거에 쥐 귀·눈 (2026-10-12 사용자). 층 `Burger/` (시트 `Sheets/burger_rat_v2.png`), 패티 0~5장 합친 그림 `Burger/stack/burger_stack0~5.png` (한 장마다 치즈·양상추·토마토·고기). 옛 파츠 `../Parts/burger/` 안 씀 |

- 쥐포·폭탄 쥐는 처음에 파츠로 그렸다가 사용자 요청("컴퓨터 마우스처럼 그대로")으로 한 장짜리로 바꿈. 옛 파츠 = `../Parts_unused/bombrat_parts_v1`, `jwipo_parts_v1`.
- `Sheets/newrats2_parts_a.png`(bomber·jwipo·mechanic) · `newrats2_parts_b.png`(trooper·bombrat·burger, `_cut` = 폭탄 몸통과 심지를 잘라 둔 것) · `newrats2_single.png`(폭탄 3 · 쥐포 2) · `newrats2_props.png` · `magic_circle.png`(처음 것은 육각별이라 다시 그림) · `newrats2_icons.png` + 각 `prompt_*.txt` · `log_*.txt`
- 자르기: `python Tools/slice_rat_parts.py <시트> 3 bomber,-,mechanic` 처럼 → 몸통 폭 412px 로 맞춤 → `python Tools/gen_sprite_pivots.py`. 한 장짜리는 같은 틀(겹친 테두리)로 잘라 바꿔 끼워도 크기가 안 튐.
- `Props/`: bomber_bomb · blaster_bolt · patty · mech_parts · wrench · smell · explosion_big · magic_circle → `Assets/Art/Rats/NewRats/` (UltimateManager Fill Props 폴더)
- `_조립비교.png`: 리그 조립 미리보기 (fondue·jjdetective 와 비교)
- 아이콘: `../RatSkillIcons/rs_20080~21085` (Sheets/rat_skill_icons_newrats2.png), 필살기 `../UltIcons/ult_30034.png` (익스플로전). 자폭 아이콘의 눈은 지움.
