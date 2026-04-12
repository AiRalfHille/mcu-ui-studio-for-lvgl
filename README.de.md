# MCU UI Studio for LVGL

English version: [README.md](README.md)

LVGL-Editor und Codegenerator fuer MCU-Projekte.

## Release-Status

Aktueller oeffentlicher Release:

- Windows-x64-Paket steht als direkter Desktop-Download bereit.
- macOS-ARM64-Paket wird aktuell als unsigniertes App-Bundle bereitgestellt.

Wichtiger Hinweis fuer macOS:

- nach dem Download und Entpacken das Paket ueber
  `Start MCU UI Studio.command` starten
- dieses Hilfsskript entfernt lokal das Quarantine-Flag vom App-Bundle und
  oeffnet danach die App
- das macOS-Paket ist aktuell noch nicht notarisiert

Wichtiger Hinweis fuer Windows:

- unter Windows wird das Handbuch derzeit im externen Browser geoeffnet
- der eingebettete WebView-Pfad ist fuer den Windows-Build aktuell noch nicht
  stabil genug

Die meisten LVGL-Editoren erzeugen Display-Code. Dieses Projekt entwickelt sich
in die Richtung, den kompletten Vertrag zwischen MCU-Anwendung und Display zu
erzeugen:

- generierter LVGL-Screen-Code
- generierte Event-Bindings vom UI zur Anwendung
- generierte Update-Funktionen von der Anwendung zurueck ins UI
- eine klarere Trennung zwischen UI-Design und Embedded-Runtime-Logik

## Was Das Projekt Ist

`MCU UI Studio for LVGL` ist ein Desktop-Editor auf Basis von Avalonia und .NET.

Der Benutzer arbeitet ueber:

- Werkzeugkasten
- Strukturbaum
- typisierte Properties
- Event-Konfiguration
- generierte JSON- und C-Artefakte

Das Ziel ist nicht, LVGL-C, XML oder JSON von Hand zu bearbeiten, sondern die
UI zu modellieren und die Artefakte generieren zu lassen.

## Aktuelle Richtung

Das Projekt unterstuetzt derzeit zwei grobe Embedded-Generierungsstile:

- `Standard`
  - behaelt den bisherigen, staerker LVGL-orientierten Generierungspfad
- `RTOS-Messages`
  - erzeugt einen expliziteren Vertrag fuer Queue-basierte MCU-Anwendungen

Fuer `RTOS-Messages` ist das aktuelle Modell:

- Widgets mit `id` und `useUpdate: true` werden zu Update-Contract-Objekten
- konfigurierte Widget-Events erzeugen typisierte Actions
- der generierte Contract kann von Controller-, Machine- und Display-Code genutzt werden
- der MCU-Entwickler soll in normaler Steuerlogik kein rohes LVGL-C schreiben muessen

## Kerngedanke

Der Designer definiert:

- Screen-Struktur
- Widget-Properties
- ausgehende Events
- eingehende Update-Ziele

Der Embedded-Entwickler bekommt:

- generierte C-Dateien
- Enums und Contracts
- Update-Funktionen
- eine klarere Grenze zwischen LVGL und Anwendungslogik

## Architektur

Der Editor folgt grob diesem Ablauf:

```text
Metamodell
  -> Dokumentmodell
  -> Validierung
  -> JSON-Serialisierung
  -> C-Codegenerierung
```

Wichtiges Prinzip:

- die JSON ist das Primaermodell
- generierte C-Dateien sind abgeleitete Artefakte

Das ist besonders wichtig fuer spaetere Domain-Features und reichere Contracts.

## Projektstruktur

- `Ai.McuUiStudio.slnx`
  - Solution-Einstieg fuer das Repository
- `src/Ai.McuUiStudio.Core`
  - Metamodell, Dokumentmodell, Validierung, Parser, Generatoren
- `src/Ai.McuUiStudio.App`
  - Avalonia-Desktopanwendung
- `src/Ai.McuUiStudio.PreviewHost`
  - Preview-Backend-Prozess
- `native/lvgl_simulator_host`
  - nativer Simulator-Baustein des Preview-Systems des Editors
- `usermanual/docs`
  - Quellbasis des Benutzerhandbuchs
- `examples`
  - Editor-Beispiele und passende Zielprojekte
- `platforms`
  - plattformspezifische Hinweise

## Aktuelle Highlights

