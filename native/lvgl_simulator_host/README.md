# LVGL Simulator Host

Dies ist das native C-Teilprojekt fuer den nativen Simulator des Editors.

## Ziel

Der Host soll spaeter:

- das vom Editor erzeugte XML ueber TCP entgegennehmen
- ein offenes Simulatorfenster behalten
- bei `render` oder `reload` die UI neu aufbauen
- Log- und Event-Ausgaben nach `stdout` oder `stderr` schreiben

## Aktueller Stand

Der aktuelle Stand ist ein lauffaehiger TCP-Stub in C mit getrennter
Architektur fuer:

- Prozessstart in `main.c`
- TCP/JSON-Protokoll in `lvgl_simulator_server.c`
- Simulator-Runtime in `lvgl_simulator_runtime_stub.c`
- vorbereitete LVGL-Runtime in `lvgl_simulator_runtime_lvgl.c`
- getrennte LVGL-Runtime-Teile fuer Plattform und Screenaufbau

Der Host nimmt bereits das gemeinsame Protokoll an:

- `render`
- `reload`
- `shutdown`

Er rendert im Standard-Stubbetrieb noch nicht mit LVGL, bestaetigt aber
Anfragen und schreibt Logs. Damit ist die Schichtentrennung bereits so
vorbereitet, dass spaeter nur die Runtime ausgetauscht oder erweitert werden
muss.

## Build

### macOS / Linux

```bash
cmake -S native/lvgl_simulator_host -B native/lvgl_simulator_host/build-macos -G Ninja
cmake --build native/lvgl_simulator_host/build-macos
```

Voraussetzung:

- CMake
- Ninja
- C-Compiler
  - Xcode CLT auf macOS
  - gcc oder clang auf Linux

### Windows

#### Toolchain-Entscheidung

**Genau eine Welt verwenden: MSYS2 MinGW64.**

Nicht mischen:

- kein MSVC (`cl.exe`) zusammen mit MinGW-Headern
- kein Windows SDK (`kernel32.lib` etc.) zusammen mit GCC/Clang aus MSYS2
- keine gemischten Shells wie Developer Command Prompt plus Git Bash

Der normale LLVM-/System-Clang mit Target `x86_64-pc-windows-msvc` erwartet
MSVC- und Windows-SDK-Bibliotheken. Das fuehrt ohne vollstaendige MSVC/SDK-
Umgebung in eine andere Toolchain-Welt und sollte fuer dieses Projekt nicht
verwendet werden.

#### Voraussetzungen

