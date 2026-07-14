# Timeline Viewport Dimensions Design

## Purpose

Appointment blocks must be fully opaque in the timeline's middle region. The only horizontal fade is anchored to the timeline edges. The vertical bubble gradient, border, and shadow are separate visual-depth elements and remain unchanged.

## Problem

`UpdateLayoutMetrics` calculates block geometry from `ActualWidth - TimelineWidthPadding`, while the masked `BlocksViewport` and its canvas rely on implicit WPF stretching. These are separate sizing paths. The opacity mask therefore has no explicit runtime contract to use the exact coordinate system used to place blocks.

## Design

`TimelineGrid.ActualWidth` is the single source of horizontal layout truth. Each layout update uses it for block positions and time indicators, and assigns the same width to both `BlocksViewport` and `BlocksCanvas`. It assigns the calculated timeline height to both elements as well. The viewport is explicitly anchored at the top-left of `TimelineGrid`.

The existing viewport opacity mask remains unchanged:

- transparent at offsets `0` and `1`
- fully opaque at offsets `.12` and `.88`
- applied only to `BlocksViewport`

This makes blocks in the interval from `.12` to `.88` fully opaque while allowing blocks outside the viewport to enter and leave through the fixed edge fades.

## Scope

- Modify WPF layout synchronization in `MainWindow.xaml` and `MainWindow.xaml.cs`.
- Add a source-level regression test for the shared layout dimensions and fixed mask structure.
- Preserve the current block fill gradient, border, shadow, interaction states, unbounded geometry, and time indicators.

## Verification

The focused source test must fail before the production change and pass after it. The Core test project and WPF project build must pass. A Windows visual check must confirm that a centered block is opaque while edge-entering and edge-leaving blocks fade only at the timeline bounds.
