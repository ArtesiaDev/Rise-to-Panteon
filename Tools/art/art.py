#!/usr/bin/env python3
import argparse
import json
import re
import shutil
import struct
import subprocess
import sys
import tempfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
STYLE_DOC = ROOT / "Docs/GDD/ArtStyle.md"
STYLE_REFERENCE = ROOT / "Docs/References/Assets/1 Chosen style/1 style final.png"
QUEUE = ROOT / "Tools/art/pending.jsonl"
RESULT_NAME = "result.png"
TIMEOUT_S = 900
EXIT_LIMIT = 3
LIMIT_RE = re.compile(r"usage limit|rate.?limit|quota|too many requests|\b429\b|try again (in|at|later)", re.I)

TASK = """$imagegen Use the built-in image_gen tool only (not the CLI fallback, no API key). Generate exactly ONE new image.
Input images: {roles}
Use the image prompt below verbatim — do not add creative requirements.{alpha}
After generation, copy the final image into the current directory as ./{result}. Do not create any other files.
Then report in one short paragraph: built-in or CLI, pixel size, alpha channel yes/no.

IMAGE PROMPT:
{prompt}
"""


def style_block() -> str:
    text = STYLE_DOC.read_text(encoding="utf-8")
    section = text.split("### Стилевой блок промпта", 1)
    match = re.search(r"```text\n(.*?)```", section[1], re.S) if len(section) == 2 else None
    if not match:
        sys.exit(f"art: стилевой блок не найден в {STYLE_DOC}")
    return match.group(1).strip()


def png_info(path: Path) -> tuple[int, int, bool]:
    with path.open("rb") as f:
        head = f.read(26)
    if head[:8] != b"\x89PNG\r\n\x1a\n":
        return 0, 0, False
    width, height = struct.unpack(">II", head[16:24])
    return width, height, head[25] in (4, 6)


def build_prompt(description: str, size: str, transparent: bool, styled: bool = True) -> str:
    block = style_block()
    if not styled:
        block = "\n".join(line for line in block.splitlines() if not line.startswith("Art style — match the attached"))
    parts = [f"Output size: exactly {size} pixels.", block, f"Asset: {description.strip()}"]
    if transparent:
        parts.append("Transparent background (PNG with alpha channel): no ground, no shadow, no other objects.")
    return "\n\n".join(parts)


def main() -> None:
    parser = argparse.ArgumentParser(description="Генерация ассета в стиле ArtStyle.md через Codex CLI (подписка ChatGPT).")
    parser.add_argument("description", nargs="?", help="описание ассета по-английски: что, ракурс, поза, состояния")
    parser.add_argument("--out", help="куда сохранить PNG")
    parser.add_argument("--size", default="2048x2048", help="ширина x высота, кратные 16")
    parser.add_argument("--transparent", action="store_true", help="прозрачный фон")
    parser.add_argument("--ref", action="append", default=[], help="доп. референс сюжета (можно несколько)")
    parser.add_argument("--style-ref", default="", help="эталон стиля вместо общего: путь к файлу или none")
    parser.add_argument("--feature", default="", help="ID фичи GDD, для которой ассет")
    parser.add_argument("--force", action="store_true", help="перезаписать существующий файл")
    parser.add_argument("--dry-run", action="store_true", help="только показать задание для Codex")
    parser.add_argument("--queue", action="store_true", help="не генерировать, а поставить задание в очередь")
    parser.add_argument("--pending", action="store_true", help="показать очередь")
    parser.add_argument("--run-pending", action="store_true", help="сгенерировать задания из очереди")
    args = parser.parse_args()

    if args.pending:
        jobs = load_queue()
        print(json.dumps(jobs, ensure_ascii=False, indent=2) if jobs else "art: очередь пуста")
        return
    if args.run_pending:
        run_pending()
        return
    if not args.description or not args.out:
        sys.exit("art: нужны описание и --out")
    if not re.fullmatch(r"\d+x\d+", args.size):
        sys.exit("art: --size в формате 2048x2048")
    job = {
        "description": args.description.strip(),
        "out": rel(Path(args.out)),
        "size": args.size,
        "transparent": args.transparent,
        "refs": [rel(Path(r)) for r in args.ref],
        "style_ref": args.style_ref if args.style_ref in ("", "none") else rel(Path(args.style_ref)),
        "feature": args.feature,
        "force": args.force,
    }
    check_job(job)
    if args.dry_run:
        print(make_task(job)[1])
        return
    if args.queue:
        queue_add(job)
        print(f"art: в очереди — {job['out']}")
        return
    status, info = generate(job)
    if status == "limit":
        queue_add(job)
        print(f"art: лимит подписки исчерпан, задание в очереди {rel(QUEUE)} — {job['out']}", file=sys.stderr)
        sys.exit(EXIT_LIMIT)
    if status == "error":
        sys.stderr.write(info)
        sys.exit("art: Codex не вернул картинку")
    print(json.dumps(info, ensure_ascii=False, indent=2))


