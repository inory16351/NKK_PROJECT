"""보스 개편 리깅 고치기 시트 자르기 (2026-10-22 2차). 시트 = UnityResources/Rats/BossRework/Sheets/<시트>.png
  cr5  팝 캣: 1줄 = 조립 참고 그림 2장 · 2줄 = 목 달린 새 몸통 · 앞다리 · 뒷다리 · 꼬리 (머리 head/head2 는 그대로)
       → 목 자리를 몸통 그림에서 직접 잼 (예전엔 모든 고양이 중앙값이라 목이 떠 보였음)
  tt2  차력거인 (참고 = tt2_full.png 통 그림): 1줄 = 머리 · 화난 머리 · 몸통 / 2줄 = 윗팔 · 아랫팔(주먹) · 허벅지 · 정강이(발) · 안장 포탑
  hr4 hr5  사람 보스 몸통만 옆모습으로 다시 (예전 몸통 = 정면 + 소매·팔이 그려져 팔이 3~4개로 보였음): 줄마다 조립 참고 그림 · 새 몸통
  hr6  원시인 쥐(버티컬 마우스) 몸통 하나 (hr4 몸통에도 팔이 그려져 다시 뽑음)
  hr8  사육사 머리 3종 (목 가늘게, 머리 폭 115px 로 headScale) · hr9 원시인 쥐 먼 팔 빈손 (arm2)
  tt8  차력거인 파츠 다시 (관절 원 없음, 허리선 맞춤) → tt2·tt5 대신 (안장·포탑은 tt3)
  tt9  차력거인 상반신 다시 (어깨 관절 덩어리 없음, ChatGPT 웹 — 원본 Sheets/chatgpt/), 엉덩이는 tt8 것
  cx5  거미 고양이 용접 목 받침 (neck)
  cx6  상어 고양이 네 발 (참고 = cx6_full.png): 머리 · 입 벌린 머리 · 몸 / 앞다리 · 뒷다리
  cx4  70층 물고기 고양이 → 머라이언 패러디: 머리 · 입 벌린 머리 · 물줄기 / 몸 · 꼬리 · 지느러미 (참고 = cx4_full.png)
  tt5  차력거인 상반신 · 팬티 엉덩이 (참고 = tt4_full.png) + tt3 2줄 안장 · 포탑 고양이 · 사격 (1줄은 안 씀)
  hr7  사육사 팔 2개: 가까운 팔 arm · 먼 팔 arm2 (예전엔 두 팔이 같은 '손바닥 내민' 그림)
바뀌기 전 파츠는 UnityResources/Rats/BossRework/Parts/_old/<코드>/ 로 옮겨 둠.
그다음: python Tools/gen_sprite_pivots.py → 유니티 NKK/Reimport Art Sprites · Build Human/Cat Art Library · Build Boss Part Art
사용: python Tools/slice_boss_fix.py [시트...]  ·  head2 (고양이 보스 두 번째 머리 관절점)  ·  discs (이음새 관절 원 잘라 내기, DISC_CUT)  ·  necks (사람 보스 목 = 목 앞선 맞춤, 몸통·머리 바꾼 뒤 다시)  ·  limbs (차력거인 윗팔·허벅지를 팔꿈치·무릎에서 자름, LIMB_CUT)"""
import io, json, os, shutil, sys
import numpy as np
from PIL import Image
from scipy import ndimage
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from slice_rat_parts import key_magenta, crop, leg_pivot, tail_pivot
from slice_human_parts import band_x
from slice_expansion import copy_to, ART, RES

BR = os.path.join(RES, 'BossRework')
SHEETS = os.path.join(BR, 'Sheets')
OLD = os.path.join(BR, 'Parts', '_old')
LEG_SHOW = 0.6      # 몸통 아래로 보이는 다리 = 몸통(목 뺀) 높이 × 이 값 (slice_cat_parts BOSS_LEG_SHOW 와 같음)
HUMAN_ROWS = {'hr4': ['-', 'boss_trainer'], 'hr5': ['boss_operator', 'boss_deepdiver']}      # hr4 원시인 쥐 몸통은 팔이 또 그려져서 hr6 (몸통 하나) 으로
# 어깨 = 몸통에 남은 소매 덩어리 위 가운데 (팔이 덮게, 몸통 그림 좌상단 기준 0~1). 없으면 목 가운데
SHOULDER = {'boss_trainer': [0.5, 0.13], 'boss_operator': [0.5, 0.15], 'boss_deepdiver': [0.5, 0.16], 'boss_vmouse': [0.3, 0.14]}      # 가까운 팔 = 몸통 가운데 (등 쪽 끝에 붙이면 안쪽 팔처럼 보였음, 등 쪽 소매 덩어리는 먼 팔 어깨 자리)


