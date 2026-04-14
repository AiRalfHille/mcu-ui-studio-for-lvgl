# MCU UI Studio for LVGL

German version: [README.de.md](README.de.md)

LVGL editor and code generator for MCU projects.

## Release Status

Current public release:

- Windows x64 package is available as a direct desktop download.
- macOS ARM64 package is provided as an unsigned app bundle.

Important macOS note:

- after downloading and unpacking the macOS package, start it through
  `Start MCU UI Studio.command`
- this helper script removes the quarantine flag from the local app bundle and
  opens the app
- the macOS package is currently not notarized yet

Important Windows note:

- on Windows the handbook currently opens in the external browser
- the embedded WebView path is not stable enough yet for the Windows build
- Microsoft Defender SmartScreen may warn when starting this first public
  release because the Windows package is not code-signed yet
- if you trust the release downloaded from this repository, continue through
  `More info` and `Run anyway`

Most LVGL editors generate display code. This one is evolving toward generating
the complete contract between your MCU application and the display:

- generated LVGL screen code
- generated event bindings from UI to application
- generated update functions from application back to UI
- a cleaner separation between UI design and embedded runtime logic

## What It Is

`MCU UI Studio for LVGL` is a desktop editor built with Avalonia and .NET.

The user works through:

- a toolbox
- a structure tree
- typed properties
- event configuration
- generated JSON and C artifacts

The goal is not to hand-edit LVGL C, XML, or JSON, but to model the UI and let
the tool generate the artifacts.

## Current Direction

The project currently supports two broad embedded generation styles:

- `Standard`
  - keeps the existing LVGL-oriented generation path
- `RTOS-Messages`
  - generates a more explicit contract for queue-based MCU applications

For `RTOS-Messages`, the current model is:

- widgets with `id` and `useUpdate: true` become update contract objects
- configured widget events produce typed actions
- the generated contract can be used by controller, machine, and display code
- the MCU developer should not need to write raw LVGL code in normal control logic

## Core Idea

The designer defines:

- screen structure
- widget properties
- outgoing events
- incoming update targets

The embedded developer gets:

- generated C files
- enums and contracts
- update functions
- a cleaner boundary between LVGL and application logic

## Architecture

The editor follows this rough flow:

```text
Metamodel
  -> document model
  -> validation
  -> JSON serialization
  -> C code generation
```

Important principle:

- the JSON model is the primary source
- generated C files are derived artifacts

That matters especially for later domain features and richer contracts.

## Repository Layout

- `Ai.McuUiStudio.slnx`
  - solution entry point for the repository
- `src/Ai.McuUiStudio.Core`
  - metamodel, document model, validation, parsers, generators
- `src/Ai.McuUiStudio.App`
  - Avalonia desktop application
- `src/Ai.McuUiStudio.PreviewHost`
  - preview backend process
- `native/lvgl_simulator_host`
  - native simulator component of the editor preview system
- `usermanual/docs`
  - end-user manual source
- `examples`
  - editor examples and matching target projects
- `platforms`
  - platform-specific notes

## Current Highlights

- LVGL 9.4 metamodel path
- typed property editor
- structured event model in JSON
- project templates in the project dialog
- `ui_start.json` style screen files
- generated `ui_start.c/.h`, `ui_start_event.c`, `ui_start_update.c`
- RTOS contract generation with object enums and action enums
- `useUpdate` flag in the `Data` property group controls which widgets appear in the update contract
- configurable build output directory
- native simulator preview through SDL2 with the configured screen resolution
- structure tree selection highlights the selected widget with a red outline in the simulator

## Version 1 Scope

The current version already supports a useful set of simple widgets and the
main generation paths, but it does not yet model every LVGL widget and every
style detail completely.

This is especially true for more complex widgets and widget parts, for example:

- advanced widget-specific styling
- part-based styling such as slider knob details
- richer domain-specific parameter modeling

In short:

- simple widgets and the main embedded flow are the current focus
- deeper LVGL styling coverage will be expanded step by step

## Preview And Simulator

The project already includes a real native preview direction, not just static
export or textual diagnostics.

The preview system belongs to the editor itself and currently consists of:

- the editor-side preview host
- the native simulator host under `native/lvgl_simulator_host`
- SDL2 for rendering the UI in a desktop window

That means the preview is intended to show the generated screen in the actual
configured display size, instead of only approximating layout in a generic UI.

Important:

- the simulator is not a separate product beside the editor
- it is one backend of the editor preview system
- the C# preview host and the native simulator are part of the same overall preview architecture

At the moment the native simulator is still built as a separate native step.
For a clean multi-platform workflow, the native simulator should no longer use
one shared generic build directory across macOS and Windows.

