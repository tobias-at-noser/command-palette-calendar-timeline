# Timeline Fixed Fade Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Termine bewegen sich geometrisch durch einen fest an der Timeline verankerten Fade.

**Architecture:** Ein fester, maskierter `BlocksViewport` beschneidet einen inneren Canvas. Zeitprojektion und Blockgeometrie bleiben ausserhalb des Viewports unbegrenzt, sodass Termine rechts in den Fade eintreten und links wieder durch ihn austreten.

**Tech Stack:** .NET 10, C#, WPF, XAML, xUnit

## Global Constraints

- Der Fade bleibt mit den Stopps `0`, `.12`, `.88` und `1` relativ zur Timeline verankert.
- Now-Line, Uhrzeit und Countdown bleiben oberhalb der maskierten Blockebene und damit unmaskiert.
- Die Mindestbreite einer Terminblase bleibt 52 DIP.
- Bestehende Lane-Hoehenlogik, Countdown-Rundung, Tooltips und Interaktionszustaende bleiben unveraendert.
- Automatisierte Tests werden vor der jeweiligen Produktionsaenderung geschrieben und muessen zuerst fehlschlagen.
- Die abschliessende visuelle WPF-Pruefung erfolgt auf Windows.

---

### Task 1: Unbegrenzte Zeitgeometrie

**Files:**
- Modify: `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Core/TimelineVisualProjector.cs`
- Modify: `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Core/Snapbar/TimelineSnapbarLayout.cs`
- Test: `calendar-timeline/src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/TimelineVisualProjectorTests.cs`
- Test: `calendar-timeline/src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/TimelineSnapbarLayoutTests.cs`

**Interfaces:**
- Consumes: vorhandene Zeitfenster, Terminzeiten, `startRatio`, `widthRatio`, `timelineWidth` und `minimumWidth`
- Produces: nicht geklemmte Start-/End-Ratios sowie nicht auf den Viewport verschobene oder gekuerzte Blockgrenzen

- [ ] **Step 1: Tests fuer Ratios ausserhalb des Zeitfensters schreiben**

Ersetze `Project_ClampsRatiosForAppointmentsOutsideWindow` durch Erwartungen, bei denen ein Termin von 30 Minuten vor bis 30 Minuten nach einem einstündigen Fenster die Ratios `-0.5` und `1.5` erhaelt.

- [ ] **Step 2: Tests fuer Eintritt und Austritt schreiben**

Ersetze den Test, der eine Mindestbreiten-Blase am rechten Rand in den Viewport schiebt, und ergaenze mindestens diese Faelle:

```csharp
var entering = TimelineSnapbarLayout.GetBlockBounds(0.95, 0.10, 100, 52);
Assert.Equal(95, entering.Left);
Assert.Equal(52, entering.Width);

var leaving = TimelineSnapbarLayout.GetBlockBounds(-0.10, 0.20, 100, 52);
Assert.Equal(-10, leaving.Left);
Assert.Equal(52, leaving.Width);

var spanning = TimelineSnapbarLayout.GetBlockBounds(-0.20, 1.40, 100, 52);
Assert.Equal(-20, spanning.Left);
Assert.Equal(140, spanning.Width);
```

- [ ] **Step 3: Fokussierte Tests ausfuehren und den Fehlschlag bestaetigen**

Run:

```bash
dotnet test calendar-timeline/src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/CalendarTimeline.Core.Tests.csproj --filter "FullyQualifiedName~TimelineVisualProjectorTests|FullyQualifiedName~TimelineSnapbarLayoutTests"
```

Expected: Die neuen Erwartungen schlagen fehl, weil Ratios, Positionen und Breiten aktuell auf den Viewport begrenzt werden.

- [ ] **Step 4: Ratio-Clamping entfernen**

Passe `TimelineVisualProjector.CalculateRatio` so an, dass bei gueltiger Fensterdauer direkt das Zeitverhaeltnis zurueckgegeben wird:

```csharp
return (point - windowStart).TotalMilliseconds / duration.TotalMilliseconds;
```

Die vorhandene Behandlung einer ungueltigen oder leeren Fensterdauer bleibt bestehen.

- [ ] **Step 5: Viewport-Clamping der Blockgrenzen entfernen**

Passe `TimelineSnapbarLayout.GetBlockBounds` so an, dass die Position allein aus dem Start-Ratio entsteht und nur die Mindestbreite erhalten bleibt:

```csharp
var left = timelineWidth * startRatio;
var width = Math.Max(minimumWidth, timelineWidth * widthRatio);
return (left, width);
```

Eine vorhandene Schutzbehandlung fuer eine nicht positive Timeline-Breite bleibt bestehen. Weder `left` noch `width` duerfen anschliessend auf die Viewportgrenzen geklemmt werden.

- [ ] **Step 6: Fokussierte Tests erneut ausfuehren**

Run:

```bash
dotnet test calendar-timeline/src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/CalendarTimeline.Core.Tests.csproj --filter "FullyQualifiedName~TimelineVisualProjectorTests|FullyQualifiedName~TimelineSnapbarLayoutTests"
```

Expected: PASS.

---

### Task 2: Fester maskierter Viewport

**Files:**
- Modify: `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Wpf/MainWindow.xaml`
- Test: `calendar-timeline/src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/Task7ReviewFixTests.cs`

**Interfaces:**
- Consumes: die unbegrenzten Blockpositionen und -breiten aus Task 1
- Produces: einen fest dimensionierten, maskierten und beschneidenden `BlocksViewport` mit einem unmaskierten inneren `BlocksCanvas`

- [ ] **Step 1: Strukturtest fuer den Viewport schreiben**

Ergaenze einen Test, der in `MainWindow.xaml` folgende Struktur nachweist:

- `x:Name="BlocksViewport"`
- `Panel.ZIndex="2"`
- `ClipToBounds="True"`
- `Grid.OpacityMask` am Viewport
- Gradient-Stopps `0`, `.12`, `.88` und `1`
- `BlocksCanvas` als Kind des Viewports ohne eigene `Canvas.OpacityMask`
- Now-Line auf Z-Index 3 und beide Indikatoren auf Z-Index 4

- [ ] **Step 2: Strukturtest ausfuehren und den Fehlschlag bestaetigen**

Run:

```bash
dotnet test calendar-timeline/src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/CalendarTimeline.Core.Tests.csproj --filter "FullyQualifiedName~Task7ReviewFixTests"
```

Expected: FAIL, weil `BlocksViewport` noch nicht existiert und die Maske aktuell direkt auf `BlocksCanvas` liegt.

- [ ] **Step 3: Maskierten Viewport einfuehren**

Ersetze den bisherigen maskierten Canvas in `MainWindow.xaml` durch:

```xml
<Grid x:Name="BlocksViewport"
      Panel.ZIndex="2"
      ClipToBounds="True">
    <Grid.OpacityMask>
        <LinearGradientBrush StartPoint="0,0"
                             EndPoint="1,0"
                             MappingMode="RelativeToBoundingBox">
            <GradientStop Offset="0" Color="Transparent" />
            <GradientStop Offset=".12" Color="White" />
            <GradientStop Offset=".88" Color="White" />
            <GradientStop Offset="1" Color="Transparent" />
        </LinearGradientBrush>
    </Grid.OpacityMask>

    <Canvas x:Name="BlocksCanvas" />
</Grid>
```

Die Now-Line und beide Indikatoren bleiben unveraenderte Geschwister mit ihren hoeheren Z-Indizes.

- [ ] **Step 4: Strukturtests erneut ausfuehren**

Run:

```bash
dotnet test calendar-timeline/src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/CalendarTimeline.Core.Tests.csproj --filter "FullyQualifiedName~Task7ReviewFixTests"
```

Expected: PASS.

---

### Task 3: Bubble-Template ausrichten

**Files:**
- Modify: `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Wpf/MainWindow.xaml`
- Test: `calendar-timeline/src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/Task7ReviewFixTests.cs`

**Interfaces:**
- Consumes: `HorizontalContentAlignment` und `VerticalContentAlignment` des Termin-Buttons
- Produces: einen `ContentPresenter`, dessen Inhalt die berechnete Blasenbreite ausnutzt

- [ ] **Step 1: Test fuer beide TemplateBindings schreiben**

Ergaenze im vorhandenen Template-Strukturtest Erwartungen fuer:

```xml
HorizontalAlignment="{TemplateBinding HorizontalContentAlignment}"
VerticalAlignment="{TemplateBinding VerticalContentAlignment}"
```

- [ ] **Step 2: Test ausfuehren und den Fehlschlag bestaetigen**

Run:

```bash
dotnet test calendar-timeline/src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/CalendarTimeline.Core.Tests.csproj --filter "FullyQualifiedName~Task7ReviewFixTests"
```

Expected: FAIL, weil der `ContentPresenter` aktuell keine Alignment-Bindings besitzt.

- [ ] **Step 3: ContentPresenter anbinden**

Ersetze den nackten `ContentPresenter` im Button-Template durch:

```xml
<ContentPresenter
    HorizontalAlignment="{TemplateBinding HorizontalContentAlignment}"
    VerticalAlignment="{TemplateBinding VerticalContentAlignment}" />
```

- [ ] **Step 4: Template- und Bubble-Tests erneut ausfuehren**

Run:

```bash
dotnet test calendar-timeline/src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/CalendarTimeline.Core.Tests.csproj --filter "FullyQualifiedName~Task7ReviewFixTests|FullyQualifiedName~Task7ReviewTests"
```

Expected: PASS.

---

### Task 4: Zeitanzeige und ungueltige Termine

