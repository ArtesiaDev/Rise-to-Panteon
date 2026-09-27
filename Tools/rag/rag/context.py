import re
from collections import defaultdict, deque

from . import notes
from .common import (
    CODE_RULES_ALWAYS, CONTOUR_DOCS, CONTOUR_RULES, DOC_RULES_ALWAYS, FEATURE_ID, GDD_RULE_REF, RULE_DOCS, ROOT,
    corpus_files, feature_id_of, layer_of, normalize_gdd_rule, parse_doc, rel, slugify,
)

_cache = {"sig": None, "docs": None, "graph": None}


def corpus():
    files = corpus_files()
    sig = tuple(sorted((p, (ROOT / p).stat().st_mtime_ns) for p in files))
    if _cache["sig"] != sig:
        docs = {p: parse_doc(p, layer) for p, layer in files.items()}
        _cache.update(sig=sig, docs=docs, graph=None)
    return _cache["docs"]


def rules_index():
    result = {}
    for doc in corpus().values():
        for rid, text in doc.rules.items():
            result.setdefault(rid, (doc.path, text))
    return result


def features():
    docs = corpus()
    return {feature_id_of(p): p for p, d in docs.items() if d.layer == "gdd-feature" and feature_id_of(p)}


def graph():
    if _cache["graph"] is not None and _cache["docs"] is corpus():
        return _cache["graph"]
    docs = corpus()
    feats = features()
    edges = defaultdict(set)

    def add(a, b, kind):
        if a != b:
            edges[a].add((b, kind))
            edges[b].add((a, kind + "↩"))

    by_name = defaultdict(list)
    for p in docs:
        by_name[p.rsplit("/", 1)[-1]].append(p)
    for path, doc in docs.items():
        for _, target, _ in doc.links:
            if target in docs:
                add(path, target, "ссылка")
        for mention in doc.mentions:
            target = mention if mention in docs else rel((ROOT / path).parent / mention)
            if target not in docs:
                named = by_name.get(mention.rsplit("/", 1)[-1], [])
                target = named[0] if len(named) == 1 else None
            if target:
                add(path, target, "упоминание")
        for fid in doc.feature_ids:
            if fid in feats and doc.layer in ("gdd-feature", "note", "roadmap"):
                add(path, feats[fid], "фича")
        if doc.layer == "note":
            for key in ("gdd", "participates"):
                for fid in doc.meta.get(key, []) or []:
                    if fid in feats:
                        add(path, feats[fid], "владелец" if key == "gdd" else "участвует")
            for dep in doc.meta.get("depends", []) or []:
                target = f"{notes.FEATURES_ROOT}/{dep}/README.md"
                if target in docs:
                    add(path, target, "зависит")
    _cache["graph"] = edges
    return edges


def related(ref: str, hops: int = 1, limit: int = 40) -> str:
    docs = corpus()
    start = resolve_doc(ref)
    if not start:
        return f"Документ не найден: {ref}"
    edges = graph()
    seen = {start: 0}
    kinds = defaultdict(set)
    queue = deque([start])
    while queue:
        node = queue.popleft()
        if seen[node] >= hops:
            continue
        for target, kind in edges.get(node, ()):
            kinds[target].add(kind)
            if target not in seen:
                seen[target] = seen[node] + 1
                queue.append(target)
    items = [p for p in seen if p != start]
    items.sort(key=lambda p: (seen[p], -len(kinds[p]), p))
    out = [f"Связи {start} (хопов: {hops}):"]
    by_layer = defaultdict(list)
    for p in items[:limit]:
        by_layer[docs[p].layer].append(f"- {p} — {docs[p].title} [{', '.join(sorted(kinds[p]))}]")
    for layer, rows in by_layer.items():
        out.append(f"\n{layer}:")
        out.extend(rows)
    if len(items) > limit:
        out.append(f"\n…ещё {len(items) - limit}")
    return "\n".join(out)


