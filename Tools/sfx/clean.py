import json
import sys
from collections import defaultdict
from pathlib import Path

import numpy as np
import soundfile as sf
from scipy.signal import butter, istft, sosfiltfilt, stft

SPLIT = {"noise": 0, "voice": 4000, "impact": 6000}
NFFT, HOP = 1024, 128
rng = np.random.default_rng(3)


def band(y, sr, kind, freq, order=8):
    return sosfiltfilt(butter(order, freq, kind, fs=sr, output="sos"), y, axis=-1)


def resynth(ch, sr):
    freqs, _, spec = stft(ch, fs=sr, nperseg=NFFT, noverlap=NFFT - HOP)
    mag_db = 20 * np.log10(np.abs(spec) + 1e-9)
    logf = np.log2(np.maximum(freqs, 20))
    env = np.empty_like(mag_db)
    for i, lf in enumerate(logf):
        env[i] = mag_db[np.abs(logf - lf) <= 1 / 8].mean(axis=0)
    env = (env + np.roll(env, 1, axis=1) + np.roll(env, -1, axis=1)) / 3
    _, _, noise = stft(rng.standard_normal(len(ch)), fs=sr, nperseg=NFFT, noverlap=NFFT - HOP)
    noise = noise[:, : spec.shape[1]] / np.sqrt(np.mean(np.abs(noise) ** 2))
    _, out = istft(noise * 10 ** (env / 20), fs=sr, nperseg=NFFT, noverlap=NFFT - HOP)
    return out[: len(ch)] if len(out) >= len(ch) else np.pad(out, (0, len(ch) - len(out)))


def rebuild(y, sr, split):
    fresh = np.array([resynth(ch, sr) for ch in y])
    if split == 0:
        return fresh
    return band(y, sr, "lowpass", split) + band(fresh, sr, "highpass", split)


def trim_silence(y, sr, hop=256):
    m = np.abs(y).mean(0)
    n = len(m) // hop
    r = np.sqrt((m[: n * hop].reshape(n, hop) ** 2).mean(1))
    loud = np.nonzero(r > 0.03 * r.max())[0]
    start, end = max(loud[0] - 2, 0), min(loud[-1] + 4, n)
    return y[:, start * hop : end * hop]


def reverb(y, sr, amount, seconds=0.9):
    n = int(seconds * sr)
    t = np.arange(n) / sr
    out = []
    for ch in y:
        ir = rng.standard_normal(n) * np.exp(-t / (seconds / 5))
        size = len(ch) + n
        wet = np.fft.irfft(np.fft.rfft(ch, size) * np.fft.rfft(ir, size), n=size)
        wet *= np.abs(ch).max() / (np.abs(wet).max() + 1e-9)
        out.append(np.pad(ch, (0, n)) + amount * wet)
    return np.array(out)


def shape(y, sr, job):
    y = band(y, sr, "highpass", 30, 2)
    y = band(y, sr, "lowpass", 14000, 4)
    if job["lowpass"]:
        y = band(y, sr, "lowpass", job["lowpass"], 4)
    if job["reverb"]:
        y = reverb(y, sr, job["reverb"])
    fade_in, fade_out = int(0.005 * sr), min(int(0.12 * sr), y.shape[1] // 3)
    y[:, :fade_in] *= np.linspace(0, 1, fade_in)
    y[:, -fade_out:] *= np.linspace(1, 0, fade_out)
    return y


def pick(candidates, punches, single):
    if single:
        return max(candidates, key=lambda c: punches.get(c[0], 1.0))[1]
    return candidates[0][1]


def main():
    job = json.loads(Path(sys.argv[1]).read_text(encoding="utf-8"))
    src, dst = Path(sys.argv[2]), Path(sys.argv[3])
    punch_file = src / "punch.json"
    punches = json.loads(punch_file.read_text(encoding="utf-8")) if punch_file.exists() else {}
    split = SPLIT[job["kind"]]
    groups = defaultdict(list)
    for raw in sorted(src.glob("raw_*.wav")):
        y, sr = sf.read(raw, always_2d=True)
        groups[raw.stem.split("_")[1]].append((raw.name, (y.T, sr)))
    report = []
    for variant in sorted(groups, key=int):
        y, sr = pick(groups[variant], punches, job["single"])
        if job.get("trim"):
            y = trim_silence(y, sr)
        if job["rebuild"]:
            y = rebuild(y, sr, split)
        if not job.get("loop"):
            y = shape(y, sr, job)
        y = y / (np.abs(y).max() + 1e-9) * 0.89
        sf.write(dst / f"out_{variant}.wav", y.T, sr, subtype="PCM_16")
        report.append({"variant": int(variant), "seconds": round(y.shape[1] / sr, 2)})
    (dst / "report.json").write_text(json.dumps(report), encoding="utf-8")


if __name__ == "__main__":
    main()
