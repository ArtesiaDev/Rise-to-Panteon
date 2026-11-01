import contextlib
import importlib.util
import io
import json
from pathlib import Path
import struct
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch
import zlib

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "Tools/rag"))
from rag import context, lint


def load(name, relative):
    spec = importlib.util.spec_from_file_location(name, ROOT / relative)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


hook = load("codex_hook", ".codex/hooks/harness.py")
art = load("art", "Tools/art/art.py")
eleven = load("eleven", "Tools/sfx/eleven.py")
sfx = load("sfx", "Tools/sfx/sfx.py")


def png(path, alpha=True):
    def chunk(kind, data):
        return struct.pack(">I", len(data)) + kind + data + struct.pack(">I", zlib.crc32(kind + data))
    path.write_bytes(b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", 16, 16, 8, 6 if alpha else 2, 0, 0, 0))
                     + chunk(b"IDAT", zlib.compress((b"\0" + bytes(16 * (4 if alpha else 3))) * 16)) + chunk(b"IEND", b""))


class HookTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.state = patch.object(hook, "STATE", Path(self.temp.name))
        self.state.start()
        self.addCleanup(self.state.stop)
        self.data = {"cwd": str(ROOT), "session_id": "test-session"}

    def test_shell_quotes_separators_and_workdir(self):
        data = dict(self.data, tool_name="Bash", tool_input={"command": "cat 'A B.cs';cat C.cs", "workdir": str(ROOT / "Assets/_Project/Code")})
        with patch.object(lint, "changed_files", return_value=([], [])):
            result = hook.candidates(data)
        self.assertEqual(result, {"Assets/_Project/Code/A B.cs": {"read"}, "Assets/_Project/Code/C.cs": {"read"}})

    def test_patch_move_and_untracked_changes(self):
        data = dict(self.data, tool_name="apply_patch", tool_input={"command": "*** Update File: A.cs\n*** Move to: B.cs\n"})
        with patch.object(lint, "changed_files", return_value=(["C.cs"], ["D.cs"])):
            result = hook.candidates(data)
        self.assertTrue(all("edit" in result[name] for name in ("A.cs", "B.cs", "C.cs", "D.cs")))

    def test_path_outside_project_is_ignored(self):
        result = hook.candidates(dict(self.data, tool_name="Read", tool_input={"path": "/tmp/not-project.cs"}))
        self.assertEqual(result, {})

    def test_session_start_preserves_base_on_resume(self):
        hook.run("session-start", self.data)
        base = next(Path(self.temp.name).glob("*.base"))
        saved = base.read_text()
        with patch("subprocess.run", side_effect=AssertionError("resume must not reset baseline")):
            hook.run("session-start", self.data)
        self.assertEqual(base.read_text(), saved)

    def test_sessions_are_separate(self):
        hook.run("session-start", self.data)
        hook.run("session-start", dict(self.data, session_id="another"))
        self.assertEqual(len(list(Path(self.temp.name).glob("*.base"))), 2)

    def test_missing_session_is_not_silently_shared(self):
        with self.assertRaises(ValueError):
            hook.run("session-start", {"cwd": str(ROOT)})

    def test_note_context_is_single_json(self):
        data = dict(self.data, tool_name="Read", tool_input={"path": "Assets/_Project/Code/Simulation/Example.cs"})
        out = io.StringIO()
        with contextlib.redirect_stdout(out):
            hook.run("note-hint", data)
        self.assertIn("CODE-19", json.loads(out.getvalue())["hookSpecificOutput"]["additionalContext"])

    def test_stop_blocks_once_per_report(self):
        hook.run("session-start", self.data)
        issue = lint.Issue("ERROR", "test.cs", 1, "COMMENT", "test violation")
        with patch.object(lint, "run", return_value=[issue]):
            first, second = io.StringIO(), io.StringIO()
            with contextlib.redirect_stdout(first):
                hook.run("stop", self.data)
            with contextlib.redirect_stdout(second):
                hook.run("stop", self.data)
        self.assertEqual(json.loads(first.getvalue())["decision"], "block")
        self.assertEqual(second.getvalue(), "")

    def test_stop_without_baseline_warns_and_still_lints(self):
        out = io.StringIO()
        with patch.object(lint, "run", return_value=[]) as run, contextlib.redirect_stdout(out):
            hook.run("stop", self.data)
        run.assert_called_once()
        self.assertIn("SessionStart", json.loads(out.getvalue())["systemMessage"])


class RagTests(unittest.TestCase):
    def test_bad_git_base_is_error_not_clean_lint(self):
        with self.assertRaises(RuntimeError):
            lint.changed_files("refs/heads/rtp-test-nonexistent-base")

    def test_codex_skills_and_entrypoint_have_link_checks(self):
        targets = lint.link_targets()
        self.assertIn("AGENTS.md", targets)
        self.assertIn(".agents/skills/rtp-assets/SKILL.md", targets)

    def test_tool_context_includes_rules_and_owner(self):
        result = context.context_for_paths(["Tools/art/art.py", ".codex/hooks/harness.py"])
        for expected in ("DOC-01", "WORK-01", "Harness.md §10", "Harness.md §13"):
            self.assertIn(expected, result)


class ArtTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.dir = Path(self.temp.name)
        self.job = dict(description="one stone", out=str(self.dir / "asset.png"), size="32x32", transparent=True,
                        refs=[], style_ref="none", feature="W11", force=False)

    def test_request_json_does_not_start_generation(self):
        out = io.StringIO()
        with patch.object(sys, "argv", ["art", "one stone", "--out", self.job["out"], "--request-json", "--transparent"]), \
                patch.object(art, "generate", side_effect=AssertionError("generation")), contextlib.redirect_stdout(out):
            art.main()
        data = json.loads(out.getvalue())
        self.assertTrue(data["transparent_background"])
        self.assertEqual(data["references"][0]["role"], "style")
        self.assertIn(art.style_block(), data["prompt"])
        self.assertFalse(Path(self.job["out"]).exists())

    def test_invalid_alpha_never_overwrites_destination(self):
        self.job["force"] = True
        target = Path(self.job["out"])
        target.write_bytes(b"original")
        def fake_run(*args, **kwargs):
            png(Path(kwargs["cwd"]) / "result.png", alpha=False)
            return subprocess.CompletedProcess(args, 0, "", "")
        with patch.object(art.shutil, "which", return_value="codex"), patch.object(art.subprocess, "run", side_effect=fake_run):
            status, _ = art.generate(self.job)
        self.assertEqual(status, "error")
        self.assertEqual(target.read_bytes(), b"original")

    def test_timeout_keeps_queue_and_destination(self):
        with patch.object(art.shutil, "which", return_value="codex"), \
                patch.object(art.subprocess, "run", side_effect=subprocess.TimeoutExpired("codex", 1)):
            status, _ = art.generate(self.job)
        self.assertEqual(status, "error")
        self.assertFalse(Path(self.job["out"]).exists())

    def test_limit_preserves_pending_job(self):
        with patch.object(art, "QUEUE", self.dir / "pending.jsonl"), patch.object(art, "generate", return_value=("limit", "quota")):
            art.queue_add(self.job)
            with contextlib.redirect_stderr(io.StringIO()), contextlib.redirect_stdout(io.StringIO()), self.assertRaises(SystemExit):
                art.run_pending()
            self.assertEqual(art.load_queue(), [self.job])

    def test_short_png_does_not_raise(self):
        path = self.dir / "short.png"
        path.write_bytes(b"\x89PNG\r\n\x1a\n")
        self.assertEqual(art.png_info(path), (0, 0, False))

    def test_dry_run_cannot_drain_queue(self):
        with patch.object(sys, "argv", ["art", "--dry-run", "--run-pending"]), \
                patch.object(art, "run_pending", side_effect=AssertionError("generation")), \
                contextlib.redirect_stderr(io.StringIO()), self.assertRaises(SystemExit):
            art.main()


class SoundTests(unittest.TestCase):
    def test_eleven_success_does_not_depend_on_credit_endpoint(self):
        out = io.StringIO()
        with tempfile.TemporaryDirectory() as tmp, patch.object(sys, "argv", ["eleven", "wind", "--out", str(Path(tmp) / "wind.wav")]), \
                patch.object(eleven, "request", return_value=(b"audio", {"character-cost": "10"})) as request, \
                patch.object(eleven, "credits", side_effect=AssertionError("extra network request")), \
                patch.object(eleven.subprocess, "run", return_value=subprocess.CompletedProcess([], 0, '{"results":[{"seconds":1}]}', "")), \
                contextlib.redirect_stdout(out):
            eleven.main()
        self.assertEqual(request.call_count, 1)
        self.assertEqual(json.loads(out.getvalue())["results"][0]["credits"], 10)

    def test_loop_rejects_generation_and_filters_before_work(self):
        for flags in (["wind", "--loop"], ["--input", "x.wav", "--loop", "--reverb", "0.5"]):
            with self.subTest(flags=flags), patch.object(sys, "argv", ["sfx", "--out", "x.wav", *flags]), \
                    patch.object(sfx.subprocess, "run", side_effect=AssertionError("processing")), \
                    contextlib.redirect_stderr(io.StringIO()), self.assertRaises(SystemExit):
                sfx.main()

    def test_clean_silent_short_and_tail_sound(self):
        try:
            clean = load("clean", "Tools/sfx/clean.py")
        except ImportError:
            self.skipTest("requires installed numpy/scipy/soundfile")
        np = clean.np
        with self.assertRaises(ValueError):
            clean.trim_silence(np.zeros((2, 1024)), 44100)
        self.assertEqual(clean.trim_silence(np.ones((2, 100)), 44100).shape, (2, 100))
        y = np.zeros((2, 300))
        y[:, -1] = 1
        self.assertTrue(np.any(clean.trim_silence(y, 44100)))
        shaped = clean.shape(np.ones((2, 100)), 44100, {"lowpass": None, "reverb": 0})
        self.assertEqual(shaped.shape, (2, 100))


if __name__ == "__main__":
    unittest.main()