def resolve_doc(ref: str):
    docs = corpus()
    ref = ref.strip().split("#", 1)[0]
    if ref in docs:
        return ref
    r = rel(ref)
    if r in docs:
        return r
    feats = features()
    if ref.upper() in feats:
        return feats[ref.upper()]
    candidates = [p for p in docs if p.endswith("/" + ref) or p.endswith("/" + ref + ".md")]
    return candidates[0] if len(candidates) == 1 else None


def section_text(path: str, selector: str):
    doc = corpus()[path]
    sel = selector.strip()
    slug = slugify(sel)
    heads = []
    for c in doc.chunks:
        for h in c.crumbs[1:]:
            if h not in heads:
                heads.append(h)
    match = [h for h in heads if slugify(h) == slug] or [h for h in heads if sel.lower() in h.lower()]
    if not match:
        return None
    parts = [c for c in doc.chunks if match[0] in c.crumbs[1:]]
    return "\n\n".join(f"{'#' * min(len(c.crumbs) + 1, 3)} {' › '.join(c.crumbs[1:]) or c.crumbs[0]}\n{c.text}"
                       for c in parts)


def get(ref: str) -> str:
    ref = ref.strip()
    rules = rules_index()
    gdd_rule = normalize_gdd_rule(ref)
    if gdd_rule and gdd_rule in rules:
        path, text = rules[gdd_rule]
        return f"{gdd_rule} ({path}):\n{text}"
    if ref.upper() in rules:
        path, text = rules[ref.upper()]
        return f"{ref.upper()} ({path}): {text}"
    if "#" in ref:
        path_ref, selector = ref.split("#", 1)
        path = resolve_doc(path_ref)
        if path:
            text = section_text(path, selector)
            return f"{path}#{selector}\n\n{text}" if text else f"Раздел «{selector}» не найден в {path}"
    path = resolve_doc(ref)
    if path:
        return f"{path}\n\n{(ROOT / path).read_text(encoding='utf-8')}"
    if (ROOT / ref).is_file():
        return f"{rel(ref)}\n\n{(ROOT / ref).read_text(encoding='utf-8')}"
    if re.fullmatch(r"[A-Z][A-Za-z0-9_]*", ref):
        hits = []
        for f in notes.all_code_files():
            if notes.type_name(f) == ref:
                note = notes.nearest_note(f)
                entry = notes.find_entry(note, ref) if note else None
                hits.append(f"{f}\nЗаметка: {note or 'нет'}\n{entry['text'] if entry else 'Записи о типе нет.'}")
        if hits:
            return "\n\n".join(hits)
    return f"Не найдено: {ref}. Для поиска по смыслу — search."


def id_blocks():
    text = (ROOT / RULE_DOCS["CODE"]).read_text(encoding="utf-8")
    m = re.search(r"<!-- id-blocks:begin -->(.*?)<!-- id-blocks:end -->", text, re.S)
    result = {}
    for line in (m.group(1) if m else "").splitlines():
        cells = [c.strip() for c in line.strip().strip("|").split("|")]
        if len(cells) >= 4 and cells[0].startswith("0x"):
            result[cells[1]] = {"block": cells[0], "gdd": re.findall(r"[BCDEIKLMNUW]\d{2}", cells[2]),
                                "status": cells[3]}
    return result


def rules_block(prefixes) -> str:
    rules = rules_index()
    out = []
    for prefix in prefixes:
        items = sorted((rid, v) for rid, v in rules.items() if rid.startswith(prefix + "-"))
        if not items:
            continue
        out.append(f"\n### {prefix} — {RULE_DOCS.get(prefix, items[0][1][0])}")
        out.extend(f"- **{rid}** {text}" for rid, (_, text) in items)
    return "\n".join(out)


