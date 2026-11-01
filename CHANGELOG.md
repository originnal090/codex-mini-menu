# Major updates / 重大更新

Version tags preserve these source milestones. [Download the current Windows release](https://github.com/originnal090/codex-mini-menu/releases/latest).

以下版本均保留源码标签；当前安装包见 [发布页](https://github.com/originnal090/codex-mini-menu/releases/latest)。

## [v2.17.0](https://github.com/originnal090/codex-mini-menu/tree/v2.17.0) · 中心旋转柔光 / Rotating glass

- Two diffuse theme colors revolve around the center every 12 seconds; the whole background and conic border share the same color field and angle.
- 冷暖柔光覆盖整张卡片，围绕中心持续旋转，经过的边框同步变色，中心柔和淡出，适配明暗主题。
- 4,683 data/desktop assertions passed, including native mouse dragging. [Design & measurements / 设计与实测](docs/rotating-glass-2.17.0.md)

## [v2.16.1](https://github.com/originnal090/codex-mini-menu/tree/v2.16.1) · 圆环真实鼠标操作 / Launcher input

- Restore native hit testing through the open glass card, including dragging after tilt return and reopening.
- 修复卡片展开后圆环无法拖动的问题，加入真实鼠标按下、移动、释放与点击关闭回归。[Details / 说明](docs/launcher-input-2.16.1.md)

## [v2.16.0](https://github.com/originnal090/codex-mini-menu/tree/v2.16.0) · 锥形渐变与流畅移动 / Conic border & motion

- Angular border texture and shared glass under the launcher, including screen-edge placement.
- 建立 90+ 移动更新率验收目标，优化拖动缓存，验证倾斜与圆环拖动两条路径。[Measurements / 实测](docs/conic-motion-2.16.0.md)

## [v2.15.0](https://github.com/originnal090/codex-mini-menu/tree/v2.15.0) · 流畅浮岛与柔和反光 / Floating editor polish

- Halve editor lift time from 380 to **190 ms**; remove the unit-label row and rebalance spacing.
- 上浮时长减半，缓存模糊背景；按下与回正时的反光亮度平滑过渡，圆环交接保持稳定。[Details / 说明](docs/motion-polish-2.15.0.md)

## [v2.14.1](https://github.com/originnal090/codex-mini-menu/tree/v2.14.1) · 从功能岛展开 / Expanding duration islands

- Work/rest islands expand into floating editors with progressive backdrop blur. Align the Back capsule with the card corner and launcher.
- 时长设置从所点功能岛展开，返回胶囊与圆角、圆环对齐，支持上下展开布局。[Duration interaction / 时长交互](docs/duration-float.md)

## [v2.14.0](https://github.com/originnal090/codex-mini-menu/tree/v2.14.0) · 闪卡细节打磨 / Card refinement

- Concentric corner controls, stable glyph rendering, and a launcher included in the card's perspective tilt.
- 圆角与控件同心，倾斜文字保持稳定，圆环随卡片一起转动，内容缓存与反光分层。[Details / 说明](docs/card-refinement-2.14.0.md)

## [v2.13.0](https://github.com/originnal090/codex-mini-menu/tree/v2.13.0) · 银色玻璃闪卡 / Live reflective card

- Hold the right button to tilt the live menu; silver highlights track the angle, with a smooth return on release.
- 右键倾斜整张实时卡片，银白高光随角度流动，松开柔和回正。[Details / 说明](docs/silver-card-2.13.0.md)

## [v2.12.0](https://github.com/originnal090/codex-mini-menu/tree/v2.12.0) · 悬浮岛与上下文 / Islands & context

- Unify timer, statistics, context and settings as rounded islands in one glass card, with scrolling for compact displays.
- 统一悬浮岛布局，集成当前聊天上下文占用与阈值提醒。[Islands / 界面](docs/menu-islands-2.12.0.md) · [Context / 上下文](CONTEXT.md)
