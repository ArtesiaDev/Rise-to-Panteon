from mcp.server.mcpserver import MCPServer

from . import context, lint as lint_module
from .common import LAYERS

mcp = MCPServer("rag", instructions=(
    "База знаний Rise to Panteon: GDD, архитектура, заметки к коду, роадмап. "
    "Перед правкой или созданием файлов — context_for_paths(paths). Вопрос «как устроено / где / почему» — search. "
    "Точный документ, раздел, правило (ARCH-06, SIM-03, E01 R3) или фичу (E01) — get."
))

_searcher = None


def searcher():
    global _searcher
    if _searcher is None:
        from .search import Searcher
        _searcher = Searcher()
    return _searcher


@mcp.tool()
def search(query: str, layers: list[str] | None = None, k: int = 6) -> str:
    """Поиск по смыслу и ключевым словам (векторы + BM25) с дотягиванием связанных документов по графу.
    layers — фильтр слоёв: gdd, gdd-feature, gdd-reference, arch, reference, harness, roadmap, note (по умолчанию все).
    Возвращает фрагменты целиком с путём и разделом."""
    from .search import format_hits
    hits = searcher().search(query, layers=[l for l in (layers or []) if l in LAYERS] or None, k=k)
    return format_hits(query, hits)


@mcp.tool()
def context_for_paths(paths: list[str], rules: bool = True) -> str:
    """Всё, что нужно знать перед правкой или созданием файлов (пути от корня репозитория, файл может ещё не
    существовать): запись заметки о типе, паспорт среза, правила GDD из записи, GDD-владелец, документ контура,
    зависимости и правила (ARCH, CODE и правила контура; для документов — DOC). Без векторов, детерминированно."""
    return context.context_for_paths(paths, include_rules=rules)


@mcp.tool()
def get(ref: str) -> str:
    """Точная выдача: правило (ARCH-06, SIM-03, DOC-02, E01 R3), фича (E01), документ (путь или имя файла),
    раздел (путь#заголовок или часть заголовка), тип кода (AbsorbSystem → запись в заметке)."""
    return context.get(ref)


@mcp.tool()
def related(ref: str, hops: int = 1) -> str:
    """Связанные документы по графу: ссылки, упоминания фич, шапки заметок (владелец, участвует, зависит)."""
    return context.related(ref, hops)


@mcp.tool()
def lint(changed: bool = True, base: str | None = None) -> str:
    """Детерминированные проверки: комментарии в коде, покрытие кода заметками, устаревшие записи, исключения Burst,
    битые ссылки, документы вне разрешённых мест. changed=True — только изменения относительно base (HEAD)."""
    return lint_module.report(lint_module.run(changed_only=changed, base=base))


def main():
    mcp.run()


if __name__ == "__main__":
    main()
