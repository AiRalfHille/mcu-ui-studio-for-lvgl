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

## Event- und Message-Gedanke

- generierte Event-Dateien bleiben pro Screen oder UI-Unit getrennt
- Queue-Messages bleiben Teil der Runtime-Architektur
- generierter Event-Code darf Queue-Senden vorbereiten oder beispielhaft zeigen, soll aber nicht die zentrale Message-Definition besitzen

## Leitgedanke

Auch wenn das Projekt anfangs kaum echte Hardwarelogik enthaelt, soll die
architektonische Rollenverteilung von Anfang an sichtbar und einuebbar sein.
