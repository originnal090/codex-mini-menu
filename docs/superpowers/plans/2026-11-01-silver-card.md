# Silver glass card implementation plan

> **For agentic workers:** Use superpowers:executing-plans to implement and verify each task inline.

**Goal:** A live silver glass mini-menu card tilts under right-button drag and returns smoothly on release.

**Architecture:** A SilverCardPresenter hosts the existing card in 2D at rest and in a native WPF Viewport2DVisual3D while tilting. A SilverCardFilm draws angle-dependent silver highlights. CardProjection shares the pose between the 3D transform and native window contour.

**Tech Stack:** C#, .NET Framework WPF, existing Win32 interop. No dependencies.

**Spec:** ../specs/2026-11-01-silver-card-design.md

## Global constraints

- Silver glass, bounded 12-degree pitch/yaw, approximately 280 ms return.
- Live controls and timers; stable user data; original navigation, scrolling and window following.
- Capture loss, release outside, Escape and hide restore normal input.
- Perspective fits the popup and preserves the launcher cutout.
- No idle rendering loop; obey system reduced-animation preference.

## Review focus

1. View reparenting must preserve focus/editing and event subscription counts.
2. Returning at display edges or while the owner moves must preserve anchor and clipping.
3. Timer refresh during a held pose must not trigger 2D sizing or region resets.
4. All pointer exit paths must detach frame callbacks and release capture.
5. The reflection layer must keep actual text readable on dark/light and transparent backgrounds.

### Task 1: Pose and silver film

Files: source/src/SilverCard.cs, source/tests/SilverCardTests.cs, source/scripts/test-silver-card.ps1.

Interfaces: CardProjection(Size, double pitch, double yaw), Project(Point), Transform and Scale; SilverCardFilm.SetPose(double pitch, double yaw, double lift).

- [x] Write and run projection tests that assert identity at rest, bounded angles, projected corner containment at wide/tall popup sizes, and asymmetric perspective. Verify missing feature failure.
- [x] Implement the shared projection and grayscale film with glass-compatible alpha and polished rim.
- [x] Run pose and film tests and inspect static silver frames.

### Task 2: Live gesture presenter

Files: source/src/SilverCard.cs, source/src/MenuFeatures.cs, source/src/MorphMotion.cs, source/src/GlassAppearance.cs, source/tests/SilverCardTests.cs.

Interfaces: SilverCardPresenter.IsPresenting, Begin(Point), Move(Point), Release(), Reset(), UpdateContour(); MenuWindow.CardPresenter internal test access.

- [x] Add failing integration tests for whole-card tilt, live countdown updates, same control identity after return, capture loss, Escape/hide, animation-disabled release and frame-loop teardown.
- [x] Host the existing content without reconstructing it. Use a persistent capture owner, perspective plane and ambient-lit live material. Apply the same projection to a rounded polygon window region with the anchor hole.
- [x] Reset before navigation/size transitions; intercept Escape during tilt; retain existing input otherwise.
- [x] Run new tests. Adapt existing layout test's root lookup to the added presenter; rerun island/layout, back anchor, window follow, work/rest, settings, stats and context tests.
- [x] Render and inspect dark/light, home/timer/settings and extreme tilted poses. Verify subtle idle and bright angle-dependent highlights without losing digits.

### Task 3: Deliver

Files: plugin/.codex-plugin/plugin.json, plugin/bin/CodexRingTimer.exe, README.zh-CN.md, docs/silver-card-2.13.0.md.

- [x] Request an independent final source review against baseline eedc66d; resolve material findings with regression coverage.
- [x] Build, verify final tests and package 2.13.0; inspect ZIP manifest and included sources.
- [x] Back up and update the local plugin with an installed-binary concurrency guard. Verify binary/cache hashes, data preservation and running process.
- [x] Record verification and commit the change locally.

## Execution record

- Baseline: eedc66d, installed 2.12.0 SHA256 43F5244E5EE9357988CF242D3D032FD18103044D6E7C823757C34CDCFF0DE3F4. User selected the silver glass material and previously specified the interaction; implementation continues from that approved direction.

- Verified: 74 silver-card checks; 3493 layout/clip checks; core 13, structure 18, settings 19, stats views 21, work/rest interaction 40, back anchor 17, window follow 27, context 28 and context integration 12. Final logs: source/artifacts/*-silver-final.log.
- Fixed after a failing regression: WPF automatically cropped empty visual margins. A full-size transparent face now aligns the rendered perspective and native polygon exactly.
- Fixed after a failing regression: same-sized feature navigation now restores the ordinary card before switching content.
- Final review: fixed return-animation focus theft by restoring keyboard focus only while the menu remains active and visible. The two-window deactivation regression is green. No material findings remain.
- Native desktop tests need access to temporary test files outside the default child-process sandbox; the final complete run used the approved desktop test execution context.
- Delivered: 2.13.0 ZIP validated (107 entries). Installed and cache binaries both SHA256 EE399751BC3871A3A6B4E558BBC99FE52FEEA255180827342F4331A777B0BFED; all five existing timer-data files retained identical hashes before restart. Backup: backup-before-silver-20261101-174008. Verification record: source/artifacts/silver-installation-verification.json.
