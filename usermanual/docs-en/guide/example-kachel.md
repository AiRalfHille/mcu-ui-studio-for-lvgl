# Example: Kachel

The `kachel` example is a Standard project with a clearly structured screen
layout.

It is a good reference for a classic combination of views, buttons, labels,
and standard widgets.

For the manual, `kachel` is especially useful as a readable Standard example.

## Connection Between Editor and ESP32 Project

`kachel` currently consists of two matching parts:

- the editor project under `examples/kachel`
- the corresponding ESP32 target project under `examples/targets/kachel`

This makes it easy to compare, in the Standard path:

- which event and update files the editor generates
- how those files are copied into an ESP32 project
- how the behavior appears afterwards on the display

## Why `kachel` Matters as a Standard Example

`kachel` is especially useful for showing the event path of the Standard
template.

The example demonstrates:

- how several widgets can flow into a shared dispatcher through one
  `eventGroup`
- how `eventType` can distinguish functional subcases inside that dispatcher
- how `action` and a typed `parameter` appear in generated code
- how a value widget such as a slider can provide its current runtime value
  separately

That makes `kachel` a strong reference example for MCU developers who want to
understand not only screen structure, but also what to expect later in the
generated `*_event.c` and `*_event.h` files.

## Role of the ESP32 Target

The target project under `examples/targets/kachel` is intentionally set up to
use the current Standard generator path for real.

It therefore demonstrates practical reuse of:

- `ui_start.c` and `ui_start.h`
- the Standard event path
- the Standard update path

For MCU developers, that makes `kachel` more than an editor example. It is a
traceable reference path from the editor into a real ESP32 project.
