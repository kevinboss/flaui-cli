---
name: flaui-cli
description: Automate and test Windows desktop applications (Win32, WinForms, WPF, UWP) by driving their UI Automation tree.
allowed-tools: Bash(flaui-cli:*)
---

# Desktop automation with flaui-cli

`flaui-cli` drives Windows desktop apps the way `playwright-cli` drives browsers. A per-session
background daemon holds the app's UI Automation tree and hands out short element refs (`w1e42`);
you snapshot to see the tree, then act on refs. Interactions use UIA control patterns first
(Invoke, Toggle, SetValue), so most actions do not move the mouse or steal focus.

## Quick start

```bash
# launch an app (starts the session daemon on first use)
flaui-cli launch notepad.exe
# read the tree and note the element refs
flaui-cli snapshot
# act on refs from the snapshot
flaui-cli fill w1e3 "hello from flaui-cli"
flaui-cli press Enter
# stop the session
flaui-cli close
```

## Commands

### App lifecycle

```bash
flaui-cli launch notepad.exe
flaui-cli launch "C:\path\to\app.exe" -- --some arg
flaui-cli launch "C:\path\to\app.exe" --inproc --minimized   # in-process mode, see below
flaui-cli attach --pid 1234
flaui-cli attach --process notepad
flaui-cli attach --title "Untitled - Notepad"   # regex on the window title
```

### Reading the tree

```bash
flaui-cli snapshot                 # active window
flaui-cli snapshot w1e5            # just that element's subtree
flaui-cli snapshot --depth 4       # limit depth
flaui-cli desktop                  # all top-level windows (overview)
flaui-cli windows                  # the app's top-level windows, with refs
flaui-cli window-select w3         # make another window the active one
flaui-cli get w1e2 value           # read one property; omit it to dump all
                                   # name|value|automationId|controlType|className|helpText|enabled|offscreen|bounds
flaui-cli screenshot               # PNG of the active window
flaui-cli screenshot w1e5 --filename=btn.png
```

### Interacting

```bash
flaui-cli click w1e42             # pattern-first click (Invoke/Toggle/Select, mouse fallback)
flaui-cli dblclick w1e42
flaui-cli rightclick w1e42
flaui-cli hover w1e42
flaui-cli focus w1e2
flaui-cli fill w1e3 "text"        # set a textbox value
flaui-cli fill w1e3 "text" --submit   # set value then press Enter
flaui-cli check w1e7              # check / uncheck / toggle a checkbox or toggle button
flaui-cli uncheck w1e7
flaui-cli toggle w1e7
flaui-cli select w1e9             # select a list item / tab / radio by ref
flaui-cli expand w1e9             # expand / collapse a tree node or combobox
flaui-cli collapse w1e9
flaui-cli type "typed into focus" # send keystrokes to whatever has focus
flaui-cli press Enter             # Enter, Tab, ArrowDown, Escape, Space, F5, …
flaui-cli press Enter w1e9        # focus that element first, then press
flaui-cli wait w1e5               # wait until enabled + on-screen (--timeout ms, default 5000)
```

### In-process mode (WPF apps on .NET 8+)

`launch --inproc` starts the app with a small agent loaded into it (a .NET startup hook).
The snapshot then shows the app's real WPF tree with `p` refs (`p12`) instead of the UI Automation tree.
Use it when the UIA tree hides parts of the app, or when the app must stay in the background:
every `p`-ref action runs inside the app, so it needs no focus, no mouse and no visible window, and works while the app is minimized.

```bash
flaui-cli -s=app launch "C:\path\to\App.exe" --inproc --minimized
flaui-cli -s=app snapshot              # WPF tree: - button "Greet" [ref=p5]
flaui-cli -s=app fill p3 "Kevin"       # set a textbox value
flaui-cli -s=app click p5              # invoke a button / toggle / select
flaui-cli -s=app press Enter p3        # deliver a key straight to that element
flaui-cli -s=app get p6 label          # role|label|name|type|value|enabled|visible
```

- Works only for apps started with `launch --inproc`; `attach` cannot load the agent into a running process.
- `p` refs work with snapshot, get, wait, press, click, dblclick, fill, focus and the state actions; `rightclick`, `hover`, `screenshot` and `ocr` need UIA refs.
- `[name=X]` in the snapshot is the element's `x:Name`, the most stable way to find a field.
- A button that contains other controls (a dashboard tile) is shown with its children; a plain button is one line with its text as the label.

