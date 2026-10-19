"""100층 확장 (2026-10-19) 내용 정의 — 구간 · 물건 · 가구 배치 · 사람 · 보스 · 밈 기술 · 대사 · 훈장 9~16.
기획: 기획서/stage_objects_16-100-1.md (구간 오브젝트) · 기획서/20261016_..._100층 확장 ..._박송호.pptx (훈장·승급)
사용자 결정 (2026-10-19): 16~30층 = 고양이 연구소로 교체 (25 해피 고양이 · 30 팝 고양이, 마녀·우주 사령관은 floor 0 = 안 나옴) ·
  초월 승급은 안 함 · 91~100 구간 보스 = 94·97·100층 · 보스마다 새 밈 기술 (나중에 고치기 쉽게 테이블 + 이 파일 + 기록 문서) ·
  훈장·스킬은 임의 배분 · 밸런스 임의.
이 파일의 값을 고친 뒤: python Tools/gen_expansion_data.py (엑셀에 반영, 같은 id 는 덮어씀). 엑셀에서 직접 고쳐도 됨 (생성기를 다시 돌리면 이 파일 값으로 되돌아감).
글은 (한국어, 영어) 쌍 → gen_expansion_data 가 스트링 테이블 영어 칸까지 채움."""

# ── 구간 (물건 테이블 Zone) : id, 이름(ko,en), 시작 층, 끝 층, 가구 테마 ──
ZONES = [
    (70005, ('고양이 연구소', 'Cat Research Lab'), 16, 30, 'Cat_A,Cat_B'),
    (70006, ('지구과학 연구소', 'Earth Science Lab'), 31, 45, 'Dino_A,Dino_B'),
    (70007, ('연구소 확장 공사 현장', 'Lab Expansion Site'), 46, 60, 'Build_A,Build_B'),
    (70008, ('수중 연구소', 'Underwater Lab'), 61, 75, 'Sea_A,Sea_B'),
    (70009, ('2차대전 박물관', 'WW2 Museum'), 76, 90, 'War_A,War_B'),
    (70010, ('우주 연구소', 'Space Research Center'), 91, 100, 'Space_A,Space_B'),
]
OLD_THEMES = 'Lab,Office,Breakroom,Nap,Storage'     # 70001~70004 구간 (예전 가구 테마)
FLOOR_ART = ['floor_catlab', 'floor_dino', 'floor_build', 'floor_sea', 'floor_war', 'floor_space']
WALL_ART = ['wall_catlab', 'wall_dino', 'wall_build', 'wall_sea', 'wall_war', 'wall_space']
WALL_CAP = ['#d9c7c9', '#b59a7a', '#8a8f96', '#5f8fa0', '#5d6b4a', '#9aa6b8']

