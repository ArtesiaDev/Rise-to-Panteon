#!/usr/bin/env python3
import argparse
import json
import os
import random
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

HERE = Path(__file__).resolve().parent
ENGINE = Path.home() / ".cache/stable-audio-3"
ENGINE_REPO = "https://github.com/Stability-AI/stable-audio-3.git"
PROCESS_DEPS = ["numpy==2.5.3", "scipy==1.18.1", "soundfile==0.14.0"]
MIN_RATE, MIN_KBPS_PER_CHANNEL = 44100, 56


def setup() -> None:
    if shutil.which("uv") is None:
        sys.exit("sfx: нет uv — brew install uv")
    if not ENGINE.exists():
        subprocess.run(["git", "clone", "--depth", "1", ENGINE_REPO, str(ENGINE)], check=True)
    subprocess.run(["uv", "sync"], cwd=ENGINE, check=True)
    print(f"sfx: движок готов — {ENGINE}; веса модели (~3 ГБ) скачаются при первой генерации")


def probe(path: Path) -> dict:
    out = subprocess.run(["ffprobe", "-v", "error", "-show_entries", "stream=sample_rate,channels:format=bit_rate",
                          "-of", "json", str(path)], capture_output=True, text=True)
    info = json.loads(out.stdout or "{}")
    stream = info.get("streams", [{}])[0]
    kbps = int(info.get("format", {}).get("bit_rate", 0) or 0) // 1000
    return {"sample_rate": int(stream.get("sample_rate", 0)), "channels": int(stream.get("channels", 1) or 1),
            "kbps": kbps}