def clean(im):
    """자홍 테두리 없애기 (slice_cat_parts 와 같음): 알파 2px 깎고 남은 가장자리의 자홍 기운 빼기"""
    a0 = np.array(im)
    a = ndimage.grey_erosion(a0[..., 3], size=(5, 5))
    edge = (a > 0) & (a < 250)
    rgb = a0[..., :3].astype(np.int32)
    fix = edge & ((np.minimum(rgb[..., 0], rgb[..., 2]) - rgb[..., 1]) > 20)
    for c in (0, 2): rgb[..., c] = np.where(fix, np.minimum(rgb[..., c], rgb[..., 1] + 20), rgb[..., c])
    return Image.fromarray(np.dstack([rgb.astype(np.uint8), a.astype(np.uint8)]))


def rows_of(sheet, counts, dilate=8):
    """줄마다 큰 덩어리 n 개를 x 순서로 (줄 경계 = 덩어리 가운데 높이)"""
    im = clean(key_magenta(Image.open(sheet)))
    arr = np.array(im); alpha = arr[..., 3] > 20
    lab, n = ndimage.label(ndimage.binary_dilation(alpha, iterations=dilate))
    H = im.height; ch = H / len(counts); out = []
    for r, cnt in enumerate(counts):
        comps = []
        for k, sl in enumerate(ndimage.find_objects(lab), 1):
            if not sl: continue
            m = (lab[sl] == k) & alpha[sl]
            ys, xs = np.nonzero(m)
            if len(xs) < 300: continue
            if r * ch <= ys.mean() + sl[0].start < (r + 1) * ch: comps.append((xs.mean() + sl[1].start, len(xs), k))
        comps = sorted(sorted(comps, key=lambda c: -c[1])[:cnt])
        if len(comps) < cnt: raise SystemExit(f'{os.path.basename(sheet)} {r + 1}줄: 덩어리 {len(comps)}개 ({cnt}개 필요)')
        row = []
        for _, _, k in comps:
            a = arr.copy(); a[..., 3] = np.where(lab == k, a[..., 3], 0); row.append(crop(Image.fromarray(a)))
        out.append(row)
    return out


def fit_h(im, h):
    """Codex 가 크게 그린 파츠를 기존 파츠 크기(높이 h 픽셀)에 맞춤"""
    k = h / im.height
    return im.resize((max(1, round(im.width * k)), h), Image.LANCZOS)


def backup(src_dir, code, names):
    d = os.path.join(OLD, code); os.makedirs(d, exist_ok=True)
    for n in names:
        f = os.path.join(src_dir, n + '.png')
        if os.path.exists(f) and not os.path.exists(os.path.join(d, n + '.png')): shutil.copy2(f, os.path.join(d, n + '.png'))


def col_span(im, fx):
    """그림 가로 fx(0~1) 세로줄의 불투명 위·아래 (픽셀)"""
    a = np.array(im.getchannel('A')) > 20
    x = min(im.width - 1, max(0, int(fx * im.width)))
    ys = np.nonzero(a[:, max(0, x - 2):x + 3].any(axis=1))[0]
    return int(ys.min()), int(ys.max())