Recommended simulator build directories:

- `native/lvgl_simulator_host/build-macos`
- `native/lvgl_simulator_host/build-windows`

Recommended platform release staging:

- `platforms/macos-arm64/app-publish`
- `platforms/macos-arm64/simulator`
- `platforms/macos-arm64/release`
- `platforms/windows/app-publish`
- `platforms/windows/simulator`
- `platforms/windows/release`

This keeps macOS and Windows artifacts separate and avoids confusion between
CMake caches, toolchains, and native output files.

### Structure tree highlight

When a widget is selected in the structure tree, the simulator highlights it
with a red outline. The outline is removed when the selection changes to a
widget without an `id` or to no selection at all.

This works only for widgets that have an `id` attribute, since `id` is the
handle used to locate the object in the running LVGL screen.

## RTOS-Messages

The `RTOS-Messages` project template is aimed at queue-based MCU projects.

The important idea is:

- the display does not know the controller
- generated code emits and consumes contract types
- the controller, machine, and fieldbus logic live outside LVGL code

The matching ESP32 reference template is kept separately and is used to validate
this integration style in practice.

### Update contract criteria

A widget appears in the generated `*_update.c` contract when:

- it has an `id` attribute (used as the handle name and enum constant)
- it has `useUpdate` set to `true` in the `Data` property group

Widgets without `id` or with `useUpdate: false` are excluded from the update
tables but can still participate in event bindings.

The `Standard` template uses the same `useUpdate` flag to decide which widgets
generate update target entries in `ui_start_update.c`.

Current tested scope:

- one active generated screen per project is the reliable path
- multi-screen generation is conceptually prepared but not yet treated as fully validated

## Local Run

Simple local start:

```bash
./run.sh
```

Or in VS Code through:

- `Terminal -> Run Task -> build`
- `Terminal -> Run Task -> run app`
- `Run and Debug -> Launch MCU UI Studio for LVGL`

## Build Notes

- .NET 10
- Avalonia UI
- local GUI startup may be limited depending on environment
- shared source tree is fine, but build artifacts should stay platform-specific

Editor build:

```bash
dotnet build src/Ai.McuUiStudio.App/Ai.McuUiStudio.App.csproj
```

Recommended macOS publish output:

```bash
dotnet publish src/Ai.McuUiStudio.App/Ai.McuUiStudio.App.csproj \
  -c Release -r osx-arm64 --self-contained true \
  -o platforms/macos-arm64/app-publish
```

Recommended Windows publish output:

```bash
dotnet publish src/Ai.McuUiStudio.App/Ai.McuUiStudio.App.csproj \
  -c Release -r win-x64 --self-contained true \
  -o platforms/windows/app-publish
```

Recommended native simulator builds:

```bash
cmake -S native/lvgl_simulator_host -B native/lvgl_simulator_host/build-macos -G Ninja
cmake --build native/lvgl_simulator_host/build-macos
```

```bash
cmake -S native/lvgl_simulator_host -B native/lvgl_simulator_host/build-windows -G Ninja
cmake --build native/lvgl_simulator_host/build-windows
```

Recommended release assembly:

- copy the app publish output into the matching `platforms/.../release` folder
- copy the native simulator binary into `platforms/.../simulator`
- assemble the final release ZIP from `platforms/macos-arm64/release` or
  `platforms/windows/release`

Current desktop notes:

- native file and folder dialogs are used again on macOS
  - this was revalidated locally after updating the Avalonia desktop packages
    to `12.0.1`
  - `Avalonia.Controls.WebView` currently remains on `12.0.0`
- the build currently reports a transitive security warning for
  `Tmds.DBus.Protocol 0.90.3`
  - advisory: `GHSA-xrw6-gwf8-vvr9`
  - this is tracked as an explicit cleanup item before the repository is
    considered technically tidied up
- the native simulator is already part of the editor repository and preview architecture
  - however, its native build and platform validation are still a dedicated step

## Documentation

The end-user manual source lives in:

- `usermanual/docs`

Generated manual pages are intended to be published separately, for example via
GitHub Pages or release artifacts.

## Examples

The active example set currently consists of:

- `examples/portal`
  - RTOS-Messages example with `examples/targets/portal`
- `examples/kachel`
  - Standard example with `examples/targets/kachel`
- `examples/widgets`
  - widget gallery with `examples/targets/widgets`

## Why This Matters

The long-term value is not just drawing screens.

The stronger product idea is:

- the UI designer defines what the display can send and receive
- the MCU developer works against a generated C contract
- LVGL becomes an implementation detail instead of application glue code
