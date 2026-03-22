# 修复通讯协议编译错误脚本
Write-Host "正在修复通讯协议编译错误..." -ForegroundColor Cyan

# 1. 清理项目
Write-Host "步骤1: 清理项目..." -ForegroundColor Yellow
dotnet clean

# 2. 恢复NuGet包
Write-Host "步骤2: 恢复NuGet包..." -ForegroundColor Yellow
dotnet restore

# 3. 构建项目
Write-Host "步骤3: 构建项目..." -ForegroundColor Yellow
$buildResult = dotnet build

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ 构建成功！" -ForegroundColor Green
} else {
    Write-Host "❌ 构建失败，正在分析错误..." -ForegroundColor Red
    
    # 分析常见错误
    if ($buildResult -match "CS1069.*System.IO.Ports") {
        Write-Host "检测到 System.IO.Ports 引用问题" -ForegroundColor Yellow
        Write-Host "已添加 System.IO.Ports 包引用到项目文件" -ForegroundColor Green
    }
    
    if ($buildResult -match "CS0246.*Parity") {
        Write-Host "检测到 Parity 类型引用问题" -ForegroundColor Yellow
        Write-Host "已添加 using System.IO.Ports; 指令" -ForegroundColor Green
    }
    
    if ($buildResult -match "warning CS8625") {
        Write-Host "检测到可为空引用类型警告" -ForegroundColor Yellow
        Write-Host "建议: 在参数类型后添加 ? 表示可为空" -ForegroundColor Cyan
        Write-Host "示例: string? message = null" -ForegroundColor Cyan
    }
}

# 4. 运行测试（如果有）
Write-Host "步骤4: 运行测试..." -ForegroundColor Yellow
if (Test-Path "tests") {
    dotnet test
}

Write-Host "`n修复完成！" -ForegroundColor Green
Write-Host "如果仍有错误，请检查以下文件：" -ForegroundColor Cyan
Write-Host "1. src/IndustrialControlHMI/Services/Communication/CommunicationServiceExtensions.cs" -ForegroundColor White
Write-Host "2. src/IndustrialControlHMI/Services/Communication/Protocols/SerialProtocol.cs" -ForegroundColor White
Write-Host "3. src/IndustrialControlHMI/IndustrialControlHMI.csproj" -ForegroundColor White

Write-Host "`n常见问题解决方案：" -ForegroundColor Yellow
Write-Host "1. 缺少 using System.IO.Ports; - 已在文件中添加" -ForegroundColor White
Write-Host "2. 缺少 System.IO.Ports 包 - 已添加到项目文件" -ForegroundColor White
Write-Host "3. 可为空引用警告 - 在类型后添加 ? 符号" -ForegroundColor White