# macOS ARM64 Build Way

Diese Notiz beschreibt den aktuell verifizierten lokalen Ablauf fuer den
macOS-ARM64-Desktop-Release inklusive:

- Editor-App
- nativer LVGL-/SDL2-Simulator
- eingebettetes Handbook
- `examples/`
- ZIP
- DMG

## Zielordner

- App-Publish: `platforms/macos-arm64/app-publish`
- nativer Runtime-Build: `native/lvgl_simulator_host/build-macos-runtime`
- Release-Bundle: `platforms/macos-arm64/release`
- Produktordner: `platforms/macos-arm64/MCU UI Studio for LVGL macOS ARM64`
- ZIP: `platforms/macos-arm64/mcu_ui_studio_for_lvgl-macos-arm64-release.zip`
- DMG: `platforms/macos-arm64/mcu_ui_studio_for_lvgl-macos-arm64.dmg`

## Voraussetzungen

- macOS ARM64
- Xcode Command Line Tools
- .NET SDK
- Python/MkDocs fuer das lokale Handbook
- `third_party/SDL2.framework`
- vollstaendiges `third_party/lvgl-9.4`

## 1. App publishen

```bash
dotnet publish src/Ai.McuUiStudio.App/Ai.McuUiStudio.App.csproj \
  -c Release \
  -r osx-arm64 \
  --self-contained true \
  -o platforms/macos-arm64/app-publish
```

## 2. Handbook lokal bauen

Das Handbook wird als statische Site in den App-Release kopiert.

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

```bash
rm -rf native/lvgl_simulator_host/build-macos-runtime

cmake -S native/lvgl_simulator_host \
  -B native/lvgl_simulator_host/build-macos-runtime \
  -DLVGL_SIMULATOR_WITH_RUNTIME=ON

cmake --build native/lvgl_simulator_host/build-macos-runtime
```

Ergebnis:

- `native/lvgl_simulator_host/build-macos-runtime/lvgl_simulator_host`

## 4. Release-Bundle aufbauen

Grundidee:

- Publish-Ausgabe als `.app`-Bundle verwenden
- Handbook ins Bundle legen
- nativen Simulator ins Bundle legen
- SDL2-Framework ins Bundle legen

Wichtige Bundle-Inhalte:

- `Ai.McuUiStudio.App.app/Contents/MacOS/Ai.McuUiStudio.App`
- `Ai.McuUiStudio.App.app/Contents/MacOS/DocumentationSite`
- `Ai.McuUiStudio.App.app/Contents/MacOS/simulator/lvgl_simulator_host`
- `Ai.McuUiStudio.App.app/Contents/Frameworks/SDL2.framework`

Der gebundelte Simulator muss aus dem Runtime-Build kommen, nicht aus dem
Stub-Build.

## 5. Produktordner aufbauen

Im Produktordner liegen sichtbar fuer den Nutzer:

- `Ai.McuUiStudio.App.app`
- `Start MCU UI Studio.command`
- `examples/`
- optional ein `Applications`-Alias

`examples/` enthaelt aktuell:

- `portal/`
- `kachel/`
- `widgets/`
- `README.md`

## 6. Startskript fuer lokale Mac-Tests

Das Startskript dient als pragmatischer Test-/Uebergangsweg fuer nicht
notarisierte Builds.

Aktuelle Aufgaben des Skripts:

- `quarantine` von der App entfernen
- App-Binary und Simulator `chmod +x`
- `SDL2.framework` mitbehandeln
- Simulator zuerst ad-hoc signieren
- danach App mit `--deep` signieren
- App oeffnen

Wichtig:

- Das ist kein Ersatz fuer echte Apple-Signierung und Notarisierung.
- Fuer einen spaeteren oeffentlichen Endnutzer-Release sollte ein sauber
  signiertes und notarisiertes Paket verwendet werden.

## 7. ZIP bauen

Das ZIP soll mit Elternordner gebaut werden, damit beim Entpacken direkt ein
klar benannter Produktordner entsteht.

```bash
/usr/bin/ditto -c -k --sequesterRsrc --keepParent \
  "platforms/macos-arm64/MCU UI Studio for LVGL macOS ARM64" \
  "platforms/macos-arm64/mcu_ui_studio_for_lvgl-macos-arm64-release.zip"
```

## 8. DMG bauen

Der direkte `hdiutil create`-Lauf aus dem Projektpfad kann auf diesem Mac mit
`Die Ressource ist zeitweilig nicht verfuegbar` scheitern.

Der robuste Ablauf ist:

1. Produktordner in ein Temp-Verzeichnis unter `/tmp` kopieren
2. von dort das DMG erzeugen
3. die fertige Datei nach `platforms/macos-arm64/` zurueckkopieren

Beispiel:

```bash
STAGE=$(mktemp -d /tmp/mcu-ui-studio-dmg.XXXXXX)
cp -R "platforms/macos-arm64/MCU UI Studio for LVGL macOS ARM64" "$STAGE/"

hdiutil create \
  -srcfolder "$STAGE/MCU UI Studio for LVGL macOS ARM64" \
  -volname "MCU UI Studio for LVGL macOS ARM64" \
  -format UDZO \
  -ov \
  /tmp/mcu_ui_studio_for_lvgl-macos-arm64.dmg

cp /tmp/mcu_ui_studio_for_lvgl-macos-arm64.dmg \
  platforms/macos-arm64/mcu_ui_studio_for_lvgl-macos-arm64.dmg
```

## 9. Verifikation

Vor einem Release immer frisch pruefen:

1. altes entpacktes Paket loeschen
2. ZIP oder DMG frisch verwenden
3. Paket nicht aus `Desktop` testen, besser aus `Documents` oder `~/Test`
4. App starten
5. Beispielprojekt laden
6. im technischen Log pruefen, dass nicht mehr `stub mode` erscheint

Erwartete Runtime-Hinweise:

- `LVGL runtime selected.`
- `Build configuration: SDL2=1 LVGL=1`
- `Preparing SDL2 window and platform glue.`

Nicht akzeptabel fuer den finalen Mac-Release:

- `Simulator runtime initialized in stub mode.`

## 10. Offene Produktgrenze

Der aktuelle lokale Mac-Release kann technisch funktionieren, ist aber ohne
Apple Developer ID und Notarisierung noch kein voll vertrauenswuerdiger
One-click-Endnutzer-Release.

Fuer einen wirklich sauberen oeffentlichen Mac-Release fehlen spaeter noch:

- Codesigning mit Developer ID
- Apple Notarisierung
- idealerweise ein final sauber signiertes DMG
