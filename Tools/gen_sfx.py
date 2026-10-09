"""효과음 합성: 웹 프로토(rat-uprising.html)의 WebAudio 합성 효과음(tone · noise)을 파이썬으로 옮겨 WAV 로 뽑음 + 새 효과음.
→ UnityResources/Audio/SFX/*.wav (44.1kHz · 16bit · 모노) + Assets/Audio/SFX/ 에 복사
목록 = 사운드 목록.md. 목소리 계열(사람 비명 등)은 합성으론 어색 → 나중에 녹음/생성 소리로 바꿀 것.
사용: python Tools/gen_sfx.py"""
import os, shutil, wave
import numpy as np
from scipy import signal

SR = 44100
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, 'UnityResources', 'Audio', 'SFX')
DST = os.path.join(ROOT, 'Unity_Making', 'NKK_Project', 'Assets', 'Audio', 'SFX')
rng = np.random.default_rng(7)


def osc(kind, f0, f1, dur, ramp='exp'):
    n = max(1, int(dur * SR)); t = np.arange(n) / SR
    if f1 is None or f1 == f0: f = np.full(n, float(f0))
    elif ramp == 'exp': f = f0 * (f1 / f0) ** (t / dur)
    else: f = f0 + (f1 - f0) * t / dur
    ph = 2 * np.pi * np.cumsum(f) / SR
    if kind == 'sine': return np.sin(ph)
    if kind == 'square': return np.sign(np.sin(ph)) * 0.7
    if kind == 'sawtooth': return 2 * ((ph / (2 * np.pi)) % 1) - 1
    if kind == 'triangle': return 2 * np.abs(2 * ((ph / (2 * np.pi)) % 1) - 1) - 1
    raise ValueError(kind)


def env_exp(n, vol, attack=0.005):
    """WebAudio 식: 0.0001 → vol (attack) → 0.0001 (끝까지 지수 감소)"""
    t = np.arange(n) / SR; a = max(1, int(attack * SR))
    e = np.empty(n)
    e[:a] = 0.0001 * (vol / 0.0001) ** (np.arange(a) / a)
    rest = n - a
    if rest > 0: e[a:] = vol * (0.0001 / vol) ** (np.arange(rest) / rest)
    return e


def tone(type='sine', f=440, f2=None, dur=0.15, vol=0.3, delay=0.0, attack=0.005, ramp='exp'):
    x = osc(type, f, f2, dur, ramp) * env_exp(int(dur * SR), vol, attack)
    return place(x, delay)


def noise(dur=0.2, vol=0.3, freq=1000, f2=None, q=1.0, type='bandpass', delay=0.0):
    n = int(dur * SR); x = rng.uniform(-1, 1, n)
    out = np.empty(n); block = 256; zi = None; prev = None
    for i in range(0, n, block):
        k = i / n; fc = freq * ((f2 / freq) ** k if f2 else 1)
        fc = min(max(fc, 30), SR / 2 - 200)
        if type == 'bandpass':
            bw = fc / max(q, 0.1); lo, hi = max(20, fc - bw / 2), min(SR / 2 - 100, fc + bw / 2)
            b, a = signal.butter(2, [lo, hi], btype='band', fs=SR)
        elif type == 'highpass': b, a = signal.butter(2, fc, btype='high', fs=SR)
        else: b, a = signal.butter(2, fc, btype='low', fs=SR)
        if zi is None or len(zi) != max(len(a), len(b)) - 1: zi = signal.lfilter_zi(b, a) * 0
        out[i:i + block], zi = signal.lfilter(b, a, x[i:i + block], zi=zi)
    e = vol * (0.0001 / vol) ** (np.arange(n) / n)
    return place(out * e * 1.6, delay)


def place(x, delay):
    return np.concatenate([np.zeros(int(delay * SR)), x])


def mix(*parts):
    n = max(len(p) for p in parts); y = np.zeros(n)
    for p in parts: y[:len(p)] += p
    return y


def r(a, b): return float(rng.uniform(a, b))


