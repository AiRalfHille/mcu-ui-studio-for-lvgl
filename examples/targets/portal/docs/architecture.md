# Architekturidee

## Ziel

Die Runtime soll fachlich einfach bleiben, aber die Grundarchitektur vollstaendig
 abbilden.

## Module

- `main`
  startet System, Tasks und Queues
- `display`
  UI- und Anzeigeebene
- `controller`
  Entscheidungs- und Vermittlungsebene
- `fieldbus`
  Aussenwelt und fachliche Wahrheit
- `message`
  gemeinsame Queue-Nachrichten

## Trennung von Template und Generator

- `main/` enthaelt die handgeschriebene Runtime-Architektur
- `main/generated/` enthaelt kopierte oder exportierte UI-Dateien aus dem Editor
- generierter UI-Code soll im ESP32-Projekt klar als UI-Unit erkennbar sein, z. B. `ui_start`
- die Bezeichnung `main` ist fuer das Runtime-Hauptmodul reserviert und soll nicht fuer generierte Screens verwendet werden

## Kommunikationsrichtung

- `display -> controller`
- `controller -> fieldbus`
- `fieldbus -> controller`
- `controller -> display`

Praktisch heisst das im Beispiel:

- generierte `ui_start_event.c`-Bindings schreiben `app_message_t` nach `control_queue`
- `controller.c` prueft Aktionen und leitet sie nach `fieldbus_queue` weiter
- `fieldbus.c` meldet bestaetigte Stati wieder an `control_queue`
- `display.c` setzt ausschliesslich `display_message_t` ueber `display_queue` auf konkrete UI-Ziele um

## Event- und Message-Gedanke

- generierte Event-Dateien bleiben pro Screen oder UI-Unit getrennt
- Queue-Messages bleiben Teil der Runtime-Architektur
- generierter Event-Code darf Queue-Senden vorbereiten oder beispielhaft zeigen, soll aber nicht die zentrale Message-Definition besitzen

Wichtig fuer das RTOS-Beispiel:

- `action` beschreibt die fachliche Bedeutung eines Events
- `parameter` ist ein optionaler Zusatzwert aus dem Event-Editor
- `value` transportiert den aktuellen Laufzeitwert von Widgets wie `slider`, `bar`, `arc` oder `spinbox`

Damit kann ein Widget-Ereignis gleichzeitig einen technischen Zusatzparameter
und einen echten Nutzwert an den Controller senden.

## Leitgedanke

Auch wenn das Projekt anfangs kaum echte Hardwarelogik enthaelt, soll die
architektonische Rollenverteilung von Anfang an sichtbar und einuebbar sein.
