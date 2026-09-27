import hashlib
import os
import re
import sqlite3
import sys

import numpy as np
import snowballstemmer

from .common import ROOT, corpus_files, parse_doc

INDEX_DIR = ROOT / "Tools/rag/.index"
DB_PATH = INDEX_DIR / "rag.sqlite"
MODEL_NAME = os.environ.get("RAG_MODEL", "BAAI/bge-m3")
MAX_SEQ = 1024

SCHEMA = """
CREATE TABLE IF NOT EXISTS files(path TEXT PRIMARY KEY, mtime INTEGER, size INTEGER, sha TEXT, layer TEXT, title TEXT);
CREATE TABLE IF NOT EXISTS chunks(id INTEGER PRIMARY KEY AUTOINCREMENT, path TEXT, anchor TEXT, heading TEXT,
    crumbs TEXT, layer TEXT, line INTEGER, text TEXT, hash TEXT);
CREATE INDEX IF NOT EXISTS chunks_path ON chunks(path);
CREATE VIRTUAL TABLE IF NOT EXISTS fts USING fts5(body, tokenize='unicode61 remove_diacritics 2');
CREATE TABLE IF NOT EXISTS vectors(hash TEXT PRIMARY KEY, vec BLOB);
CREATE TABLE IF NOT EXISTS meta(key TEXT PRIMARY KEY, value TEXT);
"""

WORD = re.compile(r"[A-Za-z][A-Za-z0-9_]*|[А-Яа-яЁё]+|\d+")
ID_TOKEN = re.compile(r"\b([A-Z]{1,5})-(\d{2,3})\b")
CAMEL = re.compile(r"[A-Z]+(?=[A-Z][a-z])|[A-Z]?[a-z]+|[A-Z]+|\d+")
STOP = set("""и в во не что он на я с со как а то все она так его но да ты к у же вы за бы по только ее мне было
вот от меня еще нет о из ему теперь когда даже ну вдруг ли если уже или ни быть был него до вас нибудь опять уж вам
ведь там потом себя ничего ей может они тут где есть надо ней для мы тебя их чем была сам чтоб без будто чего раз тоже
себе под будет ж тогда кто этот того потому этого какой совсем ним здесь этом один почти мой тем чтобы нее сейчас были
куда зачем всех никогда можно при наконец два об другой хоть после над больше тот через эти нас про всего них какая
много разве три эту моя впрочем хорошо свою этой перед иногда лучше чуть том нельзя такой им более всегда конечно всю
между это как где какие каких работает устроено""".split())

_stemmer = snowballstemmer.stemmer("russian")
_stem_cache = {}


def stem(word: str) -> str:
    s = _stem_cache.get(word)
    if s is None:
        s = _stemmer.stemWord(word)
        _stem_cache[word] = s
    return s


def analyze(text: str, query: bool = False):
    text = ID_TOKEN.sub(r"\1\2", text)
    tokens = []
    for tok in WORD.findall(text):
        if re.match(r"[А-Яа-яЁё]", tok):
            low = tok.lower().replace("ё", "е")
            if query and low in STOP:
                continue
            tokens.append(stem(low))
        else:
            tokens.append(tok.lower())
            parts = CAMEL.findall(tok)
            if len(parts) > 1:
                tokens.extend(p.lower() for p in parts if len(p) > 1)
    if query:
        tokens = [t for t in tokens if len(t) > 1]
    return tokens


def embed_text(crumbs: str, text: str) -> str:
    return f"{crumbs}\n{text}"


def chunk_hash(body: str) -> str:
    return hashlib.sha1(f"{MODEL_NAME}\n{body}".encode("utf-8")).hexdigest()


class Embedder:
    def __init__(self):
        self.model = None

    def load(self):
        if self.model is None:
            import logging
            import warnings
            warnings.filterwarnings("ignore")
            logging.getLogger("transformers").setLevel(logging.ERROR)
            os.environ.setdefault("TOKENIZERS_PARALLELISM", "false")
            os.environ.setdefault("HF_HUB_OFFLINE", "1" if self._cached() else "0")
            from sentence_transformers import SentenceTransformer
            import torch
            device = "mps" if torch.backends.mps.is_available() else "cpu"
            self.model = SentenceTransformer(MODEL_NAME, device=device)
            self.model.max_seq_length = MAX_SEQ
        return self.model

    @staticmethod
    def _cached() -> bool:
        cache = os.path.expanduser("~/.cache/huggingface/hub/models--" + MODEL_NAME.replace("/", "--"))
        return os.path.isdir(os.path.join(cache, "snapshots"))

    def encode(self, texts, progress=False):
        model = self.load()
        vecs = model.encode(texts, batch_size=16, normalize_embeddings=True, show_progress_bar=progress,
                            convert_to_numpy=True)
        return vecs.astype(np.float32)