def semis(base, s): return base * 2 ** (s / 12)


def squeak(f=2400, dur=0.08, vol=0.18, delay=0.0, up=1.25):
    """쥐 '찍' — 짧게 위로 꺾이는 사인 + 빠른 떨림"""
    n = int(dur * SR); t = np.arange(n) / SR
    fr = f * (1 + (up - 1) * np.sin(np.pi * t / dur)) * (1 + 0.04 * np.sin(2 * np.pi * 38 * t))
    x = np.sin(2 * np.pi * np.cumsum(fr) / SR) * env_exp(n, vol, 0.004)
    return place(x, delay)


S = {}
# ── UI ──
S['ui_click'] = lambda: tone('sine', 700, 900, 0.05, 0.25)
S['ui_open'] = lambda: mix(noise(0.12, 0.12, 1200, 3200, 0.8), tone('triangle', 500, 800, 0.1, 0.12))
S['ui_close'] = lambda: mix(noise(0.1, 0.1, 2800, 900, 0.8), tone('triangle', 700, 420, 0.1, 0.12))
S['ui_buy'] = lambda: mix(*[tone('triangle', semis(784, s), None, 0.12, 0.22, i * 0.05) for i, s in enumerate([0, 7, 12])], noise(0.1, 0.1, 6000, None, 1, 'highpass', 0.12))
S['ui_deny'] = lambda: mix(tone('square', 200, 150, 0.15, 0.16), tone('square', 190, 140, 0.15, 0.12, 0.17))
S['ui_locked'] = lambda: mix(*[mix(tone('triangle', r(1300, 1600), None, 0.06, 0.12, i * 0.07), noise(0.05, 0.12, 2500, None, 2, 'bandpass', i * 0.07)) for i in range(3)])
S['ui_toast'] = lambda: mix(tone('sine', 1320, None, 0.18, 0.12), tone('sine', 1760, None, 0.22, 0.08, 0.06))
S['slot_select'] = lambda: mix(*[tone('sine', semis(1046, s), None, 0.15, 0.16, i * 0.07) for i, s in enumerate([0, 7])])
S['story_page'] = lambda: noise(0.25, 0.14, 1800, 5000, 0.7)
# ── 튜토리얼 ──
for i, (code, f) in enumerate([('labrat', 2600), ('nerd', 2100), ('brownrat', 1800), ('hero', 1500)]):
    S[f'tuto_blip_{code}'] = (lambda f=f: squeak(f, 0.045, 0.14, up=1.15))
S['tuto_next'] = lambda: tone('sine', 900, 1200, 0.05, 0.16)
S['tuto_highlight'] = lambda: mix(*[tone('sine', semis(1568, s), None, 0.12, 0.08, i * 0.04) for i, s in enumerate([0, 4, 7, 12])])
# ── 징글 ──
S['jgl_unlock'] = lambda: mix(*[tone('triangle', semis(784, s), None, 0.18, 0.18, i * 0.06) for i, s in enumerate([0, 4, 7, 12, 16])], noise(0.3, 0.08, 7000, None, 1, 'highpass', 0.3))
S['jgl_floor_clear'] = lambda: mix(*[tone('square', semis(523, s), None, 0.18, 0.12, i * 0.07) for i, s in enumerate([0, 4, 7, 12, 16, 19, 24])])
S['jgl_game_over'] = lambda: mix(*[tone('triangle', semis(440, s), None, 0.32, 0.22, i * 0.16) for i, s in enumerate([0, -3, -6, -10])])
S['jgl_rank_up'] = lambda: mix(*[tone('square', semis(523, s), None, 0.22, 0.1, i * 0.08) for i, s in enumerate([0, 4, 7, 12])],
                               tone('square', semis(523, 16), None, 0.6, 0.12, 0.36), tone('sine', semis(523, 24), None, 0.7, 0.1, 0.36), noise(0.5, 0.06, 8000, None, 1, 'highpass', 0.36))
