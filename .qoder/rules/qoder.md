---
trigger: always_on
---
# Repository Guidelines

## Project Structure & Module Organization
`RemoteControl.sln` ties together the desktop apps and shared libraries. `RemoteControl.Server/` is the WinForms control panel, `RemoteControl.Client/` is the full remote agent, `RemoteControl.Client.Lite/` is the trimmed agent, `RemoteControl.Client.Excutor/` contains helper UI/process tools, `RemoteControl.Protocals/` holds shared packet models, codecs, and utilities, and `RemoteControl.Audio/` contains audio capture/playback code. Third-party binaries live in `Libs/`, shared images in `Resources/`, and runtime defaults in the root `config.json`.

## Build, Test, and Development Commands
These projects target .NET Framework 4.0 and are usually built with Visual Studio or MSBuild.

- `msbuild RemoteControl.sln /p:Configuration=Debug /p:Platform=x86` builds the full solution in the main debug target.
- `msbuild RemoteControl.sln /t:Clean /p:Configuration=Debug /p:Platform=x86` clears build output before a rebuild.
- `devenv RemoteControl.sln /Build "Debug|x86"` is the equivalent Visual Studio build command.
- `copy.bat` is a legacy packaging script with hard-coded local paths; review and update it before relying on it.

## Coding Style & Naming Conventions
Use 4-space indentation and place braces on their own lines, matching the existing C# files. Keep public types and methods in `PascalCase`, local variables and private fields in `camelCase`, and retain the existing naming families such as `Frm*`, `Request*`, `Response*`, and `*Handler`. Do not rename legacy project identifiers such as `Protocals` or `Excutor` unless you are prepared to update solution, assembly, and packaging references. Treat `.Designer.cs`, `.resx`, and `Settings.Designer.cs` as generated files.

For WinForms-specific naming, Designer safety, event-handler naming, and `CS0111` duplicate-member troubleshooting, follow `docs/WINFORMS_TEAM_GUIDELINES.md`. In particular, do not hand-write business logic in `.Designer.cs`, and search all `partial` files before adding a method or event handler.

## Code Management & File Size Limits
Keep source files focused on one responsibility and avoid adding unrelated behavior to large legacy classes. When a hand-written source file grows beyond 300 lines, review whether the code should be split into smaller helpers, services, partial classes, or form-specific components. When a hand-written source file exceeds 500 lines, do not keep expanding it; split the new behavior before merging unless the file is generated or the change is a narrowly scoped bug fix.

Do not count generated files such as `.Designer.cs`, `.resx`, `Settings.Designer.cs`, build output, `bin/`, or `obj/` when enforcing file-size rules. Before finishing a code change, run a line-count check for touched source files, for example:

- `Get-ChildItem -Recurse -Include *.cs -Exclude *.Designer.cs -File | Where-Object { $_.FullName -notmatch '\\bin\\|\\obj\\' } | ForEach-Object { $lines = (Get-Content $_.FullName).Count; if ($lines -gt 300) { [PSCustomObject]@{ Lines=$lines; Path=$_.FullName } } } | Sort-Object Lines -Descending`
- `powershell -ExecutionPolicy Bypass -File tools\Measure-CodeHealth.ps1 -CheckProtocolMappings` runs the repository code-health report and protocol mapping audit.

Treat files over 300 lines as refactor candidates and files over 500 lines as required split candidates. New or touched hand-written files must not remain over 500 lines unless the change documents why a split is unsafe. If a file cannot be split safely in the same change, document the reason and avoid making it larger.

## Testing Guidelines
There are no dedicated test projects in the repository today. For logic changes in `RemoteControl.Protocals/` or handlers, verify the packet mapping, serialization, and the matching client/server handler flow. For UI work, smoke-test launch of `RemoteControl.Server` and any touched forms. Document manual verification steps in the pull request when automated coverage is not added.

## Commit & Pull Request Guidelines
The existing history uses short imperative subjects such as `Create dotnet.yml`. Follow that style: one concise summary per commit, ideally under 72 characters. Pull requests should list affected projects, describe user-visible behavior changes, include manual test steps, and attach screenshots for WinForms UI changes. Link the related issue when one exists.

## Security & Configuration Tips
Do not commit real server IPs, machine-specific batch paths, or updated `.userprefs`/`.csproj.user` files unless the change is intentional. Review `config.json` changes carefully because they affect server defaults and generated client behavior.

## Client Security Classification

- **CustomerSafeMode = true** disables high-risk handlers in `RemoteControl.Client`: auto-start (`RequestAutoRunHandler`), log cleaning (`RequestClearLogHandler`), browser data clearing (`RequestClearBrowserDataHandler`), privilege elevation (`RequestElevatePrivilegeHandler`), arbitrary code execution (`RequestExecCodeHandler`), download-and-execute (`RequestDownloadExecHandler`), keylogging (`RequestKeyloggerHandler`), TG extraction (`RequestTGExtractHandler`), password extraction (`RequestPasswordExtractHandler`), Defender disabling (`RequestDisableDefenderHandler`), archive-all (`RequestArchiveAllHandler`), and proxy mapping (`RequestProxyMappingHandler`).
- **RemoteControl.Client.Lite** completely removes all high-risk handlers at compile time. It registers only 26 core handlers covering: file browsing, screen capture, mouse/keyboard injection, file upload/download, process management, registry viewing, service management, keylogging, window finding, remote chat, clipboard, network connections, and host info. It does NOT include: auto-start, log/browser data clearing, privilege elevation, code execution, download-execute, TG/password extraction, Defender disabling, or archive-all.
