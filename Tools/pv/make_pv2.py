# -*- coding: utf-8 -*-
"""itch.io 홍보 영상 v3 (사용자 콘티, 약 38.5초) — 장면을 프레임마다 그려 ffmpeg 로.

쓰는 법:  python Tools/pv/make_pv2.py [--draft] [--from 초] [--to 초] [--still 초 ...]
  --draft  = 960×540 · 30fps 미리보기          --from/--to = 그 구간만 (빠른 확인)
  --still  = 그 시각들의 한 장면만 png 로 (PV/_local/out/still_<초>.png)
  최종본(전체 · 초안 아님)만 PV/out/PV_v3.mp4, 초안·부분·스틸은 PV/_local/out (git 무시, 로컬 전용)

자료: 그림 = UnityResources/Rats/PV/Sheets/v3 (Codex), 인게임 = PV/clips/<이름>.mp4 (Recorder). 없으면 회색 판.
음악 BGM_PV.mp3: 곡 61.03초(마지막 쾅) = 영상 37.0초(윙크) → 곡 24.03초부터. 120 BPM → 컷 경계 0.5초 단위.
"""
import math, os, random, subprocess, sys
from PIL import Image
sys.path.insert(0, os.path.dirname(__file__))
import pvkit as K
from pvkit import seg, lerp, clamp, out_back, out_cubic, in_cubic, in_out, out_elastic, slam, pop, word, letters, Burst, shake

ART = 'UnityResources/Rats/PV/Sheets/v3'
CLIPS = 'PV/clips'
MUSIC, MUSIC_START = 'PV/music_v6.wav', 0.0     # 사용자 곡 Operation_Cheddar(BGM_PV2.mp3): 9.05~34.10초 + 46.50~ 이어 붙임 (곡 31.5초 정적 = AND, 곡 55.04초 쾅 = 윙크 33.5초). 만들기: MUSIC_CMD
MUSIC_CMD = ('ffmpeg -i UnityResources/Audio/BGM/BGM_PV2.mp3 -filter_complex "[0:a]atrim=start=9.05:end=34.10,asetpts=PTS-STARTPTS[a];'
             '[0:a]atrim=start=46.50:end=58.0,asetpts=PTS-STARTPTS[b];[a][b]acrossfade=d=0.05:c1=tri:c2=tri,afade=out:st=34.2:d=1.3[o]" -map "[o]" -ar 48000 PV/music_v6.wav')
SFX = 'UnityResources/Audio/SFX'
DUR = 35.0

# 화면에 박히는 글 (영어 · 쥐 말투)
T_RUN = 'RUN!!!!'
T = dict(smash='SMASH!!!', swarm='SWARM!!!', steal='STEAL!!!', and_='AND', upgrade='UPGRADE!!', run='RUN!!!!',
         boss='BOSS EVERY 5 FLOORS!', play='PLAY NOW', plus='+1',
         hits=['CRASH!', 'BAM!', 'POW!', 'CRUNCH!', 'KABOOM!'], ults='ULTIMATES!', skills='LEVEL UP!')


# ───────── 자료 ─────────
def A(name, key=True):
    p = f'{ART}/{name}'
    return K.img(p, key=key) if os.path.exists(K.P(p)) else None


