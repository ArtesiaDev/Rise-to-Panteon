import math
import random
import sys
import time

import numpy as np
from PIL import Image, ImageDraw, ImageFont
from scipy import ndimage as ndi

FONT = "/System/Library/Fonts/Supplemental/Arial.ttf"
CELL = 6
SLOT_W, SLOT_H, GAP = 60, 42, 9

FLOORS = {
    "abyss_f1": dict(title="Бездна, этаж 1 · уровни 1–3", biome="abyss", m=0.0, locs=4, scale=.88, lobes=(3, 5),
                     noise=.5, trail="wiggly", trail_r=(1.5, 1.7), amp=3.0, loops=(0, 0), frags=(14, 20), holes=0,
                     bg=(12, 14, 32), ground=(92, 104, 122), edge=(150, 170, 190)),
    "abyss_f2": dict(title="Бездна, этаж 2 · уровни 4–7", biome="abyss", m=.15, locs=5, scale=.95, lobes=(3, 5),
                     noise=.38, trail="wiggly", trail_r=(1.7, 2.1), amp=2.6, loops=(1, 2), frags=(8, 12), holes=0,
                     bg=(38, 44, 96), ground=(104, 118, 142), edge=(170, 185, 225)),
    "abyss_f3": dict(title="Бездна, этаж 3 · уровни 8–13 · Врата", biome="abyss", m=.3, locs=5, scale=1.22,
                     lobes=(3, 5), noise=.3, trail="wiggly", trail_r=(2.2, 2.8), amp=1.8, loops=(2, 2), frags=(4, 7),
                     holes=3, bg=(84, 46, 40), ground=(122, 122, 118), edge=(220, 190, 130)),
    "catacombs_f1": dict(title="Катакомбы, этаж 1 · уровни 14–16", biome="catacombs", m=.45, locs=5, scale=1.1,
                         lobes=(2, 3), noise=.22, n=3, trail="wiggly", trail_r=(2.0, 2.4), amp=1.0, loops=(2, 2),
                         walls=.35, stone=.1, bg=(118, 110, 132), ground=(150, 142, 170)),
    "catacombs_f2": dict(title="Катакомбы, этаж 2 · уровни 17–19", biome="catacombs", m=.6, locs=4, scale=1.05,
                         lobes=(1, 2), noise=.14, n=4, trail="mixed", trail_r=(1.6, 1.9), amp=.6, loops=(1, 2),
                         walls=.65, stone=.35, bg=(88, 82, 104), ground=(136, 130, 148)),
    "catacombs_f3": dict(title="Катакомбы, этаж 3 · уровни 20–22", biome="catacombs", m=.8, locs=4, scale=1.0,
                         lobes=(1, 1), noise=.06, n=6, trail="ortho", trail_w=(2, 3), loops=(1, 2), walls=.9,
                         stone=.8, bg=(58, 48, 52), ground=(124, 116, 112)),
    "catacombs_f4": dict(title="Катакомбы, этаж 4 · уровни 23–25", biome="catacombs", m=1.0, locs=3, scale=1.0,
                         lobes=(1, 1), noise=0, n=12, trail="ortho", trail_w=(2, 2), loops=(1, 1), walls=1.0,
                         stone=1.0, bg=(38, 29, 24), ground=(118, 106, 94)),
}

TYPES = {
    "пастбищная": ["pasture", "pasture", "nest", "nest", "lair"],
    "гнездовая": ["pasture", "nest", "nest", "nest", "nest"],
    "охотничья": ["pasture", "lair", "nest", "ambush", "ambush"],
    "руинная": ["pasture", "nest", "ruin", "ruin", "nest"],
    "опасная": ["pasture", "nest", "hazard", "hazard", "lair"],
}
SIZES = {"pasture": (7, 10), "nest": (3.3, 4.3), "lair": (4.8, 6), "ambush": (3.5, 4.5), "ruin": (4.5, 6),
         "hazard": (5, 6.5)}
TINT = {"pasture": (160, 158, 110), "nest": (110, 160, 120), "lair": (170, 100, 100), "ambush": (150, 110, 160),
        "ruin": (140, 130, 165), "hazard": (175, 128, 90), "special": (110, 170, 190)}


def dist(a, b):
    return math.hypot(a[0] - b[0], a[1] - b[1])