def rel(path: Path) -> str:
    path = path.resolve()
    return str(path.relative_to(ROOT)) if path.is_relative_to(ROOT) else str(path)


def absolute(path: str) -> Path:
    p = Path(path)
    return p if p.is_absolute() else ROOT / p


def check_job(job: dict) -> None:
    out = absolute(job["out"])
    if out.exists() and not job["force"]:
        sys.exit(f"art: {out} уже есть — другое имя или --force")
    style = style_source(job)
    for ref in ([style] if style else []) + list(map(absolute, job["refs"])):
        if not ref.is_file():
            sys.exit(f"art: нет файла {ref}")


def style_source(job: dict) -> Path | None:
    style = job.get("style_ref", "")
    if style == "none":
        return None
    return absolute(style) if style else STYLE_REFERENCE


def inputs(job: dict) -> list[tuple[Path, str]]:
    style = style_source(job)
    refs = [absolute(r) for r in job["refs"]]
    pairs = [(style, f"style_reference{style.suffix.lower()}")] if style else []
    return pairs + [(r, f"subject_{i + 1}{r.suffix.lower()}") for i, r in enumerate(refs)]


def make_task(job: dict) -> tuple[str, str]:
    styled = style_source(job) is not None
    names = [name for _, name in inputs(job)]
    roles = "; ".join(
        ([f"{names[0]} — STYLE REFERENCE only (overall art style, not content)"] if styled else [])
        + [f"{n} — SUBJECT REFERENCE (keep this exact subject)" for n in names if n.startswith("subject_")]
    ) or "none"
    prompt = build_prompt(job["description"], job["size"].replace("x", "×"), job["transparent"], styled)
    alpha = " Ask for a genuinely transparent background and preserve the alpha." if job["transparent"] else ""
    return prompt, TASK.format(roles=roles, alpha=alpha, result=RESULT_NAME, prompt=prompt)


def generate(job: dict) -> tuple[str, object]:
    if shutil.which("codex") is None:
        sys.exit("art: нет codex — npm i -g @openai/codex, затем codex login")
    prompt, task = make_task(job)
    pairs = inputs(job)
    out = absolute(job["out"])
    with tempfile.TemporaryDirectory(prefix="art-") as tmp:
        work = Path(tmp)
        for src, name in pairs:
            shutil.copy(src, work / name)
        cmd = ["codex", "exec", "--skip-git-repo-check", "-s", "workspace-write", "-C", str(work)]
        for _, name in pairs:
            cmd += ["-i", name]
        cmd += ["-o", "report.txt", "-"]
        run = subprocess.run(cmd, input=task, text=True, capture_output=True, cwd=work, timeout=TIMEOUT_S)
        result = work / RESULT_NAME
        report_file = work / "report.txt"
        report = report_file.read_text(encoding="utf-8").strip() if report_file.is_file() else ""
        if run.returncode != 0 or not result.is_file():
            log = run.stdout[-3000:] + run.stderr[-3000:] + report
            return ("limit" if LIMIT_RE.search(log) else "error"), log
        out.parent.mkdir(parents=True, exist_ok=True)
        shutil.move(result, out)
    width, height, has_alpha = png_info(out)
    return "ok", {"out": str(out), "size": f"{width}x{height}", "alpha": has_alpha, "codex": report, "prompt": prompt}


def load_queue() -> list[dict]:
    if not QUEUE.is_file():
        return []
    return [json.loads(line) for line in QUEUE.read_text(encoding="utf-8").splitlines() if line.strip()]


def save_queue(jobs: list[dict]) -> None:
    if jobs:
        QUEUE.write_text("".join(json.dumps(j, ensure_ascii=False) + "\n" for j in jobs), encoding="utf-8")
    elif QUEUE.exists():
        QUEUE.unlink()


def queue_add(job: dict) -> None:
    jobs = [j for j in load_queue() if j["out"] != job["out"]]
    save_queue(jobs + [job])


def run_pending() -> None:
    jobs = load_queue()
    if not jobs:
        print("art: очередь пуста")
        return
    while jobs:
        job = jobs[0]
        if absolute(job["out"]).exists() and not job.get("force"):
            print(f"art: {job['out']} уже есть — задание снято", flush=True)
            jobs = jobs[1:]
            save_queue(jobs)
            continue
        print(f"art: {job['out']} …", flush=True)
        status, info = generate(job)
        if status == "limit":
            save_queue(jobs)
            print(f"art: лимит подписки исчерпан, в очереди осталось {len(jobs)}", file=sys.stderr)
            sys.exit(EXIT_LIMIT)
        if status == "error":
            sys.stderr.write(info)
            save_queue(jobs)
            sys.exit(f"art: Codex не вернул картинку для {job['out']}; задание осталось в очереди")
        print(json.dumps(info, ensure_ascii=False, indent=2), flush=True)
        jobs = jobs[1:]
        save_queue(jobs)
    print("art: очередь выполнена")


if __name__ == "__main__":
    main()
