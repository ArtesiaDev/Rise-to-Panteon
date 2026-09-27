from pathlib import Path

import yaml

from .search import Searcher

GOLDEN = Path(__file__).resolve().parents[1] / "eval/golden.yaml"


def matches(row, expect: str) -> bool:
    path, anchor = row[1], row[2]
    if "#" in expect:
        file_part, section = expect.split("#", 1)
        return path.endswith(file_part) and anchor.startswith(section)
    return path.endswith(expect)


def run_eval(k: int = 5, verbose: bool = False) -> int:
    cases = yaml.safe_load(GOLDEN.read_text(encoding="utf-8"))
    searcher = Searcher()
    searcher.index.refresh(embed=True)
    hits_at_k, rr_sum, misses = 0, 0.0, []
    for case in cases:
        rows = searcher.search(case["q"], k=k, refresh=False)
        rank = next((i + 1 for i, row in enumerate(rows) if any(matches(row, e) for e in case["expect"])), None)
        if rank:
            hits_at_k += 1
            rr_sum += 1.0 / rank
        else:
            misses.append((case, rows))
        if verbose:
            print(f"{'✓' if rank else '✗'} {rank or '-'}  {case['q']}")
    n = len(cases)
    print(f"hit@{k}: {hits_at_k}/{n} = {hits_at_k / n:.0%}   MRR: {rr_sum / n:.2f}")
    for case, rows in misses:
        print(f"\n✗ {case['q']}  ждали {case['expect']}")
        for row in rows:
            print(f"    {row[1]}#{row[2]}")
    return 0 if hits_at_k / n >= 0.8 else 1
