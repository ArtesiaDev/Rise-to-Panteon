import re
from collections import defaultdict

import numpy as np

from . import context
from .common import DEFAULT_LAYERS, GDD_RULE_REF, LAYERS, RULE_ID
from .index import Index, analyze

RRF_K = 60
POOL = 60


class Searcher:
    def __init__(self, index: Index = None):
        self.index = index or Index()

    def bm25(self, query: str, layers):
        tokens = analyze(query, query=True)
        if not tokens:
            return []
        match = " OR ".join(f'"{t}"' for t in dict.fromkeys(tokens))
        placeholders = ",".join("?" * len(layers))
        rows = self.index.conn.execute(
            f"SELECT c.id FROM fts JOIN chunks c ON c.id = fts.rowid WHERE fts MATCH ? AND c.layer IN ({placeholders}) "
            f"ORDER BY bm25(fts) LIMIT {POOL}", (match, *layers)).fetchall()
        return [r[0] for r in rows]

    def dense(self, query: str, layers):
        ids, mat = self.index.matrix()
        if not len(ids):
            return []
        qv = self.index.embedder.encode([query])[0]
        sims = mat @ qv
        allowed = {r[0] for r in self.index.conn.execute(
            f"SELECT id FROM chunks WHERE layer IN ({','.join('?' * len(layers))})", layers)}
        order = np.argsort(-sims)
        result = []
        for i in order:
            cid = int(ids[i])
            if cid in allowed:
                result.append(cid)
                if len(result) >= POOL:
                    break
        return result

    def search(self, query: str, layers=None, k: int = 6, refresh: bool = True):
        layers = [l for l in (layers or DEFAULT_LAYERS) if l in LAYERS] or DEFAULT_LAYERS
        if refresh:
            self.index.refresh(embed=True)
        scores = defaultdict(float)
        for ranking in (self.bm25(query, layers), self.dense(query, layers)):
            for rank, cid in enumerate(ranking):
                scores[cid] += 1.0 / (RRF_K + rank + 1)
        ranked = sorted(scores, key=lambda c: -scores[c])
        hits, per_doc = [], defaultdict(int)
        for cid in ranked:
            row = self.index.chunk(cid)
            if per_doc[row[1]] >= 3:
                continue
            per_doc[row[1]] += 1
            hits.append(row)
            if len(hits) >= k:
                break
        return hits


def direct_hits(query: str) -> list:
    out = []
    rules = context.rules_index()
    for m in GDD_RULE_REF.finditer(query):
        rid = f"{m.group(1)} R{int(m.group(2))}"
        if rid in rules:
            out.append(f"**{rid}** ({rules[rid][0]}): {rules[rid][1]}")
    for rid in RULE_ID.findall(query):
        if rid in rules:
            out.append(f"**{rid}** ({rules[rid][0]}): {rules[rid][1]}")
    feats = context.features()
    for fid in re.findall(r"\b([BCDEIKLMNUW]\d{2})\b(?!\s*R\d)", query):
        if fid in feats:
            out.append(f"Фича {fid}: {feats[fid]} — {context.corpus()[feats[fid]].title}")
    return out


def format_hits(query: str, hits, full: bool = True, neighbors: bool = True) -> str:
    out = []
    direct = direct_hits(query)
    if direct:
        out.append("## Прямые попадания по ID")
        out.extend(direct)
    out.append(f"## Найдено по запросу «{query}»")
    for row in hits:
        _, path, anchor, heading, crumbs, layer, line, text = row
        ref = f"{path}#{anchor}" if anchor else path
        out.append(f"\n### {ref}  ·  {layer}  ·  строка {line}\n{crumbs}\n")
        out.append(text if full else (text[:600] + ("…" if len(text) > 600 else "")))
    if neighbors and hits:
        edges = context.graph()
        docs = context.corpus()
        seen = {row[1] for row in hits}
        counts = defaultdict(int)
        for path in seen:
            for target, _ in edges.get(path, ()):
                if target not in seen:
                    counts[target] += 1
        top = sorted(counts, key=lambda p: (-counts[p], p))[:8]
        if top:
            out.append("\n## Связанные документы (граф)")
            out.extend(f"- {p} — {docs[p].title}" for p in top)
    return "\n".join(out)
