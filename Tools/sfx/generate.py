import json
import sys
from pathlib import Path

import torch
import torchaudio
from huggingface_hub import hf_hub_download
from stable_audio_3.loading_utils import load_diffusion_cond
from stable_audio_3.model import StableAudioModel

REPO = "FatalErrorVXD/stable-audio-3-small-sfx"
HOP = 256


def rms(a):
    m = a.mean(0)
    n = m.shape[0] // HOP
    return m[: n * HOP].reshape(n, HOP).pow(2).mean(1).sqrt()


def trim(a, sr, single, max_len):
    r = rms(a)
    peak = r.max()
    start = max(int((r > 0.15 * peak).nonzero().flatten()[0]) - 2, 0)
    if single:
        top = int(r.argmax())
        w = max(int(0.1 * sr / HOP), 1)
        quiet = (r < 0.015 * peak).float().view(1, 1, -1)
        run = torch.nn.functional.conv1d(quiet, torch.ones(1, 1, w)).flatten()
        hits = (run[top:] >= w).nonzero().flatten()
        end = top + (int(hits[0]) if len(hits) else len(r) - top) + 3
        end = max(end, start + int(0.25 * sr / HOP))
    else:
        end = int((r > 0.03 * peak).nonzero().flatten()[-1]) + 4
    if max_len:
        end = min(end, start + int(max_len * sr / HOP))
    return a[:, start * HOP : end * HOP].clone()


def punch(a):
    r = rms(a)
    return float(r.max() / r.median().clamp(min=1e-6))


def main():
    job = json.loads(Path(sys.argv[1]).read_text(encoding="utf-8"))
    out_dir = Path(sys.argv[2])
    cfg = json.loads(Path(hf_hub_download(REPO, "model_config.json")).read_text())
    ckpt = hf_hub_download(REPO, "model.safetensors")
    model = StableAudioModel(load_diffusion_cond(cfg, ckpt, device="cpu", model_half=False), cfg, "cpu", False)
    sr = cfg["sample_rate"]
    punches = {}
    for v, seeds in enumerate(job["seeds"], 1):
        for c, seed in enumerate(seeds, 1):
            audio = model.generate(prompt=job["prompt"], duration=job["duration"], seed=seed)
            a = (audio[0] if audio.dim() == 3 else audio).float().cpu()
            a = trim(a, sr, job["single"], job["max_len"])
            name = f"raw_{v}_{c}.wav"
            torchaudio.save(str(out_dir / name), a, sr)
            punches[name] = punch(a)
    (out_dir / "punch.json").write_text(json.dumps(punches), encoding="utf-8")


if __name__ == "__main__":
    main()
