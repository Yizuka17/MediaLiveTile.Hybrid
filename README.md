# MediaLiveTile.Hybrid v2.0.0.0-preview

MediaLiveTile.Hybrid 是基于 **UWP + FullTrust TrayHost + MSIX Package** 的混合架构媒体动态磁贴工具。
原uwp版本：https://github.com/Yizuka17/MediaLiveTile

本版本是 2.0 预览版，主要目标是解决 1.x 纯 UWP 架构下：

- 无法稳定后台驻留
- 最小化后自动刷新能力弱
- 无托盘支持

等问题。

---

## 相比 1.x 的主要改进

- 引入 **Hybrid 混合架构**
  - UWP 负责前台设置、预览、固定磁贴
  - TrayHost 负责后台托盘、媒体监听、磁贴更新
- 支持 **托盘常驻**
- 支持 **主磁贴 + 次级磁贴** 后台更新
- 支持 **开机自启**
- 支持 **暂停监测 / 恢复监测**
- 支持 **多个次级磁贴绑定不同媒体槽位**
- UWP 页面重新恢复为接近 1.x 的控制台形式
- 引入 **状态快照 + 日志**

---

## 当前已支持

- 托盘自动启动
- 协议唤起 UWP
- 后台监听系统媒体会话
- 主媒体 / 次媒体排序
- 主磁贴更新
- SecondaryTile 更新
- 固定多个次级磁贴
- 开机自启（UWP + 托盘）
- 暂停监测 / 恢复监测（UWP + 托盘）
- UWP 读取 TrayHost 状态快照
- UWP 请求托盘立即刷新
- 日志输出

---

## 已知问题 / 预览版说明

当前版本仍是 **preview**，代码整体已经可用，但还不够完全健壮，主要体现在：

- 一些状态同步仍采用轻量轮询，存在小延迟
- 部分异常处理以“兜底运行”为主，日志虽有记录，但还不够细
- 某些 WinRT / Package / Shell 行为对系统环境比较敏感
- 小磁贴与开始菜单缓存行为仍可能受系统限制影响
- 某些媒体来源的封面 / 应用图标解析不一定稳定
- 预览与真实磁贴仍可能存在细微差异
- 鉴于本人较懒，图标包直接搬了以前的，也就是说依旧只针对网易云音乐、Microsoft Edge、Google Chrome、Firefox做了兜底图标

也就是说：

> 当前版本更适合作为 2.0 架构预览版和可用基础版，而不是完全收敛后的最终稳定版。

---

## 架构说明

### UWP Shell
负责：

- 设置
- 预览
- 固定磁贴
- 打开日志
- About / GitHub

### TrayHost
负责：

- 托盘常驻
- 媒体监听
- 背景刷新
- 磁贴更新
- 日志写入
- 状态快照写入

### Package
负责：

- 协议
- FullTrust 启动
- StartupTask
- 打包部署

---

## 版本信息

**MediaLiveTile.Hybrid v2.0.0.0-preview**

这是 2.0 混合架构路线的首个预览版本。
https://github.com/Yizuka17/MediaLiveTile.Hybrid

---

## 说明

如果你在使用中遇到问题，建议优先提供：

- Windows 版本
- 媒体来源（网易云 / Edge / Chrome 等）
- 磁贴尺寸
- 日志文件内容

后续版本将继续以稳定性和细节完善为主。