# ── 구간 물건: 시트 칸 순서와 같음 (1~10 일반 · 11~13 값진 물건 · 14~18 가구). (code, ko, en) ──
ZONE_ITEMS = {
    1: [('yarn', '실뭉치', 'Yarn Ball'), ('churu_box', '츄르 상자', 'Churu Boxes'), ('scratcher', '스크래처', 'Scratching Post'),
        ('cucumber', '오이', 'Cucumber'), ('feather_wand', '깃털 낚싯대', 'Feather Wand'), ('paw_beaker', '발자국 비커', 'Paw-Print Beaker'),
        ('cat_laptop', 'ffff 노트북', '"ffffff" Laptop'), ('cat_box', '딱 맞는 빈 박스', 'If-I-Fits Box'), ('cat_bread', '구멍 난 식빵', 'Cat-Bread Slice'),
        ('tipped_vase', '넘어진 꽃병', 'Tipped Vase'),
        ('golden_churu', '황금 츄르', 'Golden Churu'), ('liquid_cat', '고양이는 액체다 병', 'Liquid Cat Jar'), ('nyan_toast', '냥캣 토스트', 'Nyan Toast'),
        ('f_cat_tower', '대형 캣타워', 'Giant Cat Tower'), ('f_salad_table', '샐러드 식탁', 'Salad Table'), ('f_pet_carrier', '펫 이동장', 'Pet Carrier'),
        ('f_churu_vending', '츄르 자판기', 'Churu Vending Machine'), ('f_bongo_desk', '봉고 드럼 책상', 'Bongo Desk')],
    2: [('ammonite', '암모나이트', 'Ammonite'), ('trilobite', '삼엽충 화석', 'Trilobite Fossil'), ('footprint_cast', '발자국 석고본', 'Footprint Cast'),
        ('flat_earth', '평평한 지구본', 'Flat Earth Globe'), ('pickaxe', '발굴용 곡괭이', 'Dig Pickaxe'), ('amber_mosquito', '호박 속 모기', 'Mosquito in Amber'),
        ('dino_egg', '공룡알', 'Dino Egg'), ('offline_dino', '오프라인 공룡 모니터', 'Offline Dino Monitor'), ('dino_nuggets', '공룡 너겟', 'Dino Nuggets'),
        ('raptor_skull', '공룡 머리 화석', 'Raptor Skull'),
        ('golden_egg', '황금 공룡알', 'Golden Dino Egg'), ('amber_gold', '금괴 박힌 호박', 'Gold-Filled Amber'), ('meteorite', '불타는 운석', 'Flaming Meteorite'),
        ('f_trex', '티렉스 화석', 'T-Rex Fossil'), ('f_display_case', '유리 진열장', 'Display Case'), ('f_dig_table', '발굴 작업대', 'Dig Table'),
        ('f_research_desk', '연구원 책상', 'Researcher Desk'), ('f_snack_counter', '매점 카운터', 'Snack Counter')],
    3: [('joassu_helmet', '조아쓰 안전모', '"Joassu!" Helmet'), ('rubber_cone', '러버콘', 'Traffic Cone'), ('cement_bag', '시멘트 포대', 'Cement Sack'),
        ('yangeun_pot', '찌그러진 양은냄비', 'Dented Ramen Pot'), ('wheelbarrow', '외발 수레', 'Wheelbarrow'), ('work_gloves', '목장갑', 'Work Gloves'),
        ('rebar_bundle', '철근 다발', 'Rebar Bundle'), ('coffee_mix', '믹스커피 상자', 'Coffee Mix Box'), ('zero_days_sign', '무재해 0일 표지판', '"0 Days Safe" Sign'),
        ('doge_poster', '시바견 안전 포스터', 'Swole Doge Poster'),
        ('golden_shovel', '기공식 황금 삽', 'Golden Shovel'), ('golden_rat', '황금 쥐 장식', 'Golden Rat Statue'), ('pay_envelope', '두툼한 월급봉투', 'Fat Pay Envelope'),
        ('f_this_is_fine', '불타는 현장 책상', 'This-Is-Fine Desk'), ('f_scaffold', '비계', 'Scaffolding'), ('f_pallet', '자재 팔레트', 'Brick Pallet'),
        ('f_mini_excavator', '미니 포크레인', 'Mini Excavator'), ('f_tray_cart', '함바 식판 수레', 'Canteen Tray Cart')],
    4: [('coral', '산호 조각', 'Coral Branch'), ('starfish', '불가사리', 'Starfish'), ('conch', '소라', 'Conch Shell'),
        ('octopus_pot', '문어 항아리', 'Octopus Pot'), ('flatfish_box', '광어 스티로폼 박스', 'Flatfish Box'), ('rubber_basin', '빨간 고무대야', 'Red Rubber Basin'),
        ('market_price', '싯가 가격판', '"Market Price" Sign'), ('singing_fish', '노래하는 물고기 장식', 'Singing Fish Plaque'), ('crab_rave', '춤추는 꽃게', 'Crab Rave Crab'),
        ('blobfish_jar', '블롭피시 표본병', 'Blobfish Jar'),
        ('giant_clam', '진주 거대 조개', 'Giant Pearl Clam'), ('golden_tuna', '경매 1호 황금 참치', 'Golden Auction Tuna'), ('diamond_crab', '다이아몬드 킹크랩', 'Diamond King Crab'),
        ('f_aquarium', '대형 수조', 'Big Aquarium'), ('f_fish_stall', '수산시장 좌판', 'Fish Market Stall'), ('f_specimen_shelf', '표본 보관장', 'Specimen Shelf'),
        ('f_seafood_freezer', '수산물 냉동고', 'Seafood Freezer'), ('f_coral_tank', '산호 배양 탱크', 'Coral Culture Tank')],
    5: [('army_helmet', '철모', 'Steel Helmet'), ('ammo_crate', '탄약 상자', 'Ammo Crate'), ('field_radio', '무전기', 'Field Radio'),
        ('ration_tins', '전투식량', 'Ration Tins'), ('model_plane', '모형 전투기', 'Model Fighter'), ('red_barrel', '빨간 드럼통', 'Red Barrel'),
        ('x_keycap', 'X 키캡', '"X" Keycap'), ('flex_poster', '알통 쥐 포스터', '"We Can Squeak It" Poster'), ('pointing_poster', '손가락 쥐 포스터', '"I Want You" Poster'),
        ('wilson', '철모 쓴 윌슨', 'Wilson in a Helmet'),
        ('gold_medal', '황금 훈장', 'Golden Medal'), ('airdrop_crate', '낙하산 보급 상자', 'Airdrop Crate'), ('golden_pan', '순금 프라이팬', 'Golden Frying Pan'),
        ('f_ammo_depot', '탄약 창고', 'Ammo Depot'), ('f_map_table', '작전 지도 테이블', 'War Map Table'), ('f_field_kitchen', '야전 취사대', 'Field Kitchen'),
        ('f_jeep', '군용 지프', 'Army Jeep'), ('f_souvenir_stand', '기념품 가판대', 'Souvenir Stand')],
    6: [('fuel_can', '연료통', 'Fuel Canister'), ('rocket_model', '로켓 모형', 'Rocket Model'), ('planet_model', '행성 모형', 'Planet Model'),
        ('space_food', '동결건조 우주식량', 'Freeze-Dried Food'), ('tinfoil_hat', '은박지 모자', 'Tinfoil Hat'), ('telescope', '망원경', 'Telescope'),
        ('sat_dish', '위성 안테나', 'Satellite Dish'), ('space_helmet', '우주 헬멧', 'Space Helmet'), ('houston_phone', '휴스턴 직통 전화기', 'Houston Hotline'),
        ('to_the_moon', '떡상 관제 모니터', '"To the Moon" Monitor'),
        ('moon_rock', '월석 케이스', 'Moon Rock Case'), ('golden_record', '황금 레코드', 'Golden Record'), ('monolith', '모노리스', 'Monolith'),
        ('f_control_console', '관제 콘솔', 'Mission Console'), ('f_launch_pad', '발사대', 'Launch Pad'), ('f_zerog_capsule', '무중력 훈련 캡슐', 'Zero-G Capsule'),
        ('f_planetarium', '천체투영기', 'Star Projector'), ('f_tesseract_shelf', '인터스텔라 책장', 'Tesseract Bookshelf')],
}
# 물건 기본 수치 (구간 안에서 순서대로 조금씩 커짐). 값진 물건(11~13)·가구(14~18)는 따로
ITEM_HP = [1.0, 1.1, 1.6, 0.8, 1.0, 1.2, 2.4, 1.8, 0.9, 1.4]
FURN_RAD = [44, 46, 36, 40, 42]
SPILL_COLOR = {1: '#f2d6a0', 2: '#c9a26b', 3: '#9a9a9a', 4: '#8fd3e8', 5: '#8a9a6a', 6: '#c7d3e6'}

