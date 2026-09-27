import re
from dataclasses import dataclass
from pathlib import Path

from .common import (
    CODE_ROOT, CONTOUR_RULES, EXCLUDED_PARTS, FEATURES_ROOT, GENERATED_PARTS, HEADING, LEGACY_ROOTS, ROOT,
    parse_frontmatter, rel,
)

NOTE_NAME = "README.md"
TYPE_ROW = re.compile(r"^\|\s*`([A-Za-z_][A-Za-z0-9_<>,.]*)`")
CONTOUR_H2 = set(CONTOUR_RULES)


@dataclass
class CodePlace:
    path: str
    kind: str
    unit: str = ""
    unit_root: str = ""
    contour: str = ""


def classify(path: str) -> CodePlace:
    p = rel(path)
    parts = p.split("/")
    if any(p == r or p.startswith(r + "/") for r in LEGACY_ROOTS):
        return CodePlace(p, "legacy")
    if p.startswith(FEATURES_ROOT + "/") and len(parts) >= 4:
        unit = parts[3]
        contour = parts[4] if len(parts) > 5 else ""
        kind = "template" if unit in EXCLUDED_PARTS else "feature"
        return CodePlace(p, kind, unit, f"{FEATURES_ROOT}/{unit}", contour if contour in CONTOUR_RULES else "")
    if p.startswith(CODE_ROOT + "/") and len(parts) >= 4:
        unit = parts[3]
        return CodePlace(p, "infra", f"Code/{unit}", f"{CODE_ROOT}/{unit}", unit if unit in CONTOUR_RULES else "")
    if p.startswith("Docs/"):
        return CodePlace(p, "doc")
    return CodePlace(p, "other")


def is_code_file(path: str) -> bool:
    place = classify(path)
    if place.kind not in ("feature", "infra") or not path.endswith(".cs"):
        return False
    if path.endswith(".g.cs"):
        return False
    return not any(part in GENERATED_PARTS for part in Path(path).parts)


def type_name(path: str) -> str:
    return Path(path).name.split(".")[0]


def note_candidates(path: str):
    place = classify(path)
    if place.kind not in ("feature", "infra", "template"):
        return []
    root = ROOT / place.unit_root
    current = (ROOT / place.path).parent
    found = []
    while True:
        found.append(current / NOTE_NAME)
        if current == root or root not in current.parents:
            break
        current = current.parent
    return [rel(f) for f in found]


def nearest_note(path: str):
    for candidate in note_candidates(path):
        if (ROOT / candidate).is_file():
            return candidate
    return None


def passport(path: str):
    place = classify(path)
    if place.kind not in ("feature", "infra", "template"):
        return None
    top = f"{place.unit_root}/{NOTE_NAME}"
    return top if (ROOT / top).is_file() else nearest_note(path)


def read_note(note: str):
    text = (ROOT / note).read_text(encoding="utf-8")
    meta, body, offset = parse_frontmatter(text)
    return meta, body, offset


def note_sections(body: str):
    lines = body.splitlines()
    sections, h2, current = [], None, None
    for i, line in enumerate(lines):
        m = HEADING.match(line)
        if not m:
            if current is not None:
                current["lines"].append(line)
            continue
        level, text = len(m.group(1)), m.group(2).strip()
        if level <= 2:
            h2 = re.sub(r"[`*]", "", text).strip() if level == 2 else None
            current = None
        if level == 3:
            name = re.sub(r"[`*]", "", text).strip().split()[0] if text.strip() else ""
            current = {"h2": h2, "name": name, "heading": text, "line": i + 1, "lines": []}
            sections.append(current)
        elif level > 3 and current is not None:
            current["lines"].append(line)
    return sections


def table_rows(body: str):
    rows = []
    lines = body.splitlines()
    h2 = None
    header = None
    for i, line in enumerate(lines):
        m = HEADING.match(line)
        if m and len(m.group(1)) == 2:
            h2 = re.sub(r"[`*]", "", m.group(2)).strip()
        if line.startswith("|") and i + 1 < len(lines) and re.match(r"^\|[\s:|-]+\|?\s*$", lines[i + 1]):
            header = [line, lines[i + 1]]
            continue
        if not line.startswith("|"):
            header = None if not line.strip() else header
            continue
        tm = TYPE_ROW.match(line)
        if tm:
            name = re.split(r"[<,.]", tm.group(1))[0]
            rows.append({"h2": h2, "name": name, "line": i + 1, "text": "\n".join((header or []) + [line])})
    return rows


def find_entry(note: str, name: str):
    meta, body, offset = read_note(note)
    for s in note_sections(body):
        if s["name"] == name:
            text = f"### {s['heading']}\n" + "\n".join(s["lines"]).strip()
            return {"kind": "section", "line": s["line"] + offset, "text": text}
    for r in table_rows(body):
        if r["name"] == name:
            return {"kind": "row", "line": r["line"] + offset, "text": r["text"]}
    return None


def scope_files(note: str):
    note_dir = (ROOT / note).parent
    nested = set()
    for readme in note_dir.rglob(NOTE_NAME):
        if readme.parent != note_dir:
            nested.add(readme.parent)
    files = []
    for f in note_dir.rglob("*.cs"):
        if any(n == f.parent or n in f.parents for n in nested):
            continue
        r = rel(f)
        if is_code_file(r):
            files.append(r)
    return sorted(files)


def all_code_files():
    files = []
    for root in (CODE_ROOT, FEATURES_ROOT):
        base = ROOT / root
        if not base.is_dir():
            continue
        for f in base.rglob("*.cs"):
            r = rel(f)
            if is_code_file(r):
                files.append(r)
    return sorted(files)


def all_notes():
    notes = []
    for root in (CODE_ROOT, FEATURES_ROOT):
        base = ROOT / root
        if not base.is_dir():
            continue
        for f in base.rglob(NOTE_NAME):
            if not any(part in EXCLUDED_PARTS for part in f.parts):
                notes.append(rel(f))
    return sorted(notes)
