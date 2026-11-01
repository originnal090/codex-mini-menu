# Development and release

Use Windows PowerShell 5.1 and .NET Framework 4.8 on Windows. Git is needed for source control. Open `CodexMiniMenu.code-workspace` in VS Code to use the build, test and package tasks, or run these commands from the repository root:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File ./dev.ps1 -Action Check
powershell -NoProfile -ExecutionPolicy Bypass -File ./dev.ps1 -Action Test
powershell -NoProfile -ExecutionPolicy Bypass -File ./dev.ps1 -Action TestUI
powershell -NoProfile -ExecutionPolicy Bypass -File ./dev.ps1 -Action Package
```

`Test` builds the application and runs the core, cycle, statistics and context checks. `TestUI` also runs the desktop interaction checks and requires an interactive Windows desktop. `Package` rebuilds and runs the data checks before creating the release ZIP. Run `TestUI` before releasing changes to desktop interactions.

The entry point resolves all project paths from its own location, including when invoked from another directory or a path containing spaces. Temporary test files stay in `source/artifacts/tmp`; the caller's temporary-directory environment is restored afterward.

| Location | Purpose |
| --- | --- |
| `source/src` | Application source |
| `source/tests` | Test source |
| `source/bin` | Compiled application |
| `source/artifacts` | Test binaries, fixtures and captures |
| `plugin/bin` | Application included in the plugin package |
| `dist` | Staged release and versioned ZIP |

The plugin manifest at `plugin/.codex-plugin/plugin.json` supplies the executable and package version. Build scripts discover Windows framework tools from `WINDIR`. The installer resolves its destination and user data from the current Windows user profile.

## GitHub workflow

The repository remote is `https://github.com/originnal090/codex-mini-menu.git`. For a linked worktree, `git rev-parse --git-common-dir` must resolve to its current main checkout, and `git worktree list` must list the current directories. When moving checkouts, repair the links from the main checkout with `git worktree repair <current-worktree-path>`.

Before publishing:

1. Run `dev.ps1 -Action Check`, `TestUI` and `Package`.
2. Review `git status --short` and `git diff --check`, including the packaged binary and plugin version.
3. Commit the reviewed source, tests, build scripts, documentation and packaged plugin binary on the development branch.
4. Push that branch to `origin` and open a pull request into `main`.
5. Attach the ZIP from `dist` when publishing a versioned release.

Generated test data, local backups, build output and release ZIPs are excluded by `.gitignore`. Keep machine-specific paths and credentials outside the repository.
