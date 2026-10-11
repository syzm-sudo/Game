# THE LAST PATIENT

Ein Unity-6-Projekt für den spielbaren Vertical Slice von **Kapitel 1 – Die Ankunft** aus deiner Vorlage. Daniel Carter kehrt 2013 nach Blackwood zurück, nachdem er eine Kassette mit der Stimme seiner verschwundenen Schwester Emily erhalten hat.

## Start auf deinem Windows-PC

1. Die Projekt-ZIP entpacken, beispielsweise nach `C:\Games\TheLastPatient`. Der Projektordner enthält direkt `Assets`, `Packages` und `ProjectSettings`.
2. Im Unity Hub **Projects → Add → Add project from disk** wählen und diesen Ordner hinzufügen. **Unity 6.6 (6000.6.5f1)** auswählen; diese Version entspricht deinem Screenshot.
3. Beim ersten Öffnen Paketinstallation und C#-Kompilierung abwarten. Internetzugang zu Unitys Paketservern ist dafür nötig. Nicht nur den `Assets`-Ordner in ein anderes Projekt kopieren.
4. Der Editor-Generator versucht beim ersten Öffnen automatisch, die Szene zu erstellen und zu öffnen. Falls die Szene nicht erscheint, **Tools → The Last Patient → Generate Game Prototype** ausführen. Falls Unity nach TextMeshPro-Ressourcen fragt: **Window → TextMeshPro → Import TMP Essential Resources**, danach den Generator erneut ausführen.
5. **Play** drücken und ins **Game**-Fenster klicken. Das Intro beginnt; mit **Esc** überspringen. Danach kannst du dich direkt bewegen. Es müssen keine Inspector-Referenzen von Hand zugewiesen werden.

Die Szene liegt anschließend unter `Assets/TheLastPatient/Generated/Scenes/Chapter01.unity`. Du kannst sie jederzeit über **Tools → The Last Patient → Open Chapter 1** öffnen.

## Steuerung

| Taste | Aktion |
| --- | --- |
| WASD / Maus | Bewegen / umsehen |
| Linke Shift-Taste | Sprinten |
| Linke Strg-Taste | Ducken, gedrückt halten |
| E | Objekt im Fadenkreuz benutzen |
| F | Taschenlampe umschalten |
| Leertaste | Dialogtext sofort anzeigen / nächste Zeile |
| J | Journal und Inventar |
| Esc | Cutscene überspringen / Dokument schließen / Pause |
| F5 / F9 | Speichern / Checkpoint laden |
| Mausrad | Längere Dokumente im Lesefenster scrollen |

## Spielablauf von Kapitel 1

- Kurzes Intro vor dem Krankenhaus; erste Mission: **„Finde einen Weg in das Krankenhaus.“**
- Die Eingangstür mit E öffnen und hineinlaufen. Die Tür verriegelt sich; das Hauptlicht fällt aus und Emily spricht über den Lautsprecher.
- Die grün markierte Taschenlampe auf dem Wagen rechts nehmen.
- Die Akte auf dem Empfangstresen links untersuchen. Im Dokumentfenster den **Schlüssel aus der Akte nehmen**.
- Das Tonband daneben abspielen. Es läuft automatisch mit deutschen Untertiteln; Leertaste beschleunigt den Dialog.
- Weiter den Flur entlanggehen. Der Watcher erscheint kurz; diese erste Begegnung ist noch keine Kampf- oder Verfolgungssituation.
- Die Tür zur Patientenstation mit E aufschließen und in den Trakt laufen. Die Abschlusssequenz beendet den Ausschnitt.

Die Taschenlampe verbraucht Batterie. Ersatzbatterien liegen auf dem zweiten Wagen rechts. Der Not-Dynamo lädt sie außerdem ausgeschaltet langsam auf, damit kein unlösbarer Dunkelheitszustand entsteht. Journal, Akte und Tonband bleiben erneut zugänglich.

## Enthalten

