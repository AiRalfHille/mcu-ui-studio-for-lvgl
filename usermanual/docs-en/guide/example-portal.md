# Example: Portal

The `portal` example is based on the `RTOS-Messages` template.

It serves as a reference for how a screen is built in the editor when the
focus is not only on the visual UI itself, but also on structured forwarding
of events and updates.

For the manual, this example is especially interesting because it already
points from the editor side toward MCU integration.

## Connection Between Editor and ESP32 Project

The example currently consists of two matching parts:

- the editor project under `examples/portal`
- the corresponding ESP32 target project under `examples/targets/portal`

This makes the path easy to follow:

- edit the screen in the editor
- generate files
- use those generated files in the ESP32 project
- verify the behavior on the real display

## Role of the Example

`portal` shows two things at the same time:

- the screen structure of a typical overview or portal page
- the layering between generated UI code and handwritten MCU application code

The left side contains input widgets such as buttons and a `slider`. The
middle area shows status and feedback labels. This makes the example useful
for demonstrating both outgoing events and return updates.

## Confirmed RTOS Path in the Example

In the current state, `portal` verifies that:

- the RTOS generator transports widget runtime values separately from free
  event parameters
- a `slider` can send its current value while also carrying an additional free
  parameter such as `WARNING`
- the ESP example processes both pieces of information correctly

In practical terms:

- the slider speed arrives as its own numeric value
- `WARNING` remains available as an additional text parameter

That makes `portal` a reliable reference example for:

- `id` as contract mapping
- `action` as the functional meaning of an event
- `parameter` as a free additional value
- `useUpdate` for return and update targets

## What Belongs to the ESP32 Target

The target project under `examples/targets/portal` demonstrates how the
generated RTOS path is actually integrated:

- display initialization
- theme initialization
- generated contract, event, and update code
- handwritten controller and fieldbus integration

That combination is what makes `portal` the main reference example for the
RTOS-Messages path.

## Theme and Display Relation

In the ESP reference project it is also confirmed that:

- `theme_project.c` is really applied to the display
- `lv_conf_project.h` is considered in the build
- dark mode works in principle on the target system

One important lesson was:

- theme-neutral screens are more robust than screens with hard-coded bright
  default colors

## Color Format

For the current reference system the following applies:

- SDL simulator: `16 bit`
- ESP display: `16 bit`, specifically `RGB565`

That matters because colors can look visibly different on the desktop preview
and on the real display.