def main() -> None:
    parser = argparse.ArgumentParser(description="Звуковой эффект: генерация Stable Audio 3 Small SFX локально или "
                                                 "обработка готового файла; ресинтез убирает пищание модели.")
    parser.add_argument("description", nargs="?", help="описание звука по-английски (для генерации)")
    parser.add_argument("--out", required=False, help="куда сохранить WAV; при --variants N — <имя>_v1.wav …")
    parser.add_argument("--kind", choices=["noise", "voice", "impact"], default="voice",
                        help="noise — шорохи, шипение, жужжание (весь звук пересобирается); voice — голоса, стоны "
                             "(выше 4 кГц); impact — удары, металл, предметы (выше 6 кГц)")
    parser.add_argument("--input", help="обработать готовый файл (mp3/wav) вместо генерации")
    parser.add_argument("--duration", type=float, default=3.0, help="длина генерации, с (звук потом подрезается)")
    parser.add_argument("--single", action="store_true", help="одно событие (удар, лязг): подрезка до одного события")
    parser.add_argument("--max-len", type=float, default=None, help="максимальная длина результата, с")
    parser.add_argument("--variants", type=int, default=1, help="сколько разных вариантов сохранить")
    parser.add_argument("--candidates", type=int, default=None, help="генераций на вариант для --single (по умолчанию 3); берётся самая ударная")
    parser.add_argument("--seed", type=int, default=None, help="начальное зерно")
    parser.add_argument("--reverb", type=float, default=0.0, help="отзвук 0–1")
    parser.add_argument("--lowpass", type=float, default=None, help="доп. срез верхов, Гц")
    parser.add_argument("--no-rebuild", action="store_true", help="без ресинтеза")
    parser.add_argument("--loop", action="store_true", help="готовая петля (--input): без подрезки, фильтров, отзвука и затуханий")
    parser.add_argument("--force", action="store_true", help="перезаписать существующие файлы")
    parser.add_argument("--setup", action="store_true", help="установить движок")
    parser.add_argument("--offline", action="store_true", help="использовать только локальный кэш Python, зависимостей и моделей")
    parser.add_argument("--processing-python", default=os.environ.get("RTP_SFX_PYTHON"),
                        help="готовый Python с numpy/scipy/soundfile, обработка без uv и установки пакетов (также RTP_SFX_PYTHON)")
    args = parser.parse_args()

    if args.offline:
        if args.setup:
            parser.error("--setup несовместим с --offline")
        os.environ.update(UV_OFFLINE="1", UV_PYTHON_DOWNLOADS="never", HF_HUB_OFFLINE="1", TRANSFORMERS_OFFLINE="1")
    if args.loop and (not args.input or args.reverb or args.lowpass):
        parser.error("--loop требует --input и несовместим с --reverb и --lowpass")
    if args.duration <= 0 or args.variants < 1 or (args.candidates is not None and args.candidates < 1):
        parser.error("длительность, число вариантов и кандидатов должны быть положительными")
    if args.setup:
        setup()
        return
    if not args.out or not (args.description or args.input):
        sys.exit("sfx: нужны --out и описание (генерация) или --input (готовый файл)")
    if not args.input and not (ENGINE / ".venv").exists():
        sys.exit("sfx: движок не установлен — python3 Tools/sfx/sfx.py --setup")

    out = Path(args.out).resolve()
    count = 1 if args.input else max(args.variants, 1)
    targets = [out] if count == 1 else [out.with_name(f"{out.stem}_v{i}{out.suffix}") for i in range(1, count + 1)]
    for target in targets:
        if target.exists() and not args.force:
            sys.exit(f"sfx: {target} уже есть — другое имя или --force")
    base = args.seed if args.seed is not None else random.randint(0, 10**6)
    candidates = max(args.candidates or (3 if args.single else 1), 1)
    job = {
        "prompt": (args.description or "").strip(),
        "duration": args.duration,
        "single": args.single,
        "max_len": args.max_len if args.max_len is not None else (1.2 if args.single else None),
        "seeds": [[base + v * 100 + c for c in range(candidates)] for v in range(count)],
        "kind": args.kind,
        "rebuild": not args.no_rebuild and not args.input,
        "trim": bool(args.input) and not args.loop,
        "loop": args.loop,
        "reverb": args.reverb,
        "lowpass": args.lowpass,
    }
    source = {}

    with tempfile.TemporaryDirectory(prefix="sfx-") as tmp:
        work = Path(tmp)
        (work / "job.json").write_text(json.dumps(job, ensure_ascii=False), encoding="utf-8")
        if args.input:
            src = Path(args.input).resolve()
            source = probe(src)
            subprocess.run(["ffmpeg", "-v", "error", "-y", "-i", str(src), "-ar", "44100", "-ac", "2",
                            str(work / "raw_1_1.wav")], check=True)
        else:
            gen = subprocess.run(["uv", "run", "--no-sync", "--project", str(ENGINE), "python", str(HERE / "generate.py"),
                                  str(work / "job.json"), str(work)], capture_output=True, text=True)
            if gen.returncode != 0:
                sys.stderr.write(gen.stderr[-3000:])
                sys.exit("sfx: генерация не удалась")
        if args.processing_python:
            cmd = [args.processing_python]
        else:
            cmd = ["uv", "run", "--quiet", "--no-project", "--python", "3.12"]
            for dep in PROCESS_DEPS:
                cmd += ["--with", dep]
            cmd.append("python")
        proc = subprocess.run(cmd + [str(HERE / "clean.py"), str(work / "job.json"), str(work), str(work)],
                              capture_output=True, text=True)
        if proc.returncode != 0:
            sys.stderr.write(proc.stderr[-3000:])
            sys.exit("sfx: обработка не удалась")
        report = json.loads((work / "report.json").read_text(encoding="utf-8"))
        for item, target in zip(report, targets):
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.move(work / f"out_{item['variant']}.wav", target)
            item["file"] = str(target)
    result = {"prompt": job["prompt"], "kind": job["kind"], "results": report}
    if source:
        result["source"] = source
    print(json.dumps(result, ensure_ascii=False, indent=2))
    if source and (source["sample_rate"] < MIN_RATE or source["kbps"] / source["channels"] < MIN_KBPS_PER_CHANNEL):
        print(f"sfx: исходник низкого качества ({source['sample_rate']} Гц, {source['kbps']} кбит/с, "
              f"каналов {source['channels']}) — искать другой", file=sys.stderr)


if __name__ == "__main__":
    main()