**Files:**
- Modify: `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Core/Snapbar/TimelineTimeDisplay.cs`
- Modify: `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Wpf/MainWindow.xaml.cs`
- Test: `calendar-timeline/src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/TimelineTimeDisplayTests.cs`

**Interfaces:**
- Consumes: `DateTimeOffset now` und die projizierten Terminbloecke
- Produces: `TimelineTimeDisplay.GetCurrentTime(DateTimeOffset now)` sowie einen Countdown, der nur gueltige Termine mit `End > Start` beruecksichtigt

- [ ] **Step 1: Test fuer deterministische Uhrzeitformatierung schreiben**

Ergaenze einen Test mit einem festen `DateTimeOffset`, der exakt `"09:07"` von `TimelineTimeDisplay.GetCurrentTime` erwartet.

- [ ] **Step 2: Tests fuer ungueltige Countdown-Kandidaten schreiben**

Ergaenze zwei Faelle:

- Ein zukuenftiger Block mit `End <= Start` liefert keinen Countdown.
- Ein ungueltiger zukuenftiger Block verhindert nicht den Countdown zum naechsten gueltigen Block.

- [ ] **Step 3: Tests ausfuehren und den Fehlschlag bestaetigen**

Run:

```bash
dotnet test calendar-timeline/src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/CalendarTimeline.Core.Tests.csproj --filter "FullyQualifiedName~TimelineTimeDisplayTests"
```

Expected: FAIL, weil `GetCurrentTime` noch nicht existiert und ungueltige Bloecke aktuell nicht herausgefiltert werden.

- [ ] **Step 4: Uhrzeitformatierung extrahieren**

Ergaenze in `TimelineTimeDisplay`:

```csharp
public static string GetCurrentTime(DateTimeOffset now) =>
    now.ToString("HH:mm", CultureInfo.InvariantCulture);
```

Nutze in `MainWindow.xaml.cs` anschliessend:

```csharp
NowTimeTextBlock.Text = TimelineTimeDisplay.GetCurrentTime(now);
```

- [ ] **Step 5: Ungueltige Bloecke aus der Countdown-Auswahl entfernen**

Beschraenke sowohl die Pruefung eines laufenden Termins als auch die Auswahl des naechsten Termins auf Bloecke mit:

```csharp
block.End > block.Start
```

Damit darf ein ungueltiger Block weder einen Countdown erzeugen noch einen gueltigen Countdown unterdruecken.

- [ ] **Step 6: Zeitanzeige-Tests erneut ausfuehren**

Run:

```bash
dotnet test calendar-timeline/src/CalendarTimeline/tests/CalendarTimeline.Core.Tests/CalendarTimeline.Core.Tests.csproj --filter "FullyQualifiedName~TimelineTimeDisplayTests"
```

Expected: PASS.

---

### Task 5: Gesamte Verifikation

**Files:**
- Verify: `calendar-timeline/src/CalendarTimeline/CalendarTimeline.sln`
- Verify: `calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Wpf/CalendarTimeline.Wpf.csproj`

**Interfaces:**
- Consumes: alle Aenderungen aus Task 1 bis Task 4
- Produces: automatisierte Regressionssicherheit und eine visuell bestaetigte WPF-Darstellung

- [ ] **Step 1: Gesamte Testsuite ausfuehren**

Run:

```bash
dotnet test calendar-timeline/src/CalendarTimeline/CalendarTimeline.sln
```

Expected: PASS ohne fehlgeschlagene Tests.

- [ ] **Step 2: WPF-Projekt auf Windows bauen**

Run:

```bash
dotnet build calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Wpf/CalendarTimeline.Wpf.csproj
```

Expected: Build erfolgreich ohne Fehler.

- [ ] **Step 3: Anwendung auf Windows starten**

Run:

```bash
dotnet run --project calendar-timeline/src/CalendarTimeline/src/CalendarTimeline.Wpf/CalendarTimeline.Wpf.csproj
```

- [ ] **Step 4: Fade-Verhalten visuell pruefen**

Pruefe nacheinander:

1. Ein Termin erscheint zuerst teilweise im rechten Fade.
2. Er wird zur Mitte hin vollstaendig deckend.
3. Er laeuft links wieder in den Fade und wird am Viewport abgeschnitten.
4. Der Fade selbst bleibt relativ zur Timeline unbewegt.
5. Now-Line, Uhrzeit und Countdown bleiben vollstaendig unmaskiert.
6. Breite Titel nutzen die berechnete Blasenbreite und werden am Ende gekuerzt.
7. Automatische und manuelle Timeline-Hoehen verhalten sich unveraendert.

- [ ] **Step 5: Abschliessenden Diff auf den vereinbarten Umfang pruefen**

Der Diff darf nur die genannten Produktions- und Testdateien enthalten. Unabhaengige bestehende Arbeitsbaum-Aenderungen bleiben unberuehrt.
