"""자홍 키잉 뒤 가장자리 정리: 밝은 털(흰 쥐 등) 둘레에 남는 자홍빛 테두리를 없앰.
가장자리 띠(불투명 경계에서 BAND px 안쪽까지)의 색을 그 안쪽 깨끗한 픽셀 색으로 바꾸고, 자홍빛이 도는 옅은 픽셀은 지움.
사용: python Tools/clean_part_edges.py <png> [<png> ...]"""
import sys
import numpy as np
from PIL import Image
from scipy import ndimage
BAND = 3

for f in sys.argv[1:]:
    a = np.array(Image.open(f).convert('RGBA')).astype(np.int32)
    r, g, b, al = a[..., 0], a[..., 1], a[..., 2], a[..., 3]
    tint = (r > g + 40) & (b > g + 40)                       # 자홍빛 (코·발 분홍은 b 가 낮아 빠짐)
    core = ndimage.binary_erosion(al >= 250, iterations=BAND) & ~tint
    _, (iy, ix) = ndimage.distance_transform_edt(~core, return_indices=True)
    band = (al > 0) & ~core
    a[band, :3] = a[iy[band], ix[band], :3]
    a[(al < 160) & tint, 3] = 0                              # 자홍빛 옅은 부스러기
    a[..., 3] = np.where(ndimage.binary_opening(a[..., 3] > 0, iterations=1), a[..., 3], 0)
    Image.fromarray(a.astype(np.uint8)).save(f)
    print('clean', f, int(band.sum()))
