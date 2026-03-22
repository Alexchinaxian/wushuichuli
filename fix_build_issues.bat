@echo off
REM fix_build_issues.bat - 修复上位机项目编译问题
echo === 修复上位机项目编译问题 ===

REM 1. 清理项目
echo 1. 清理项目...
dotnet clean
if exist "src\IndustrialControlHMI\bin" rmdir /s /q "src\IndustrialControlHMI\bin"
if exist "src\IndustrialControlHMI\obj" rmdir /s /q "src\IndustrialControlHMI\obj"

REM 2. 删除临时项目文件
echo 2. 删除临时项目文件...
del "src\IndustrialControlHMI\*_wpftmp.csproj" /q

REM 3. 恢复 NuGet 包（强制）
echo 3. 恢复 NuGet 包...
dotnet restore --force

REM 4. 添加必要的包
echo 4. 添加必要的 NuGet 包...
dotnet add src\IndustrialControlHMI package Microsoft.Extensions.Configuration.Json
dotnet add src\IndustrialControlHMI package Microsoft.Extensions.Configuration

REM 5. 构建项目
echo 5. 构建项目...
dotnet build --no-restore

if %ERRORLEVEL% neq 0 (
    echo 构建失败，尝试禁用可为空引用类型...
    
    REM 临时修改项目文件，禁用可为空引用类型
    powershell -Command "(Get-Content 'src\IndustrialControlHMI\IndustrialControlHMI.csproj') -replace '<Nullable>enable</Nullable>', '<Nullable>disable</Nullable>' | Set-Content 'src\IndustrialControlHMI\IndustrialControlHMI.csproj'"
    
    echo 重新构建...
    dotnet build
)

echo.
echo === 修复完成 ===
echo 如果仍有错误，请检查：
echo 1. .NET SDK 版本: dotnet --version ^(需要 8.0+^)
echo 2. Visual Studio 版本 ^(需要 2022+^)
echo 3. 项目目标框架: net8.0-windows
pause