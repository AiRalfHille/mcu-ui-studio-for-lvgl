# Plattformen

Dieser Bereich sammelt **plattformbezogene Build-, Setup- und Laufzeithinweise**.

Wichtig:

- der eigentliche Quellcode bleibt gemeinsam
- plattformspezifische Hinweise, Skripte und Besonderheiten werden hier getrennt gesammelt

## Ziel

Die Struktur soll helfen, das Projekt sauber auf mehreren Desktop-Plattformen zu betreiben:

- macOS ARM64
- Windows
- Linux

## Inhalt pro Plattform

Jedes Plattformverzeichnis kann spaeter enthalten:

- `README.md`
- Setup-Schritte
- benoetigte Werkzeuge
- Build-Befehle
- bekannte Probleme
- kleine Hilfsskripte

## Aktueller Stand

Die Plattformverzeichnisse sind zunaechst als vorbereiteter Rahmen angelegt.

Geplant ist, dort spaeter die echten Erfahrungen und Build-Hinweise aus den jeweiligen Plattformtests abzulegen.

## Empfohlene Artefakt-Trennung

Der Quellstand kann gemeinsam genutzt werden, Build- und Release-Artefakte
sollten jedoch pro Plattform getrennt bleiben.

Empfohlene Konvention:

- `platforms/macos-arm64/app-publish`
- `platforms/macos-arm64/simulator`
- `platforms/macos-arm64/release`
- `platforms/windows/app-publish`
- `platforms/windows/simulator`
- `platforms/windows/release`
- `platforms/linux/app-publish`
- `platforms/linux/simulator`
- `platforms/linux/release`

Fuer den nativen Simulator gilt zusaetzlich:

- `native/lvgl_simulator_host/build-macos`
- `native/lvgl_simulator_host/build-windows`

Damit werden Konflikte zwischen macOS- und Windows-Builds vermieden, vor allem
bei CMake-Caches, Generator-Dateien und nativen Toolchain-Artefakten.
