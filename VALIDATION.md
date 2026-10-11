# Prüfungen und nächster Test in Unity

## In der Cloud ausgeführt

- C#-Quellen mit dem Tree-sitter-C#-Parser auf Syntaxfehler geprüft.
- JSON-Manifeste und Assembly-Definitionen eingelesen; interne Assembly-Referenzen, Komponenten-Dateinamen und verwendete `StoryIds` überprüft.
- Eindeutige Unity-GUIDs und vollständige ZIP-Projektstruktur geprüft.
- Paket-APIs mit den offiziellen Unity-Quellen abgeglichen: URP 17.6.0, Unity UI/TextMeshPro 2.6.0 für den Unity-6000.6-Zweig und Input System 1.20.1. Dieser Quellabgleich bestätigt keine Paketinstallation oder Unity-Kompilierung.

## Behobene Paket-Inkompatibilität

Die erste Fassung enthielt Input System 1.11.2. Im lokalen Unity-6.6-Editor wurde damit `CS0619` in `HIDDescriptorWindow.cs` gemeldet: Die nicht-generische `TreeViewItem`-API ist dort als Compilerfehler veraltet. Das Manifest verwendet jetzt den veröffentlichten Tag **1.20.1**. In dessen offiziellem Quelltext verwenden die HID-Editor-Klassen ab `UNITY_6000_2_OR_NEWER` die generischen `TreeView<int>`, `TreeViewItem<int>` und `TreeViewState<int>`-Typen. Der Tag erklärt Unity 6000.0 als Mindestversion. Der Fehlerpfad wurde im Quelltext geprüft; der erneute Unity-Build steht noch aus.

Bereits entpacktes Projekt aktualisieren: In `Packages/manifest.json` nur den Wert von `com.unity.inputsystem` auf `1.20.1` ändern und Unity die Pakete neu auflösen lassen. Keine Dateien in `Library/PackageCache` bearbeiten. Danach die Console leeren und neu auftretende rote Fehler prüfen. Die gemeldeten gelben UXML-/Typ-Warnungen müssen nach erfolgreicher Kompilierung separat überprüft werden.

Unity 6.6 meldete anschließend `com.unity.modules.vr@1.0.0` als nicht verfügbare Abhängigkeit. Dieser unnötige Legacy-VR-Eintrag wurde aus dem Manifest entfernt. Der Kapitel-1-Code verwendet keine VR-APIs. Bestehende Projekte können die gesamte korrigierte `Packages/manifest.json` aus dem GitHub-Branch übernehmen; nach dem Speichern im Package-Manager-Dialog **Retry** wählen. Diese Korrektur wurde als JSON geprüft; eine erfolgreiche Paketauflösung im lokalen Editor ist noch zu bestätigen.

Diese Prüfungen ersetzen keine Unity-Kompilierung und keinen Spieltest.

## Unity EditMode-Tests: vorhanden, hier nicht ausgeführt

13 Tests unter `Assets/TheLastPatient/Tests/EditMode`:

- Missionsziele lassen sich nicht überspringen.
- Eintritt setzt das Story-Flag vor der Checkpoint-Benachrichtigung; Ereignisse laufen nur einmal.
- Stationszugang benötigt Schlüssel und gehörtes Tonband.
- Kapitelabschluss und JSON-Roundtrip behalten Fortschritt und Inventar.
- Wiederholte Interaktion erzeugt keine doppelten Gegenstände oder Zielabschlüsse.
- Beschädigte Spielstände mit fehlendem Schlüssel werden zurückgewiesen.
- Falsche Zielreihenfolge, ungültige Positionen und NaN werden zurückgewiesen.
- Laden berechnet das aktive Ziel neu.
- Dialogentscheidungen überstehen den JSON-Roundtrip.
- Generierte Systeme besitzen ihre Inspector-Referenzen.
- Tatsächliche Physics-Raycast-Prüfung erreicht Taschenlampe, Akte und Tonband.
- Stations- und Final-Trigger besitzen die richtigen Voraussetzungen.
- Prefabs und Build-Szene sind registriert.

Nach dem Generieren der Szene **Window → General → Test Runner → EditMode → Run All** ausführen. Wenn diese Tests fehlschlagen, ist das Projekt noch nicht als geprüft spielbar zu betrachten.

## Manueller Play-Test

1. Neue Szene starten. Intro läuft; ohne Überspringen und in einem zweiten Durchlauf mit Esc testen. Danach WASD und Maus prüfen.
2. Eingang öffnen und hineingehen. Tür muss verriegeln, Licht ausfallen, Mission zur Taschenlampe wechseln. Esc während des Stromausfalls darf die Tür nicht wieder entriegeln.
3. Taschenlampe nehmen, F mehrfach drücken, Batterie am Wagen ersetzen. Beim Ducken die Strg-Taste lösen: Daniel muss wieder aufstehen.
4. Akte öffnen, mit Esc schließen, erneut öffnen und Schlüssel nehmen. Akte bleibt im Journal; Schlüssel kommt nur einmal ins Inventar.
5. Tonband vollständig hören; in einem zweiten Durchlauf Leertaste testen. Die Stations-Tür darf ohne Tonband nicht öffnen.
6. F5 speichern. Weglaufen, F9 laden: Position, Lampe, Schlüssel, Türen, Ziel und gehörtes Tonband müssen wiederhergestellt sein. Das Tonband bleibt wiederholbar.
7. Watcher-Erscheinung ohne und mit Esc prüfen. Danach muss die Steuerung wieder funktionieren und die Figur verschwunden sein.
8. Patientenstation öffnen und hineinlaufen. Kapitelabschluss anzeigen, Durchlauf neu beginnen; zuvor gesammelte Gegenstände müssen wieder vorhanden sein.
9. Pause, Journal und Dokumente öffnen und schließen. Maus darf dabei frei sein und beim Weiterspielen wieder eingefangen werden.
10. Windows-Build starten und den Ablauf dort wiederholen. Leistung im Profiler messen; 60 FPS ist ein Ziel, keine bestätigte Messung.

## Batchmode mit installiertem, lizenziertem Editor

PowerShell im Projektordner:

```powershell
$unityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor\Unity.exe'
& $unityEditor -batchmode -quit -projectPath $PWD.Path -executeMethod TheLastPatient.Editor.PrototypeGenerator.GenerateForCI -logFile generate.log
if ($LASTEXITCODE -ne 0) { throw 'Szenengenerierung fehlgeschlagen; generate.log prüfen.' }
& $unityEditor -batchmode -projectPath $PWD.Path -runTests -testPlatform EditMode -testResults TestResults.xml -logFile tests.log
if ($LASTEXITCODE -ne 0) { throw 'Unity-Tests fehlgeschlagen; TestResults.xml und tests.log prüfen.' }
```

Für `-runTests` kein zusätzliches `-quit` verwenden: Der Test Runner beendet den Batch-Aufruf selbst. Die Ergebnisdatei muss zur aktuellen Ausführung gehören und 13 ausgeführte Tests enthalten; ein bloßer Exitcode oder null ausgeführte Tests reicht nicht.
