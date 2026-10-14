# -*- coding: utf-8 -*-
"""itch.io 홍보 영상(PV) 만들기 — 컷 목록(JSON) → ffmpeg 로 mp4.

쓰는 법:  python Tools/pv/make_pv.py PV/cuts.json  [--draft]
  --draft = 960×540 · 30fps 로 빨리 미리보기

컷 목록 (PV/cuts.json):
  size [1920,1080] · fps 60 · music (mp3, 없으면 무음) · music_start (곡에서 몇 초부터) · music_fade (끝 페이드 초)
  font (자막 폰트 ttf) · out (출력 mp4)
  cuts: [ { name, src (그림·영상 경로, 없으면 회색 '촬영 전' 판), dur (초),
            in (영상 시작 초), speed (영상 배속),
            zoom [시작, 끝] (그림 천천히 확대, 기본 [1,1.06]),
            flash (true = 하얀 화면에서 시작, 게임 컷 전환과 같은 느낌),
            captions: [ { text, t (컷 안 시작 초), d (보이는 초), size, y (0~1 화면 높이 비율) } ],
            overlay: { src (투명 png, 예: 로고), t, w (화면 폭 비율), y } } ]
  captions: [ { text, t (영상 전체 기준 시작 초), d, size, y } ]  ← 컷을 넘어가는 자막은 여기
"""
import json, os, subprocess, sys, shutil, tempfile
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))


def run(args):
    r = subprocess.run(['ffmpeg', '-hide_banner', '-loglevel', 'error', '-y'] + args)
    if r.returncode != 0:
        sys.exit('ffmpeg 실패: ' + ' '.join(args))


def p(path):
    return path if os.path.isabs(path) else os.path.join(ROOT, path)


