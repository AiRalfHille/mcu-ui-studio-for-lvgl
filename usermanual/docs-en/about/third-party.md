# Third-Party Components

MCU UI Studio for LVGL uses several third-party components for the editor,
preview, simulator, web view, and documentation.

This page describes the current state as clearly as possible and distinguishes
between:

- directly referenced components
- transitive runtime components
- platform-side system components
- the separate documentation toolchain

!!! note "Basis of this overview"
    The statements on directly referenced packages and visible runtime
    components are based on the current repository state, especially the
    `.csproj` files, `project.assets.json`, the native simulator
    configuration, and the MkDocs setup.

## Directly Referenced Components

These components are directly visible in the editor, the PreviewHost, or the
native simulator.

### LVGL

- Usage: directly in the native simulator and as the target library of the
  generated C code
- Role: central embedded graphics library that the editor, metamodel,
  generators, and simulator align with
- Project version: `9.4`
- License: `MIT`
- Project: [https://lvgl.io](https://lvgl.io)

### SDL2

- Integration: directly in the native simulator path
- Role: windowing, input, and rendering infrastructure for the desktop
  simulator
- Project version: framework- or system-dependent
- License: `zlib`
- Project: [https://libsdl.org](https://libsdl.org)

### Avalonia

- Integration: direct
- Role: desktop UI framework of the editor and PreviewHost
- Project version: `12.0.0`
- License: `MIT`
- Project: [https://avaloniaui.net](https://avaloniaui.net)

### Avalonia.Desktop

- Integration: direct
- Role: desktop backend for windowing, input, and application startup
- Project version: `12.0.0`
- License: `MIT`

### Avalonia.Themes.Fluent

- Integration: direct
- Role: default editor theme
- Project version: `12.0.0`
- License: `MIT`

### Avalonia.Fonts.Inter

- Integration: direct
- Role: default font package in the Avalonia UI stack
- Project version: `12.0.0`
- License: `MIT`

### Avalonia.Controls.WebView

- Integration: direct
- Role: embedded manual display inside the editor
- Project version: `12.0.0`
- License: `MIT`

## Runtime Platform

The application runs on .NET. While .NET is not the UI framework itself, it is
the core runtime platform for the application and its generators.

### .NET

- Integration: runtime platform
- Role: development and execution foundation of the application
- Project version: `.NET 10`
- License: `MIT`
- Project: [https://dotnet.microsoft.com](https://dotnet.microsoft.com)

## Transitive Runtime Components

These components are not the main advertised building blocks of the editor,
but they are clearly part of the package stack and should therefore be named.

### SkiaSharp

- Integration: transitive
- Role: 2D rendering in the desktop UI stack
- Project version: `3.119.3-preview.1.1`
- License: `MIT`
- Project: [https://github.com/mono/SkiaSharp](https://github.com/mono/SkiaSharp)

Additional platform-specific native assets are carried for macOS, Windows, and
Linux.

### HarfBuzzSharp

- Integration: transitive
- Role: text layout and font shaping
- Project version: `8.3.1.3`
- License: `MIT`
- Project: [https://github.com/mono/SkiaSharp](https://github.com/mono/SkiaSharp)

### MicroCom.Runtime

- Integration: transitive
- Role: interop runtime component in the Avalonia environment
- Project version: `0.11.4`
- License: `MIT`
- Project: [https://github.com/kekekeks/MicroCom](https://github.com/kekekeks/MicroCom)

### Tmds.DBus.Protocol

- Integration: transitive
- Role: platform-specific desktop integration, especially on Linux
- Project version: `0.90.3`
- License: `MIT`
- Project: [https://github.com/tmds/Tmds.DBus](https://github.com/tmds/Tmds.DBus)

## Documentation Toolchain

The manual itself is built as static documentation and is not generated inside
the editor application.

### MkDocs

- Integration: documentation toolchain
- Role: static documentation generator
- Project version: not pinned in the repository at the moment
- License: `BSD-2-Clause`
- Project: [https://www.mkdocs.org](https://www.mkdocs.org)

### Material for MkDocs

- Integration: documentation toolchain
- Role: theme, navigation, and visual structure of the manual
- Project version: not pinned in the repository at the moment
- License: `MIT`
- Project: [https://squidfunk.github.io/mkdocs-material](https://squidfunk.github.io/mkdocs-material)

### pymdown-extensions

- Integration: documentation toolchain
- Role: Markdown extensions for notes, tabs, code blocks, and other
  documentation features
- Project version: not pinned in the repository at the moment
- License: `MIT`
- Project: [https://facelessuser.github.io/pymdown-extensions](https://facelessuser.github.io/pymdown-extensions)

!!! note "Documentation toolchain versions"
    The MkDocs configuration shows which components are used for the manual.
    The exact Python package versions are not currently pinned in the
    repository through a dedicated `requirements.txt` or similar lock file.

## Platform-Side System Components

Some features also rely on native system components of the respective desktop
platform. These are functionally relevant, but they are not shipped as normal
repository packages.

### WKWebView

- Integration: system-side on macOS
- Role: native web view engine for manual display
- Origin: Apple WebKit / macOS system frameworks

### WebView2

- Integration: system-side on Windows
- Role: native web view engine for manual display
- Origin: Microsoft Edge WebView2 Runtime

## Maintenance Note

This page should be updated especially when one of the following changes:

- larger version jumps of Avalonia, LVGL, or .NET
- changes in the web view stack
- changes in the simulator or documentation toolchain
- newly added directly referenced NuGet packages or native libraries