def gdd_refs_in(text: str):
    refs = set()
    for m in re.finditer(r"\b([BCDEIKLMNUW]\d{2})[ .]?R(\d+)((?:\s*[,–-]\s*R?\d+)*)", text):
        fid, prev = m.group(1), int(m.group(2))
        refs.add((fid, prev))
        for sep, num in re.findall(r"\s*([,–-])\s*R?(\d+)", m.group(3)):
            num = int(num)
            if sep in "–-" and num > prev:
                refs.update((fid, n) for n in range(prev + 1, num + 1))
            else:
                refs.add((fid, num))
            prev = num
    return [f"{fid} R{n}" for fid, n in sorted(refs)]


def context_for_paths(paths, include_rules: bool = True) -> str:
    feats = features()
    rules = rules_index()
    out = []
    prefixes = []

    def want(items):
        for p in items:
            if p not in prefixes:
                prefixes.append(p)

    for raw in paths:
        path = rel(raw)
        place = notes.classify(path)
        out.append(f"## {path}")
        if place.kind == "legacy":
            out.append("Прототип старой концепции: не менять, не ссылаться, не брать за образец (CODE-01).")
            continue
        if place.kind in ("feature", "infra", "template"):
            want(CODE_RULES_ALWAYS + CONTOUR_RULES.get(place.contour, []))
            note = notes.nearest_note(path)
            pas = notes.passport(path)
            head = [f"Единица: {place.unit}", f"контур: {place.contour or '—'}", f"заметка: {note or 'нет'}"]
            out.append(" · ".join(head))
            if not note:
                expected = notes.note_candidates(path)[-1] if notes.note_candidates(path) else "—"
                out.append(f"Заметки нет — создай {expected} по CodeStructure §6.4.")
            meta = notes.read_note(pas)[0] if pas else {}
            if not meta and place.kind == "feature":
                planned = id_blocks().get(place.unit)
                if planned:
                    meta = {"gdd": planned["gdd"], "block": planned["block"]}
                    out.append(f"Срез по CodeStructure §7.4: блок {planned['block']}, статус «{planned['status']}».")
                else:
                    out.append("Среза нет в CodeStructure §7.4 — сначала выбери владельца и блок (§6.1, §7.2).")
            if meta:
                out.append("Паспорт: " + "; ".join(f"{k}: {', '.join(v) if isinstance(v, list) else v}"
                                                  for k, v in meta.items()))
            if note and path.endswith(".cs"):
                entry = notes.find_entry(note, notes.type_name(path))
                if entry:
                    out.append(f"\n{entry['text']}")
                    refs = gdd_refs_in(entry["text"])
                    if refs:
                        out.append("\nПравила GDD из записи:")
                        out.append("\n\n".join(rules[r][1] for r in refs if r in rules))
                else:
                    out.append(f"В заметке нет записи о {notes.type_name(path)} — добавь (CODE-19).")
            docs_list = []
            for fid in (meta.get("gdd", []) or []):
                if fid in feats:
                    docs_list.append(f"- GDD (владелец): {feats[fid]}")
            for fid in (meta.get("participates", []) or []):
                if fid in feats:
                    docs_list.append(f"- GDD (участвует): {feats[fid]}")
            if place.contour in CONTOUR_DOCS:
                docs_list.append(f"- Контур: {CONTOUR_DOCS[place.contour]}")
            for dep in (meta.get("depends", []) or []):
                docs_list.append(f"- Зависимость: {notes.FEATURES_ROOT}/{dep}/README.md")
            if docs_list:
                out.append("\nДокументы:")
                out.extend(docs_list)
            continue
        layer = layer_of(path)
        want(DOC_RULES_ALWAYS)
        if layer:
            out.append(f"Слой: {layer}")
            fid = feature_id_of(path)
            if fid:
                out.append(f"Фича {fid}: правил {sum(1 for r in rules if r.startswith(fid + ' R'))}")
            out.append(related(path, 1, limit=15))
        else:
            out.append("Файл вне базы знаний.")
    if include_rules and prefixes:
        out.append("\n## Правила")
        out.append(rules_block(prefixes))
    return "\n".join(out)
