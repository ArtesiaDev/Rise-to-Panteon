#!/usr/bin/env python3
"""Read-only local prerequisites; never starts Unity, providers or package installers."""
import importlib.util
import json
import os
from pathlib import Path
import shlex
import shutil
import subprocess
import sys
import tomllib

ROOT = Path(__file__).resolve().parents[2]


def inspect():
    checks = []

    def record(name, ok, detail, required=True):
        checks.append({"name": name, "status": "ok" if ok else "error" if required else "unverified", "detail": detail})

    def file(name, path, required=True):
        record(name, path.is_file(), str(path), required)

    config = tomllib.loads((ROOT / ".codex/config.toml").read_text())
    hooks = json.loads((ROOT / ".codex/hooks.json").read_text())
    claude = json.loads((ROOT / ".mcp.json").read_text())
    record("hooks-enabled", config.get("features", {}).get("hooks") is True, "config flag only; /hooks trust is checked in the client")
    for event in ("SessionStart", "PostToolUse", "Stop"):
        groups = hooks.get("hooks", {}).get(event, [])
        record(event, bool(groups), "configured, not a runtime execution proof")
        for group in groups:
            for handler in group.get("hooks", []):
                words = shlex.split(handler.get("command", ""))
                valid = bool(words) and Path(words[0]).is_file() and os.access(words[0], os.X_OK)
                scripts = [w for w in words[1:] if w.endswith(".py")]
                record(event + "-command", valid and bool(scripts) and all(Path(p).is_file() for p in scripts),
                       "interpreter and script exist; command not executed")
    rag = config.get("mcp_servers", {}).get("rag", {})
    record("rag-shared", "rag" in claude.get("mcpServers", {}) and bool(rag) and rag.get("enabled", True), "both clients declare rag")
    file("rag-python", Path(rag.get("command", "/missing")))
    record("rag-cwd", Path(rag.get("cwd", "/missing")).resolve() == ROOT / "Tools/rag", "project corpus")
    record("rag-offline", all(rag.get("env", {}).get(k) == "1" for k in ("HF_HUB_OFFLINE", "TRANSFORMERS_OFFLINE")), "model downloads disabled")
    file("claude-rules", ROOT / "CLAUDE.md")
    file("review-prompt", ROOT / ".claude/agents/reviewer.md")
    for name in ("rtp-reviewer", "rtp-unity-mcp", "rtp-assets"):
        file(name, ROOT / ".agents/skills" / name / "SKILL.md")
    sys.path.insert(0, str(ROOT / "Tools/rag"))
    from rag import lint
    from rag.context import get
    record("rag-get-local", "ISystem" in get("ARCH-06"), "direct get; MCP transport and semantic search need separate checks")
    issues = lint.run(changed_only=True)
    record("rag-lint", not any(i.level == "ERROR" for i in issues), lint.report(issues))
    for command in ("codex", "uv", "ffmpeg", "ffprobe"):
        record(command, bool(shutil.which(command)), shutil.which(command) or "not found", required=False)
    style_dir = ROOT / "Docs/References/Assets/1 Chosen style"
    file("art-style", style_dir / "1 style final.png")
    for key in ("abyss_f1", "abyss_f2", "abyss_f3", "catacombs_f1", "catacombs_f2", "catacombs_f3", "catacombs_f4"):
        file("floor-style-" + key, style_dir / "Floors" / (key + ".png"))
    for module in ("numpy", "scipy", "PIL", "soundfile"):
        record("asset-python-" + module, importlib.util.find_spec(module) is not None, sys.executable, required=False)
    file("layout-font", Path("/System/Library/Fonts/Supplemental/Arial.ttf"), required=False)
    file("elevenlabs-key-file", Path.home() / ".config/elevenlabs/api_key", required=False)
    file("stable-audio-python", Path.home() / ".cache/stable-audio-3/.venv/bin/python", required=False)
    package = json.loads((ROOT / "Packages/manifest.json").read_text()).get("dependencies", {})
    record("unity-mcp-package", "com.coplaydev.unity-mcp" in package, "declared package; editor/server not contacted", required=False)
    others = [name for name, server in config.get("mcp_servers", {}).items() if name != "rag" and server.get("enabled", True)]
    record("other-project-mcp", bool(others), ", ".join(others) or "none; Unity connection is not configured", required=False)
    run = subprocess.run(["git", "diff", "--check"], cwd=ROOT, capture_output=True, text=True)
    record("diff-check", run.returncode == 0, run.stdout + run.stderr or "no whitespace errors")
    return checks


def main():
    try:
        checks = inspect()
    except Exception as error:
        print(json.dumps({"error": str(error)}, ensure_ascii=False))
        return 1
    print(json.dumps({"checks": checks, "limits": "No Unity, network, generation, installation, credential reads or hook execution. Presence does not prove authentication or runtime readiness."}, ensure_ascii=False, indent=2))
    return int(any(c["status"] == "error" for c in checks))


if __name__ == "__main__":
    sys.exit(main())