# ── 사람 (사람 테이블 Human) : 구간마다 4명 (code, ko, en, 체력, 치즈, 속도, (공포 대사 ko,en) × 2) ──
ZONE_HUMANS = {
    1: [('catres', '털 범벅 집사 연구원', 'Cat-Hair Researcher', 3, 6, 1.0, [('내 실험복이 털 범벅이야!', 'My coat is all fur!'), ('냥이들 밥 줄 시간인데!', "It's feeding time!")]),
        ('vet', '수의사', 'Vet', 3.5, 6.5, 1.05, [('쥐는 진료 안 해요!!', "I don't treat rats!!"), ('주사 맞을래?!', 'Want a shot?!')]),
        ('feeder', '사료 배달원', 'Pet Food Courier', 4, 7, 1.1, [('배송 지연이에요!', 'Delivery delayed!'), ('사료 봉지 물지 마!', "Don't chew the bags!")]),
        ('allergy', '알레르기 연구원', 'Allergic Researcher', 3, 6, 0.95, [('에, 에취!!', 'Ah... ACHOO!!'), ('쥐 알레르기도 있었나?!', 'Am I allergic to rats too?!')])],
    2: [('paleo', '고생물학자', 'Paleontologist', 3.5, 6.5, 1.0, [('화석 건드리지 마!', "Don't touch the fossils!"), ('이건 백악기 유물이라고!', "That's Cretaceous!")]),
        ('trainer', '조련사', 'Dino Handler', 4.5, 8, 1.1, [('진정해, 진정!', 'Easy, easy!'), ('랩터보다 빠르잖아!', "They're faster than raptors!")]),
        ('kid', '견학 어린이', 'Field-Trip Kid', 2.5, 5, 1.2, [('엄마, 쥐가 막 뛰어!', 'Mom, the rats are running!'), ('와 쥐다! 귀여워!', 'Aww, rats!')]),
        ('nedry', '매직 워드 프로그래머', 'Magic Word Programmer', 3, 7, 0.9, [('아아아, 매직 워드를 말 안 했잖아!', "Ah ah ah, you didn't say the magic word!"), ('시스템 다운이다!', "System's down!")])],
    3: [('harold', '현장소장', 'Site Manager Harold', 4, 8, 0.95, [('하하… 괜찮아요… (안 괜찮음)', "Haha... I'm fine... (not fine)"), ('무재해 기록이 깨진다!', 'There goes our safety record!')]),
        ('flagger', '신호수', 'Flagger', 4, 7, 1.1, [('멈춰! 멈추라고!', 'Stop! I said stop!'), ('차량 통제 중!', 'Traffic control!')]),
        ('coffeeman', '믹스커피 인부', 'Coffee-Mix Worker', 4.5, 7.5, 1.0, [('커피 타는 중이었는데!', 'I was making coffee!'), ('봉지로 젓는 게 국룰인데!', 'Stirring with the packet is the rule!')]),
        ('rebar', '철근 작업자', 'Rebar Worker', 5, 8.5, 1.0, [('철근 무너진다!', "Rebar's falling!"), ('안전모 써!', 'Helmets on!')])],
    4: [('oceanog', '해양학자', 'Marine Biologist', 3.5, 6.5, 1.0, [('샘플이 도망간다!', 'The samples are escaping!'), ('쥐는 해양 생물이 아냐!', "Rats aren't marine life!")]),
        ('diver', '다이버', 'Diver', 4, 7, 1.05, [('꼬르륵… 쥐다!', 'Blub... rats!'), ('산소통 물지 마!', "Don't bite the tank!")]),
        ('auctioneer', '생선 경매사', 'Fish Auctioneer', 4.5, 9, 1.0, [('묻고 더블로 가!', 'Double or nothing!'), ('이 쥐 싯가요!', 'Rats at market price!')]),
        ('saltbae', '소금 뿌리는 사장', 'Salt Bae Chef', 4, 8, 1.1, [('소금 맛 좀 볼래?!', 'Want some salt?!'), ('내 팔꿈치 각도가!', 'My elbow angle!')])],
    5: [('veteran', '노병 해설사', 'Veteran Guide', 3.5, 7, 0.85, [('내가 왕년에 말이야!', 'Back in my day!'), ('전시물 만지지 마!', 'No touching the exhibits!')]),
        ('reenactor', '군복 재연 배우', 'Reenactor', 4.5, 8, 1.1, [('이건 대본에 없었어!', "This wasn't in the script!"), ('엄폐! 엄폐!', 'Take cover!')]),
        ('student', '견학 학생', 'Field-Trip Student', 3, 6, 1.15, [('숙제 사진 찍어야 돼!', 'I need a homework photo!'), ('선생님, 쥐요!!', 'Teacher, RATS!!')]),
        ('radioman', '통신병', 'Radio Operator', 4, 7.5, 1.0, [('여기는 박물관, 쥐 출현!', 'Museum to HQ, rats sighted!'), ('치직… 응답하라!', 'Kssh... come in!')])],
    6: [('controller', '관제사', 'Mission Controller', 3.5, 7, 1.0, [('휴스턴, 문제가 생겼다!', 'Houston, we have a problem!'), ('궤도 이탈!', "We're off course!")]),
        ('astro_banana', '바나나 우주비행사', 'Banana Astronaut', 4.5, 8.5, 1.05, [('원래 이랬어. (Always has been)', 'Always has been.'), ('바나나 내려놔!', 'Drop the banana!')]),
        ('astro_set', '조명 우주비행사', 'Moon-Set Astronaut', 4.5, 8.5, 1.05, [('컷! 컷! 다시 찍어!', 'Cut! Cut! Take two!'), ('조명 넘어진다!', 'The lights are falling!')]),
        ('flightdir', '커피 쏟는 관제소장', 'Flight Director', 4, 9, 0.95, [('내 커피!!', 'My coffee!!'), ('발사 중지!', 'Abort launch!')])],
}
OLD_HUMAN_END = {90001: 30, 90002: 30, 90004: 30}    # 연구원·청소부·요리사는 30층까지 (경비원은 계속 = 게임 오버 습격·보스 호출)

