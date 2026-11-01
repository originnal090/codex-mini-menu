---
name: open-ring-timer
description: 在 Windows 上打开小菜单悬浮插件及其已安装的计时器模块。用于用户要求打开小菜单、计时器或本地倒计时提醒。
---

此插件附带 Windows 本地小菜单。相对此 SKILL.md，启动 `../../bin/CodexRingTimer.exe`。PowerShell 使用 `Start-Process -FilePath <解析后的绝对路径> -ArgumentList '--open' -WindowStyle Hidden`。重复启动会唤起已有菜单，不会重置正在运行的计时。

用户点击菜单中的“计时器”，设置时、分、秒。返回菜单或收起窗口后，计时继续。悬浮按钮跟随前台 Codex 窗口，可拖动；其他软件前台时隐藏。托盘提供打开、登录启动和退出选项。计时器模块清单位于 `../../modules/timer/module.json`，随安装包默认安装。

启动程序本身不等于开始倒计时。除非通过界面验证，不要声称用户指定的计时已经开始。说明用户可以在面板中选择时长并点击开始。程序退出或电脑休眠期间无法即时提醒，恢复后补报。

仅支持本地 Windows 桌面环境。没有 Windows 执行环境时说明限制，不要尝试在远端或云环境启动。
