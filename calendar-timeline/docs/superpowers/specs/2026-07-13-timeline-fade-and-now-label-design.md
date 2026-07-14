# Design: Feste Timeline-Fades und getrennte Jetzt-Anzeigen

## Ziel

Terminblöcke durchqueren zwei fest an der Timeline verankerte Fade-Zonen: Sie werden beim Eintritt am rechten Rand sichtbar und verschwinden beim Austritt am linken Rand. Die aktuelle Uhrzeit und der Countdown erhalten getrennte, eindeutig ausgerichtete Positionen an der Jetzt-Linie.

## Umfang

Enthalten sind:

- eine feste horizontale Opazitätsmaske auf der Block-Ebene der Timeline
- eine links und unten an der Jetzt-Linie ausgerichtete Anzeige der lokalen Uhrzeit
- ein rechts der Jetzt-Linie und vertikal mittig ausgerichteter Countdown
- Tests für die strukturelle Lage der Fade-Maske und der zwei Zeitindikatoren

Nicht enthalten sind Änderungen an Zeitfenster, Blockprojektion, Countdown-Berechnung, Outlook-Datenabfrage oder manueller Fenstergröße.

## Feste Fade-Zonen

`BlocksCanvas` behält eine horizontale `OpacityMask`, deren Geometrie sich ausschließlich auf die Breite der Timeline bezieht. Die linke Fade-Zone erstreckt sich von 0 bis 12 Prozent, die rechte von 88 bis 100 Prozent. Dazwischen ist die Block-Ebene vollständig deckend.

Die Maske ist eine feste Ebene und wird nicht aus einzelnen Terminblöcken abgeleitet. Wenn die bestehende Zeitprojektion bei einem Refresh die Blocks nach links verschiebt, fahren sie deshalb rechts durch den Fade in den sichtbaren Bereich und links wieder aus ihm heraus. Die Jetzt-Linie, die Uhrzeit und der Countdown bleiben oberhalb der Maske und werden nicht ausgeblendet.

## Jetzt-Anzeigen

Die bisher gemeinsame Anzeige wird in zwei unabhängige Elemente aufgeteilt:

- Die aktuelle lokale Uhrzeit im Format `HH:mm` sitzt links neben der Jetzt-Linie. Ihre Unterkante ist bündig mit dem unteren Ende der Jetzt-Linie; ein kleiner konstanter Abstand trennt Text und Linie.
- Der optionale Countdown sitzt rechts neben der Jetzt-Linie und ist vertikal auf die Mitte der Timelinevisualisierung ausgerichtet.

Das Datum bleibt als Tooltip der Uhrzeit verfügbar. Die bestehende Countdown-Regel bleibt unverändert: Er erscheint nur, wenn kein sichtbarer Termin läuft und ein späterer sichtbarer Termin existiert.

## Aktualisierung und Fehlerverhalten

Der vorhandene Minuten-Timer aktualisiert Uhrzeit, Datumstooltip und Countdown weiterhin gemeinsam mit dem vorhandenen Render-Refresh. Ohne sichtbare oder zukünftige Termine bleibt die Zeit sichtbar, während der Countdown ausgeblendet ist. Bei einem Layoutfehler wird das bestehende Verhalten zum Leeren der Blöcke und Melden der Nichtverfügbarkeit beibehalten.

## Tests

Die Tests verifizieren:

- die `OpacityMask` auf `BlocksCanvas` mit festen äußeren Fade-Zonen
- dass die Jetzt-Linie und beide Zeitindikatoren über der Block-Ebene liegen
- getrennte X- und Y-Anker für Uhrzeit links unten sowie Countdown rechts mittig
- unveränderte Berechnung, Sichtbarkeit und Formatierung des Countdowns

## Erfolgskriterien

- Ein Terminblock wird beim Durchlaufen des rechten Timeline-Rands eingeblendet, ist im Zentrum vollständig sichtbar und wird am linken Rand ausgeblendet.
- Die Fade-Zonen bleiben beim Refresh an den Timeline-Rändern fixiert.
- `HH:mm` steht links neben der Jetzt-Linie und endet bündig mit deren Unterkante.
- Der Countdown steht rechts neben der Jetzt-Linie und ist zur Timeline vertikal zentriert.
