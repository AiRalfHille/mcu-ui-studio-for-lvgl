# Benutzeroberfläche: Struktur

Dieses Kapitel beschreibt den Strukturbaum des aktuellen Screens.

![Strukturbaum](../assets/screenshots/Struktur.png){ width="520" }

## Aufgabe des Strukturbaums

Der Strukturbaum zeigt den logischen Aufbau des aktuellen Screens.

Er macht sichtbar:

- welche Widgets vorhanden sind
- wie sie verschachtelt sind
- welches Element gerade ausgewählt ist
- in welchem Zusammenhang ein Widget innerhalb des Screens steht

Der Strukturbaum ist damit die wichtigste Sicht auf die interne Ordnung des
Screens.

## 1. Bereichskopf

Im Kopf des Bereichs wird der Strukturbaum als eigener Arbeitsbereich
gekennzeichnet.

Zusätzlich befinden sich dort Steuerelemente für die Darstellung oder
Navigation innerhalb der Struktur.

## 2. Ausgewähltes Element

Im Baum selbst ist jeweils ein Element markiert, das aktuell bearbeitet wird.

Diese Auswahl ist wichtig, weil sie mit anderen Bereichen der Anwendung
zusammenhängt:

- Properties zeigen die Eigenschaften des markierten Elements
- der Simulator hebt das gewählte Widget hervor
- Diagnose und Bearbeitung beziehen sich auf diese Auswahl

## 3. Kindbeziehungen und Verschachtelung

Die Einrückung im Baum zeigt die Hierarchie der Widgets.

Dadurch wird erkennbar:

- welches Widget ein Kind eines anderen Widgets ist
- welche Container welche Inhalte tragen
- wie ein Screen logisch aufgebaut ist

Gerade bei komplexeren Screens ist diese Sicht wichtiger als die rein optische
Anordnung.

## 4. Drag & Drop

Der Strukturbaum unterstützt Drag & Drop in zwei typischen Formen:

- Ein Widget kann aus dem Werkzeugkasten in die Struktur gezogen werden.
- Ein vorhandenes Element kann innerhalb der Struktur an eine andere Stelle
  verschoben werden.

Beim Ziehen innerhalb der Struktur unterscheidet der Editor zwischen drei
Einfügearten:

- vor einem Element
- als Kind eines Elements
- nach einem Element

Dadurch lassen sich Hierarchien schnell anpassen, ohne ein Element neu
anlegen zu müssen.

Nicht jede Position ist dabei erlaubt. Der Editor berücksichtigt die im
Metamodell definierten Eltern-Kind-Regeln und verhindert unzulässige
Drop-Ziele automatisch.

## Verwendung im Arbeitsablauf

Im typischen Ablauf wird im Strukturbaum:

- ein Element ausgewählt
- seine Position in der Hierarchie geprüft
- die Beziehung zu Eltern- und Kind-Elementen nachvollzogen
- und bei Bedarf per Drag & Drop die Struktur angepasst

Der Strukturbaum ist damit die zentrale Sicht auf die innere Ordnung des
Screens.
