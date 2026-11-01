# Menu Islands Implementation Plan

> **For agentic workers:** Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Apply the approved card-with-islands design to the entire local menu plugin.
**Architecture:** Shared live theme materials and island containers, with existing WPF controls and state retained. Native width 368 DIP accommodates a 278 DIP statistics plot inside a padded island.
**Tech Stack:** C#, WPF, .NET Framework 4.8, PowerShell.
**Spec:** ../specs/2026-11-01-menu-islands-design.md

## Global Constraints
- Island radius 24 DIP, gap 14 DIP, shadow blur 12 DIP; shell radius 34 DIP.
- Preserve timer data and keyboard/pointer behavior; no new dependencies.
- Equal timer/statistics page height; theme and transparency updates preserve focus and state.

## Review Focus
- English and 24-hour values fit all islands: layout capture test.
- Statistics charts fit the reduced padded body: plot bounds test and existing view suite.
- Editor overlay fits and suspends navigation: structure and work/rest tests.
- Return/launcher alignment survives width and height changes: back/window suites.
- Theme/transparency updates preserve controls and timer deadline: layout and settings suites.

### Task 1: Shared materials and shell
Files: source/src/IslandAppearance.cs (new), TimerViews.cs, GlassAppearance.cs, MenuFeatures.cs, NativeInteractions.cs.
Interfaces: Theme.Island(UIElement, Thickness, int), Theme.Raise(Button), live IslandFill/IslandEdge/IslandHover/IslandPressed brushes.
- [x] Add layout regression test; run baseline build and show new geometry assertion fails.
- [x] Implement shared materials, navigation island and home controls.

### Task 2: Feature pages
Files: CycleViews.cs, StatsViews.cs, SettingsPanels.cs, ReminderAudio.cs, GlassAppearance.cs.
- [x] Wrap timer hero, duration buttons, loop and editor; preserve events.
- [x] Wrap statistics summaries/rail/body; keep 278 DIP inner chart.
- [x] Apply islands to settings overview, language, appearance and sound sections.
- [x] Run build, layout captures and relevant existing suites; inspect rendered images.

### Task 3: Delivery
Files: plugin metadata, distribution binary, release notes, local installation.
- [x] Run independent whole-diff review and address material issues.
- [x] Build and package version 2.12.0; verify installer payload and local paths.
- [x] Back up installed files, update the authorized local plugin, open and verify new executable.

## Execution record
- Approved visual and implementation instruction received directly from user; execute in this session without another approval round.
- Native worktree tool cannot target the nested repository. Created a git worktree at ../codex-mini-menu-islands on codex/menu-islands; original main checkout remains intact.
- Installation guard detected the concurrently installed context-module release before any write. Preserved its source, launcher/application integration, UIAutomationClient references, installer UTF-8 fix and module manifest. Applied the same design to its detail/reminder views and advanced the combined release to 2.12.0.
- Review corrections: actual-palette contrast guard and scrollable page capacity for small work areas. Both have red-to-green regression checks.

- Validation: core 13; cycle 21; statistics data 33; context data 28; context integration 12; island layout 2760; structure 18; settings 19; statistics views 21; interactions 40. Final positioning/window checks recorded in source/artifacts/test-back-final.log and test-window-final.log.
- Existing animation test sampled only at 170 ms; continuous rendered-frame peak measurement now verifies overshoot without depending on one scheduler tick.
