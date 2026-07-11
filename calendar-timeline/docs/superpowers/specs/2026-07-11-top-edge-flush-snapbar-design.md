# Design: Buendige obere Kante der Timeline-Snapbar

## Ziel

Die Timeline-Snapbar soll bei `Top = 0` auch sichtbar buendig am oberen Bildschirmrand liegen. Die obere Kante wird deshalb nicht mehr als Resize-Zone verwendet.

## Verhalten

- Die Snapbar behaelt ihre gespeicherte Position; bei `Top = 0` beginnt die sichtbare Hover-Flaeche direkt am oberen Fensterrand.
- Der obere Innenabstand der sichtbaren Timeline-Flaeche wird entfernt. Die Hoehenberechnung beruecksichtigt nur noch den verbleibenden unteren Abstand.
- Die obere Kante und die oberen Ecken liefern beim nativen Hit-Test keinen `HTTOP`, `HTTOPLEFT` oder `HTTOPRIGHT` mehr.
- Die linke und rechte Kante bleiben horizontal skalierbar; die untere Kante und die unteren Ecken bleiben vertikal bzw. diagonal skalierbar.
- Ein Ziehen in freier Timeline-Flaeche verschiebt das Fenster weiterhin. Terminbuttons behalten ihre Klickinteraktion.

## Umsetzung

`SnapbarWindowInteraction.GetResizeDirection` unterscheidet nur noch linke, rechte und untere Resize-Zonen. Der WPF-Hit-Test verwendet diese Richtungen unveraendert. In XAML wird der obere Margin der Hover- und Timeline-Flaeche auf null gesetzt; die erforderliche Fensterhoehe wird entsprechend angepasst.

## Fehlerbehandlung und Tests

- Bestehende Geometrie-Persistenz und Wiederherstellung bleiben unveraendert.
- Unit-Tests decken ab, dass obere Punkte keinen vertikalen Resize-Hit mehr erzeugen, waehrend Seiten und Unterkante weiterhin funktionieren.
- Der WPF-Build und die gesamte Test-Suite muessen weiterhin ohne Warnungen bzw. Fehler durchlaufen.

## Akzeptanzkriterien

1. Bei einer Fensterposition `Top = 0` ist die sichtbare Snapbar am oberen Bildschirmrand buendig.
2. Der Cursor bietet oben kein vertikales oder diagonales Resizing an.
3. Resize funktioniert weiterhin links, rechts, unten sowie an den beiden unteren Ecken.
4. Verschieben ueber freie Timeline-Flaeche und Klicks auf Termine funktionieren weiterhin.
