# Codex 小菜单 · Windows 悬浮计时器与休息提醒

**专注写代码，也记得站起来！**

一个陪在 Codex 身边的 Windows 悬浮计时器：支持工作／休息倒计时、自定义番茄钟式专注循环、定时休息提醒、专注时间统计与聊天上下文占用。还有可以倾斜把玩的银色闪卡，以及围绕卡片中心旋转、与边框同步变色的冷暖模糊柔光。

[English](README.md) · **简体中文** · **[下载 v2.17.0](https://github.com/originnal090/codex-mini-menu/releases/latest)** · [更新记录](CHANGELOG.md) · [反馈与建议](https://github.com/originnal090/codex-mini-menu/issues)

Windows 10 / 11 · .NET Framework 4.8 · 简体中文 / English

## 为什么做这个小插件？

和 Codex 聊着聊着，几个小时就过去了。再问一个问题，再改一个功能，再试一个想法……代码越写越起劲，回过神来才发现自己一直没离开椅子，腰都坐酸了。

于是我做了这个小菜单：专注时陪你计时，到点提醒你起来走走、伸个懒腰，给自己留一点休息的时间。**项目要好好做，身体也要好好照顾！**

既然它要每天陪在桌面上，那就让它也好看、好玩一点。玻璃要有层次，光要会流动，卡片要能跟着手里的动作倾斜。一个小工具，也值得认真打磨！

## 让一张小卡片，有光、有动作

<table>
<tr><th>冷暖模糊光，围绕中心旋转</th><th>按住右键，把玩银色闪卡</th></tr>
<tr><td><img src="docs/images/rotating-glass.gif" width="320" alt="冷暖模糊背景围绕卡片中心旋转，锥形渐变边框同步变色"></td><td><img src="docs/images/silver-card-live.gif" width="320" alt="整张实时卡片透视倾斜，银色高光随角度流动，松开后柔和回正"></td></tr>
</table>

**光转到哪里，边框就跟到哪里。** 两束来自主题配色的冷暖柔光，以卡片中心为轴持续旋转，约 **12 秒一圈**。整张背景与锥形渐变描边共用颜色场和角度，颜色沿卡片流动，再延伸到对应的边缘；中心柔和淡出，透出玻璃底色。

**它真的是一张可以倾斜的闪卡。** 按住 **鼠标右键** 移动，整张菜单随之产生透视倾斜，银白反光跟着角度流过卡面。松开后平滑回正，计时内容保持实时更新，圆环也跟随整张卡片一起倾斜。

*动图由真实 WPF 界面脚本渲染，使用演示数据。动图经过压缩用于展示，播放帧率不代表实际运行性能。*

## 好用，也要有一点手感

<p align="center"><img src="docs/images/floating-editor.gif" width="320" alt="工作与休息功能岛展开为时长浮岛，背景渐进模糊，时间滚轮保持清晰"></p>

点击 **工作** 或 **休息**，对应功能岛展开为悬浮时长面板。背景模糊渐渐铺开，时间滚轮保持清晰，**190 ms** 完成上浮；关闭后收回计时页面。

- **点击圆环**展开／收起菜单，**按住左键拖动圆环**调整位置；卡片展开后也可以拖。
- 悬浮入口跟随当前 Codex 窗口，工具以圆润的功能岛组织，随手就能打开。
- 指针柔光、按钮弹性反馈、带阻尼的统计滑轨，让小动作也有回应。
- 自动跟随 Codex 明暗主题与配色，透明度可在 **0–90%** 之间调整。
- 尊重应用和系统动画设置；隐藏卡片后停止光效动画。

<table>
<tr><th>深色玻璃</th><th>浅色玻璃</th></tr>
<tr><td><img src="docs/images/showcase-timer-dark.png" width="280" alt="深色主题下的冷暖旋转玻璃计时器"></td><td><img src="docs/images/showcase-timer-light.png" width="280" alt="浅色主题下的玻璃计时器"></td></tr>
</table>

## 漂亮之外，也认真做事

| 工具 | 能做什么 |
| --- | --- |
| 工作／休息计时 | 每段支持 **1 秒至 24 小时**，按 **1–99 组**工作 → 休息执行，可暂停、继续、重置。 |
| 提示音 | 随时开关提醒，选择自己的 WAV / MP3、试听，或使用系统默认声音。 |
| 活动统计 | 查看 **13 周热力图**、每日分段，以及按日／周／月分类的工作与休息总量。 |
| 上下文圆环 | 查看当前聊天的上下文占用；80% 变橙，90% 变红，每个聊天触发一次阈值提醒。[兼容性](CONTEXT.md) |

收起菜单后继续计时。休眠或退出期间无法即时发声提醒，恢复后按绝对截止时间推进到相应阶段或结束。统计来自实际计时记录，不计入暂停时长。

## 装好它，给下一次休息留个提醒

1. 前往 **[最新发布页](https://github.com/originnal090/codex-mini-menu/releases/latest)**，下载 `CodexMiniMenu-Windows-2.17.0.zip`。
2. 完整解压，运行 **`install.cmd`**。
3. 打开桌面的 **Codex Mini Menu**，设好工作与休息时长，就可以开始啦！

安装器会注册本地 Codex 插件并创建快捷方式；升级保留计时状态和偏好。需要 .NET Framework 4.8，运行插件无需 Node.js 或 Python。托盘支持打开菜单、恢复位置和退出；卸载请运行 `uninstall.cmd`。

状态、历史、外观和声音设置保存在本机 `%USERPROFILE%\.codex\timer-data`。Windows 背景模糊取决于合成能力和系统透明效果设置。

## 这几次大更新，都留下来了

从悬浮岛界面，到银色闪卡、流畅浮岛，再到背景与边框同步旋转的玻璃光效：**[更新记录](CHANGELOG.md)** 收录每次重大变化，版本标签保留对应源码快照。

2.17.0 完成 **4,683 项数据与桌面检查**，包括真实鼠标拖动。实测 240 Hz／DPI 1.5 环境中，开启光效后倾斜约 **99.3 次更新／秒**，拖动约 **108.5 次更新／秒**。这是应用与渲染更新率，并非独立测得的 GPU 呈现帧率。[设计与实测](docs/rotating-glass-2.17.0.md)

## 一起把它变得更好

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File ./dev.ps1 -Action TestUI
powershell -NoProfile -ExecutionPolicy Bypass -File ./dev.ps1 -Action Package
```

打开 `CodexMiniMenu.code-workspace` 即可开发。UI 验证需要交互式 Windows 桌面。[开发说明](docs/development.md) · [演示素材生成](docs/showcase.md)

想要一个新的小工具？有更舒服的交互点子？或者发现哪里不太对劲？**[欢迎来聊！](https://github.com/originnal090/codex-mini-menu/issues)** 也欢迎提交 Pull Request，带上改动说明与验证结果。

界面使用微软雅黑与 Roboto。Roboto 许可证见 [LICENSE-Roboto.txt](plugin/assets/fonts/LICENSE-Roboto.txt)；项目源码许可证尚未指定。这是面向 Codex 的独立社区工具。
