#!/bin/bash
# 批量修复缺少的 using 指令

echo "🔧 批量修复缺少的 using 指令..."
echo "========================================"

# 修复函数：在文件顶部添加 using 指令
add_using() {
    local file="$1"
    local namespace="$2"
    
    if [ ! -f "$file" ]; then
        echo "❌ 文件不存在: $file"
        return 1
    fi
    
    # 检查是否已经有该 using
    if grep -q "using $namespace;" "$file"; then
        echo "   ✅ $file 已有 using $namespace;"
        return 0
    fi
    
    # 在第一个 using 之前插入新的 using
    if grep -q "^using" "$file"; then
        # 找到第一个 using 的行号
        first_using_line=$(grep -n "^using" "$file" | head -1 | cut -d: -f1)
        
        # 在第一个 using 之前插入
        sed -i "${first_using_line}iusing $namespace;" "$file"
        echo "   ✅ $file 已添加 using $namespace; (插入到第 $first_using_line 行)"
    else
        # 如果没有 using，在 namespace 声明之前插入
        if grep -q "^namespace" "$file"; then
            namespace_line=$(grep -n "^namespace" "$file" | head -1 | cut -d: -f1)
            sed -i "${namespace_line}iusing $namespace;" "$file"
            echo "   ✅ $file 已添加 using $namespace; (在 namespace 前)"
        else
            # 如果连 namespace 都没有，在文件开头添加
            sed -i "1iusing $namespace;" "$file"
            echo "   ✅ $file 已添加 using $namespace; (文件开头)"
        fi
    fi
}

# 修复 CommunicationManager.cs
echo "1. 修复 CommunicationManager.cs:"
add_using "src/IndustrialControlHMI/Services/Communication/CommunicationManager.cs" "System.IO"
add_using "src/IndustrialControlHMI/Services/Communication/CommunicationManager.cs" "System.Text.Json"

# 修复 CommunicationServiceExtensions.cs
echo ""
echo "2. 修复 CommunicationServiceExtensions.cs:"
add_using "src/IndustrialControlHMI/Services/Communication/CommunicationServiceExtensions.cs" "System.IO"
add_using "src/IndustrialControlHMI/Services/Communication/CommunicationServiceExtensions.cs" "System.Text.Json"
add_using "src/IndustrialControlHMI/Services/Communication/CommunicationServiceExtensions.cs" "System.Text"
add_using "src/IndustrialControlHMI/Services/Communication/CommunicationServiceExtensions.cs" "System.Net"

# 修复 CommunicationViewModel.cs
echo ""
echo "3. 修复 CommunicationViewModel.cs:"
add_using "src/IndustrialControlHMI/ViewModels/CommunicationViewModel.cs" "System.IO"
add_using "src/IndustrialControlHMI/ViewModels/CommunicationViewModel.cs" "System.Text.Json"

# 修复 PlcDataBindingService.cs
echo ""
echo "4. 修复 PlcDataBindingService.cs:"
add_using "src/IndustrialControlHMI/Services/PlcDataBindingService.cs" "System.Timers"

# 修复 FlowchartViewModel.cs
echo ""
echo "5. 修复 FlowchartViewModel.cs:"
add_using "src/IndustrialControlHMI/ViewModels/FlowchartViewModel.cs" "System.Timers"

# 修复 AlarmManagementViewModel.cs
echo ""
echo "6. 修复 AlarmManagementViewModel.cs:"
add_using "src/IndustrialControlHMI/ViewModels/AlarmManagementViewModel.cs" "System.Text"

# 修复 HtmlToInlineConverter.cs
echo ""
echo "7. 修复 HtmlToInlineConverter.cs:"
add_using "src/IndustrialControlHMI/Converters/HtmlToInlineConverter.cs" "System.Text.RegularExpressions"

# 修复 SettingsViewModel.cs
echo ""
echo "8. 修复 SettingsViewModel.cs:"
add_using "src/IndustrialControlHMI/ViewModels/SettingsViewModel.cs" "System.Net"

echo ""
echo "🔍 验证修复结果："
./find-missing-usings.sh | tail -20

echo ""
echo "✅ 批量修复完成！"
echo "========================================"
echo "💡 下一步："
echo "1. 提交修复：git add . && git commit -m '修复缺少的 using 指令'"
echo "2. 推送到远程：git push origin master"
echo "3. 在 Windows 上测试编译：dotnet build"