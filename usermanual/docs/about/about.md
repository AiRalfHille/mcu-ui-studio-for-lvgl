# Über dieses Handbuch

Dieses Handbuch begleitet MCU UI Studio for LVGL und richtet sich an Anwender,
die mit dem Tool arbeiten, Projekte anlegen, Screens bearbeiten und den
generierten LVGL-Code besser verstehen möchten.

## Aufbau

Das Handbuch ist in folgende Bereiche gegliedert:

- **Einführung** — Vorwort, Orientierung im Handbuch, Lizenz und
  Drittanbieter-Komponenten
- **Erste Schritte** — Einstieg in das Tool, erstes Projekt und erste
  Arbeitsschritte
- **Benutzeroberfläche** — Übersicht über die Arbeitsbereiche des Editors
- **Projektvorlagen** — die unterstützten Projektmodi und ihre Unterschiede
- **Widgets** — verfügbare Widgets, ihre Rolle im Editor und ihre Properties
- **Code-Generierung** — Aufbau und Bedeutung der erzeugten Dateien
- **Beispiele** — Orientierung an den mitgelieferten Beispielprojekten

## Konventionen

In diesem Handbuch werden folgende Hinweistypen verwendet:

!!! note "Hinweis"
    Ergänzende Information zum besseren Verständnis.

!!! tip "Tipp"
    Empfehlung für effizientes Arbeiten.

!!! warning "Achtung"
    Wichtiger Hinweis — bitte beachten.

!!! info "In Entwicklung"
    Diese Funktion ist noch nicht vollständig implementiert.

Code-Beispiele werden in einem eigenen Codeblock dargestellt:

```c
lv_obj_t *btn = lv_button_create(parent);
```

Dateinamen, Pfade, Properties und Bezeichner werden im Fließtext `so`
hervorgehoben.

## Wie dieses Handbuch am besten genutzt wird

Für den Einstieg ist es sinnvoll, mit den Kapiteln unter **Einführung** und
**Erste Schritte** zu beginnen. Danach hilft in der Regel die
**Benutzeroberfläche**, um die Struktur des Editors besser einzuordnen.

Die späteren Kapitel zu **Widgets**, **Projektvorlagen** und
**Code-Generierung** sind eher als Nachschlagebereiche gedacht. Sie müssen
nicht vollständig am Stück gelesen werden.

Die **Beispiele** sind als praktische Ergänzung gedacht. Sie zeigen den Umgang
mit dem Tool an konkreten Projekten und helfen dabei, die generierten
Artefakte besser mit dem Editorzustand zu verknüpfen.

## Versionsstand

Dieses Handbuch bezieht sich auf MCU UI Studio for LVGL im aktuellen
Projektstand mit LVGL `9.4`.
