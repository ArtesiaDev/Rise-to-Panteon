import re
from dataclasses import dataclass, field
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]

CORPUS = [
    ("Docs/GDD/Features/*.md", "gdd-feature"),
    ("Docs/GDD/Glossary.md", "gdd-reference"),
    ("Docs/GDD/Formulas.md", "gdd-reference"),
    ("Docs/GDD/*.md", "gdd"),
    ("Docs/Tech/Architecture/*.md", "arch"),
    ("Docs/Tech/Reference/Unity/*.md", "reference"),
    ("Docs/Tech/Harness.md", "harness"),
    ("Docs/Roadmap.md", "roadmap"),
    ("Assets/_Project/Code/**/README.md", "note"),
    ("Assets/_Project/Features/**/README.md", "note"),
]

LAYERS = list(dict.fromkeys(layer for _, layer in CORPUS))
DEFAULT_LAYERS = list(LAYERS)

EXCLUDED_PARTS = {"_Template~"}

CODE_ROOT = "Assets/_Project/Code"
FEATURES_ROOT = "Assets/_Project/Features"
LEGACY_ROOTS = [
    "Assets/_Project/Dots",
    "Assets/_Project/Framework",
    "Assets/_Project/Main",
    "Assets/_Project/Dev",
    "Assets/_Project/Editor",
    "Assets/_Project/Data",
]
GENERATED_PARTS = {"Generated"}

CONTOUR_RULES = {
    "Core": [],
    "Contracts": ["SIM"],
    "Simulation": ["SIM"],
    "Bridge": ["SIM", "SVC", "CONT"],
    "Services": ["SVC"],
    "Presentation": ["PRES"],
    "UI": ["UI"],
    "App": ["SVC"],
    "Dev": ["SVC", "SIM"],
    "Editor": ["CONT"],
    "Tests": [],
}
CODE_RULES_ALWAYS = ["ARCH", "CODE"]
DOC_RULES_ALWAYS = ["DOC"]

RULE_DOCS = {
    "ARCH": "Docs/Tech/Architecture/README.md",
    "CODE": "Docs/Tech/Architecture/CodeStructure.md",
    "SIM": "Docs/Tech/Architecture/Simulation.md",
    "PRES": "Docs/Tech/Architecture/Presentation.md",
    "UI": "Docs/Tech/Architecture/UI.md",
    "CONT": "Docs/Tech/Architecture/Content.md",
    "SVC": "Docs/Tech/Architecture/Services.md",
    "DOC": "Docs/Tech/Harness.md",
    "WORK": "Docs/Tech/Harness.md",
}

CONTOUR_DOCS = {
    "Contracts": "Docs/Tech/Architecture/Simulation.md",
    "Simulation": "Docs/Tech/Architecture/Simulation.md",
    "Bridge": "Docs/Tech/Architecture/Simulation.md",
    "Services": "Docs/Tech/Architecture/Services.md",
    "App": "Docs/Tech/Architecture/Services.md",
    "Dev": "Docs/Tech/Architecture/Services.md",
    "Presentation": "Docs/Tech/Architecture/Presentation.md",
    "UI": "Docs/Tech/Architecture/UI.md",
    "Editor": "Docs/Tech/Architecture/Content.md",
}

FEATURE_ID = re.compile(r"(?<![A-Za-z0-9])(?:F-)?([BCDEIKLMNUW]\d{2})(?![0-9A-Za-z])")
RULE_ID = re.compile(r"(?<![A-Za-z0-9-])([A-Z]{1,5}-\d{2,3})(?![0-9])")
GDD_RULE_REF = re.compile(r"\b([BCDEIKLMNUW]\d{2})[ .]?R(\d{1,2})\b")
DOC_MENTION = re.compile(r"`((?:[\w.~-]+/)*[\w.~-]+\.md)`")
LINK = re.compile(r"(?<!!)\[([^\]]*)\]\(([^)\s]+)(?:\s+\"[^\"]*\")?\)")
HEADING = re.compile(r"^(#{1,6})\s+(.*?)\s*#*\s*$")
FENCE = re.compile(r"^\s*(```|~~~)")
TABLE_RULE_ROW = re.compile(r"^\|\s*`?([A-Z]{1,5}-\d{2,3})`?\s*\|\s*(.+?)\s*\|?\s*$")
GDD_RULE_LINE = re.compile(r"^- \*\*R(\d+)\.\*\*\s*(.*)$")

