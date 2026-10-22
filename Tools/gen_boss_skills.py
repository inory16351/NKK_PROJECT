"""보스 개편 행동 (2026-10-22, 명세 = UnityResources/Rats/BossRework/boss_spec_20261022.md) → 보스 테이블 Atk_Type · Boss.
· 25~97층 = 개편 보스 (95024~95039) · 100층 = 우주 고양이 사령관(95006, 맥스웰 드롭 → 버터 고양이 = 잼 식빵 고양이 회전) · 5~20층 그대로.
· 예전 25~100층 보스(95007~95023)는 floor 0 (테스트 패널로만).
· 쿨타임 8초(처음 4초) · 발악 70%/35% · 둘 다 쓴 뒤 special_chance 확률로 번갈아 · 평타 = 리그별 기본. 체력·치즈·속도 = 같은 층 예전 보스 값.
· 기술 패턴 = Boss.Meme.cs + Boss.Rework.cs. 그림 이름 br_* = 아직 없음 (임시 그림, 사용자: 동작 먼저 그림 나중).
고치기 전 원본 = Data_Table/_backup_20261022/. 다음: python Tools/gen_string_table.py → (이 파일이 영어 채움) → python Tools/xlsx2json.py
사용: python Tools/gen_boss_skills.py"""
import os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import gen_expansion_data as G

G.BACKUP = os.path.join(G.DT, '_backup_20261022')
ko = G.ko

