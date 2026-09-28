#!/usr/bin/env python3
import argparse
import html
import json
import re
import subprocess
import sys
import tempfile
import urllib.parse
import urllib.request
from pathlib import Path

SITE = "https://zvukipro.com"
HERE = Path(__file__).resolve().parent
UA = {"User-Agent": "Mozilla/5.0"}
MIN_RATE, MIN_KBPS_PER_CHANNEL = 44100, 56


def fetch(url: str, data: dict | None = None) -> str:
    body = urllib.parse.urlencode(data).encode() if data else None
    with urllib.request.urlopen(urllib.request.Request(url, data=body, headers=UA), timeout=30) as resp:
        return resp.read().decode("utf-8", "replace")


def clean_text(fragment: str) -> str:
    return re.sub(r"\s+", " ", html.unescape(re.sub(r"<[^>]+>", " ", fragment))).strip()


def search(query: str, page: int) -> list[dict]:
    data = {"do": "search", "subaction": "search", "story": query}
    if page > 1:
        data.update({"search_start": page, "full_search": 0, "result_from": (page - 1) * 10 + 1})
    text = fetch(f"{SITE}/index.php?do=search", data)
    marker = text.find("По Вашему запросу")
    if marker < 0:
        return []
    found = re.search(r"найдено (\d+)", text[marker:marker + 200])
    body = text[marker:]
    seen, results = set(), []
    for url, title in re.findall(r'<a href="(https://zvukipro\.com/[a-z_]+/\d+-[^"]+\.html)"[^>]*>([^<]+)</a>', body):
        title = clean_text(title)
        if url in seen or not title or title == "Случайный звук":
            continue
        seen.add(url)
        results.append({"title": title, "url": url})
    return results[:10] if found else results


def sounds(page_url: str) -> list[dict]:
    text = fetch(page_url)
    start = text.find('class="makarona"')
    body = text[start:] if start >= 0 else text
    items = []
    for m in re.finditer(r"<!--dle_audio_begin:(https://[^>]+?\.mp3)-->", body):
        before = body[:m.start()]
        cut = max(before.rfind("<br><br>"), before.rfind('class="makarona">'))
        title = clean_text(before[cut:].replace('class="makarona">', ""))
        after = body[m.end():m.end() + 1500]
        size = re.search(r"\[([\d.,]+\s*[KkMm]b)\]", after)
        count = re.search(r"скачиваний:\s*(\d+)", after)
        items.append({"n": len(items) + 1, "title": title, "mp3": m.group(1),
                      "size": size.group(1) if size else "", "downloads": int(count.group(1)) if count else 0})
    return items


def probe(url_or_path: str) -> dict:
    out = subprocess.run(["ffprobe", "-v", "error", "-show_entries", "stream=sample_rate,channels:format=bit_rate,duration",
                          "-of", "json", url_or_path], capture_output=True, text=True, timeout=60)
    info = json.loads(out.stdout or "{}")
    fmt = info.get("format", {})
    stream = info.get("streams", [{}])[0]
    return {"sample_rate": int(stream.get("sample_rate", 0)), "channels": int(stream.get("channels", 1) or 1),
            "kbps": int(fmt.get("bit_rate", 0) or 0) // 1000,
            "seconds": round(float(fmt.get("duration", 0) or 0), 2)}


def good(q: dict) -> bool:
    return q["sample_rate"] >= MIN_RATE and q["kbps"] / max(q["channels"], 1) >= MIN_KBPS_PER_CHANNEL


def main() -> None:
    parser = argparse.ArgumentParser(description="Звуки с zvukipro.com: поиск подборок, список звуков, скачивание.")
    sub = parser.add_subparsers(dest="cmd", required=True)
    s = sub.add_parser("search", help="найти подборки по запросу (по-русски)")
    s.add_argument("query")
    s.add_argument("--page", type=int, default=1)
    ls = sub.add_parser("list", help="звуки на странице подборки")
    ls.add_argument("page_url")
    ls.add_argument("--probe", action="store_true", help="проверить частоту и битрейт каждого файла")
    g = sub.add_parser("get", help="скачать звук и обработать в WAV (подрезка, громкость)")
    g.add_argument("mp3_url")
    g.add_argument("--out", required=True)
    g.add_argument("--force", action="store_true")
    args = parser.parse_args()

    if args.cmd == "search":
        print(json.dumps(search(args.query, args.page), ensure_ascii=False, indent=2))
    elif args.cmd == "list":
        items = sounds(args.page_url)
        if args.probe:
            for item in items:
                item.update(probe(item["mp3"]))
                item["ok"] = good(item)
        print(json.dumps(items, ensure_ascii=False, indent=2))
    else:
        with tempfile.TemporaryDirectory(prefix="zvuki-") as tmp:
            src = Path(tmp) / "source.mp3"
            with urllib.request.urlopen(urllib.request.Request(args.mp3_url, headers=UA), timeout=60) as resp:
                src.write_bytes(resp.read())
            quality = probe(str(src))
            cmd = [sys.executable, str(HERE / "sfx.py"), "--input", str(src), "--out", args.out]
            if args.force:
                cmd.append("--force")
            done = subprocess.run(cmd, capture_output=True, text=True)
            if done.returncode != 0:
                sys.stderr.write(done.stderr)
                sys.exit("zvuki: обработка не удалась")
        print(json.dumps({"out": str(Path(args.out).resolve()), "source": args.mp3_url, **quality, "ok": good(quality)},
                         ensure_ascii=False, indent=2))
        if not good(quality):
            print(f"zvuki: исходник ниже {MIN_RATE} Гц или {MIN_KBPS_PER_CHANNEL} кбит/с на канал — поискать другой",
                  file=sys.stderr)


if __name__ == "__main__":
    main()
