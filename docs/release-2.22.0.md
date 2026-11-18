# v2.22.0 · 柔光、阶段进度与数字入场

## 安装与启动

1. 下载 `CodexMiniMenu-Windows-2.22.0.zip`，完整解压后运行 `install.cmd`。
2. 安装会在桌面和开始菜单创建 **Codex Mini Menu** 快捷方式。
3. **推荐先启动 GPT 桌面版（承载 Codex 聊天的桌面应用），再点击桌面的 Codex Mini Menu 快捷方式启动小菜单。**

适用于 Windows 10 / 11，需要 .NET Framework 4.8。升级保留本机计时记录和偏好。

## 本次更新

- Menu 提供 Timer、Context 和 Settings，Statistics 从 Timer 顶部进入。
- 菜单展开和 Context、Settings 切页采用柔光晕开、文字淡入与轻微清晰化。Timer 和 Statistics 使用短距离滑入。
- 悬浮小环按工作与休息时长分段，当前阶段亮起，开始阶段时平滑填入进度。
- Timer 时间和统计指标从零平滑递增，统计图表逐步展开，日常刷新保持稳定。
- 汇入始终悬浮、玻璃提示音开关、轻薄浮岛反光及卡片伸缩与拖动合成改进。
- 修复扩展日志路径、聊天标题加粗标记导致的上下文读数匹配问题。

## 构建与发布记录

本版完成编译、打包及文件哈希核对。按维护者要求，本次未运行自动化、桌面或性能测试。历史版本的测试数字和性能测量保留在对应文档中。

## Installation and launch

Extract the entire Windows ZIP and run `install.cmd`. Installation creates **Codex Mini Menu** shortcuts on the desktop and in the Start menu. **Start the GPT desktop app hosting your Codex chats first, then launch the menu using its desktop shortcut.**

This release includes soft-light transitions, work/rest progress segments, animated metrics and chart entrances, floating mode, refined glass controls, and context-title/path compatibility fixes. Compilation and package file checks were completed. Automated, desktop and performance tests were not run for this release.