S['jgl_achievement'] = lambda: mix(*[tone('sine', semis(1046, s), None, 0.2, 0.15, i * 0.08) for i, s in enumerate([0, 7, 12])])
S['jgl_boss_warn'] = lambda: mix(tone('sine', 90, 40, 0.8, 0.5), noise(0.8, 0.3, 500, 80, 0.7, 'lowpass'), tone('sawtooth', 330, 330, 0.25, 0.08, 0.1), tone('sawtooth', 311, 311, 0.35, 0.08, 0.38))
S['jgl_boss_down'] = lambda: mix(*[tone('square', semis(392, s), None, 0.25, 0.12, i * 0.12) for i, s in enumerate([0, 4, 7])], tone('square', semis(392, 12), None, 0.8, 0.14, 0.36), tone('sine', semis(392, 19), None, 0.8, 0.1, 0.36))
S['jgl_time_warn'] = lambda: mix(tone('square', 1200, None, 0.09, 0.1), tone('square', 1200, None, 0.09, 0.1, 0.16))
S['jgl_desperation'] = lambda: mix(*[tone('sawtooth', 600, 900, 0.22, 0.1, i * 0.24, ramp='lin') for i in range(3)], tone('sine', 80, 40, 0.7, 0.4))
S['jgl_ult_cutin'] = lambda: mix(noise(0.35, 0.3, 600, 5000, 0.8), tone('sine', 110, 28, 0.5, 0.5, 0.32), noise(0.5, 0.3, 900, 120, 0.7, 'lowpass', 0.32))
# ── 쥐 ──
for i in range(3): S[f'rat_squeak_{i + 1}'] = (lambda i=i: mix(squeak(r(2200, 3000), r(0.06, 0.1), 0.16), squeak(r(2400, 3200), 0.05, 0.1, r(0.08, 0.12))) if i == 2 else squeak(r(2200, 3000), r(0.07, 0.11), 0.18))
S['rat_gnaw'] = lambda: mix(*[noise(0.025, 0.14, r(2500, 4500), None, 1.5, 'bandpass', i * 0.045 + r(0, 0.01)) for i in range(5)])
S['rat_birth'] = lambda: mix(tone('sine', r(380, 460), 950, 0.09, 0.22), tone('sine', 1400, 2100, 0.12, 0.08, 0.06))
S['rat_rush_call'] = lambda: mix(*[squeak(r(1900, 3200), r(0.06, 0.1), 0.12, r(0, 0.08)) for _ in range(7)], noise(0.22, 0.18, 800, 3500, 1.2, 'bandpass', 0.05))
S['rat_dash'] = lambda: noise(0.22, 0.3, 800, 3500, 1.2)
S['rat_promote'] = lambda: mix(*[tone('triangle', semis(659, s), None, 0.14, 0.18, i * 0.06) for i, s in enumerate([0, 4, 7, 12, 16])], noise(0.25, 0.08, 7000, None, 1, 'highpass', 0.25))
S['rat_stun'] = lambda: tone('sine', 900, 300, 0.4, 0.2) * 1 + 0
S['rat_jump'] = lambda: tone('square', 260, 520, 0.1, 0.12)
S['rat_land'] = lambda: noise(0.07, 0.2, 300, None, 1)
S['rat_grade_birth'] = lambda: mix(*[tone('sine', semis(1046, s), None, 0.16, 0.12, i * 0.05) for i, s in enumerate([0, 4, 7, 11, 14])], noise(0.4, 0.06, 8000, None, 1, 'highpass', 0.15))
S['ult_ready'] = lambda: mix(*[tone('sine', semis(1046, s), None, 0.15, 0.18, i * 0.07) for i, s in enumerate([0, 7, 12])])
S['superjump_charge'] = lambda: mix(tone('sawtooth', 200, 1200, 1.0, 0.08, ramp='exp'), noise(1.0, 0.1, 400, 4000, 1))
S['superjump_slam'] = lambda: mix(tone('sine', 90, 22, 0.8, 0.6), noise(0.9, 0.45, 1500, 90, 0.6, 'lowpass'), noise(0.4, 0.2, 4000, None, 0.5, 'highpass'))
# ── 파괴 · 재화 ──
S['hit_glass'] = lambda: mix(tone('sine', 160, 45, 0.15, 0.3), tone('triangle', r(2400, 3200), None, 0.08, 0.1))
S['hit_wood'] = lambda: mix(tone('sine', 160, 45, 0.17, 0.32), noise(0.08, 0.16, 600, None, 0.8))
S['hit_metal'] = lambda: mix(tone('sine', 150, 45, 0.15, 0.25), tone('triangle', r(900, 1300), None, 0.2, 0.12), tone('triangle', r(1700, 2300), None, 0.15, 0.06))
S['smash_glass'] = lambda: mix(tone('sine', 140, 40, 0.18, 0.4), noise(0.3, 0.32, 4000, None, 0.5, 'highpass'), *[tone('triangle', r(2200, 5200), None, 0.09, 0.09, i * 0.025 + r(0, 0.02)) for i in range(6)])
S['smash_wood'] = lambda: mix(tone('sine', 140, 40, 0.2, 0.42), noise(0.18, 0.3, 400, None, 1.2), noise(0.12, 0.15, 1200, None, 1, 'bandpass', 0.03))
S['smash_metal'] = lambda: mix(tone('sine', 120, 35, 0.22, 0.4), noise(0.2, 0.22, 2500, None, 1), tone('triangle', r(700, 900), None, 0.35, 0.12), tone('triangle', r(1500, 1800), None, 0.3, 0.07, 0.02))
S['crit'] = lambda: mix(tone('sawtooth', 90, 30, 0.4, 0.35), noise(0.45, 0.4, 1800, 200, 0.6), tone('square', 1400, 700, 0.25, 0.08))
for n in range(2, 13):
    sm = [0, 2, 4, 7, 9, 12, 14, 16, 19, 21, 24][min(n - 2, 10)]
    S[f'combo_{n:02d}'] = (lambda sm=sm: mix(tone('square', semis(523, sm), None, 0.1, 0.1), tone('sine', semis(1046, sm), None, 0.14, 0.1, 0.02)))
