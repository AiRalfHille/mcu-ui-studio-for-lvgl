# macOS ARM64

Dieses Verzeichnis ist fuer alle **macOS-ARM64-spezifischen Build- und Setup-Hinweise** vorgesehen.

Typische Themen fuer spaeter:

- .NET-Setup
- SDL2-/LVGL-Preview-Host
- CMake-Build
- Apple-spezifische Laufzeit- oder Fensterfragen

## Zielbild

Hier sollen spaeter die konkreten Hinweise fuer die lokale macOS-ARM64-Entwicklung gesammelt werden.

## App-Publish

Die Avalonia-App kann auch fuer macOS ARM64 als eigenstaendige Anwendung veroeffentlicht werden.

Aktueller vorbereiteter Pfad in [Ai.McuUiStudio.App.csproj](/Users/ralfhille/develop/net10/mcu_ui_studio_for_lvgl/src/Ai.McuUiStudio.App/Ai.McuUiStudio.App.csproj):

- `Release|osx-arm64` wird als `SelfContained` veroeffentlicht
- `PublishSingleFile` ist fuer diesen macOS-Pfad aktiviert
- ein spaeterer Native-AOT-Test bleibt getrennt moeglich

Beispiel:

```bash
dotnet publish src/Ai.McuUiStudio.App/Ai.McuUiStudio.App.csproj -c Release -r osx-arm64 -o platforms/macos-arm64/app-publish
```

Wichtig:

- Das betrifft zunaechst nur die C#-/Avalonia-App.
- Der native LVGL-Preview-Host bleibt davon getrennt und hat seinen eigenen nativen Buildpfad.

## Empfohlene macOS-Artefaktpfade

- App-Publish: `platforms/macos-arm64/app-publish`
- nativer Simulator: `platforms/macos-arm64/simulator`
- Release-Zusammenbau: `platforms/macos-arm64/release`

Fuer den nativen Simulator sollte unter macOS zusaetzlich ein eigener
CMake-Buildordner verwendet werden:

- `native/lvgl_simulator_host/build-macos`

## Verifiziert

Der jeweilige App-Publish-Pfad wurde bereits erfolgreich ausgefuehrt.

- die eigenstaendige App-Ausgabe wird erzeugt
- der normale Startpfad der Avalonia-App bleibt erhalten
- der native LVGL-Preview-Host bleibt ein getrennter Bestandteil des Gesamtprojekts
