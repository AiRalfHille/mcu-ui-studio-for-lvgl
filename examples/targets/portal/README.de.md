# portal target runtime

Referenz- und Demoprojekt fuer `MCU UI Studio for LVGL`.

English version: [README.md](README.md)

Dieses Target-Projekt zeigt einen moeglichen Integrationsweg fuer vom Editor erzeugte
LVGL-Oberflaechen in einem ESP-IDF-Projekt mit FreeRTOS, Message-Queues und
klar getrennten Runtime-Modulen.

Wichtig:

- dieses Target-Projekt ist ein Template und keine universelle Pflichtarchitektur
- der Editor soll langfristig mehrere Integrationsstile ermoeglichen
- dieses Repo zeigt bewusst den RTOS-/Message-basierten Weg

Das Projekt richtet sich damit an Anwender, die sehen wollen, wie generierter
UI-Code sauber in eine Embedded-Runtime eingebunden werden kann, ohne dass die
Anwendungslogik direkt in LVGL-Callbacks landet.

## Wofuer dieses Projekt gedacht ist

Dieses Projekt ist gedacht als:

- Demo fuer `MCU UI Studio for LVGL`
- Referenz fuer eine RTOS-basierte Integration
- Ausgangspunkt fuer eigene Templates
- Beispiel dafuer, wie generierter Code und handgeschriebene Runtime getrennt bleiben koennen

Dieses Projekt ist nicht gedacht als:

- einzig gueltige Architektur fuer alle Targets
- Vorgabe fuer Systeme ohne RTOS
- vollstaendige Produktionsreferenz fuer jede Embedded-Anwendung

Andere Integrationsstile sind ausdruecklich ebenfalls denkbar, zum Beispiel:

- direkter Code in Event-Callbacks
- einfache Main-Loop ohne RTOS
- projektspezifische HAL-/Fieldbus-Integration

## Kerngedanke

Der Editor erzeugt UI-Code.

Die Runtime entscheidet, wie dieser Code in das eigentliche Embedded-Projekt
eingebunden wird.

In diesem Template bedeutet das:

- generierte UI-Dateien liegen unter `main/generated/`
- handgeschriebene Runtime-Dateien liegen unter `main/`
- der Datenaustausch zwischen Modulen laeuft ueber FreeRTOS-Queues
- die Fachlogik bleibt ausserhalb des generierten LVGL-Codes

## Architektur in diesem Template

Der Datenfluss folgt in diesem Projekt bewusst einem geschlossenen RTOS-Muster:

```text
Display (LVGL)
  -> control_queue
Controller
  -> fieldbus_queue
Fieldbus / Hardware
  -> control_queue
Controller
  -> display_queue
Display (LVGL)
```

Die Rollen sind dabei klar getrennt:

- `display`
  kapselt den Zugriff auf die generierten UI-Update-Funktionen
- `controller`
  trifft fachliche Entscheidungen und vermittelt zwischen Display und Hardware
- `fieldbus`
  fuehrt Hardwareaktionen aus und meldet Zustaende zurueck
- `machine`
  enthaelt die statische Zuordnung zwischen UI-Contract und Hardwareobjekten
- `message`
  definiert die Queue-Nachrichten

## Warum gerade diese Architektur

Dieses Template bevorzugt eine RTOS-/Message-Architektur, weil sie fuer viele
groessere MCU-Projekte gut nachvollziehbar ist:

- Events werden entkoppelt verarbeitet
- Display-Logik und Hardware-Logik bleiben getrennt
- Controller-Code muss keine direkten LVGL-Aufrufe kennen
- die Architektur ist fuer spaetere Erweiterungen stabiler als reine Callback-Logik

Das bedeutet aber nicht, dass jede Anwendung so aufgebaut werden muss.

## Projektstruktur

```text
main/
├── generated/        # Vom Editor erzeugte UI-Dateien
├── include/          # Header der handgeschriebenen Runtime
├── controller.c      # Fachlogik / Vermittlung
├── display.c         # Display-Runtime, Aufruf der generierten Update-API
├── display_font.c    # Laufzeit-Font fuer Umlaute
├── fieldbus.c        # Hardware-/GPIO-Schicht
├── machine.c         # Binding zwischen UI-Contract und Hardwareobjekten
├── message.c         # Queue-Erzeugung
├── main.c            # BSP-Start, Display-Start, Task-Erzeugung
└── Arial.ttf         # Eingebettete TTF-Font fuer Umlaute

docs/
├── architecture.md
└── ui-fieldbus-integration.md
```

## Generierter Code vs. Runtime-Code

Die zentrale Trennung dieses Projekts lautet:

