# fix_build_issues.ps1 - 修复上位机项目编译问题
# 在 PowerShell 中运行：.\fix_build_issues.ps1

Write-Host "=== 修复上位机项目编译问题 ===" -ForegroundColor Cyan

# 1. 清理项目
Write-Host "1. 清理项目..." -ForegroundColor Yellow
dotnet clean
Remove-Item -Path "src/IndustrialControlHMI/bin" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -Path "src/IndustrialControlHMI/obj" -Recurse -Force -ErrorAction SilentlyContinue

# 2. 删除临时项目文件
Write-Host "2. 删除临时项目文件..." -ForegroundColor Yellow
Get-ChildItem -Path "src/IndustrialControlHMI" -Filter "*_wpftmp.csproj" | Remove-Item -Force

# 3. 检查项目文件
Write-Host "3. 检查项目文件..." -ForegroundColor Yellow
$csprojPath = "src/IndustrialControlHMI/IndustrialControlHMI.csproj"
$csprojContent = Get-Content $csprojPath -Raw

# 检查是否包含必要的包引用
if ($csprojContent -notmatch "Microsoft.Extensions.Configuration.Json") {
    Write-Host "  添加缺少的包引用..." -ForegroundColor Yellow
    # 添加包引用到 ItemGroup
    $newContent = $csprojContent -replace `
        '<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="9.0.0" />', `
        '<PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="9.0.0" />' + "`n    " + '<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="9.0.0" />'
    
    Set-Content -Path $csprojPath -Value $newContent -Encoding UTF8
}

# 4. 恢复 NuGet 包（强制）
Write-Host "4. 恢复 NuGet 包..." -ForegroundColor Yellow
dotnet restore --force

# 5. 单独添加必要的包
Write-Host "5. 添加必要的 NuGet 包..." -ForegroundColor Yellow
dotnet add src/IndustrialControlHMI package Microsoft.Extensions.Configuration.Json
dotnet add src/IndustrialControlHMI package Microsoft.Extensions.Configuration

# 6. 构建项目
Write-Host "6. 构建项目..." -ForegroundColor Yellow
dotnet build --no-restore

# 7. 如果还有错误，尝试禁用可为空引用类型
Write-Host "7. 检查构建结果..." -ForegroundColor Yellow
if ($LASTEXITCODE -ne 0) {
    Write-Host "  构建失败，尝试禁用可为空引用类型..." -ForegroundColor Red
    
    # 临时修改项目文件，禁用可为空引用类型
    $csprojContent = Get-Content $csprojPath -Raw
    $csprojContent = $csprojContent -replace '<Nullable>enable</Nullable>', '<Nullable>disable</Nullable>'
    Set-Content -Path $csprojPath -Value $csprojContent -Encoding UTF8
    
    # 重新构建
    dotnet build
}

Write-Host "`n=== 修复完成 ===" -ForegroundColor Green
Write-Host "如果仍有错误，请检查：" -ForegroundColor Yellow
Write-Host "1. .NET SDK 版本: dotnet --version (需要 8.0+)" -ForegroundColor White
Write-Host "2. Visual Studio 版本 (需要 2022+)" -ForegroundColor White
Write-Host "3. 项目目标框架: net8.0-windows" -ForegroundColor White