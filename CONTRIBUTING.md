# Contributing

`flaui-cli` is Windows-only. The main CLI and core library target
`net10.0-windows10.0.19041.0` for FlaUI (UIA3) and Windows OCR, while the
embedded in-process WPF agent targets `net8.0-windows`.

## Prerequisites

- Windows
- [Git](https://git-scm.com/downloads)
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

## Build and publish

```bash
dotnet build src/flaui-cli.slnx
dotnet run --project src/flaui-cli -- <command>
dotnet publish src/flaui-cli/flaui-cli.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o publish
```

Windows only. The publish step produces a single `publish/flaui-cli.exe`; put it
on your `PATH`.

`flaui-cli` ships as a self-contained single-file executable rather than a .NET
global tool because FlaUI requires a `-windows` target framework, which .NET
global tools reject.

## Manual verification

The solution in `src/flaui-cli.slnx` has no automated test project yet. Verify
changes manually against a Windows desktop app such as `notepad.exe` or by
building and launching the sample WPF application:

```bash
dotnet build samples/DemoWpfApp/DemoWpfApp.csproj
```

## Repository layout

- `src/flaui-cli/`: CLI entry point, command definitions, named-pipe daemon
  host and client, and output renderer.
- `src/flaui-cli.Core/`: UI Automation session management via FlaUI (UIA3),
  snapshot builder, element registry, OCR, window capture, and in-process agent
  client and deployer.
- `src/flaui-cli.Agent/`: Embedded .NET 8 WPF startup hook agent served over a
  named pipe.
- `samples/DemoWpfApp/`: Sample WPF application for manual testing.
- `skill/SKILL.md`: Claude Code skill embedded as a resource in `flaui-cli.exe`
  for `flaui-cli skill install`.

## Releases

Publishing a GitHub release triggers `.github/workflows/publish.yaml` on
`windows-latest`. The workflow publishes a self-contained single-file `win-x64`
build stamped with the release tag version and attaches both
`flaui-cli-<tag>-win-x64.zip` and `flaui-cli.exe` to the release.

## Pull requests

Commit messages follow
[Conventional Commits](https://www.conventionalcommits.org/):
`type(scope): summary`, with a lowercase type, an imperative summary, and no
trailing period. Types in use are `feat`, `fix`, `docs`, and `chore` (with
`chore(deps)` for dependency updates), plus `refactor`, `test`, `build`, and
`ci` where they fit. Breaking changes add `!` after the type and a
`BREAKING CHANGE:` footer.

Open a pull request once your changes build cleanly and have been verified
manually.
