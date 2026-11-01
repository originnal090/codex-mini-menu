# Conic lighting and movement implementation plan

**Goal:** Deliver the user's requested 90+ movement update target, continuous frosted launcher material, angular rotating border and visible breathing light from 175a1c2.

**Architecture:** Measure both actual position changes and unique WPF rendering timestamps with all visual effects active. Cache stable text/material layers. Draw one conic border with an angular color field and fixed rounded mask. Share the card's glass beneath the docked launcher in both flat and projected modes.

- [x] Add the persistent 90+ movement rule and benchmark tilt/drag baselines.
- [x] Unify docked launcher glass and remove the cutout beneath its transparent face, preserving closed-menu launcher appearance and input.
- [x] Implement conic border, visible breathing glow and efficient retained/cached rendering; eliminate redundant outer rim drawing.
- [x] Add material, lifecycle and lighting regressions. Verify rendering cadence, visuals and full data/UI suite.
- [ ] Build/install 2.16.0 with backups and matching hashes; report measured results precisely.

Review focus: desktop drag at all screen edges, simultaneous native motion and theme lighting, continuous launcher material at return, hidden/reduced-motion lifecycle, theme changes during duration editing.

Verification: TestUI 4627 checks passed. Unique render updates: tilt 105.9/s, drag 117.0/s; both budget checks passed. Review: screen-clamped glass extension corrected and all eight edge assertions passed. Package includes AGENTS.md.
