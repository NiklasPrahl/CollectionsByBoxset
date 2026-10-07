# Collections by Boxset

Ein Plugin für Jellyfin 12.2, das aus Ordnern mit dem Zusatz `[boxset]` im Namen automatisch Sammlungen erstellt.

Es ist ein Fork von [CollectionsByFolder](https://github.com/Solas79/folder-collection) von Solas79, angepasst an Jellyfin 12.2 und .NET 10.

## Wie es funktioniert

Das Plugin sucht in deinen Filmordnern nach Ordnern, deren Name auf `[boxset]` endet. Jeder dieser Ordner wird zu einer Sammlung, die alle Filme darin enthält, auch aus Unterordnern.

```text
Filme/
└── Alien [boxset]/
    ├── Alien (1979)/
    │   └── Alien.mkv
    └── Aliens (1986)/
        └── Aliens.mkv
```

Aus diesem Beispiel entsteht die Sammlung **Alien** mit beiden Filmen. Der Zusatz `[boxset]` erscheint nur im Ordnernamen, nicht im Namen der Sammlung. Deine Ordner und Dateien werden nie verändert.

Die Sammlungen entstehen auf jedem Jellyfin-Server aus den dort vorhandenen Ordnern. Wenn du dieselbe Ordnerstruktur auf einen zweiten Server synchronisierst, bekommst du dort dieselben Sammlungen, ohne sie von Hand anzulegen.

## Regeln

- Bei verschachtelten Markierungen gilt der nächstgelegene Ordner mit `[boxset]` oberhalb des Films.
- Das Plugin ändert nur Sammlungen, die es selbst erstellt hat. Manuell angelegte Sammlungen bleiben unberührt.
- Gibt es bereits eine nicht verwaltete Sammlung mit demselben Namen, oder ergeben zwei Ordner denselben Namen, wird dieser Fall übersprungen und im Log gemeldet.
- Filme, die nicht mehr im markierten Ordner liegen, werden aus der Plugin-Sammlung entfernt. Das lässt sich in den Einstellungen abschalten.
- Vorhandene Bilder im markierten Ordner (zum Beispiel `poster.jpg`, `folder.jpg`, `backdrop.jpg`) werden in die Sammlung übernommen. Bereits vorhandene Bilder werden nicht überschrieben.

## Installation

1. Öffne in Jellyfin **Dashboard › Plugins › Repositories** und füge ein neues Repository hinzu.
2. Trage diese URL ein:

   ```text
   https://github.com/NiklasPrahl/CollectionsByBoxset/releases/latest/download/manifest.json
   ```

3. Öffne den **Katalog**, installiere „Collections by Boxset“ und starte Jellyfin neu.

Die URL funktioniert erst, nachdem im Repository ein Release veröffentlicht wurde.

## Einrichtung

Öffne **Dashboard › Plugins › Collections by Boxset**.

| Einstellung | Bedeutung |
|---|---|
| Erlaubte Pfade | Ordner, in denen gesucht wird, einer pro Zeile. Leer bedeutet: alle Bibliothekspfade. |
| Ausgeschlossene Pfade | Ordner, die ignoriert werden. |
| Mindestanzahl Filme | Ordner mit weniger Filmen werden übersprungen. |
| Filme entfernen | Entfernt Filme aus Plugin-Sammlungen, wenn sie nicht mehr im Ordner liegen. |
| Bilder übernehmen | Kopiert vorhandene Bilder aus dem Ordner, ohne Bestehendes zu überschreiben. |

Mit **Jetzt scannen** startest du den Abgleich sofort. Zusätzlich gibt es unter **Dashboard › Geplante Aufgaben** die Aufgabe „Sammlungen aus [boxset]-Ordnern aktualisieren“, der du einen Zeitplan geben kannst.

Meldungen des Plugins stehen im Jellyfin-Log mit dem Kürzel `CBB`.

## Selbst bauen

Benötigt .NET-10-SDK.

```bash
dotnet test tests/Jellyfin.Plugin.CollectionsByBoxset.Tests/Jellyfin.Plugin.CollectionsByBoxset.Tests.csproj -c Release
dotnet build src/Jellyfin.Plugin.CollectionsByBoxset/Jellyfin.Plugin.CollectionsByBoxset.csproj -c Release -o build
```

Die fertige Datei liegt danach unter `build/Jellyfin.Plugin.CollectionsByBoxset.dll`. Zum manuellen Einsetzen kopierst du sie in einen Unterordner des Jellyfin-Plugin-Verzeichnisses und startest Jellyfin neu.

## Lizenz

GPL-3.0-or-later. Siehe [LICENSE](LICENSE) und [NOTICE.md](NOTICE.md).