def popcat(sheet):
    cid = 'boss_popcat'
    (ref1, ref2), (torso, front, back, tail) = rows_of(sheet, [2, 4])
    d = os.path.join(RES, 'Cats', 'Parts', cid)
    backup(d, cid, ['torso', 'front', 'back', 'tail'])
    imgs = {'torso': torso, 'front': front, 'back': back, 'tail': tail}
    for k, v in imgs.items(): v.save(os.path.join(d, k + '.png'))
    ref1.save(os.path.join(BR, 'Parts', '_ref_popcat.png'))
    head = Image.open(os.path.join(d, 'head.png'))
    T = torso; W, H = T.size
    a = np.array(T.getchannel('A')) > 20; ys, xs = np.nonzero(a)
    # 목 = 몸통 그림 맨 위 띠의 가운데 (머리가 단면을 덮게 조금 아래)
    band = ys < ys.min() + max(3, int(H * 0.05))
    neck = [round(float(xs[band].mean()) / W, 3), round((ys.min() + H * 0.07) / H, 3)]
    sx, hx = 0.27, 0.8                               # 앞다리 · 뒷다리 붙는 가로 위치
    st, sb = col_span(T, sx); ht, hb = col_span(T, hx)
    body_h = sb - st
    shoulder = [sx, round((st + sb) / 2 / H, 3)]; hip = [hx, round((ht + hb) / 2 / H, 3)]
    tt, tb = col_span(T, 0.95); tail_a = [0.95, round((tt + (tb - tt) * 0.25) / H, 3)]
    piv = {'head': json.load(io.open(os.path.join(RES, 'Cats', 'Parts', 'pivots.json'), encoding='utf-8'))[cid]['pivot']['head'],
           'tail': tail_pivot(tail), 'front': leg_pivot(front), 'back': leg_pivot(back)}
    ground = sb + body_h * LEG_SHOW
    f_len = front.height * (1 - piv['front'][1]); b_len = back.height * (1 - piv['back'][1])
    leg = {'front': round((ground - shoulder[1] * H) / f_len, 3), 'back': round((ground - hip[1] * H) / b_len, 3)}
    anchor = {'neck': neck, 'tail': tail_a, 'shoulder': shoulder, 'hip': hip}
    pj_path = os.path.join(RES, 'Cats', 'Parts', 'pivots.json'); pj = json.load(io.open(pj_path, encoding='utf-8'))
    size = {k: list(v.size) for k, v in imgs.items()}; size['head'] = list(head.size)
    pj[cid] = {'size': size, 'pivot': piv, 'anchor': anchor, 'legScale': leg,
               'pivotUnity': {k: [v[0], round(1 - v[1], 3)] for k, v in piv.items()},
               'anchorUnity': {k: [v[0], round(1 - v[1], 3)] for k, v in anchor.items()}}
    json.dump(pj, io.open(pj_path, 'w', encoding='utf-8'), ensure_ascii=False, indent=2)
    for f in os.listdir(d):
        if f.endswith('.png'): copy_to(os.path.join(d, f), os.path.join(ART, 'Cats', 'Parts', cid, f))
    print(' ', cid, 'anchor', anchor, 'legs', leg)


def cartgiant(sheet):
    code = 'boss_cartgiant'
    (head, head2, body), (uparm, forearm, thigh, shin, saddle) = rows_of(sheet, [3, 5])
    src = os.path.join(BR, 'Parts', code); dst = os.path.join(ART, 'BossParts', code)
    backup(src, code, ['body', 'butt', 'forearm', 'head', 'head2', 'shin', 'thigh', 'uparm'])
    for n in ('butt',):      # 새 몸통에 엉덩이가 붙어 있어 안 씀
        for f in (os.path.join(src, n + '.png'), os.path.join(dst, n + '.png'), os.path.join(dst, n + '.png.meta')):
            if os.path.exists(f): os.remove(f)
    for n, im in zip(['head', 'head2', 'body', 'uparm', 'forearm', 'thigh', 'shin', 'saddle'], [head, head2, body, uparm, forearm, thigh, shin, saddle]):
        f = os.path.join(src, n + '.png'); im.save(f); copy_to(f, os.path.join(dst, n + '.png'))
        print(' ', code, n, im.size)


def human_torsos(s, sheet):
    ids = HUMAN_ROWS[s]
    rows = rows_of(sheet, [2] * len(ids))
    for cid, (ref, torso) in zip(ids, rows):
        if cid != '-': human_save(cid, torso, ref)


def human_save(cid, torso, ref):
    pj_path = os.path.join(RES, 'Humans', 'Parts', 'pivots.json'); pj = json.load(io.open(pj_path, encoding='utf-8'))
    d = os.path.join(RES, 'Humans', 'Parts', cid)
    backup(d, cid, ['torso'])
    torso.save(os.path.join(d, 'torso.png'))
    if ref: ref.save(os.path.join(BR, 'Parts', f'_ref_{cid}.png'))
    nx = band_x(torso, True)
    m = pj[cid]
    m['anchor'] = {'neck': [nx, 0.03], 'shoulder': SHOULDER.get(cid) or [nx, 0.12], 'hip': [band_x(torso, False), 0.95]}
    m['anchorUnity'] = {k: [v[0], round(1 - v[1], 3)] for k, v in m['anchor'].items()}
    m['size']['torso'] = list(torso.size)
    copy_to(os.path.join(d, 'torso.png'), os.path.join(ART, 'Humans', 'Parts', cid, 'torso.png'))
    json.dump(pj, io.open(pj_path, 'w', encoding='utf-8'), ensure_ascii=False, indent=2)
    print(' ', cid, 'torso', torso.size, m['anchor'])


def vmouse_torso(sheet):
    (torso,), = rows_of(sheet, [1])
    human_save('boss_vmouse', fit_h(torso, 305), None)      # 305 = 예전 몸통 높이 (머리·다리와 같은 축척)


def trainer_arms(sheet):
    """hr7 사육사 팔: 가까운 팔(편히 내린 손) = arm · 먼 팔(반대쪽 손) = arm2 (HumanRig 가 먼 팔에 씀)"""
    cid = 'boss_trainer'
    (arm, arm2), = rows_of(sheet, [2])
    k = 290 / arm.height; arm = fit_h(arm, 290); arm2 = fit_h(arm2, round(arm2.height * k))      # 몸통(278) 과 같은 축척
    d = os.path.join(RES, 'Humans', 'Parts', cid)
    backup(d, cid, ['arm'])
    pj_path = os.path.join(RES, 'Humans', 'Parts', 'pivots.json'); pj = json.load(io.open(pj_path, encoding='utf-8'))
    m = pj[cid]
    for n, im in (('arm', arm), ('arm2', arm2)):
        im.save(os.path.join(d, n + '.png')); copy_to(os.path.join(d, n + '.png'), os.path.join(ART, 'Humans', 'Parts', cid, n + '.png'))
        m['pivot'][n] = [band_x(im, True), 0.04]; m['size'][n] = list(im.size)
    m['pivotUnity'] = {k: [v[0], round(1 - v[1], 3)] for k, v in m['pivot'].items()}
    json.dump(pj, io.open(pj_path, 'w', encoding='utf-8'), ensure_ascii=False, indent=2)
    print(' ', cid, 'arm', arm.size, 'arm2', arm2.size)


def human_parts(cid, names, imgs, pivots, head_w=None):
    """사람 파츠 몇 개만 바꿈 (pivots = 이름별 (위 띠 가운데? , y)). head_w = 머리가 화면에 보일 폭 (픽셀) → headScale"""
    d = os.path.join(RES, 'Humans', 'Parts', cid)
    backup(d, cid, names)
    pj_path = os.path.join(RES, 'Humans', 'Parts', 'pivots.json'); pj = json.load(io.open(pj_path, encoding='utf-8'))
    m = pj[cid]
    for n, im in zip(names, imgs):
        im.save(os.path.join(d, n + '.png')); copy_to(os.path.join(d, n + '.png'), os.path.join(ART, 'Humans', 'Parts', cid, n + '.png'))
        top, y = pivots[n]; m['pivot'][n] = [band_x(im, top), y]; m['size'][n] = list(im.size)
    if head_w: m['headScale'] = round(head_w / imgs[names.index('head')].width, 3)
    m['pivotUnity'] = {k: [v[0], round(1 - v[1], 3)] for k, v in m['pivot'].items()}
    json.dump(pj, io.open(pj_path, 'w', encoding='utf-8'), ensure_ascii=False, indent=2)
    print(' ', cid, {n: im.size for n, im in zip(names, imgs)}, 'headScale', m.get('headScale'))


def part_save(code, names, imgs, drop=()):
    """전용 리그 파츠 저장 (BossRework/Parts/<code> → Assets/Art/Rats/BossParts/<code>). drop = 더는 안 쓰는 예전 파츠 (지움, _old 에 보관)"""
    src = os.path.join(BR, 'Parts', code); dst = os.path.join(ART, 'BossParts', code)
    os.makedirs(src, exist_ok=True)
    backup(src, code, list(names) + list(drop))
    for n in drop:
        for f in (os.path.join(src, n + '.png'), os.path.join(dst, n + '.png'), os.path.join(dst, n + '.png.meta')):
            if os.path.exists(f): os.remove(f)
    for n, im in zip(names, imgs):
        f = os.path.join(src, n + '.png'); im.save(f); copy_to(f, os.path.join(dst, n + '.png'))
        print(' ', code, n, im.size)


def merlion(sheet):
    """cx4 70층 물고기 고양이 → 머라이언 패러디 (고양이 얼굴 + 갈기, 물고기 몸, 꼬리 말아 올림, 입에서 물줄기)"""
    (head, head2, water), (body, tail, fin) = rows_of(sheet, [3, 3])
    part_save('boss_fishcat', ['head', 'head2', 'water', 'body', 'tail', 'fin'], [head, head2, water, body, tail, fin])


def giant_split(sheet):
    """tt5 차력거인 상반신(body) · 팬티 엉덩이(butt) (참고 = tt4_full 통 그림에 팬티만 입힘) + tt3 2줄 안장 · 포탑 고양이 · 사격"""
    (body, butt), = rows_of(sheet, [2])
    body, butt = fit_h(body, round(body.height * 0.7)), fit_h(butt, round(butt.height * 0.7))      # tt5 는 통 그림보다 1.4배 크게 그려짐 → tt2 파츠(머리·팔다리)와 같은 축척
    names, imgs = ['body', 'butt'], [body, butt]
    t3 = os.path.join(SHEETS, 'tt3.png')
    if os.path.exists(t3):
        _, (saddle, gunner, gunner2) = rows_of(t3, [2, 3])
        # 사격 그림은 총구 불꽃만큼 왼쪽으로 넓음 → 보통 그림 왼쪽을 비워 폭을 맞춤 (같은 관절 비율에서 고양이 자리가 안 움직이게)
        pad = Image.new('RGBA', (gunner2.width, max(gunner.height, gunner2.height)), (0, 0, 0, 0))
        pad.paste(gunner, (gunner2.width - gunner.width, pad.height - gunner.height)); gunner = pad
        g2 = Image.new('RGBA', pad.size, (0, 0, 0, 0)); g2.paste(gunner2, (0, pad.height - gunner2.height)); gunner2 = g2
        names += ['saddle', 'gunner', 'gunner2']; imgs += [saddle, gunner, gunner2]
    part_save('boss_cartgiant', names, imgs)


def trainer_heads(sheet):
    """hr8 사육사 머리 3종: 목 단면이 몸통 목보다 넓어서 (가로 접합부가 길어 보임) 목이 가는 머리로. 보이는 머리 폭 = 예전과 같게 115px"""
    (head, angry, scared), = rows_of(sheet, [3])
    human_parts('boss_trainer', ['head', 'angry', 'scared'], [head, angry, scared], {k: (False, 0.97) for k in ('head', 'angry', 'scared')}, head_w=115)


def vmouse_arm2(sheet):
    """hr9 원시인 쥐 먼 팔 = 빈손 (예전엔 두 팔 다 몽둥이를 들었음)"""
    (arm2,), = rows_of(sheet, [1])
    human_parts('boss_vmouse', ['arm2'], [fit_h(arm2, 300)], {'arm2': (True, 0.04)})