# (atk_type, 이름(한,영), pattern, radius, stun, count, windup, dur, sprite, size, color, popup(한,영)|None, line(한,영)|None, p1, p2, p3, opt, 설명)
SKILLS = [
    # 25 밈 고양이
    ('R_HappyDrop', ('해피캣 투하', 'Happy Cat Drop'), 'Drop', 110, 1, 8, 0.8, 1.2, 'bm_happy_mini', 90, '#f3efe6', ('해피!', 'Happy!'), None, 4, 3, 0, '', '[스킬] 해피 고양이 8마리가 떨어져 3초간 폴짝폴짝, 닿으면 1초 기절'),
    ('R_Maxwell', ('맥스웰 소환', 'Maxwell Summon'), 'Rise', 120, 2, 6, 0.9, 0.8, '@maxwell', 130, '#c9b49a', ('맥스웰~', 'Maxwell~'), None, 1, 4, 0, '', '[발악1] 맥스웰 6마리가 바닥 포탈에서 솟아 좌우로 흔들, 닿으면 기절'),
    ('R_HuhCat', ('허 고양이', 'Huh Cat'), 'Throw', 120, 2, 4, 1.0, 1.0, 'br_huh_h,br_huh_u,br_huh_h,br_huh_q', 90, '#e8c07a', ('HUH?', 'HUH?'), None, 0, 0, 0, 'head:br_huh_head,around', '[발악2] 허 고양이 머리 → HUH? 글자가 팝콘처럼 사방 포물선, 착탄 지점 기절'),
    # 30 실험체 고양이
    ('R_HairSpit', ('헤어볼 뱉기', 'Hairball Spit'), 'Fan', 70, 1, 6, 0.8, 0, '@hairball', 70, '#c9a27a', ('켁!', 'Hack!'), None, 40, 380, 200, '', '[스킬] 몸을 숙이고 입에서 헤어볼이 앞으로 우르르 굴러 나옴'),
    ('R_SawIt', ('너.. 봤구나?', 'You... Saw That?'), 'Pulse', 340, 2, 1, 1.2, 0.5, '-', 0, '#f2f0e6', None, ('너... 봤구나?', 'You... saw that?'), 0, 0, 0, 'head_keep:br_goerem_saw', '[발악1] 목이 길어진 기괴한 얼굴로 바뀌어 그대로 유지 + 빛나며 둘레 기절 (다시 쓰면 빛+기절만)'),
    ('R_SadBread', ('나 우울해서 빵됐어', 'Sad Into Bread'), 'Drop', 100, 2, 12, 1.0, 2.4, 'br_bread_loaf,br_bread_pizza,br_bread_cake,br_bread_chiffon', 100, '#e9c48a', None, ('나 우울해서 빵됐어..', 'I got sad... so I became bread..'), 0, 4, 0, 'body:br_goerem_bread@260,sticky:4', '[발악2] 식빵으로 변하고 우는 빵 12개 낙하, 맞거나 바닥 반죽에 닿으면 기절'),
    # 35 공룡탈 알바
    ('R_MaskToss', ('탈 던지기', 'Mask Toss'), 'Fan', 90, 1, 1, 1.5, 0, 'br_dino_head', 120, '#7fb069', ('으랏차!', 'Heave!'), None, 0, 650, 0, 'explode', '[스킬] 끙끙 탈을 뽑아 던짐 → 착탄 지점까지 일직선으로 날아가 폭발 (머리엔 탈 그대로)'),
    ('R_TRex', ('티라노 흉내', 'T-Rex Impression'), 'Grow', 240, 2, 0, 0.8, 6, '-', 0, '#7fb069', ('크아앙!', 'RAWR!'), None, 0, 0, 0, '', '[발악1] 공룡처럼 거대화해 걸어다님, 화면 진동 + 둘레 기절'),
    ('R_Smoke', ('현타 담배', 'Existential Smoke Break'), 'Zone', 260, 2, 1, 0.6, 3, 'br_smoke', 0, '#b8b8b8', None, None, 1, 0.4, 0, 'body:br_dino_sit@220', '[발악2] 쭈그려 앉아 담배, 퍼지는 연기에 닿으면 기절 (3초 뒤 복귀)'),
    # 40 원시인 버티컬 마우스
    ('R_ClubSmash', ('몽둥이 강타', 'Club Smash'), 'Spin', 260, 1, 0, 0.4, 2, '-', 0, '#a07850', ('퍽!', 'Bonk!'), None, 1, 0, 0, '', '[스킬] 2초간 몽둥이를 마구 휘둘러 범위 쥐를 벽까지 날림'),
    ('R_WarCry', ('워크라이', 'War Cry'), 'Shout', 420, 2, 0, 1.0, 0.6, 'br_club', 120, '#d9a066', ('우가!!', 'OOGA!!'), None, 0, 0, 0, '', '[발악1] 몽둥이를 마이크처럼 대고 외침, 음파에 닿으면 기절'),
    ('R_MouseMeteor', ('버티컬 마우스 투하', 'Mouse Meteor'), 'Drop', 150, 2, 6, 1.0, 1.5, 'br_vmouse_meteor', 130, '#8a7a6a', ('쾅!', 'Boom!'), None, 5, 4, 0, 'morph:br_vmouse_head@3', '[발악2] 버티컬 마우스 6개 운석 → 크레이터 4초, 닿으면 미니 버티컬 마우스로 3초간 멋대로 (인구 그대로)'),
    # 45 랩터 사육사
    ('R_Shotgun', ('샷건 발사', 'Shotgun Blast'), 'Fan', 70, 1, 7, 1.0, 0, 'br_pellet', 40, '#e0c080', ('탕!', 'BANG!'), None, 50, 900, 0, 'near', '[스킬] 장전 1초 → 전방 부채꼴 발사 + 보스 둘레 기절'),
    ('R_Stampede', ('랩터 떼', 'Raptor Stampede'), 'Parade', 120, 2, 7, 0.8, 2.2, 'bm_raptor', 150, '#9ab06a', ('끼아악!', 'SKREE!'), None, 1, 0, 0, '', '[발악1] 랩터 떼가 화면 왼쪽 밖 → 오른쪽 밖, 지나간 뒤 화면 안 쥐 전부 2초 기절'),
    ('R_Whisperer', ('랩터 다루기', 'Raptor Whisperer'), 'Orbit', 110, 1, 3, 1.0, 5, 'bm_raptor', 140, '#9ab06a', ('진정해!', 'Easy!'), None, 240, 70, 0, '', '[발악2] 랩터 3마리가 투명→불투명 등장, 5초간 보스 둘레를 돎 (보스는 천천히 걸음)'),
    # 50 노조 고양이
    ('R_PawSlap', ('냥냥펀치', 'Paw-Paw Slap'), 'Slam', 200, 1, 1, 1.0, 0, '-', 0, '#e8a0a0', ('냥!', 'Nyah!'), None, 4, 0, 0, '', '[스킬] 사냥 자세 1초 → 쥐 무리로 뛰어가 앞발 4연타 (칠 때마다 흔들림 + 기절)'),
    ('R_Solidarity', ('단결 투쟁', 'Solidarity Strike'), 'Parade', 120, 2, 8, 1.2, 3, 'bm_picket', 150, '#e05050', ('단결 투쟁!', 'UNITE & FIGHT!'), None, 0, 0, 0, 'red,body:br_union_megaphone@300', '[발악1] 두 발로 서서 확성기 → 화면 붉게 깜빡 + 시위대가 화면을 가로지름'),
    ('R_LooksGood', ('좋았쓰!! 호출', 'Call the Looks-Good Cats'), 'Throw', 120, 2, 8, 1.6, 1.2, 'bm_joassu', 110, '#f0d050', ('좋았쓰!!', 'LOOKS GOOD!!'), None, 0, 0, 0, 'around,shake,body:br_union_phone@300', '[발악2] 안전모 쓰고 전화 → 점점 진동, 먼지 속에서 좋았쓰 고양이들이 사방 포물선'),
    # 55 거미 고양이
    ('R_RebarSpikes', ('철근 가시', 'Rebar Spikes'), 'Fan', 60, 1, 7, 0.9, 0, 'br_rebar', 90, '#8a8f99', None, None, 60, 700, 0, '', '[스킬] 입을 우물거리다 바라보는 방향 60° 부채꼴로 철근 가시'),
    ('R_RebarWeb', ('철근 거미줄', 'Rebar Web'), 'Drop', 360, 2, 1, 1.0, 0.3, 'br_rebar_web', 600, '#8a8f99', ('철컹!', 'Clang!'), None, 0, 0, 0, '', '[발악1] 공중에서 큰 철근 거미줄이 낙하 (그림자 예고 1초)'),
    ('R_WebDash', ('거미줄 돌진', 'Web Dash'), 'Path', 130, 2, 0, 0.9, 3, '-', 0, '#c0c4cc', None, None, 0, 320, 0, '', '[발악2] 시동 자세 → 3초간 별 + 오각형 모양으로 매우 빠르게 돌진'),
    # 60 굴착기 기사 (발악 1 에 굴착기 탑승)
    ('R_ConeDrop', ('라바콘 투하', 'Cone Drop'), 'Drop', 120, 1, 3, 0.7, 0.3, '@cone', 90, '#f08a30', ('콘!', 'Cone!'), None, 0, 0, 0, '', '[스킬] 그림자 예고 0.7초 → 라바콘 3개 낙하'),
    ('R_ExcSpin', ('굴삭기 회전', 'Excavator Spin'), 'Spin', 330, 2, 0, 0.8, 3, '-', 0, '#f0b030', ('위이잉!', 'Whirr!'), None, 1, 0, 0, 'ride:boss_excavator', '[발악1] 굴착기 탑승 → 팔을 펼치고 3초간 빙글빙글'),
    ('R_DiggerDance', ('포크레인 댄스', 'Digger Dance'), 'Fan', 60, 2, 36, 0.6, 3, 'bm_rock', 60, '#a08060', None, None, 360, 650, 0, 'ride:boss_excavator', '[발악2] 포크레인 춤추며 사방으로 돌멩이 난사'),
    # 65 상어 고양이
    ('R_BubbleJail', ('물방울 감옥', 'Bubble Jail'), 'Drop', 140, 1, 3, 0.7, 0.3, 'bm_bubble', 260, '#7ec8f0', ('뽀글!', 'Blub!'), None, 5, 1, 0, '', '[스킬] 그림자 예고 0.7초 → 3곳에 물방울 감옥 (안의 쥐 1초 갇힘)'),
    ('R_FinRush', ('지느러미 돌진', 'Fin Rush'), 'Dash', 150, 2, 1, 1.0, 1.2, '-', 160, '#e05050', ('빠밤-', 'Dun dun-'), None, 1400, 0, 0, 'hide:bm_fin', '[발악1] 몸을 숨기고 붉게 물든 지느러미만 쥐 무리로 돌진'),
    ('R_SneakerTornado', ('신발 토네이도', 'Sneaker Tornado'), 'Path', 150, 2, 0, 0.8, 3, '-', 0, '#4a90d9', ('위잉~', 'Vroom~'), None, 1, 420, 0, '', '[발악2] 롤러스케이트 신발로 3초간 바깥 → 안쪽 나선 돌진'),
    # 70 머라옹 고양이
    ('R_BubbleBarrage', ('거품 난사', 'Bubble Barrage'), 'Fan', 70, 1, 8, 0.7, 0.8, 'bm_bubble', 70, '#9fd8f5', None, None, 30, 450, 0, '', '[스킬] 바라보는 쪽으로 거품 여러 개'),
    ('R_MiniMerlion', ('꼬마 머라옹 소환', 'Mini Mer-Meows'), 'Summon', 120, 2, 3, 0.9, 2, 'br_mini_merlion', 110, '#f0b060', ('머라옹!', 'Mer-meow!'), None, 0, 0, 0, '', '[발악1] 작은 머라옹 3마리가 2초간 돌아다님'),
    ('R_WaterCannon', ('물대포', 'Water Cannon'), 'Fan', 90, 2, 16, 1.0, 1.2, 'bm_wave', 110, '#5ab0e8', ('촤아아!', 'SPLOOSH!'), None, 12, 900, 700, '', '[발악2] 입에서 거대 물줄기, 맞은 쥐 밀려나며 기절'),
    # 75 심해 잠수부
    ('R_Sonar', ('음파 발사', 'Sonar Ping'), 'Pulse', 420, 1, 3, 0.6, 1.5, '-', 0, '#7fd0e0', ('핑-', 'Ping-'), None, 0, 0, 0, '', '[스킬] 얼굴에서 음파 파동'),
    ('R_AnchorDrop', ('닻 투하', 'Anchor Drop'), 'Drop', 260, 2, 1, 1.0, 0.3, 'bm_anchor', 240, '#6a7a8a', ('쿵!', 'THUD!'), None, 0, 0, 0, '', '[발악1] 공중 포탈에서 거대 닻 투하'),
    ('R_DepthCharge', ('폭뢰', 'Depth Charge'), 'Sweep', 140, 2, 5, 1.0, 1.2, 'bm_wave', 140, '#4aa0d0', ('콰앙!', 'KABOOM!'), None, 0, 0, 0, '', '[발악2] 일직선으로 물기둥 5개가 솟아 쥐를 튕김'),
    # 80 차력거인
    ('R_MachineGun', ('기관총 사격', 'Machine Gun Fire'), 'Fan', 50, 1, 36, 0.6, 3, 'br_bullet', 30, '#f0d070', None, None, 50, 1000, 150, '', '[스킬] 3초간 기관총 사격 (다시 기절 가능)'),
    ('R_HammerFist', ('내려찍기', 'Hammer Fist'), 'Smash', 280, 2, 0, 0.9, 0.4, '-', 0, '#e0b090', ('쾅!!', 'SLAM!!'), None, 0, 0, 0, '', '[발악1] 팔을 들었다 바로 앞을 내려침'),
    ('R_StompMarch', ('쿵쿵 행진', 'Stomp March'), 'Grow', 280, 2, 0, 0.8, 3, '-', 0, '#e0b090', ('쿵! 쿵!', 'STOMP! STOMP!'), None, 0, 0, 0, '', '[발악2] 몸을 일으켜 3초간 크게 쿵쿵 걸으며 둘레 기절'),
    # 85 항공모함 고양이
    ('R_RatSquadron', ('쥐 전투기 출격', 'Rat Squadron'), 'Rush', 90, 1, 6, 0.8, 5, 'br_rat_jet', 110, '#a0b0c0', ('출격!', 'Scramble!'), None, 420, 0, 0, '', '[스킬] 활주로에서 쥐 전투기 6대, 5초간 쥐 사이를 날아다님'),
    ('R_EyeBeam', ('눈깔 빔', 'Eye Beam'), 'Beam', 150, 2, 3, 1.0, 1.0, '-', 0, '#ff5040', ('삐-', 'Bzzt-'), None, 0, 0, 0, '', '[발악1] 관제탑 창문 눈에서 빔'),
    ('R_CrocBomber', ('악어 폭격기', 'Crocodile Bomber'), 'Sweep', 160, 3, 6, 1.2, 1.8, 'bm_shell', 110, '#6a9a5a', ('폭격!', 'Bombs away!'), None, 0, 0, 0, '', '[발악2] 악어 폭격기가 폭탄 6번 투하'),
    # 90 총통 고양이
    ('R_ArtToss', ('그림 도구 던지기', 'Art Supply Toss'), 'Fan', 80, 2, 4, 0.8, 0.8, 'br_brush,br_palette,br_easel,br_canvas', 100, '#c09060', None, None, 0, 600, 0, '', '[스킬] 붓·팔레트·이젤·캔버스를 일직선으로 던짐'),
    ('R_FartGas', ('방구가스', 'Fart Gas'), 'Zone', 220, 3, 1, 0.8, 5, '@gas', 0, '#90c050', ('뿌웅~', 'Pfffft~'), None, 1, 0.3, 5, 'mask:br_gasmask', '[발악1] 초록 가스 구름 5초, 기절한 쥐는 5초간 방독면 면역'),
    ('R_MustacheToss', ('수염 던지기', 'Mustache Toss'), 'Fan', 90, 2, 1, 0.8, 0, 'br_mustache', 110, '#3a3a3a', None, None, 0, 750, 700, '', '[발악2] 수염을 떼어 일직선으로 던짐, 맞은 쥐 밀려남'),
    # 94 그렘린 고양이
    ('R_Charge', ('돌진', 'Charge'), 'Dash', 140, 1, 1, 0.8, 1.0, '-', 0, '#70a050', ('끼햐햐!', 'Kyahaha!'), None, 1300, 0, 0, '', '[스킬] 쥐가 많은 쪽으로 1회 돌진'),
    ('R_BabyGremlins', ('새끼 그렘린', 'Baby Gremlins'), 'Summon', 100, 2, 6, 0.9, 2, 'br_mini_gremlin', 80, '#70a050', None, None, 0, 0, 0, '', '[발악1] 등에서 새끼 그렘린 6마리, 2초간 돌아다님'),
    ('R_Microwave', ('전자레인지 투하', 'Microwave Drop'), 'Drop', 330, 3, 1, 1.3, 0.4, 'br_microwave', 260, '#c0c0c0', ('띵-! 콰광!', 'Ding! KABOOM!'), None, 0, 0, 0, '', '[발악2] 하늘에서 거대 전자레인지 → 폭발'),
    # 97 수상한 우주복 쥐
    ('R_SirenDrop', ('사이렌 투하', 'Siren Drop'), 'Drop', 170, 1, 4, 0.8, 1.0, 'br_siren', 90, '#ff4040', ('삐뽀삐뽀', 'Wee-woo'), None, 5, 1.5, 0, '', '[스킬] 사이렌 4개 낙하, 둘레 1초 기절'),
    ('R_TentacleGulp', ('촉수 삼키기', 'Tentacle Gulp'), 'Swallow', 300, 0, 0, 1.0, 0.6, '-', 0, '#c03030', ('꿀꺽!', 'Gulp!'), None, 3, 0, 0, 'intro', '[발악1·등장] 둘레 쥐를 삼켰다 3초 뒤 뱉음 (인구 그대로)'),
    ('R_Huddle', ('긴급 회의', 'Emergency Huddle'), 'Global', 0, 2, 0, 1.2, 1.0, 'br_red_button', 90, '#ff3030', ('긴급 회의!', 'EMERGENCY HUDDLE!'), None, 0, 0, 0, 'all', '[발악2] 빨간 버튼 → 맵의 모든 쥐 2초 멈춤'),
]

