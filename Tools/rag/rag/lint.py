import re
import subprocess
from dataclasses import dataclass
from pathlib import Path

from . import notes
from .common import ROOT, corpus_files, feature_docs, glob_regex, parse_doc, rel

ALLOWED_MD = [glob_regex(p) for p in [
    "CLAUDE.md",
    ".claude/agents/*.md",
    "Docs/Roadmap.md",
    "Docs/GDD/*.md",
    "Docs/GDD/Features/*.md",
    "Docs/Tech/Harness.md",
    "Docs/Tech/ArchitectureDecisions.md",
    "Docs/Tech/Architecture/*.md",
    "Docs/Tech/Reference/**/*.md",
    "Docs/Parked/*.md",
    "Assets/_Project/Code/**/README.md",
    "Assets/_Project/Features/**/README.md",
    "Configs/**/*.md",
]]
LINK_CHECKED = ["CLAUDE.md", ".claude/agents/*.md"]
DIRECTIVE = re.compile(r"^//\s*ReSharper\s+(disable|restore)\b")
ISYSTEM = re.compile(r"\bstruct\s+(\w+)\s*:\s*[^{]*\bISystem\b")
NOTE_KEYS = {"gdd", "participates", "depends", "block"}
SOURCE_EXT = {".cs", ".uss", ".uxml", ".hlsl", ".shader", ".cginc", ".compute"}
SOURCE_ROOTS = ("Assets/_Project/Code/", "Assets/_Project/Features/", "Assets/_Project/Art/")


@dataclass
class Issue:
    level: str
    path: str
    line: int
    code: str
    message: str

    def __str__(self):
        loc = f"{self.path}:{self.line}" if self.line else self.path
        return f"{self.level:5} {loc} [{self.code}] {self.message}"


def git(*args):
    r = subprocess.run(["git", "-c", "core.quotePath=false", *args], cwd=ROOT, capture_output=True, text=True)
    return r.stdout if r.returncode == 0 else ""


def changed_files(base: str = None):
    base = base or "HEAD"
    changed, deleted = set(), set()
    for line in git("diff", "--name-status", "--no-renames", base).splitlines():
        status, _, path = line.partition("\t")
        (deleted if status.startswith("D") else changed).add(path)
    for line in git("ls-files", "--others", "--exclude-standard").splitlines():
        changed.add(line)
    return sorted(changed), sorted(deleted)


def comments_in(source: str):
    found = []
    i, n, line = 0, len(source), 1
    while i < n:
        c = source[i]
        nxt = source[i + 1] if i + 1 < n else ""
        if c == "\n":
            line += 1
            i += 1
        elif c == "/" and nxt == "/":
            end = source.find("\n", i)
            end = n if end < 0 else end
            text = source[i:end].strip()
            if not DIRECTIVE.match(text):
                found.append((line, text[:80]))
            i = end
        elif c == "/" and nxt == "*":
            end = source.find("*/", i + 2)
            end = n if end < 0 else end + 2
            found.append((line, source[i:end].split("\n", 1)[0][:80]))
            line += source.count("\n", i, end)
            i = end
        elif c == "@" and nxt == '"' or (c == "$" and nxt == "@") or (c == "@" and nxt == "$"):
            j = source.find('"', i) + 1
            while j < n:
                if source[j] == '"' and j + 1 < n and source[j + 1] == '"':
                    j += 2
                elif source[j] == '"':
                    break
                else:
                    j += 1
            line += source.count("\n", i, j)
            i = j + 1
        elif c == '"':
            j = i + 1
            while j < n and source[j] != '"' and source[j] != "\n":
                j += 2 if source[j] == "\\" else 1
            i = j + 1
        elif c == "'":
            j = i + 1
            while j < n and source[j] != "'" and source[j] != "\n":
                j += 2 if source[j] == "\\" else 1
            i = j + 1
        else:
            i += 1
    return found


def has_burst(source: str, pos: int) -> bool:
    line_start = source.rfind("\n", 0, pos) + 1
    if "BurstCompile" in source[line_start:pos]:
        return True
    for line in reversed(source[:line_start].splitlines()):
        s = line.strip()
        if not s.startswith("["):
            return False
        if "BurstCompile" in s:
            return True
    return False


