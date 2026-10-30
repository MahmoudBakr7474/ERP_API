# Extracts the Codlek logo variants from the approved brand sheet (codlek-logo-design.pdf) without redrawing them:
# the mark comes from the PDF's own filled paths, the wordmark from its own Type3 glyph outlines.
# Development tool only (PyMuPDF is AGPL and is never shipped):  python extract_logo.py codlek-logo-design.pdf <out-dir>
import pymupdf, re, sys
sys.stdout.reconfigure(encoding='utf-8')
src, outdir = sys.argv[1], sys.argv[2]
doc = pymupdf.open(src); p = doc[0]
# MuPDF's SVG of the logo area: only used to read the wordmark's glyph outlines and positions.
crop = pymupdf.open(src); crop[0].set_cropbox(pymupdf.Rect(286, 230, 484, 286))
full = crop[0].get_svg_image(text_as_path=True)

def hexc(c): return '#%02X%02X%02X' % tuple(round(v * 255) for v in c)
def fmt(v): return ('%.3f' % v).rstrip('0').rstrip('.')
def pathd(items, ox, oy):
    d = []; cur = None
    for it in items:
        k = it[0]
        if k == 're':
            r = it[1]; d.append(f"M{fmt(r.x0-ox)} {fmt(r.y0-oy)}H{fmt(r.x1-ox)}V{fmt(r.y1-oy)}H{fmt(r.x0-ox)}Z"); cur = None; continue
        if k == 'qu':
            q = it[1]; pts = [q.ul, q.ur, q.lr, q.ll]
            d.append('M' + ' L'.join(f"{fmt(pt.x-ox)} {fmt(pt.y-oy)}" for pt in pts) + 'Z'); cur = None; continue
        a = it[1]
        if cur is None or abs(cur.x - a.x) > 1e-3 or abs(cur.y - a.y) > 1e-3:
            d.append(f"M{fmt(a.x-ox)} {fmt(a.y-oy)}")
        if k == 'l':
            b = it[2]; d.append(f"L{fmt(b.x-ox)} {fmt(b.y-oy)}"); cur = b
        elif k == 'c':
            c1, c2, b = it[2], it[3], it[4]
            d.append(f"C{fmt(c1.x-ox)} {fmt(c1.y-oy)} {fmt(c2.x-ox)} {fmt(c2.y-oy)} {fmt(b.x-ox)} {fmt(b.y-oy)}"); cur = b
    return ''.join(d) + ('Z' if not d[-1].endswith('Z') else '')

def shapes(region):
    R = pymupdf.Rect(*region); out = []
    for dr in p.get_drawings():
        if dr.get('fill') is not None and R.contains(dr['rect']):
            out.append((dr, hexc(dr['fill'])))
    return out

# glyph defs used by the 40.5pt wordmark
uses = re.findall(r'<use data-text="(.)" xlink:href="#(font_9_\d+)" transform="matrix\(40\.5,0,0,-40\.5,([\d.]+),([\d.]+)\)"', full)
def glyph(gid):
    g = re.search(r'<g id="%s">(.*?)</g>' % gid, full, re.S).group(1)
    return [(tuple(float(v) for v in m.group(1).split(',')), m.group(2)) for m in re.finditer(r'<path transform="matrix\(([^)]+)\)" d="([^"]+)"', g)]
defs = {gid: glyph(gid) for _, gid, _, _ in uses}
def mul(A, B):
    a,b,c,d,e,f_ = A; a2,b2,c2,d2,e2,f2 = B
    return (a*a2+c*b2, b*a2+d*b2, a*c2+c*d2, b*c2+d*d2, a*e2+c*f2+e, b*e2+d*f2+f_)
print('wordmark', ''.join(u[0] for u in uses))

def write(name, w, h, body, title):
    svg = f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {fmt(w)} {fmt(h)}" role="img" aria-label="{title}"><title>{title}</title>{body}</svg>\n'
    open(f'{outdir}/{name}', 'w', encoding='utf-8', newline='\n').write(svg); print(name, len(svg))

NAVY, BLUE, WHITE = '#0B1B3F', '#0B63CE', '#FFFFFF'
# Logo region in page coordinates (mark 290..335 x 236..282, wordmark to ~479)
mark = shapes((288, 234, 337, 284))
print([(s[1], [round(v,2) for v in s[0]['rect']]) for s in mark])

def mark_body(ox, oy, navy):
    return ''.join(f'<path fill="{navy if col == NAVY else col}" d="{pathd(dr["items"], ox, oy)}"/>' for dr, col in mark)

def word_body(navy, dx, dy):
    # uses coordinates are relative to the crop origin (286,230); shift into our frame
    out = []
    for _, g, x, y in uses:
        for m, d in defs[g]:
            M = mul((40.5, 0, 0, -40.5, float(x) + dx, float(y) + dy), m)
            out.append(f'<path fill="{navy}" transform="matrix({",".join("%.6g" % v for v in M)})" d="{d}"/>')
    return ''.join(out)

# Tight bounds: mark x 290.0.. ; wordmark ink ends ~ 479; vertical 236..282
mx0 = min(dr['rect'].x0 for dr, _ in mark); my0 = min(dr['rect'].y0 for dr, _ in mark)
mx1 = max(dr['rect'].x1 for dr, _ in mark); my1 = max(dr['rect'].y1 for dr, _ in mark)
print('mark bounds', mx0, my0, mx1, my1)
pad = 0
wx1 = 479.5
W = wx1 - mx0; H = my1 - my0
for name, navy in [('codlek-logo.svg', NAVY), ('codlek-logo-reversed.svg', WHITE)]:
    write(name, W, H, mark_body(mx0, my0, navy) + word_body(navy, 286 - mx0, 230 - my0), 'Codlek')
write('codlek-mark.svg', mx1 - mx0, my1 - my0, mark_body(mx0, my0, NAVY), 'Codlek')
write('codlek-mark-reversed.svg', mx1 - mx0, my1 - my0, mark_body(mx0, my0, WHITE), 'Codlek')

# App tile (navy rounded square with white C) exactly as drawn in the source
tile = shapes((548, 419, 574, 445))
tx0 = min(dr['rect'].x0 for dr, _ in tile); ty0 = min(dr['rect'].y0 for dr, _ in tile)
tx1 = max(dr['rect'].x1 for dr, _ in tile); ty1 = max(dr['rect'].y1 for dr, _ in tile)
print('tile', [(c, [round(v,2) for v in d['rect']]) for d, c in tile])
write('codlek-app-icon.svg', tx1 - tx0, ty1 - ty0, ''.join(f'<path fill="{col}" d="{pathd(dr["items"], tx0, ty0)}"/>' for dr, col in tile), 'Codlek')