- First-Person-CharacterController mit Sprinten und Ducken, Kollisionsprüfung beim Aufstehen.
- New Input System, Maussteuerung und UI-Eingabe.
- Taschenlampe mit Batterie, Nachladen und Schatten.
- Raycast-Interaktion, Inventar, Schlüssel, animierte Türen, Dokumente und Tonband.
- Ereignisbasiertes `StoryManager`-System mit stabilen IDs und geordneten Missionszielen.
- ScriptableObjects für Kapitel, Ziele, Dialoge und Story-Ereignisse; separate Missionsanzeige.
- TextMeshPro-Untertitel mit Sprecher, Schreibmaschinen-Effekt, automatischem Fortschritt und Überspringen. Optionale AudioClips und Dialogentscheidungen werden vom System unterstützt.
- Geskriptete Intro-, Stromausfall-, Watcher- und Kapitelabschluss-Sequenzen mit Steuerungsrückgabe; Esc führt auch beim Überspringen den notwendigen Abschluss aus.
- Krankenhaus-Greybox mit Empfang, Flur, Nebenraum-Andeutungen und Patientenstation; URP-Materialien, Nebel, Notlicht und prozeduralen Geräuschen.
- JSON-Speichern, automatische Checkpoints nach Zielen, Laden, Pause und Neustart.
- Wiederverwendbare Prefabs für Spieler, Watcher, Türen, Taschenlampe, Akte und Tonbandgerät.
- Editor-Generator und Windows-Build-Menü.

## Umfang und Prüfstatus

**Dies ist Phase 1 plus der Kapitel-1-Vertical-Slice, wie am Ende deiner Vorlage verlangt.** Kapitel 2–5, die drei Enden, aktive Watcher-KI, Verstecke und die späteren Rätsel sind noch nicht implementiert. Die Watcher-Figur ist ein Platzhalter für die erste Erscheinung. Es gibt keine aufgenommenen Sprecherstimmen; vorhandene Dialog-AudioClips können später zugewiesen werden. Kamerasequenzen verwenden eigene Coroutines statt Timeline/Cinemachine. AI Navigation ist als Voraussetzung der nächsten Phase enthalten, wird in diesem Kapitel aber noch nicht verwendet.

In der Cloud ist **kein Unity-Editor** vorhanden. C#-Syntax, Projektstruktur, IDs, Assembly-Konfiguration und ZIP-Inhalt wurden statisch geprüft. **Unity-Kompilierung, Paketeinspielung, Szenengenerierung, Play-Modus, Windows-Build und 60-FPS-Ziel sind hier nicht ausgeführt oder bestätigt.** Das Startverfahren muss deshalb in deinem Editor geprüft werden. Details stehen in [VALIDATION.md](VALIDATION.md).

## Windows-Spiel bauen

Nach erfolgreichem Play-Test **Tools → The Last Patient → Build Windows Player** wählen. Ergebnis: `Builds/Windows/TheLastPatient.exe`. Zum Weitergeben den **gesamten** `Builds/Windows`-Ordner inklusive `_Data` und DLLs verwenden. Bei fehlender Build-Unterstützung im Hub unter **Installs → Manage → Add modules** das passende Windows-Modul hinzufügen. Im Editor beendet der „Spiel beenden“-Button den Play-Modus nicht; dort die Play-Taste verwenden.

## Speichern und Bearbeiten

Spielstand: `Application.persistentDataPath/chapter01.json`, auf Windows normalerweise `%USERPROFILE%\AppData\LocalLow\BlackwoodStudio\The Last Patient\chapter01.json`. Die vorherige Datei wird als `.bak` behalten. Ein ungültiger Spielstand wird mit einer Meldung abgelehnt. Neustart setzt den aktuellen Durchlauf zurück; die alte Speicherdatei bleibt bis zum nächsten Checkpoint erhalten.

Alle Quelltexte liegen unter `Assets/TheLastPatient/Runtime`, der Generator unter `Editor`, die Tests unter `Tests/EditMode`. Die erzeugten Daten liegen ausschließlich im ignorierten Ordner `Generated`. Vor einer erneuten Generierung manuell bearbeitete Generated-Inhalte außerhalb dieses Ordners sichern. Die automatische Erstellung ersetzt keine vorhandene generierte Szene und überschreibt keine ungespeicherte Szene.

Das Projekt nutzt den vorhandenen Checkout. Für spätere Cloud-Aufgaben ist kein zusätzlicher Git-Worktree nötig. Der lokale Unity-Editor und seine Lizenz sind erforderlich, um Unity auszuführen; dieser Quellordner enthält weder den Editor noch einen bereits gebauten Windows-Player.
