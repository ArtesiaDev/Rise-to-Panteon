import hashlib
import json
import subprocess
import sys
import tempfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "Tools/rag"))

from rag import lint, notes  # noqa: E402
from rag.common import rel  # noqa: E402

STATE = Path(tempfile.gettempdir()) / "rtp-harness"
EDIT_TOOLS = {"Edit", "Write", "MultiEdit"}
MAX_STOP_BLOCKS = 3


def emit(event: str, text: str):
    print(json.dumps({"hookSpecificOutput": {"hookEventName": event, "additionalContext": text}}, ensure_ascii=False))


def state_file(data: dict, suffix: str) -> Path:
    STATE.mkdir(parents=True, exist_ok=True)
    return STATE / f"{data.get('session_id', 'unknown')}.{suffix}"


def session_start(data: dict):
    base = state_file(data, "base")
    if base.exists():
        return
    head = subprocess.run(["git", "rev-parse", "HEAD"], cwd=ROOT, capture_output=True, text=True).stdout.strip()
    if head:
        base.write_text(head)


def note_hint(data: dict):
    tool = data.get("tool_name", "")
    path = (data.get("tool_input") or {}).get("file_path") or ""
    if not path.endswith(".cs"):
        return
    path = rel(path)
    place = notes.classify(path)
    if place.kind == "legacy":
        if tool in EDIT_TOOLS:
            emit("PostToolUse", f"{path} — код прототипа старой концепции: его не меняют и не берут за образец (CODE-01).")
        return
    if not notes.is_code_file(path):
        return
    name = notes.type_name(path)
    note = notes.nearest_note(path)
    if not note:
        target = notes.note_candidates(path)[-1]
        emit("PostToolUse", f"У {path} нет заметки: создай {target} и запись о {name} (CodeStructure.md §6.4, CODE-19).")
        return
    entry = notes.find_entry(note, name)
    if not entry:
        emit("PostToolUse", f"{name} не описан в заметке {note}: добавь запись или строку таблицы (CODE-19).")
    elif tool in EDIT_TOOLS:
        emit("PostToolUse", f"Запись о {name}: {note}:{entry['line']}. Изменилось поведение — обнови её тем же коммитом (CODE-19).")
    else:
        emit("PostToolUse", f"Заметка {note}:{entry['line']} о {name}:\n{entry['text']}")


def stop(data: dict):
    base_file = state_file(data, "base")
    base = base_file.read_text().strip() if base_file.exists() else None
    issues = lint.run(changed_only=True, base=base)
    if not issues:
        return
    report = lint.report(issues)
    digest = hashlib.sha1(report.encode("utf-8")).hexdigest()
    seen = state_file(data, "reported")
    reported = seen.read_text().split() if seen.exists() else []
    if digest in reported or len(reported) >= MAX_STOP_BLOCKS:
        return
    seen.write_text(" ".join(reported + [digest]))
    print(json.dumps({
        "decision": "block",
        "reason": "Проверка изменений сессии (rag lint --changed):\n" + report + "\n"
                  "Исправь ошибки. NOTE-SYNC: если поведение типа изменилось — обнови запись заметки. "
                  "Если замечание неверно — скажи об этом человеку.",
    }, ensure_ascii=False))


def main():
    command = sys.argv[1] if len(sys.argv) > 1 else ""
    try:
        data = json.load(sys.stdin)
    except json.JSONDecodeError:
        data = {}
    handler = {"session-start": session_start, "note-hint": note_hint, "stop": stop}.get(command)
    if handler:
        try:
            handler(data)
        except Exception as error:
            print(f"harness hook {command}: {error}", file=sys.stderr)


if __name__ == "__main__":
    main()
