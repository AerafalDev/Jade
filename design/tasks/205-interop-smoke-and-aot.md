# 205: Interop smoke tests and NativeAOT check

- Depends on: 202, 203, 204
- ADRs: 0002, 0003, 0006

## Goal

A smoke project exercises every bound library through the packaged `Jade`/`Jade.Native` from the
local feed. It publishes with NativeAOT and trimming with zero warnings, and CI runs it on every
desktop RID it can.

## Context

- 106 created a package consumer test with a hand-written import. This task replaces it with real
  generated bindings and widens it.
- From 201: the bindings use `[LibraryImport]`. Their NativeAOT publish has not been tried yet.
- From 102: on headless CI, Dawn's Null backend answers adapter requests. Use it for the WebGPU
  part of the smoke run, and report whether device creation works on it.

## Scope

- One smoke app per surface, or one app with sections: SDL3 (init, quit, version), WebGPU
  (instance, adapter if available), miniaudio (null-backend engine), and `jade_native_abi_version`.
- NativeAOT publish with `TrimmerSingleWarn=false`, failing on any IL or AOT warning. The resulting
  binary runs.
- CI: run the smoke app on the desktop runners after downloading the matching native artifact.
  Write the job; whether it is green is known only after a push.

## Out of scope

- Mobile and web smoke runs. They follow 104 and 105 as separate tasks.

## Acceptance criteria

- [ ] The smoke app runs from the local feed on linux-x64, both JIT and NativeAOT.
- [ ] Zero trim and AOT warnings, with the publish log line quoted.
- [ ] The CI job is written. The Outcome states plainly that it has not run until pushed.

## Verification

Pack, restore from `artifacts/packages`, run, AOT publish, run the AOT binary.

## Pitfalls

- Stale package cache (see 106).

## Outcome

_Filled by the task session._

- Summary:
- Verification (commands and results):
- Decisions taken (and ADRs added):
- Deviations from the brief:
- Follow-ups:
