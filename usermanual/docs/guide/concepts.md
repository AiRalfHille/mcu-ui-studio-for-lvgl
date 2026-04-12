# Konzepte

Dieses Kapitel beschreibt die grundlegenden Ideen, auf denen
MCU UI Studio for LVGL aufbaut.

Es geht dabei nicht um jede einzelne Funktion, sondern um die
Arbeitsprinzipien des Werkzeugs und um die Frage, wie Editor, Vorschau und
Code-Generierung zusammenhängen.

## Ein gemeinsames Modell

Ein zentrales Konzept des Werkzeugs ist ein gemeinsames internes Modell für
Screens und Widgets.

Ein Screen wird nicht nur als sichtbare Oberfläche verstanden, sondern als
strukturierte Beschreibung mit:

- Elementtyp
- Attributen
- Events
- Kind-Elementen

Dieses Modell wird intern als JSON-basierte Struktur verwaltet. Dadurch bleibt
der Aufbau eines Screens nicht nur visuell, sondern auch technisch
nachvollziehbar.

## Ein Modell für mehrere Pfade

Dasselbe Modell dient nicht nur dem Editor, sondern mehreren Bereichen der
Anwendung gleichzeitig.

Es bildet die Grundlage für:

- den Strukturbaum im Editor
- den Property-Editor
- den nativen Preview- und Simulatorpfad
- die Code-Generierung

Dadurch soll vermieden werden, dass verschiedene Teile des Werkzeugs
unabhängig voneinander mit unterschiedlichen Interpretationen desselben
Screens arbeiten.

## Metamodell statt freier Beliebigkeit

Das Werkzeug erlaubt nicht beliebige Strukturen ohne Regeln, sondern arbeitet
mit einem eigenen LVGL-orientierten Metamodell.

Dieses Metamodell beschreibt:

- welche Widgets bekannt sind
- welche Properties zu einem Widget gehören
- welche Wertearten verwendet werden
- welche Kind-Elemente erlaubt sind
- welche Bereiche aktuell unterstützt sind

Damit wird der Editor bewusst geführt. Ziel ist nicht maximale Freiheit um
jeden Preis, sondern eine Oberfläche, die technische Klarheit und
Vorhersehbarkeit unterstützt.

## Sichtbare Unterstützung statt stiller Annahmen

Ein weiteres Grundprinzip ist die sichtbare Kennzeichnung des
Unterstützungsgrads.

Widgets und Properties werden nicht einfach nur angezeigt, sondern zusätzlich
als unterstützt oder noch nicht vollständig unterstützt bewertet.

Der Hintergrund dafür ist einfach:

- Anwender sollen möglichst früh erkennen, was verlässlich nutzbar ist
- der Editor soll keine falsche Sicherheit erzeugen
- Unterschiede zwischen Modell, Preview und Zielpfad sollen nicht erst spät
  auffallen

Deshalb werden nicht vollständig unterstützte Bereiche im Editor bewusst
kenntlich gemacht.

!!! warning "Achtung"
    Ein sichtbares Widget im Editor bedeutet nicht automatisch, dass es in
    allen Pfaden bereits vollständig unterstützt ist. Maßgeblich ist der
    durchgängige Zusammenhang zwischen Modell, Vorschau und Generierung.

## Vorschau und Display sollen zusammenpassen

Die Simulator-Vorschau ist nicht nur als optische Annäherung gedacht.

Ein wichtiges Ziel des Projekts ist, dass das, was im Simulator als
unterstützt sichtbar ist, auch im Displaypfad sinnvoll wieder auftaucht.

Daraus ergibt sich ein harter Maßstab:

- Unterstützung soll nicht nur auf dem Papier existieren
- ein Widget oder eine Property gilt erst dann als wirklich unterstützt,
  wenn die Unterstützung in den relevanten Pfaden zusammenpasst

Dieses Prinzip ist wichtig, damit Anwender nicht im Zielsystem nach Fehlern
suchen, die in Wirklichkeit aus uneinheitlicher Tool-Unterstützung entstehen.

## LVGL als Ziel, nicht als Black Box

MCU UI Studio for LVGL basiert fachlich auf LVGL.

Das Werkzeug versteht LVGL dabei nicht nur als externes Zielsystem, sondern
bildet zentrale Strukturen der Bibliothek im eigenen Modell nach. Ziel ist,
den Weg von der Screen-Beschreibung bis zum erzeugten LVGL-C-Code möglichst
transparent zu halten.

Damit unterscheidet sich der Ansatz bewusst von Werkzeugen, die stärker auf
proprietäre Zwischenmodelle oder schwer nachvollziehbare Generatorpfade
setzen.

## Erweiterte Strukturinformationen

Neben allgemeinen Widget-Properties enthält das Modell zusätzliche
Strukturinformationen, die für den späteren Einsatz im Projekt hilfreich sind.

Dazu gehören beispielsweise:

- `id`
- `useUpdate`

Solche Angaben helfen dabei, Elemente nicht nur visuell zu platzieren, sondern
sie auch für spätere Aktualisierung, Zuordnung und systematische
Weiterverarbeitung greifbar zu machen.

## Perspektive auf Generierung und Bindung

Das Werkzeug endet nicht bei der visuellen Bearbeitung eines Screens.

Die weitere Richtung des Projekts umfasst auch:

- klarere Generatorpfade für MCU-Projekte
- stärker typisierte Übergaben
- strukturierte Contracts zwischen UI und Anwendungslogik
- perspektivisch auch weitergehende Bindings

Nicht jede dieser Ideen ist bereits vollständig ausgebaut, aber sie gehören
zur inhaltlichen Ausrichtung des Projekts.

!!! info "In Entwicklung"
    Weitergehende Contracts, stärker typisierte Übergaben und spätere
    Bindings gehören zur Projektperspektive, sind aber nicht in allen Teilen
    bereits vollständig umgesetzt.

## Zusammenfassung

Die wichtigsten Konzepte von MCU UI Studio for LVGL sind:

- ein gemeinsames JSON-basiertes Modell
- ein geführtes LVGL-orientiertes Metamodell
- sichtbare Kennzeichnung des Unterstützungsgrads
- ein enger Zusammenhang zwischen Editor, Vorschau und Code-Generierung
- ein transparenter Weg hin zu LVGL-C-Code

Diese Grundideen prägen den Aufbau des Werkzeugs stärker als einzelne
Oberflächendetails.
