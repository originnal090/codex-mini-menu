# Codex 小菜单

[English](README.md) · [下载](https://github.com/originnal090/codex-mini-menu/archive/refs/heads/main.zip)

![滑轨与按钮交互](docs/images/interactions.gif)

*使用实际 WPF 控件录制。滑块覆盖到的文字实时换色，按键按下压缩、释放回弹。*

![计时器](docs/images/timer.png)

![统计](docs/images/statistics.png)

*统计截图为演示数据。*

点击悬浮圆环打开小菜单。计时器和统计是菜单中的功能模块，左下角返回主页，收起窗口后继续计时。

## 工作与休息

- 设置工作时长和休息时长，两段合为一组。点击时长卡片，用时、分、秒滚轮编辑，每段支持 1 秒至 24 小时。
- 默认执行 1 组；可以输入 1–99 组。工作结束自动进入休息，休息结束后按组数继续，最后一组结束后等待再次开始。
- 环形进度显示当前阶段的剩余时间，工作绿色、休息蓝色。运行期间可暂停、继续或结束本次循环。
- 暂停期间不累计时间；手动结束会保留已执行的工作和休息时长。关闭程序、休眠后恢复时，按原定时间推进到相应阶段或结束。
- 提示音开关在设置 → 提示音，音频选择、试听和恢复默认也在此页。

## 统计

- 今日摘要分别显示工作与休息的实际时长，包含当前正在进行的阶段。
- 日历、总量、分段使用可拖动轨道，支持点击、触摸和左右方向键；按下时从原位置滑向鼠标，拖动带轻微阻尼，松手平滑吸附，文字按滑块实际覆盖位置实时换色。
- 日历可选择工作或休息热力图，每屏 13 周，可翻阅历史；点击日期查看当天分段。
- 总量提供日、周、月的工作／休息堆叠柱状图。周从周一开始，月按自然月。
- 分段以不同颜色显示工作与休息，暂停会拆分为多个有效时段。
- 旧版单次计时记录归入工作，原有休息分类保留。历史记录按本地午夜拆分，唯一记录 ID 防止重复累计。
- 数据在用户目录 `.codex/timer-data`；统计保存失败保留待写队列，自动重试。

## 交互与设置

按键按下时弹性压缩并显示内阴影，释放后轻微超调回弹。系统关闭界面动画时直接显示终态。保留鼠标跟随高光、语言、外观与声音设置，以及悬浮按钮位置设置。

系统错误弹窗通过所属关系解析到 Codex 主窗口；独立对话框和工具窗口不会成为悬浮按钮的宿主。

## 安装

Windows 10/11：通过 Code → Download ZIP 下载并完整解压，双击 `install.cmd`。已有安装会保留计时状态、历史和偏好。依赖 Windows 自带的 .NET Framework 4.8。

中文使用微软雅黑，数字使用随包 Roboto；字体许可证见 `plugin/assets/fonts/LICENSE-Roboto.txt`。

## 验证

源码在 `source/src`，测试在 `source/tests`。执行 `source/scripts/build.ps1` 构建程序；循环、统计、交互、菜单结构和窗口跟随分别由 `test-cycle.ps1`、`test-stats.ps1`、`test-work-rest.ps1`、`test-structure.ps1`、`test-host-selection.ps1` 和 `test-window.ps1` 验证。

## 从源码构建

在 Windows PowerShell 中运行：

```powershell
powershell -ExecutionPolicy Bypass -File source/scripts/build.ps1
powershell -ExecutionPolicy Bypass -File source/scripts/package.ps1
```

安装包生成在 `dist/`。GitHub Release 可附加该 ZIP，用户完整解压后运行 `install.cmd`。

仓库中的测试使用独立测试目录；个人计时数据、安装备份和测试生成文件不纳入版本控制。Roboto 字体保留其原许可证；项目源码的开源许可证由维护者另行指定。
