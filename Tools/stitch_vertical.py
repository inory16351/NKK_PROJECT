"""세로로 긴 그림 이어 붙이기: 위 그림의 아래 띠와 다음 그림의 위 띠를 겹쳐 서서히 섞음 (이음새 숨김)
사용: python Tools/stitch_vertical.py <출력.png> <겹침px | auto> <그림1.png> <그림2.png> ...
  auto = 다음 그림의 위 띠가 위 그림 아래 어디와 가장 닮았는지 찾아 그만큼 겹침 (참고 그림으로 아래 띠를 이어 그렸을 때)
그림은 폭이 같아야 함 (다르면 첫 그림 폭에 맞춤). 생성된 그림을 잘라 붙이기만 함 (코드로 그리기 아님)."""
import sys
import numpy as np
from PIL import Image


def find_overlap(a, b, lo=120, hi=700, strip=150):
    """a 의 아래 k px 와 b 의 위 k px 가 같은 곳이 되는 k (밝기 차이 평균이 가장 작은 값)"""
    ga, gb = a.mean(axis=2), b.mean(axis=2)
    best = None
    for k in range(max(lo, strip), min(hi, len(ga), len(gb)) + 1):
        d = np.abs(ga[len(ga) - k:len(ga) - k + strip] - gb[:strip]).mean()
        if best is None or d < best[0]:
            best = (d, k)
    return best


def main():
    out, ov_arg, files = sys.argv[1], sys.argv[2], sys.argv[3:]
    ims = [Image.open(f).convert('RGB') for f in files]
    w = ims[0].width
    ims = [im if im.width == w else im.resize((w, round(im.height * w / im.width)), Image.LANCZOS) for im in ims]
    acc = np.asarray(ims[0]).astype(np.float32)
    for f, im in zip(files[1:], ims[1:]):
        nxt = np.asarray(im).astype(np.float32)
        if ov_arg == 'auto':
            d, ov = find_overlap(acc, nxt)
            print(f'{f}: 겹침 {ov}px (차이 {d:.1f})')
        else:
            ov = int(ov_arg)
        a = acc[-ov:]; b = nxt[:ov]
        t = np.linspace(0, 1, ov)[:, None, None]
        t = t * t * (3 - 2 * t)                       # 부드럽게
        blend = a * (1 - t) + b * t
        acc = np.concatenate([acc[:-ov], blend, nxt[ov:]], axis=0)
    Image.fromarray(np.clip(acc, 0, 255).astype(np.uint8)).save(out)
    print(out, w, acc.shape[0])


if __name__ == '__main__':
    main()
