"""쥐 고유 컷씬 씬 오브젝트 만들기 코드 생성 (Tools/gacha_intro_build.cs.txt 의 //DATA 를 채움) → 출력 .cs 를 Unity MCP execute_code 로 실행
사용: python Tools/gen_gacha_intro_scene.py <출력.cs> [code ...]   (없으면 그림이 있는 쥐 전부)
입자 · 효과음 · 돌풍은 아래 표. 그림 = Assets/Art/Rats/GachaIntro/<code>/<파일>_c1~3.png"""
import os, sys

ART = os.path.join(os.path.dirname(__file__), '..', 'Unity_Making', 'NKK_Project', 'Assets', 'Art', 'Rats', 'GachaIntro')
TPL = os.path.join(os.path.dirname(__file__), 'gacha_intro_build.cs.txt')

# code: 오브젝트 이름, 그림 접두어, 큰 입자 그림, 바람, 색 / 팔레트, 점 크기, 줄무늬, 흔들림, 큰 입자 비율, 컷2 효과음, 입자 수(컷1, 컷2), 돌풍(컷1, 컷2)
W = (1, 1, 1)
RATS = {
    'icequeen':    ('IceQueen', 'iq', 'Assets/Art/Rats/NewRats/snowflake.png', (-1100, -320), [(1, 1, 1, .95)], (5, 22), .0028, 140, .08, 'superjump_charge', (70, 30), (2, 3)),
    'zapham':      ('ZapHam', 'zapham', '', (300, 900), [(1, .92, .3, .95), (1, 1, .85, .95)], (4, 14), .004, 320, 0, 'laser', (40, 45), (2, 3)),
    'parkrat':     ('ParkRat', 'parkrat', '', (-150, -350), [(1, .55, .7, .95), (1, .9, .35, .95), (.4, .85, .95, .95), (.6, .9, .5, .95), (1, 1, 1, .95)], (10, 24), .0005, 260, 0, 'combo_word', (40, 50), (1, 1.5)),
    'emperor':     ('Emperor', 'emperor', '', (0, -450), [(1, .85, .35, .95), (1, .6, .25, .9), (1, 1, .9, .9)], (4, 14), .002, 90, 0, 'meteor', (35, 45), (1.5, 2.5)),
    'pharaoh':     ('Pharaoh', 'pharaoh', '', (-1300, -80), [(.95, .8, .5, .8)], (3, 14), .003, 60, 0, 'jgl_unlock', (70, 30), (2, 2)),
    'knight':      ('Knight', 'knight', '', (60, 140), [(1, .95, .7, .7)], (4, 14), .0006, 60, 0, 'boss_slam', (40, 20), (1, 2)),
    'sultan':      ('Sultan', 'sultan', '', (0, -350), [(1, .85, .35, .95), (1, 1, .9, .9), (1, .6, .75, .9)], (5, 16), .001, 120, 0, 'cheese', (30, 50), (1, 2)),
    'alien':       ('Alien', 'alien', '', (0, 500), [(.55, 1, .55, .85), (.7, 1, .9, .85)], (4, 14), .002, 40, 0, 'laser', (40, 40), (1.5, 2.5)),
    'streamrat':   ('StreamRat', 'streamrat', '', (0, 380), [(1, .45, .75, .9), (.4, .9, 1, .9), (.75, .5, 1, .9)], (6, 18), .0008, 150, 0, 'combo_word', (30, 50), (1, 3)),
    'ramjui':      ('Ramjui', 'ramjui', '', (-350, -2000), [(.75, .85, 1, .7)], (3, 9), .0035, 20, 0, 'wall_blast', (40, 70), (1, 3)),
    'jwerry':      ('Jwerry', 'jwerry', '', (-200, -260), [(.9, .15, .25, .9), (1, .5, .6, .9)], (8, 18), .0004, 220, 0, 'smash_wood', (10, 15), (1, 2)),
    'starchef':    ('StarChef', 'starchef', '', (40, 220), [(1, 1, 1, .45)], (10, 30), .0008, 100, 0, 'hit_wood', (35, 25), (1, 2)),
    'jjdetective': ('JJDetective', 'jjdetective', '', (-200, -1400), [(.7, .8, .95, .5)], (3, 8), .003, 20, 0, 'throw', (60, 20), (1, 3)),
    'fondue':      ('Fondue', 'fondue', '', (150, -600), [(1, .85, .3, .95), (1, .7, .2, .9)], (6, 18), .0015, 120, 0, 'spill', (20, 50), (1, 2.5)),
    'bomber':      ('Bomber', 'bomber', '', (120, 520), [(1, .97, .7, .95), (1, .85, .4, .95), (1, 1, .92, .95)], (5, 16), .0012, 120, 0, 'wall_blast', (40, 60), (1.5, 3.5)),
    'maid':        ('Maid', 'maid', '', (-250, -1500), [(.9, .92, .98, .95), (.95, .82, .45, .9)], (4, 12), .003, 30, 0, 'superjump_charge', (40, 40), (1, 2)),     # 메이드 쥐 (2026-10-20): 은빛 칼날 · 금빛 시계 알갱이 (초필살기 Style 색만 씀)
}


def v2(p): return f'new UnityEngine.Vector2({p[0]}f, {p[1]}f)'
def col(c): return f'new UnityEngine.Color({c[0]}f, {c[1]}f, {c[2]}f, {c[3]}f)'


def data(code):
    name, pre, flake, wind, cols, dot, streak, sway, fch, sfx2, n, g = RATS[code]
    path = lambda i: f'Assets/Art/Rats/GachaIntro/{code}/{pre}_c{i}.png'
    pal = 'null' if len(cols) == 1 else 'new[] { ' + ', '.join(col(c) for c in cols) + ' }'
    steps = [
        f'St("{path(1)}", 1.2f, 1.12f, 1.03f, {v2((0, -50))}, {v2((0, 20))}, 0f, "story_page", {n[0]}, {g[0]}f)',
        f'St("{path(2)}", 1.25f, 1.0f, 1.1f, {v2((-40, 0))}, {v2((30, 10))}, 12f, "{sfx2}", {n[1]}, {g[1]}f)',
        f'St("{path(3)}", 1.1f, 1.2f, 1.05f, {v2((0, -30))}, {v2((0, 0))}, 18f, "jgl_ult_cutin", 0, 1.2f)',
    ]
    return (f'Build("{name}", "{code}", "{flake}", {v2(wind)}, {col(cols[0])}, {pal}, {v2(dot)}, {v2((34, 70))}, {streak}f, {sway}f, {fch}f,\n'
            f'  new[] {{ {", ".join(steps)} }});')


def main():
    out = sys.argv[1]
    codes = sys.argv[2:] or [c for c in RATS if os.path.exists(os.path.join(ART, c, f'{RATS[c][1]}_c3.png'))]
    tpl = open(TPL, encoding='utf-8').read()
    open(out, 'w', encoding='utf-8').write(tpl.replace('//DATA', '\n'.join(data(c) for c in codes)))
    print(out, ' '.join(codes))


if __name__ == '__main__':
    main()