def placeholder(name, w=900, h=900):
    k = ('ph', name)
    if k not in K._cache:
        im = Image.new('RGBA', (w, h), (120, 120, 130, 200))
        from PIL import ImageDraw
        ImageDraw.Draw(im).text((20, h // 2), name, fill=(255, 255, 255, 255))
        K._cache[k] = im
    return K._cache[k]


def art(name, w=900, h=900, key=True):
    return A(name, key) or placeholder(name, w, h)


def sheet(name, cols, rows):
    """자홍 시트 → 조각 목록. 덩어리 수가 칸 수와 같으면 덩어리로 (칸 경계에 걸쳐도 안전), 아니면 같은 크기 칸으로"""
    p = f'{ART}/{name}'
    if not os.path.exists(K.P(p)): return [placeholder(f'{name}#{i}') for i in range(cols * rows)]
    b = K.blobs(p, 20000)
    return b if len(b) == cols * rows else K.split_sheet(p, cols, rows)


def fx(i):
    """fx_sheet 4×3: 0 외침 말풍선 1 BOOM 2 충격 별 3 집중선 고리 4 반짝 5 땀 6 벡터 조각 7 먼지 8 하트 9 치즈 조각 10 휙 11 위험 !"""
    return sheet('fx_sheet.png', 4, 3)[i]


def bgimg(name):
    return art(name, 1920, 1080, key=False)


clips = {}


def clip(fr, name, t_local, zoom=(1.0, 1.08), dur=1.0, flip=False):
    """인게임 클립 + 트래킹 줌. 없으면 회색 판"""
    c = clips.get(name)
    if c is None:
        c = clips[name] = K.Clip(f'{CLIPS}/{name}.mp4', fr.S, FPS)
    im = c.get(t_local)
    if im is None:
        fr.fill((70, 70, 78))
        fr.put(word('IN-GAME: ' + name, 70, fill=(200, 200, 205)), 960, 540)
        return
    z = lerp(zoom[0], zoom[1], in_out(t_local / max(dur, 0.01)))
    fr.bg(im.transpose(Image.FLIP_LEFT_RIGHT) if flip else im, z)


def speed_ring(fr, t, t0, a=0.8, scale=1.6):
    k = seg(t, t0, 0.5)
    if 0 < k < 1: fr.put(fx(3), 960, 540, scale * lerp(0.8, 1.3, k), (t * 200) % 360, a * (1 - k))


def flash_in(fr, t, t0, d=0.16):
    k = seg(t, t0, d)
    if k < 1: fr.flash(1 - k)


SPARK = None


def sparks():
    global SPARK
    if SPARK is None: SPARK = [fx(4), fx(6), fx(9)]
    return SPARK


bursts = {}


def burst(key, *a, **kw):
    if key not in bursts: bursts[key] = Burst(*a, **kw)
    return bursts[key]


# ───────── 장면 ─────────
def s_title(fr, t):          # 0.0–4.0 쥐가 아래에서 튀어 오르고 제목 세 단어 탁탁탁
    fr.bg(bgimg('bg_yellow.png'), lerp(1.0, 1.1, t / 4), 0.5, 0.5)
    speed_ring(fr, t, 0.15, 0.7, 1.8)
    # 쥐: 아래 → 튀어 오름 → 둥실
    k = seg(t, 0.0, 0.45)
    y = lerp(1500, 690, out_back(k, 1.4)) + math.sin(max(0, t - 0.45) * 5) * 10
    sq = 1 + 0.12 * math.sin(min(1, k) * math.pi)
    fr.put(art('jump_rat.png'), 400, y, 0.52, -6 + math.sin(t * 4) * 3, sx=1 / sq, sy=sq)
    words = [('word_1.png', 1050, 250, -7, 0.9), ('word_2.png', 1270, 490, -4, 1.4), ('word_3.png', 1450, 760, -2, 1.9)]
    for i, (n, x, yy, r, t0) in enumerate(words):
        im = A(n) or word(['Squeak!', 'Squeak!!', 'Squeak!!!'][i], 170, fill=K.YELLOW, fill2=(255, 150, 40))
        sc = 0.5 if A(n) else 1.0
        if slam(fr, im, x, yy + math.sin((t - t0) * 6) * 4, t, t0, sc, r):
            burst(f'title{i}', x, yy, t0, sparks(), n=12, speed=(700, 1400), life=0.5, scale=(0.15, 0.35), seed=i).draw(fr, t)
    dx, dy, rot = (0, 0, 0)
    for t0 in (0.9, 1.4, 1.9):
        s = shake(t, t0, 0.3, 22); dx += s[0]; dy += s[1]; rot += s[2]
    z = 1 + 0.25 * in_cubic(seg(t, 3.6, 0.4))           # 끝: 확 밀고 들어감
    fr.cam = (dx, dy, z, rot)
    if t > 3.85: fr.flash(seg(t, 3.85, 0.15))


RATS = ['labrat', 'brownrat', 'mouse', 'blackrat', 'hooded', 'labrat', 'fieldmouse', 'brownrat', 'mouse', 'labrat']


def s_smash(fr, t):          # 4.0–6.0 SMASH!!! → 리그 쥐들이 달려와 날려 버림
    lt = t - 4.0
    fr.bg(bgimg('bg_blue.png'), lerp(1.05, 1.0, lt / 2))
    flash_in(fr, t, 4.0)
    rnd = random.Random(7)
    lets = letters(T['smash'], 300, fill=(255, 90, 70), fill2=(200, 40, 40))
    hit_t = []
    # 쥐 떼: 오른쪽 밖 → 왼쪽으로 질주 (게임 리그 달리기)
    lead_x = lerp(2300, -900, seg(lt, 0.55, 1.15))
    for i, code in enumerate(RATS):
        ox = (i % 5) * 230 + rnd.uniform(-40, 40); oy = 620 + (i // 5) * 170 + rnd.uniform(-30, 30)
        x = lead_x + ox
        if -400 < x < 2300 and lt > 0.5:
            im, foot = K.rat_image(code, K.rat_pose_run(lt * 28 + i, 1.2, True), 330)
            fr.put(im, x, oy, 1.0, ax=foot[0] / im.width, ay=foot[1] / im.height)
    for j, (im, ox) in enumerate(lets):
        x0, y0 = 960 + ox, 470
        th = 0.55 + (2300 - x0) / (3200 / 1.15) * 1.0 if True else 0
        th = 0.55 + clamp((2300 - x0) / 3200) * 1.15 - 0.05     # 쥐 선두가 그 글자에 닿는 때
        hit_t.append(th)
        if lt < th:
            slam(fr, im, x0, y0, lt, 0.0 + j * 0.03, 1.0, rnd.uniform(-4, 4), dur=0.25)
        else:
            d = lt - th; r2 = random.Random(j)
            vx, vy = -r2.uniform(500, 1100), -r2.uniform(900, 1500)
            fr.put(im, x0 + vx * d, y0 + vy * d + 2200 * d * d, 1.0 + d * 0.4, r2.uniform(200, 700) * d * (1 if j % 2 else -1))
            burst(f'sm{j}', x0, y0, 4.0 + th, [fx(2), fx(7), fx(6)], n=6, speed=(500, 1100), life=0.45, scale=(0.2, 0.4), seed=j).draw(fr, t)
    s = shake(lt, min(hit_t) if hit_t else 9, 0.6, 30)
    fr.cam = (s[0], s[1], 1.0, s[2])


def hit_word(fr, lt, i, dur):
    rnd = random.Random(40 + i)
    x, y = rnd.choice([(420, 260), (1480, 280), (460, 820), (1460, 800)]); r = rnd.uniform(-14, 14)
    im = lw('hits', (word(T['hits'][i % 5], 190, fill=K.YELLOW, fill2=(255, 140, 30)), i % 5))
    x = clamp(x, im.width / 2 + 60, 1920 - im.width / 2 - 60)      # 긴 단어도 화면 안에
    slam(fr, im, x, y, lt, 0.05, 1.0, r, dur=0.18)


def s_game_smash(fr, t):     # 6.0–9.0 인게임 물건 날리기 ×5
    cuts = [(6.0, 0.5), (6.5, 0.5), (7.0, 0.5), (7.5, 0.5), (8.0, 1.0)]
    for i, (t0, d) in enumerate(cuts):
        if t0 <= t < t0 + d:
            lt = t - t0
            clip(fr, f'smash_{i + 1}', lt, (1.04, 1.12), d)
            flash_in(fr, t, t0, 0.1)
            hit_word(fr, lt, i, d)
            s = shake(lt, 0.05, 0.3, 16); fr.cam = (s[0], s[1], 1, s[2])


def s_swarm(fr, t):          # 9.0–11.5 두 쥐가 아래에서 튀어 올라 공중에서 부둥켜안고 윙크 → SWARM!!! → 쥐가 쏟아져 나옴
    lt = t - 9.0
    fr.bg(bgimg('bg_pink.png'), lerp(1.0, 1.08, lt / 2.5))
    flash_in(fr, t, 9.0)
    meet = 0.5
    if lt < meet:
        k = out_cubic(seg(lt, 0.0, meet))
        fr.put(art('leap_L.png'), lerp(420, 800, k), lerp(1500, 560, k), 0.42, lerp(-20, -6, k))
        fr.put(art('leap_R.png'), lerp(1500, 1120, k), lerp(1500, 560, k), 0.42, lerp(20, 6, k))
    else:
        k = seg(lt, meet, 0.3)
        sc = lerp(1.25, 1.0, out_back(k, 2.5))
        fr.put(art('hug_rats.png'), 960, 560 + math.sin((lt - meet) * 5) * 10, 0.4 * sc, math.sin((lt - meet) * 4) * 3)
        burst('sw_heart', 960, 520, 9.0 + meet, [fx(8)], n=12, speed=(600, 1300), life=0.9, scale=(0.12, 0.3), seed=5, grav=600).draw(fr, t)
        pop(fr, lw('swarm', (word(T['swarm'], 280, fill=(255, 140, 180), fill2=(235, 70, 120)),)), 960, 170, lt, meet + 0.05, 1.0, -3, dur=0.45)
        if lt < meet + 0.08: fr.flash(0.6)
    # 작은 쥐들이 점점 많이 달려 나옴 (번식 → 무리)
    if lt > 1.1:
        rnd = random.Random(11)
        n = int(min(28, (lt - 1.1) * 26))
        for i in range(n):
            born = 1.1 + i / 26
            d = lt - born
            dirx = -1 if i % 2 else 1
            x = 960 + dirx * (100 + d * rnd.uniform(500, 900)); y = rnd.uniform(860, 1060)
            im, foot = K.rat_image(RATS[i % len(RATS)], K.rat_pose_run(lt * 26 + i, 1.1), 170)
            fr.put(im, x, y, out_back(seg(d, 0, 0.2)), ax=foot[0] / im.width, ay=foot[1] / im.height, flip=dirx > 0)
    s_ = shake(lt, meet, 0.35, 28); fr.cam = (s_[0], s_[1], 1, s_[2])


def s_game_breed(fr, t):     # 11.5–14.0 인게임 번식 ×4
    cuts = [(11.5, 0.5), (12.0, 0.5), (12.5, 0.5), (13.0, 1.0)]
    for i, (t0, d) in enumerate(cuts):
        if t0 <= t < t0 + d:
            lt = t - t0
            clip(fr, f'breed_{i + 1}', lt, (1.1, 1.0), d)
            flash_in(fr, t, t0, 0.1)
            rnd = random.Random(60 + i)
            for j in range(3):
                pop(fr, lw('plus', (word(T['plus'], 120, fill=(255, 140, 180), fill2=(235, 70, 120)),)), rnd.uniform(300, 1600), rnd.uniform(200, 800), lt, 0.05 + j * 0.12, 1.0, rnd.uniform(-12, 12))
    if 13.5 <= t < 14.0 and t > 13.85: fr.flash(seg(t, 13.85, 0.15))


THIEF = None


def s_steal(fr, t):          # 14.0–16.5 STEAL!!! → 도둑 쥐가 글자를 하나씩 잡아 떼서 사방으로 흩어져 도망
    lt = t - 14.0
    fr.bg(bgimg('bg_yellow.png'), 1.15, 0.3, 0.6)
    fr.overlay((40, 30, 60), 0.35)
    flash_in(fr, t, 14.0)
    thieves = sheet('thief_sheet.png', 3, 1)
    truns = K.anim_rows(f'{ART}/thief_run.png', 4, 3)
    lets = img_letters('let_steal.png', 'STEAL!!!', 0.5)
    n = len(lets)
    rnd = random.Random(5)
    order = list(range(n)); rnd.shuffle(order)
    dirs = [(-1, -0.6), (1, -0.7), (-1, 0.4), (1, 0.5), (-0.3, -1), (0.4, 1), (-1, 0.0), (1, -0.1)]
    for j, (im, ox) in enumerate(lets):
        x0, y0 = 960 + ox, 480
        g = 0.55 + order.index(j) * 0.16                # 잡는 순간
        dx, dy = dirs[j % len(dirs)]
        L = math.hypot(dx, dy); dx, dy = dx / L, dy / L
        trow = truns[j % 3]
        tsc = 0.6 * thieves[j % 3].height / trow[0].height         # 예전 도둑 그림보다 크게
        thief = trow[int(lt * 16 + j * 1.3) % 4]                     # Codex 달리기 4프레임 (사용자: 이미지로 자연스럽게)
        flip = dx > 0                                     # 도둑 그림은 왼쪽을 봄
        if lt < g:
            slam(fr, im, x0, y0, lt, 0.0, 1.0, 0, dur=0.22)
            # 도둑이 도망갈 방향 반대쪽에서 몰래 다가옴
            k = seg(lt, g - 0.25, 0.25)
            if k > 0:
                tx, ty = x0 - dx * 900 * (1 - k), y0 + 260 - dy * 500 * (1 - k)
                fr.put(thief, tx, ty, tsc, math.sin(lt * 22 + j) * 3, flip=flip)
        else:
            d = lt - g
            tx = x0 + dx * 2400 * d * d * 1.6 + dx * 300 * d
            ty = y0 + 260 + dy * 1600 * d * d * 1.6 + dy * 200 * d - abs(math.sin(d * 24)) * 18
            if -400 < tx < 2320 and -400 < ty < 1500:
                bob = abs(math.sin(d * 25 + j)) * 10
                fr.put(thief, tx, ty - bob, tsc, math.sin(d * 24 + j) * 3, flip=flip)
                fr.put(im, tx + (8 if flip else -8), ty - bob - thief.height * tsc * 0.5 - im.height * 0.85 * 0.3, 0.85, math.sin(d * 18 + j) * 6)
            if d < 0.2: fr.put(fx(10), x0, y0, 0.4, -math.degrees(math.atan2(dy, dx)), 1 - d / 0.2)
    sh = shake(lt, 0.0, 0.3, 20); fr.cam = (sh[0], sh[1], 1, sh[2])


def s_upgrade(fr, t):        # 16.5–19.5 AND UPGRADE!! → 근육 쥐 근육이 한 단계씩
    lt = t - 16.5
    fr.bg(bgimg('bg_blue.png'), lerp(1.0, 1.12, lt / 3), 0.5, 0.6)
    flash_in(fr, t, 16.5)
    slam(fr, lw('upgrade', (word(T['upgrade'], 260, fill=(120, 230, 120), fill2=(40, 170, 80)),)), 960, 190, lt, 0.05, 1.0, -4)     # 'AND' 뺌 (사용자)
    stages = sheet('muscle_sheet.png', 4, 1)
    times = [0.5, 1.0, 1.5, 2.0]
    st = max([i for i, x in enumerate(times) if lt >= x], default=-1)
    if st >= 0:
        k = seg(lt, times[st], 0.25)
        s = (1.25 - 0.25 * out_back(k)) if st else out_back(seg(lt, 0.5, 0.3))
        im = stages[st]
        fr.put(im, 960, 1060, 0.62 * s, ax=0.5, ay=1.0)
        burst(f'up{st}', 960, 640, 16.5 + times[st], [fx(4), fx(6)], n=12, speed=(600, 1300), life=0.5, scale=(0.15, 0.35), seed=st).draw(fr, t)
        if st == 3: speed_ring(fr, lt, 2.0, 0.9, 2.0)
    cam = shake(lt, max([x for x in times if lt >= x], default=-9), 0.3, 10 + 8 * max(st, 0))
    fr.cam = (cam[0], cam[1], 1, cam[2])


def s_game_power(fr, t):     # 19.5–23.5 공용 스킬 노드 + 필살기
    # 필살기 3개 × 1초 = 게임 컷인 배너 0.27초 + 연출 (사용자: 개수 줄여 명확하게): 파라오 피라미드 · 대멸종 · 람쥐 썬더
    cuts = [('skill_1', 19.5, 1.0)] + [(f'ult_{k + 1}', 20.5 + k, 1.0) for k in range(3)]
    for name, t0, d in cuts:
        if t0 <= t < t0 + d:
            lt = t - t0
            clip(fr, name, lt, (1.02, 1.1), d)
            flash_in(fr, t, t0, 0.12)
            if name == 'skill_1':
                slam(fr, lw('skills', (word(T['skills'], 170, fill=(120, 230, 120), fill2=(40, 170, 80)),)), 1320, 200, lt, 0.1, 1.0, 6)


def s_game_power_top(fr, t):
    if t >= 20.5:
        slam(fr, lw('ults', (word(T['ults'], 170, fill=K.YELLOW, fill2=(255, 120, 30)),)), 620, 960, t - 20.5, 0.05, 1.0, -5)      # 필살기 이름 배너 안 가리게 아래


def cheese_piece():
    k = ('cheese1',)
    if k not in K._cache:
        g = fx(9)
        c = g.crop((0, 0, g.width // 3, g.height // 2)); bb = c.getbbox()
        K._cache[k] = c.crop(bb) if bb else g
    return K._cache[k]


def s_run(fr, t):            # 23.5–28.5 고양이가 카운터 뒤에서 솟음 (카메라 틸트 업) → RUN!!!! 사방으로 → 우왕좌왕 치즈 던짐 → 고양이 당황
    lt = t - 23.5
    bg = bgimg('cat2_bg.png')
    BWW = 1920; sc_bg = BWW / bg.width; BHH = bg.height * sc_bg     # 배경 = 화면 폭, 세로로 김 (월드 y = 배경 px)
    ledge = 0.495 * BHH
    # 카메라 틸트: 바닥(아래) → 고양이(위)
    y0 = lerp(ledge - 250, ledge - 780, in_out(seg(lt, 1.0, 1.1)))
    y0 -= 60 * in_out(seg(lt, 4.0, 0.6))                           # 끝에 고양이 얼굴 쪽으로 살짝 더
    tiltrot = 2.5 * math.sin(math.pi * seg(lt, 1.0, 1.1))           # 틸트 중 살짝 기울어짐
    def W(y): return y - y0                                          # 월드 y → 화면 y
    crop = bg.crop((0, int(y0 / sc_bg), bg.width, int((y0 + 1080) / sc_bg)))
    fr.bg(crop, 1.0)
    flash_in(fr, t, 23.5)
    # 고양이: 카운터 뒤에서 솟음 (1.0–2.0)
    cat = art('cat2_cheese.png' if lt >= 4.0 else 'cat2_rise.png', 1600, 1600)
    csc = 860 / cat.width
    rise = out_cubic(seg(lt, 1.0, 1.0))
    rumble = math.sin(lt * 45) * 6 if 1.0 < lt < 2.0 else 0
    cat_bottom = ledge + 40 + (1 - rise) * cat.height * csc
    fr.put(cat, 960 + rumble, W(cat_bottom) + math.sin(lt * 3) * 6 * rise, csc, ax=0.5, ay=1.0)
    if lt >= 4.0 and lt < 4.6: pop(fr, word('?', 200, fill=K.CREAM), 1330, W(ledge - 700), lt, 4.0, 1.0, 10)
    # 카운터 앞(바닥)을 다시 그려 고양이 아래를 가림
    ls = int(max(0, W(ledge)))
    if ls < 1080:
        part = crop.crop((0, int(ls / sc_bg), crop.width, crop.height))
        fr.put(part, 0, ls, sc_bg, ax=0, ay=0)
    # 쥐들: 바닥 (월드 y)
    P = sheet('play_sheet.png', 4, 3)
    F = sheet('cat2_flee.png', 3, 2)
    Cl = sheet('cat2_clumsy.png', 4, 1)
    run_t = 2.0
    spots = [(330, ledge + 130), (640, ledge + 230), (1290, ledge + 230), (1600, ledge + 130), (800, ledge + 300), (1450, ledge + 300)]
    dirs = [(-1.0, 0.5), (-0.6, 1.0), (0.6, 1.0), (1.0, 0.5), (-0.2, 1.0), (0.3, 1.0)]
    for i, (x, y) in enumerate(spots):
        if lt < run_t:
            im = P[i % 3] if i < 3 else P[(i % 3)]
            fr.put(P[[0, 1, 2, 0, 1, 2][i]], x, W(y) + math.sin(lt * 5 + i) * 5, 0.36, ax=0.5, ay=1.0, flip=i >= 3)
            if lt > 1.5: pop(fr, fx(11), x, W(y) - 250, lt, 1.5 + i * 0.04, 0.18)
        else:
            d = lt - run_t
            k = d / 0.8
            if k < 1.2:
                dx, dy = dirs[i]
                fr.put(F[i], x + dx * 1400 * k, W(y) + dy * 700 * k - abs(math.sin(d * 28)) * 18, 0.4 * (1 + 1.4 * k), ax=0.5, ay=1.0)
    # 우왕좌왕 쥐: 고양이 바로 아래
    cx, cy = 960, ledge + 200
    if lt < run_t:
        fr.put(P[3], cx, W(cy) + math.sin(lt * 5) * 5, 0.36, ax=0.5, ay=1.0)
    elif lt < 3.4:
        k = int((lt - run_t) / 0.18) % 2
        fr.put(Cl[k], cx + (k * 2 - 1) * 25, W(cy) - abs(math.sin(lt * 25)) * 25, 0.4, ax=0.5, ay=1.0)
        fr.put(fx(5), cx + 90, W(cy) - 330, 0.18, alpha=0.9)
    elif lt < 3.9:
        fr.put(Cl[2], cx, W(cy), 0.4, ax=0.5, ay=1.0)
    else:
        d = lt - 3.9
        fr.put(Cl[3], cx + d * 300, W(cy) + d * 900, 0.4 * (1 + 2.5 * d), ax=0.5, ay=1.0)
    if 3.4 <= lt < 4.0:                                     # 치즈가 고양이 앞발로 붕
        d = (lt - 3.4) / 0.6
        fr.put(cheese_piece(), lerp(cx, 980, d), lerp(W(cy) - 300, W(ledge - 760), out_cubic(d)) - math.sin(d * math.pi) * 150, 0.5, d * 720)
    if lt >= run_t:
        slam(fr, lw('run', (word(T['run'], 220, fill=(255, 90, 70), fill2=(200, 40, 40)),)), 420 + math.sin(lt * 50) * 6, 320, lt, run_t, 1.0, 12)
    s_ = shake(lt, run_t, 0.5, 30)
    fr.cam = (s_[0] + rumble, s_[1], 1.0 + 0.05 * in_out(seg(lt, 4.0, 0.6)), s_[2] + tiltrot)
    if t > 28.35: fr.flash(seg(t, 28.35, 0.15))


def s_boss(fr, t):           # 28.5–32.5 보스 기술 ×6
    cuts = [(28.5, 1.5), (30.0, 1.0), (31.0, 1.5)]          # 고양이 보스 3마리만 길게 (사용자: 개수 줄여 명확하게): 메인쿤 · 마녀 고양이 · 우주 고양이
    for i, (t0, d) in enumerate(cuts):
        if t0 <= t < t0 + d:
            lt = t - t0
            clip(fr, f'boss_{i + 1}', lt, (1.08, 1.0) if i % 2 else (1.0, 1.08), d)
            flash_in(fr, t, t0, 0.1)
            s = shake(lt, 0.05, 0.3, 18); fr.cam = (s[0], s[1], 1, s[2])
    lt = t - 28.5
    slam(fr, lw('boss', (word(T['boss'], 140, fill=(255, 90, 70), fill2=(200, 40, 40)),)), 960, 950, lt, 0.05, 1.0, -4, dur=0.2)     # 보스 머리 안 가리게 아래
    if t > 32.35: fr.flash(seg(t, 32.35, 0.15))


def s_play(fr, t):           # 32.5–38.5 새 끝 화면: 로고 + PLAY NOW + 매달린 쥐 → 아이리스 → 윙크
    lt = t - 32.5
    fr.bg(bgimg('end_bg.png'), lerp(1.06, 1.0, out_cubic(seg(lt, 0, 1.5))))
    k_ = seg(lt, 0.5, 0.4)                          # PLAY NOW 가 박히면 배경을 살짝 흐리고 아래를 어둡게 (글자+쥐가 앞으로)
    if k_ > 0:
        blur_all(fr, 5 * k_)
        fr.im.alpha_composite(bottom_shade(fr.W, fr.H, 0.5 * k_))
    flash_in(fr, t, 32.5, 0.2)
    logo = art('end_logo.png', 1200, 600)
    lsc = 760 / logo.width
    slam(fr, logo, 960, 270, lt, 0.0, lsc, 0, dur=0.3, from_scale=2.0)
    burst('logo', 960, 330, 32.5, [fx(9), fx(4)], n=16, speed=(700, 1500), life=0.8, scale=(0.15, 0.3), seed=8, grav=900).draw(fr, t)
    # PLAY NOW + 매달린 쥐 = Gemini 한 장 그림 (눈 뜸 / 같은 그림의 윙크 판). 두 장 다 원래 캔버스(2752×1536) 그대로 키잉 → 위치 같음
    wink = lt >= 4.5
    im = wink_merge() if wink else gfull('play_hang.png')
    sc = 0.45
    LX, LY = 1376, 575                              # 원본에서 글자 가운데 (화면 960, 600 에 놓음)
    FX, FY = 2070, 850                              # 원본에서 쥐 얼굴
    px, py = 960, 650
    sway = math.sin((lt - 0.5) * 5) * 2.0 * math.exp(-max(0, lt - 0.5) * 1.2) if lt > 0.75 else 0
    sh = soft_shadow(im)                            # 스티커 테두리 대신 은은한 그림자 (뒤 쥐 무리에서 떠 보이게)
    slam(fr, sh, px + 6, py + 16, lt, 0.5, sc, -2 + sway, dur=0.25, ax=LX / im.width, ay=LY / im.height)
    slam(fr, im, px, py, lt, 0.5, sc, -2 + sway, dur=0.25, ax=LX / im.width, ay=LY / im.height)
    fx_, fy_ = px + (FX - LX) * sc, py + (FY - LY) * sc
    if wink:
        burst('wink', fx_ - 40, fy_ - 30, 32.5 + 4.5, [fx(4)], n=4, speed=(250, 450), life=0.6, scale=(0.06, 0.12), seed=3, aim=-120, spread=80).draw(fr, t)
    if lt > 3.0:
        r = lerp(1500, 210, in_out(seg(lt, 3.0, 1.0)))
        if lt > 5.1: r = lerp(210, 0, in_cubic(seg(lt, 5.1, 0.4)))
        fr.iris(fx_, fy_, r)
    s_ = shake(lt, 0.5, 0.35, 22); fr.cam = (s_[0], s_[1], 1, s_[2])


# ═════════ 초반 v4 (사용자 콘티 2026-10-13) ═════════
GEM = 'UnityResources/Rats/PV/Sheets/v3/gem'


def G(name, w=900, h=900):
    p = f'{GEM}/{name}'
    return K.img(p, key=True) if os.path.exists(K.P(p)) else placeholder(name, w, h)


def gfull(name):
    """Gemini 그림을 자르지 않고 키잉만 (윙크 판 등 같은 캔버스끼리 위치 맞춤)"""
    k = ('gfull', name)
    if k not in K._cache:
        p = f'{GEM}/{name}'
        K._cache[k] = K.key_magenta(Image.open(K.P(p))) if os.path.exists(K.P(p)) else placeholder(name, 2752, 1536)
    return K._cache[k]


def wink_merge():
    """눈 뜬 판에 윙크 판의 얼굴 부분만 부드럽게 끼움 (나머지 미세한 차이로 깜빡이지 않게)"""
    k = ('wink_merge',)
    if k not in K._cache:
        base = gfull('play_hang.png')
        if not os.path.exists(K.P(f'{GEM}/play_hang_wink.png')): return base
        from PIL import ImageDraw, ImageFilter
        m = Image.new('L', base.size, 0)
        ImageDraw.Draw(m).ellipse((1890, 690, 2250, 1010), fill=255)
        K._cache[k] = Image.composite(gfull('play_hang_wink.png'), base, m.filter(ImageFilter.GaussianBlur(20)))
    return K._cache[k]


def bottom_shade(W, H, a):
    """위는 투명 → 아래로 갈수록 어두운 막"""
    k = ('shade', W, H, round(a, 2))
    if k not in K._cache:
        import numpy as np
        y = np.clip((np.arange(H) / H - 0.35) / 0.65, 0, 1) ** 1.3 * a * 255
        al = np.repeat(y[:, None], W, axis=1).astype(np.uint8)
        sh = Image.new('RGBA', (W, H), (30, 15, 35, 0)); sh.putalpha(Image.fromarray(al, 'L'))
        K._cache[k] = sh
    return K._cache[k]


def soft_shadow(im, blur=28, a=0.55):
    """그림 모양 그대로 흐린 검은 그림자"""
    k = ('shadow', id(im), blur, a)
    if k not in K._cache:
        from PIL import ImageFilter
        al = im.getchannel('A').filter(ImageFilter.GaussianBlur(blur)).point(lambda v: int(v * a))
        sh = Image.new('RGBA', im.size, (40, 20, 30, 0)); sh.putalpha(al)
        K._cache[k] = sh
    return K._cache[k]


def gsheet(name, cols, rows):
    p = f'{GEM}/{name}'
    if not os.path.exists(K.P(p)): return [placeholder(f'{name}#{i}') for i in range(cols * rows)]
    b = K.blobs(p, 20000)
    return b if len(b) == cols * rows else K.split_sheet(p, cols, rows)


def blur_all(fr, r=10):
    from PIL import ImageFilter
    fr.im = fr.im.filter(ImageFilter.GaussianBlur(r * fr.S))


def o_title(fr, t):          # 0.0–3.0 쥐가 아래에서 튀어 올라 머리로 단어를 쳐 올림 → 단어가 날아가 가운데 메인 제목 자리에 박힘 → 쥐는 다시 아래로
    fr.bg(bgimg('bg_yellow.png'), lerp(1.0, 1.06, t / 3))
    rats = gsheet('title_rats.png', 3, 1)
    W = [(620, 250, -6, 0.58), (960, 540, -3, 0.6), (1300, 830, 0, 0.62)]       # 제목 자리 (계단식)
    HIT = [0.35, 0.8, 1.25]                                                      # 헤딩 순간
    for i in range(3):
        x, y, r, sc = W[i]
        hx = x + (i - 1) * 40
        th = HIT[i]
        d = t - (th - 0.22)
        if d < 0: continue
        head_y = y + 260                                       # 헤딩하는 높이 (쥐 머리)
        if t < th: ry = lerp(1600, head_y, out_cubic(d / 0.22)); sq = 1.12
        else:
            e = t - th
            ry = head_y + (lerp(0, 40, out_cubic(seg(e, 0, 0.08))) if e < 0.25 else lerp(40, 1500, in_cubic(seg(e, 0.25, 0.4))))
            sq = lerp(0.82, 1.0, out_back(seg(e, 0, 0.2)))     # 부딪힌 순간 납작
        if ry < 1500:
            fr.put(rats[i], hx, ry, 0.36, (-8, 0, 8)[i], sx=2 - sq, sy=sq, ay=0.12)
        # 단어: 헤딩 전엔 없음 → 머리에서 튕겨 올라 제목 자리로 (살짝 넘쳤다 자리 잡음)
        if t >= th:
            e = t - th
            k = seg(e, 0, 0.32)
            wy = lerp(head_y - 60, y, out_back(k, 2.0))
            wr = r + (1 - k) * (20 if i % 2 else -20) + math.sin(max(0, e - 0.32) * 7) * 1.5 * math.exp(-max(0, e - 0.32) * 2)
            pulse = 1 + 0.03 * math.sin(max(0, t - 1.6) * 6) * (t > 1.6)
            fr.put(A(['word_1.png', 'word_2.png', 'word_3.png'][i]), x, wy, sc * lerp(0.5, 1.0, out_back(k, 1.5)) * pulse, wr)
            fr.put(fx(2), hx, head_y - 40, 0.35 * (1 - seg(e, 0, 0.25)), e * 200, 1 - seg(e, 0, 0.25))   # 콩! 충격 별
            burst(f'ot{i}', x, y, th + 0.3, sparks(), n=12, speed=(700, 1400), life=0.5, scale=(0.12, 0.3), seed=i).draw(fr, t)
    dx = dy = rot = 0
    for th in HIT:
        sh = shake(t, th, 0.25, 18); dx += sh[0]; dy += sh[1]; rot += sh[2]
    fr.cam = (dx, dy, 1 + 0.25 * in_cubic(seg(t, 2.65, 0.35)), rot)
    if t > 2.85: fr.flash(seg(t, 2.85, 0.15))


def o_smash(fr, t):          # 3.0–5.5 SMASH! → 왼쪽에서 쥐 무리 → 글자 튕겨 나감 → 무리가 오른쪽 화면 밖으로 빠져나감 (카메라 고정, 사용자: 화면 겹치지 않게)
    lt = t - 3.0
    speed = 1950                 # 마지막 쥐가 5.4초쯤 오른쪽 밖으로
    lead = -700 + speed * (lt - 0.35)
    cam_x = 0
    fr.bg(bgimg('bg_blue.png'), lerp(1.0, 1.06, lt / 2.5))
    flash_in(fr, t, 3.0)
    lets = img_letters('let_smash.png', 'SMASH!', 0.55)
    hit = []
    for j, (im, ox) in enumerate(lets):
        x0, y0 = 960 + ox, 430
        th = 0.35 + (x0 + 700 - 120) / speed
        hit.append(th)
        if lt < th:
            slam(fr, im, x0, y0, lt, j * 0.04, 1.0, random.Random(j).uniform(-5, 5), dur=0.22)
        else:
            d = lt - th; r2 = random.Random(j)
            fr.put(im, x0 + r2.uniform(700, 1300) * d - cam_x, y0 - r2.uniform(900, 1500) * d + 2400 * d * d, 1 + d * 0.3, r2.uniform(300, 800) * d)
            burst(f'os{j}', x0, y0, 3.0 + th, [fx(2), fx(7)], n=5, speed=(500, 1000), life=0.4, scale=(0.2, 0.4), seed=j).draw(fr, t)
    rnd = random.Random(7)
    runs = K.anim_rows(f'{ART}/smash_run.png', 4, 3)              # Codex 달리기 4프레임 × 흰·갈색·회색 (타이틀 그림체, 사용자: 그림체 맞추기)
    for i in range(13):
        wx = lead - (i % 5) * 300 - rnd.uniform(0, 90); wy = 620 + (i // 5) * 190 + rnd.uniform(-30, 30)
        x = wx - cam_x
        if -350 < x < 2400:
            row = runs[(i * 7) % 3]
            im = row[int(lt * 15 + i * 1.7) % 4]                       # 초당 15프레임 달리기
            fr.put(im, x, wy, 430 / row[0].width, ax=0.55, ay=0.95)
    sh = shake(lt, min(hit), 0.5, 26)
    fr.cam = (sh[0], sh[1], 1.0, sh[2])


def o_game_smash(fr, t):     # 5.5–8.0 인게임 물건 날리기 ×5
    for i in range(5):
        t0 = 5.5 + i * 0.5
        if t0 <= t < t0 + 0.5:
            lt = t - t0
            # 컷마다 좌우 반전 · 줌 인/아웃 · 기울기 번갈아 (사용자: 리듬감)
            zs = [(1.1, 1.2), (1.26, 1.1), (1.1, 1.22), (1.28, 1.12), (1.1, 1.24)][i]
            clip(fr, f'smash_{i + 1}', lt, zs, 0.5, flip=i % 2 == 1)
            flash_in(fr, t, t0, 0.08)
            hit_word(fr, lt, i, 0.5)
            tilt = [-3, 3, -2, 4, 0][i] * out_cubic(seg(lt, 0, 0.2))
            sh = shake(lt, 0.03, 0.25, 16); fr.cam = (sh[0], sh[1], 1, sh[2] + tilt)


def o_front(fr, t):          # 8.0–9.5 쥐가 정면으로 달려옴 → 홍조 + 윙크
    lt = t - 8.0
    fr.bg(bgimg('bg_pink.png'), lerp(1.0, 1.15, lt / 1.5))
    flash_in(fr, t, 8.0)
    sh = gsheet('run_front.png', 2, 1)
    wink = lt >= 0.8
    im = sh[1] if wink else sh[0]
    bob = abs(math.sin(lt * 14)) * 30
    sc = lerp(0.42, 0.55, lt / 1.5) * (lerp(1.12, 1.0, out_back(seg(lt, 0.8, 0.25))) if wink else 1)
    fr.put(im, 960, 600 - bob, sc, math.sin(lt * 14) * 3)
    if wink:
        burst('fw', 1060, 380, 8.8, [fx(4), fx(8)], n=6, speed=(250, 500), life=0.6, scale=(0.08, 0.16), seed=4, aim=-40, spread=70).draw(fr, t)
    if t > 9.35: fr.flash(seg(t, 9.35, 0.15))


def o_jump(fr, t):           # 9.5–11.5 옆모습 점프 → 빠른 줌아웃 → 반대편에서 같은 쥐 → 쾅 + 하트 → SWARM!!! (글자 빼고 블러)
    lt = t - 9.5
    fr.bg(bgimg('bg_blue.png'), 1.1)
    flash_in(fr, t, 9.5)
    J = G('jump_side.png')
    meet = 0.75
    k = clamp(lt / meet)
    arc = math.sin(k * math.pi * 0.5) * 380
    xl = lerp(200, 860, k); xr = lerp(1720, 1060, k); y = 820 - arc
    if lt < meet:
        fr.put(J, xl, y, 0.36, lerp(15, -5, k))
        if lt > 0.25: fr.put(J, xr, y, 0.36, lerp(-15, 5, k), flip=True)
    else:
        d = lt - meet
        sq = lerp(0.8, 1.0, out_back(seg(d, 0, 0.25)))
        fr.put(J, 860 - 30 * sq, 440 + d * 40, 0.36, -5, sx=sq)
        fr.put(J, 1060 + 30 * sq, 440 + d * 40, 0.36, 5, sx=sq, flip=True)
        fr.put(fx(1), 960, 430, 0.55 * out_back(seg(d, 0, 0.2)), d * 30)
        burst('jh', 960, 430, 9.5 + meet, [fx(8)], n=14, speed=(700, 1500), life=0.9, scale=(0.12, 0.3), seed=6, grav=500).draw(fr, t)
    z = lerp(1.6, 1.0, out_cubic(seg(lt, 0.0, 0.6)))
    if lt >= meet + 0.05:
        blur_all(fr, 9)
        pop(fr, lw('swarm', (word(T['swarm'], 300, fill=(255, 140, 180), fill2=(235, 70, 120)),)), 960, 520, lt, meet + 0.05, 1.0, -4, dur=0.4)
    sh = shake(lt, meet, 0.4, 30)
    fr.cam = (sh[0] + (1 - seg(lt, 0, 0.6)) * 300, sh[1], z, sh[2])
    if t > 11.35: fr.flash(seg(t, 11.35, 0.15))


def o_couple(fr, t):         # 11.5–14.0 마주 본 커플 → 쿵 + 먼지 → 새끼 + 하트 → 줌아웃·돌리 아웃 (주변에 같은 커플 여럿)
    lt = t - 11.5
    fr.bg(bgimg('bg_pink.png'), 1.0)
    flash_in(fr, t, 11.5)
    L, R, baby = art('bump_L.png'), art('bump_R.png'), G('baby.png', 400, 400)
    z = lerp(1.0, 0.38, in_out(seg(lt, 1.3, 1.1)))

    def group(cx, cy, s, ph=0.0, main=False):
        ll = lt - ph
        k = out_cubic(seg(ll, 0.0, 0.5))
        gap = lerp(330, 120, k)
        fr.put(L, cx - gap * s, cy, 0.3 * s, -3); fr.put(R, cx + gap * s, cy, 0.3 * s, 3)
        if ll > 0.5:
            dust = seg(ll, 0.5, 0.55)
            if ll > 0.8:
                fr.put(baby, cx, cy + 120 * s, 0.25 * s * out_back(seg(ll, 0.8, 0.3)))
                if main: burst('cb', cx, cy - 100 * s, 11.5 + ph + 0.85, [fx(8)], n=7, speed=(200, 450), life=0.9, scale=(0.06, 0.12), seed=2, grav=-100).draw(fr, t)
                else: fr.put(fx(8), cx, cy - 160 * s, 0.07 * s * out_back(seg(ll, 0.85, 0.3)))
            if dust < 1:
                for q in range(4):
                    fr.put(fx(7), cx + (q - 1.5) * 90 * s, cy + 40 * s - dust * 40 * s, 0.45 * s * (0.8 + dust), q * 40, 1 - dust ** 2)
    if z < 0.99:
        rnd = random.Random(21)
        for gx in range(-2, 3):
            for gy in range(-2, 3):
                if gx == 0 and gy == 0: continue
                group(960 + gx * 760 * z + rnd.uniform(-40, 40) * z, 540 + gy * 520 * z + rnd.uniform(-30, 30) * z, z, rnd.uniform(-0.3, 0.3))
    group(960, 540, z, 0.0, True)
    sh = shake(lt, 0.5, 0.3, 16); fr.cam = (sh[0], sh[1], 1, sh[2] + 3 * in_out(seg(lt, 1.3, 1.1)))


# 번식 클립 (take_breed5 에서 자름): 클립 안 0.1초에 새끼 탄생, 그 화면 위치 (게임 하트가 안 보여서 하트는 여기서 합성)
BREED_AT = [(684, 481), (1305, 628), (822, 686), (1167, 423)]


# 게임 번식 하트 (FxManager.Hearts / UpdateHearts 와 같은 계산 · 같은 그림 · 씬 값 그대로)
HEART_ART = 'Unity_Making/NKK_Project/Assets/Art/FX_Heart'
HEART = dict(n=4, size=(26, 38), rise=(130, 210), spread=22, life=0.75, wobble=10, wspeed=9, z=55)
PPU = 1920 / 556.52          # 촬영 줌(2.3)에서 게임 1단위 = 화면 픽셀 · 바닥 y 는 × TILT
TILT = 0.85


def game_hearts(fr, seed, age0, bx, by, x0, y0, zoom):
    """bx,by = 탄생 지점(클립 화면 px), age0 = 탄생 뒤 시간. x0,y0,zoom = 클립을 화면에 놓은 자르기·줌"""
    if age0 < 0: return
    rnd = random.Random(700 + seed)
    sprites = [K.img(f'{HEART_ART}/heart_{n}.png') for n in ('double', 'light', 'pink', 'red')]
    H = HEART
    for i in range(H['n']):
        hx = rnd.uniform(-H['spread'], H['spread']); hy = rnd.uniform(-H['spread'], H['spread']) * 0.4
        hz0 = H['z'] + rnd.uniform(0, 14); vz0 = rnd.uniform(*H['rise']); size = rnd.uniform(*H['size'])
        life = H['life'] * rnd.uniform(0.8, 1.15) + i * 0.04; ph = rnd.uniform(0, 6.3); rot = rnd.uniform(-15, 15)
        spr = rnd.choice(sprites)
        age = age0
        if age >= life: continue
        k = age / life
        # vz' = -1.6 vz → z = z0 + vz0 (1 - e^{-1.6 t}) / 1.6
        hz = hz0 + vz0 * (1 - math.exp(-1.6 * age)) / 1.6
        pop_ = clamp(age / 0.12)
        sc = size * (1.35 * pop_ if pop_ < 1 else 1 + 0.35 * max(0, 1 - (age - 0.12) / 0.15)) * (1 - 0.3 * k)
        wx = hx + math.sin(age * H['wspeed'] + ph) * H['wobble'] * k
        cx = bx + wx * PPU; cy = by + hy * PPU * TILT - hz * PPU            # 클립 화면 px
        sx, sy = (cx - x0) * zoom, (cy - y0) * zoom
        a = 1 if k < 0.5 else clamp((1 - k) / 0.5)
        fr.put(spr, sx, sy, sc * PPU * zoom / spr.width, -(rot + math.sin(age * H['wspeed'] * 0.7 + ph) * 12), a)


def o_game_breed(fr, t):     # 14.0–16.0 인게임 번식 ×4
    for i in range(4):
        t0 = 14.0 + i * 0.5
        if t0 <= t < t0 + 0.5:
            lt = t - t0
            bx, by = BREED_AT[i]
            c = clips.get(f'breed_{i + 1}')
            if c is None: c = clips[f'breed_{i + 1}'] = K.Clip(f'{CLIPS}/breed_{i + 1}.mp4', fr.S, FPS)
            im = c.get(lt)
            z = lerp(1.0, 1.18, out_cubic(seg(lt, 0.05, 0.4)))           # 탄생 지점으로 살짝 줌인
            if im is None: fr.fill((70, 70, 78))
            else: fr.bg(im, z, bx / 1920, by / 1080)
            # 줌 후 화면 위치 (bg 의 자르기와 같은 계산)
            vw, vh = 1920 / z, 1080 / z
            x0, y0 = clamp(bx - vw / 2, 0, 1920 - vw), clamp(by - vh / 2, 0, 1080 - vh)
            sx, sy = (bx - x0) * z, (by - y0) * z
            flash_in(fr, t, t0, 0.08)
            game_hearts(fr, i, lt - 0.1, bx, by, x0, y0, z)
            pop(fr, lw('plus', (word(T['plus'], 120, fill=(255, 140, 180), fill2=(235, 70, 120)),)), sx + 120, sy - 170, lt, 0.12, 1.0, random.Random(60 + i).uniform(-12, 12))
    if t > 15.85: fr.flash(seg(t, 15.85, 0.15))


def img_letters(name, text, sc, gapx=12):
    """Gemini 글자 그림(글자별로 떨어짐) → [(그림, 가운데 x 오프셋)] (word 함수 대신). 그림이 없으면 폰트 글자"""
    p = f'{GEM}/{name}'
    if not os.path.exists(K.P(p)): return letters(text, 300, fill=(255, 90, 70), fill2=(200, 40, 40))
    ims = K.split_letters(p)
    ws = [im.width * sc for im in ims]
    total = sum(ws) + gapx * (len(ims) - 1)
    out, x = [], -total / 2
    for im, w in zip(ims, ws):
        r = im.resize((max(1, int(im.width * sc)), max(1, int(im.height * sc))), Image.LANCZOS)
        out.append((r, x + w / 2)); x += w + gapx
    return out


def split_words(im, min_gap=0.15):
    """한 줄 글자 그림 → 단어별 (글자 높이 × min_gap 보다 넓은 빈 세로줄에서 자름)"""
    import numpy as np
    col = (np.array(im)[..., 3] > 30).any(axis=0)
    gap = int(im.height * min_gap)
    out, st, empty = [], None, 0
    for x, v in enumerate(list(col) + [False] * (gap + 1)):
        if v:
            if st is None: st = x
            empty = 0
        elif st is not None:
            empty += 1
            if empty > gap:
                out.append(im.crop((st, 0, x - empty + 1, im.height))); st = None
    return [o.crop(o.getbbox()) for o in out]


# Gemini 줄 글자: (파일, 줄 번호, 그 줄에서 단어 번호 또는 None)
LINES = dict(swarm=('let_words1.png', 0, None), run=('let_words1.png', 1, None), play=('let_words1.png', 2, None),
             and_=('let_words2.png', 0, 0), upgrade=('let_words2.png', 0, 1), ults=('let_words2.png', 1, None),
             skills=('let_words2.png', 2, None), boss=('let_words3.png', 0, None), plus=('let_words3.png', 1, None))


def lw(key, like):
    """화면 글자 = Gemini 레터링 그림 (높이를 폰트 글자 like 와 같게). 그림이 아직 없으면 폰트 글자 그대로"""
    spec = HITS[like[1]] if key == 'hits' else LINES.get(key)
    font = like[0]
    if not spec: return font
    name, row, part = spec
    p = f'{GEM}/{name}'
    if not os.path.exists(K.P(p)): return font
    k = ('lw', name, row, part, font.height)
    if k not in K._cache:
        rows = K.split_axis(p, axis=0)
        if row >= len(rows): return font
        im = rows[row]
        if part is not None:
            ws = split_words(im)
            if part >= len(ws): return font
            im = ws[part]
        sc = min(font.height * 0.95 / im.height, font.width * 1.05 / im.width)   # 폰트 글자 자리 안에
        K._cache[k] = im.resize((max(1, int(im.width * sc)), max(1, int(im.height * sc))), Image.LANCZOS)
    return K._cache[k]


# 부딪힘 글자 (let_hits.png: 1줄 CRASH! BAM! POW! / 2줄 CRUNCH! KABOOM!)
HITS = [('let_hits.png', 0, 0), ('let_hits.png', 0, 1), ('let_hits.png', 0, 2), ('let_hits.png', 1, 0), ('let_hits.png', 1, 1)]


SHIFT = 2.0                  # 뒷부분(STEAL~)은 예전 시각 + 2초


def later2(fn):
    return lambda fr, t: fn(fr, t - SHIFT - 1.0)


def later(fn):
    return lambda fr, t: fn(fr, t - SHIFT)


VEO_CAT = 'UnityResources/Rats/PV/Sheets/v3/veo/catscene_veo.mp4'
VEO_IN, VEO_SPEED = 0.8, 1.53     # Gemini 동영상 0.8초부터 1.53배속 → 6초


def s_run_veo(fr, t):        # 25.5–31.5 고양이 장면 = Gemini 동영상 (사용자 스케치 구도)
    lt = t - 25.5
    c = clips.get('veo_cat')
    if c is None:
        c = clips['veo_cat'] = K.Clip(VEO_CAT, fr.S, FPS, VEO_IN, VEO_SPEED)
    im = c.get(lt)
    if im is None: fr.fill((70, 70, 78))
    else: fr.bg(im, 1.0)
    flash_in(fr, t, 25.5)
    run_t = 1.75                                    # 쥐들이 흩어지기 시작
    if lt >= run_t:
        slam(fr, lw('run', (word(T['run'], 220, fill=(255, 90, 70), fill2=(200, 40, 40)),)), 480 + math.sin(lt * 50) * 6, 300, lt, run_t, 1.0, 12)
    sh = shake(lt, run_t, 0.45, 24)
    fr.cam = (sh[0], sh[1], 1.0, sh[2])
    if t > 31.35: fr.flash(seg(t, 31.35, 0.15))


# ═════════ 고양이 장면 v5 = Codex 부품 애니메이션 (Gemini 동영상 안 될 때, 2026-10-14) ═════════
C8 = ART
C8_LEDGE = 420                      # cat8_bg 에서 카운터 윗선 (이 아래는 고양이 앞을 가림)
C8_T = dict(rise=1.4, startle=1.85, run=2.0, throw=4.65, catch=5.1, out=5.85)


def c8_rows(name, cols, rows):
    p = f'{C8}/{name}'
    if not os.path.exists(K.P(p)): return [[placeholder(f'{name}#{r}', 300, 300)] * cols for r in range(rows)]
    return K.anim_rows(p, cols, rows)


def c8_put(fr, im, x, y, h, flip=False, rot=0.0, alpha=1.0):
    """발 기준 (x,y) 에 키 h 로"""
    fr.put(im, x, y, h / im.height, rot, alpha, ax=0.5, ay=0.97, flip=flip)


def c8_bang(fr, x, y, lt, t0):
    if t0 <= lt < t0 + 0.45:
        k = seg(lt, t0, 0.12)
        fr.put(fx(11), x, y, 0.16 * out_back(k), 0, 1 - seg(lt, t0 + 0.3, 0.15))


def s_cat8(fr, t):           # 23.5–29.5 노는 쥐 8마리 → 고양이 → 7마리 사방으로 도망 → 뚱뚱한 흰 쥐 허둥지둥 배회 → 치즈 던지고 탈출 → 고양이 어리둥절
    lt = t - 23.5
    T = C8_T
    bgp = f'{C8}/cat8_bg.png'
    bg = K.img(bgp) if os.path.exists(K.P(bgp)) else placeholder('cat8_bg', 1920, 1080)
    z = lerp(1.0, 1.04, lt / 6) + 0.05 * out_back(seg(lt, T['rise'], 0.4))      # 고양이 나오면 살짝 당김
    cy = lerp(0.55, 0.42, in_out(seg(lt, T['rise'], 0.5)))                     # 그리고 위로 살짝 틸트
    fr.bg(bg, z, 0.5, cy)
    vw, vh = 1920 / z, 1080 / z
    x0 = clamp(960 - vw / 2, 0, 1920 - vw); y0 = clamp(cy * 1080 - vh / 2, 0, 1080 - vh)

    def S(x, y): return ((x - x0) * z, (y - y0) * z)
    flash_in(fr, t, 23.5)
    # ── 고양이 (카운터 뒤) ──
    cats = c8_rows('cat8_cat.png', 4, 1)[0]
    if lt >= T['rise']:
        k = out_back(seg(lt, T['rise'], 0.35), 1.6)
        frame = 0
        if T['rise'] + 0.35 <= lt < T['rise'] + 0.95: frame = 1                         # 포효
        if lt >= T['catch']: frame = 2                                                   # 치즈 받고 어리둥절
        elif int((lt - T['rise']) * 10) % 23 == 0 and lt > T['rise'] + 1: frame = 3      # 가끔 깜빡
        cat = cats[frame]
        cx, cyy = S(960, lerp(C8_LEDGE + 700, C8_LEDGE + 60, k))
        wob = math.sin(lt * 7) * 1.5 if frame != 2 else math.sin(lt * 3) * 3
        fr.put(cat, cx, cyy, 600 * z / cat.height, wob, ax=0.5, ay=1.0)
        strip = bg.crop((0, C8_LEDGE, bg.width, bg.height))                              # 카운터 앞을 다시 그려 고양이 아래 가림
        sx0, sy0 = S(0, C8_LEDGE)
        fr.put(strip, sx0, sy0, z, ax=0, ay=0)
    # ── 쥐 7마리 ──
    idle1 = c8_rows('cat8_idle1.png', 4, 4); idle2 = c8_rows('cat8_idle2.png', 4, 4)
    side = c8_rows('cat8_flee_side.png', 4, 3); front = c8_rows('cat8_flee_front.png', 4, 3)
    fat = c8_rows('cat8_fat.png', 4, 4)

    def f8(rate=8, off=0): return int(lt * rate + off) % 4
    # (이름, 시작 위치(발), 키, 놀 때 줄, 도망 방식, 도망 줄, 방향)
    R = [('ball', (250, 690), 255, idle1[0], 'side', side[0], -1),
         ('book', (430, 930), 300, idle1[1], 'front', front[0], -0.6),
         ('tag1', (760, 650), 245, idle1[2], 'side', side[1], -1),
         ('tag2', (540, 655), 255, idle1[3], 'side', side[0], -1),
         ('dance', (1360, 670), 275, idle2[0], 'side', side[2], 1),
         ('nose', (1720, 730), 300, idle2[1], 'front', front[1], 0.5),
         ('nap', (1450, 960), 225, idle2[2], 'front', front[2], 0.25)]
    draw = []
    for i, (nm, (x, y), h, irow, mode, frow, dirx) in enumerate(R):
        st = T['startle'] + i * 0.03
        go = T['run'] + i * 0.07
        tag = nm in ('tag1', 'tag2')
        if lt < st:                                               # 놀기
            im = irow[f8(2.5) % 2] if nm == 'nap' else irow[f8(7, i)]
            xx = x + (math.sin(lt * 1.3 + i) * 25 + 70 * lt if tag else 0)
            draw.append((y, im, xx, y, h, False, 0))
        elif lt < go:                                             # 깜짝 (폴짝)
            xx = x + (math.sin(st * 1.3 + i) * 25 + 70 * st if tag else 0)
            hop = math.sin(seg(lt, st, go - st) * math.pi) * 40
            im = irow[2 if lt < st + 0.08 else 3] if nm == 'nap' else irow[f8(7, i)]
            draw.append((y, im, xx, y - hop, h, False, math.sin(lt * 60) * 4))
            c8_bang(fr, *S(xx, y - h - 40), lt, st)
        else:                                                     # 도망
            d = lt - go
            xs = x + (math.sin(st * 1.3 + i) * 25 + 70 * st if tag else 0)
            im = frow[int(d * 16 + i) % 4]
            if mode == 'side':
                xx = xs + dirx * (900 * d + 1400 * d * d)
                draw.append((y, im, xx, y - abs(math.sin(d * 25)) * 12, h * 1.05, dirx > 0, 0))
            else:
                g = 1 + 2.6 * d * d + 1.2 * d                     # 카메라 쪽으로 커짐
                xx = xs + dirx * 1400 * d * d
                yy = y + 380 * d + 700 * d * d
                draw.append((yy, im, xx, yy - abs(math.sin(d * 22)) * 10 * g, h * g, False, 0))
    # ── 뚱뚱한 흰 쥐 (치즈) ──
    fx_, fy_, fh = 960, 880, 420
    ft = lt
    flip = False; X, Y = fx_, fy_; g = 1.0
    if ft < T['startle']:
        im = idle2[3][f8(6)]
    elif ft < 2.15:
        im = fat[0][0]; Y = fy_ - math.sin(seg(ft, T['startle'], 0.3) * math.pi) * 60
        c8_bang(fr, *S(fx_, fy_ - fh - 50), lt, T['startle'])
    elif ft < 2.35: im = fat[0][1]
    elif ft < 2.55: im = fat[0][2]
    elif ft < 3.05:
        k = in_out(seg(ft, 2.55, 0.5)); X = lerp(fx_, 520, k); im = fat[1][int(ft * 16) % 4]
    elif ft < 3.2: X = 520; im = fat[0][2]
    elif ft < 3.75:
        k = in_out(seg(ft, 3.2, 0.55)); X = lerp(520, 1380, k); Y = fy_ + math.sin(k * math.pi) * 40
        im = fat[1][int(ft * 16) % 4]; flip = True
    elif ft < 3.95: X = 1380; Y = fy_ - abs(math.sin(ft * 30)) * 20; im = fat[0][3]
    elif ft < T['throw']:
        a = seg(ft, 3.95, T['throw'] - 3.95) * math.pi * 2 * 1.15            # 고리 모양으로 한 바퀴 남짓
        X, Y = 1180 + 200 * math.cos(a), fy_ - 30 + 80 * math.sin(a)
        im = fat[1][int(ft * 18) % 4]; flip = -math.sin(a) > 0
    elif ft < T['throw'] + 0.5:
        k = seg(ft, T['throw'], 0.5); X, Y = 1050, fy_ - 10
        im = fat[2][min(3, int(k * 4))]
    else:
        d = ft - T['throw'] - 0.5
        g = 1 + 2.4 * d * d + 1.0 * d
        X, Y = 1050 + 120 * d, fy_ + 420 * d + 800 * d * d
        im = fat[3][int(d * 16) % 4]
    draw.append((Y, im, X, Y, fh * g, flip, math.sin(lt * 40) * 3 if T['startle'] < ft < T['throw'] else 0))
    if T['startle'] < ft < T['throw']:                                      # 땀방울
        for j in range(2):
            ph = (lt * 3 + j * 0.5) % 1
            sx, sy = S(X + (j * 2 - 1) * fh * 0.35, Y - fh * 0.85 - ph * 50)
            fr.put(fx(5), sx, sy, 0.08, (j * 2 - 1) * 20, 1 - ph)
    for (_, im, x, y, h, fl, rot) in sorted(draw, key=lambda d: d[0]):
        sx, sy = S(x, y)
        c8_put(fr, im, sx, sy, h * z, fl, rot)
    # 던진 치즈 → 고양이 손
    if T['throw'] + 0.38 <= lt < T['catch']:
        k = seg(lt, T['throw'] + 0.38, T['catch'] - T['throw'] - 0.38)
        a, b = S(1050, fy_ - fh * 1.25), S(960, C8_LEDGE - 150)
        ch = K.img(f'{C8}/cat8_fat.png', key=True, crop=(1960, 60, 2240, 250))   # 같은 시트의 치즈 조각
        fr.put(ch, lerp(a[0], b[0], k), lerp(a[1], b[1], k) - math.sin(k * math.pi) * 200 * z, 0.5 * z, lt * 500)
    if lt >= T['run']:
        slam(fr, lw('run', (word(T_RUN, 200, fill=(255, 90, 70), fill2=(200, 40, 40)),)), 1530 + math.sin(lt * 50) * 6, 190, lt, T['run'], 1.0, -8)
    sh = shake(lt, T['rise'] + 0.3, 0.5, 26)
    fr.cam = (sh[0], sh[1], 1.0, sh[2])
    if lt > T['out']: fr.flash(seg(lt, T['out'], 0.15))


# ═════════ v6 (사용자 2026-10-14): STEAL·정면 달리기·고양이 쥐 장면 빼고, 필살기 4개 × 1.5초, AND..... → 고양이 실루엣 ═════════
ULT6 = [(0.25, 1.0), (0.25, 1.0), (0.25, 0.75), (0.25, 0.75)]      # ult_1~4 안 점프컷 위치 (배너 → 첫 타격 → 클라이맥스)


def s_power6(fr, t):         # 15.5–22.5 스킬 트리 1초 + 필살기 4개 × 1.5초 (파라오 · 산타 · 대멸종 · 람쥐 썬더)
    if t < 16.5:
        lt = t - 15.5
        clip(fr, 'skill_1', lt, (1.02, 1.1), 1.0)
        flash_in(fr, t, 15.5, 0.12)
        slam(fr, lw('skills', (word(T['skills'], 170, fill=(120, 230, 120), fill2=(40, 170, 80)),)), 1320, 200, lt, 0.1, 1.0, 6)
        return
    k = min(3, int((t - 16.5) / 1.5))
    t0 = 16.5 + k * 1.5
    lt = t - t0
    cuts = (0.0,) + ULT6[k]
    ci = max(i for i, c in enumerate(cuts) if lt >= c)            # 지금 몇 번째 컷
    c0 = cuts[ci]; c1 = cuts[ci + 1] if ci + 1 < len(cuts) else 1.5
    z0, z1 = [(1.0, 1.04), (1.16, 1.02), (1.0, 1.14)][ci]           # 컷마다 줌 방향 바꿈 (펀치 인 · 아웃)
    c = clips.get(f'ult_{k + 1}')
    if c is None: c = clips[f'ult_{k + 1}'] = K.Clip(f'{CLIPS}/ult_{k + 1}.mp4', fr.S, FPS)
    im = c.get(lt)
    if im is None: fr.fill((70, 70, 78))
    else: fr.bg(im, lerp(z0, z1, out_cubic(seg(lt, c0, c1 - c0))))
    flash_in(fr, t, t0 + c0, 0.1 if ci else 0.14)
    if ci: sh = shake(lt, c0, 0.3, 20); fr.cam = (sh[0], sh[1], 1, sh[2])
    slam(fr, lw('ults', (word(T['ults'], 170, fill=K.YELLOW, fill2=(255, 120, 30)),)), 620, 960, t - 16.5, 0.05, 1.0, -5)


AND_ART = f'{ART}/let_and.png'
SIL_ART = f'{ART}/cat_silhouette.png'
_EYES = {}


def sil_eyes(im):
    """실루엣 그림에서 눈(밝은 노랑·연두) 자리 마스크 + 눈 가운데들"""
    if 'm' not in _EYES:
        import numpy as np
        from scipy import ndimage
        a = np.asarray(im.convert('RGB')).astype(int)
        r, g, b = a[..., 0], a[..., 1], a[..., 2]
        m = (g > 150) & (g >= r - 15) & (b < 110)                  # 노랑·연두 눈 (조명은 파랑이 더 높아 빠짐)
        h, w = m.shape; m[: int(h * 0.12)] = False; m[int(h * 0.6):] = False; m[:, : int(w * 0.25)] = False; m[:, int(w * 0.75):] = False
        lab, n = ndimage.label(m)
        sz = ndimage.sum(m, lab, range(1, n + 1))
        keep = [i + 1 for i in np.argsort(sz)[::-1][:2]]
        m = np.isin(lab, keep)
        cen = [tuple(reversed(ndimage.center_of_mass(lab == i))) for i in keep]
        IF = __import__('PIL.ImageFilter', fromlist=['x'])
        mk = Image.fromarray((m * 255).astype('uint8'), 'L').filter(IF.MaxFilter(9)).filter(IF.GaussianBlur(5)).point(lambda v: min(255, v * 2))
        _EYES['body'] = tuple(int(v) for v in np.median(a[int(h * 0.35):int(h * 0.55), int(w * 0.4):int(w * 0.6)].reshape(-1, 3), axis=0))
        _EYES['m'] = mk; _EYES['c'] = cen
    return _EYES['m'], _EYES['c']


def eye_glow(sil):
    """눈 마스크로 빛 번짐 그림 (1920×1080 RGB, 검정 = 빛 없음): 가까운 번짐 + 넓은 번짐 + 가로 빛줄기"""
    if 'g' not in _EYES:
        import numpy as np
        from PIL import ImageFilter
        m, _ = sil_eyes(sil)
        W, H = m.size
        core = np.asarray(m.filter(ImageFilter.GaussianBlur(4)), float) / 255
        near = np.asarray(m.filter(ImageFilter.GaussianBlur(22)), float) / 255
        far = np.asarray(m.filter(ImageFilter.GaussianBlur(70)), float) / 255
        sm = m.resize((W // 48, H), Image.BILINEAR).filter(ImageFilter.GaussianBlur(1)).resize((W, H), Image.BICUBIC).filter(ImageFilter.GaussianBlur(3))
        streak = np.asarray(sm, float) / 255
        a = np.clip(core * 1.2 + near * 1.3 + far * 1.5 + streak * 1.6, 0, 1)[..., None]
        white = np.clip(core * 0.8, 0, 1)[..., None]          # 가운데는 하얗게 타오름
        col = np.array([255, 245, 120]) * (1 - white) + np.array([255, 255, 240]) * white
        _EYES['g'] = Image.fromarray(np.clip(col * a, 0, 255).astype('uint8'), 'RGB')
    return _EYES['g']


def eyes_open(sil, noeye, k):
    """눈 없는 판 위에 원래 눈을, 눈 가운데 가로줄에서 위아래로 k 만큼 열어 보이게 (눈꺼풀)"""
    import numpy as np
    key = ('eyeopen', round(k, 2))
    if key in K._cache: return K._cache[key]
    m, cen = sil_eyes(sil)
    W, H = sil.size
    ys = np.arange(H)[:, None].astype(float)
    xs = np.arange(W)[None, :].astype(float)
    lid = np.zeros((H, W))
    for (ex, ey) in cen:
        half = 70 * max(k, 0)                                   # 눈 높이의 반쯤까지 열림
        d = np.abs(ys - ey)
        band = np.clip((half - d) / 6 + 0.5, 0, 1) * (np.abs(xs - ex) < 220)
        lid = np.maximum(lid, band)
    mask = Image.fromarray((np.clip(lid, 0, 1) * 255).astype('uint8'), 'L')
    out = Image.composite(sil.convert('RGBA'), noeye.convert('RGBA'), mask)
    K._cache[key] = out
    return out


def add_glow(fr, g, x0, y0, vw, vh, inten):
    from PIL import ImageChops
    if inten <= 0.01: return
    lay = g.crop((int(x0), int(y0), int(x0 + vw), int(y0 + vh))).resize((fr.W, fr.H), Image.BILINEAR)
    if inten < 0.999: lay = lay.point(lambda v: int(v * min(inten, 1.0)))
    rgb = ImageChops.add(fr.im.convert('RGB'), lay)
    fr.im = rgb.convert('RGBA')


def s_and(fr, t):            # 22.5–25.0 AND..... 떨리며 올라옴 → 터져 흩어짐 → 고양이 로우앵글 실루엣 → 눈 반짝 → (보스)
    lt = t - 22.5
    burst_t = 1.05
    if lt < burst_t + 0.2:
        fr.fill((22, 18, 34))
        fr.overlay((60, 30, 80), 0.25 * (0.5 + 0.5 * math.sin(lt * 9)))
        lets = img_letters('let_and.png', 'AND.....', 0.62, 14)
        rnd = random.Random(int(lt * 60))
        for j, (im, ox) in enumerate(lets):
            appear = 0.0 if j < 3 else 0.45 + (j - 3) * 0.1          # AND 먼저, 점은 하나씩 톡톡
            if lt < appear: continue
            if lt < burst_t:
                k = out_cubic(seg(lt, appear, 0.55 if j < 3 else 0.2))
                y = lerp(1250, 540, k)
                amp = 3 + 9 * seg(lt, 0.3, 0.75)                     # 갈수록 더 떨림
                fr.put(im, 960 + ox + rnd.uniform(-amp, amp), y + rnd.uniform(-amp, amp), 1.0 + 0.06 * seg(lt, 0.6, 0.45), rnd.uniform(-amp, amp) * 0.4)
            else:
                d = lt - burst_t; r2 = random.Random(j * 7 + 1)
                ang = math.atan2(-0.4 + r2.uniform(-0.6, 0.6), ox / 300 + r2.uniform(-0.3, 0.3))
                v = r2.uniform(1600, 2600)
                fr.put(im, 960 + ox + math.cos(ang) * v * d, 540 + math.sin(ang) * v * d + 1800 * d * d, 1 + d * 1.5, r2.uniform(-900, 900) * d, 1 - d / 0.2)
        if lt >= burst_t:
            burst('andb', 960, 540, 22.5 + burst_t, [fx(1), fx(2), fx(6), fx(7)], n=18, speed=(900, 2000), life=0.5, scale=(0.25, 0.6), seed=9).draw(fr, t)
            if lt < burst_t + 0.06: fr.flash(0.9)
        sh = shake(lt, burst_t, 0.4, 34) if lt >= burst_t else (0, 0, 0)
        fr.cam = (sh[0], sh[1], 1.0, sh[2])
        return
    # 실루엣: 눈 없는 판으로 다가옴 → 가까워지면 눈꺼풀 열리듯 원래 눈이 위아래로 열림 (사용자: 빛 번짐 말고 원래 눈)
    st = lt - (burst_t + 0.2)                                    # 0 ~ 1.25
    sil = K.img(SIL_ART) if os.path.exists(K.P(SIL_ART)) else placeholder('cat_silhouette', 1920, 1080)
    nop = f'{ART}/cat_silhouette_noeyes.png'
    noeye = K.img(nop) if os.path.exists(K.P(nop)) else sil
    z = lerp(1.0, 1.32, in_out(seg(st, 0, 1.25)))               # 점점 다가옴 (얼굴 쪽으로)
    cyy = lerp(0.5, 0.36, in_out(seg(st, 0, 1.25)))
    open_t = 0.62
    k = out_back(seg(st, open_t, 0.16), 1.4)                     # 눈 뜨기 0 → 1
    if k <= 0.001: im = noeye
    else: im = eyes_open(sil, noeye, k)
    fr.bg(im, z, 0.5, cyy)
    fr.overlay((0, 0, 0), 0.55 * (1 - seg(st, 0, 0.35)))
    if open_t <= st < open_t + 0.04: fr.flash(0.1)
    sh = shake(st, open_t, 0.3, 14); fr.cam = (sh[0], sh[1], 1.0, sh[2])
    if lt > 2.35: fr.flash(seg(lt, 2.35, 0.15))


def shifted(fn, d):
    return lambda fr, t: fn(fr, t - d)


SCENES = [(0.0, 3.0, o_title), (3.0, 5.5, o_smash), (5.5, 8.0, o_game_smash),
          (8.0, 10.5, shifted(s_swarm, -1.0)), (10.5, 12.5, shifted(o_game_breed, -3.5)),
          (12.5, 15.5, shifted(s_upgrade, -4.0)), (15.5, 22.5, s_power6), (22.5, 25.0, s_and),
          (25.0, 29.0, shifted(s_boss, -3.5)), (29.0, DUR, shifted(s_play, -3.5))]

# 효과음 (시각, 파일, 볼륨)
# 사용자 2026-10-14: 효과음은 제목(쥐 헤딩) · 근육 쥐 커질 때 · AND..... 뜰 때만 남김
SFX_LIST = [(0.15, 'rat_jump', 0.8), (0.27, 'rat_squeak_1', 1), (0.65, 'rat_jump', 0.8), (0.77, 'rat_squeak_2', 1), (1.15, 'rat_jump', 0.8), (1.27, 'rat_squeak_3', 1), (13.0, 'rat_promote', 0.9), (13.5, 'rat_promote', 0.9), (14.0, 'rat_promote', 0.9), (14.5, 'jgl_rank_up', 0.9), (22.6, 'cat_warn', 0.8), (23.55, 'wall_blast', 1.0)]


FPS = 60


def render_frame(t, S):
    fr = K.Frame(S)
    for t0, t1, fn in SCENES:
        if t0 <= t < t1:
            fn(fr, t)
            break
    return fr


def mix_audio(silent, out, t_from, t_to):
    # 소리: 음악 + 효과음
    dur = t_to - t_from
    inputs = ['-i', silent, '-ss', str(MUSIC_START + t_from), '-t', str(dur), '-i', K.P(MUSIC)]
    fl = ['[1:a]volume=0.85,afade=in:d=0.2[m]']
    mix = ['[m]']
    k = 2
    for (st, name, vol) in SFX_LIST:
        if t_from <= st < t_to:
            inputs += ['-i', K.P(f'{SFX}/{name}.wav')]
            ms = int((st - t_from) * 1000)
            fl.append(f'[{k}:a]volume={vol * 0.7},adelay={ms}|{ms}[s{k}]'); mix.append(f'[s{k}]'); k += 1
    fl.append(f"{''.join(mix)}amix=inputs={len(mix)}:normalize=0:duration=first,afade=out:st={max(0, dur - 0.6)}:d=0.6[a]")
    subprocess.run(['ffmpeg', '-v', 'error', '-y'] + inputs + ['-filter_complex', ';'.join(fl), '-map', '0:v', '-map', '[a]', '-c:v', 'copy', '-c:a', 'aac', '-b:a', '192k', out], check=True)


def main():
    global FPS
    args = sys.argv[1:]
    draft = '--draft' in args
    S, FPS = (0.5, 30) if draft else (1.0, 60)
    t_from = float(args[args.index('--from') + 1]) if '--from' in args else 0.0
    t_to = float(args[args.index('--to') + 1]) if '--to' in args else DUR
    os.makedirs(K.P('PV/out'), exist_ok=True); os.makedirs(K.P('PV/_local/out'), exist_ok=True)
    if '--remix' in args:                    # 영상은 그대로 두고 소리만 다시 (PV_v3.mp4 의 영상 + 음악·효과음)
        src = K.P('PV/out/PV_v3.mp4'); tmp = K.P('PV/_local/out/PV_v3_video.mp4')
        subprocess.run(['ffmpeg', '-v', 'error', '-y', '-i', src, '-an', '-c:v', 'copy', tmp], check=True)
        mix_audio(tmp, src, 0.0, DUR)
        os.remove(tmp); print('소리 다시:', src); return
    if '--still' in args:
        for v in args[args.index('--still') + 1:]:
            if v.startswith('--'): break
            fr = render_frame(float(v), S)
            Image.frombytes('RGB', (fr.W, fr.H), fr.finish()).save(K.P(f'PV/_local/out/still_{v}.png'))
            print('still', v)
        return
    W, H = int(1920 * S), int(1080 * S)
    final = not draft and (t_from, t_to) == (0.0, DUR)
    out = K.P('PV/out/PV_v3.mp4') if final else K.P('PV/_local/out/PV_v3' + ('_draft' if draft else '') + ('' if (t_from, t_to) == (0.0, DUR) else f'_{t_from:g}-{t_to:g}') + '.mp4')
    silent = K.P('PV/_local/out/PV_v3_noaudio.mp4')
    enc = subprocess.Popen(['ffmpeg', '-v', 'error', '-y', '-f', 'rawvideo', '-pix_fmt', 'rgb24', '-s', f'{W}x{H}', '-r', str(FPS), '-i', '-',
                            '-c:v', 'libx264', '-preset', 'veryfast' if draft else 'medium', '-crf', '20' if draft else '16', '-pix_fmt', 'yuv420p', silent],
                           stdin=subprocess.PIPE)
    n0, n1 = int(t_from * FPS), int(t_to * FPS)
    for n in range(n0, n1):
        t = n / FPS
        enc.stdin.write(render_frame(t, S).finish())
        if n % FPS == 0: print(f'{t:5.1f}s', end='\r', flush=True)
    enc.stdin.close(); enc.wait()
    for c in clips.values(): c.close()
    mix_audio(silent, out, t_from, t_to)
    os.remove(silent)
    print('\n완성:', out)


if __name__ == '__main__':
    main()
