# MediaLiveTile.Hybrid v2.0.1.0

MediaLiveTile.Hybrid 是基于 **UWP + FullTrust TrayHost + MSIX Package** 的混合架构媒体动态磁贴工具。

- 原 UWP 版本：https://github.com/Yizuka17/MediaLiveTile
- 项目地址：https://github.com/Yizuka17/MediaLiveTile.Hybrid
- 截图示例：<img width="2536" height="1377" alt="Snipaste_2026-04-23_12-15-38" src="https://github.com/user-attachments/assets/49f2ad9a-f9d6-4247-aac5-0dec8341917e" />

本版本是 2.0 混合架构路线的稳定性修补版本，主要目标是解决 1.x 纯 UWP 架构下：

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
- UWP 页面恢复为接近 1.x 的控制台形式
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

## v2.0.1.0 更新摘要

- 改进 TrayHost 后台稳定性与异常兜底
- 优化媒体变化、切歌、磁贴更新时的容错处理
- 改进日志写入方式，减少调试 / Package 环境下的 WinRT Storage 依赖
- 改进状态快照读写与 UWP 同步体验
- 修复 UWP 窗口拉伸时可能出现透明区域的问题
- 保留中磁贴 / 大磁贴轮播显示，并降低图片缓存不刷新的概率

完整更新记录见 [CHANGELOG.md](CHANGELOG.md)。

---

## 已知问题 / 说明

当前版本整体已经可用，但仍有一些细节受系统 Shell、媒体来源和 Package 环境影响，主要体现在：

- 一些状态同步仍采用轻量轮询，存在小延迟
- 部分异常处理以“兜底运行”为主，日志细节仍可继续完善
- 某些 WinRT / Package / Shell 行为对系统环境比较敏感
- 小磁贴与开始菜单缓存行为仍可能受系统限制影响
- 某些媒体来源的封面 / 应用图标解析不一定稳定
- 预览与真实磁贴仍可能存在细微差异
- 兜底图标仍主要针对网易云音乐、Microsoft Edge、Google Chrome、Firefox

也就是说：

> 当前版本作为 2.0 混合架构路线的可用修补版本，后续仍会继续以稳定性和细节完善为主。

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

## 调试说明

调试 UWP + FullTrust 混合项目时，建议：

- 使用 `MediaLiveTile.Hybrid.Package` 作为启动项目
- 如遇 VS2022 调试期 `SEHException`，可尝试关闭 **XAML 热重载**
- 最终行为建议以 MSIX Package 部署运行结果为准

---

## 版本信息

**MediaLiveTile.Hybrid v2.0.1.0**

这是 2.0 混合架构路线的稳定性修补版本。

---

## 反馈说明

如果你在使用中遇到问题，建议优先提供：

- Windows 版本
- 媒体来源（网易云 / Edge / Chrome 等）
- 磁贴尺寸
- 日志文件内容

后续版本将继续以稳定性和细节完善为主。