def text_png(path, text, font, size, W, H, y):
    """자막 한 줄 = 화면 크기 투명 png (흰 글자 + 두꺼운 갈색 테두리, 로고와 같은 색)."""
    im = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    f = ImageFont.truetype(font, size)
    sw = max(2, size // 9)
    box = d.textbbox((0, 0), text, font=f, stroke_width=sw)
    tw, th = box[2] - box[0], box[3] - box[1]
    x, yy = (W - tw) // 2 - box[0], int(H * y - th / 2) - box[1]
    # 그림자 → 테두리 글자
    d.text((x, yy + sw), text, font=f, fill=(40, 20, 10, 150), stroke_width=sw, stroke_fill=(40, 20, 10, 150))
    d.text((x, yy), text, font=f, fill=(255, 248, 225, 255), stroke_width=sw, stroke_fill=(74, 40, 18, 255))
    im.save(path)


def slate_png(path, name, font, W, H):
    im = Image.new('RGB', (W, H), (70, 70, 76))
    d = ImageDraw.Draw(im)
    f = ImageFont.truetype(font, H // 12)
    d.text((W // 2, H // 2), '촬영 전: ' + name, font=f, fill=(200, 200, 205), anchor='mm')
    im.save(path)


def render_cut(c, i, cfg, tmp, W, H, fps):
    dur = float(c['dur'])
    src = c.get('src')
    font = p(cfg['font'])
    if not src or not os.path.exists(p(src)):
        src = os.path.join(tmp, f'slate{i}.png')
        slate_png(src, c.get('name', str(i)), font, W, H)
    else:
        src = p(src)
    is_img = src.lower().endswith(('.png', '.jpg', '.jpeg', '.webp'))
    frames = int(round(dur * fps))

    inputs, chains = [], []
    if is_img:
        z0, z1 = c.get('zoom', [1.0, 1.06])
        # 크게 키운 뒤 zoompan (작은 그림에서 생기는 떨림 줄임)
        inputs += ['-loop', '1', '-t', f'{dur}', '-i', src]
        chains.append(
            f"[0:v]scale={W*2}:{H*2}:force_original_aspect_ratio=increase,crop={W*2}:{H*2},"
            f"zoompan=z='{z0}+({z1}-{z0})*on/{max(frames-1,1)}':x='iw/2-iw/zoom/2':y='ih/2-ih/zoom/2':d=1:s={W}x{H}:fps={fps},"
            f"trim=duration={dur},setpts=PTS-STARTPTS[base]")
    else:
        sp = float(c.get('speed', 1))
        inputs += ['-ss', f"{c.get('in', 0)}", '-t', f'{dur*sp}', '-i', src]
        chains.append(
            f"[0:v]setpts=(PTS-STARTPTS)/{sp},fps={fps},scale={W}:{H}:force_original_aspect_ratio=increase,crop={W}:{H},"
            f"trim=duration={dur},setpts=PTS-STARTPTS[base]")
    last = 'base'
    n = 1
    layers = []
    for k, cap in enumerate(c.get('captions', [])):
        png = os.path.join(tmp, f'cap{i}_{k}.png')
        text_png(png, cap['text'], font, int(cap.get('size', 120) * W / 1920), W, H, cap.get('y', 0.82))
        layers.append((png, float(cap.get('t', 0)), float(cap.get('d', dur - cap.get('t', 0)))))
    ov = c.get('overlay')
    if ov:
        lg = Image.open(p(ov['src']))
        w = int(W * ov.get('w', 0.7))
        lg = lg.resize((w, int(lg.height * w / lg.width)), Image.LANCZOS)
        canvas = Image.new('RGBA', (W, H), (0, 0, 0, 0))
        canvas.alpha_composite(lg, ((W - lg.width) // 2, int(H * ov.get('y', 0.45) - lg.height / 2)))
        png = os.path.join(tmp, f'ov{i}.png')
        canvas.save(png)
        layers.append((png, float(ov.get('t', 0)), dur - float(ov.get('t', 0))))
    for png, t, d in layers:
        inputs += ['-loop', '1', '-t', f'{dur}', '-i', png]
        fi = min(0.15, d / 3)
        chains.append(f"[{n}:v]format=rgba,fade=in:st={t}:d={fi}:alpha=1,fade=out:st={max(t, t+d-fi)}:d={fi}:alpha=1[l{n}]")
        chains.append(f"[{last}][l{n}]overlay=0:0:enable='between(t,{t},{t+d})'[o{n}]")
        last = f'o{n}'
        n += 1
    if c.get('flash'):
        chains.append(f"[{last}]fade=in:st=0:d=0.18:color=white[fl]")
        last = 'fl'
    # 필터에 따라 끝 프레임 몇 개가 빠질 때가 있음 → 마지막 프레임을 늘려 두고 -frames:v 로 정확히 자름
    chains.append(f"[{last}]tpad=stop_mode=clone:stop_duration=1,format=yuv420p[out]")
    out = os.path.join(tmp, f'seg{i:02d}.mp4')
    run(inputs + ['-filter_complex', ';'.join(chains), '-map', '[out]', '-r', str(fps), '-frames:v', str(frames),
                  '-c:v', 'libx264', '-preset', 'veryfast', '-crf', '16', out])
    return out


def caption_layers(caps, font, tmp, W, H, tag):
    """자막 → (png, 시작, 길이) 목록"""
    out = []
    for k, cap in enumerate(caps):
        png = os.path.join(tmp, f'{tag}{k}.png')
        text_png(png, cap['text'], font, int(cap.get('size', 120) * W / 1920), W, H, cap.get('y', 0.82))
        out.append((png, float(cap.get('t', 0)), float(cap['d'])))
    return out


def overlay_captions(video, caps, cfg, tmp, W, H, fps):
    layers = caption_layers(caps, p(cfg['font']), tmp, W, H, 'gcap')
    inputs, chains, last = ['-i', video], [], '0:v'
    for n, (png, t, d) in enumerate(layers, 1):
        fi = min(0.15, d / 3)
        inputs += ['-loop', '1', '-i', png]
        # 살짝 튀어 들어오기: 페이드 인/아웃
        chains.append(f"[{n}:v]format=rgba,fade=in:st={t}:d={fi}:alpha=1,fade=out:st={t+d-fi}:d={fi}:alpha=1[c{n}]")
        chains.append(f"[{last}][c{n}]overlay=0:0:shortest=1:enable='between(t,{t},{t+d})'[v{n}]")
        last = f'v{n}'
    chains.append(f"[{last}]format=yuv420p[out]")
    out = os.path.join(tmp, 'video_cap.mp4')
    run(inputs + ['-filter_complex', ';'.join(chains), '-map', '[out]', '-r', str(fps),
                  '-c:v', 'libx264', '-preset', 'veryfast', '-crf', '16', out])
    return out


def main():
    if len(sys.argv) < 2:
        sys.exit(__doc__)
    cfg = json.load(open(p(sys.argv[1]), encoding='utf-8'))
    draft = '--draft' in sys.argv
    W, H = cfg.get('size', [1920, 1080])
    fps = cfg.get('fps', 60)
    if draft:
        W, H, fps = W // 2, H // 2, 30
    tmp = tempfile.mkdtemp(prefix='nkk_pv_')
    try:
        segs = [render_cut(c, i, cfg, tmp, W, H, fps) for i, c in enumerate(cfg['cuts'])]
        lst = os.path.join(tmp, 'list.txt')
        with open(lst, 'w', encoding='utf-8') as f:
            f.writelines(f"file '{s.replace(os.sep, '/')}'\n" for s in segs)
        video = os.path.join(tmp, 'video.mp4')
        run(['-f', 'concat', '-safe', '0', '-i', lst, '-c', 'copy', video])
        total = sum(float(c['dur']) for c in cfg['cuts'])
        caps = cfg.get('captions', [])
        if caps:
            video = overlay_captions(video, caps, cfg, tmp, W, H, fps)
        out = p(cfg.get('out', 'PV/out/PV.mp4'))
        if draft:
            out = out.replace('.mp4', '_draft.mp4')
        os.makedirs(os.path.dirname(out), exist_ok=True)
        music = cfg.get('music')
        if music and os.path.exists(p(music)):
            fd = float(cfg.get('music_fade', 1.5))
            run(['-i', video, '-ss', str(cfg.get('music_start', 0)), '-i', p(music),
                 '-filter_complex', f"[1:a]atrim=duration={total},afade=in:d=0.25,afade=out:st={total-fd}:d={fd}[a]",
                 '-map', '0:v', '-map', '[a]', '-c:v', 'copy', '-c:a', 'aac', '-b:a', '192k', '-shortest', out])
        else:
            shutil.copy(video, out)
        print(f'완성: {out}  ({total:.1f}초, {W}x{H} {fps}fps)')
    finally:
        shutil.rmtree(tmp, ignore_errors=True)


if __name__ == '__main__':
    main()
