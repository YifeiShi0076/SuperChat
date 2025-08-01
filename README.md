# 💬 SuperChat - 基于 Redis 的局域网即时通讯工具

**LANChat** 是一款使用 C# 和 WPF 技术开发的局域网内即时通讯桌面应用，基于 Redis 实现实时消息发布与订阅，支持文字聊天、好友管理和群聊功能，适用于公司、学校或家庭内部的轻量级通信场景。

## ✨ 主要功能

- 📡 **局域网文字通信**：无需公网服务器，Redis 发布/订阅机制实现实时消息传输
- 👤 **好友系统**：添加、管理好友，支持双向私聊
- 👥 **群聊系统**：
  - 创建群聊
  - 加入/退出群聊
  - 群组消息广播
- 🖥️ **本地部署，无依赖浏览器**

## 🛠️ 技术栈

- **开发语言**：C#
- **界面框架**：WPF (.NET 8.0 或更高)
- **消息中间件**：Redis（用于消息发布/订阅、好友和群数据管理）
- **本地运行环境**：Windows 10/11

## 🗂️ 项目结构

