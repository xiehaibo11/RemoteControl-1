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

## Testing Guidelines
There are no dedicated test projects in the repository today. For logic changes in `RemoteControl.Protocals/` or handlers, verify the packet mapping, serialization, and the matching client/server handler flow. For UI work, smoke-test launch of `RemoteControl.Server` and any touched forms. Document manual verification steps in the pull request when automated coverage is not added.

## Commit & Pull Request Guidelines
The existing history uses short imperative subjects such as `Create dotnet.yml`. Follow that style: one concise summary per commit, ideally under 72 characters. Pull requests should list affected projects, describe user-visible behavior changes, include manual test steps, and attach screenshots for WinForms UI changes. Link the related issue when one exists.

## Security & Configuration Tips
Do not commit real server IPs, machine-specific batch paths, or updated `.userprefs`/`.csproj.user` files unless the change is intentional. Review `config.json` changes carefully because they affect server defaults and generated client behavior.
