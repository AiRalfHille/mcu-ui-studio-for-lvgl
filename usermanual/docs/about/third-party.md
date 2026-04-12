# Drittanbieter-Bibliotheken

MCU UI Studio for LVGL verwendet verschiedene Drittanbieter-Komponenten für
Editor, Vorschau, Simulator, WebView und Dokumentation.

Diese Seite soll den aktuellen Stand möglichst klar und prüfbar beschreiben.
Sie unterscheidet deshalb zwischen:

- direkt referenzierten Komponenten
- transitiv mitgebrachten Laufzeitkomponenten
- systemseitigen Plattformkomponenten
- der separaten Dokumentations-Toolchain

!!! note "Grundlage dieser Übersicht"
    Die Angaben zu direkt referenzierten Paketen und erkennbaren
    Laufzeitkomponenten basieren auf dem aktuellen Repository-Stand,
    insbesondere auf den `.csproj`-Dateien, den `project.assets.json`-Dateien,
    der nativen Simulator-Konfiguration und der `mkdocs.yml`.

## Direkt referenzierte Komponenten

Diese Komponenten sind im aktuellen Projektstand unmittelbar im Editor, im
PreviewHost oder im nativen Simulator erkennbar eingebunden.

### LVGL

- Verwendung: direkt im nativen Simulator; außerdem Zielsystem des
  generierten C-Codes
- Rolle: zentrale Embedded-Grafikbibliothek, auf die sich Editor,
  Metamodell, Generatoren und Simulator fachlich ausrichten