### When the tree can't see it: OCR

Some apps draw parts of their UI without exposing them to UI Automation, or expose a broken tree.
When a region is visible on screen but missing from `snapshot`, read it with OCR and click by coordinates.

```bash
flaui-cli ocr                      # text lines of the active window, each with its screen center
flaui-cli ocr w1e5 --words         # only that element's area, plus a position per word
flaui-cli ocr --lang de-CH         # pick the OCR language (default: user profile languages)
flaui-cli click-at 303 360        # click at screen coordinates taken from an [at=x,y]
flaui-cli click-at 400 300 --double
flaui-cli click-at 400 300 --right
```

Each OCR line looks like `- line "Greet" [at=303,360]`.
OCR renders the window itself, so it works while the window is covered, but text scrolled out of view is not read and OCR can misread characters (`O` for `0`, `l` for `1`).
`click-at` brings the app to the front when the point is covered and refuses to click if the point still shows another app's window.
Prefer refs whenever the snapshot has the element; coordinates break as soon as the layout moves.

### Batch

Run several commands in one round-trip (lower latency). Pass a JSON array of command objects via
`--file` or stdin; it stops at the first failure.

```bash
flaui-cli batch --file steps.json
echo '[{"cmd":"fill","ref":"w1e2","text":"hi"},{"cmd":"click","ref":"w1e5"},{"cmd":"snapshot"}]' | flaui-cli batch
```

### Sessions

```bash
flaui-cli -s=calc launch calc.exe   # named session, isolated from the default
flaui-cli -s=calc snapshot
flaui-cli -s=calc close
flaui-cli list                       # running sessions
flaui-cli close                      # close the default session
flaui-cli close-all                  # close every session
flaui-cli kill-all                   # force-kill daemon processes if one is stuck
```

The session defaults to `default`, or `$FLAUI_CLI_SESSION` if set. Use `-s=<name>` to run several
apps side by side without their refs colliding.

## Output shaping

```bash
flaui-cli --raw snapshot > tree.txt   # snapshot text only, no status header
flaui-cli --json snapshot             # full response as JSON, for piping into other tools
```

## Snapshots and refs

Every command prints a status header plus a snapshot of the active window:

```
### Window
- "Untitled - Notepad" [w1]
- focused: "Text editor" [w1e3]
### Snapshot
- window "Untitled - Notepad" [ref=w1]
  - document "Text editor" [ref=w1e3]
  - menubar "Application" [ref=w1e4]
    - menuitem "File" [ref=w1e5]
```

Each line is `- <role> "<name>" [ref=<id>] [state]`. States include `disabled`, `offscreen`,
`checked`, `selected`, `expanded` / `collapsed`, `readonly`. Textboxes and similar also show
`value="..."`. Refs are stable for the life of the session: the same element keeps the same ref
across snapshots, so you can snapshot, reason, then act. If an action reports a stale ref, the
element changed; run `snapshot` again to refresh.

## Targeting

Use refs from a snapshot. When a ref is reported stale, re-snapshot rather than guessing a new id.
If nothing has focus for `type`, click or fill an element first to give it focus.

## Example: greet in the demo app

```bash
flaui-cli launch DemoWpfApp.exe
flaui-cli snapshot                 # find the InputBox and GreetButton refs
flaui-cli fill w1e2 "Kevin"
flaui-cli click w1e5               # GreetButton
flaui-cli snapshot                 # OutputLabel now shows "Hello, Kevin!"
flaui-cli close
```

## Example: Calculator

```bash
flaui-cli -s=calc launch calc.exe
flaui-cli -s=calc snapshot         # find digit and operator button refs
flaui-cli -s=calc click <7> && flaui-cli -s=calc click <plus> && flaui-cli -s=calc click <3>
flaui-cli -s=calc click <equals>
flaui-cli -s=calc snapshot         # read the result off the display
flaui-cli -s=calc close
```

## Installing this skill

`flaui-cli skill install` writes this file to `~/.claude/skills/flaui-cli/SKILL.md`.

## Notes

- Windows only; built on FlaUI (UIA3). WPF and UWP apps expose the richest trees.
- Keyboard input (`type`, and `fill` on controls without a value pattern) goes to whatever has
  focus and can interrupt other work, so prefer ref-based `click` / `fill` where possible.
- A session daemon shuts itself down after 10 minutes idle.
```
