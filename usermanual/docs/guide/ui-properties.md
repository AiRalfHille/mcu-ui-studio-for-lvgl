# Benutzeroberfläche: Properties

Dieses Kapitel beschreibt den Property-Bereich der Anwendung.

![Properties](../assets/screenshots/Eigenschaften.png){ width="760" }

## Aufgabe des Property-Bereichs

Der Property-Bereich zeigt die Eigenschaften des aktuell markierten Elements.

Er ist die zentrale Stelle für die konkrete Bearbeitung eines Widgets oder
Containers. Hier wird festgelegt, wie ein Element aufgebaut ist, welche Werte
es trägt und wie es sich im Screen verhält.

!!! tip "Tipp"
    Der Property-Bereich ist am hilfreichsten, wenn zuerst das passende
    Element im Strukturbaum markiert wird. Danach lassen sich Änderungen
    gezielt und nachvollziehbar auf genau dieses Widget beziehen.

## 1. Bereichskopf

Im Kopf des Bereichs wird der Property-Editor als eigener Arbeitsbereich
gekennzeichnet.

Zusätzlich befinden sich dort Steuerelemente, die die Darstellung oder
Sortierung der Eigenschaften beeinflussen können.

## 2. Register und Gruppen

Der Property-Bereich ist in Register und Property-Gruppen gegliedert.

Typisch sind dabei:

- `Eigenschaften`
- `Events`

Innerhalb dieser Register werden die verfügbaren Angaben weiter in Gruppen wie
zum Beispiel `Data` oder `Layout` unterteilt.

Diese Struktur hilft dabei, auch bei vielen Eigenschaften die Übersicht zu
behalten.

## *Kennzeichnung technischer MCU-Properties*

*Der Editor hebt einige Properties bewusst hervor, weil sie nicht nur die
sichtbare Widgetbeschreibung betreffen, sondern direkt mit Generatoren,
Contracts oder MCU-Integrationspfaden zusammenhängen.*

*Aktuell werden diese Eigenschaften in beiden Tabs grün und fett dargestellt:*

- *`id`*
- *`useUpdate`*
- *`callback`*
- *`action`*
- *`parameter`*
- *`eventGroup`*
- *`eventType`*
- *`useMessages`*

*Diese Hervorhebung bedeutet:*

- *die Angabe ist technisch für Generatoren oder Integrationspfade relevant*
- *sie beschreibt nicht nur Layout, Stil oder einen sichtbaren Widgetwert*
- *sie sollte im Projektkontext bewusst und konsistent gepflegt werden*

*`id` gehört dabei ausdrücklich mit zu dieser Gruppe, weil sie nicht nur ein
allgemeiner Name ist, sondern in den Generatorpfaden als stabile Zuordnung für
Handles, Contracts und Updates verwendet wird.*

## *Bedeutung der Event-Properties fuer MCU-Entwickler*

*Gerade im Event-Bereich ist es wichtig, die technische und die fachliche
Bedeutung der Felder zu trennen.*

*Im aktuellen Stand gilt:*

- *`callback` beschreibt die technische Callback-Zuordnung*
- *`eventGroup` gruppiert mehrere Events zu einer gemeinsamen Dispatcher-
  Funktion*
- *`eventType` unterscheidet innerhalb einer Gruppe die fachlichen Unterfälle*
- *`action` beschreibt die fachliche Hauptbedeutung eines Events*
- *`parameter` beschreibt einen freien Zusatzwert*
- *`useMessages` steuert im Standard-Pfad, ob ein projektspezifischer
  Nachrichten-/Queue-Block vorbereitet werden soll*

*Wichtig dabei: Der LVGL-Ausloeser wie `clicked` oder `released` bleibt zwar
fuer die interne Callback-Registrierung relevant, wird aber nicht mehr als
zentrales fachliches Feld fuer die MCU-Logik behandelt. Fuer den generierten
Code sind stattdessen vor allem `action`, `parameter`, `eventGroup` und
`eventType` entscheidend.*

## 3. Bearbeitungsfelder

Der eigentliche Inhalt des Bereichs besteht aus Bearbeitungsfeldern für die
Properties des aktuell markierten Elements.

Dazu gehören je nach Widget unter anderem:

- Textfelder
- Zahlenfelder
- Auswahlfelder
- Boolesche Schalter

Welche Properties sichtbar sind, hängt vom ausgewählten Widgettyp ab.

## Verwendung im Arbeitsablauf

Im typischen Ablauf wird zuerst ein Element im Strukturbaum ausgewählt. Danach
werden im Property-Bereich:

- allgemeine Daten wie `id` geprüft
- Layout- und Größenwerte angepasst
- widget-spezifische Eigenschaften bearbeitet
- Event-Angaben ergänzt

Der Property-Bereich ist damit die wichtigste Stelle für die inhaltliche und
technische Feinarbeit an einem Screen-Element.

## Unterstützungsgrad

Nicht jede Property ist im aktuellen Stand für alle Pfade vollständig
umgesetzt.

Deshalb macht der Editor auch auf Property-Ebene sichtbar, welche Angaben
bereits verlässlich unterstützt werden und welche noch nicht vollständig in
Preview und Generierung zusammenpassen.

!!! note "Hinweis"
    Welche Properties tatsächlich verfügbar sind, hängt immer vom ausgewählten
    Widgettyp und vom aktuellen Unterstützungsgrad im Werkzeug ab.
