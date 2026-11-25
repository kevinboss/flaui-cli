# In-process agent

For WPF apps on .NET 8+, `launch --inproc` injects a small agent into the app as
a .NET startup hook. The agent is embedded in `flaui-cli.exe`, written to disk
on launch, and loaded via `DOTNET_STARTUP_HOOKS` before the app's `Main` runs.
It serves its own named pipe from inside the app and runs every request on the
app's UI thread.

The snapshot then shows the app's real WPF tree with `p` refs instead of the UI
Automation tree. Actions on `p` refs go through automation peers inside the app,
so they need no focus, no mouse, and no visible window, and keep working while
the app is minimized or covered.

## Example

```bash
flaui-cli -s=app launch "C:\path\to\App.exe" --inproc --minimized
flaui-cli -s=app snapshot              # WPF tree: - button "Greet" [ref=p5]
flaui-cli -s=app fill p3 "Kevin"       # set a textbox value
flaui-cli -s=app click p5              # invoke a button / toggle / select
flaui-cli -s=app press Enter p3        # deliver a key straight to that element
flaui-cli -s=app get p6 label          # role|label|name|type|value|enabled|visible
```

## Scope and limits

- Only for apps started with `launch --inproc`; `attach` cannot inject into a
  running process.
- `p` refs work with `snapshot`, `get`, `wait`, `press`, `click`, `dblclick`,
  `fill`, `focus`, and the state actions (`check`, `uncheck`, `toggle`,
  `select`, `expand`, `collapse`).
- `rightclick`, `hover`, `screenshot`, and `ocr` need UI Automation refs
  (`w1e...`).
- `[name=X]` in the snapshot is the element's `x:Name`, the most stable way to
  find a field.
