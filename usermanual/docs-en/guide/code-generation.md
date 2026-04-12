# Code Generation

This chapter describes which artifacts MCU UI Studio for LVGL generates and
how the generation process is structured.

Code generation is not an external afterthought. It is a central part of the
tool and translates the internal screen model into files that can be reused in
the target project.

## Starting Point of Generation

Generation always starts from the editor's internal screen model.

A screen is not captured from a visual surface, but generated from its
structured description, including:

- element types
- attributes
- events
- child elements

This means generation is based on the same information that also drives the
editor and the simulator.

## Goal of Generation

The goal is to derive reusable source files for the target project from the
edited screen.

Depending on the selected project template, generation may produce:

- direct LVGL C code
- display-side initialization and layout files
- in `RTOS-Messages` mode, additional contract, event, and update files

## Generation in Standard Mode

In Standard mode the focus is on direct LVGL C code and straightforward use on
the display side.

Typical artifacts are:

- a header file
- a C file with initialization and layout creation

This path typically includes:

- exported object handles
- an initialization function
- creation of the screen hierarchy
- setter calls for supported properties
- screen loading

The generated code stays close to real LVGL calls and is intended to make the
connection between editor state and output code transparent.

The current Standard path already includes a structured event binding model for
MCU projects:

- one dispatcher function per `eventGroup`
- additional differentiation through `eventType`
- an `action` as the primary functional meaning
- a typed free event `parameter`
- a typed runtime value for widgets such as `slider`, `bar`, `arc`, or
  `spinbox`

The technical LVGL trigger such as `clicked` or `released` still matters for
callback registration, but it is no longer the main field for MCU-side logic.
For the application side, `action`, `parameter`, `eventGroup`, `eventType`,
and optional runtime values are the relevant pieces.

## Generation in RTOS-Messages Mode

In `RTOS-Messages` mode generation goes beyond plain screen construction.

In addition to display code, it creates a contract layer between the UI and
controller logic.

Current artifacts include:

- a contract header
- an event source for outgoing UI events
- an update source for incoming UI updates

This path transfers structured information about:

- objects
- actions
- parameter types
- event bindings

into generated code.

An important point in the current state is the distinction between:

- a free event `parameter`
- the actual runtime value of a value widget

For example, a `slider` can therefore:

- send an additional functional parameter such as `WARNING`
- and also transport its current numeric value separately

The same basic separation now also exists in the Standard path.

## Typical Artifacts

Depending on template and target path, generation can produce:

- `*.h`
- `*.c`
- contract files
- event files
- update files

The project scaffolding can also provide or update:

- `screens/`
- `build/`
- `lv_conf`
- theme files

So generation does not only work at screen level, but in the broader context
of the project structure.

## `lv_conf` and Theme in the Target Project

For MCU projects, not only screen files matter. The integration of `lv_conf`
and theme files into the target system is also important.

In the current ESP reference path the following is verified:

- `lv_conf` is generated and actually included in the build
- theme code is generated and applied at runtime

The confirmed ESP path currently works like this:

- `sdkconfig` provides the base LVGL configuration
- `lv_conf_project.h` acts as a project-specific overlay
- `theme_project.c` provides the actual initialized LVGL theme

Runtime logging on the ESP target confirmed that values such as the following
really arrive in the build:

- `LV_COLOR_DEPTH=16`
- `LV_USE_LOG=0`
- `LV_LOG_LEVEL=5`
- `LV_USE_OBJ_NAME=0`
- `LV_THEME_DEFAULT_DARK=0`

It is also confirmed that:

- theme changes in `theme_project.c` visibly affect the ESP display
- dark mode works in principle
- theme-neutral screens are the more robust basis than hard-coded bright
  default colors

!!! note "Note"
    Which files are actually produced depends on the project template and on
    the current support level of the widgets and properties used in the
    screen.

## What the Generator Handles

The generator mainly handles:

- translating the screen model into code
- generating the object hierarchy
- applying supported properties
- naming and exporting relevant handles
- in the RTOS path, deriving events, actions, and update entry points

## What the Generator Does Not Replace

Code generation does not replace the entire target application.

It provides the UI-related part and, depending on the template, additional
contract and messaging structures. The actual MCU application, controller
logic, task structure, and system integration remain part of the target
project.

That is also why the target examples are valuable: they show how the generated
code is integrated into a real MCU application.

!!! warning "Attention"
    Code generation does not replace the complete target application. MCU
    logic, task structure, integration code, and functional behavior remain
    part of the target project.

## Level of Support

Whether a widget or property can actually be generated depends on the current
support level of the tool.

Not every widget that exists in the metamodel is already fully implemented in
every generator path. That is why the editor's visible support markings matter
for code generation as well.

## Summary

Code generation in MCU UI Studio for LVGL is intended to make the path from
screen model to usable source code transparent.

Depending on the selected template, it ranges from:

- direct LVGL C code
- to a structured contract and messaging layer

In that way it forms the bridge between editor, simulator, and target
project.