# (boss_id, 이름(한,영), 층, 같은 층 예전 보스 id(체력·치즈·속도·색), 쿨타임, 발악1, 발악2)
BOSSES = [
    (95024, ('밈 고양이', 'Meme Cat'), 25, 95007, 'R_HappyDrop', 'R_Maxwell', 'R_HuhCat'),
    (95025, ('실험체 고양이', 'Subject Kitty'), 30, 95008, 'R_HairSpit', 'R_SawIt', 'R_SadBread'),
    (95026, ('공룡탈 알바', 'T-Rex Suit Part-Timer'), 35, 95009, 'R_MaskToss', 'R_TRex', 'R_Smoke'),
    (95027, ('원시인 버티컬 마우스', 'Caveman Vertical Mouse'), 40, 95010, 'R_ClubSmash', 'R_WarCry', 'R_MouseMeteor'),
    (95028, ('랩터 사육사', 'Raptor Keeper'), 45, 95011, 'R_Shotgun', 'R_Stampede', 'R_Whisperer'),
    (95029, ('노조 고양이', 'Union Cat'), 50, 95012, 'R_PawSlap', 'R_Solidarity', 'R_LooksGood'),
    (95030, ('거미 고양이', 'Spider Cat'), 55, 95013, 'R_RebarSpikes', 'R_RebarWeb', 'R_WebDash'),
    (95031, ('굴착기 기사', 'Excavator Operator'), 60, 95014, 'R_ConeDrop', 'R_ExcSpin', 'R_DiggerDance'),
    (95032, ('상어 고양이', 'Shark Cat'), 65, 95015, 'R_BubbleJail', 'R_FinRush', 'R_SneakerTornado'),
    (95033, ('머라옹 고양이', 'Mer-Meow'), 70, 95016, 'R_BubbleBarrage', 'R_MiniMerlion', 'R_WaterCannon'),
    (95034, ('심해 잠수부', 'Deep Sea Diver'), 75, 95017, 'R_Sonar', 'R_AnchorDrop', 'R_DepthCharge'),
    (95035, ('차력거인', 'Strongman Giant'), 80, 95018, 'R_MachineGun', 'R_HammerFist', 'R_StompMarch'),
    (95036, ('항공모함 고양이', 'Carrier Cat'), 85, 95019, 'R_RatSquadron', 'R_EyeBeam', 'R_CrocBomber'),
    (95037, ('총통 고양이', 'Bunker General Cat'), 90, 95020, 'R_ArtToss', 'R_FartGas', 'R_MustacheToss'),
    (95038, ('그렘린 고양이', 'Gremlin Cat'), 94, 95021, 'R_Charge', 'R_BabyGremlins', 'R_Microwave'),
    (95039, ('수상한 우주복 쥐', 'Sus Spacesuit Rat'), 97, 95022, 'R_SirenDrop', 'R_TentacleGulp', 'R_Huddle'),
]
RETIRED = range(95007, 95024)        # 예전 25~100층 보스
COMMANDER = 95006                    # 100층 = 우주 고양이 사령관 (예전 그림 재사용)
OLD_100 = 95023