class Zone:
    def __init__(self, kind, x, y, r, loc, a=None, b=None):
        self.kind, self.x, self.y, self.r, self.loc = kind, x, y, r, loc
        self.a, self.b = a or r, b or r

    @property
    def c(self):
        return (self.x, self.y)


def slots_for(n):
    top = math.ceil(n / 2)
    bottom = n - top
    width = top * SLOT_W + (top + 1) * GAP
    res = [(GAP + i * (SLOT_W + GAP), GAP) for i in range(top)]
    off = (width - (bottom * SLOT_W + (bottom + 1) * GAP)) / 2
    res += [(off + GAP + i * (SLOT_W + GAP), GAP * 2 + SLOT_H) for i in range(bottom)]
    return res, width, SLOT_H * 2 + GAP * 3


def build(key, seed):
    P = FLOORS[key]
    rng = random.Random(seed)
    slots, FW, FH = slots_for(P["locs"])
    yy, xx = np.mgrid[:FH, :FW] + .5
    noise = ndi.gaussian_filter(np.random.default_rng(seed).normal(size=(FH, FW)), 2.5)
    noise /= np.abs(noise).max()
    walk = np.zeros((FH, FW), bool)
    types = rng.sample(list(TYPES), min(P["locs"], 5))
    while len(types) < P["locs"]:
        types.append(rng.choice(list(TYPES)))
    zones, locs = [], []
    cat = P["biome"] == "catacombs"

    def disc(mask, x, y, r):
        mask |= (xx - x) ** 2 + (yy - y) ** 2 <= r * r

    def stamp(mask, pts, r):
        for p, q in zip(pts, pts[1:]):
            n = int(dist(p, q) * 2) + 1
            for t in range(n + 1):
                disc(mask, p[0] + (q[0] - p[0]) * t / n, p[1] + (q[1] - p[1]) * t / n, r)

    def wiggly(a, b, amp):
        pts = [a, b]
        for _ in range(3):
            new = [pts[0]]
            for p, q in zip(pts, pts[1:]):
                mx, my = (p[0] + q[0]) / 2, (p[1] + q[1]) / 2
                dx, dy = q[0] - p[0], q[1] - p[1]
                L = math.hypot(dx, dy) or 1
                off = rng.uniform(-amp, amp) * L / 12
                new += [(mx - dy / L * off, my + dx / L * off), q]
            pts = new
            amp *= .6
        return pts

    def ortho(mask, a, b, w):
        (x0, y0), (x1, y1) = (int(a[0]), int(a[1])), (int(b[0]), int(b[1]))
        h = w // 2
        if rng.random() < .5:
            mask[y0 - h:y0 - h + w, min(x0, x1) - h:max(x0, x1) - h + w] = True
            mask[min(y0, y1) - h:max(y0, y1) - h + w, x1 - h:x1 - h + w] = True
        else:
            mask[min(y0, y1) - h:max(y0, y1) - h + w, x0 - h:x0 - h + w] = True
            mask[y1 - h:y1 - h + w, min(x0, x1) - h:max(x0, x1) - h + w] = True

    def connect(a, b, mask):
        style = P["trail"]
        if style == "mixed":
            style = "ortho" if rng.random() < .5 else "wiggly"
        if style == "ortho":
            ortho(mask, a, b, rng.choice(range(P.get("trail_w", (3, 3))[0], P.get("trail_w", (3, 3))[1] + 1)))
        else:
            stamp(mask, wiggly(a, b, P.get("amp", 1)), rng.uniform(*P.get("trail_r", (1.8, 1.8))))

    def shape(z):
        n = P.get("n", 2)
        field = np.full((FH, FW), -9.0)
        lobes = 1 if z.kind in ("nest", "ambush") and cat else rng.randint(*P["lobes"])
        for i in range(lobes):
            cx = z.x + (rng.uniform(-.45, .45) * z.a if i else 0)
            cy = z.y + (rng.uniform(-.45, .45) * z.b if i else 0)
            a, b = z.a * rng.uniform(.6, .95), z.b * rng.uniform(.6, .95)
            if lobes == 1:
                a, b = z.a, z.b
            d = (np.abs((xx - cx) / a) ** n + np.abs((yy - cy) / b) ** n) ** (1 / n)
            field = np.maximum(field, 1 - d)
        return field + noise * P["noise"] > 0

    for li, ((sx, sy), t) in enumerate(zip(slots, types)):
        kinds = TYPES[t][:] + (["nest"] if rng.random() < .4 else [])
        if cat and P["m"] >= .6 and "ambush" not in kinds and rng.random() < .7:
            kinds.append("ambush")
        zs = []
        for k in kinds:
            for _ in range(4000):
                r = rng.uniform(*SIZES[k]) * P["scale"]
                if cat and k == "ambush":
                    a, b = rng.uniform(8, 11), rng.uniform(1.6, 2.0)
                    if rng.random() < .3:
                        a, b = b, a
                elif cat:
                    asp = rng.uniform(.7, 1.4)
                    a, b = r * asp, r / asp
                else:
                    a = b = r
                margin = 4 if P["m"] < .3 else 3
                x = rng.uniform(sx + a + 2, sx + SLOT_W - a - 2)
                y = rng.uniform(sy + b + 2, sy + SLOT_H - b - 2)
                if all(abs(x - z.x) > a + z.a + margin or abs(y - z.y) > b + z.b + margin for z in zs):
                    zs.append(Zone(k, x, y, max(a, b), li, a, b))
                    break
        for z in zs:
            z.mask = shape(z)
            walk |= z.mask
            if P.get("holes") and z.kind in ("pasture", "hazard", "ruin"):
                for _ in range(rng.randint(0, P["holes"])):
                    hx, hy = z.x + rng.uniform(-.5, .5) * z.a, z.y + rng.uniform(-.5, .5) * z.b
                    if dist((hx, hy), z.c) > 2.5:
                        hole = np.zeros_like(walk)
                        disc(hole, hx, hy, rng.uniform(1.2, 2.2))
                        walk &= ~hole
        core = [i for i, z in enumerate(zs) if z.kind != "nest"]
        edges, inside = [], {core[0]}
        while len(inside) < len(core):
            a_, b_ = min(((i, j) for i in inside for j in core if j not in inside),
                         key=lambda e: dist(zs[e[0]].c, zs[e[1]].c))
            edges.append((a_, b_))
            inside.add(b_)
        extra = sorted(((i, j) for i in core for j in core if i < j and (i, j) not in edges),
                       key=lambda e: dist(zs[e[0]].c, zs[e[1]].c))
        edges += extra[:rng.randint(*P["loops"]) + (1 if t == "охотничья" else 0)]
        for i, z in enumerate(zs):
            if z.kind == "nest":
                edges.append((i, min(core, key=lambda j: dist(z.c, zs[j].c))))
        for a_, b_ in edges:
            connect(zs[a_].c, zs[b_].c, walk)
        zones += zs
        locs.append(dict(type=t, slot=(sx, sy), zones=zs))

    pairs = set()
    for i, (ax, ay) in enumerate(slots):
        for j, (bx, by) in enumerate(slots):
            same_row = abs(ay - by) < 1 and abs(ax - bx) <= SLOT_W + GAP + 1
            overlap = min(ax, bx) + SLOT_W - max(ax, bx)
            if i < j and (same_row or (abs(ay - by) > 1 and overlap > SLOT_W * .3)):
                pairs.add((i, j))
    deg = {i: sum(1 for p in pairs if i in p) for i in range(len(slots))}
    for i in range(len(slots)):
        while deg[i] < 2:
            j = min((j for j in range(len(slots)) if j != i and (min(i, j), max(i, j)) not in pairs),
                    key=lambda j: dist(slots[i], slots[j]))
            pairs.add((min(i, j), max(i, j)))
            deg[i] += 1
            deg[j] += 1
    passes = []

    def add_pass(a_, b_, used):
        za = [z for z in locs[a_]["zones"] if z.kind != "nest"]
        zb = [z for z in locs[b_]["zones"] if z.kind != "nest"]
        options = sorted(((u, v) for u in za for v in zb), key=lambda e: dist(e[0].c, e[1].c))
        p, q = next(((u, v) for u, v in options if (id(u), id(v)) not in used), options[0])
        used.add((id(p), id(q)))
        mask = np.zeros_like(walk)
        connect(p.c, q.c, mask)
        walk[:] |= mask
        cells = np.argwhere(mask & ~p.mask & ~q.mask)
        cells = cells if len(cells) else np.argwhere(mask)
        tx, ty = (p.x + q.x) / 2, (p.y + q.y) / 2
        mid = cells[np.argmin((cells[:, 1] - tx) ** 2 + (cells[:, 0] - ty) ** 2)]
        passes.append(dict(pos=(mid[1] + .5, mid[0] + .5), a=a_, b=b_))

    used = set()
    for a_, b_ in sorted(pairs):
        add_pass(a_, b_, used)
    kinds = ["застава", "печать", "опасный"][:len(passes)] + ["тихий"] * max(0, len(passes) - 3)
    rng.shuffle(kinds)
    for p, k in zip(passes, kinds):
        p["kind"] = k
    free_pairs = [(i, j) for i in range(len(slots)) for j in range(i + 1, len(slots)) if (i, j) not in pairs]
    extra = min(free_pairs, key=lambda e: dist(slots[e[0]], slots[e[1]])) if free_pairs else rng.choice(sorted(pairs))
    add_pass(*extra, used)
    passes[-1]["kind"] = "тайный"
    frags = np.zeros_like(walk)
    if not cat:
        for _ in range(rng.randint(*P["frags"])):
            f = np.zeros_like(walk)
            disc(f, rng.uniform(2, FW - 2), rng.uniform(2, FH - 2), rng.uniform(.8, 2.4))
            if not (ndi.binary_dilation(walk, iterations=2) & f).any():
                frags |= f
    walk[0, :] = walk[-1, :] = False
    walk[:, 0] = walk[:, -1] = False
    return dict(key=key, P=P, rng=rng, walk=walk, zones=zones, locs=locs, passes=passes, frags=frags,
                noise=noise, FW=FW, FH=FH, seed=seed)


