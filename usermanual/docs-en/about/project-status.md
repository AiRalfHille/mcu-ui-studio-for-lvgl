# Project Status

MCU UI Studio for LVGL is already a usable tool for creating, editing, and
generating LVGL user interfaces, and it is still under active development.

The current focus is on a clearly structured editor, a native simulator
preview, and the generation of LVGL C code for the display path.

## Public Release Status

The current public release already provides downloadable desktop packages, but
platform details still matter.

- Windows x64 is available as a direct desktop package.
- macOS ARM64 is currently distributed as an unsigned app bundle.

For the current macOS package:

- unpack the archive
- start the app through `Start MCU UI Studio.command`
- the helper script removes the local quarantine flag from the unpacked app
  bundle and opens the app

On Windows, the handbook currently opens in the external browser because the
embedded WebView path is not stable enough yet.

## What the Tool Is Meant For

MCU UI Studio for LVGL is intended for users who want to:

- design LVGL screens in a structured way
- edit widgets and properties in an editor
- check the result in a simulator
- understand and reuse generated LVGL C code
- work with a clearer link between UI model, preview, and target system

## Current Core of the Project

The current core of the tool includes in particular:

- project and screen management
- a structured widget tree
- a typed property editor
- a native preview and simulator path
- LVGL C code generation
- manual integration inside the editor

## Internal Foundation

Screens are described internally as a JSON-based model.

This model is the shared basis for the editor, simulator, and code
generation. It keeps the structure of a screen understandable and relatively
transparent from a technical point of view.

## Level of Support

The tool already covers a reliable core area, but it does not cover the full
LVGL scope everywhere.

!!! note "Note"
    The level of support is not defined only by whether a widget exists in the
    metamodel. What matters is whether editor, preview, and generation fit
    together in a meaningful way for that widget or property.

In concrete terms:

- not every LVGL widget is fully implemented yet
- not every property is consistently available in all paths
- supported and not-yet-fully-supported areas are marked deliberately in the
  editor

## Basic Principle for Preview and Display

An important project principle is that the simulator preview and the display
path should match as closely as possible from a functional point of view.

What is shown as supported in the simulator should not later lead to
unexpected differences on the target system.

That is why widgets and properties that are not fully supported yet are
clearly marked in the editor.

## Positioning

MCU UI Studio for LVGL is not a theoretical concept. It is a practical tool
with a clear core scope.

At the same time, the project is still being extended and refined. The feature
set is growing step by step without hiding the actual maturity of the current
state in either the editor or the manual.