def is_source_file(path: str) -> bool:
    if Path(path).suffix not in SOURCE_EXT or not path.startswith(SOURCE_ROOTS):
        return False
    if path.endswith(".cs"):
        return notes.is_code_file(path)
    return notes.classify(path).kind != "legacy" and not path.startswith("Assets/_Project/Art/Tiles/")


def check_comments(path: str, issues: list):
    source = (ROOT / path).read_text(encoding="utf-8", errors="replace")
    if path.endswith(".uxml"):
        found = [(source.count("\n", 0, m.start()) + 1, m.group(0)[:80]) for m in re.finditer(r"<!--.*?-->", source, re.S)]
    else:
        found = comments_in(source)
        if path.endswith(".uss"):
            found = [(line, text) for line, text in found if text.startswith("/*")]
    for line, text in found:
        issues.append(Issue("ERROR", path, line, "COMMENT", f"комментарий в коде (CODE-18): {text}"))


def check_code_file(path: str, issues: list):
    source = (ROOT / path).read_text(encoding="utf-8", errors="replace")
    note = notes.nearest_note(path)
    name = notes.type_name(path)
    if not note:
        target = notes.note_candidates(path)[-1] if notes.note_candidates(path) else "?"
        issues.append(Issue("ERROR", path, 0, "NOTE-MISSING", f"нет заметки, ожидается {target} (CODE-19)"))
        return
    entry = notes.find_entry(note, name)
    if not entry:
        issues.append(Issue("ERROR", path, 0, "NOTE-ENTRY", f"{name} не описан в {note} (CODE-19)"))
    for m in ISYSTEM.finditer(source):
        if has_burst(source, m.start()):
            continue
        sys_entry = notes.find_entry(note, m.group(1))
        if not sys_entry or "Исключение ARCH-06" not in sys_entry["text"]:
            line = source.count("\n", 0, m.start()) + 1
            issues.append(Issue("ERROR", path, line, "BURST",
                                f"{m.group(1)} без [BurstCompile] и без «Исключение ARCH-06: …» в заметке"))


def check_note(note: str, issues: list, feats: dict):
    meta, body, offset = notes.read_note(note)
    for key in meta:
        if key not in NOTE_KEYS:
            issues.append(Issue("WARN", note, 1, "NOTE-FM", f"неизвестный ключ шапки: {key}"))
    for key in ("gdd", "participates"):
        value = meta.get(key, [])
        for fid in value if isinstance(value, list) else [value]:
            if fid not in feats:
                issues.append(Issue("ERROR", note, 1, "NOTE-FM", f"{key}: нет фичи {fid} в Docs/GDD/Features"))
    files = notes.scope_files(note)
    names = {notes.type_name(f) for f in files}
    for s in notes.note_sections(body):
        if s["name"] and s["name"] not in names:
            issues.append(Issue("ERROR", note, s["line"] + offset, "NOTE-STALE",
                                f"раздел {s['name']}: файла {s['name']}.cs нет в области заметки"))
    for r in notes.table_rows(body):
        if r["name"][:1].isupper() and r["name"] not in names:
            issues.append(Issue("WARN", note, r["line"] + offset, "NOTE-STALE",
                                f"строка {r['name']}: файла {r['name']}.cs нет в области заметки"))


def check_links(path: str, issues: list):
    doc = parse_doc(path, "any")
    for line, target, raw in doc.links:
        if not (ROOT / target).exists():
            issues.append(Issue("ERROR", path, line, "LINK", f"битая ссылка: {raw}"))


FEATURE_ROW = re.compile(r"^\|\s*([BCDEIKLMNUW]\d{2})\s*\|.*?\|\s*(.+?)\s*\|[^|]*\|\s*$")
ROADMAP_ROW = re.compile(r"^\|\s*\[([BCDEIKLMNUW]\d{2})\b[^\]]*\]\([^)]*\)\s*\|\s*(.*?)\s*\|")
STATUSES = {"—", "◐", "✓"}