def open_graph(f, skip=None):
    n = len(f["locs"])
    adj = {i: set() for i in range(n)}
    for k, p in enumerate(f["passes"]):
        if p["kind"] != "тайный" and k != skip:
            adj[p["a"]].add(p["b"])
            adj[p["b"]].add(p["a"])
    return adj


def hops(adj, start):
    seen, frontier, d = {start: 0}, [start], 0
    while frontier:
        d += 1
        nxt = []
        for u in frontier:
            for v in adj[u]:
                if v not in seen:
                    seen[v] = d
                    nxt.append(v)
        frontier = nxt
    return seen


def passes_ok(f):
    n = len(f["locs"])
    adj = open_graph(f)
    if any(len(adj[i]) < 2 for i in range(n)):
        return False
    for k, p in enumerate(f["passes"]):
        if p["kind"] in ("застава", "печать", "опасный") and len(hops(open_graph(f, k), 0)) < n:
            return False
    return True


def valid(f):
    if not passes_ok(f):
        return False
    er = ndi.binary_erosion(f["walk"], np.ones((2, 2), bool))
    lab, n = ndi.label(er)
    ys, xs = np.nonzero(er)
    ids = set()
    for z in f["zones"]:
        k = np.argmin((xs - z.x) ** 2 + (ys - z.y) ** 2)
        ids.add(lab[ys[k], xs[k]])
    return len(ids) == 1


