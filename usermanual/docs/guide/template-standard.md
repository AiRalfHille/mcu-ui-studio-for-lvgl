# Projektvorlage: Standard

MCU UI Studio for LVGL unterstützt unterschiedliche Projektvorlagen, um den
Editor an verschiedene Zielstrukturen anzupassen.

Die Vorlage **Standard** ist der einfachere und direktere Arbeitsmodus. Sie
eignet sich für Projekte, bei denen der Screen, seine Widgets und der daraus
generierte LVGL-C-Code ohne zusätzliche Contract-Struktur oder
RTOS-spezifische Nachrichtenpfade genutzt werden sollen.

## Zweck der Standard-Vorlage

Die Standard-Vorlage ist für klassische LVGL-Projekte gedacht.

Sie eignet sich besonders dann, wenn:

- Screens direkt bearbeitet werden sollen
- der Fokus auf Layout, Widgets und Properties liegt
- der generierte LVGL-C-Code möglichst direkt weiterverwendet werden soll
- kein zusätzlicher Nachrichten- oder Contract-Mechanismus im Vordergrund
  steht

Damit ist sie die naheliegende Wahl für den Einstieg und für viele
Anwendungsfälle ohne zusätzliche Systemabstraktion.

!!! tip "Tipp"
    Die Standard-Vorlage ist meist die beste Wahl, wenn zunächst Layout,
    Widgets und lesbarer LVGL-C-Code im Vordergrund stehen.

## Charakter der Vorlage

Die Standard-Vorlage folgt einem direkten Arbeitsmodell:

- Screen im Editor bearbeiten
- Struktur und Properties festlegen
- Vorschau im Simulator prüfen
- LVGL-C-Code generieren

Der Schwerpunkt liegt dabei auf dem sichtbaren Screen-Aufbau und auf der
nachvollziehbaren Ableitung des generierten Codes aus dem Editorzustand.

## Typischer Einsatz

Die Standard-Vorlage passt besonders gut zu:

- kleineren und mittleren LVGL-Projekten
- einfachen bis moderaten UI-Strukturen
- Projekten mit direkter Anbindung an eine Display-Schicht
- Lern- und Prototyping-Szenarien
- Anwendungsfällen, in denen der generierte Code gut lesbar und direkt
  einsetzbar sein soll

## Verhältnis zur Code-Generierung

Auch in der Standard-Vorlage basiert die Arbeit auf demselben internen
Screen-Modell wie im restlichen Werkzeug.

Der Unterschied liegt nicht im Modell selbst, sondern in der Art, wie das
Projekt fachlich gedacht ist:

- direkter
- weniger formalisiert
- ohne den Schwerpunkt auf Nachrichten- oder Contract-Strukturen

Dadurch bleibt die Standard-Vorlage besonders gut geeignet für Projekte, bei
denen der Screen und sein LVGL-C-Code im Vordergrund stehen.

## Relevante Attributgruppen

Auch in der Standard-Vorlage gibt es einige Attributgruppen, die über reine
Layout- oder Stilwerte hinausgehen.

Zur **Data**-Gruppe gehören insbesondere:

- `id`
- `useUpdate`

Diese Angaben helfen dabei, Widgets eindeutig zu benennen und gezielt für
spätere Aktualisierung oder Weiterverarbeitung vorzubereiten.

Zusätzlich gibt es im Standard-Umfeld eine Gruppe **MCU-Integration** mit
Attributen wie:

- *`callback`*
- *`action`*
- *`parameter`*
- `eventGroup`
- `eventType`
- `useMessages`

Diese Angaben gehören funktional bereits über die reine Widget-Beschreibung
hinaus und dienen dazu, die spätere MCU-seitige Integration strukturierter
vorzubereiten.

*Im Editor werden diese technischen Eigenschaften ebenso wie `id` und
`useUpdate` grün und fett hervorgehoben. Dadurch lässt sich schneller erkennen,
welche Felder nicht nur die sichtbare Widgetbeschreibung betreffen, sondern
direkt auf Generatoren oder Zielsystem-Integration wirken.*

## *Wie Event-Informationen im Standard-Pfad verwendet werden*

*Im aktuellen Standard-Generator werden Event-Angaben nicht mehr nur lose als
technische Callback-Information behandelt, sondern in einer MCU-tauglichen Form
in das generierte Binding uebernommen.*

*Dabei ist die Rollenverteilung wichtig:*

