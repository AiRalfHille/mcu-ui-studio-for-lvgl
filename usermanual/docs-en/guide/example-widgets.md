# Example: Widgets

The `widgets` example serves as a compact shared screen for different widget
types.

It is especially useful for:

- seeing supported widgets together in one place
- trying out individual widget types quickly
- comparing preview behavior and property editing

For the manual, this example mainly complements the **Widgets** chapter.

## Connection Between Editor and ESP32 Project

`widgets` also belongs to a simple ESP32 target project:

- editor project: `examples/widgets`
- ESP32 target project: `examples/targets/widgets`

Unlike `portal` or `kachel`, this example is intentionally kept very simple.
It is not meant to demonstrate event or update paths, but to make a shared
widget screen usable as a display project.

## What This Example Intentionally Does Not Show

`widgets` is not an event demo and not an MCU contract example. That is why it
deliberately contains:

- no functional `m1` structure
- no event files
- no update files
- no additional runtime path in the target project

That keeps the corresponding ESP32 target very lean and makes it a good
project for checking the pure screen layout on real hardware.