- LVGL-9.4-Metamodellpfad
- typisierter Property-Editor
- strukturierte Event-Modellierung in JSON
- Projektvorlagen im Projektdialog
- `ui_start.json`-artige Screen-Dateien
- generierte `ui_start.c/.h`, `ui_start_event.c`, `ui_start_update.c`
- RTOS-Contract-Generierung mit Objekt-Enums und Action-Enums
- das Flag `useUpdate` in der Property-Gruppe `Data` steuert, welche Widgets
  im Update-Contract erscheinen
- konfigurierbares Build-Ausgabeverzeichnis
- native Simulator-Vorschau ueber SDL2 in der jeweils konfigurierten Screengroesse
- die Auswahl im Strukturbaum markiert das zugehoerige Widget im Simulator mit
  einer roten Umrandung

## Scope der Version 1

Die aktuelle Version unterstuetzt bereits eine nuetzliche Menge einfacher
Widgets und die wesentlichen Generierungspfade, bildet aber noch nicht jedes
LVGL-Widget und jedes Styling-Detail vollstaendig ab.

Das betrifft besonders komplexere Widgets und Widget-Parts, zum Beispiel:

- weitergehendes widget-spezifisches Styling
- Part-basiertes Styling wie Slider-Knob-Details
- reichere domain-spezifische Parameter-Modellierung

Kurz gesagt:

- einfache Widgets und der zentrale Embedded-Flow stehen aktuell im Fokus
- tiefere LVGL-Styling-Abdeckung wird schrittweise ausgebaut

## Preview und Simulator

Das Projekt hat bereits eine echte native Preview-Richtung und nicht nur
statische Exporte oder Textdiagnostik.

Das Preview-System gehoert zum Editor selbst und besteht aktuell aus:

- dem editorseitigen Preview-Host
- dem nativen Simulator-Host unter `native/lvgl_simulator_host`
- SDL2 fuer das Rendern der UI in einem Desktop-Fenster

Damit ist die Vorschau darauf ausgelegt, den generierten Screen in der wirklich
konfigurierten Displaygroesse zu zeigen und nicht nur ein allgemeines Layout
grob anzunaehern.

Wichtig:

- der Simulator ist kein separates Zusatzprodukt neben dem Editor
- er ist ein Backend des Preview-Systems des Editors
- C#-Preview-Host und nativer Simulator gehoeren zur selben Gesamtarchitektur

Aktuell wird der native Simulator weiterhin in einem separaten nativen
Build-Schritt erzeugt. Fuer einen sauberen Mehrplattform-Workflow sollten
macOS und Windows dabei nicht denselben generischen Build-Ordner teilen.

Empfohlene Simulator-Buildordner:

- `native/lvgl_simulator_host/build-macos`
- `native/lvgl_simulator_host/build-windows`

Empfohlene plattformbezogene Release-Ablage:

- `platforms/macos-arm64/app-publish`
- `platforms/macos-arm64/simulator`
- `platforms/macos-arm64/release`
- `platforms/windows/app-publish`
- `platforms/windows/simulator`
- `platforms/windows/release`

Dadurch bleiben Artefakte, CMake-Caches und Toolchain-Staende sauber nach
Plattform getrennt.

### Markierung aus dem Strukturbaum

Wenn im Strukturbaum ein Widget ausgewaehlt wird, markiert der Simulator dieses
Objekt mit einer roten Umrandung. Die Markierung wird wieder entfernt, wenn die
Auswahl auf ein Widget ohne `id` wechselt oder keine Auswahl mehr aktiv ist.

Das funktioniert nur fuer Widgets mit einer `id`, weil die `id` im laufenden
LVGL-Screen als Handle zur Objektzuordnung verwendet wird.

## RTOS-Messages

Die Projektvorlage `RTOS-Messages` ist fuer Queue-basierte MCU-Projekte gedacht.

Der wichtige Gedanke ist:

- das Display kennt den Controller nicht
- generierter Code produziert und konsumiert Contract-Typen
- Controller-, Machine- und Fieldbus-Logik liegen ausserhalb von LVGL-Code

Das passende ESP32-Referenztemplate wird separat gehalten und dient dazu, diesen
Integrationsstil praktisch zu validieren.

### Kriterien fuer den Update-Contract

Ein Widget erscheint im generierten `*_update.c`-Contract, wenn:

- es ein `id`-Attribut besitzt
- `useUpdate` in der Property-Gruppe `Data` auf `true` gesetzt ist

