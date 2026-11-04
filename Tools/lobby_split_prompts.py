"""로비 배경 소품 분리 (2026-11-04): 배경마다 Codex 두 장
① empty = 소품(낮잠침대 · 회의 테이블 세트 · 책 더미 · 치즈 선반)만 지운 빈 배경
② props = 소품 4종만 그대로 두고 나머지 전부 자홍(#FF00FF) → key_magenta 로 키잉 → 소품별로 잘라 씀
사용: python Tools/lobby_split_prompts.py <작업 폴더> <원본 png>  → 폴더에 base.png · prompt_empty.txt · prompt_props.txt"""
import os, shutil, sys

PROPS = """the FOUR furniture groups standing on the floor deck:
  (1) BED = the nap bed at the far LEFT of the floor deck, under the round window, INCLUDING the small rug/mat under it and everything on it (pillow, blanket).
  (2) TABLE SET = the round table in the left-middle of the floor deck INCLUDING everything on top of it (pencil cup, papers, small items) AND the two small round stools/seats on its left and right.
  (3) BOOKS = the pile of books/binders/boxes in the middle-right of the floor deck INCLUDING the clip lamp attached to it, any small object on top of the pile, and the small flask/bottle standing right next to it.
  (4) CHEESE SHELF = the whole shelf unit at the RIGHT edge (its frame/boards, all the cheese on it, and the box/basket/case at its bottom)."""

EMPTY = f"""Use your built-in image generation tool to create ONE image and save it as PNG to: empty.png (in the current working directory). Do not write code to draw it; it must be a generated illustration. After saving, reply only with the file name.

THIS IS AN EDIT OF base.png (1536x1024). Keep base.png EXACTLY as it is - same canvas framing, same composition, same art style, same colors, every remaining element at exactly the same position and size - but REMOVE {PROPS}

Where those objects were, paint what is naturally behind them: the continuing back wall (with its stripe/border continuing at the same height), the vertical post(s) continuing down to the floor, and the plain empty top surface and front face of the floor deck. The floor deck top edge and front face must stay exactly where they are. Leave those areas EMPTY - do not add any new furniture, objects, characters or decorations there.

Everything else (top band, big beam, string lights, round window, board, photos, map, wall stripe, empty space for the hamster wheel, bottom band) must stay unchanged. No text, no letters, no characters, no animals."""

PROPS_P = f"""Use your built-in image generation tool to create ONE image and save it as PNG to: props.png (in the current working directory). Do not write code to draw it; it must be a generated illustration. After saving, reply only with the file name.

THIS IS AN EDIT OF base.png (1536x1024). Keep ONLY {PROPS}

Each of these four groups must stay EXACTLY as in base.png: same position on the canvas, same size, same outline, same colors and same art style (pixel-aligned with base.png).
Paint EVERYTHING ELSE as one solid, completely flat pure magenta color #FF00FF: the whole back wall, the floor deck, the beams, posts, top band, bottom band, string lights, window, board, photos, map - all of it becomes flat #FF00FF. No shadows, no floor, no outlines or halos around the kept objects, no gradient in the magenta. Do not add anything new. Do not use magenta or pink-purple colors inside the kept objects."""

if __name__ == '__main__':
    d, src = sys.argv[1], sys.argv[2]
    os.makedirs(d, exist_ok=True)
    shutil.copyfile(src, os.path.join(d, 'base.png'))
    open(os.path.join(d, 'prompt_empty.txt'), 'w', encoding='utf-8').write(EMPTY)
    open(os.path.join(d, 'prompt_props.txt'), 'w', encoding='utf-8').write(PROPS_P)
    print('ok', d)
