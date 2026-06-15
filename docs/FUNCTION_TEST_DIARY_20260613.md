# Function Test Diary Continuation

Date: 2026-06-13

## Scope

- No client binary was started.
- No remote command was sent.
- No system-changing action was executed.
- High-risk features were limited to static inventory only.

## Results

| Check | Result | Evidence / notes |
| --- | --- | --- |
| Code health dry run | Completed with warnings | `tools\Measure-CodeHealth.ps1 -CheckProtocolMappings`; 2 files exceed 500 lines: `RemoteControl.Server\FrmMain.cs`, `RemoteControl.Protocals\Request\RequestKeyboardEvent.cs`. Protocol report found 29 review-only unmapped packet types. |
| Customer requirement coverage dry run | Completed | `tools\Test-CustomerRequirementCoverage.ps1`; 11 implemented, 6 restricted, 1 data-model gap. No client binary was started and no remote command was sent. |
| Customer installer integrity | Passed | `artifacts\releases\RCSetup.exe` SHA256 `F4F36835D902207A14B64EA7452D328CA5C588E10449933777861B581EAAE91C`; matches root `RemoteControl.Client.Installer.exe`. |
| Generated client parameters | Passed | `RemoteControl.Client.Generated.exe` has parameter header and points to `203.91.76.159:10010`, service name `RemoteControlClient.exe`, avatar `16238_100.png`. |
| Customer package high-risk string scan | Passed | `rg -a` found no `DisableDefender`, `PasswordExtract`, `TGExtract`, or `ArchiveAll` strings in `artifacts\releases\RCSetup.exe`, `RemoteControl.Client.Generated.exe`, or the Lite client exe. |
| Lite handler gap | Still open | Lite client still does not register process list, remote registry browse, remote chat, or message-box handlers. Full client has some of these handlers, but the current customer package is Lite. |
| New high-risk code inventory | Execution blocked | Uncommitted full-client/protocol/server wiring was detected for `TGExtract`, `PasswordExtract`, `DisableDefender`, and `ArchiveAll`. These were not executed, not packaged into the Lite customer installer, and not validated as runnable features. |

## Build Note

Full-solution build was intentionally not run in this mixed worktree because the current project files include newly detected high-risk modules. Building the full client at this point could compile those modules into output artifacts.
