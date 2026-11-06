"""앙기뮤띠쥐 옮기기(자석 배치) 웹 미리보기: 파츠 PNG + 아지트 화면 캡처를 web_drag_template.html 에 넣어 한 파일로
→ C:/Project/NKK3/preview/hamster_drag.html
조립 · 관절 = build_web.py 와 같음. 배경 = ref_lobby_neon_clean.png (플레이 모드 아지트 캡처, 쥐 · 말풍선 숨김, 2배 크기)"""
import base64, io, json, os
from PIL import Image
here = os.path.dirname(os.path.abspath(__file__)); D = os.path.dirname(here)
OUT = 'C:/Project/NKK3/preview/hamster_drag.html'

def b64(im):
    buf = io.BytesIO(); im.save(buf, 'PNG'); return 'data:image/png;base64,' + base64.b64encode(buf.getvalue()).decode()

files = {'head': 'head_neutral', 'head_blink': 'head_neutral_blink', 'head_happy': 'head_happy',
         'body': 'body', 'arm': 'arm', 'foot': 'foot', 'tail': 'tail', 'thumb': 'arm_thumb'}
P = {k: Image.open(os.path.join(D, v + '.png')).convert('RGBA') for k, v in files.items()}
P['arm_l'] = P['arm'].transpose(Image.FLIP_LEFT_RIGHT)
PIV = {'head': (0.5, 0.96), 'head_blink': (0.5, 0.96), 'head_happy': (0.5, 0.96), 'body': (0.5, 0.5), 'tail': (0.5, 0.92),
       'arm': (0.76, 0.04), 'arm_l': (0.24, 0.04), 'foot': (0.5, 0.04), 'thumb': (0.03, 0.8)}
bw, bh = P['body'].size
headK = 0.92
footK, armK = 0.5, 0.55
NECK = (0.5, 0.33)
foot_bottom = (0.86 - 0.5) * bh + P['foot'].height * footK * 0.96
head_top = (NECK[1] - 0.5) * bh - P['head'].height * headK * 0.96
RIG = dict(neck=NECK, shL=(0.22, 0.24), shR=(0.78, 0.24), footL=(0.26, 0.86), footR=(0.74, 0.86), tail=(0.80, 0.86),
           headK=headK, armK=armK, footK=footK, armAngL=0, armAngR=0,
           thumbK=0.6, thumbAng0=-80, thumbAng1=0, thumbOff=(0, 0),
           height=foot_bottom - head_top, width=bw * 1.1, footToCenter=foot_bottom)
bg = Image.open(os.path.join(here, 'ref_lobby_neon_clean.png')).convert('RGB')
buf = io.BytesIO(); bg.save(buf, 'JPEG', quality=88); BG = 'data:image/jpeg;base64,' + base64.b64encode(buf.getvalue()).decode()
t = open(os.path.join(here, 'web_drag_template.html'), encoding='utf-8').read()
t = (t.replace('__IMGS__', json.dumps({k: b64(v) for k, v in P.items()})).replace('__PIV__', json.dumps(PIV))
      .replace('__RIG__', json.dumps(RIG)).replace('__BG__', BG).replace('__BW__', str(bg.width)).replace('__BH__', str(bg.height)))
os.makedirs(os.path.dirname(OUT), exist_ok=True)
open(OUT, 'w', encoding='utf-8').write(t); print(OUT, len(t), bg.size, round(RIG['height'], 1))
