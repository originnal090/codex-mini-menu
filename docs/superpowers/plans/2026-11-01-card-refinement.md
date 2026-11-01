# Card refinement and rendering performance

The user reported nonconcentric upper/lower right buttons, font weight changes at tilt boundaries, a stationary launcher during tilt, and visible frame-rate loss. Fix all four in the existing WPF implementation and report measured benefit and cost.

- [x] Geometry: derive launcher docking and close-button centers from the outer card radius (34 DIP). Keep header/body/return bounds stable in above/below layouts and at display edges.
- [x] Text: explicitly preserve Display/Grayscale text rendering across presentation changes, cache the stable content independently from moving reflections, and verify glyph layout and zero-pose output.
- [x] Launcher: include a live visual representation in the tilted card, temporarily suppress the original window pixels, restore it on every exit, and keep its anchor/window identity for follow logic. The tilt contour contains the launcher; the resting cutout still belongs to its independent window.
- [x] Performance: measure the same visible card before and after changes; avoid rebuilding static brushing, controls and shadows each pose; reuse camera/transforms and cache textures. Retain runtime dependencies and live timer updates.
- [x] Test real corner coordinates, text metrics, launcher lifecycle/contour, gesture exits and focus; run existing suites. Independently review changed behavior.
- [x] Package/install with the usual binary concurrency guard and data backup, then report benchmark scope and tradeoffs.

Baseline f247bc4 / 2.13.0. Visible six-second fixture, first second warm-up, rendering tier 2, 150% DPI: 27.5 rendering callbacks/s, median 35.00 ms, p95 53.49 ms, CPU 55.3% of one logical core; private memory 468.6 MiB at end (delta 137.0 MiB). This measures WPF callback cadence, not a hardware presentation trace; report that distinction and do not promise a fixed refresh rate on every machine.

## Verification record

- Final refinement suite: 17 checks, including projected-image coverage, concentric corners, unchanged glyph pixels, launcher restoration and four display edges.
- Existing suites passed: core 13, silver card 74, islands 4177, structure 18, settings 19, statistics views 21, interactions 40, return anchors 17, window following 27, context 28, context integration 12.
- Independent review findings (edge clipping and relative launcher movement) were reproduced, fixed and covered by regression tests.
- With-launcher benchmark: 2.13.0 baseline 40.3 callbacks/s; final 2.14.0 102.2 callbacks/s, median 10.02 ms, p95 11.89 ms. Both used the same idle timer fixture. CPU one-core equivalent 88.6% to 78.3%; private memory 475.1 to 317.6 MiB. GPU memory was not measured.
- Package executable, installed executable and registered plugin cache share SHA256 DE7A05C4E6ED1BCB6885F780E9E062567BDFFA74E6FB00A6AA5596AEBF1A0B72. Installation backed up 2.13.0 and preserved all five data files by hash; 2.14.0 restarted successfully.
