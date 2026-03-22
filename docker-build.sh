#!/bin/bash
# Docker 编译脚本 - 在容器中编译 WPF 项目

echo "🔧 创建 Docker 编译环境..."

# 检查 Docker 是否安装
if ! command -v docker &> /dev/null; then
    echo "❌ Docker 未安装，请先安装 Docker"
    echo "安装命令: sudo apt install docker.io"
    exit 1
fi

# 创建 Dockerfile
cat > Dockerfile << 'EOF'
FROM mcr.microsoft.com/dotnet/sdk:8.0

WORKDIR /app

# 复制项目文件
COPY . .

# 安装必要的工具
RUN dotnet tool install -g dotnet-format

# 设置工作目录
WORKDIR /app/src/IndustrialControlHMI

# 显示项目信息
RUN dotnet --info

# 编译项目
CMD ["dotnet", "build", "--verbosity", "normal"]
EOF

echo "📦 构建 Docker 镜像..."
docker build -t industrial-hmi-builder .

echo "🚀 运行编译..."
docker run --rm industrial-hmi-builder

echo "📋 编译完成！"
echo ""
echo "📁 输出文件位置:"
echo "- 在 Windows 上: bin/Debug/net8.0-windows/"
echo "- 在 Linux 上: 只能编译，不能运行 WPF 应用"
echo ""
echo "💡 提示: WPF 应用程序只能在 Windows 上运行"
echo "      Linux 上只能进行代码编译检查"