def generate(key, seed):
    for attempt in range(40):
        f = build(key, seed * 1000 + attempt)
        if valid(f):
            f["attempts"] = attempt + 1
            return f
    raise RuntimeError(key)


def render(f):
    P, rng, walk, FW, FH = f["P"], f["rng"], f["walk"], f["FW"], f["FH"]
    cat = P["biome"] == "catacombs"
    bg = np.array(P["bg"], float)
    px = np.zeros((FH, FW, 3)) + bg
    px += f["noise"][..., None] * (10 if not cat else 6)
    zone_of = np.full((FH, FW), -1)
    yy, xx = np.mgrid[:FH, :FW] + .5
    for i, z in enumerate(f["zones"]):
        inside = (np.abs((xx - z.x) / (z.a * 1.05)) ** 2 + np.abs((yy - z.y) / (z.b * 1.05)) ** 2 <= 1) & walk
        zone_of[inside & (zone_of < 0)] = i
    ground = np.array(P["ground"], float)
    g = ground + f["noise"][..., None] * (14 if not cat else 6)
    for i, z in enumerate(f["zones"]):
        m = zone_of == i
        g[m] = g[m] * .66 + np.array(TINT[z.kind]) * .34
    px[walk] = g[walk]
    if not cat:
        px[f["frags"]] = bg + 22
        edge = walk & ~ndi.binary_erosion(walk)
        px[edge] = px[edge] * .5 + np.array(P["edge"]) * .5
    else:
        wall = ndi.binary_dilation(walk, iterations=1) & ~walk
        face = np.zeros_like(walk)
        face[:-1] = walk[1:] & ~walk[:-1]
        solid = f["noise"] * .5 + np.random.default_rng(f["seed"]).random((FH, FW)) * .5
        keep = solid < P["walls"] * 1.0 - .0 if P["walls"] < 1 else np.ones_like(walk)
        stone = np.random.default_rng(f["seed"] + 1).random((FH, FW)) < P["stone"]
        wl = wall & keep
        px[wl & stone] = (70, 64, 70)
        px[wl & ~stone] = (185, 170, 225)
        if P["m"] >= .8:
            px[face & keep] = (48, 42, 50)
    img = Image.fromarray(np.clip(px, 0, 255).astype(np.uint8)).resize((FW * CELL, FH * CELL), Image.NEAREST)
    d = ImageDraw.Draw(img)
    fnt = ImageFont.truetype(FONT, 11)
    big = ImageFont.truetype(FONT, 13)
    S = lambda x, y: (x * CELL, y * CELL)
    counts = {}

    def cnt(k):
        counts[k] = counts.get(k, 0) + 1

    def icon(x, y, shape, col, r=4):
        X, Y = S(x, y)
        if shape == "dot":
            d.ellipse([X - r, Y - r, X + r, Y + r], fill=col, outline=(15, 15, 15))
        elif shape == "ring":
            d.ellipse([X - r, Y - r, X + r, Y + r], outline=col, width=2)
        elif shape == "sq":
            d.rectangle([X - r, Y - r, X + r, Y + r], fill=col, outline=(15, 15, 15))
        elif shape == "tri":
            d.polygon([(X, Y - r - 1), (X + r + 1, Y + r), (X - r - 1, Y + r)], fill=col, outline=(15, 15, 15))
        elif shape == "dia":
            d.polygon([(X, Y - r - 2), (X + r, Y), (X, Y + r + 2), (X - r, Y)], fill=col, outline=(15, 15, 15))
        elif shape == "star":
            pts = []
            for k in range(10):
                ang = math.pi / 5 * k - math.pi / 2
                rr = r + 2 if k % 2 == 0 else (r + 2) / 2.3
                pts.append((X + rr * math.cos(ang), Y + rr * math.sin(ang)))
            d.polygon(pts, fill=col, outline=(15, 15, 15))

    def free(x, y, spread):
        for _ in range(80):
            qx, qy = x + rng.uniform(-spread, spread), y + rng.uniform(-spread, spread)
            if 0 <= int(qx) < FW and 0 <= int(qy) < FH and walk[int(qy), int(qx)]:
                return qx, qy
        return x, y

    edge = walk & ~ndi.binary_erosion(walk)
    ey, ex = np.nonzero(edge)

    def at_edge(x, y):
        k = np.argsort((ex - x) ** 2 + (ey - y) ** 2)[rng.randrange(0, 5)]
        return ex[k] + .5, ey[k] + .5

    m = P["m"]
    methods = ["пробить", "сквозь", "напитать"] if m < .7 else ["пробить", "напитать"]
    deep_left = 1
    for li, loc in enumerate(f["locs"]):
        sx, sy = loc["slot"]
        d.text(S(sx, sy - 1.6), f"{li + 1} · {loc['type']}", fill=(240, 240, 245), font=big)
        for z in loc["zones"]:
            if z.kind == "pasture":
                for _ in range(rng.randint(1, 2)):
                    fx, fy = free(z.x, z.y, z.r * .45)
                    icon(fx, fy, "ring", (255, 220, 120), 5)
                    cnt("кормёжка")
                    for _ in range(rng.randint(3, 6)):
                        icon(*free(fx, fy, 3), "dot", (235, 205, 120), 2)
            elif z.kind == "nest":
                icon(z.x, z.y, "ring", (150, 235, 150), 6)
                for _ in range(rng.randint(2, 3)):
                    icon(*free(z.x, z.y, 2), "dot", (150, 225, 150), 2)
            elif z.kind == "lair":
                icon(z.x, z.y, "dot", (225, 70, 70), 6)
                cnt("логово")
            elif z.kind == "ambush":
                icon(*free(z.x, z.y, 2), "dot", (205, 70, 205), 4)
                cnt("засада")
                if cat and z.a > z.b:
                    for nx in np.arange(z.x - z.a + 1.5, z.x + z.a - 1, 2.2):
                        X, Y = S(nx, z.y - z.b)
                        d.rectangle([X - 3, Y - 4, X + 3, Y + 1], fill=(215, 170, 110))
            elif z.kind == "ruin":
                for _ in range(2):
                    icon(*free(z.x, z.y, z.r * .6), "sq", (205, 195, 235), 3)
                    cnt("останки")
            elif z.kind == "hazard":
                for _ in range(rng.randint(2, 3)):
                    icon(*free(z.x, z.y, z.r * .7), "tri", (245, 145, 60), 4)
                    cnt("ловушка")
            if z.kind in ("pasture", "hazard") and rng.random() < .45:
                icon(*free(z.x, z.y, z.r * .5), "star", (255, 250, 210), 4)
                cnt("всплеск")
            if z.kind in ("pasture", "ruin", "hazard", "lair") and rng.random() < .6:
                icon(*free(z.x, z.y, z.r * .7), "sq", (120, 200, 255), 3)
                cnt("контейнер")
            if cat and z.kind in ("ruin", "pasture", "lair", "ambush"):
                for _ in range(rng.randint(1, 1 + int(m * 3))):
                    icon(*at_edge(z.x, z.y), "sq", (232, 205, 145), 3)
                    cnt("саркофаг")
            if cat and m >= .8 and z.kind in ("lair", "nest", "ruin") and rng.random() < .8:
                icon(*free(z.x, z.y, z.r * .6), "dot", (240, 235, 215), 2)
                cnt("кости")
            if rng.random() < .3:
                icon(*free(z.x, z.y, z.r * .7), "dia", (140, 255, 205), 3)
                cnt("материал")
        n_hidden = 2 if cat or loc["type"] == "руинная" else 1
        for h in range(n_hidden):
            z = rng.choice([z for z in loc["zones"] if z.kind != "nest"])
            hx0, hy0 = at_edge(z.x, z.y)
            ang = math.atan2(hy0 - z.y, hx0 - z.x)
            hx, hy = hx0 + math.cos(ang) * 4, hy0 + math.sin(ang) * 4
            X, Y = S(hx, hy)
            method = "давление" if deep_left and li == len(f["locs"]) - 1 and h == 0 else rng.choice(methods)
            deep_left -= method == "давление"
            d.rectangle([X - 10, Y - 7, X + 10, Y + 7], outline=(255, 120, 200), width=2)
            d.line([*S(hx0, hy0), X, Y], fill=(255, 120, 200), width=2)
            d.text((X - 16, Y + 8), method, fill=(255, 165, 220), font=fnt)
            cnt("скрытое место")
        for _ in range(rng.randint(1, 2)):
            z = rng.choice(loc["zones"])
            icon(*at_edge(z.x, z.y), "dia", (255, 255, 255), 2)
            cnt("надпись")
    col = {"застава": (225, 70, 70), "печать": (120, 220, 240), "опасный": (245, 145, 60), "тайный": (255, 120, 200),
           "тихий": (215, 215, 215)}
    for p in f["passes"]:
        X, Y = S(*p["pos"])
        c = col[p["kind"]]
        if p["kind"] == "тайный":
            for k in range(0, 360, 40):
                a0 = math.radians(k)
                d.arc([X - 9, Y - 9, X + 9, Y + 9], k, k + 20, fill=c, width=3)
        else:
            d.ellipse([X - 9, Y - 9, X + 9, Y + 9], outline=c, width=3)
        d.text((X + 11, Y - 6), p["kind"], fill=c, font=fnt)
        cnt("перевал")
    locs = f["locs"]
    start = rng.randrange(len(locs))
    dist_from_start = hops(open_graph(f), start)
    far = sorted(range(len(locs)), key=lambda i: -dist_from_start[i])
    arrival = {"abyss_f1": "пробуждение", "catacombs_f1": "из Врат Бездны"}.get(f["key"], "вниз")
    placed = [((arrival, (170, 170, 170)), start), (("якорь", (120, 220, 240)), start)]
    if f["key"] == "abyss_f3":
        placed.append((("Врата Бездны", (255, 235, 150)), far[0]))
    elif f["key"] != "catacombs_f4":
        placed += [(("вверх", (255, 255, 255)), far[0]), (("вверх", (255, 255, 255)), far[1])]
    taken = set()
    for (lab, c), li in placed:
        pool = [z for z in locs[li]["zones"] if z.kind in ("pasture", "ruin", "hazard", "lair") and id(z) not in taken]
        z = rng.choice(pool or [z for z in locs[li]["zones"] if id(z) not in taken] or locs[li]["zones"])
        taken.add(id(z))
        X, Y = S(*free(z.x, z.y, z.r * .5))
        if lab == "якорь":
            d.polygon([(X, Y - 8), (X + 6, Y), (X, Y + 8), (X - 6, Y)], fill=c)
        else:
            d.rectangle([X - 8, Y - 8, X + 8, Y + 8], outline=c, width=2)
        d.text((X + 10, Y - 6), lab, fill=c, font=fnt)
    sw, sh = 19, 11
    d.rectangle([*S(FW - sw - 1, FH - sh - 1), *S(FW - 1, FH - 1)], outline=(255, 90, 90), width=2)
    d.text(S(FW - sw - 1, FH - sh - 2.8), "экран телефона", fill=(255, 140, 140), font=fnt)
    return img, counts


