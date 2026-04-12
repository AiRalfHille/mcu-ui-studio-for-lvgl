# Examples

MCU UI Studio for LVGL contains several example projects that demonstrate the
editor on concrete screens.

These examples are not only meant to list features. They are intended to make
the workflow, the project structure, and the generated output easier to
understand in practice.

## Purpose of the Examples

The examples are mainly intended to:

- make typical screen structures easier to understand
- show different project templates
- place widgets into a realistic context
- make the connection between editor, preview, and generation easier to grasp

## Currently Available Examples

### `portal`

- Project template: `RTOS-Messages`
- Purpose: example of a screen with a more structured UI-to-MCU relationship
- Focus: message and contract-oriented integration
- Editor project: `examples/portal`
- ESP32 target project: `examples/targets/portal`

### `kachel`

- Project template: `Standard`
- Purpose: example of a structured Standard screen
- Focus: classic editor and generation workflow
- Editor project: `examples/kachel`
- ESP32 target project: `examples/targets/kachel`

### `widgets`

- Project template: `Standard`
- Purpose: overview of different widget types in one shared screen
- Focus: widget-oriented exploration and verification
- Editor project: `examples/widgets`
- ESP32 target project: `examples/targets/widgets`

## How to Use the Examples

The examples can be opened directly in the editor and used as a practical
starting point for:

- understanding the project structure
- working with screens
- testing individual widgets or properties
- comparing editor state and generated output
- following the path from an editor project into a corresponding ESP32
  reference project

## Further Expansion

The example pages in this manual are meant to make the link between two levels
visible:

- the editor example under `examples/...`
- the matching ESP32 target project under `examples/targets/...`

The most interesting questions are:

- what an example really teaches a user
- which parts are primarily for orientation
- and how far the example is already connected into a real MCU target project