def main():
    wb, p = G.book('보스 테이블')
    at = G.Sheet(wb['Atk_Type'])
    at.col('p3', '패턴 값 3 (Boss.Rework.cs)', 'float'); at.col('opt', '옵션 키워드 (Boss.Rework.cs)', 'string')
    for (t, name, pat, rad, stun, cnt, wind, dur, spr, size, color, pop, line, p1, p2, p3, opt, desc) in SKILLS:
        at.upsert('atk_type', t, dict(desc=desc, radius=rad, stun=stun, count=cnt, windup=wind, dur=dur, atk_name=ko(name), line=ko(line) if line else None,
                                      pattern=pat, sprite=spr, size=size, color=color, popup=ko(pop) if pop else None, p1=p1, p2=p2, p3=p3, opt=opt or None))
    b = G.Sheet(wb['Boss'])
    def row(bid): r = b.find('boss_id', bid); return {k: b.ws.cell(r, i + 1).value for i, k in enumerate(b.keys)}
    old = {bid: row(bid) for bid in list(RETIRED) + [COMMANDER]}
    for bid in RETIRED:
        r = b.find('boss_id', bid)
        b.ws.cell(r, b.keys.index('floor') + 1, 0)
        if '-' in b.keys:
            dc = b.keys.index('-') + 1; v = b.ws.cell(r, dc).value or ''
            if '보스 개편' not in v: b.ws.cell(r, dc, '(미사용 2026-10-22: 보스 개편으로 교체, 테스트 패널로만) ' + v)
    for bid, name, floor, src, sk, s1, s2 in BOSSES:
        o = old[src]
        b.upsert('boss_id', bid, {'boss_name': ko(name), 'floor': floor, 'color': o['color'], 'hp_pow_sec': o['hp_pow_sec'], 'cheese_mul': o['cheese_mul'], 'move_speed': o['move_speed'],
                                  'skill_type': sk, 'skill_cd': 8, 'skill_first': 4, 'special1_type': s1, 'special2_type': s2, 'special_chance': 0.25,
                                  '-': f'{floor}층 보스 (보스 개편 2026-10-22). 쿨타임 {sk} · 발악 {s1} · {s2}'})
    o = old[OLD_100]
    b.upsert('boss_id', COMMANDER, {'boss_name': ko(('우주 고양이 사령관', 'Space Cat Commander')), 'floor': 100, 'hp_pow_sec': o['hp_pow_sec'], 'cheese_mul': o['cheese_mul'], 'scale': o['scale'], 'radius_mul': o['radius_mul'],
                                    'skill_type': 'Buttered_Cat', 'skill_cd': 8, 'skill_first': 4,
                                    '-': '100층 보스 (보스 개편 2026-10-22): 예전 우주 고양이 사령관, 맥스웰 드롭 → 버터 고양이(잼 식빵 고양이 회전)'})
    at.upsert('atk_type', 'Buttered_Cat', {'atk_name': ko(('잼 식빵 고양이 회전', 'Jam Toast Cat Spin'))})      # 100층 (예전 이름 '무한동력 버터 고양이', 동작은 그대로)
    wb.save(p)
    print('기술', len(SKILLS), '· 보스', len(BOSSES) + 1)


if __name__ == '__main__':
    main()
    if '--english' in sys.argv: G.fill_english()
