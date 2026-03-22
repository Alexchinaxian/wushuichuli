#!/bin/bash
# 跨平台编译检查脚本

echo "🔍 检查项目编译状态..."
echo "========================================"

# 1. 检查项目文件
echo "📁 项目结构检查:"
if [ -f "src/IndustrialControlHMI/IndustrialControlHMI.csproj" ]; then
    echo "✅ 找到主项目文件"
    
    # 检查目标框架
    TARGET_FRAMEWORK=$(grep -oP 'TargetFramework>\K[^<]+' src/IndustrialControlHMI/IndustrialControlHMI.csproj)
    echo "  目标框架: $TARGET_FRAMEWORK"
    
    # 检查项目类型
    if grep -q "UseWPF" src/IndustrialControlHMI/IndustrialControlHMI.csproj; then
        echo "  项目类型: WPF 应用程序 (Windows 专用)"
    fi
else
    echo "❌ 未找到项目文件"
    exit 1
fi

echo ""
echo "📦 NuGet 包依赖检查:"
if [ -f "src/IndustrialControlHMI/IndustrialControlHMI.csproj" ]; then
    # 提取包引用
    echo "  已安装的包:"
    grep "PackageReference" src/IndustrialControlHMI/IndustrialControlHMI.csproj | sed 's/.*Include="//;s/".*//' | sed 's/^/  - /'
fi

echo ""
echo "📄 源代码文件检查:"
# 统计各类文件数量
CS_FILES=$(find src -name "*.cs" | wc -l)
XAML_FILES=$(find src -name "*.xaml" | wc -l)
echo "  C# 文件: $CS_FILES 个"
echo "  XAML 文件: $XAML_FILES 个"

echo ""
echo "🔧 编译问题检查:"
echo "  1. 检查 using 指令..."
PROBLEM_FILES=0
for file in $(find src -name "*.cs"); do
    # 检查是否缺少必要的 using
    if grep -q "Parity\|StopBits\|SerialPort" "$file" && ! grep -q "using System.IO.Ports" "$file"; then
        echo "  ⚠️  $file 可能缺少 using System.IO.Ports"
        PROBLEM_FILES=$((PROBLEM_FILES + 1))
    fi
done

if [ $PROBLEM_FILES -eq 0 ]; then
    echo "  ✅ 未发现明显的 using 指令问题"
fi

echo ""
echo "📋 编译建议:"
echo "  1. 此项目是 WPF 应用程序，需要在 Windows 上编译运行"
echo "  2. 在 Windows 上使用: dotnet build"
echo "  3. 在 Windows 上运行: dotnet run 或直接运行 exe 文件"
echo "  4. 当前环境 (Linux) 只能进行代码分析，不能运行 WPF 应用"

echo ""
echo "🚀 快速编译命令 (Windows):"
cat << 'EOF'
# 清理项目
dotnet clean

# 恢复包
dotnet restore

# 编译项目
dotnet build

# 运行项目
dotnet run --project src/IndustrialControlHMI

# 发布项目
dotnet publish -c Release
EOF

echo ""
echo "✅ 检查完成！"
echo "========================================"