# ── 밈 기술 (보스 테이블 Atk_Type) ──
# (type, (이름 ko,en), pattern, radius, stun, count, windup, dur, sprite, size, color, (팝업 ko,en), p1, p2, 원본 밈 · 설명)
# sprite = Assets/Art/Rats/BossMeme/<이름>.png (Boss.memeSprites). '@grill' 같은 '@' 이름은 보스 인스펙터의 기존 그림
MEMES = [
    # 25 해피 고양이
    ('Chipi_Chapa', ('치피치피 차파차파', 'Chipi Chipi Chapa Chapa'), 'Pulse', 260, 1.2, 6, 0.5, 3, 'bm_note', 0, '#f2a7c3', ('치피치피 차파차파!', 'Chipi chipi chapa chapa!'), 0, 0, '[스킬] 치피치피 차파차파 (2024 춤추는 고양이 밈): 박자마다 좌우로 흔들며 둘레 충격파'),
    ('Happy_Rain', ('해피 고양이 대량 강하', 'Happy Cat Airdrop'), 'Drop', 110, 2, 8, 0.8, 2, 'bm_happy_mini', 90, '#f3efe6', ('해피해피~', 'Happy happy~'), 4, 4, '[필살] 해피 고양이 (2023): 작은 해피 고양이 8마리가 떨어져 통통 뛰어다님'),
    ('Huh_Cat', ('허? 고양이', 'Huh Cat'), 'Shout', 380, 2.4, 0, 0.9, 0.6, '-', 0, '#e8c07a', ('허?', 'Huh?'), 0, 0, '[필살] 허? 고양이 (2023 Huh cat): 크게 "허?" → 주변 쥐가 놀라 날아감'),
    # 30 팝 고양이
    ('Pop_Pop', ('팝팝팝', 'Pop Pop Pop'), 'Throw', 100, 2, 5, 0.4, 1.2, 'bm_pop_wave', 90, '#9fc6e8', ('팝!', 'Pop!'), 30, 0, '[스킬] 팝캣 (2020): 입을 뻥뻥 → 소리 고리를 쥐에게 날림'),
    ('Pop_Clicker', ('팝캣 클릭 대회', 'Popcat Click-Off'), 'Drop', 120, 2.2, 10, 0.8, 2.2, 'bm_cursor', 110, '#f0a65a', ('클릭!', 'Click!'), 0, 0, '[필살] popcat.click 클릭 대회 (2020): 거대 마우스 커서가 쥐 무리를 연타'),
    ('Vibing_Cat', ('고개 까딱 고양이', 'Vibing Cat'), 'Global', 0, 1.8, 0, 1.4, 1, 'bm_shades', 110, '#9fd3a8', ('까딱 까딱~', '*head bob*'), 0, 0, '[필살] 고개 까딱 고양이 (2020 Vibing Cat): 화면 안 쥐가 전부 같이 까딱거리며 멈춤'),
    # 35 원시인
    ('Rock_Throw', ('돌멩이 투척', 'Rock Toss'), 'Throw', 110, 2.4, 3, 0.4, 0.8, 'bm_rock', 70, '#9a8a7a', ('쿵!', 'Thud!'), 20, 0, '[두 번째 공격] 돌멩이 3개'),
    ('Fire_Discovery', ('불의 발견', 'Discovering Fire'), 'Zone', 170, 2.2, 1, 0.8, 6, '@grill', 0, '#f0a05a', ('앗 뜨거!!', 'Ouch, hot!!'), 1, 0.6, '[스킬] 자기 횃불에 놀란 원시인: 제자리에 불바다 장판'),
    ('Return_To_Monke', ('원숭이로 돌아가자', 'Return to Monke'), 'Summon', 130, 2.2, 4, 1, 4, 'bm_monke', 90, '#a07850', ('우가우가!', 'Ooga booga!'), 0, 0, '[필살] Return to Monke (2020): 원숭이 4마리 소환 → 쥐에게 달려가 펑'),
    ('Extinction', ('공룡 멸종 운석', 'Extinction Event'), 'Drop', 420, 3, 1, 1.6, 0.5, 'bm_meteor', 380, '#e8743a', ('멸종이다!!', 'EXTINCTION!!'), 0, 0, '[필살] 공룡 멸종 운석: 아주 큰 운석 하나'),
    # 40 랩터 사육사
    ('Hold_Up', ('랩터 진정시키기', 'Raptor Hold'), 'Shout', 340, 2, 0, 0.8, 0.6, 'bm_stop', 90, '#6f8a5a', ('워워… 진정해.', 'Easy... easy.'), 0, 0, '[스킬] 쥬라기 월드 손 내밀기 장면 (2015): 손을 내밀어 주변 쥐를 멈춤'),
    ('Clever_Girl', ('영리한 녀석', 'Clever Girl'), 'Summon', 120, 2.4, 3, 1, 4, 'bm_raptor', 110, '#7a9a4a', ('영리한 녀석…', 'Clever girl...'), 0, 0, '[필살] 쥬라기 공원 "Clever girl" (1993): 수풀에서 랩터 3마리'),
    ('Tranq_Dart', ('마취총 연사', 'Tranq Barrage'), 'Throw', 80, 2.8, 8, 0.5, 1.6, 'bm_dart', 60, '#d0503a', ('푹!', 'Thwip!'), 10, 0, '[필살] 마취총 8발'),
    # 45 공룡탈 알바
    ('Trex_Dance', ('공룡탈 댄스', 'Inflatable Dino Dance'), 'Spin', 200, 1.6, 0, 0.5, 3, '-', 0, '#7cb86a', ('둠칫둠칫!', 'Dum-dum-dance!'), 220, 0, '[스킬] 공룡 풍선옷 춤 영상: 빙글빙글 흐느적 춤추며 다가옴'),
    ('Tiny_Arms', ('짧은 팔의 분노', 'Tiny Arms Rage'), 'Hop', 240, 2.3, 3, 0.6, 0, '-', 0, '#5a9a4a', ('팔이… 안 닿아…!', "Can't... reach...!"), 0, 0, '[필살] 티렉스 짧은 팔 밈: 팔이 안 닿아 분해서 3번 쿵쿵 점프'),
    ('Deflate', ('바람 빠진 공룡', 'Deflating Dino'), 'Dash', 130, 2, 5, 0.6, 3.5, '-', 0, '#9ad08a', ('푸쉬이이~', 'Pssshhh~'), 1100, 0, '[필살] 공룡탈 바람 빠짐: 풍선처럼 이리저리 쌩쌩'),
    # 50 야가다 김씨
    ('Excavator', ('포크레인 퍼 올리기', 'Excavator Scoop'), 'Drop', 160, 2.6, 2, 1, 1, 'bm_bucket', 200, '#e0b23a', ('퍼 올려!', 'Scoop!'), 0, 0, '[스킬] 굴착기 버킷이 쥐 무리를 퍼 올림'),
    ('Concrete', ('레미콘 타설', 'Concrete Pour'), 'Zone', 150, 2.4, 3, 0.8, 6, 'bm_concrete', 0, '#9a9a9a', ('굳는다!', 'Setting!'), 0, 0.8, '[필살] 시멘트 장판 3곳 (오래 서 있으면 굳어 기절)'),
    ('Joa_Ssu', ('조아쓰!!', 'Joassu!!'), 'Buff', 300, 1, 0, 0.7, 6, 'bm_joassu', 90, '#f2c84a', ('조아쓰!!', 'Joassu!!'), 0.4, 0, '[필살] "조아쓰" 기합 밈 (2020): 기합 → 6초 동안 받는 피해 ×0.4'),
    # 55 노조 고양이
    ('Picket', ('피켓 시위', 'Picket Line'), 'Drop', 110, 2, 4, 0.8, 1.4, 'bm_picket', 120, '#c9564a', ('투쟁!', 'Strike!'), 5, 3, '[스킬] 피켓 4개가 떨어져 3초 꽂혀 있음'),
    ('Churu_Raise', ('츄르 인상 요구', 'Churu Raise Demand'), 'Shout', 420, 2.4, 0, 1, 0.6, 'bm_megaphone', 90, '#e86a5a', ('츄르 인상하라!!', 'Raise the churu!!'), 0, 0, '[필살] 확성기 외침'),
    ('Lie_Down', ('드러눕기 투쟁', 'Lie-Down Strike'), 'Zone', 160, 2.2, 3, 0.8, 6, 'bm_zzz', 0, '#a6b4d8', ('드르렁…', 'Zzz...'), 0, 0.6, '[필살] 낮잠 구름 장판 3곳 (안에 있으면 같이 잠듦)'),
    # 60 함바집 아줌마
    ('Rice_Bowl', ('공깃밥 투척', 'Rice Bowl Toss'), 'Throw', 110, 2.2, 3, 0.4, 0.8, 'bm_rice_bowl', 70, '#e8e0d0', ('밥 먹어!', 'Eat up!'), 20, 0, '[두 번째 공격] 공깃밥 3개'),
    ('Gobong', ('고봉밥 투하', 'Heaping Rice Drop'), 'Drop', 150, 2.4, 4, 0.9, 1.6, 'bm_rice_mountain', 160, '#f4efe2', ('고봉밥!', 'Mountain of rice!'), 5, 2, '[스킬] 고봉밥 4그릇'),
    ('Did_You_Eat', ('밥은 먹고 다니냐?', 'Have You Eaten?'), 'Global', 0, 2.2, 0, 1.4, 1, '-', 0, '#f6d58a', ('밥은 먹고 다니냐?', 'Have you even eaten?'), 0, 0, '[필살] 살인의 추억 "밥은 먹고 다니냐?" (2003): 화면 안 쥐 전부 숙연해짐'),
    ('Ladle_Spin', ('국자 회오리', 'Ladle Tornado'), 'Spin', 230, 2, 0, 0.6, 3, 'bm_ladle', 110, '#e57b7b', ('국자 회오리!', 'Ladle tornado!'), 260, 0, '[필살] 국자를 들고 빙글빙글'),
    # 65 인어 고양이
    ('Bubble_Shot', ('물방울 발사', 'Bubble Shot'), 'Throw', 100, 2, 4, 0.4, 1, 'bm_bubble', 70, '#9ad8e8', ('뽀글!', 'Bloop!'), 20, 0, '[두 번째 공격] 물방울 4개'),
    ('Under_The_Sea', ('언더 더 씨', 'Under the Sea'), 'Summon', 110, 2, 5, 1, 4, 'bm_bubble', 90, '#6cc4c0', ('언더 더 씨~♪', 'Under the sea~♪'), 0, 0, '[스킬] 인어공주 "Under the Sea": 물방울 5개가 쥐에게 둥실둥실 다가가 펑'),
    ('Tidal_Wave', ('해일', 'Tidal Wave'), 'Sweep', 150, 2.4, 7, 1, 1.6, 'bm_wave', 150, '#4a9ad0', ('촤아악!', 'Splash!'), 0, 0, '[필살] 가로로 쓸고 가는 해일'),
    ('Fish_Slap', ('생선 따귀', 'Fish Slap Dance'), 'Rush', 90, 2.4, 4, 0.8, 3, 'bm_fish', 130, '#a8b8c8', ('생선 따귀!', 'Fish slap!'), 520, 0, '[필살] 몬티 파이튼 생선 따귀 춤 (1972): 커다란 고등어 4마리가 날아다님'),
    # 70 잠수복 고양이
    ('Anchor_Drop', ('닻 내려!', 'Drop Anchor!'), 'Drop', 140, 2.6, 3, 0.9, 1.2, 'bm_anchor', 150, '#7a8a9a', ('닻 내려!', 'Drop anchor!'), 0, 0, '[스킬] 닻 3개'),
    ('Sonar_Ping', ('소나 핑', 'Sonar Ping'), 'Pulse', 440, 1.6, 4, 0.6, 3, '-', 0, '#6ad0c8', ('핑―', 'Ping—'), 0, 0, '[필살] 잠수함 소나: 큰 충격파 4번'),
    ('Octo_Ink', ('문어 먹물', 'Octopus Ink'), 'Zone', 160, 2.2, 3, 0.8, 6, 'bm_ink', 0, '#3a3a4a', ('먹물!', 'Ink!'), 0, 0.6, '[필살] 먹물 장판 3곳'),
    # 75 상어 고양이
    ('Baby_Shark', ('아기상어', 'Baby Shark'), 'Summon', 110, 2, 4, 1, 4, 'bm_baby_shark', 90, '#f2d34a', ('아기상어 뚜루루뚜루~', 'Baby shark doo doo~'), 0, 0, '[스킬] 아기상어 (2016): 아기상어 4마리'),
    ('Jaws_Dash', ('죠스 돌진', 'Jaws Charge'), 'Dash', 150, 2.4, 4, 1.2, 3.2, 'bm_fin', 90, '#5a7a9a', ('빠밤… 빠밤…', 'Dun dun... dun dun...'), 1250, 0, '[필살] 죠스 테마 (1975): 등지느러미를 세우고 4번 돌진'),
    ('Sharknado', ('샤크네이도', 'Sharknado'), 'Pull', 300, 2.8, 0, 2.4, 0, 'bm_sharknado', 260, '#8a9aaa', ('샤크네이도!!', 'Sharknado!!'), 0, 0, '[필살] 샤크네이도 (2013): 상어 회오리가 쥐를 빨아들였다가 펑'),
    # 80 포탑 고양이
    ('Wrong_Way', ('엉뚱한 포격', 'Wrong-Way Shot'), 'Throw', 120, 2.4, 5, 0.6, 1.5, 'bm_shell', 60, '#c8a04a', ('어… 거기 아닌데?', 'Uh... wrong way?'), 380, 0, '[스킬] 포탑이 엉뚱한 방향: 포탄 5발이 이상한 데 떨어짐'),
    ('Barrage', ('일제 포격', 'Artillery Barrage'), 'Sweep', 140, 2.4, 8, 1.2, 1.8, 'bm_shell', 90, '#e88a3a', ('쾅!', 'BOOM!'), 0, 0, '[필살] 가로줄 포격 8발'),
    ('No_Scope', ('360 노스코프', '360 No-Scope'), 'Beam', 110, 2.4, 8, 0.9, 1.2, '-', 0, '#ffd25a', ('360 노스코프!!', '360 no-scope!!'), 1, 0, '[필살] 360 노스코프 (게임 밈): 빙글 돌며 사방으로 8발'),
    # 85 라이언 이병 고양이
    ('Lion_Roar', ('사자후', "Lion's Roar"), 'Shout', 400, 2.2, 0, 0.9, 0.6, '-', 0, '#d9a441', ('어흥!!', 'ROAR!!'), 0, 0, '[스킬] 사자후'),
    ('Circle_Of_Life', ('생명의 순환', 'Circle of Life'), 'Global', 0, 2, 0, 1.6, 1, '-', 0, '#ffcf6b', ('나~ 쥐 고냐~!!', 'Naaants ingonyama!!'), 0, 0, '[필살] 라이온 킹 "Circle of Life" (1994): 화면 안 쥐 전부 감동해서 멈춤'),
    ('Supply_Drop', ('이병 구하기 보급', 'Saving Private Supply'), 'Drop', 140, 2.4, 5, 1, 2, 'bm_airdrop', 140, '#c8b88a', ('보급 왔다!', 'Supplies!'), 5, 2, '[필살] 라이언 일병 구하기 (1998): 낙하산 보급 상자 5개'),
    # 90 총통 고양이
    ('Pencil_Throw', ('연필 던지기', 'Pencil Throw'), 'Throw', 100, 2.2, 3, 0.7, 0.8, 'bm_pencil', 80, '#e8c84a', ('연필 던짐!', '*pencil toss*'), 40, 0, '[스킬] 몰락 벙커 장면 (2004) 연필 던지기'),
    ('Bunker_Rant', ('벙커 분노', 'Bunker Rant'), 'Global', 0, 2.2, 0, 1.8, 1, '-', 0, '#c84a3a', ('다 나가!! 츄르 담당만 남아!!', 'Everyone out! Except the churu guy!!'), 0, 0, '[필살] 몰락 벙커 분노 장면: 화면 안 쥐 전부 얼어붙음'),
    ('Map_Slam', ('작전 지도 쾅!', 'Map Table Slam'), 'Hop', 250, 2.3, 3, 0.6, 0, '-', 0, '#8a7a6a', ('쾅!! 작전 실패다!!', 'BANG!! The plan failed!!'), 0, 0, '[필살] 작전 지도 테이블 내리치기 3번'),
    # 94 빨간 우주복
    ('Vent', ('환풍구 이동', 'Vent Hop'), 'Teleport', 200, 2.4, 2, 0.6, 2, 'bm_vent', 120, '#d43b3b', ('환풍구!', 'Vent!'), 0, 0, '[스킬] 어몽어스 (2018) 환풍구: 사라졌다가 쥐 무리 한가운데로'),
    ('Sus', ('수상해', 'Sus'), 'Mark', 0, 2.6, 6, 1.6, 0, 'bm_sus', 0, '#ff4a4a', ('수상해…', 'Sus...'), 0, 0, '[필살] "Sus" 투표: 쥐 6마리를 지목했다가 우주로 날려 버림'),
    ('Lights_Out', ('전등 사보타주', 'Lights Sabotage'), 'Global', 0, 1.6, 0, 1.6, 1, 'bm_wrench', 110, '#20202a', ('전등 사보타주!', 'Lights sabotaged!'), 0, 0, '[필살] 어몽어스 전등 사보타주: 화면이 깜깜 → 쥐 전부 멈춤'),
    # 97 레이저 눈 고양이
    ('Laser_Eyes', ('레이저 눈', 'Laser Eyes'), 'Beam', 120, 2.4, 4, 0.8, 1, '-', 0, '#ff3a3a', ('삐융!', 'Pew!'), 0, 0, '[스킬] 레이저 눈 밈: 눈에서 빔 4발'),
    ('OIIA', ('우이아 고양이', 'OIIA Cat'), 'Spin', 220, 2, 0, 0.5, 3.5, '-', 0, '#b08aff', ('우이아이아~', 'Oiia oiia~'), 330, 0, '[필살] OIIA 빙글빙글 고양이 (2024)'),
    ('Galaxy_Bang', ('빅뱅', 'Big Bang'), 'Pull', 340, 3, 0, 2.4, 0, '-', 260, '#6b4ac8', ('빅뱅!!', 'Big Bang!!'), 0, 0, '[필살] 은하 소용돌이가 쥐를 빨아들였다가 대폭발'),
    # 100 우주 사령관 고양이 (Grumpy)
    ('No_Cat', ('싫어.', 'No.'), 'Shout', 440, 2.4, 0, 0.9, 0.6, '-', 0, '#5a6fa8', ('싫어.', 'No.'), 0, 0, '[스킬] 그럼피 캣 "NO." (2012)'),
    ('Had_Fun_Once', ('재미없어', 'I Had Fun Once'), 'Global', 0, 2.4, 0, 1.6, 1, '-', 0, '#6a7fb8', ('재밌었던 적 한 번 있었지. 최악이었어.', 'I had fun once. It was awful.'), 0, 0, '[필살] 그럼피 캣 "I had fun once. It was awful."'),
    ('Moon_Drop', ('달 떨어뜨리기', 'Moon Drop'), 'Drop', 480, 3.2, 1, 1.8, 0.5, 'bm_moon', 420, '#c8c8d0', ('달이다!!', 'The moon!!'), 0, 0, '[필살] 거대한 달 하나'),
]

