# UNDERTALE — NVDA Accessibility Mod

**Play UNDERTALE by ear.** A fan-made accessibility mod that makes the whole game —
story, menus, battles, navigation, puzzles, and even the final boss — playable with the
[NVDA screen reader](https://www.nvaccess.org/) for blind and low-vision players.

This mod was made by and tested by a blind player who beat the **entire game** with it,
start to finish, including the hardest fights.

**New in v1.5 — audio description.** Beyond making the game *playable* by ear, the mod now
*describes* it: what each character looks like, what every room looks like (press **L** to
re-hear), the silent visual moments in cutscenes, and what each monster looks like in battle
(press **D**). See the [changelog](CHANGELOG.md).

---

please note due to this Games status as a bullet hell title, direct adaptation of the combat was impossible. Following the directional beeps with your arrow keys will teleport  your soul to safe places on the board. While not the exact same, it should still provide an engaging and challenging experience. 

## ⬇️ Download & Install

1. Get the latest **`Undertale-NVDA-Access.zip`** from the
   [**Releases**](../../releases) page and unzip it anywhere.
2. Make sure UNDERTALE is closed, then run **`Install`** (`Install.bat`).
3. It auto-finds your game, backs it up, and patches it in a few seconds. Done.

Everything the installer says is spoken by NVDA. Your original game is saved as
`data.win.NVDA-BACKUP`, and **`Uninstall`** puts it back exactly.

## 🇪🇸 Spanish edition (Español Castellano)

There's a **Spanish version** — grab `Undertale-NVDA-Access-ES.zip` from the
[Releases](../../releases) page. It adds the same accessibility **on top of** the
community Spanish translation **"Undertale Español Castellano" v1.2.1 by
[ArceUseless](https://twitter.com/Rafael_Nicar)** — all credit for the game's
translation goes to them. Apply their translation first, then run `Instalar.bat`, and
**set NVDA's voice to Spanish**. This project does not include or redistribute the
translation; it only ships its own accessibility layer. Source in
[`src/es/`](src/es/).

## ✅ Requirements

- Windows PC
- **UNDERTALE on Steam, Windows version 1.08** (the installer checks your version and
  refuses safely if it doesn't match — it can't break your game)
- **NVDA** running while you play (free from [nvaccess.org](https://www.nvaccess.org/))

## 🎮 Controls

Full list in **`KEYS.txt`** (included in the download). The essentials:

- **Arrow keys / WASD** move · **Z** interact/confirm · **C** menu
- **E** scans your surroundings; the mod guides you by voice as you walk
- **V** auto-walks you to your selected target (pathfinds around walls)
- **L** describes the room you're in; **D** (in battle) describes the monster you're facing
- **K** opens the in-game **Accessibility menu** (toggle features, difficulty, music) — also on the title screen
- **N / B** turn the music down / up so the cues come through
- In dodging fights, press **M** for **Assisted mode** (can't be defeated) and follow the
  guide beeps — *which ear* = left/right, *pitch* = up/down

## What's covered

Intro & all dialogue, naming screen, title/settings menus, walk-by-ear navigation with
**auto-walk** and ambient navigation sounds, an in-game **Accessibility menu**, music
volume control, the in-game menu (items/stats/cell/save), shops, elevators, battle menus,
the FIGHT timing bar, attack-dodging (with assist/slow/normal modes), the **jump** and
**shield** soul fights, area puzzles (with a skip option for the few that truly need
eyesight), the Mettaton quiz, and the final boss.

**Audio description (v1.5):** character introductions, a description of every room in the
game (on first visit, or on demand with **L**), narration of the silent cutscene beats, and
a monster describe key (**D**) in battle — plus the speaker's name before each line.

### Honest limitations

- A few purely visual puzzles use the **skip** option rather than a by-ear solution.
- Some moving-target boss moments lean on **Assisted/Slow** mode rather than precise
  dodging by ear.
- Built and tested for **one game version** (Steam v1.08, Windows).

This is a fan project — it will have rough edges, and feedback is very welcome.

## How it works (for the curious)

The mod patches Undertale's `data.win` with [UndertaleModTool](https://github.com/UnderminersTeam/UndertaleModTool),
injecting GML that speaks text through NVDA and adds navigation, menu reading, and audio
guidance. Speech goes through NVDA's controller client; spatial cues use a small custom
stereo-beep library. The patch source (`inject_all.csx`) and the bridge sources
(`src/*.c`) are in this repo. **No Undertale game files are included or redistributed** —
the installer only modifies the copy you already own.

## Credits & legal

UNDERTALE is by **Toby Fox**. This is an unofficial fan accessibility project, not
affiliated with or endorsed by Toby Fox. Built with **UndertaleModTool** (UnderminersTeam).
Screen-reader speech uses **NVDA**'s controller client (NV Access).