- Verwendeter Stand im Projekt: `9.4`
- Lizenz: `MIT`
- Projekt: [https://lvgl.io](https://lvgl.io)

LVGL ist die Bibliothek, auf die sich das Werkzeug inhaltlich und technisch
bezieht. Der Simulator verwendet LVGL direkt, und der Generator erzeugt
LVGL-C-Code für den weiteren Einsatz im Zielprojekt.

### SDL2

- Einbindung: direkt im nativen Simulatorpfad
- Rolle: Fenster-, Eingabe- und Render-Infrastruktur für den Desktop-Simulator
- Verwendeter Stand im Projekt: framework- oder systemabhängig
- Lizenz: `zlib`
- Projekt: [https://libsdl.org](https://libsdl.org)

SDL2 wird verwendet, um LVGL im nativen Simulatorfenster auf dem Desktop
auszuführen.

### Avalonia

- Einbindung: direkt
- Rolle: Desktop-UI-Framework des Editors und des PreviewHost
- Verwendeter Stand im Projekt: `12.0.0`
- Lizenz: `MIT`
- Projekt: [https://avaloniaui.net](https://avaloniaui.net)

Avalonia stellt die Hauptoberfläche des Editors bereit.

### Avalonia.Desktop

- Einbindung: direkt
- Rolle: Desktop-Backend für Fenstersystem, Eingabe und Anwendungsstart
- Verwendeter Stand im Projekt: `12.0.0`
- Lizenz: `MIT`
- Projekt: [https://avaloniaui.net](https://avaloniaui.net)

### Avalonia.Themes.Fluent

- Einbindung: direkt
- Rolle: Standard-Theme des Editors
- Verwendeter Stand im Projekt: `12.0.0`
- Lizenz: `MIT`
- Projekt: [https://avaloniaui.net](https://avaloniaui.net)

### Avalonia.Fonts.Inter

- Einbindung: direkt
- Rolle: Standardschrift im Avalonia-UI-Stack
- Verwendeter Stand im Projekt: `12.0.0`
- Lizenz: `MIT`
- Projekt: [https://avaloniaui.net](https://avaloniaui.net)

### Avalonia.Controls.WebView

- Einbindung: direkt
- Rolle: eingebettete Handbuchanzeige im Editor
- Verwendeter Stand im Projekt: `12.0.0`
- Lizenz: `MIT`
- Projekt: [https://avaloniaui.net](https://avaloniaui.net)

Diese Komponente wird verwendet, um das Benutzerhandbuch direkt im rechten
Bereich der Anwendung anzuzeigen.

## Laufzeitplattform

Die Anwendung selbst wird auf Basis von .NET ausgeführt. .NET ist dabei keine
klassische UI-Bibliothek des Projekts, aber eine zentrale Laufzeitvoraussetzung.

### .NET

- Einbindung: Laufzeitplattform
- Rolle: Entwicklungs- und Laufzeitbasis der Anwendung und ihrer Generatoren
- Verwendeter Stand im Projekt: `.NET 10`
- Lizenz: `MIT`
- Projekt: [https://dotnet.microsoft.com](https://dotnet.microsoft.com)

## Transitive Laufzeitkomponenten

Diese Komponenten werden im aktuellen Stand nicht als fachliche Hauptbausteine
des Editors direkt beworben, sind aber im verwendeten Paketstapel klar
erkennbar und sollten deshalb in einer Drittanbieter-Übersicht genannt werden.

### SkiaSharp

- Einbindung: transitiv
- Rolle: 2D-Rendering im Desktop-UI-Stack
- Verwendeter Stand im Projekt: `3.119.3-preview.1.1`
- Lizenz: `MIT`
- Projekt: [https://github.com/mono/SkiaSharp](https://github.com/mono/SkiaSharp)

Zusätzlich werden plattformspezifische native Asset-Pakete von SkiaSharp
mitgeführt, unter anderem für macOS, Windows und Linux.

### HarfBuzzSharp

- Einbindung: transitiv
- Rolle: Textlayout und Schrift-Shaping
- Verwendeter Stand im Projekt: `8.3.1.3`
- Lizenz: `MIT`
- Projekt: [https://github.com/mono/SkiaSharp](https://github.com/mono/SkiaSharp)

Auch hier kommen ergänzende plattformspezifische Native-Assets mit dem gleichen
Versionsstand zum Einsatz.

### MicroCom.Runtime

- Einbindung: transitiv
- Rolle: Interop-Laufzeitkomponente im Avalonia-Umfeld
- Verwendeter Stand im Projekt: `0.11.4`
- Lizenz: `MIT`
- Projekt: [https://github.com/kekekeks/MicroCom](https://github.com/kekekeks/MicroCom)

### Tmds.DBus.Protocol

- Einbindung: transitiv
- Rolle: plattformspezifische Desktop-Integration, insbesondere unter Linux
- Verwendeter Stand im Projekt: `0.90.3`
- Lizenz: `MIT`
- Projekt: [https://github.com/tmds/Tmds.DBus](https://github.com/tmds/Tmds.DBus)

## Dokumentations-Toolchain

Das Benutzerhandbuch selbst wird als statische Dokumentation erzeugt und nicht
direkt aus der Editor-Anwendung heraus aufgebaut.

### MkDocs

- Einbindung: Dokumentations-Toolchain
- Rolle: statischer Dokumentationsgenerator
- Verwendeter Stand im Projekt: aktuell nicht im Repository fixiert
- Lizenz: `BSD-2-Clause`
- Projekt: [https://www.mkdocs.org](https://www.mkdocs.org)

### Material for MkDocs

- Einbindung: Dokumentations-Toolchain
- Rolle: Theme, Navigation und visuelle Struktur des Handbuchs
- Verwendeter Stand im Projekt: aktuell nicht im Repository fixiert
- Lizenz: `MIT`
- Projekt: [https://squidfunk.github.io/mkdocs-material](https://squidfunk.github.io/mkdocs-material)

### pymdown-extensions

- Einbindung: Dokumentations-Toolchain
- Rolle: Markdown-Erweiterungen für Hinweise, Tabs, Codeblöcke und weitere
  Dokumentationsfunktionen
- Verwendeter Stand im Projekt: aktuell nicht im Repository fixiert
- Lizenz: `MIT`
- Projekt: [https://facelessuser.github.io/pymdown-extensions](https://facelessuser.github.io/pymdown-extensions)

!!! note "Versionsstand der Dokumentations-Toolchain"
    In `mkdocs.yml` ist erkennbar, welche Komponenten für das Handbuch
    verwendet werden. Die genauen Python-Paketversionen sind im Repository
    derzeit jedoch nicht über eine eigene `requirements.txt` oder ein
    vergleichbares Locking festgeschrieben.

## Systemseitige Plattformkomponenten

Bestimmte Funktionen greifen zusätzlich auf native Systemkomponenten der
jeweiligen Desktop-Plattform zurück. Diese sind funktional relevant, werden
aber nicht als normale Bibliothekspakete im Repository mitgeführt.

### WKWebView

- Einbindung: systemseitig unter macOS
- Rolle: native WebView-Engine für die Handbuchanzeige
- Herkunft: Bestandteil von Apple WebKit / macOS-Systemframeworks

### WebView2

- Einbindung: systemseitig unter Windows
- Rolle: native WebView-Engine für die Handbuchanzeige
- Herkunft: Microsoft Edge WebView2 Runtime

## Pflegehinweis

Diese Seite sollte insbesondere dann aktualisiert werden, wenn sich einer der
folgenden Punkte ändert:

- größere Versionssprünge bei Avalonia, LVGL oder .NET
- Austausch oder Erweiterung des WebView-Stacks
- Änderungen an der Simulator- oder Dokumentations-Toolchain
- neue direkt referenzierte NuGet-Pakete oder native Bibliotheken
