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

`flaui-cli` drives Windows desktop applications from the command line through
FlaUI (UIA3). A per-session background daemon holds the target app's UI
Automation tree and assigns short element refs such as `w1e3`. You snapshot the
active window to see the tree, then act on refs through UI Automation patterns
without moving the mouse or stealing focus.

Install on Windows with Winget:

```powershell
winget install kevinboss.flaui-cli
```

Or with Scoop:

```powershell
scoop bucket add maple 'https://github.com/kevinboss/maple.git'
scoop install flaui-cli
```

You can also download `flaui-cli.exe` from the
[releases page](https://github.com/kevinboss/flaui-cli/releases) and put it on
your `PATH`.

Try it on Notepad:

```bash
flaui-cli launch notepad.exe          # launch an app (or use attach)
flaui-cli snapshot                    # list element refs such as w1e3
flaui-cli fill w1e3 "hello" --submit  # fill a text box
flaui-cli click w1e5                  # click an element
flaui-cli get w1e3 value              # read a property
flaui-cli close                       # close the session
```

See [docs/commands.md](docs/commands.md) for all commands (including sessions,
OCR, batching, and the Claude Code skill) and
[docs/in-process-agent.md](docs/in-process-agent.md) for the WPF in-process
agent.

## How to contribute

Building requires Windows, Git, and the .NET 10 SDK. Fork, make your changes,
and open a pull request. See [CONTRIBUTING.md](CONTRIBUTING.md) for build
commands, manual verification, and commit conventions.