LEGEND = [((255, 220, 120), "кормёжка и стадо"), ((255, 250, 210), "всплеск силы"), ((150, 235, 150), "гнездо"),
          ((225, 70, 70), "логово, застава"), ((205, 70, 205), "охотник, засада"), ((205, 195, 235), "останки восходящего"),
          ((245, 145, 60), "ловушка, опасный перевал"), ((120, 200, 255), "контейнер"), ((232, 205, 145), "саркофаг"),
          ((240, 235, 215), "кости"), ((140, 255, 205), "материал биома"), ((255, 255, 255), "надпись"),
          ((255, 120, 200), "скрытое место, тайный перевал"), ((120, 220, 240), "якорь, печать")]


def sheet(img, title, counts, attempts, walk_cells):
    f = ImageFont.truetype(FONT, 13)
    head = 22
    lines = []
    x, row = 0, []
    d0 = ImageDraw.Draw(Image.new("RGB", (10, 10)))
    for c, t in LEGEND:
        w = 26 + int(d0.textlength(t, font=f))
        if x + w > img.width - 20 and row:
            lines.append(row)
            row, x = [], 0
        row.append((c, t))
        x += w + 8
    lines.append(row)
    out = Image.new("RGB", (img.width, img.height + head + 24 * len(lines) + 30), (18, 18, 22))
    d = ImageDraw.Draw(out)
    poi = sum(v for k, v in counts.items() if k not in ("перевал",))
    d.text((8, 4), f"{title} · {walk_cells} проходимых клеток · точек интереса {poi} · перевалов {counts.get('перевал', 0)}"
                   f" · собран с попытки {attempts}", fill=(255, 255, 255), font=f)
    out.paste(img, (0, head))
    y = img.height + head + 8
    for row in lines:
        x = 10
        for c, t in row:
            d.ellipse([x, y + 3, x + 12, y + 15], fill=c)
            d.text((x + 17, y + 1), t, fill=(220, 220, 220), font=f)
            x += 26 + int(d.textlength(t, font=f)) + 8
        y += 24
    detail = ", ".join(f"{k} {v}" for k, v in sorted(counts.items()))
    d.text((10, y + 2), detail, fill=(170, 170, 180), font=ImageFont.truetype(FONT, 12))
    return out


def main():
    outdir = sys.argv[1]
    seed = int(sys.argv[2]) if len(sys.argv) > 2 else 5
    for key in FLOORS:
        f = generate(key, seed)
        img, counts = render(f)
        out = sheet(img, FLOORS[key]["title"], counts, f["attempts"], int(f["walk"].sum()))
        out.save(f"{outdir}/{key}.png")
        print(key, f["attempts"], int(f["walk"].sum()), counts)


def bench(n):
    for key in FLOORS:
        t = time.time()
        tries = []
        for s in range(n):
            tries.append(generate(key, s)["attempts"])
        dt = (time.time() - t) / n
        print(f"{key}: {n} этажей, все собраны; попыток в среднем {np.mean(tries):.2f}, максимум {max(tries)}; "
              f"{dt * 1000:.0f} мс на этаж (Python)")


if __name__ == "__main__":
    if sys.argv[1] == "bench":
        bench(int(sys.argv[2]))
    else:
        main()
