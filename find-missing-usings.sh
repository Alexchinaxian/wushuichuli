#!/bin/bash
# 查找缺少 using 指令的文件

echo "🔍 检查缺少 using 指令的文件..."
echo "========================================"

# 常见需要 using 的类型和对应的命名空间
declare -A TYPE_TO_NAMESPACE=(
    ["IOException"]="System.IO"
    ["FileStream"]="System.IO"
    ["StreamReader"]="System.IO"
    ["StreamWriter"]="System.IO"
    ["Directory"]="System.IO"
    ["File"]="System.IO"
    ["Path"]="System.IO"
    ["JsonSerializer"]="System.Text.Json"
    ["JsonDocument"]="System.Text.Json"
    ["JsonElement"]="System.Text.Json"
    ["HttpClient"]="System.Net.Http"
    ["WebClient"]="System.Net"
    ["Socket"]="System.Net.Sockets"
    ["TcpClient"]="System.Net.Sockets"
    ["UdpClient"]="System.Net.Sockets"
    ["IPAddress"]="System.Net"
    ["Dns"]="System.Net"
    ["DataTable"]="System.Data"
    ["DataSet"]="System.Data"
    ["SqlConnection"]="System.Data.SqlClient"
    ["SqlCommand"]="System.Data.SqlClient"
    ["SqlDataAdapter"]="System.Data.SqlClient"
    ["Timer"]="System.Timers"
    ["Stopwatch"]="System.Diagnostics"
    ["Process"]="System.Diagnostics"
    ["Regex"]="System.Text.RegularExpressions"
    ["Encoding"]="System.Text"
    ["CultureInfo"]="System.Globalization"
    ["DateTime"]="System"
    ["TimeSpan"]="System"
    ["Guid"]="System"
    ["Random"]="System"
    ["Math"]="System"
    ["Console"]="System"
    ["Environment"]="System"
)

echo "📋 检查结果："

TOTAL_PROBLEMS=0

for type in "${!TYPE_TO_NAMESPACE[@]}"; do
    namespace="${TYPE_TO_NAMESPACE[$type]}"
    
    # 查找使用该类型的文件
    files=$(find src -name "*.cs" -exec grep -l "\b$type\b" {} \; 2>/dev/null)
    
    for file in $files; do
        # 检查是否已经有对应的 using
        if ! grep -q "using $namespace;" "$file" && ! grep -q "using static $namespace\." "$file"; then
            # 检查是否在 System 命名空间中（不需要额外 using）
            if [ "$namespace" != "System" ] || ! grep -q "using System;" "$file"; then
                echo "⚠️  $file 使用 $type 但缺少 using $namespace;"
                TOTAL_PROBLEMS=$((TOTAL_PROBLEMS + 1))
            fi
        fi
    done
done

echo ""
echo "🔧 检查特定问题："

# 检查 IOException
echo "1. IOException 检查："
io_files=$(find src -name "*.cs" -exec grep -l "\bIOException\b" {} \; 2>/dev/null)
for file in $io_files; do
    if ! grep -q "using System.IO;" "$file"; then
        echo "   ⚠️  $file 使用 IOException 但缺少 using System.IO;"
    else
        echo "   ✅  $file 已有 using System.IO;"
    fi
done

# 检查常见的串口相关类型
echo ""
echo "2. 串口相关类型检查："
for type in "SerialPort" "Parity" "StopBits" "Handshake"; do
    files=$(find src -name "*.cs" -exec grep -l "\b$type\b" {} \; 2>/dev/null)
    for file in $files; do
        if ! grep -q "using System.IO.Ports;" "$file"; then
            echo "   ⚠️  $file 使用 $type 但缺少 using System.IO.Ports;"
        fi
    done
done

echo ""
echo "📊 统计："
echo "  发现 $TOTAL_PROBLEMS 个可能的 using 指令问题"
echo ""
echo "💡 修复建议："
echo "  1. 在文件顶部添加缺少的 using 指令"
echo "  2. 示例：using System.IO;"
echo "  3. 对于 System 命名空间，通常不需要额外 using"
echo ""
echo "✅ 检查完成！"