S['combo_word'] = lambda: mix(*[tone('square', semis(659, s), None, 0.12, 0.1, i * 0.05) for i, s in enumerate([0, 4, 7, 12])])
S['cheese'] = lambda: mix(tone('triangle', r(1800, 2600), None, 0.12, 0.18), tone('triangle', r(2800, 3600), None, 0.08, 0.1, 0.02))
S['spill'] = lambda: mix(noise(0.45, 0.25, 3000, 600, 0.6, 'lowpass'), noise(0.3, 0.1, 1500, None, 2, 'bandpass', 0.05))
S['wall_crack'] = lambda: mix(*[noise(0.03, 0.2, r(1500, 3500), None, 2, 'bandpass', i * 0.03 + r(0, 0.015)) for i in range(6)], tone('sine', 120, 60, 0.15, 0.2))
S['wall_blast'] = lambda: mix(tone('sine', 110, 28, 0.6, 0.55), noise(0.7, 0.45, 900, 120, 0.7, 'lowpass'), *[noise(0.04, 0.12, r(1000, 3000), None, 2, 'bandpass', 0.15 + i * r(0.04, 0.08)) for i in range(6)])
S['stairs_open'] = lambda: mix(noise(0.35, 0.2, 300, None, 2), tone('triangle', 180, 120, 0.3, 0.14), *[tone('sine', semis(784, s), None, 0.15, 0.1, 0.25 + i * 0.06) for i, s in enumerate([0, 7, 12])])
S['delivery_drop'] = lambda: mix(tone('sine', 1500, 300, 0.6, 0.08, ramp='lin'), tone('sine', 110, 30, 0.45, 0.45, 0.6), noise(0.5, 0.3, 900, 120, 0.7, 'lowpass', 0.6))
S['trap_snap'] = lambda: mix(tone('square', 2200, 800, 0.03, 0.2), noise(0.08, 0.3, 3000, None, 1.5), tone('triangle', 1100, None, 0.2, 0.12, 0.01))
S['meteor'] = lambda: mix(tone('sine', 2200, 400, 0.7, 0.06, ramp='lin'), noise(0.7, 0.05, 2000, 600, 1), tone('sine', 100, 25, 0.6, 0.5, 0.7), noise(0.6, 0.35, 800, 100, 0.7, 'lowpass', 0.7))
S['laser'] = lambda: mix(tone('sawtooth', 1800, 200, 0.4, 0.12), tone('square', 900, 1200, 0.35, 0.06))
S['throw'] = lambda: tone('triangle', 300, 700, 0.15, 0.18)
# ── 사람 · 고양이 · 보스 ──
S['human_fly'] = lambda: mix(tone('sine', 900, 2400, 0.35, 0.08), noise(0.35, 0.08, 1200, 3500, 1))
S['guard_siren'] = lambda: mix(*[tone('sawtooth', 700 if i % 2 == 0 else 950, None, 0.3, 0.08, i * 0.3, attack=0.02) for i in range(6)])
def meow(p=1.0):
    n = int(0.42 * SR); t = np.arange(n) / SR
    f = np.interp(t, [0, 0.12, 0.38, 0.42], [520 * p, 820 * p, 480 * p, 470 * p])
    x = 2 * ((np.cumsum(f) / SR) % 1) - 1
    b, a = signal.butter(2, [700, 2400], btype='band', fs=SR)
    x = signal.lfilter(b, a, x) * env_exp(n, 0.5, 0.04) * 1.8
    return x