def giant_feet():
    """차력거인 정강이+발(tt2 shin) 을 발목에서 잘라 정강이(shin) · 발(foot) 두 조각으로 (자르기만).
    통 그림 자세 = 정강이는 뒤로 눕고 발은 발끝으로 섬 → 한 조각이면 발이 세로로 서 버림. 원본은 shinfoot.png 로 둠"""
    code = 'boss_cartgiant'; src = os.path.join(BR, 'Parts', code)
    f = os.path.join(src, 'shinfoot.png')
    if not os.path.exists(f): shutil.copy2(os.path.join(src, 'shin.png'), f)      # (tt8 은 shinfoot.png 를 바로 저장)
    im = Image.open(f).convert('RGBA'); W, H = im.size
    a = np.array(im)[..., 3] > 20
    widths = [(np.ptp(np.nonzero(a[y])[0]) if a[y].any() else 9999, y) for y in range(int(H * 0.5), int(H * 0.85))]
    ank = min(widths)[1] / H                                          # 발목 = 0.5~0.85 에서 가장 가는 줄
    shin = crop(im.crop((0, 0, W, int(H * (ank + 0.08)))), 0)        # 발목 조금 아래까지 → 발 위를 덮음
    foot = im.crop((0, int(H * (ank - 0.08)), W, H))                 # 발목 위 조금부터 → 정강이 밑에 숨음
    print('  발목', round(ank, 3))
    part_save(code, ['shin', 'foot'], [shin, foot])


def spider_neck(sheet):
    """cx5 거미 고양이 용접 목 받침 (원판 + 파이프 + 철근 다리, 용접 자국): 머리와 몸통이 따로 놀아 보여서 사이에 끼움"""
    (neck,), = rows_of(sheet, [1])
    part_save('boss_spider', ['neck'], [neck])


def cat_head2_pivots():
    """고양이 보스 두 번째 머리(head2) 관절점 넣기: 예전엔 없어서(가운데) 머리가 바뀔 때마다 위치가 튀었음 (팝 캣은 늘 뻐끔 → 계속 튐).
    head2 = head 와 같은 방식 (아래 띠 가운데, 높이는 head 와 같게). 팝 캣은 머리 그림 아래 35% 가 넓은 흰 목이라 관절을 턱(0.62)으로 → 몸통 목 위에 겹침"""
    from slice_rat_parts import head_pivot
    pj_path = os.path.join(RES, 'Cats', 'Parts', 'pivots.json'); pj = json.load(io.open(pj_path, encoding='utf-8'))
    for cid, m in pj.items():
        f = os.path.join(RES, 'Cats', 'Parts', cid, 'head2.png')
        if not isinstance(m, dict) or 'pivot' not in m or not os.path.exists(f): continue
        if cid == 'boss_popcat': m['pivot']['head'][1] = 0.62
        hp = head_pivot(Image.open(f).convert('RGBA')); hp[1] = m['pivot']['head'][1]
        m['pivot']['head2'] = hp
        m['pivotUnity'] = {k: [v[0], round(1 - v[1], 3)] for k, v in m['pivot'].items()}
        print(' ', cid, 'head', m['pivot']['head'], 'head2', hp)
    json.dump(pj, io.open(pj_path, 'w', encoding='utf-8'), ensure_ascii=False, indent=2)


# 관절 원 가리기 (사용자: 겹침 순서로): 위에 그려지는 조각의 이음새 끝 원만 잘라 냄 → 아래 조각의 원은 위 조각 몸 뒤로 숨김 (리그에서 관절을 안쪽으로).
# (코드, 파츠, 남길 범위 = 좌상단 기준 0~1 (x0, y0, x1, y1)) · 원본 = <파츠>_full.png
DISC_CUT = [('boss_fishcat', 'body', (0, 0, 0.87, 1))]         # 오른쪽 끝 원 (0.87~0.99, 꼬리 이음새). 차력거인은 tt8 (원 없는 그림) 로 바뀌어 뺌


# 몸통 위에 겹치는 끝 원은 그 색만 키잉 (투명) → 밑의 같은 살색 몸통이 비침: (코드, 파츠, 원을 찾을 세로 범위 0~1)
DISC_KEY = []      # (예전: 차력거인 가까운 팔 어깨 원 → tt8 로 바뀌어 필요 없음)


