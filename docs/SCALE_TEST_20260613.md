# Scale Test Notes

Date: 2026-06-13

## Goal

Improve responsiveness when thousands of clients are online.

## Changes

| Area | Change |
| --- | --- |
| Relay accept loop | Replaced per-connection blocking thread startup with async accept/session handling. |
| Relay socket settings | Increased listen backlog to `4096`, enabled `NoDelay`, `KeepAlive`, and 64 KB send/receive buffers. |
| Relay session IDs | Increased session ID length from 8 to 16 hex characters to reduce collision risk at larger counts. |
| Relay send path | Added async send with per-session `SemaphoreSlim` to keep packet ordering without blocking unrelated connections. |
| Controller list sync | Added batch client-list synchronization event instead of raising one UI event per client during full list refresh. |
| Controller host lookup | Added dictionaries for `SocketId -> SocketSession` and `SocketId -> TreeNode` to avoid repeated linear scans. |
| Controller TreeView | Wrapped bulk changes in `BeginUpdate` / `EndUpdate` and rebuilt visible nodes only once per batch. |
| Controller log box | Capped runtime log display to 500 lines and stopped rebuilding unbounded text on every message. |

## Verification

| Check | Result |
| --- | --- |
| Relay build | Passed with `dotnet build RemoteControl.Relay\RemoteControl.Relay.csproj --configuration Debug -p:UseAppHost=false -o artifacts\build\relay-scale`. |
| Server build | Passed with solution target `RemoteControl_Server` and separate `OutDir=artifacts\build\server-scale`. Existing runtime output was not overwritten. |
| Synthetic Relay scale test, 1500 clients | Passed. Client list returned `1500/1500`, packet type `202`, connect time `3926 ms`, working set `59.71 MB`, process threads `20`. |
| Synthetic Relay scale test, 3000 clients | Passed. Client list returned `3000/3000`, packet type `202`, connect time `6910 ms`, working set `77.41 MB`, process threads `20`. |

## Notes

- The synthetic test only opens local TCP clients, sends normal Relay handshakes, and requests the online list.
- No remote-control command was sent.
- No client payload, installer, or high-risk module was built or executed by the synthetic scale test.
- The current bottleneck after this change is likely WinForms `TreeView` rendering when many nodes are expanded or filtered. If the target is 10,000+ visible hosts, replacing `TreeView` with a virtual `ListView` or paged host table will be the next major step.
