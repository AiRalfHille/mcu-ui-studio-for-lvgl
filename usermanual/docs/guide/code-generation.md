# Code-Generierung

Dieses Kapitel beschreibt, welche Artefakte MCU UI Studio for LVGL erzeugt und
wie die Generierung grundsätzlich aufgebaut ist.

Die Code-Generierung ist kein nachgelagerter Fremdschritt, sondern ein
zentraler Teil des Werkzeugs. Sie übersetzt das interne Screen-Modell in
Dateien, die im Zielprojekt weiterverwendet werden können.

## Ausgangspunkt der Generierung

Grundlage der Generierung ist immer das interne Screen-Modell des Editors.

Ein Screen wird dabei nicht direkt aus einer visuellen Oberfläche „abfotografiert“,
sondern aus seiner strukturierten Beschreibung erzeugt. Diese Beschreibung
enthält unter anderem:

- Elementtypen
- Attribute
- Events
- Kind-Elemente

Dadurch basiert die Generierung auf denselben Informationen wie Editor und
Simulator.

## Ziel der Generierung

Das Ziel der Generierung ist, aus dem bearbeiteten Screen verwertbare
Quelltexte für den weiteren Einsatz im Projekt abzuleiten.

Je nach Projektvorlage entstehen dabei unterschiedliche Artefakte:

- allgemeiner LVGL-C-Code
- Display-seitige Initialisierungs- und Aufbau-Dateien
- im RTOS-Messages-Modus zusätzlich Contract-, Event- und Update-Dateien

## Generierung im Standard-Modus

Im Standard-Modus liegt der Schwerpunkt auf direktem LVGL-C-Code und auf einer
einfachen Einbindung in die Display-Seite.

Dabei entstehen typischerweise:

- eine Header-Datei
- eine C-Datei mit Initialisierung und Layout-Aufbau

In diesem Pfad erzeugt der Generator unter anderem:

- Objekt-Handles für exportierte Elemente
- eine Initialisierungsfunktion
- die Erstellung der Screen-Hierarchie
- Setter-Aufrufe für unterstützte Properties
- das Laden des Screens

Der erzeugte Code orientiert sich eng an den verwendeten LVGL-Aufrufen und
soll den Aufbau des Screens nachvollziehbar abbilden.

*Im aktuellen Stand erzeugt der Standard-Pfad zusaetzlich bereits eine
strukturierte Event-Bindung fuer MCU-Projekte. Dazu gehoeren insbesondere:*

- *eine Dispatcher-Funktion pro `eventGroup`*
- *fachliche Unterscheidung ueber `eventType`*
- *eine `action` als fachliche Hauptbedeutung*
- *ein getypter freier Event-`parameter`*
- *ein getypter Laufzeitwert fuer Wert-Widgets wie `slider`, `bar`, `arc` oder
  `spinbox`*

*Der technische LVGL-Trigger wie `clicked` oder `released` bleibt dabei fuer
die Registrierung des Callbacks relevant, steht aber nicht mehr als
zentrales fachliches Feld im Vordergrund. Fuer den MCU-seitigen Code sind
stattdessen `action`, `parameter`, `eventGroup`, `eventType` und gegebenenfalls
der aktuelle Widgetwert entscheidend.*

## Generierung im RTOS-Messages-Modus

Im RTOS-Messages-Modus geht die Generierung über den bloßen Screen-Aufbau
hinaus.

Zusätzlich zum Display-Code entsteht eine Contract-Schicht zwischen Anzeige
und Controllerlogik.

Im aktuellen Stand gehören dazu insbesondere:

- ein Contract-Header
- eine Event-Quelle für ausgehende UI-Ereignisse
- eine Update-Quelle für eingehende UI-Aktualisierungen

Dabei werden strukturierte Informationen über:

- Objekte
- Aktionen
- Parametertypen
- Event-Bindungen

in generierten Code überführt.

Dieser Pfad ist darauf ausgelegt, UI und Anwendungslogik über klar benannte
Nachrichten und Update-Funktionen miteinander zu verbinden.

*Ein wichtiger Punkt im aktuellen Stand ist die Trennung zwischen:*

- *einem freien Event-`parameter`*
- *dem eigentlichen Laufzeitwert eines Widgets*

*Damit kann zum Beispiel ein `slider` gleichzeitig:*

- *einen zusätzlichen Parameter wie `WARNING` mitgeben*
- *und separat seinen aktuellen numerischen Wert übertragen*

*Das ist insbesondere für den RTOS-Messages-Pfad relevant, weil Controller und
Display-Verarbeitung dadurch fachliche Zusatzinformation und Widgetwert sauber
unterscheiden können.*

*Ein vergleichbares Prinzip gilt inzwischen auch im Standard-Pfad: Auch dort
werden freier Event-`parameter` und aktueller Widgetwert bewusst getrennt
behandelt, damit der generierte Code fuer MCU-Projekte ohne Informationsverlust
weiterverwendet werden kann.*

## Typische Artefakte

Je nach Vorlage und Zielpfad können insbesondere folgende Dateitypen
entstehen:

- `*.h`
- `*.c`
- Contract-Dateien
- Event-Dateien
- Update-Dateien

Zusätzlich erzeugt das Projektgerüst bei Bedarf die grundlegende
Projektstruktur, etwa:

- `screens/`
- `build/`
- `lv_conf`
- Theme-Dateien

Die Generierung arbeitet also nicht nur auf einzelner Screen-Ebene, sondern
auch im Kontext der Projektstruktur.

## *`lv_conf` und Theme im Zielprojekt*

*Für MCU-Projekte sind nicht nur Screen-Dateien relevant, sondern auch die
Einbindung von `lv_conf` und Theme-Dateien in das Zielsystem.*

*Im aktuellen ESP-Referenzpfad ist dabei verifiziert:*

- *`lv_conf` wird nicht nur erzeugt, sondern im Build tatsächlich eingebunden*
- *Theme-Code wird nicht nur erzeugt, sondern zur Laufzeit auf das Display
  angewendet*

*Für den bestätigten ESP-Pfad gilt aktuell:*

- *`sdkconfig` bildet die Basis der LVGL-Konfiguration*
- *`lv_conf_project.h` wirkt als projektbezogenes Overlay*
- *`theme_project.c` liefert das tatsächlich initialisierte LVGL-Theme*

*Per Laufzeit-Log wurde im ESP-Beispiel bestätigt, dass unter anderem diese
Werte im Build ankommen:*

- *`LV_COLOR_DEPTH=16`*
- *`LV_USE_LOG=0`*
- *`LV_LOG_LEVEL=5`*
- *`LV_USE_OBJ_NAME=0`*
- *`LV_THEME_DEFAULT_DARK=0`*

*Zusätzlich ist bestätigt:*

- *Theme-Änderungen in `theme_project.c` wirken sichtbar auf dem ESP-Display*
- *der Dark Mode funktioniert grundsätzlich*
- *theme-neutrale Screens sind dafür die robustere Grundlage als fest hell
  voreingestellte Standardfarben*

*Für den aktuellen Stand des Projekts ist deshalb wichtig:*

- *feste Screen-Standardfarben können das Theme teilweise überlagern*
- *ein Dark Mode funktioniert am zuverlässigsten mit theme-neutralen Screens*

!!! note "Hinweis"
    Welche Dateien tatsächlich entstehen, hängt von der Projektvorlage und vom
    aktuellen Unterstützungsgrad der verwendeten Widgets und Properties ab.

## Was der Generator übernimmt

Der Generator übernimmt vor allem:

- die Übersetzung des Screen-Modells in Code
- die Erzeugung der Objekt-Hierarchie
- die Anwendung unterstützter Properties
- die Benennung und Bereitstellung exportierter Handles
- im RTOS-Pfad die Ableitung von Events, Aktionen und Update-Zugängen

## Was der Generator nicht ersetzt

Die Code-Generierung ersetzt nicht die gesamte Zielanwendung.

Sie liefert den UI-bezogenen Teil und, je nach Vorlage, zusätzliche
Vertrags- und Nachrichtenstrukturen. Die eigentliche MCU-Anwendung,
Controllerlogik, Task-Struktur und Systemintegration bleiben weiterhin Teil
des Zielprojekts.

Gerade deshalb sind ergänzende Zielsystem-Beispiele sinnvoll: Sie zeigen, wie
der generierte Code in einer realen MCU-Anwendung weiterverwendet wird.

!!! warning "Achtung"
    Die Code-Generierung ersetzt nicht die vollständige Zielanwendung. MCU-
    Logik, Task-Struktur, Integrationscode und fachliches Verhalten bleiben
    weiterhin Teil des Zielprojekts.

## Unterstützungsgrad

Welche Widgets und Properties tatsächlich generiert werden können, hängt vom
aktuellen Unterstützungsgrad des Werkzeugs ab.

Nicht jedes im Metamodell vorhandene Widget ist automatisch in allen
Generatorpfaden vollständig umgesetzt. Deshalb ist die sichtbare Kennzeichnung
unterstützter und nicht vollständig unterstützter Bereiche auch für die
Code-Generierung wichtig.

## Zusammenfassung

Die Code-Generierung in MCU UI Studio for LVGL verfolgt das Ziel, den Weg vom
Screen-Modell zum verwendbaren Quelltext transparent und nachvollziehbar zu
machen.

Je nach Vorlage reicht sie dabei:

- vom direkten LVGL-C-Code
- bis zu einer strukturierten Contract- und Nachrichtenebene

Damit bildet sie die Brücke zwischen Editor, Simulator und Zielprojekt.

*Für die aktuelle Embedded-Arbeit ist zusätzlich verifiziert:*

- *der native SDL-Simulator arbeitet im Projekt ebenfalls mit `16 Bit`*
- *das ESP-Display nutzt konkret `RGB565`*

*Das ist relevant für die visuelle Bewertung von Farben, weil kräftige Farbtöne
auf `RGB565` sichtbar anders wirken können als im Desktop-Preview.*