- *`eventGroup` erzeugt eine gemeinsame Dispatcher-Funktion pro Gruppe*
- *`eventType` unterscheidet innerhalb dieser Gruppe fachliche Unterfaelle*
- *`action` beschreibt die fachliche Hauptbedeutung des Events*
- *`parameter` wird als getypter Zusatzwert uebernommen*
- *Wert-Widgets wie `slider`, `bar`, `arc` oder `spinbox` liefern zusaetzlich
  ihren aktuellen Laufzeitwert mit Typinformation*

*Dadurch kann der generierte Standard-Code fuer MCU-Projekte bereits klar
fachlich gelesen werden:*

- *welches Objekt ein Ereignis ausgeloest hat*
- *welche fachliche `action` gemeint ist*
- *welcher optionale `parameter` mitgegeben wird*
- *ob zusaetzlich ein aktueller Widgetwert vorliegt*

*Der LVGL-Ausloeser selbst, zum Beispiel `clicked` oder `released`, bleibt im
Standard-Pfad vor allem eine interne Generator-Information fuer
`lv_obj_add_event_cb(...)`. Fuer die MCU-seitige Anwendungslogik stehen
stattdessen `action`, `parameter`, `eventGroup`, `eventType` und gegebenenfalls
der Laufzeitwert im Vordergrund.*

## *Dispatcher-Verhalten im Standard-Pfad*

*Wenn mehrere Events dieselbe `eventGroup` verwenden, erzeugt der Generator
genau eine gemeinsame Dispatcher-Funktion fuer diese Gruppe.*

*Innerhalb dieser Funktion wird dann schrittweise unterschieden:*

- *zuerst nach dem tatsaechlichen LVGL-Ereignis wie `CLICKED` oder `RELEASED`*
- *danach nach `eventType`*
- *und bei Bedarf zusaetzlich nach dem konkreten Objekt*

*Das bedeutet fuer MCU-Entwickler: Eine `eventGroup` ist keine dekorative
Angabe, sondern steuert direkt die Struktur des generierten C-Codes.*

## *Getypter Parameter und Laufzeitwert*

*Der Standard-Pfad unterscheidet jetzt ebenso wie der RTOS-Pfad zwischen zwei
Wertarten:*

- *einem freien Event-`parameter`*
- *dem aktuellen Laufzeitwert eines Wert-Widgets*

*Beides wird im generierten Binding typisiert vorbereitet. Dadurch kann zum
Beispiel ein `slider`:*

- *einen zusaetzlichen Textparameter wie `WARNING` mitgeben*
- *und gleichzeitig seinen aktuellen numerischen Wert separat liefern*

*Das ist fuer ESP32- und andere MCU-Projekte besonders hilfreich, weil
fachliche Zusatzinformation und aktueller Widgetwert sauber getrennt bleiben.*

## Wann Standard die richtige Wahl ist

Die Vorlage **Standard** ist in der Regel die richtige Wahl, wenn:

- ein Projekt neu begonnen wird
- zunächst die UI-Struktur im Vordergrund steht
- der Editor ohne zusätzliche Systemlogik genutzt werden soll
- der generierte LVGL-C-Code direkt im Zielprojekt eingebunden werden soll

Für viele Arbeitsabläufe ist sie deshalb die natürliche Ausgangsbasis.

## Abgrenzung zur RTOS-Messages-Vorlage

Im Unterschied zur Vorlage **RTOS-Messages** verlangt die Standard-Vorlage
keinen stärker formalisierten Ansatz für Ereignisse, ids und nachgelagerte
UI-MCU-Kommunikation.

*Trotzdem ist der aktuelle Standard-Pfad bereits deutlich mehr als nur roher
LVGL-Callback-Code: Er transportiert `action`, getypten `parameter` und
gegebenenfalls Laufzeitwerte so, dass eine MCU-seitige Weiterverarbeitung ohne
Informationsverlust moeglich bleibt.*

Die Vorlage **RTOS-Messages** ist deshalb eher für Projekte gedacht, in denen
die UI systematischer in eine Nachrichten- oder Contract-Struktur eingebunden
werden soll.

Die Standard-Vorlage bleibt dagegen der direktere und einfachere Weg.

## Zielsystem-Beispiele

Ergänzend zu den Editor-Beispielen können zu dieser Vorlage auch einfache
Zielsystem-Projekte gehören, die zeigen, wie der generierte Code in ein
konkretes MCU-Projekt eingebunden wird.

Der Zweck solcher Projekte ist nicht, vollständige Referenzanwendungen zu
liefern, sondern den Übergang vom Editor in ein reales Zielsystem möglichst
klar und nachvollziehbar zu machen.
