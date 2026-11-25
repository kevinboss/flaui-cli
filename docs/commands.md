# Command reference

All commands support `--json` for the full response and `--raw` for the result
value only. Pass `-s=<session>` (or set `$FLAUI_CLI_SESSION`) to select a
session; when omitted, the session name defaults to `default`.

The first command starts a detached per-session daemon listening on a named
pipe. The daemon shuts down automatically after 10 minutes idle.

## App lifecycle

```bash
# Launch an app (starts the session daemon on first use)
flaui-cli launch notepad.exe
flaui-cli launch "C:\path\to\app.exe" -- --some arg
flaui-cli launch "C:\path\to\App.exe" --inproc --minimized

# Attach to an app that is already running
flaui-cli attach --pid 1234
flaui-cli attach --process notepad
flaui-cli attach --title "Untitled - Notepad"
```

`attach` requires one of `--pid`, `--process`, or `--title` (a case-insensitive
regular expression matched against top-level window titles). See
[in-process-agent.md](in-process-agent.md) for `--inproc`.

## Inspecting windows and elements

```bash
# Snapshot the active window and note the element refs (w1e3, w1e7, ...)
flaui-cli snapshot
flaui-cli snapshot w1e7 --depth 2

# Snapshot every top-level window on the desktop
flaui-cli desktop
flaui-cli desktop --depth 3

# List the app's windows and switch the active one
flaui-cli windows
flaui-cli window-select w2

# Read an element property, or dump them all
flaui-cli get w1e3 value
flaui-cli get w1e3

# Wait until an element is enabled and on-screen
flaui-cli wait w1e9 --timeout 10000

# Screenshot the window or a single element
flaui-cli screenshot
flaui-cli screenshot w1e7 --filename dialog.png
```

For UI Automation refs (`w...e...`), `get` supports `name`, `value`,
`automationId` (or `id`), `controlType` (or `role`), `className` (or `class`),
`helpText`, `enabled`, `offscreen`, and `bounds`. For in-process refs (`p...`),
`get` supports `role`, `label`, `name`, `type`, `value`, `enabled`, and
`visible`.

## Acting on elements

```bash
# Fill a textbox and submit (uses the Value pattern, no keystrokes)
flaui-cli fill w1e3 "hello" --submit

# Click, check, select, expand (UIA patterns first, so no mouse movement)
flaui-cli click w1e5
flaui-cli check w1e8
flaui-cli uncheck w1e8
flaui-cli toggle w1e8
flaui-cli select w1e12
flaui-cli expand w1e15
flaui-cli collapse w1e15
flaui-cli focus w1e3

# Real pointer input when an app needs it
flaui-cli dblclick w1e20
flaui-cli rightclick w1e20
flaui-cli hover w1e21

# Keyboard: type into focus, or press a key on a given element
flaui-cli type "some text"
flaui-cli press Enter
flaui-cli press F5 w1e3
```

## OCR and coordinate clicks

```bash
# Read text that UI Automation can't see, then click it by position
flaui-cli ocr
flaui-cli ocr --words
flaui-cli ocr w1e5 --lang de-CH
flaui-cli click-at 303 360
flaui-cli click-at 400 300 --double
flaui-cli click-at 400 300 --right
```

`ocr` uses Windows OCR (`Windows.Media.Ocr`) and prints each line (and each word
with `--words`) along with its screen center `[at=x,y]`. `click-at` brings the
target app to the front when covered and refuses to click if another app still
covers the coordinates.

## Batching

```bash
# Several commands in one round-trip
echo '[{"cmd":"fill","ref":"w1e2","text":"hi"},{"cmd":"click","ref":"w1e5"}]' | flaui-cli batch
flaui-cli batch --file steps.json
```

`batch` reads a JSON array of command objects from stdin or `--file`, runs them
in order, and stops at the first failure.

## Sessions

```bash
# Named sessions drive several apps side by side
flaui-cli -s=calc launch calc.exe
flaui-cli -s=calc snapshot
flaui-cli list
flaui-cli close
flaui-cli close-all
flaui-cli kill-all
```

Set `FLAUI_CLI_DEBUG=1` to print named-pipe enumeration errors from `list` to
stderr.

## Output shaping

```bash
# Scriptable output
flaui-cli --raw snapshot > tree.txt
flaui-cli --json get w1e3
```

## Claude Code skill

```bash
# Install the flaui-cli skill into Claude Code
flaui-cli skill install
flaui-cli skill install --force
```

`skill install` writes the bundled skill to
`~/.claude/skills/flaui-cli/SKILL.md` and refuses to overwrite an existing file
unless `--force` (`-f`) is given.
