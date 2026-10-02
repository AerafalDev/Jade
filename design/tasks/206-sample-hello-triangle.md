# 206: Sample: hello triangle

- Depends on: 205
- ADRs: 0006

## Goal

`samples/HelloTriangle` opens an SDL3 window, creates a WebGPU surface for it in C#, and renders a
triangle with a WGSL shader through Dawn, on the desktop RIDs. Window resize and quit are handled.

## Context

- Surface creation needs native window handles from SDL window properties (Win32 HWND, X11,
  Wayland, Cocoa or `CAMetalLayer`) and the matching `WGPUSurfaceSource*` chained struct. Do it in
  C# with the generated bindings, not in a native shim (ADR-0003 keeps shims for C++-only
  libraries).
- This sample is the first real user of the bindings. API friction found here is a generator issue:
  report it under Follow-ups, do not patch around it.

## Scope

- The sample project referencing `Jade` from the local feed (or ProjectReference plus local natives,
  whichever matches how users consume Jade; justify).
- Surface creation for Windows, X11, Wayland and macOS. Verify it on Linux (X11 and/or Wayland,
  whichever this machine runs) and state the others as unverified.
- Render loop with surface reconfiguration on resize and a clean shutdown releasing every handle.

## Out of scope

- Mobile and web variants (later tasks), engine abstractions.

## Acceptance criteria

- [ ] On this machine, the window shows the triangle, survives resizing and exits cleanly. The user
      confirms visually: ask them, since the session cannot see the screen.
- [ ] No WebGPU validation errors are logged (with the Dawn error callback wired).
- [ ] The Follow-ups list API friction points with concrete generator changes.

## Verification

Run the sample and ask the user to confirm the output; show the logs.

## Pitfalls

- Wayland vs X11: SDL picks the video driver at runtime. Read the property names for the active
  driver, and handle `SDL_VIDEO_DRIVER` overrides.

## Outcome

_Filled by the task session._

- Summary:
- Verification (commands and results):
- Decisions taken (and ADRs added):
- Deviations from the brief:
- Follow-ups:
