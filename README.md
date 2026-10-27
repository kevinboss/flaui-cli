<h1 align="center">flaui-cli</h1>

<p align="center">
  Drive Windows desktop apps from the command line &mdash; Playwright-style, through UI Automation.
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-512BD4?logo=dotnet&logoColor=white" alt=".NET">
  <img src="https://img.shields.io/badge/UI%20Automation-FlaUI%20(UIA3)-6E4AFF" alt="FlaUI (UIA3)">
  <a href="https://github.com/kevinboss/maple"><img src="https://img.shields.io/scoop/v/flaui-cli?bucket=https://github.com/kevinboss/maple" alt="Scoop"></a>
  <a href="https://github.com/microsoft/winget-pkgs/tree/master/manifests/k/kevinboss/flaui-cli"><img src="https://img.shields.io/badge/winget-kevinboss.flaui--cli-0078D4?logo=windows&logoColor=white" alt="winget"></a>
  <a href="https://github.com/kevinboss/flaui-cli/actions/workflows/publish.yaml"><img src="https://img.shields.io/github/actions/workflow/status/kevinboss/flaui-cli/publish.yaml?label=CI" alt="CI"></a>
  <a href="LICENSE"><img src="https://img.shields.io/github/license/kevinboss/flaui-cli" alt="License"></a>
  <a href="https://github.com/kevinboss/heartbeat"><img src="https://raw.githubusercontent.com/kevinboss/heartbeat/main/badges/kevinboss_flaui-cli.svg" alt="Heartbeat"></a>
</p>

> This project is being developed using AI development tools. However, every line of code is reviewed and approved by a human before being committed.

## Install

**Scoop**

```powershell
scoop bucket add maple 'https://github.com/kevinboss/maple.git'
scoop install flaui-cli
```

**Winget**

```powershell
winget install kevinboss.flaui-cli
```

**Manual**

Grab `flaui-cli.exe` from the [releases page](https://github.com/kevinboss/flaui-cli/releases)
and put it on your `PATH`.

## Usage

```bash
# Launch an app (starts the session daemon on first use)
flaui-cli launch notepad.exe
flaui-cli launch "C:\path\to\app.exe" -- --some arg

# Or attach to one that is already running
flaui-cli attach --process notepad
flaui-cli attach --title "Untitled - Notepad"

# Snapshot the active window and note the element refs (w1e3, w1e7, …)
flaui-cli snapshot
flaui-cli snapshot w1e7 --depth 2

# Snapshot every top-level window on the desktop
flaui-cli desktop

# List the app's windows and switch the active one
flaui-cli windows
flaui-cli window-select w2

# Read an element property, or dump them all
flaui-cli get w1e3 value
flaui-cli get w1e3

# Fill a textbox and submit (uses the Value pattern, no keystrokes)
flaui-cli fill w1e3 "hello" --submit

# Click, check, select, expand (UIA patterns first, so no mouse movement)
flaui-cli click w1e5
flaui-cli check w1e8
flaui-cli select w1e12
flaui-cli expand w1e15

# Real pointer input when an app needs it
flaui-cli dblclick w1e20
flaui-cli rightclick w1e20
flaui-cli hover w1e21

# Keyboard: type into focus, or press a key on a given element
flaui-cli type "some text"
flaui-cli press Enter
flaui-cli press F5 w1e3

# Wait until an element is enabled and on-screen
flaui-cli wait w1e9 --timeout 10000

# Screenshot the window or a single element
flaui-cli screenshot
flaui-cli screenshot w1e7 --filename dialog.png

# Read text that UI Automation can't see, then click it by position
flaui-cli ocr --words
flaui-cli click-at 303 360

# Inject the in-process agent into a WPF app (see below)
flaui-cli launch "C:\path\to\App.exe" --inproc --minimized

# Several commands in one round-trip
echo '[{"cmd":"fill","ref":"w1e2","text":"hi"},{"cmd":"click","ref":"w1e5"}]' | flaui-cli batch

# Named sessions drive several apps side by side
flaui-cli -s=calc launch calc.exe
flaui-cli -s=calc snapshot
flaui-cli list
flaui-cli close
flaui-cli close-all

# Install the flaui-cli skill into Claude Code
flaui-cli skill install

# Scriptable output
flaui-cli --raw snapshot > tree.txt
flaui-cli --json get w1e3
```

All commands support `--json` for the full response and `--raw` for the result value only.
`-s=<session>` (or `$FLAUI_CLI_SESSION`) picks the session.

## In-process agent

For WPF apps on .NET 8+, `launch --inproc` injects a small agent into the app as a .NET startup
hook. The agent is embedded in `flaui-cli.exe`, written to disk on launch and loaded via
`DOTNET_STARTUP_HOOKS` before the app's `Main` runs. It serves its own named pipe from inside the
app and runs every request on the app's UI thread.

The snapshot then shows the app's real WPF tree with `p` refs instead of the UI Automation tree.
Actions on `p` refs go through automation peers inside the app, so they need no focus, no mouse
and no visible window, and keep working while the app is minimized or covered.

```bash
flaui-cli -s=app launch "C:\path\to\App.exe" --inproc --minimized
flaui-cli -s=app snapshot              # WPF tree: - button "Greet" [ref=p5]
flaui-cli -s=app fill p3 "Kevin"       # set a textbox value
flaui-cli -s=app click p5              # invoke a button / toggle / select
flaui-cli -s=app press Enter p3        # deliver a key straight to that element
flaui-cli -s=app get p6 label          # role|label|name|type|value|enabled|visible
```

- Only for apps started with `launch --inproc`; `attach` cannot inject into a running process.
- `p` refs work with snapshot, get, wait, press, click, dblclick, fill, focus and the state actions.
  `rightclick`, `hover`, `screenshot` and `ocr` need UI Automation refs (`w1e…`).
- `[name=X]` in the snapshot is the element's `x:Name`, the most stable way to find a field.

## Build

```bash
dotnet build src/flaui-cli.slnx
dotnet run --project src/flaui-cli -- <command>
dotnet publish src/flaui-cli/flaui-cli.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o publish
```

Windows only. The publish step produces a single `publish/flaui-cli.exe`; put it on your PATH.

## License

[MIT](LICENSE)
