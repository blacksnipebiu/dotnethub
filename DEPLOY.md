# DotNetHub - Windows x64 部署指南

## 📦 版本

- .NET 8 自包含发布 (self-contained)
- 架构: Windows x64
- 端口: 5100

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

编辑 `appsettings.json`，修改 `Kestrel` 配置（或通过环境变量）：

```json
{
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://0.0.0.0:8080"
      }
    }
  }
}
```

或设置环境变量后启动：

```cmd
set ASPNETCORE_URLS=http://0.0.0.0:8080
DotNetHub.Server.exe
```

## 📁 目录结构

```
C:\DotNetHub\
├── DotNetHub.Server.exe     # 主程序
├── appsettings.json          # 配置文件
├── dotnethub.db              # 数据库（自动创建）
├── wwwroot/                  # 前端界面
└── *.dll                     # 运行时依赖
```

## 🐳 Docker 部署

也可以使用 Docker 一键部署：

```bash
docker compose up -d
```

## 📦 项目结构

```
dotnethub/
├── backend/          # .NET 8 Web API 源码
├── frontend/         # Vue 3 前端源码
├── publish/          # Windows x64 自包含发布
├── Dockerfile        # Docker 构建
└── docker-compose.yml
```

## 🔐 API 端点

| 方法     | 端点                                | 说明       |
|----------|-------------------------------------|------------|
| POST     | /api/auth/login                     | 登录       |
| POST     | /api/auth/register                  | 注册       |
| GET      | /api/projects                       | 项目列表   |
| POST     | /api/projects                       | 创建项目   |
| POST     | /api/projects/{id}/upload           | 上传文件   |
| POST     | /api/projects/{id}/build            | 构建项目   |
| POST     | /api/projects/{id}/deploy           | 部署运行   |
| POST     | /api/projects/{id}/stop             | 停止项目   |
| GET      | /api/admin/stats                    | 管理统计   |