MAX_CHUNK = 3500


def rel(path) -> str:
    p = Path(path)
    if not p.is_absolute():
        p = ROOT / p
    try:
        return p.resolve().relative_to(ROOT).as_posix()
    except ValueError:
        return p.as_posix()


def glob_regex(pattern: str):
    out, i = [], 0
    while i < len(pattern):
        if pattern.startswith("**/", i):
            out.append("(?:.*/)?")
            i += 3
        elif pattern[i] == "*":
            out.append("[^/]*")
            i += 1
        else:
            out.append(re.escape(pattern[i]))
            i += 1
    return re.compile("".join(out) + r"\Z")


CORPUS_RE = [(glob_regex(pattern), layer) for pattern, layer in CORPUS]


def layer_of(path: str):
    if any(part in EXCLUDED_PARTS for part in Path(path).parts):
        return None
    for regex, layer in CORPUS_RE:
        if regex.match(path):
            return layer
    return None


def corpus_files():
    seen = {}
    for pattern, layer in CORPUS:
        for p in sorted(ROOT.glob(pattern)):
            if not p.is_file():
                continue
            r = rel(p)
            if r in seen or any(part in EXCLUDED_PARTS for part in p.parts):
                continue
            seen[r] = layer
    return seen


def read(path: str) -> str:
    return (ROOT / path).read_text(encoding="utf-8")


def parse_frontmatter(text: str):
    if not text.startswith("---\n"):
        return {}, text, 0
    end = text.find("\n---", 4)
    if end < 0:
        return {}, text, 0
    block = text[4:end]
    rest_start = text.find("\n", end + 1) + 1
    data = {}
    for line in block.splitlines():
        line = line.split(" #", 1)[0].rstrip()
        if not line.strip() or ":" not in line:
            continue
        key, value = line.split(":", 1)
        value = value.strip()
        if value.startswith("[") and value.endswith("]"):
            items = [v.strip().strip("\"'") for v in value[1:-1].split(",")]
            data[key.strip()] = [v for v in items if v]
        else:
            data[key.strip()] = value.strip("\"'")
    skipped_lines = text[:rest_start].count("\n")
    return data, text[rest_start:], skipped_lines


def slugify(heading: str) -> str:
    s = heading.strip().lower()
    s = re.sub(r"[`*_~\[\]()<>{}:;,.!?\"'«»—–/\\|+=#@$%^&№]", "", s)
    s = re.sub(r"\s+", "-", s)
    return s.strip("-")


def clean_heading(text: str) -> str:
    return re.sub(r"[`*]", "", text).strip()


@dataclass
class Chunk:
    path: str
    anchor: str
    heading: str
    crumbs: list
    line: int
    text: str


@dataclass
class Doc:
    path: str
    layer: str
    title: str
    meta: dict
    body: str
    chunks: list = field(default_factory=list)
    links: list = field(default_factory=list)
    mentions: set = field(default_factory=set)
    feature_ids: set = field(default_factory=set)
    rule_ids: set = field(default_factory=set)
    rules: dict = field(default_factory=dict)


def iter_lines_outside_fences(lines):
    in_fence = False
    for i, line in enumerate(lines):
        if FENCE.match(line):
            in_fence = not in_fence
            yield i, line, True
            continue
        yield i, line, in_fence


def split_long(text: str, limit: int = MAX_CHUNK):
    if len(text) <= limit:
        return [text]
    parts, current, in_fence = [], [], False
    size = 0
    lines = text.splitlines()
    header = []
    for i, line in enumerate(lines):
        if FENCE.match(line):
            in_fence = not in_fence
        is_row = line.startswith("|")
        if is_row and i + 1 < len(lines) and re.match(r"^\|[\s:|-]+\|?\s*$", lines[i + 1]):
            header = [line, lines[i + 1]]
        in_table = is_row and i > 0 and lines[i - 1].startswith("|") and line not in header
        soft = not in_fence and (not line.strip() or line.startswith("- ") or in_table)
        hard = not in_fence and size > limit
        if (soft and size > limit * 0.6) or hard:
            parts.append("\n".join(current).strip())
            current, size = (list(header) if in_table else []), 0
        if not is_row:
            header = [] if line.strip() else header
        current.append(line)
        size += len(line) + 1
    if current:
        parts.append("\n".join(current).strip())
    return [p for p in parts if p]


