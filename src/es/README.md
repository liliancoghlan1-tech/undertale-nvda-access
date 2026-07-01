# Spanish edition — source

Accessibility layer for the Spanish (Español Castellano) translation of Undertale.

The game's Spanish translation is **"Undertale Español Castellano" v1.2.1 by
ArceUseless** — all credit for translating the game goes to them. This project
does **not** include or redistribute that translation; it only adds the NVDA
accessibility layer on top of a copy the user has already translated.

## Files
- `inject_all_es.csx` — the English `../inject_all.csx` with (1) the `#`
  line-break character added to the three speech sanitizers (this translation
  uses `#` as its newline, where English uses `&`), and (2) our own spoken
  strings translated to Castilian Spanish.
- `translate_es.py` — the curated dictionary (167 full-token `"…"` → `"…"`
  replacements) applied to `inject_all_es.csx` to translate the mod's own cues.
  Only quote-delimited GML string tokens are replaced, so identifiers and ini
  keys are never touched.
- `nvda_access_ES.xdelta` — the shipped patch: an xdelta3 diff that turns a
  Spanish-translated `data.win` (ArceUseless v1.2.1, md5 `b5fee0ae…`) into the
  Spanish + accessibility build (md5 `90dde37b…`). Contains only our changes.

## Rebuild
1. Copy `../inject_all.csx` → `inject_all_es.csx`; add `#` next to `&` in the
   three sanitizers; run `translate_es.py`.
2. Apply ArceUseless's translation to a pristine Undertale 1.08 `data.win`.
3. `UndertaleModCli load <spanish>.win --scripts inject_all_es.csx --output <out>.win`
4. `xdelta3 -e -9 -S lzma -s <spanish>.win <out>.win nvda_access_ES.xdelta`