Widgets ohne `id` oder mit `useUpdate: false` erscheinen nicht in den
Update-Tabellen, koennen aber weiterhin Event-Bindings besitzen.

Auch die Vorlage `Standard` verwendet dasselbe `useUpdate`-Flag, um zu
entscheiden, welche Widgets Update-Ziele in `ui_start_update.c` erzeugen.

Aktuell getesteter Scope:

- ein aktiver generierter Screen pro Projekt ist der belastbare Weg
- Mehrscreen-Generierung ist konzeptionell vorbereitet, aber noch nicht als voll validiert behandelt

## Lokaler Start

Einfacher lokaler Start:

```bash
./run.sh
```

Oder in VS Code ueber:

- `Terminal -> Run Task -> build`
- `Terminal -> Run Task -> run app`
- `Run and Debug -> Launch MCU UI Studio for LVGL`

## Build-Hinweise

- .NET 10
- Avalonia UI
- lokaler GUI-Start kann je nach Umgebung eingeschraenkt sein
- gemeinsamer Quellstand ist moeglich, Build-Artefakte sollten aber
  plattformgetrennt bleiben

Editor-Build:

```bash
dotnet build src/Ai.McuUiStudio.App/Ai.McuUiStudio.App.csproj
```

Empfohlener macOS-Publish:

```bash
dotnet publish src/Ai.McuUiStudio.App/Ai.McuUiStudio.App.csproj \
  -c Release -r osx-arm64 --self-contained true \
  -o platforms/macos-arm64/app-publish
```

Empfohlener Windows-Publish:

```bash
dotnet publish src/Ai.McuUiStudio.App/Ai.McuUiStudio.App.csproj \
  -c Release -r win-x64 --self-contained true \
  -o platforms/windows/app-publish
```

Empfohlene Builds fuer den nativen Simulator:

```bash
cmake -S native/lvgl_simulator_host -B native/lvgl_simulator_host/build-macos -G Ninja
cmake --build native/lvgl_simulator_host/build-macos
```

```bash
cmake -S native/lvgl_simulator_host -B native/lvgl_simulator_host/build-windows -G Ninja
cmake --build native/lvgl_simulator_host/build-windows
```

Empfohlener Release-Zusammenbau:

- den App-Publish in den passenden `platforms/.../release`-Ordner uebernehmen
- das native Simulator-Binary in `platforms/.../simulator` ablegen
- das finale Release-ZIP aus `platforms/macos-arm64/release` oder
  `platforms/windows/release` erzeugen

Aktuell bekannte Desktop-Hinweise:

- fuer Datei- und Ordnerauswahl verwendet das Projekt derzeit eigene Dialoge
  statt nativer Plattformdialoge
  - Hintergrund: der native Dialogpfad war unter macOS fuer den aktuellen
    Projektalltag nicht stabil genug
- beim Build erscheint derzeit eine transitive Sicherheitswarnung zu
  `Tmds.DBus.Protocol 0.90.3`
  - Advisory: `GHSA-xrw6-gwf8-vvr9`
  - dieser Punkt ist als expliziter Bereinigungs- und Nacharbeitspunkt notiert
- der native Simulator ist bereits Teil des Editor-Repositories und der
  Preview-Architektur
  - sein nativer Build und seine Plattformvalidierung sind derzeit aber noch
    ein eigener Schritt

## Dokumentation

Die Quellbasis der Bedienungsanleitung liegt unter:

- `usermanual/docs`

Die generierten Handbuchseiten sind fuer eine separate Veroeffentlichung
gedacht, zum Beispiel ueber GitHub Pages oder Release-Artefakte.

## Beispiele

Der aktuell aktive Beispielsatz besteht aus:

- `examples/portal`
  - RTOS-Messages-Beispiel mit `examples/targets/portal`
- `examples/kachel`
  - Standard-Beispiel mit `examples/targets/kachel`
- `examples/widgets`
  - Widget-Galerie mit `examples/targets/widgets`

## Warum Das Relevant Ist

Der langfristige Wert liegt nicht nur darin, Screens zu zeichnen.

Die staerkere Produktidee ist:

- der UI-Designer definiert, was das Display senden und empfangen kann
- der MCU-Entwickler arbeitet gegen einen generierten C-Contract
- LVGL wird vom Glue-Code eher zum Implementierungsdetail