S['cat_meow_1'] = lambda: meow(1.0)
S['cat_meow_2'] = lambda: meow(0.8)
S['cat_hiss'] = lambda: noise(0.5, 0.35, 5000, 3000, 0.6, 'highpass')
S['cat_warn'] = lambda: mix(*[tone('square', 880, None, 0.08, 0.1, i * 0.12) for i in range(3)])
S['cat_pounce'] = lambda: mix(tone('sine', 140, 40, 0.25, 0.45), noise(0.2, 0.25, 700, None, 0.9), noise(0.15, 0.15, 3000, 800, 1, 'bandpass'))
S['cat_down'] = lambda: mix(meow(0.7), *[tone('triangle', semis(660, s), None, 0.2, 0.12, 0.35 + i * 0.1) for i, s in enumerate([0, -4, -7])])
S['boss_hit'] = lambda: mix(tone('sine', 110, 40, 0.2, 0.45), noise(0.12, 0.25, 700, None, 0.8))
S['boss_slam'] = lambda: mix(tone('sine', 80, 22, 0.7, 0.6), noise(0.7, 0.4, 700, 80, 0.7, 'lowpass'))
S['boss_swing'] = lambda: noise(0.25, 0.32, 3000, 500, 0.9)
S['boss_throw'] = lambda: mix(noise(0.2, 0.2, 2000, 800, 1), tone('triangle', 400, 900, 0.2, 0.12))


def write(name, x):
    x = np.asarray(x, dtype=np.float64)
    fade = min(len(x), int(0.004 * SR)); x[-fade:] *= np.linspace(1, 0, fade)
    peak = np.max(np.abs(x)) or 1
    x = x / peak * 0.8          # 모두 최대 0.8 로 맞춤 (실제 크기는 유니티 SfxManager 에서)
    pcm = (np.clip(x, -1, 1) * 32767).astype(np.int16)
    p = os.path.join(OUT, name + '.wav')
    with wave.open(p, 'wb') as w: w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR); w.writeframes(pcm.tobytes())
    shutil.copy(p, os.path.join(DST, name + '.wav'))
    return len(x) / SR


os.makedirs(OUT, exist_ok=True); os.makedirs(DST, exist_ok=True)
for name, fn in S.items():
    d = write(name, fn())
print(len(S), 'sounds ->', OUT)
