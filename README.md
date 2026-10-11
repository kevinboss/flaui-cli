<h1 align="center">flaui-cli</h1>

<p align="center">
  Drive Windows desktop apps from the command line &mdash; Playwright-style, through UI Automation.
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-512BD4?logo=dotnet&logoColor=white" alt=".NET">
  <img src="https://img.shields.io/badge/Windows-0078D6?logo=windows&logoColor=white" alt="Windows">
  <img src="https://img.shields.io/badge/UI%20Automation-FlaUI%20(UIA3)-6E4AFF" alt="FlaUI (UIA3)">
  <img src="https://img.shields.io/badge/license-MIT-blue" alt="License: MIT">
</p>

> This project is being developed using AI development tools. However, every line of code is reviewed and approved by a human before being committed.

A Playwright-style command-line tool for driving Windows desktop applications (Win32, WinForms,
WPF, UWP) through their UI Automation tree. A per-session background daemon holds the app's live
UIA tree and hands out short element refs (`w1e42`); you `snapshot` to see the tree, then act on
refs. Interactions use UIA control patterns first (Invoke, Toggle, SetValue), so most actions do
not move the mouse or steal keyboard focus.

Built on [FlaUI](https://github.com/FlaUI/FlaUI) (UIA3). CLI framework: `Spectre.Console.Cli`.

## Install

Windows only (FlaUI needs a `-windows` target). Publish a self-contained single-file exe:

```
dotnet publish src/flaui-cli/flaui-cli.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o publish
```

Copy `publish/flaui-cli.exe` onto a directory on your PATH.

## Quick start

```
flaui-cli launch notepad.exe     # start an app (spawns the session daemon)
flaui-cli snapshot               # read the tree, note the element refs
flaui-cli fill w1e3 "hello"      # set a textbox value by ref
flaui-cli press Enter
flaui-cli close                  # stop the session
```

## Commands

| Command | Purpose |
|---|---|
| `launch <exe> [-- args] [--inproc] [--minimized]` | Launch an app and attach automation; `--inproc` loads the in-process agent |
| `attach --pid \| --process \| --title` | Attach to a running app |
| `snapshot [ref] [--depth N]` | Snapshot the active window or a subtree |
| `desktop [--depth N]` | Snapshot all top-level windows |
| `windows` / `window-select <ref>` | List the app's windows / switch the active one |
| `get <ref> [property]` | Read an element property (or dump all) |
| `screenshot [ref] [--filename P]` | Capture the window or an element to a PNG |
| `ocr [ref] [--words] [--lang TAG]` | Read on-screen text with screen positions, for UI that UI Automation can't see |
| `click` / `dblclick` / `rightclick` / `hover` / `focus` `<ref>` | Pointer and focus actions (pattern-first) |
| `click-at <x> <y> [--double] [--right]` | Click at screen coordinates, e.g. an `[at=x,y]` from `ocr` |
| `check` / `uncheck` / `toggle` / `select` / `expand` / `collapse` `<ref>` | State actions |
| `fill <ref> <text> [--submit]` | Set a textbox value, optionally press Enter |
| `type <text>` / `press <key> [ref]` | Type into focus / press Enter, Tab, ArrowDown, F5, … (into a given element with a ref) |
| `wait <ref> [--timeout ms]` | Wait until an element is enabled and on-screen |
| `batch [--file P]` | Run a JSON array of commands in one round-trip |
| `list` / `close` / `close-all` / `kill-all` | Session management |
| `skill install` | Install the Claude Code skill to `~/.claude/skills` |

Global options: `-s=<session>` (or `$FLAUI_CLI_SESSION`), `--raw`, `--json`.

## How it works

- One binary, two roles: a thin client, and a per-session daemon spawned detached on first use.
- The daemon owns a single long-lived **MTA** worker thread that makes every UIA call (per
  Microsoft's UI Automation threading guidance) plus a ref registry. Refs are stable for the life
  of the session, deduped by UIA `RuntimeId`.
- Client and daemon exchange one JSON request/response per command over a named pipe. The daemon
  shuts itself down after 10 minutes idle.
- In-process mode (`launch --inproc`, WPF apps on .NET 8+) adds a third piece: an agent assembly,
  embedded in the exe, that the daemon writes to disk and the app loads as a .NET startup hook.
  The agent serves its own pipe from inside the app and runs each request on the app's UI thread.
  The daemon routes `p` refs to it and everything else through UIA as before.
  Actions run through automation peers the agent creates for the target element, so they need no
  real input, focus or visible window.

## Claude Code skill

`flaui-cli skill install` writes `SKILL.md` to `~/.claude/skills/flaui-cli`, so an agent can drive
desktop apps the way the `playwright-cli` skill drives browsers.

## Layout

```
src/flaui-cli        entry point: CLI client + daemon host
src/flaui-cli.Core   engine: UIA worker, session, ref registry, snapshot builder
src/flaui-cli.Agent  in-process agent: startup hook loaded into the app for --inproc
samples/DemoWpfApp   a small WPF app used for verification
skill/SKILL.md       the Claude Code skill (embedded into the exe)
```

## License

MIT. See [LICENSE](LICENSE).
