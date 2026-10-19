#!/usr/bin/env python3
import argparse
import json
import subprocess
import sys
import tempfile
import urllib.error
import urllib.request
from datetime import datetime
from pathlib import Path

API = "https://api.elevenlabs.io/v1"
MODEL = "eleven_text_to_sound_v2"
FORMAT = "mp3_44100_128"
KEY_FILE = Path.home() / ".config/elevenlabs/api_key"
HERE = Path(__file__).resolve().parent
HINTS = {
    "invalid_api_key": f"ключ не принят — заново записать его в {KEY_FILE}",
    "quota_exceeded": "кредиты месяца кончились — ждать сброса (--credits)",
    "detected_unusual_activity": "бесплатный план заблокирован за VPN или общий IP — платный план или другой IP",
}


def request(path: str, body: dict | None = None) -> tuple[bytes, dict]:
    if not KEY_FILE.exists():
        sys.exit(f"eleven: нет ключа {KEY_FILE} — Harness.md §11")
    headers = {"xi-api-key": KEY_FILE.read_text(encoding="utf-8").strip()}
    data = None
    if body is not None:
        headers["Content-Type"] = "application/json"
        data = json.dumps(body).encode()
    try:
        with urllib.request.urlopen(urllib.request.Request(API + path, data=data, headers=headers), timeout=180) as resp:
            return resp.read(), dict(resp.headers.items())
    except urllib.error.HTTPError as err:
        text = err.read().decode("utf-8", "replace")
        try:
            detail = json.loads(text).get("detail", text)
        except json.JSONDecodeError:
            detail = text
        status = detail.get("status", "") if isinstance(detail, dict) else ""
        message = detail.get("message", "") if isinstance(detail, dict) else str(detail)
        hint = f" — {HINTS[status]}" if status in HINTS else ""
        sys.exit(f"eleven: HTTP {err.code} {status} {message}{hint}")


def credits() -> dict:
    sub = json.loads(request("/user/subscription")[0])
    reset = datetime.fromtimestamp(sub.get("next_character_count_reset_unix") or 0).strftime("%Y-%m-%d")
    return {"tier": sub.get("tier"), "used": sub.get("character_count"), "limit": sub.get("character_limit"),
            "reset": reset}


def main() -> None:
    parser = argparse.ArgumentParser(description="Звуковой эффект: генерация ElevenLabs Sound Effects по API и "
                                                 "обработка, как у звуков библиотеки.")
    parser.add_argument("description", nargs="?", help="описание звука по-английски")
    parser.add_argument("--out", help="куда сохранить WAV; при --variants N — <имя>_v1.wav …")
    parser.add_argument("--duration", type=float, default=None, help="длина 0,5–30 с; без неё длину выбирает модель")
    parser.add_argument("--influence", type=float, default=0.3, help="0–1: насколько строго следовать описанию")
    parser.add_argument("--loop", action="store_true", help="бесшовная петля: без подрезки, фильтров, отзвука и затуханий")
    parser.add_argument("--variants", type=int, default=1, help="сколько вариантов; каждый — отдельная генерация")
    parser.add_argument("--reverb", type=float, default=0.0, help="отзвук 0–1")
    parser.add_argument("--lowpass", type=float, default=None, help="доп. срез верхов, Гц")
    parser.add_argument("--force", action="store_true", help="перезаписать существующие файлы")
    parser.add_argument("--credits", action="store_true", help="расход кредитов и дата сброса")
    args = parser.parse_args()

    if args.credits:
        print(json.dumps(credits(), ensure_ascii=False, indent=2))
        return
    if not args.description or not args.out:
        sys.exit("eleven: нужны описание и --out")
    if args.loop and (args.reverb or args.lowpass):
        sys.exit("eleven: петля не обрабатывается — --loop без --reverb и --lowpass")
    out = Path(args.out).resolve()
    count = max(args.variants, 1)
    targets = [out] if count == 1 else [out.with_name(f"{out.stem}_v{i}{out.suffix}") for i in range(1, count + 1)]
    for target in targets:
        if target.exists() and not args.force:
            sys.exit(f"eleven: {target} уже есть — другое имя или --force")
    body = {"text": args.description.strip(), "model_id": MODEL, "prompt_influence": args.influence, "loop": args.loop}
    if args.duration is not None:
        body["duration_seconds"] = args.duration

    results = []
    with tempfile.TemporaryDirectory(prefix="eleven-") as tmp:
        for i, target in enumerate(targets, 1):
            audio, headers = request(f"/sound-generation?output_format={FORMAT}", body)
            raw = Path(tmp) / f"raw_{i}.mp3"
            raw.write_bytes(audio)
            cmd = [sys.executable, str(HERE / "sfx.py"), "--input", str(raw), "--out", str(target), "--force"]
            if args.loop:
                cmd.append("--loop")
            if args.reverb:
                cmd += ["--reverb", str(args.reverb)]
            if args.lowpass:
                cmd += ["--lowpass", str(args.lowpass)]
            done = subprocess.run(cmd, capture_output=True, text=True)
            if done.returncode != 0:
                sys.stderr.write(done.stderr)
                sys.exit("eleven: обработка не удалась")
            seconds = json.loads(done.stdout)["results"][0]["seconds"]
            cost = {k.lower(): v for k, v in headers.items()}.get("character-cost", "0")
            results.append({"file": str(target), "seconds": seconds, "credits": int(cost)})
    print(json.dumps({"prompt": body["text"], "results": results, "account": credits()}, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
