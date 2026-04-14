# Erste Schritte

Dieses Kapitel beschreibt einen ersten einfachen Arbeitsdurchlauf mit
MCU UI Studio for LVGL.

Ziel ist nicht, bereits alle Funktionen des Werkzeugs im Detail zu erklären,
sondern in kurzer Zeit einen vollständigen Ablauf kennenzulernen:

- Projektverzeichnis wählen
- Screen anlegen oder laden
- erste Widgets platzieren
- Vorschau prüfen
- Screen speichern und Code generieren

## 1. Projektverzeichnis öffnen

Beim Start der Anwendung wird zunächst ein Projektverzeichnis gewählt.

Dafür öffnet sich ein eigener Verzeichnisdialog innerhalb der Anwendung.
Dort kann:

- ein bestehender Projektordner ausgewählt werden
- ein neuer Ordner angelegt werden
- ein leerer Ordner für ein neues Projekt verwendet werden

Wird ein leerer Ordner ausgewählt, legt die Anwendung dort die
Projektstruktur an. Dazu gehören insbesondere:

- die Projektdatei `*.lvglproj`
- der Ordner `screens/`
- der Ordner `build/`

Wird ein Ordner mit einer bestehenden Projektdatei verwendet, arbeitet der
Editor mit diesem Projekt weiter.

*Die Datei- und Ordnerauswahl nutzt wieder native Plattformdialoge. Dieser
Stand wurde unter macOS lokal nach dem Update der Avalonia-Desktop-Pakete auf
`12.0.1` erneut verifiziert.*

*`Avalonia.Controls.WebView` bleibt aktuell noch auf `12.0.0`.*

!!! tip "Tipp"
    Für den ersten Durchlauf ist ein leerer Projektordner oder ein sehr
    einfaches Beispielprojekt meist der angenehmste Einstieg.

## 2. Ersten Screen anlegen oder laden

Nach dem Öffnen des Projektverzeichnisses kann ein vorhandener Screen geladen
oder eine neue Screen-Datei angelegt werden.

Screens werden im Projekt unter `screens/` verwaltet. Intern basiert ein
Screen auf einem JSON-Modell, das vom Editor, vom Simulator und von der
Code-Generierung gemeinsam verwendet wird.

Für den ersten Einstieg ist es sinnvoll, einen einfachen Screen mit wenigen
Widgets zu verwenden.

## 3. Die Oberfläche kurz einordnen

Die Anwendung ist in mehrere Arbeitsbereiche gegliedert:

- **Werkzeugkasten** auf der linken Seite
- **Strukturbaum** in der Mitte
- **Properties** rechts neben dem Strukturbaum
- **Diagnosebereich** im unteren Teil
- **Simulator-Vorschau** im rechten Hauptbereich
- **Handbuchbereich** zusätzlich auf der rechten Seite

Im Werkzeugkasten werden verfügbare Widgets angezeigt. Im Strukturbaum ist der
aktuelle Aufbau des Screens sichtbar. Die Properties zeigen die Eigenschaften
des aktuell markierten Elements.

## 4. Erste Widgets platzieren

Ein typischer erster Screen besteht aus wenigen einfachen Widgets, zum Beispiel:

- `view`
- `button`
- `label`
- `slider`

Widgets werden über den Werkzeugkasten in den Screen oder in einen passenden
Container eingefügt. Danach können sie im Strukturbaum ausgewählt und über den
Property-Editor angepasst werden.

Für die ersten Schritte sind vor allem diese Angaben hilfreich:

- `id`
- Position und Größe
- Text bei Labels und Buttons
- einfache Widget-spezifische Werte wie Slider- oder Bar-Werte

## 5. Auswahl und Vorschau

Die Vorschau im Simulator zeigt den aktuellen Screenzustand.

Wenn im Strukturbaum ein Element markiert wird, wird das zugehörige Widget im
Simulator ebenfalls hervorgehoben. Dadurch lässt sich schnell erkennen, welches
Objekt gerade bearbeitet wird.

Das ist besonders hilfreich bei komplexeren Screens mit mehreren ähnlichen
Widgets oder verschachtelten Views.

## 6. Unterstützte und nicht vollständig unterstützte Inhalte

Der Editor kennzeichnet Widgets und Properties, die noch nicht vollständig
unterstützt sind.

Nicht vollständig unterstützte Bereiche werden im Editor farblich markiert.
Dadurch soll früh sichtbar werden, welche Elemente bereits verlässlich im
Simulator- und Displaypfad funktionieren und wo noch Einschränkungen bestehen.

Der Grundsatz dabei ist:

- was als unterstützt markiert ist, soll im Simulator und im Displaypfad
  zusammenpassen
- was noch nicht vollständig unterstützt ist, soll nicht stillschweigend den
  Eindruck erwecken, bereits vollständig nutzbar zu sein

!!! warning "Achtung"
    Nicht vollständig unterstützte Widgets oder Properties sollten nicht erst
    im Zielsystem ausprobiert werden. Die Markierung im Editor ist bewusst als
    früher Hinweis gedacht.

## 7. Speichern und Code generieren

Nach den ersten Änderungen kann der Screen gespeichert werden.

Anschließend lässt sich die Code-Generierung ausführen. Der erzeugte Code
orientiert sich an LVGL-C-Code und bildet den aktuellen Screenzustand für den
weiteren Einsatz im Zielprojekt ab.

Die generierten Dateien werden im Projektkontext abgelegt und können danach in
den weiteren MCU- oder Anwendungsablauf eingebunden werden.

## 8. Wie es danach weitergeht

Nach diesem ersten Durchlauf sind vor allem die folgenden Kapitel sinnvoll:

- **Benutzeroberfläche**, um die Arbeitsbereiche genauer zu verstehen
- **Konzepte**, um Modell, Vorschau und Generierung besser einzuordnen
- **Widgets**, um einzelne Elemente gezielt nachzuschlagen
- **Beispiele**, um typische Screen-Strukturen im Zusammenhang zu sehen

Damit ist der erste Arbeitsablauf abgeschlossen und die grundlegende Struktur
des Werkzeugs sichtbar.
