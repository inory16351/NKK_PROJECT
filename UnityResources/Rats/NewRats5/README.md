# 콩쥐팥쥐 · 나비쥐 (2026-10-29)

사용자 참고 그림 `Sheets/ref_kongpat_concept.png` · `ref_butterfly_concept.png` 는 아이디어만, 그림체는 게임 플랫(Codex).

| id | 쥐 | 구성 |
|---|---|---|
| 10088 | 콩쥐팥쥐 `kongpat` (Epic · 훈장 8 · 공격력 88) | **한 마리를 180° 돌려 위아래로 맞붙인 쌍둥이**. 파츠 = 일반 쥐 5조각 두 벌 `Parts/kongpat`(노랑 저고리·산호 치마) · `Parts/patjwi`(민트 저고리·남색 치마). 위 쥐 = 아래 쥐를 쌍의 가운데 축으로 180° 돌린 것(머리·꼬리가 서로 반대 끝), 거꾸로 선 쪽은 팔다리 바둥. 특수 액션(공중제비 쌍둥이 = Jump_Slam 3번) = 점프마다 쌍이 가운데 축으로 반 바퀴씩 돌아 위아래가 바뀜(3번 → 홀수 → 바뀐 채 끝) + 착지 충격. 필살기·초필살기 없음 |
| 10089 | 나비쥐 `butterfly` (Rare · 훈장 8 · 공격력 46) | **몸통·다리 없이 머리 + 귀 날개 2 + 꼬리**만 (날아다니는 머리). `Parts/butterfly` = head · tail · wing(큰 귀) · wing2(작은 아래 날개), 가까운·먼 쪽 둘씩 씀 (`slice_butterfly.py`). 땅에서 떠서 둥실, 귀가 펄럭 (연출만, 이동·충돌은 그대로). 필살기·초필살기 없음 |

- 시트: `Sheets/twin_parts.png` · `butterfly_parts.png` (첫 시안 `_v1_torso_wings` = 일반 쥐 몸통에 날개 → 사용자 "몸통 없이 머리가 날아다니는 느낌" 으로 다시) · `twin_butterfly_icons.png`. 프롬프트·로그 같이 보관.
- 자르기: `python Tools/slice_rat_parts.py <시트> 3 kongpat,patjwi,-` · 나비쥐 날개는 시트 2째 줄을 따로 잘라 `Parts/butterfly/wing.png` · `python Tools/clean_part_edges.py` → `python Tools/add_rats_to_unity.py kongpat patjwi butterfly` (Assets 복사 + sprite_pivots · RatRigMeta 추가. gen_sprite_pivots.py 는 지운 쥐가 되살아나서 안 씀).
- 테이블: `python Tools/add_twin_butterfly_rats.py` (캐릭터 10088·10089 · 스킬 20088·21088·20089·21089) → gen_string_table → xlsx2json. 영어 이름 Hanbok Twins · Butterfly Mouse (콩쥐팥쥐 어감은 못 살려서 쌍둥이로).
- 코드: `Rats/Rat.Pair.cs`(쌍둥이 · 나비 동작) · `RatRig.cs`(headOnly · 날개 · ApplyWings) · `RatArtLibrary.Entry`(wing · wingAnchor · wingSize · headScale · twinCodeId · headOnly) · 값 = RatManager 인스펙터 `콩쥐팥쥐`(twinGap · twinFlail* · twinFlipRate) · `나비쥐`(flyHeight · flyBob · flap* · flyLean), RatRig `wingSpread`.
