"""초필살기 그림 Codex 일괄 실행 (gen_superult_prompts.py 다음). 동시 N 개, 의존: pano_a → b → c (앞 장 아래 400px = 참고), pano_?_alt ← pano_?.
있는 그림은 건너뜀, 실패(안전 필터 등)는 한 번 더. 결과 = UnityResources/Rats/SuperUlt/<code>/Sheets/*.png, 로그 log_*.txt
사용: python Tools/run_superult_codex.py <동시 수> <code ...>"""
import os, subprocess, sys, threading, time
from concurrent.futures import ThreadPoolExecutor
from PIL import Image

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
CODEX = os.path.join(ROOT, 'Tools', 'codex.sh')
lock = threading.Lock()


def log(*a):
    with lock: print(time.strftime('%H:%M:%S'), *a, flush=True)


def run(sheets, name, refs):
    out = os.path.join(sheets, name + '.png')
    for attempt in range(2):
        if os.path.exists(out): return True
        args = ['bash', CODEX, 'exec', '--skip-git-repo-check', '--ephemeral', '-s', 'workspace-write', '-C', sheets]
        for r in refs: args += ['-i', r]
        with open(os.path.join(sheets, f'prompt_{name}.txt'), encoding='utf-8') as fin, open(os.path.join(sheets, f'log_{name}.txt'), 'w', encoding='utf-8', errors='replace') as flog:
            subprocess.run(args, stdin=fin, stdout=flog, stderr=subprocess.STDOUT)
        if os.path.exists(out): return True
        log('  다시', os.path.basename(os.path.dirname(sheets)), name)
    log('  실패', os.path.basename(os.path.dirname(sheets)), name)
    return False


def bottom(sheets, src, dst, horiz=False):
    """앞 장의 이어 그릴 띠: 세로 = 아래 400px · 가로 = 오른쪽 400px"""
    im = Image.open(os.path.join(sheets, src))
    (im.crop((im.width - 400, 0, im.width, im.height)) if horiz else im.crop((0, im.height - 400, im.width, im.height))).save(os.path.join(sheets, dst))


def rat_chain(code, pool):
    """파노라마 줄 (순서대로) + 상태 바뀐 판"""
    sh = os.path.join(ROOT, 'UnityResources', 'Rats', 'SuperUlt', code, 'Sheets')
    alt = os.path.exists(os.path.join(sh, 'prompt_pano_a_alt.txt'))
    import json
    horiz = json.load(open(os.path.join(sh, 'story.json'), encoding='utf-8')).get('orient') == 'h'
    futs = []
    prev = None
    for k in 'abc':
        refs = ['ref_pano_style.png']
        if prev:
            bottom(sh, f'pano_{prev}.png', f'ref_prev_bottom_{k}.png', horiz); refs.append(f'ref_prev_bottom_{k}.png')
        if not run(sh, f'pano_{k}', refs): return
        log(code, f'pano_{k}')
        if alt: futs.append(pool.submit(lambda kk=k: run(sh, f'pano_{kk}_alt', [f'pano_{kk}.png']) and log(code, f'pano_{kk}_alt')))
        prev = k
    for f in futs: f.result()


def main():
    n, codes = int(sys.argv[1]), sys.argv[2:]
    with ThreadPoolExecutor(n) as pool, ThreadPoolExecutor(min(4, len(codes))) as chains:
        jobs = []
        for code in codes:
            sh = os.path.join(ROOT, 'UnityResources', 'Rats', 'SuperUlt', code, 'Sheets')
            for name in ('pose_intro', 'pose_act', 'pose_finish', 'face_close'):
                jobs.append(pool.submit(lambda s=sh, c=code, nm=name: run(s, nm, ['ref_zap_style.png', 'ref_char.png', 'ref_parts.png', 'ref_style.png']) and log(c, nm)))
            for name, refs in (('fx', ['ref_style.png']), ('props', ['ref_zap_props.png', 'ref_style.png'])):
                jobs.append(pool.submit(lambda s=sh, c=code, nm=name, rf=refs: run(s, nm, rf) and log(c, nm)))
            jobs.append(chains.submit(rat_chain, code, pool))
        for j in jobs: j.result()
    log('끝')


if __name__ == '__main__':
    main()
