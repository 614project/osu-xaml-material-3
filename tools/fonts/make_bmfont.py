"""
Turns TrueType/WOFF2 fonts into the binary BMFont files (.bin + _N.png pages) osu!framework's GlyphStore reads.

Text fonts follow osu!framework's own Roboto: a line is 100 pixels from the font's hhea ascender to descender, the
baseline sits at the ascender, and kerning comes from the font's GPOS table (through HarfBuzz/raqm).
Icon fonts (Material Symbols) are cut as whole em squares, so an icon keeps the padding its 24dp grid gives it when
SpriteIcon fits the glyph to its size.

  python make_bmfont.py text  --name Roboto-Medium --out OUT  --chars-from REFERENCE.bin  FONT [FONT...]
  python make_bmfont.py icons --name MaterialSymbols-Outlined --out OUT --em 48 --axes FILL=0,wght=400,GRAD=0,opsz=24 --names-out names.tsv FONT

Needs fontTools, brotli and Pillow (with raqm for kerning).
"""

import argparse
import io
import math
import os
import struct
import sys

from fontTools.ttLib import TTFont
from PIL import Image, ImageDraw, ImageFont

PAGE_SIZE = 1024


def read_bmfont_chars(path):
    """The character ids in a binary BMFont file."""
    with open(path, 'rb') as f:
        data = f.read()

    assert data[:4] == b'BMF\x03', 'not a binary BMFont v3 file'
    i = 4
    chars = []

    while i < len(data):
        block, size = data[i], struct.unpack_from('<I', data, i + 1)[0]
        i += 5

        if block == 4:
            for c in range(size // 20):
                chars.append(struct.unpack_from('<I', data, i + c * 20)[0])

        i += size

    return chars


def to_ttf(path, axes=None):
    """Loads a font (decompressing WOFF2), pins variable axes, and returns it as TrueType bytes for FreeType."""
    font = TTFont(path)

    if axes and 'fvar' in font:
        from fontTools.varLib import instancer
        font = instancer.instantiateVariableFont(font, axes)

    font.flavor = None
    out = io.BytesIO()
    font.save(out)
    return font, out.getvalue()


class Packer:
    """Shelf packing into fixed-size pages."""

    def __init__(self, size, spacing):
        self.size = size
        self.spacing = spacing
        self.pages = []
        self._new_page()

    def _new_page(self):
        self.pages.append(Image.new('L', (self.size, self.size), 0))
        self.x = self.y = 0
        self.shelf = 0

    def add(self, glyph):
        w, h = glyph.size

        if self.x + w > self.size:
            self.x = 0
            self.y += self.shelf + self.spacing
            self.shelf = 0

        if self.y + h > self.size:
            self._new_page()

        x, y = self.x, self.y
        self.pages[-1].paste(glyph, (x, y))
        self.x += w + self.spacing
        self.shelf = max(self.shelf, h)
        return len(self.pages) - 1, x, y

    def trimmed_pages(self):
        """The pages, the last one cut down to the rows it uses (in powers of two, as textures like)."""
        pages = list(self.pages)
        used = self.y + self.shelf
        height = 64

        while height < used:
            height *= 2

        pages[-1] = pages[-1].crop((0, 0, self.size, min(self.size, height)))
        return pages


def save_page(alpha, path):
    """Saves a page as osu!framework's are: an indexed PNG whose palette is white at every alpha."""
    page = alpha.copy()
    page.putpalette([255, 255, 255] * 256)
    page.info['transparency'] = bytes(range(256))
    page.save(path, optimize=True, transparency=bytes(range(256)))


def write_bin(path, face, size, line_height, base, scale_w, scale_h, page_names, chars, kernings, spacing):
    def block(kind, payload):
        return struct.pack('<BI', kind, len(payload)) + payload

    # Smooth, unicode; 4x supersampling as osu!framework's own fonts declare.
    info = struct.pack('<hBBHBBBBBBBB', size, 0xC0, 0, 100, 4, 0, 0, 0, 0, spacing, spacing, 0) + face.encode() + b'\0'
    # Alpha holds the glyph, red/green/blue are one.
    common = struct.pack('<HHHHHBBBBB', line_height, base, scale_w, scale_h, len(page_names), 0, 0, 4, 4, 4)
    pages = b''.join(n.encode() + b'\0' for n in page_names)
    char_data = b''.join(struct.pack('<IHHHHhhhBB', *c) for c in chars)
    kerning_data = b''.join(struct.pack('<IIh', a, b, k) for a, b, k in kernings)

    with open(path, 'wb') as f:
        f.write(b'BMF\x03')
        f.write(block(1, info))
        f.write(block(2, common))
        f.write(block(3, pages))
        f.write(block(4, char_data))
        if kernings:
            f.write(block(5, kerning_data))


def make_text(args):
    line_height = 100
    spacing = 4
    sources = []

    for path in args.fonts:
        font, data = to_ttf(path)
        units = font['hhea'].ascent - font['hhea'].descent
        em_px = line_height * font['head'].unitsPerEm / units
        # Rounded up, as osu!framework's Roboto has it, so its weights share a baseline.
        base = math.ceil(line_height * font['hhea'].ascent / units)
        sources.append((font.getBestCmap(), ImageFont.truetype(io.BytesIO(data), size=em_px, layout_engine=ImageFont.Layout.RAQM), base))

    wanted = read_bmfont_chars(args.chars_from) if args.chars_from else sorted(set().union(*(s[0].keys() for s in sources)))
    base = sources[0][2]
    packer = Packer(PAGE_SIZE, spacing)
    chars = []
    owner = {}

    glyphs = []

    for cp in wanted:
        source = next((s for s in sources if cp in s[0]), None)
        if source is None:
            continue

        _, font, _ = source
        ch = chr(cp)
        advance = round(font.getlength(ch, features=['-kern', '-liga']))
        # The ink's bounds: drawn on a roomy canvas around a known origin, then cut to the pixels it covers.
        origin = (line_height, line_height * 2)
        canvas = Image.new('L', (line_height * 3, line_height * 3), 0)
        ImageDraw.Draw(canvas).text(origin, ch, font=font, fill=255, anchor='ls', features=['-kern', '-liga'])
        ink = canvas.getbbox()

        if ink is None:
            # Spaces: an empty pixel that only advances.
            glyph = Image.new('L', (1, 1), 0)
            left, top = 0, line_height - base - 1
        else:
            glyph = canvas.crop(ink)
            left, top = ink[0] - origin[0], ink[1] - origin[1]

        glyphs.append((cp, glyph, left, top, advance))
        owner[cp] = source

    # Tallest first packs the shelves tightly.
    for cp, glyph, left, top, advance in sorted(glyphs, key=lambda g: -g[1].height):
        page, x, y = packer.add(glyph)
        chars.append((cp, x, y, glyph.width, glyph.height, left, base + top, advance, page, 15))

    chars.sort()

    kernings = []
    by_source = {}
    for cp in owner:
        by_source.setdefault(id(owner[cp]), []).append(cp)

    for cps in by_source.values():
        font = owner[cps[0]][1]
        lengths = {cp: font.getlength(chr(cp), features=['-liga']) for cp in cps}

        for a in cps:
            if not chr(a).isprintable() or chr(a).isspace():
                continue

            for b in cps:
                if chr(b).isspace():
                    continue

                k = round(font.getlength(chr(a) + chr(b), features=['-liga']) - lengths[a] - lengths[b])
                if k != 0:
                    kernings.append((a, b, k))

    write_output(args, line_height, line_height, base, packer, chars, kernings, spacing, face=args.face or args.name.split('-')[0])


def make_icons(args):
    axes = dict((k, float(v)) for k, v in (a.split('=') for a in args.axes.split(','))) if args.axes else None
    font, data = to_ttf(args.fonts[0], axes)
    em = args.em
    image_font = ImageFont.truetype(io.BytesIO(data), size=em)
    spacing = 2

    # One codepoint per glyph: the icon's own private-use codepoint (the lowest, when it has aliases).
    by_glyph = {}
    for cp, glyph in font.getBestCmap().items():
        if cp >= 0xE000 and (glyph not in by_glyph or cp < by_glyph[glyph]):
            by_glyph[glyph] = cp

    names = sorted(by_glyph)
    if args.names:
        with open(args.names) as f:
            keep = {line.split()[0] for line in f if line.strip() and not line.startswith('#')}
        missing = keep - set(names)
        if missing:
            print('not in the font:', ' '.join(sorted(missing)), file=sys.stderr)
        names = [n for n in names if n in keep]

    packer = Packer(args.page_size, spacing)
    chars = []

    for name in names:
        cp = by_glyph[name]
        glyph = Image.new('L', (em, em), 0)
        # The icon's grid is the em square from the baseline up.
        ImageDraw.Draw(glyph).text((0, em), chr(cp), font=image_font, fill=255, anchor='ls')
        page, x, y = packer.add(glyph)
        chars.append((cp, x, y, em, em, 0, 0, em, page, 15))

    write_output(args, em, em, em, packer, chars, [], spacing, face=args.face or args.name)

    if args.names_out:
        with open(args.names_out, 'w') as f:
            for name in names:
                f.write(f'{name}\t{by_glyph[name]:04x}\n')


def write_output(args, size, line_height, base, packer, chars, kernings, spacing, face):
    os.makedirs(args.out, exist_ok=True)
    pages = packer.trimmed_pages()
    digits = len(str(len(pages) - 1))
    names = []

    for i, page in enumerate(pages):
        name = f'{args.name}_{str(i).zfill(digits)}.png'
        save_page(page, os.path.join(args.out, name))
        names.append(name)

    write_bin(os.path.join(args.out, args.name + '.bin'), face, size, line_height, base, packer.size, packer.size, names, chars, kernings, spacing)
    print(f'{args.name}: {len(chars)} glyphs, {len(kernings)} kerning pairs, {len(pages)} page(s)')


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument('mode', choices=['text', 'icons'])
    parser.add_argument('fonts', nargs='+')
    parser.add_argument('--name', required=True, help='the font name osu!framework looks it up by, e.g. Roboto-Medium')
    parser.add_argument('--face', help='the face name written into the file')
    parser.add_argument('--out', required=True)
    parser.add_argument('--chars-from', help='text: take the character set from this BMFont .bin')
    parser.add_argument('--em', type=int, default=48, help='icons: pixels per em square')
    parser.add_argument('--axes', help='icons: variable font axis values, e.g. FILL=1,wght=400')
    parser.add_argument('--names', help='icons: only the icons listed in this file (one name per line)')
    parser.add_argument('--names-out', help='icons: write name<TAB>codepoint for every icon cut')
    parser.add_argument('--page-size', type=int, default=2048)
    args = parser.parse_args()

    make_text(args) if args.mode == 'text' else make_icons(args)


if __name__ == '__main__':
    main()
