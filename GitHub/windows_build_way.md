# Windows Build Way

Diese Notiz beschreibt den aktuell verifizierten lokalen Ablauf fuer den
Windows-x64-Desktop-Release inklusive:

- Editor-App
- nativer LVGL-/SDL2-Simulator
- eingebettetes Handbook
- `examples/`
- Release-ZIP

## Zielordner

- App-Publish: `platforms/windows/app-publish`
- nativer Runtime-Build: `native/lvgl_simulator_host/build-windows`
- Simulator-Ablage: `platforms/windows/simulator`
- Release-Bundle: `platforms/windows/release`
- Produktordner: `platforms/windows/MCU UI Studio for LVGL Windows x64`
- ZIP: `platforms/windows/mcu_ui_studio_for_lvgl-windows-release.zip`

## Voraussetzungen

- Windows x64
- .NET SDK
- MSYS2 mit MinGW64-Toolchain
- Python/MkDocs fuer das lokale Handbook
- vollstaendiges `third_party/lvgl-9.4`

## 1. App publishen

```bash
dotnet publish src/Ai.McuUiStudio.App/Ai.McuUiStudio.App.csproj \
  -c Release \
  -r win-x64 \
  --self-contained true \
  -o platforms/windows/app-publish
```

## 2. Handbook lokal bauen

Das Handbook wird als statische Site in den Release kopiert.

```bash
mkdocs build -f usermanual/mkdocs.de.yml
mkdocs build -f usermanual/mkdocs.en.yml
```

Erwartet wird danach eine gebaute Site unter:

- `usermanual/site`

## 3. Nativen Runtime-Simulator bauen

Wichtig:

- nicht den Stub-Build verwenden
- fuer das echte Simulatorfenster ist `LVGL_SIMULATOR_WITH_RUNTIME=ON` Pflicht
- der Build muss in einer MSYS2-MinGW64-Shell laufen

```bash
cmake -S native/lvgl_simulator_host \
  -B native/lvgl_simulator_host/build-windows \
  -G Ninja \
  -DCMAKE_C_COMPILER="C:/msys64/mingw64/bin/gcc.exe" \
  -DCMAKE_CXX_COMPILER="C:/msys64/mingw64/bin/g++.exe" \
  -DCMAKE_MAKE_PROGRAM="C:/msys64/mingw64/bin/ninja.exe" \
  -DLVGL_SIMULATOR_WITH_RUNTIME=ON

cmake --build native/lvgl_simulator_host/build-windows
```

Ergebnis:

- `native/lvgl_simulator_host/build-windows/lvgl_simulator_host.exe`

## 4. Simulator-Dateien bereitstellen

Der nativen EXE muessen die Laufzeitdateien danebenliegen.

Pflichtdateien in `platforms/windows/simulator`:

- `lvgl_simulator_host.exe`
- `SDL2.dll`
- `libgcc_s_seh-1.dll`
- `libstdc++-6.dll`
- `libwinpthread-1.dll`

Die DLLs kommen typischerweise aus:

- `C:/msys64/mingw64/bin/`

## 5. Release-Bundle aufbauen

Grundidee:

- Publish-Ausgabe als Basis verwenden
- Handbook in den Produktordner legen
- Simulator mit DLLs in den Produktordner legen
- `examples/` fuer den Nutzer mitliefern

Wichtige Bundle-Inhalte:

- `MCU UI Studio for LVGL Windows x64/Ai.McuUiStudio.App.exe`
- `MCU UI Studio for LVGL Windows x64/DocumentationSite`
- `MCU UI Studio for LVGL Windows x64/simulator/lvgl_simulator_host.exe`
- `MCU UI Studio for LVGL Windows x64/simulator/SDL2.dll`
- `MCU UI Studio for LVGL Windows x64/examples`

## 6. ZIP bauen

Das ZIP soll mit Elternordner gebaut werden, damit beim Entpacken direkt ein
klar benannter Produktordner entsteht.

Beispiel unter macOS/Linux:

```bash
/usr/bin/ditto -c -k --keepParent \
  "platforms/windows/MCU UI Studio for LVGL Windows x64" \
  "platforms/windows/mcu_ui_studio_for_lvgl-windows-release.zip"
```

Beispiel unter Windows mit PowerShell:

```powershell
Compress-Archive `
  -Path "platforms/windows/MCU UI Studio for LVGL Windows x64" `
  -DestinationPath "platforms/windows/mcu_ui_studio_for_lvgl-windows-release.zip" `
  -Force
```

## 7. Verifikation

Vor einem Release immer frisch pruefen:

1. alten entpackten Produktordner loeschen
2. ZIP frisch entpacken
3. `Ai.McuUiStudio.App.exe` starten
4. Beispielprojekt laden
5. Simulator-Vorschau pruefen
6. im technischen Log pruefen, dass nicht mehr `stub mode` erscheint

Erwartete Runtime-Hinweise:

- `LVGL runtime selected.`
- `Build configuration: SDL2=1 LVGL=1`
- `Preparing SDL2 window and platform glue.`

Nicht akzeptabel fuer den finalen Windows-Release:

- `Simulator runtime initialized in stub mode.`

## 8. Aktuelle Produktgrenze

Der aktuelle Windows-Release funktioniert technisch, ist aber noch nicht
code-signiert.

Das bedeutet fuer Endnutzer aktuell:

- Microsoft Defender SmartScreen kann beim ersten Start warnen
- das Handbook oeffnet derzeit im externen Browser

Fuer einen spaeteren saubereren oeffentlichen Windows-Release fehlen noch:

- Windows-Code-Signing
- idealerweise ein reproduzierbarer Signing-Schritt im Release-Prozess