def check_roadmap(issues: list):
    roadmap, registry = "Docs/Roadmap.md", "Docs/GDD/Features.md"
    if not (ROOT / roadmap).is_file() or not (ROOT / registry).is_file():
        return
    expected = {}
    for line in (ROOT / registry).read_text(encoding="utf-8").splitlines():
        m = FEATURE_ROW.match(line)
        if m:
            stage = re.search(r"Прототип|Срез|Гл1", m.group(2))
            expected[m.group(1)] = stage.group(0) if stage else "?"
    seen = {}
    section = None
    for n, line in enumerate((ROOT / roadmap).read_text(encoding="utf-8").splitlines(), 1):
        if line.startswith("## "):
            section = line[3:].strip()
            continue
        if line.startswith("|") and not line.startswith("|---"):
            status = [c.strip() for c in line.strip().strip("|").split("|")]
            if len(status) >= 2 and status[0] not in ("Фича", "Часть") and status[1] not in STATUSES:
                issues.append(Issue("ERROR", roadmap, n, "ROADMAP", f"статус «{status[1]}» не из {sorted(STATUSES)}"))
        m = ROADMAP_ROW.match(line)
        if not m:
            continue
        fid = m.group(1)
        if fid in seen:
            issues.append(Issue("ERROR", roadmap, n, "ROADMAP", f"{fid} повторяется (строка {seen[fid]})"))
        seen[fid] = n
        if fid not in expected:
            issues.append(Issue("ERROR", roadmap, n, "ROADMAP", f"{fid} нет в {registry}"))
        elif expected[fid] != section:
            issues.append(Issue("ERROR", roadmap, n, "ROADMAP",
                                f"{fid} в разделе «{section}», а этап в реестре — «{expected[fid]}»"))
    for fid in sorted(set(expected) - set(seen)):
        issues.append(Issue("ERROR", roadmap, 0, "ROADMAP", f"нет строки для {fid} ({expected[fid]})"))


def link_targets(paths=None):
    targets = set(corpus_files())
    for pattern in LINK_CHECKED:
        targets.update(rel(p) for p in ROOT.glob(pattern))
    if paths is not None:
        targets &= set(paths)
    return sorted(targets)


def run(changed_only: bool = False, base: str = None):
    issues = []
    feats = feature_docs()
    if changed_only:
        changed, deleted = changed_files(base)
        code = [p for p in changed if notes.is_code_file(p) and (ROOT / p).is_file()]
        sources = [p for p in changed if is_source_file(p) and (ROOT / p).is_file()]
        affected_notes = {n for p in changed + deleted if notes.is_code_file(p) and (n := notes.nearest_note(p))}
        affected_notes |= {p for p in changed if p.endswith("/README.md") and p in notes.all_notes()}
        for p in changed:
            if p.endswith(".md") and (ROOT / p).is_file() and not any(r.match(p) for r in ALLOWED_MD):
                issues.append(Issue("ERROR", p, 0, "MD-PLACE",
                                    "документ вне разрешённых мест (DOC-06): перенеси знание в канонический документ"))
        changed_set = set(changed)
        for p in code:
            note = notes.nearest_note(p)
            if note and note not in changed_set:
                issues.append(Issue("WARN", p, 0, "NOTE-SYNC",
                                    f"код изменён, заметка {note} — нет; обнови, если изменилось поведение"))
        links = link_targets() if deleted else link_targets([p for p in changed if p.endswith(".md")])
        roadmap_touched = any(p == "Docs/Roadmap.md" or p.startswith("Docs/GDD/Features") for p in changed + deleted)
    else:
        code = notes.all_code_files()
        sources = [rel(f) for root in SOURCE_ROOTS[:2] if (ROOT / root).is_dir()
                   for f in (ROOT / root).rglob("*") if f.is_file() and is_source_file(rel(f))]
        affected_notes = set(notes.all_notes())
        links = link_targets()
        roadmap_touched = True
    for p in sources:
        check_comments(p, issues)
    for p in code:
        check_code_file(p, issues)
    for n in sorted(affected_notes):
        if (ROOT / n).is_file():
            check_note(n, issues, feats)
    for p in links:
        if (ROOT / p).is_file():
            check_links(p, issues)
    if roadmap_touched:
        check_roadmap(issues)
    return issues


def report(issues) -> str:
    if not issues:
        return "lint: замечаний нет"
    errors = sum(1 for i in issues if i.level == "ERROR")
    lines = [str(i) for i in sorted(issues, key=lambda i: (i.level != "ERROR", i.path, i.line))]
    lines.append(f"lint: ошибок {errors}, предупреждений {len(issues) - errors}")
    return "\n".join(lines)