def key_disc(im, v0, v1):
    """세로 v0~v1 안에서 몸 대표색보다 어두운 가장 큰 둥근 덩어리(관절 원)를 투명으로 (가장자리 2px 부드럽게)"""
    a = np.array(im).astype(int); H = a.shape[0]
    op = a[..., 3] > 200; lum = a[..., :3].sum(-1); med = np.median(a[..., :3][op], axis=0).sum()
    dark = op & (lum < med - 35); dark[:int(v0 * H)] = False; dark[int(v1 * H):] = False
    lab, n = ndimage.label(ndimage.binary_opening(dark, iterations=2))
    if n == 0: return im
    k = 1 + int(np.argmax(ndimage.sum(np.ones_like(lab), lab, range(1, n + 1))))
    m = ndimage.binary_dilation(lab == k, iterations=2).astype(float)
    m = ndimage.gaussian_filter(m, 1.0)
    out = a.copy(); out[..., 3] = (out[..., 3] * (1 - np.clip(m, 0, 1))).astype(int)
    return Image.fromarray(out.astype(np.uint8))


def cut_discs():
    keys = {(c, n): r for c, n, r in DISC_KEY}
    for code, name, (x0, y0, x1, y1) in DISC_CUT:
        src = os.path.join(BR, 'Parts', code); full = os.path.join(src, name + '_full.png')
        if not os.path.exists(full): shutil.copy2(os.path.join(src, name + '.png'), full)
        im = Image.open(full).convert('RGBA'); W, H = im.size
        if (code, name) in keys: im = key_disc(im, *keys[(code, name)])
        part_save(code, [name], [im.crop((round(x0 * W), round(y0 * H), round(x1 * W), round(y1 * H)))])


def sharkcat(sheet):
    """cx6 65층 상어 고양이 다시 (참고 = cx6_full.png 네 발로 선 통 그림): 머리 · 입 벌린 머리 · 몸 / 앞다리 · 뒷다리 (허벅지가 몸에서 이어지는 다리).
    예전 = 가는 양말 다리가 배 밑에 꽂힌 모양이라 어색 + 가슴지느러미 겹침 → fin 뺌"""
    (head, head2, body), (leg, leg2) = rows_of(sheet, [3, 2])
    part_save('boss_sharkcat', ['head', 'head2', 'body', 'leg', 'leg2'], [head, head2, body, leg, leg2], drop=['fin'])


NECK_FRONT = ['boss_vmouse', 'boss_trainer', 'boss_operator', 'boss_deepdiver']


def human_necks():
    """사람 보스 목 자리 = 머리 목 앞선(목젖 쪽)을 몸통 목 단면 앞선에 맞춤 (사용자: 머리가 실제 목보다 조금 뒤에 붙음).
    예전 = 머리 목 가운데 ↔ 몸통 맨 위 가운데 → 옆모습 목은 뒤로 비스듬해서 몸통 목 단면이 머리 앞으로 삐져나왔음"""
    pj_path = os.path.join(RES, 'Humans', 'Parts', 'pivots.json'); pj = json.load(io.open(pj_path, encoding='utf-8'))
    def band(im, top, frac=0.05):
        a = np.array(im.getchannel('A')) > 20; ys, xs = np.nonzero(a); n = max(3, int(im.height * frac))
        sel = ys < ys.min() + n if top else ys > ys.max() - n
        return xs[sel].min() / im.width
    for cid in NECK_FRONT:
        d = os.path.join(RES, 'Humans', 'Parts', cid); m = pj[cid]
        t = Image.open(os.path.join(d, 'torso.png')); h = Image.open(os.path.join(d, 'head.png'))
        x = round(band(t, True) + (m['pivot']['head'][0] - band(h, False)) * h.width * m.get('headScale', 1) / t.width, 3)
        m['anchor']['neck'][0] = x; m['anchorUnity']['neck'][0] = x
        print(' ', cid, 'neck x', x)
    json.dump(pj, io.open(pj_path, 'w', encoding='utf-8'), ensure_ascii=False, indent=2)