# ── 보스 (보스 테이블 Boss) ──
# (id, (이름 ko,en), code_id, rig, floor, color, scale, atk, atk2, skill, special1, special2, hp_pow_sec)
BOSSES = [
    (95007, ('해피 고양이', 'Happy Cat'), 'boss_happy', 'Cat', 25, '#f3efe6', 3.4, 'Pounce', 'Hairball', 'Chipi_Chapa', 'Happy_Rain', 'Huh_Cat', 45),
    (95008, ('팝 고양이', 'Pop Cat'), 'boss_pop', 'Cat', 30, '#f0a65a', 3.4, 'Pounce', 'Hairball', 'Pop_Pop', 'Pop_Clicker', 'Vibing_Cat', 45),
    (95009, ('원시인 우가', 'Caveman Ooga'), 'boss_caveman', 'Human', 35, '#b7804a', 2.8, 'Swing', 'Rock_Throw', 'Fire_Discovery', 'Return_To_Monke', 'Extinction', 45),
    (95010, ('랩터 사육사', 'Raptor Keeper'), 'boss_keeper', 'Human', 40, '#6f8a5a', 2.8, 'Swing', 'Stomp', 'Hold_Up', 'Clever_Girl', 'Tranq_Dart', 45),
    (95011, ('공룡탈 알바', 'T-Rex Suit Part-Timer'), 'boss_dino', 'Human', 45, '#7cb86a', 2.9, 'Stomp', 'Swing', 'Trex_Dance', 'Tiny_Arms', 'Deflate', 48),
    (95012, ('야가다 김씨', 'Construction Kim'), 'boss_kim', 'Human', 50, '#e0b23a', 2.9, 'Stomp', 'Swing', 'Excavator', 'Concrete', 'Joa_Ssu', 50),
    (95013, ('노조 고양이', 'Union Cat'), 'boss_union', 'Cat', 55, '#c9564a', 3.4, 'Pounce', 'Hairball', 'Picket', 'Churu_Raise', 'Lie_Down', 48),
    (95014, ('함바집 아줌마', 'Canteen Auntie'), 'boss_hamba', 'Human', 60, '#e57b7b', 2.9, 'Swing', 'Rice_Bowl', 'Gobong', 'Did_You_Eat', 'Ladle_Spin', 52),
    (95015, ('인어 고양이', 'Mermaid Cat'), 'boss_mermaid', 'Cat', 65, '#6cc4c0', 3.4, 'Pounce', 'Bubble_Shot', 'Under_The_Sea', 'Tidal_Wave', 'Fish_Slap', 48),
    (95016, ('잠수복 고양이', 'Diver Cat'), 'boss_diver', 'Cat', 70, '#c89b4a', 3.5, 'Pounce', 'Hairball', 'Anchor_Drop', 'Sonar_Ping', 'Octo_Ink', 50),
    (95017, ('상어 고양이', 'Shark Cat'), 'boss_shark', 'Cat', 75, '#7f97ad', 3.5, 'Pounce', 'Hairball', 'Baby_Shark', 'Jaws_Dash', 'Sharknado', 52),
    (95018, ('포탑 고양이', 'Turret Cat'), 'boss_turret', 'Cat', 80, '#7d8a4e', 3.5, 'Pounce', 'Hairball', 'Wrong_Way', 'Barrage', 'No_Scope', 50),
    (95019, ('라이언 이병 고양이', 'Private Lion Cat'), 'boss_ryan', 'Cat', 85, '#d9a441', 3.5, 'Pounce', 'Hairball', 'Lion_Roar', 'Circle_Of_Life', 'Supply_Drop', 52),
    (95020, ('총통 고양이', 'Bunker General Cat'), 'boss_fuhrer', 'Cat', 90, '#7a7a7a', 3.5, 'Pounce', 'Hairball', 'Pencil_Throw', 'Bunker_Rant', 'Map_Slam', 55),
    (95021, ('빨간 우주복', 'Red Crewmate'), 'boss_red', 'Human', 94, '#d43b3b', 2.9, 'Stomp', 'Swing', 'Vent', 'Sus', 'Lights_Out', 50),
    (95022, ('레이저 눈 고양이', 'Laser Eyes Cat'), 'boss_lasercat', 'Cat', 97, '#6b4ac8', 3.5, 'Pounce', 'Hairball', 'Laser_Eyes', 'OIIA', 'Galaxy_Bang', 52),
    (95023, ('우주 사령관 그럼피', 'Grumpy Space Commander'), 'boss_grumpy', 'Cat', 100, '#5a6fa8', 3.8, 'Pounce', 'Gravity', 'No_Cat', 'Had_Fun_Once', 'Moon_Drop', 65),
]
RETIRED_BOSSES = [95005, 95006]      # 마녀 · 우주 사령관: floor 0 (안 나옴, 테스트 패널로는 부를 수 있음)

