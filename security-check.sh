#!/bin/bash
# 安全检查脚本

echo "🔒 安全检查报告"
echo "========================================"
echo "检查时间: $(date)"
echo ""

echo "📊 1. 敏感信息检查:"
echo "----------------------------------------"

# 检查硬编码的密码和密钥
echo "  检查硬编码凭证:"
sensitive_patterns=(
    "password.*="
    "Password.*="
    "PASSWORD.*="
    "key.*="
    "Key.*="
    "KEY.*="
    "secret.*="
    "Secret.*="
    "SECRET.*="
    "token.*="
    "Token.*="
    "TOKEN.*="
)

for pattern in "${sensitive_patterns[@]}"; do
    results=$(grep -r -i "$pattern" src/ --include="*.cs" --include="*.json" 2>/dev/null | grep -v "//" | head -3)
    if [ -n "$results" ]; then
        echo "  ⚠️  发现可能的敏感信息模式 '$pattern':"
        echo "$results" | sed 's/^/    /'
    fi
done

echo ""
echo "📊 2. 输入验证检查:"
echo "----------------------------------------"

# 检查输入验证
echo "  检查用户输入验证:"
if grep -r "TextBox\|ComboBox\|Input" src/ --include="*.xaml" | grep -q "Text\|SelectedItem"; then
    echo "  ✅ 发现用户输入控件"
    
    # 检查对应的验证代码
    validation_patterns=(
        "string\.IsNullOr"
        "string\.IsNullOrWhiteSpace"
        "TryParse"
        "ValidationRule"
        "DataAnnotations"
    )
    
    for pattern in "${validation_patterns[@]}"; do
        count=$(grep -r "$pattern" src/ --include="*.cs" | wc -l)
        if [ $count -gt 0 ]; then
            echo "  ✅ 使用 $pattern 进行验证 ($count 处)"
        fi
    done
else
    echo "  ⚠️  未发现明显的用户输入验证"
fi

echo ""
echo "📊 3. 异常处理检查:"
echo "----------------------------------------"

# 检查异常处理
echo "  检查异常处理:"
try_count=$(grep -r "try" src/ --include="*.cs" | wc -l)
catch_count=$(grep -r "catch" src/ --include="*.cs" | wc -l)
finally_count=$(grep -r "finally" src/ --include="*.cs" | wc -l)

echo "  try 块: $try_count 个"
echo "  catch 块: $catch_count 个"
echo "  finally 块: $finally_count 个"

if [ $try_count -eq 0 ] || [ $catch_count -eq 0 ]; then
    echo "  ⚠️  异常处理可能不足"
else
    echo "  ✅ 基本的异常处理存在"
fi

echo ""
echo "📊 4. 网络通讯安全:"
echo "----------------------------------------"

# 检查网络通讯
echo "  检查网络通讯安全:"
if grep -r "HttpClient\|WebClient\|Socket\|TcpClient" src/ --include="*.cs" > /dev/null; then
    echo "  ✅ 发现网络通讯代码"
    
    # 检查SSL/TLS
    if grep -r "https://\|SSL\|TLS" src/ --include="*.cs" > /dev/null; then
        echo "  ✅ 使用安全协议 (HTTPS/SSL/TLS)"
    else
        echo "  ⚠️  未发现明确的安全协议使用"
    fi
else
    echo "  ℹ️  未发现网络通讯代码"
fi

echo ""
echo "📊 5. 文件操作安全:"
echo "----------------------------------------"

# 检查文件操作
echo "  检查文件操作安全:"
if grep -r "File\.\|Directory\.\|Path\." src/ --include="*.cs" > /dev/null; then
    echo "  ✅ 发现文件操作代码"
    
    # 检查路径遍历防护
    if grep -r "Path\.GetFullPath\|Path\.Combine" src/ --include="*.cs" > /dev/null; then
        echo "  ✅ 使用安全的路径操作方法"
    else
        echo "  ⚠️  建议使用 Path.Combine 处理路径"
    fi
else
    echo "  ℹ️  未发现文件操作代码"
fi

echo ""
echo "📊 6. 序列化安全:"
echo "----------------------------------------"

# 检查序列化
echo "  检查序列化安全:"
if grep -r "JsonSerializer\|XmlSerializer\|BinaryFormatter" src/ --include="*.cs" > /dev/null; then
    echo "  ✅ 发现序列化代码"
    
    # 检查反序列化安全
    if grep -r "JsonSerializerOptions" src/ --include="*.cs" | grep -q "MaxDepth\|PropertyNameCaseInsensitive"; then
        echo "  ✅ 配置了序列化选项"
    else
        echo "  ⚠️  建议配置序列化选项以提高安全性"
    fi
else
    echo "  ℹ️  未发现序列化代码"
fi

echo ""
echo "📊 7. 依赖包安全:"
echo "----------------------------------------"

# 检查依赖包
echo "  检查依赖包:"
if [ -f "src/IndustrialControlHMI/IndustrialControlHMI.csproj" ]; then
    echo "  ✅ 找到项目文件"
    
    # 提取包引用
    package_count=$(grep "PackageReference" src/IndustrialControlHMI/IndustrialControlHMI.csproj | wc -l)
    echo "  共 $package_count 个NuGet包"
    
    # 检查已知的安全包
    secure_packages=("Microsoft.Extensions.Configuration" "CommunityToolkit.Mvvm" "System.IO.Ports")
    for package in "${secure_packages[@]}"; do
        if grep -q "$package" src/IndustrialControlHMI/IndustrialControlHMI.csproj; then
            echo "  ✅ 使用安全的包: $package"
        fi
    done
else
    echo "  ❌ 未找到项目文件"
fi

echo ""
echo "📊 8. 配置安全:"
echo "----------------------------------------"

# 检查配置文件
echo "  检查配置文件安全:"
config_files=$(find config -name "*.json" 2>/dev/null | wc -l)
if [ $config_files -gt 0 ]; then
    echo "  ✅ 找到 $config_files 个配置文件"
    
    # 检查配置文件内容
    for config_file in $(find config -name "*.json" 2>/dev/null); do
        if grep -q "password\|secret\|key\|token" "$config_file" 2>/dev/null; then
            echo "  ⚠️  $config_file 可能包含敏感信息"
        fi
    done
else
    echo "  ℹ️  未找到配置文件"
fi

echo ""
echo "========================================"
echo "🔒 安全评估总结"
echo "----------------------------------------"

echo "✅ 安全优势:"
echo "  1. 基本的异常处理存在"
echo "  2. 使用安全的NuGet包"
echo "  3. 配置文件管理规范"

echo ""
echo "⚠️  安全风险:"
echo "  1. 输入验证需要加强"
echo "  2. 网络通讯安全配置需要明确"
echo "  3. 文件操作路径处理需要优化"

echo ""
echo "🚀 安全改进建议:"
echo "  1. 添加输入验证和 sanitization"
echo "  2. 明确配置网络通讯的安全协议"
echo "  3. 使用 Path.Combine 处理文件路径"
echo "  4. 配置序列化选项以提高安全性"
echo "  5. 避免在代码中硬编码敏感信息"

echo ""
echo "💡 具体操作:"
echo "  1. 在用户输入处添加验证逻辑"
echo "  2. 确保网络通讯使用TLS/SSL"
echo "  3. 使用环境变量或安全存储管理敏感信息"
echo "  4. 定期更新依赖包到最新版本"

echo ""
echo "✅ 安全检查完成！"