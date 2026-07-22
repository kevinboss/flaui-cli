# flaui-cli

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
| `launch <exe> [-- args]` | Launch an app and attach automation |
| `attach --pid \| --process \| --title` | Attach to a running app |
| `snapshot [ref] [--depth N]` | Snapshot the active window or a subtree |
| `desktop [--depth N]` | Snapshot all top-level windows |
| `windows` / `window-select <ref>` | List the app's windows / switch the active one |
| `get <ref> [property]` | Read an element property (or dump all) |
| `screenshot [ref] [--filename P]` | Capture the window or an element to a PNG |
| `click` / `dblclick` / `rightclick` / `hover` / `focus` `<ref>` | Pointer and focus actions (pattern-first) |
| `check` / `uncheck` / `toggle` / `select` / `expand` / `collapse` `<ref>` | State actions |
| `fill <ref> <text> [--submit]` | Set a textbox value, optionally press Enter |
| `type <text>` / `press <key>` | Type into focus / press Enter, Tab, ArrowDown, F5, … |
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

## Claude Code skill

`flaui-cli skill install` writes `SKILL.md` to `~/.claude/skills/flaui-cli`, so an agent can drive
desktop apps the way the `playwright-cli` skill drives browsers.

## Layout

```
src/flaui-cli        entry point: CLI client + daemon host
src/flaui-cli.Core   engine: UIA worker, session, ref registry, snapshot builder
samples/DemoWpfApp   a small WPF app used for verification
skill/SKILL.md       the Claude Code skill (embedded into the exe)
```

## License

MIT. See [LICENSE](LICENSE).
