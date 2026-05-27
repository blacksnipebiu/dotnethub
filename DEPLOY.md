# DotNetHub - Windows x64 部署指南

## 📦 版本

- .NET 8 自包含发布 (self-contained)
- 架构: Windows x64
- 端口: 5100

## 🔨 构建发布包

```bash
cd backend
dotnet publish -c Release -r win-x64 --self-contained -o ../publish
cp -r ../frontend/dist ../publish/wwwroot
```

## 🚀 快速部署

### 1. 下载

将 `publish/` 文件夹完整复制到 Windows 服务器上（任意路径，如 `C:\DotNetHub`）。

### 2. 直接运行

双击 `DotNetHub.Server.exe` 或在命令行运行：

```cmd
cd C:\DotNetHub
DotNetHub.Server.exe
```

### 3. 访问

打开浏览器访问 `http://localhost:5100`

### 4. 默认管理员

- 用户名: `admin`
- 密码: `admin123`

## 🔧 自定义端口

编辑 `appsettings.json` 或设置环境变量：

```cmd
set ASPNETCORE_URLS=http://0.0.0.0:8080
DotNetHub.Server.exe
```

## 🐳 Docker 部署

也可以使用 Docker 一键部署：

```bash
docker compose up -d
```

## 📁 目录结构（部署后）

```
C:\DotNetHub\
├── DotNetHub.Server.exe     # 主程序（双击运行）
├── appsettings.json          # 配置文件
├── dotnethub.db              # 数据库（自动创建）
├── wwwroot/                  # 前端界面
└── *.dll                     # .NET 运行时依赖
```

> ⚠️ 不需要安装 .NET SDK — 自包含发布已包含运行时。
