# Motion and theme lighting implementation plan

**Goal:** Continue from 9f1dba2 with a 190 ms duration lift, compact picker spacing, continuous launcher/reflective light transitions, rotating theme border and breathing bottom light.

**Architecture:** Keep WPF and the current live 3D card. Cache the editor during transforms; avoid recaching the entire flat card. Fade reflection strength independently of pointer state. Keep the launcher HWND composition alive while suppressing its native region during projection. Add a separate, non-interactive theme lighting visual driven by compositor animations, outside the cached text layer.

**Requirements:** User's four numbered requirements in this chat. No runtime dependency changes. Preserve timer data, keyboard access, reduced motion, live theme updates and cleanup on hide.

## Work

- [x] Capture a baseline and add runtime regressions for opening completion, rapid reflection gestures and launcher composition continuity.
- [x] Update DurationFloat.cs and CycleViews.cs: halve opening duration, cache transformed content, remove unit row, rebalance header gap.
- [x] Update SilverCard.cs and native launcher clipping: smooth intensity attack/release and retain native window opacity; restore native region after an identity-pose frame.
- [x] Add CardAtmosphere.cs: theme-derived cool/warm rim and blurred radial bottom glow, slow rotation and breathing; stop clocks while hidden or animations disabled.
- [x] Run UI/data suites and compare animation timing using the same visible-window benchmark. Inspect dark/light captures.
- [x] Build 2.15.0 package, back up and update local installation, verify installed binary hash.

## Review focus

Quick press/release; hide or close during return; moving launcher near screen edges; theme switches/reduced motion; opening and canceling editor while animation is running.

## Progress

Baseline source is clean on codex/duration-float at 9f1dba2. Latest installed version and source manifest identify as 2.14.1.

All tasks complete. Full TestUI: 4609 checks passed; Package passed. Independent review findings on native region reentry and live system motion preference changes were corrected. Installed version 2.15.0 with matching build hash; prior installation backed up. Work isolated in codex/motion-polish after the original checkout received concurrent changes.