def parse_doc(path: str, layer: str, text: str = None) -> Doc:
    if text is None:
        text = read(path)
    meta, body, offset = parse_frontmatter(text)
    lines = body.splitlines()
    title = Path(path).stem
    headings = []
    for i, line, fenced in iter_lines_outside_fences(lines):
        if fenced:
            continue
        m = HEADING.match(line)
        if m:
            headings.append((i, len(m.group(1)), clean_heading(m.group(2))))
    h1 = [h for h in headings if h[1] == 1]
    if h1:
        title = h1[0][2]
    doc = Doc(path=path, layer=layer, title=title, meta=meta, body=body)

    cuts = [h for h in headings if h[1] in (2, 3)]
    stack = {2: None}
    bounds = [(0, None)] + [(h[0], h) for h in cuts] + [(len(lines), None)]
    for idx in range(len(bounds) - 1):
        start, head = bounds[idx]
        end = bounds[idx + 1][0]
        if head is None:
            heading, crumbs, anchor = title, [title], ""
            content_lines = [l for l in lines[start:end] if not l.startswith("# ")]
        else:
            _, level, htext = head
            if level == 2:
                stack[2] = htext
                crumbs = [title, htext]
            else:
                crumbs = [title] + ([stack[2]] if stack[2] else []) + [htext]
            heading, anchor = htext, slugify(htext)
            content_lines = lines[start + 1:end]
        content = "\n".join(content_lines).strip()
        if not content:
            continue
        for n, part in enumerate(split_long(content)):
            doc.chunks.append(Chunk(
                path=path,
                anchor=anchor if n == 0 else f"{anchor}~{n + 1}",
                heading=heading,
                crumbs=crumbs,
                line=start + 1 + offset,
                text=part,
            ))

    base = (ROOT / path).parent
    for i, line, fenced in iter_lines_outside_fences(lines):
        if fenced:
            continue
        for m in LINK.finditer(line):
            target = m.group(2)
            if re.match(r"^[a-z]+:", target) or target.startswith("#"):
                continue
            target = target.split("#", 1)[0]
            if not target:
                continue
            doc.links.append((i + 1 + offset, rel(base / target), target))
    for i, line, fenced in iter_lines_outside_fences(lines):
        if not fenced:
            doc.mentions.update(DOC_MENTION.findall(line))
    doc.feature_ids = set(FEATURE_ID.findall(body))
    doc.rule_ids = set(RULE_ID.findall(body))
    if layer in ("arch", "harness"):
        for line in lines:
            m = TABLE_RULE_ROW.match(line)
            if m:
                doc.rules[m.group(1)] = m.group(2).rstrip("|").strip()
    if layer == "gdd-feature":
        fid = feature_id_of(path)
        if fid:
            doc.rules.update(extract_gdd_rules(fid, lines))
    return doc


def feature_id_of(path: str):
    m = re.match(r"([BCDEIKLMNUW]\d{2})-", Path(path).name)
    return m.group(1) if m else None


def extract_gdd_rules(fid: str, lines):
    rules = {}
    i = 0
    while i < len(lines):
        m = GDD_RULE_LINE.match(lines[i])
        if not m:
            i += 1
            continue
        num = m.group(1)
        collected = [lines[i]]
        i += 1
        while i < len(lines):
            line = lines[i]
            if line.startswith("- ") or line.startswith("#"):
                break
            if not line.strip():
                j = i + 1
                while j < len(lines) and not lines[j].strip():
                    j += 1
                if j < len(lines) and lines[j].startswith((" ", "\t")):
                    collected.append(line)
                    i += 1
                    continue
                break
            collected.append(line)
            i += 1
        rules[f"{fid} R{num}"] = "\n".join(collected).strip()
    return rules


def load_corpus():
    docs = {}
    for path, layer in corpus_files().items():
        docs[path] = parse_doc(path, layer)
    return docs


def feature_docs(docs=None):
    result = {}
    if docs is None:
        for p in sorted((ROOT / "Docs/GDD/Features").glob("*.md")):
            fid = feature_id_of(p.name)
            if fid:
                result[fid] = rel(p)
        return result
    for path in docs:
        fid = feature_id_of(path) if docs[path].layer == "gdd-feature" else None
        if fid:
            result[fid] = path
    return result


def normalize_gdd_rule(ref: str):
    m = re.fullmatch(r"\s*([BCDEIKLMNUW]\d{2})[ .]?R(\d{1,2})\s*", ref)
    return f"{m.group(1)} R{int(m.group(2))}" if m else None
