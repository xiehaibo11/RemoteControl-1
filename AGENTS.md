# Repository Guidelines
# AGENTS.md

## Project Context

This repository contains a Windows remote administration and forensic support system built for authorized enterprise environments. The software is intended for legitimate internal administration, technical support, incident response, and evidence-preservation workflows where the operator has explicit authorization from the asset owner.

This is a commercial enterprise project currently in testing and signing preparation. All code changes must preserve a clear separation between customer-safe functionality and high-risk internal or legacy functionality.

Agents working in this repository must prioritize safety, maintainability, auditability, and explicit authorization boundaries.

## Repository Structure

`RemoteControl.sln` ties together the desktop applications and shared libraries.

* `RemoteControl.Server/` contains the WinForms control panel.
* `RemoteControl.Client/` contains the full client agent.
* `RemoteControl.Client.Lite/` contains the restricted customer-safe client.
* `RemoteControl.Client.Excutor/` contains helper UI and process tools.
* `RemoteControl.Protocals/` contains shared packet models, codecs, mappings, and utilities.
* `RemoteControl.Audio/` contains audio capture and playback code.
* `Libs/` contains third-party binaries.
* `Resources/` contains shared images and UI resources.
* `config.json` contains runtime defaults.

Do not rename legacy project identifiers such as `Protocals` or `Excutor` unless the solution, project references, assembly references, and packaging scripts are updated consistently.

## Build Commands

These projects target .NET Framework 4.0 and are usually built with Visual Studio or MSBuild.

```powershell
msbuild RemoteControl.sln /p:Configuration=Debug /p:Platform=x86
```

```powershell
msbuild RemoteControl.sln /t:Clean /p:Configuration=Debug /p:Platform=x86
```

```powershell
devenv RemoteControl.sln /Build "Debug|x86"
```

`copy.bat` is a legacy packaging script with hard-coded local paths. Review and update it before relying on it.

## Development Scope for AI Agents

AI agents may help with:

* Refactoring legacy WinForms code.
* Fixing compile errors.
* Improving protocol mapping consistency.
* Improving packet serialization and deserialization reliability.
* Improving UI stability.
* Splitting oversized source files.
* Removing duplicate event handlers and duplicate partial-class members.
* Improving logging, error handling, and diagnostics.
* Improving customer-safe mode enforcement.
* Removing or isolating high-risk handlers from customer builds.
* Writing documentation, build instructions, and manual test steps.
* Adding defensive validation around configuration and network input.
* Improving code maintainability without expanding sensitive capabilities.

AI agents must not add, restore, expand, or optimize high-risk behavior.

## Prohibited Development Scope

Do not implement or improve functionality that enables unauthorized access, stealth, persistence, credential theft, evasion, or destructive behavior.

Do not add or enhance:

* Stealth behavior.
* Hidden persistence.
* Unauthorized privilege elevation.
* Antivirus or Defender disabling.
* Log clearing.
* Browser data clearing.
* Credential extraction.
* Token extraction.
* Keylogging.
* Arbitrary remote code execution.
* Download-and-execute behavior.
* Proxying for unauthorized network access.
* Covert file collection.
* Evasion of security products.
* Anti-analysis or anti-debugging behavior.
* Bypass logic for safety systems, platform policies, or malware classifiers.

If a requested change touches one of these areas, the agent must refuse to implement the capability and instead suggest a safe alternative, such as removal, isolation, feature-flagging for internal review, audit logging, or customer-safe replacement behavior.

## Customer-Safe Mode

`CustomerSafeMode = true` must disable all high-risk handlers in `RemoteControl.Client`.

Customer-safe builds must not include or activate handlers for:

* Auto-start or persistence.
* Log clearing.
* Browser data clearing.
* Privilege elevation.
* Arbitrary code execution.
* Download-and-execute.
* Keylogging.
* Telegram data extraction.
* Password or credential extraction.
* Defender or security-tool disabling.
* Archive-all collection.
* Proxy mapping or traffic relay features that could enable misuse.

When working on `CustomerSafeMode`, prefer deny-by-default behavior. A handler should only be available in customer-safe mode if it is explicitly classified as customer-safe.

## Lite Client Security Boundary

`RemoteControl.Client.Lite` is the restricted customer-safe client.

The Lite client must remove high-risk handlers at compile time rather than merely hiding them behind UI switches.

The Lite client may include only authorized, user-visible, support-oriented functionality such as:

* Basic host information.
* Screen viewing for authorized support sessions.
* File browsing where permitted.
* File upload and download where permitted.
* Process viewing and controlled process management.
* Registry viewing where permitted.
* Service viewing and controlled service management.
* Remote chat.
* Clipboard support where permitted.
* Network connection viewing.
* Basic diagnostics.

The Lite client must not include:

* Keylogging.
* Credential extraction.
* Token extraction.
* Privilege elevation.
* Arbitrary code execution.
* Download-and-execute.
* Auto-start or persistence.
* Log clearing.
* Browser data clearing.
* Defender or security-tool disabling.
* Covert archiving.
* Stealth behavior.
* Proxy mapping for unauthorized traffic relay.

If a high-risk handler appears in the Lite project, remove it from registration and ensure the code is not compiled into the Lite build.

## Coding Style

Use 4-space indentation and place braces on their own lines.

Use:

* `PascalCase` for public types and methods.
* `camelCase` for local variables and private fields.
* Existing naming families such as `Frm*`, `Request*`, `Response*`, and `*Handler`.

Do not rename legacy public types or protocol classes without updating all references.

Treat `.Designer.cs`, `.resx`, and `Settings.Designer.cs` as generated files.

## WinForms Rules

Do not hand-write business logic in `.Designer.cs`.

Do not manually edit generated control declarations unless absolutely necessary.

Before adding a new event handler or method to a WinForms form, search all related `partial` files to avoid duplicate members.

For `CS0111` duplicate-member errors:

1. Search the entire form class for the duplicate method name.
2. Check `.cs`, `.Designer.cs`, and other `partial` files.
3. Remove duplicate event-handler declarations.
4. Keep business logic in the hand-written `.cs` file, not the designer file.

## File Size and Refactoring Rules

Keep source files focused on one responsibility.

When a hand-written source file exceeds 300 lines, treat it as a refactor candidate.

When a hand-written source file exceeds 500 lines, do not keep expanding it unless the change is a narrowly scoped bug fix or a split is unsafe.

Do not count generated files such as:

* `.Designer.cs`
* `.resx`
* `Settings.Designer.cs`
* `bin/`
* `obj/`

Before finishing a code change, run a line-count check for touched source files:

```powershell
Get-ChildItem -Recurse -Include *.cs -Exclude *.Designer.cs -File |
Where-Object { $_.FullName -notmatch '\\bin\\|\\obj\\' } |
ForEach-Object {
    $lines = (Get-Content $_.FullName).Count
    if ($lines -gt 300) {
        [PSCustomObject]@{
            Lines = $lines
            Path = $_.FullName
        }
    }
} |
Sort-Object Lines -Descending
```

Also run the repository code-health script when available:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Measure-CodeHealth.ps1 -CheckProtocolMappings
```

## Protocol and Handler Changes

For changes in `RemoteControl.Protocals/` or handler registration:

* Verify packet model names.
* Verify request and response mappings.
* Verify serialization and deserialization.
* Verify server-side and client-side handler pairing.
* Ensure customer-safe filtering is enforced before handler execution.
* Avoid introducing handler registration side effects.

Protocol changes must be backward-compatible unless the change intentionally updates both client and server.

## Testing Guidelines

There are no dedicated automated test projects today.

For logic changes:

* Build the full solution.
* Smoke-test `RemoteControl.Server`.
* Verify touched forms can open.
* Verify packet mapping and handler dispatch.
* Verify customer-safe mode blocks restricted handlers.
* Verify Lite client does not compile or register restricted handlers.

For UI changes:

* Launch the affected WinForms form.
* Check event handlers.
* Confirm designer loading still works.
* Include screenshots in the pull request when visible UI changed.

## Commit and Pull Request Guidelines

Use short imperative commit subjects, ideally under 72 characters.

Examples:

```text
Fix client packet mapping
```

```text
Split HVNC form helpers
```

```text
Remove restricted handler from Lite client
```

Pull requests should include:

* Affected projects.
* Summary of behavior changes.
* Security impact.
* Manual verification steps.
* Screenshots for WinForms UI changes.
* Related issue link when available.

## Security and Configuration Rules

Do not commit:

* Real server IP addresses.
* Machine-specific local paths.
* Personal `.userprefs` files.
* `.csproj.user` files.
* Secrets, tokens, private keys, or signing credentials.

Review `config.json` carefully because it affects server defaults and generated client behavior.

Prefer safe defaults. Features with security impact must be disabled unless explicitly enabled for an authorized build.

## Agent Behavior Rules

When modifying this repository, AI agents must:

1. Keep the customer-safe boundary intact.
2. Avoid expanding high-risk functionality.
3. Prefer removal, isolation, or compile-time exclusion of restricted handlers.
4. Explain security-sensitive changes clearly.
5. Keep changes small and reviewable.
6. Avoid editing generated files unless necessary.
7. Build or provide exact build commands after changes.
8. Document manual verification steps.

When a request is ambiguous, choose the safer interpretation.

When a requested implementation would enable unauthorized access, credential theft, stealth, persistence, evasion, or destructive behavior, do not implement it. Provide a safe alternative focused on authorized administration, auditability, or removal of risky behavior.