def giant_v2(sheet):
    """tt8 차력거인 파츠 다시 (관절 원 없음 · 상반신 허리 끝이 팬티 밑으로 · 엉덩이는 팬티 앞선에서 시작): 머리 · 화난 머리 · 상반신 · 팬티 엉덩이 / 윗팔 · 아랫팔 · 허벅지 · 정강이+발
    → 정강이+발은 giant_feet 로 발목에서 자름 (안장·포탑은 tt3 그대로)"""
    (head, head2, body, butt), (uparm, forearm, thigh, shinfoot) = rows_of(sheet, [4, 4])
    src = os.path.join(BR, 'Parts', 'boss_cartgiant')
    for f in ('uparm_full.png', 'thigh_full.png'):      # 예전 원 잘라 내기 원본 → _old 로
        if os.path.exists(os.path.join(src, f)): shutil.move(os.path.join(src, f), os.path.join(OLD, 'boss_cartgiant', f))
    part_save('boss_cartgiant', ['head', 'head2', 'body', 'butt', 'uparm', 'forearm', 'thigh', 'shinfoot'], [head, head2, body, butt, uparm, forearm, thigh, shinfoot])
    giant_feet()


def giant_torso_v3(sheet):
    """tt9 (ChatGPT, Codex 한도) 차력거인 상반신: 어깨 관절 덩어리 없이 통 그림 실루엣대로 (사용자: 몸통에 팔 관절이 남으면 이상).
    엉덩이는 tt8 것 (사용자: tt9 엉덩이 너무 이상함)"""
    (body, _), = rows_of(sheet, [2])
    body = fit_h(body, round(body.height * 0.68))      # ChatGPT 가 통 그림보다 크게 그림 → match_parts 로 찾은 축척
    (_, _, _, butt), _ = rows_of(os.path.join(SHEETS, 'tt8.png'), [4, 4])
    part_save('boss_cartgiant', ['body', 'butt'], [body, butt])


# tt8 윗팔·허벅지 그림은 팔꿈치·무릎까지 포함한 팔다리 전체 (가운데 잘록한 곳 = 관절) → 아랫팔·정강이와 겹치면 관절이 두 개로 보였음 (사용자 지적)
# → 잘록한 곳 조금 아래에서 잘라 어깨~팔꿈치 · 엉덩이~무릎 조각만. (파츠, 관절 높이, 남길 높이) · 원본 = <파츠>_tt8.png
LIMB_CUT = [('uparm', 0.62, 0.68)]      # 허벅지는 원래 길이가 통 그림 허벅지와 맞아서 안 자름


def giant_cut_limbs():
    src = os.path.join(BR, 'Parts', 'boss_cartgiant')
    ims = []
    for name, joint, keep in LIMB_CUT:
        f = os.path.join(src, name + '_tt8.png')
        if not os.path.exists(f): shutil.copy2(os.path.join(src, name + '.png'), f)
        im = Image.open(f).convert('RGBA'); ims.append(im.crop((0, 0, im.width, round(im.height * keep))))
    part_save('boss_cartgiant', [n for n, _, _ in LIMB_CUT], ims)


def run(s):
    sheet = os.path.join(SHEETS, s + '.png')
    if not os.path.exists(sheet): print('없음', s); return
    print(s)
    if s == 'cr5': popcat(sheet)
    elif s == 'tt2': cartgiant(sheet)
    elif s in HUMAN_ROWS: human_torsos(s, sheet)
    elif s == 'hr6': vmouse_torso(sheet)
    elif s == 'hr7': trainer_arms(sheet)
    elif s == 'cx4': merlion(sheet)
    elif s == 'hr8': trainer_heads(sheet)
    elif s == 'cx5': spider_neck(sheet)
    elif s == 'cx6': sharkcat(sheet)
    elif s == 'tt8': giant_v2(sheet)
    elif s == 'tt9': giant_torso_v3(sheet)
    elif s == 'hr9': vmouse_arm2(sheet)
    elif s == 'tt5': giant_split(sheet); giant_feet()


if __name__ == '__main__':
    if sys.argv[1:] == ['head2']: cat_head2_pivots(); sys.exit()
    if sys.argv[1:] == ['discs']: cut_discs(); sys.exit()
    if sys.argv[1:] == ['necks']: human_necks(); sys.exit()
    if sys.argv[1:] == ['limbs']: giant_cut_limbs(); sys.exit()
    for s in (sys.argv[1:] or ['cr5', 'tt2', 'hr4', 'hr5', 'hr6', 'hr7', 'cx4', 'tt5', 'hr8', 'hr9', 'cx5', 'cx6', 'tt8', 'tt9']): run(s)
