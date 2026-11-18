## 用户开发约束（优先于历史流程）

1. 只实现业务代码，**不要新增、不要自动编写单元测试**；不要自动执行测试命令。
2. 除非用户明确要求写或运行测试，否则不调用 test、jest、pytest、vitest 等测试相关命令，包括项目测试脚本、UI 回归、性能测试和测试验收脚本。
3. 修改完成后直接停止，不要反复自测、不要自动修复自测出来的问题。用户同时要求安装时，完成安装后停止。
4. 只在用户主动要求时才运行现有项目测试；要求修改、构建、升级或安装不等于授权测试。
5. 不要自动补充 mock、边界 case、断言。

涉及测量、回归、观察与验收脚本的历史流程仅在用户明确要求相应测试时执行；不得把它们作为普通修改或安装的自动前置步骤，也不得因此阻止用户要求的安装。

仅构建使用 source/scripts/compile-release.ps1；不要自动调用附带测试的 build.ps1、dev.ps1 的 Build/Test/TestUI/Regression/Package 动作或 package.ps1。安装时可核对程序版本及文件哈希，不运行测试。

# Existing implementation requirements

- Start from the most recent committed and installed feature version; retain ongoing timer data and user preferences.
- Moving interactions target at least 65 updates per second on hardware/display modes that support it. Aim higher when practical. Measure right-button tilt and launcher-position dragging with all shipped visual effects enabled.
- Run `source/scripts/measure-motion.ps1 -Mode tilt -EnforceBudget` and `-Mode drag -EnforceBudget` before releasing movement or visual-effect changes. Report mean update rate, median/P95 intervals, DPI and display refresh. Do not describe WPF rendering callbacks as independently measured GPU presentation FPS.
- Investigate any result below 65 updates/second; do not hide a regression behind faster animation durations. Lower-refresh displays must be identified explicitly when interpreting measurements.
- Launcher material must remain visually continuous with the open glass card through rest, tilt, return and dragging.
- The whole card uses two opposing diffuse theme colors rotating about its center. The outer conic border and background share the same color field, center and rotation phase. Respect application and system animation preferences and stop animation while hidden.
- Run the complete data and desktop UI suite before packaging; verify installed and plugin-cache binary hashes.
- Launcher input changes require source/scripts/test-launcher-input.ps1 on the interactive desktop: real mouse presses, dragging, click-to-close, reopen and post-tilt input. Calling BeginDrag directly cannot establish native hit-test correctness.
- Keep recurring user problems in docs/issue-regression-ledger.md with permanent REG IDs, reproducible steps, expected behavior, separate automated/observed evidence, and honest current status. Reopen the existing ID when feedback recurs; historical test passes do not close new feedback.
- Before every delivery, check every ledger scenario in addition to the complete normal suite. Reuse this build's normal-suite logs for mapped automated cases; actually perform and record the separate desktop observations. Run source/scripts/test-release-regressions.ps1 -Action Verify against the final source/binary and preserve its evidence report. Missing, failed or unobserved cases must not be reported as resolved.
- Record application/WPF performance, real desktop visual observations, and user acceptance separately. An agent/developer may perform visual checks without requesting user approval; never infer human acceptance from automated results. Open-menu host-window dragging needs its own observation; launcher dragging does not substitute for it.
