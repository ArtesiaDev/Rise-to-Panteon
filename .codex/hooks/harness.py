from contextlib import redirect_stdout
from io import StringIO
import hashlib
import importlib.util
import json
from pathlib import Path
import re
import shlex
import sys
import tempfile

ROOT = Path(__file__).resolve().parents[2]
STATE = Path(tempfile.gettempdir()) / "rtp-codex-harness" / hashlib.sha256(str(ROOT).encode()).hexdigest()[:16]


def load_legacy():
    spec = importlib.util.spec_from_file_location("rtp_claude_harness", ROOT / ".claude/hooks/harness.py")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    STATE.mkdir(parents=True, exist_ok=True, mode=0o700)
    module.STATE = STATE
    return module


def candidates(data):
    args = data.get("tool_input") or {}
    name = data.get("tool_name", "").rsplit(".", 1)[-1]
    result = {}
    def add(path, edited=False):
        if not isinstance(path, str) or not path.endswith(".cs"):
            return
        p = Path(path)
        if not p.is_absolute():
            cwd = args.get("workdir") if isinstance(args, dict) else None
            p = Path(cwd or data.get("cwd") or ROOT) / p
        try:
            path = p.resolve().relative_to(ROOT).as_posix()
        except ValueError:
            return
        result.setdefault(path, set()).add("edit" if edited else "read")
    if isinstance(args, dict):
        add(args.get("file_path") or args.get("path"), name in {"Edit", "Write", "MultiEdit", "apply_patch"})
        command = args.get("command") or args.get("cmd") or ""
        try:
            lexer = shlex.shlex(command, posix=True, punctuation_chars=True)
            lexer.whitespace_split = True
            for word in lexer:
                add(word, False)
        except ValueError:
            pass
        patch = args.get("patch") or args.get("input") or (command if name in {"apply_patch", "Edit", "Write"} else "")
    else:
        patch = args if isinstance(args, str) else ""
    for path in re.findall(r"^\*\*\* (?:(?:Add|Update|Delete) File|Move to): (.+)$", patch, re.M):
        add(path, True)
    # Shell writes may not name their paths explicitly. Include actual changed C# files.
    if name in {"Bash", "exec_command", "shell_command", "write_stdin", "apply_patch", "Edit", "Write", "MultiEdit"}:
        module = load_legacy()
        changed, deleted = module.lint.changed_files()
        for path in changed + deleted:
            add(str(ROOT / path), True)
    return result


def run(action, data):
    cwd = Path(data.get("cwd") or ROOT).resolve()
    if cwd != ROOT and ROOT not in cwd.parents:
        return
    module = load_legacy()
    data = dict(data)
    if not data.get("session_id"):
        raise ValueError("Нет session_id: нельзя разделить состояние сессий")
    data["session_id"] = hashlib.sha256(str(data["session_id"]).encode()).hexdigest()
    if action == "session-start":
        module.session_start(data)
        if not module.state_file(data, "base").exists():
            raise RuntimeError("Не удалось сохранить HEAD начала сессии")
    elif action == "note-hint":
        output = StringIO()
        with redirect_stdout(output):
            for path, modes in candidates(data).items():
                for mode in ("read", "edit"):
                    if mode in modes:
                        module.note_hint(dict(data, tool_name="Edit" if mode == "edit" else "Read", tool_input={"file_path": path}))
        contexts = [json.loads(line)["hookSpecificOutput"]["additionalContext"] for line in output.getvalue().splitlines() if line]
        if contexts:
            module.emit("PostToolUse", "\n\n".join(dict.fromkeys(contexts)))
    elif action == "stop":
        if not module.state_file(data, "base").exists():
            output = StringIO()
            with redirect_stdout(output):
                module.stop(data)
            response = json.loads(output.getvalue()) if output.getvalue().strip() else {}
            response["systemMessage"] = "SessionStart не сохранил HEAD; lint проверил рабочее дерево от текущего HEAD, коммиты сессии не покрыты."
            print(json.dumps(response, ensure_ascii=False))
        else:
            module.stop(data)
    else:
        raise ValueError("Unknown hook action")
    with (STATE / "events.jsonl").open("a") as f:
        f.write(json.dumps({"event": action, "session": data["session_id"]}) + "\n")


if __name__ == "__main__":
    try:
        run(sys.argv[1], json.load(sys.stdin))
    except Exception as error:
        print(json.dumps({"systemMessage": "Проверка харнесса Codex не выполнена: " + str(error)}, ensure_ascii=False))
        print(str(error), file=sys.stderr)
        sys.exit(1)
