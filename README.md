# PuffinProof

**Spellcheck for every Windows app. Nothing else.**

PuffinProof is an open-source, local-only alternative to Grammarly that does **one job**: catch spelling mistakes as you type, in any application. The mascot redlines. It does not rewrite.

It will not rewrite your sentences. It will not coach you on tone. It will not send your text anywhere.

## What it does

- Watches what you type in Notepad, browsers, Slack, Word, mail, terminals — anything with a keyboard.
- After you finish a word (space or punctuation), checks it against a bundled [Hunspell](https://hunspell.github.io/) English dictionary.
- If it is misspelled, shows a small popup with numbered suggestions.
- Press `1`–`5` or click to replace the word. Press `Esc` to keep what you typed.

## What it refuses to do

- Grammar or style advice
- Auto-rewrites or “improve this”
- Cloud accounts, telemetry, or network calls
- Reading a document you did not type

## Shortcuts

| Shortcut | Action |
| --- | --- |
| `Ctrl+Alt+S` | Turn spellcheck on or off |
| Click a suggestion | Replace the misspelled word in the focused field |

Add a word from the popup so it is never flagged again. Ignore it once for the rest of the session. Manage the list from the tray icon → **Your words**.

## Privacy

All checking runs on your PC. The dictionary is a local Hunspell word list. Settings and your custom words live in `%AppData%\JustSpell\`. Nothing is uploaded.

Password boxes (Win32 `ES_PASSWORD` / class names containing “Password”) are skipped. Password managers are skipped by default. Use **Pause in this app** from the tray if you want PuffinProof out of a specific program.

PuffinProof cannot see keystrokes in elevated (Administrator) windows unless you also run it as Administrator. Prefer leaving it unelevated.

PuffinProof never sends text off the machine. It does not install a keyboard hook.

## Install it (Windows)

Microsoft only: **MSIX + App Installer**, a small **stub EXE**, and **winget**. There is no WiX and no MSI (Windows has MakeAppx, not MakeMsi).

Needs the **.NET 10 Desktop Runtime (x64)** from Microsoft (~1 MB PuffinProof package).

### Stub EXE (latest from GitHub)

```
dist\JustSpellSetup.exe
```

Checks GitHub Releases for `JustSpell.msix` and installs it only if you are behind. Or pick a local MSIX.

Set `src/JustSpell.Stub/feed.json` or `$env:JUSTSPELL_GITHUB_REPO="PuffinProof/PuffinProof"`.

### MSIX + App Installer

```
dist\JustSpell.msix
dist\JustSpell.appinstaller
```

```powershell
Add-AppxPackage -Path dist\JustSpell.msix -AllowUnsigned
```

### winget

```powershell
winget validate installer\winget
winget install --manifest installer\winget
```

Point `installer/winget/JustSpell.yaml` at a real Release URL first.

### Build

[.NET 10 SDK](https://dotnet.microsoft.com/download) plus Windows SDK `MakeAppx`.

Version is **year.month.day.build** (UTC). Build **restarts at 1 each new UTC day**. After 99 we use 100, 101, … (Windows versions are numbers only — hex letters like `A` are not valid). Tags: `v2026.8.16.1`.

```powershell
.\build.ps1
```

CI tests every push/PR. **Release** workflow (tag `v*` or Run workflow) publishes the EXE stub, MSIX, App Installer, checksums, and attestations.

## Add another language

Drop a Hunspell `xx_YY.dic` + `xx_YY.aff` pair into `src/JustSpell/Dictionaries/` (or next to the published exe, in a `Dictionaries` folder) and pick it in Settings. American English (`en_US`, SCOWL) ships with the app.

## How it works

PuffinProof is a system-tray WPF app. It does **not** install a keyboard hook.

1. Windows UI Automation watches the focused text field (the same family of APIs screen readers use).
2. When a word is finished (space or punctuation), Hunspell checks that word.
3. URLs, emails, digits (optional), password fields, and your personal word list are skipped.
4. A popup offers replacements. Accepting one writes back through the field’s value/text pattern when the host app allows it.

Some apps (notably parts of Chrome) expose little or no text to UI Automation. Those fields cannot be checked or corrected. That is a Windows limitation, not a hook we can “add back.”

It cannot draw a red underline *inside* another program. The popup is the process-safe version of that idea.

## License

- PuffinProof application code: [MIT](LICENSE)
- English word list: [SCOWL / Hunspell en_US](src/JustSpell/Dictionaries/README_en_US.txt)
- Spell engine: [WeCantSpell.Hunspell](https://github.com/aarondandy/WeCantSpell.Hunspell) (Hunspell MPL / LGPL / GPL tri-license)

## Project layout

```
src/JustSpell.Core          spell engine, settings, user dictionary
src/JustSpell               tray app, UI Automation watcher, settings
src/JustSpell.Stub          small EXE that pulls the latest MSIX from GitHub
installer/JustSpell.Msix    MakeAppx layout + App Installer
installer/winget            winget manifest
tests/JustSpell.Tests
```