- der Editor erzeugt Struktur, Layout, Event-Bindings und Update-Ziele
- die Runtime bestimmt, was diese Events fachlich bedeuten

Typische generierte Dateien:

- `ui_start.c`
- `ui_start.h`
- `ui_start_contract.h`
- `ui_start_event.c`
- `ui_start_update.c`

Typische handgeschriebene Dateien:

- `controller.c`
- `display.c`
- `fieldbus.c`
- `machine.c`
- `message.c`

## Was ein MCU-Entwickler in diesem Template aendert

Nach einer UI-Neugenerierung sind in diesem Projekt typischerweise diese Stellen
relevant:

1. `main/machine.c`
   hier wird der aktuelle UI-Contract auf reale Hardwareobjekte gemappt

2. `main/fieldbus.c`
   hier wird entschieden, was eine UI-Aktion an der Hardware ausloest

3. `main/controller.c`
   hier wird entschieden, welche Rueckmeldungen ans Display gehen

Der MCU-Entwickler muss damit nicht den generierten LVGL-Layout-Code von Hand
bearbeiten, sondern arbeitet ueber den Contract und die Runtime-Module.

## Typischer Laufzeitfluss

Beispiel:

1. Ein generierter Button loest ein LVGL-Event aus.
2. `ui_start_event.c` baut daraus eine typisierte Contract-Message und sendet sie in `control_queue`.
3. `controller.c` prueft oder verteilt die Anfrage weiter.
4. `fieldbus.c` fuehrt die Hardware-Aktion aus.
5. Eine Status-Meldung geht ueber den Controller zurueck.
6. `display.c` ruft die generierten `ui_start_update_*`-Funktionen auf.

So bleiben rohe LVGL-Details aus Controller- und Fieldbus-Logik heraus.

## Build

Das Projekt ist ein ESP-IDF-CMake-Projekt fuer `esp32p4`.

Wichtige Punkte:

- Ziel: `esp32p4`
- Framework: ESP-IDF
- UI: LVGL 9.4
- Referenz-Setup in VS Code: `.vscode/settings.json`

## Aktueller Scope von `RTOS-Messages`

Der aktuell getestete und belastbare Stand dieses Templates ist:

- ein Projekt
- ein aktiver generierter Screen
- ein zugehoeriger Contract, z. B. `ui_start_contract.h`

Mehrere Screens sind konzeptionell vorbereitet, weil pro Screen eigene Dateien
wie z. B. `ui_screen2_contract.h` erzeugt werden koennen.

Fuer diese Version gilt aber bewusst:

- der Ein-Screen-Fall ist der empfohlene und getestete Weg
- Mehrscreen-Projekte sind noch nicht systematisch validiert
- moegliche Kollisionen oder Integrationsfragen bei mehreren Screen-Contracts
  muessen spaeter noch sauber getestet werden

Zum Bauen kann das lokale Hilfsskript verwendet werden:

```bash
zsh ./run.sh build
zsh ./run.sh flash
zsh ./run.sh monitor
```

`run.sh` laedt die lokale ESP-IDF-Umgebung und ruft danach `idf.py` auf.

## Fonts / Umlaute

Die eingebaute LVGL-Montserrat-Font enthaelt in diesem Setup standardmaessig
nicht alle benoetigten deutschen Glyphen.

Deshalb wird in diesem Projekt `main/Arial.ttf` eingebettet und beim
Display-Start ueber `display_font.c` als Laufzeit-Font geladen.

## Hinweis zu `LV_ATTRIBUTE_FAST_MEM`

`CONFIG_LV_ATTRIBUTE_FAST_MEM_USE_IRAM` bleibt aktiv.

Fuer dieses Projekt wurde die LVGL-Definition von `LV_ATTRIBUTE_FAST_MEM`
lokal angepasst, damit keine konfligierenden `.iram1.x`-Sections aus
`IRAM_ATTR` entstehen.

Die lokale Anpassung liegt in:

- `managed_components/lvgl__lvgl/env_support/cmake/esp.cmake`

## Ausblick

Ein staerkeres Domain Model ist bewusst kein Kernziel von Version 1 dieses
Templates.

Der aktuelle Fokus ist:

- Editor erzeugt stabile UI-Dateien
- das Template zeigt eine nachvollziehbare RTOS-Integration
- der Anwender kann entscheiden, ob er spaeter direkter, einfacher oder
  domaenennaher integrieren moechte

Weitergehende Themen wie separate Domaenen-Metadaten oder Domain-Enums koennen
spaeter folgen, wenn sich dafuer echter Bedarf aus Anwendersicht zeigt.
