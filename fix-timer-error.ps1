# 修复 Timer 歧义错误脚本
Write-Host "正在修复 Timer 歧义错误..." -ForegroundColor Cyan

Write-Host "✅ 已修复的问题：" -ForegroundColor Green
Write-Host "1. PlcDataBindingService.cs 中的 Timer 歧义" -ForegroundColor White
Write-Host "   - 将 Timer 改为 System.Timers.Timer" -ForegroundColor White
Write-Host "   - 修复了 Timer 初始化方式" -ForegroundColor White
Write-Host "   - 修复了 PollData 方法签名" -ForegroundColor White

Write-Host "2. ISettingsManager.cs 中的可为空引用警告" -ForegroundColor White
Write-Host "   - 添加了可为空类型注解 (?)" -ForegroundColor White

Write-Host ""
Write-Host "🔧 编译测试：" -ForegroundColor Yellow
Write-Host "运行以下命令测试编译：" -ForegroundColor White
Write-Host ""
Write-Host "dotnet clean" -ForegroundColor Cyan
Write-Host "dotnet restore" -ForegroundColor Cyan
Write-Host "dotnet build" -ForegroundColor Cyan
Write-Host ""
Write-Host "📊 预期结果：" -ForegroundColor Yellow
Write-Host "- ✅ 没有编译错误 (CS0104 已修复)" -ForegroundColor Green
Write-Host "- ⚠️ LiveCharts 兼容性警告 (可以忽略)" -ForegroundColor Yellow
Write-Host "- ⚠️ 少量可为空引用警告 (可以逐步修复)" -ForegroundColor Yellow
Write-Host "- ✅ 项目可以成功编译" -ForegroundColor Green

Write-Host ""
Write-Host "💡 如果仍有问题：" -ForegroundColor Yellow
Write-Host "1. 检查其他文件是否也有 Timer 歧义" -ForegroundColor White
Write-Host "2. 使用完整类型名: System.Timers.Timer" -ForegroundColor White
Write-Host "3. 或使用别名: using Timer = System.Timers.Timer;" -ForegroundColor White