class Index:
    def __init__(self, embedder: Embedder = None):
        INDEX_DIR.mkdir(parents=True, exist_ok=True)
        self.conn = sqlite3.connect(DB_PATH, timeout=120)
        self.conn.executescript(SCHEMA)
        self.embedder = embedder or Embedder()
        self._matrix = None
        self._generation = None

    def generation(self) -> int:
        row = self.conn.execute("SELECT value FROM meta WHERE key='generation'").fetchone()
        return int(row[0]) if row else 0

    def bump(self):
        self.conn.execute("INSERT OR REPLACE INTO meta(key, value) VALUES('generation', ?)", (str(self.generation() + 1),))

    def refresh(self, embed: bool = True, progress: bool = False) -> dict:
        files = corpus_files()
        known = {r[0]: r[1:] for r in self.conn.execute("SELECT path, mtime, size, sha FROM files")}
        stats = {"changed": 0, "removed": 0, "embedded": 0}
        for path in set(known) - set(files):
            self._drop(path)
            self.conn.execute("DELETE FROM files WHERE path=?", (path,))
            stats["removed"] += 1
        for path, layer in files.items():
            st = (ROOT / path).stat()
            prev = known.get(path)
            if prev and prev[0] == st.st_mtime_ns and prev[1] == st.st_size:
                continue
            text = (ROOT / path).read_text(encoding="utf-8")
            sha = hashlib.sha1(text.encode("utf-8")).hexdigest()
            if prev and prev[2] == sha:
                self.conn.execute("UPDATE files SET mtime=?, size=? WHERE path=?", (st.st_mtime_ns, st.st_size, path))
                continue
            doc = parse_doc(path, layer, text)
            self._drop(path)
            for c in doc.chunks:
                crumbs = " › ".join(c.crumbs)
                body = embed_text(crumbs, c.text)
                cur = self.conn.execute(
                    "INSERT INTO chunks(path, anchor, heading, crumbs, layer, line, text, hash) VALUES(?,?,?,?,?,?,?,?)",
                    (path, c.anchor, c.heading, crumbs, layer, c.line, c.text, chunk_hash(body)))
                self.conn.execute("INSERT INTO fts(rowid, body) VALUES(?, ?)",
                                  (cur.lastrowid, " ".join(analyze(f"{path} {body}"))))
            self.conn.execute("INSERT OR REPLACE INTO files(path, mtime, size, sha, layer, title) VALUES(?,?,?,?,?,?)",
                              (path, st.st_mtime_ns, st.st_size, sha, layer, doc.title))
            stats["changed"] += 1
        if stats["changed"] or stats["removed"]:
            self.bump()
        self.conn.commit()
        if embed:
            stats["embedded"] = self._embed_missing(progress)
        return stats

    def _drop(self, path: str):
        ids = [r[0] for r in self.conn.execute("SELECT id FROM chunks WHERE path=?", (path,))]
        self.conn.executemany("DELETE FROM fts WHERE rowid=?", [(i,) for i in ids])
        self.conn.execute("DELETE FROM chunks WHERE path=?", (path,))

    def _embed_missing(self, progress: bool) -> int:
        rows = self.conn.execute(
            "SELECT DISTINCT c.hash, c.crumbs, c.text FROM chunks c LEFT JOIN vectors v ON v.hash = c.hash "
            "WHERE v.hash IS NULL").fetchall()
        if not rows:
            return 0
        if progress:
            print(f"Векторизация {len(rows)} фрагментов…", file=sys.stderr)
        batch = 64
        for i in range(0, len(rows), batch):
            part = rows[i:i + batch]
            vecs = self.embedder.encode([embed_text(r[1], r[2]) for r in part], progress=False)
            self.conn.executemany("INSERT OR REPLACE INTO vectors(hash, vec) VALUES(?, ?)",
                                  [(r[0], v.tobytes()) for r, v in zip(part, vecs)])
            self.conn.commit()
            if progress:
                print(f"  {min(i + batch, len(rows))}/{len(rows)}", file=sys.stderr)
        self.bump()
        self.conn.commit()
        return len(rows)

    def matrix(self):
        gen = self.generation()
        if self._matrix is None or self._generation != gen:
            rows = self.conn.execute(
                "SELECT c.id, v.vec FROM chunks c JOIN vectors v ON v.hash = c.hash ORDER BY c.id").fetchall()
            ids = np.array([r[0] for r in rows], dtype=np.int64)
            mat = np.frombuffer(b"".join(r[1] for r in rows), dtype=np.float32).reshape(len(rows), -1) \
                if rows else np.zeros((0, 1024), dtype=np.float32)
            self._matrix = (ids, mat)
            self._generation = gen
        return self._matrix

    def chunk(self, cid: int):
        return self.conn.execute(
            "SELECT id, path, anchor, heading, crumbs, layer, line, text FROM chunks WHERE id=?", (cid,)).fetchone()
