# Windows

Dieses Verzeichnis ist fuer alle **Windows-spezifischen Build- und Setup-Hinweise** vorgesehen.

Typische Themen fuer spaeter:

- .NET-SDK-Installation
- CMake
- C/C++ Build Tools
- SDL2-Setup
- nativer Preview-Host
- bekannte Unterschiede zur macOS-Umgebung

## Zielbild

Hier sollen spaeter die konkreten Hinweise landen, die fuer:

- Build
- Start
- Preview
- Fehlersuche

unter Windows relevant sind.

## App-Publish

Die Avalonia-App kann unter Windows bereits als eigenstaendige `.exe` veroeffentlicht werden.

Aktueller vorbereiteter Pfad in [Ai.McuUiStudio.App.csproj](/Users/ralfhille/develop/net10/mcu_ui_studio_for_lvgl/src/Ai.McuUiStudio.App/Ai.McuUiStudio.App.csproj):

- `Release|win-x64` wird als `SelfContained` veroeffentlicht
- `PublishSingleFile` ist fuer diesen Windows-Pfad aktiviert
- ein spaeterer Native-AOT-Test bleibt getrennt moeglich

Beispiel:

```bash
dotnet publish src/Ai.McuUiStudio.App/Ai.McuUiStudio.App.csproj -c Release -r win-x64 -o platforms/windows/app-publish
```

Wichtig:

- Das betrifft zunaechst nur die C#-/Avalonia-App.
- Der native LVGL-Preview-Host bleibt davon getrennt und muss fuer Windows weiterhin separat gebaut werden.

## Empfohlene Windows-Artefaktpfade

- App-Publish: `platforms/windows/app-publish`
- nativer Simulator: `platforms/windows/simulator`
- Release-Zusammenbau: `platforms/windows/release`

Fuer den nativen Simulator sollte unter Windows zusaetzlich ein eigener
CMake-Buildordner verwendet werden:

- `native/lvgl_simulator_host/build-windows`

## Verifiziert

Der jeweilige App-Publish-Pfad wurde bereits erfolgreich ausgefuehrt.

- die eigenstaendige App-Ausgabe wird erzeugt
- der normale Startpfad der Avalonia-App bleibt erhalten
- der native LVGL-Preview-Host bleibt ein getrennter Bestandteil des Gesamtprojekts

## Rueckvergleich Windows-Kopie

Beim Rueckvergleich der Windows-Arbeitskopie `mcu_ui_studio_for_lvgl_win` mit dem Hauptprojekt zeigte sich:

- im nativen C-/Header-Bereich war fachlich nur `native/lvgl_simulator_host/CMakeLists.txt` relevant
- uebernommen wurden daraus:
  - Linken von `ws2_32` unter Windows
  - direktes Linken von SDL2 an das `lvgl`-Target
- die Unterschiede in den `.c`- und `.h`-Dateien selbst waren nicht fachlich relevant
- Unterschiede in `build/` waren reine Plattform-/Build-Artefakte

Im `src/`-Bereich gab es ebenfalls Unterschiede, diese waren aber kein Hinweis auf zusaetzliche Windows-spezifische C#-Aenderungen, sondern vor allem:

- Windows-Build-Ausgaben (`bin/`, `obj`, `.exe`)
- aelterer Arbeitsstand der Windows-Kopie gegenueber dem weiterentwickelten Hauptprojekt

Leitlinie:
- Windows-Vergleichskopien nur fuer gezielte Rueckpruefung verwenden
- fachlich relevante Aenderungen einzeln uebernehmen
- Build-Artefakte und Plattformnebenprodukte nicht zurueckfuehren
