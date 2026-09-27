import argparse
import sys


def main(argv=None):
    parser = argparse.ArgumentParser(prog="rag", description="База знаний Rise to Panteon")
    sub = parser.add_subparsers(dest="cmd", required=True)

    p = sub.add_parser("search", help="поиск по смыслу и ключевым словам")
    p.add_argument("query")
    p.add_argument("--layers", default="")
    p.add_argument("-k", type=int, default=6)
    p.add_argument("--short", action="store_true")

    p = sub.add_parser("context", help="контекст для файлов, которые будут меняться")
    p.add_argument("paths", nargs="+")
    p.add_argument("--no-rules", action="store_true")

    p = sub.add_parser("get", help="документ, раздел (путь#раздел), правило (SIM-03, E01 R3), фича (E01), тип")
    p.add_argument("ref", nargs="+")

    p = sub.add_parser("related", help="связанные документы по графу")
    p.add_argument("ref")
    p.add_argument("--hops", type=int, default=1)

    p = sub.add_parser("lint", help="проверки базы знаний и кода")
    p.add_argument("--changed", action="store_true", help="только изменения относительно --base (по умолчанию HEAD)")
    p.add_argument("--base")

    p = sub.add_parser("index", help="обновить индекс")
    p.add_argument("--rebuild", action="store_true")

    p = sub.add_parser("eval", help="эталонные запросы")
    p.add_argument("-k", type=int, default=5)
    p.add_argument("-v", action="store_true")

    args = parser.parse_args(argv)

    if args.cmd == "context":
        from .context import context_for_paths
        print(context_for_paths(args.paths, include_rules=not args.no_rules))
    elif args.cmd == "get":
        from .context import get
        print(get(" ".join(args.ref)))
    elif args.cmd == "related":
        from .context import related
        print(related(args.ref, args.hops))
    elif args.cmd == "lint":
        from .lint import report, run
        issues = run(changed_only=args.changed, base=args.base)
        print(report(issues))
        sys.exit(1 if any(i.level == "ERROR" for i in issues) else 0)
    elif args.cmd == "index":
        from .index import DB_PATH, Index
        if args.rebuild and DB_PATH.exists():
            import sqlite3
            conn = sqlite3.connect(DB_PATH)
            conn.executescript("DELETE FROM files; DELETE FROM chunks; DELETE FROM fts;")
            conn.commit()
            conn.close()
        stats = Index().refresh(embed=True, progress=True)
        print(f"Индекс: изменено {stats['changed']}, удалено {stats['removed']}, векторизовано {stats['embedded']}")
    elif args.cmd == "search":
        from .search import Searcher, format_hits
        layers = [l for l in args.layers.split(",") if l] or None
        hits = Searcher().search(args.query, layers=layers, k=args.k)
        print(format_hits(args.query, hits, full=not args.short))
    elif args.cmd == "eval":
        from .evaluate import run_eval
        sys.exit(run_eval(k=args.k, verbose=args.v))


if __name__ == "__main__":
    main()