# 보스 대사: id → {상황: [(ko, en), ...]}
BOSS_LINES = {
    95007: {'Intro': [('해피해피해피~!!', 'Happy happy happy~!!')], 'Attack': [('냥!', 'Nyan!'), ('통통!', 'Boing!')], 'Hit': [('냥?!', 'Nya?!'), ('안 해피해!', 'Not happy!')],
            'Down': [('해피… 해피…', 'Happy... happy...'), ('냐아아~', 'Nyaaa~')], 'Special': [('다 같이 해피~!', 'Everybody happy~!')], 'Skill': [('치피치피~', 'Chipi chipi~')]},
    95008: {'Intro': [('팝!!', 'POP!!')], 'Attack': [('팝!', 'Pop!'), ('뻐끔!', 'Gulp!')], 'Hit': [('팝?!', 'Pop?!'), ('캬악!', 'Hiss!')],
            'Down': [('팝… 팝…', 'Pop... pop...'), ('클릭 수가…!', 'My click count...!')], 'Special': [('전 세계 클릭 1등이다!', 'World click champion!')], 'Skill': [('팝팝팝!', 'Pop pop pop!')]},
    95009: {'Intro': [('우가! 쥐! 사냥!', 'Ooga! Rat! Hunt!')], 'Attack': [('우가!', 'Ooga!'), ('부가!', 'Booga!')], 'Hit': [('우우?!', 'Ooh?!'), ('아파 우가!', 'Ouch, ooga!')],
            'Down': [('불… 무서워…', 'Fire... scary...'), ('우가아아~', 'Oogaaa~')], 'Special': [('원숭이로 돌아간다!', 'Return to monke!')], 'Skill': [('이건… 불?!', 'This is... FIRE?!')]},
    95010: {'Intro': [('내 랩터들 건드리지 마.', "Don't touch my raptors.")], 'Attack': [('물러서!', 'Back off!'), ('조심해!', 'Careful!')], 'Hit': [('으윽!', 'Ugh!'), ('이건 계획에 없었어!', "This wasn't the plan!")],
            'Down': [('영리한… 녀석들…', 'Clever... girls...'), ('후퇴다!', 'Fall back!')], 'Special': [('블루, 가!', 'Blue, go!')], 'Skill': [('진정해, 진정…', 'Easy, easy...')]},
    95011: {'Intro': [('어서 오세요~ 공룡 체험관입니다!', 'Welcome to the dino experience!')], 'Attack': [('크아앙!', 'RAWR!'), ('둠칫!', 'Boom-tss!')], 'Hit': [('바람 빠져요!', "I'm deflating!"), ('시급이 얼만데!', "I'm not paid enough!")],
            'Down': [('퇴근할래요…', 'I wanna clock out...'), ('푸쉬이이~', 'Pssshhh~')], 'Special': [('팔이 안 닿아!!', "My arms can't reach!!")], 'Skill': [('둠칫둠칫~', 'Dum-dum-dance~')]},
    95012: {'Intro': [('어이, 여기 공사 중이여!', 'Hey, construction zone!')], 'Attack': [('어이쿠!', 'Whoa!'), ('비켜, 비켜!', 'Move it!')], 'Hit': [('아이고 허리야!', 'Oh, my back!'), ('일당 깎인다!', "There goes my pay!")],
            'Down': [('오늘 일당은 받아야 혀…', 'I still want my pay...'), ('칼퇴다아~', 'Clocking out~')], 'Special': [('조아쓰!! 가즈아!!', "Joassu!! Let's go!!")], 'Skill': [('포크레인 출동!', 'Excavator, go!')]},
    95013: {'Intro': [('츄르 인상 없인 일 안 한다냥!', 'No churu raise, no work!')], 'Attack': [('투쟁!', 'Strike!'), ('냥!', 'Nyan!')], 'Hit': [('노동 탄압이다!', 'Union busting!'), ('캬악!', 'Hiss!')],
            'Down': [('협상… 하자냥…', "Let's... negotiate..."), ('드르렁…', 'Zzz...')], 'Special': [('총파업이다냥!!', 'General strike!!')], 'Skill': [('피켓 들어!', 'Signs up!')]},
    95014: {'Intro': [('밥은 먹고 다니냐?', 'Have you even eaten?')], 'Attack': [('밥 먹어!', 'Eat up!'), ('더 먹어!', 'Have more!')], 'Hit': [('아이고!', 'Oh my!'), ('밥이 식잖아!', "The rice is getting cold!")],
            'Down': [('국 식는다…', 'The soup is cooling...'), ('설거지는 너희가 해!', 'You do the dishes!')], 'Special': [('밥은… 먹고 다니냐?', 'Have you... even eaten?')], 'Skill': [('고봉밥 나간다!', 'Heaping bowls coming!')]},
    95015: {'Intro': [('바닷속이 더 좋다냥~', "It's better down where it's wetter~")], 'Attack': [('찰싹!', 'Splash!'), ('냥!', 'Nyan!')], 'Hit': [('비늘이!', 'My scales!'), ('캬악!', 'Hiss!')],
            'Down': [('거품이 되어…', 'Turning into sea foam...'), ('냐아아~', 'Nyaaa~')], 'Special': [('파도야 와라냥!', 'Come, waves!')], 'Skill': [('언더 더 씨~', 'Under the sea~')]},
    95016: {'Intro': [('잠수 준비 완료냥.', 'Ready to dive.')], 'Attack': [('보글!', 'Blub!'), ('냥!', 'Nyan!')], 'Hit': [('유리에 금이!', 'My glass is cracking!'), ('캬악!', 'Hiss!')],
            'Down': [('부상한다냥…', 'Surfacing...'), ('보글보글…', 'Blub blub...')], 'Special': [('핑―', 'Ping—')], 'Skill': [('닻 내려라냥!', 'Drop anchor!')]},
    95017: {'Intro': [('빠밤… 빠밤…', 'Dun dun... dun dun...')], 'Attack': [('와앙!', 'Chomp!'), ('냥!', 'Nyan!')], 'Hit': [('지느러미가!', 'My fin!'), ('캬악!', 'Hiss!')],
            'Down': [('뚜루루뚜루…', 'Doo doo doo...'), ('냐아아~', 'Nyaaa~')], 'Special': [('샤크네이도다냥!', 'Sharknado time!')], 'Skill': [('아기상어 출동!', 'Baby sharks, go!')]},
    95018: {'Intro': [('포탑… 이쪽이 앞인가냥?', 'Turret... is this the front?')], 'Attack': [('발사!', 'Fire!'), ('냥!', 'Nyan!')], 'Hit': [('조준이 안 돼!', "Can't aim!"), ('캬악!', 'Hiss!')],
            'Down': [('포탑이 돌아간다…', 'The turret is spinning...'), ('냐아아~', 'Nyaaa~')], 'Special': [('전 포문 발사!', 'All guns, fire!')], 'Skill': [('어… 거기 아닌데?', 'Uh... wrong way?')]},
    95019: {'Intro': [('라이언 이병, 임무 수행 중이다냥!', 'Private Lion, on duty!')], 'Attack': [('어흥!', 'Roar!'), ('돌격!', 'Charge!')], 'Hit': [('갈기가!', 'My mane!'), ('위생병!', 'Medic!')],
            'Down': [('이병… 구해 줘…', 'Save... the private...'), ('어흐응…', 'Rrrgh...')], 'Special': [('하쿠나 마타타!', 'Hakuna matata!')], 'Skill': [('사자후!!', 'Lion roar!!')]},
    95020: {'Intro': [('츄르 담당만 남고 다 나가!!', 'Everyone out except the churu guy!!')], 'Attack': [('냥!!', 'Nyan!!'), ('안 돼!!', 'No!!')], 'Hit': [('작전이!', 'My plan!'), ('캬악!', 'Hiss!')],
            'Down': [('이건… 반칙이다…', "This... isn't fair..."), ('안경 어디 갔지…', 'Where are my glasses...')], 'Special': [('누가 츄르를 먹었냥!!', 'Who ate the churu!!')], 'Skill': [('연필 던진다!', 'Pencil incoming!')]},
    95021: {'Intro': [('...수상한 쥐들이군.', '...Sus rats.')], 'Attack': [('삐빅!', 'Beep!'), ('조사한다!', 'Investigating!')], 'Hit': [('나 크루원이야!', "I'm a crewmate!"), ('으악!', 'Argh!')],
            'Down': [('임포스터는… 쥐였어…', 'The impostor... was a rat...'), ('투표 결과…', 'Vote results...')], 'Special': [('긴급 회의!!', 'Emergency meeting!!')], 'Skill': [('환풍구로!', 'To the vent!')]},
    95022: {'Intro': [('눈이 뜨거워지는군냥.', 'My eyes are heating up.')], 'Attack': [('삐융!', 'Pew!'), ('냥!', 'Nyan!')], 'Hit': [('눈부셔!', 'Too bright!'), ('캬악!', 'Hiss!')],
            'Down': [('은하로 돌아간다냥…', 'Back to the galaxy...'), ('우이아…', 'Oiia...')], 'Special': [('우이아이아~', 'Oiia oiia~')], 'Skill': [('레이저 발사!', 'Laser on!')]},
    95023: {'Intro': [('싫어.', 'No.')], 'Attack': [('싫어.', 'No.'), ('귀찮아.', "Don't care.")], 'Hit': [('짜증 나.', 'Annoying.'), ('하.', 'Ugh.')],
            'Down': [('재밌었던 적 한 번 있었지. 최악이었어.', 'I had fun once. It was awful.'), ('…싫어.', '...No.')], 'Special': [('전부 싫어.', 'I hate everything.')], 'Skill': [('싫다고.', 'I said no.')]},
}