MSYS2 von [msys2.org](https://www.msys2.org/) installieren, dann in der
**MSYS2 MinGW64 Shell** (`mingw64.exe`) die benoetigten Pakete installieren:

```bash
pacman -S \
  mingw-w64-x86_64-gcc \
  mingw-w64-x86_64-cmake \
  mingw-w64-x86_64-ninja
```

Alternativ zu `gcc` ist auch `mingw-w64-x86_64-clang` moeglich.

#### Konfigurieren und Bauen

Bevorzugt direkt aus der **MSYS2 MinGW64 Shell** arbeiten.

Falls aus einer anderen Shell gearbeitet wird, muss zuerst der MinGW64-Pfad
gesetzt werden:

```bash
export PATH="/c/msys64/mingw64/bin:$PATH"
```

Dann:

```bash
cmake -S native/lvgl_simulator_host \
      -B native/lvgl_simulator_host/build-windows \
      -G Ninja \
      -DCMAKE_C_COMPILER="C:/msys64/mingw64/bin/gcc.exe" \
      -DCMAKE_MAKE_PROGRAM="C:/msys64/mingw64/bin/ninja.exe"

cmake --build native/lvgl_simulator_host/build-windows
```

#### Ergebnis

```text
native/lvgl_simulator_host/build-windows/lvgl_simulator_host.exe
```

Das Ergebnis ist eine Windows-Executable. Laufzeitabhaengigkeiten haengen von
der verwendeten Toolchain und Link-Art ab.

#### Aktueller Scope unter Windows

Aktuell verifiziert:

- Host-Build im **Stub-Betrieb**
- Standardwert: `LVGL_SIMULATOR_WITH_RUNTIME=OFF`
- TCP-Protokoll funktioniert
- es wird noch kein echtes LVGL-UI gerendert

Noch nicht end-to-end validiert:

- vollstaendige LVGL/SDL2-Runtime unter Windows

Fuer diesen Pfad wird zusaetzlich mindestens SDL2 benoetigt:

```bash
pacman -S mingw-w64-x86_64-SDL2
```

#### Bekannte Stolpersteine

**MinGW-Toolchain nicht vollstaendig im PATH**

Wenn `C:/msys64/mingw64/bin/gcc.exe` direkt verwendet wird, aber
`C:/msys64/mingw64/bin` nicht im PATH liegt, kann der interne Compiler-Pass
`cc1.exe` nicht gefunden werden. CMake meldet dann oft nur einen defekten
Compiler. Loesung: PATH wie oben beschrieben setzen.

**POSIX-Socket-Header nicht in MinGW64 vorhanden**

MinGW64 kennt `<arpa/inet.h>`, `<sys/socket.h>`, `<netinet/in.h>` und
`<unistd.h>` nicht als POSIX-Welt. Der Host wurde deshalb fuer Windows auf
`<winsock2.h>` und `<ws2tcpip.h>` hinter `#ifdef _WIN32` portiert.

**`setsockopt`-Signatur unter Windows**

Winsock erwartet fuer den 4. Parameter von `setsockopt` einen `const char *`.
POSIX erwartet `const void *`. Unter Windows ist deshalb der Cast
`(const char *)&reuse` erforderlich.

**`uint16_t` ohne `<stdint.h>`**

`uint16_t` ist nicht automatisch verfuegbar, wenn nur Winsock-Header
eingebunden sind. `#include <stdint.h>` muss explizit vorhanden sein.

#### Verifiziert am

- Windows 11
- MSYS2 MinGW64
- CMake
- Ninja

### LVGL/SDL2-Runtime (alle Plattformen)

```bash
cmake -S native/lvgl_simulator_host -B native/lvgl_simulator_host/build-macos \
  -DLVGL_SIMULATOR_WITH_RUNTIME=ON
cmake --build native/lvgl_simulator_host/build-macos
```

Mit expliziten Pfaden fuer SDL2/LVGL bei Bedarf:

```bash
cmake -S native/lvgl_simulator_host -B native/lvgl_simulator_host/build-macos \
  -DLVGL_SIMULATOR_WITH_RUNTIME=ON \
  -DLVGL_SIMULATOR_SDL2_INCLUDE_DIR=/path/to/SDL2/include \
  -DLVGL_SIMULATOR_SDL2_LIBRARY=/path/to/libSDL2.a \
  -DLVGL_SIMULATOR_LVGL_INCLUDE_DIR=/path/to/lvgl
cmake --build native/lvgl_simulator_host/build-macos
```

Die SDL2-Erkennung versucht in dieser Reihenfolge:

- explizite CMake-Pfade
- `find_package(SDL2)`
- manueller Bibliotheks-/Include-Fallback
- auf macOS zuletzt das mitgelieferte `third_party/SDL2.framework`

LVGL wird standardmaessig aus `third_party/lvgl-9.4` verwendet.

Wichtig:

- das native Host-Projekt besitzt eine eigene `lv_conf.h`
- das temporaer kopierte Projekt `foerderbaender` wird fuer den nativen Build
  nicht mehr benoetigt

## Start

### macOS / Linux

```bash
./native/lvgl_simulator_host/build-macos/lvgl_simulator_host --port 40123
```

### Windows

```bash
native/lvgl_simulator_host/build-windows/lvgl_simulator_host.exe --port 40123
```

## Build Directory Convention

To avoid cross-platform confusion, this project should no longer use one
shared generic `build` directory for all desktop platforms.

Recommended convention:

- `build-macos` for macOS builds
- `build-windows` for Windows builds

This is especially important on shared source trees, where CMake caches,
generator files, and native toolchain outputs from macOS and Windows would
otherwise overwrite or confuse each other.

## Naechste Schritte

1. LVGL- und SDL2-Abhaengigkeiten in `CMakeLists.txt` weiter absichern
2. in `lvgl_simulator_runtime_lvgl_platform.c` die echte SDL2-/LVGL-Initialisierung stabilisieren
3. in `lvgl_simulator_runtime_lvgl_screen.c` den echten XML-Ladepfad verdrahten
4. ersten realen LVGL-Demo-Screen erzeugen und laden
5. Render-Loop und Event-Verarbeitung von der Server-Schicht trennen
6. Event-Callbacks und Fehler als Logs ausgeben

## Zielarchitektur

- `main.c`
  Einstiegspunkt und Lebensdauer der Anwendung
- `lvgl_simulator_server.c`
  TCP-Transport und Protokollauswertung
- `lvgl_simulator_runtime_*.c`
  eigentliche Simulator-Implementierung

Die spaetere LVGL-/SDL2-Integration soll nur in der Runtime-Schicht passieren.
Der Editor und das IPC-Protokoll bleiben davon unberuehrt.

## Herkunft der ersten nativen Quellen

Die ersten LVGL-/SDL2-Quellen wurden aus einem vorhandenen Referenzprojekt
uebernommen und jetzt in den eigenen Projektbestand ueberfuehrt:

- [third_party/lvgl-9.4](/Users/ralfhille/develop/net10/mcu_ui_studio_for_lvgl/third_party/lvgl-9.4)
- [third_party/SDL2.framework](/Users/ralfhille/develop/net10/mcu_ui_studio_for_lvgl/third_party/SDL2.framework)
  auf macOS als Projekt-Fallback

Damit ist klar:

- SDL2 und LVGL sind jetzt Teil des eigenen Projektbestands
- der native Simulator-Build ist nicht mehr an `foerderbaender` gekoppelt

## Was mit dem aktuellen C#-Preview-Fenster passiert

Der bisherige C#-Preview-Host ist als Referenz- und Debug-Backend weiterhin
nuetzlich.

Im spaeteren Endzustand gibt es deshalb zwei sinnvolle Modi:

- `C# Preview Host`
  schnell, einfach, hilfreich fuer Protokoll- und Editor-Debugging
- `Native Simulator Stub / LVGL Runtime`
  echte Simulator-Richtung mit LVGL und SDL2

Das C#-Fenster muss also nicht verschwinden. Es kann bewusst als alternatives
Preview-Backend erhalten bleiben.
