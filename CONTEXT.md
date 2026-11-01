# 上下文模块

悬浮圆环中心显示当前 Codex 聊天的已用上下文百分比，外圈继续显示计时进度。

- 少于 80%：跟随主题文字颜色。
- 达到 80%：橙色。
- 达到 90%：红色，并弹出一次不抢焦点的提醒，12 秒后收起，也可手动关闭。
- 每个聊天独立记录是否提醒过，切换回来或重启小菜单不会重复提醒。
- 切换聊天后约一秒更新；无数据或无法明确对应时显示 `—`。
- 从小菜单打开「上下文」，可查看聊天名称、已用 token、上下文容量和提醒规则。

## 数据与兼容性

支持 Windows 本地 Codex 聊天。通过 Windows 无障碍接口读取当前窗口的聊天标题，用只读本地索引对应聊天 ID，再读取该聊天最近的 `last_token_usage.total_tokens` 与 `model_context_window`。整个聊天累计 token 消耗不参与占比计算。

当前支持 Codex 的 `state_5.sqlite` 索引与本地 `sessions` / `archived_sessions` 记录格式。远程聊天没有本地记录、同名或截断标题无法唯一对应、索引不可读时显示 `—`。上下文压缩后等待新的用量记录。读数随 Codex 记录更新，不估算正在生成但尚未记录的 token。

不会读取其他软件或上传数据。仅在 `%USERPROFILE%\.codex\timer-data\context-alerts.json` 保存已提醒过的聊天 ID。

## 开发验证

`source/scripts/test-context.ps1` 验证用量解析、阈值、选择、压缩、部分写入和提醒去重。

`source/scripts/test-context-view.ps1` 验证模块入口、圆环数字、阈值颜色、计时外圈保留、语言与提醒窗，并生成界面截图。

`source/scripts/test-context-view.ps1 -HostWindow <HWND>` 对实际 Codex 窗口执行只读核查，输出解析出的聊天 ID 和用量。