# ── 찍찍!! 훈장 9~16 (티어 테이블 Tier) ──
# (tier, (이름 ko,en), 아이콘, 조건 [(type, value)] (최대 4), 탄생 배율, 시작 쥐, 쥐 공격력 배율)
#   연구자료 비용 = gen_expansion_data 가 스테이지 테이블 research(조건 층) × 4 로 계산 (기획서 8장)
# 조각 레벨 합 (2026-10-19 저녁): 시뮬레이터(balance_sim)에서 조건 층에 닿을 때 조각 합(클릭·미클릭 중 작은 값)의 약 95% — 예전 240·690 은 10·15훈장 승급을 1시간 넘게 막아 49·50층, 100층 연속 벽이 생겼음
TIERS = [
    (9, ('지하 제국 총독', 'Underground Governor'), '🏰', [('Max_Floor', 31), ('Shard_Level_Sum', 170), ('Skill_Tree_Complete', 0)], 2.9, 13, 3.0),
    (10, ('초월 실험체', 'Transcendent Subject'), '🧬', [('Max_Floor', 41), ('Shard_Level_Sum', 200), ('Skill_Tree_Complete', 0)], 3.05, 13, 3.25),
    (11, ('연구소 점령자', 'Lab Occupier'), '🚩', [('Max_Floor', 51), ('Shard_Level_Sum', 310), ('Dex_Count', 72), ('Skill_Tree_Complete', 0)], 3.2, 14, 3.5),
    (12, ('도시의 그림자', 'Shadow of the City'), '🌃', [('Max_Floor', 61), ('Shard_Level_Sum', 370), ('Dex_Count', 75), ('Skill_Tree_Complete', 0)], 3.35, 14, 3.75),
    (13, ('쥐 공화국 의장', 'Rat Republic Chair'), '⚖️', [('Max_Floor', 71), ('Shard_Level_Sum', 450), ('Dex_Count', 78), ('Skill_Tree_Complete', 0)], 3.5, 15, 4.0),
    (14, ('행성 탈출자', 'Planet Escapee'), '🚀', [('Max_Floor', 81), ('Shard_Level_Sum', 510), ('Dex_Count', 81), ('Skill_Tree_Complete', 0)], 3.65, 15, 4.25),
    (15, ('은하 방랑자', 'Galaxy Wanderer'), '🌌', [('Max_Floor', 91), ('Shard_Level_Sum', 600), ('Dex_Count', 84), ('Skill_Tree_Complete', 0)], 3.8, 16, 4.5),
    (16, ('찍찍!! 신화', 'SQUEAK!! Myth'), '👑', [('Max_Floor', 85), ('Shard_Level_Sum', 690), ('Dex_Count', 87), ('Skill_Tree_Complete', 0)], 3.95, 16, 4.75),
]
COND_DOC = [('Dex_Count', '도감에서 만난 친구(쥐 종) 수 (2026-10-19)', '필요한 종 수'),
            ('Boss_Clear', '이 층까지 클리어 (마지막 보스 처치 = 100)', '클리어해야 하는 층')]
