#!/bin/bash
# 数据库状态检查脚本

echo "🗄️ 数据库状态检查报告"
echo "========================================"
echo "检查时间: $(date)"
echo ""

echo "📊 1. 数据库配置检查:"
echo "----------------------------------------"

# 检查数据库上下文
if [ -f "src/IndustrialControlHMI/Infrastructure/AppDbContext.cs" ]; then
    echo "  ✅ 找到 AppDbContext.cs"
    
    # 检查连接字符串配置
    if grep -q "Data Source=:memory:" "src/IndustrialControlHMI/Infrastructure/AppDbContext.cs"; then
        echo "  ⚠️  当前使用内存数据库（数据不持久化）"
    elif grep -q "Data Source=" "src/IndustrialControlHMI/Infrastructure/AppDbContext.cs"; then
        echo "  ✅ 使用文件数据库"
        connection_string=$(grep -o "Data Source=[^;]*" "src/IndustrialControlHMI/Infrastructure/AppDbContext.cs" | head -1)
        echo "  连接字符串: $connection_string"
    else
        echo "  ⚠️  未找到明确的连接字符串配置"
    fi
else
    echo "  ❌ 未找到 AppDbContext.cs"
fi

echo ""
echo "📊 2. 数据模型检查:"
echo "----------------------------------------"

# 检查实体类
entities=("AlarmRecord" "Setting")
for entity in "${entities[@]}"; do
    if find src -name "*.cs" -exec grep -l "class $entity" {} \; > /dev/null; then
        echo "  ✅ 找到实体类: $entity"
    else
        echo "  ⚠️  未找到实体类: $entity"
    fi
done

# 检查协议相关实体
protocol_entities=("ProtocolConfiguration" "Device" "PointMapping" "CommunicationLog")
for entity in "${protocol_entities[@]}"; do
    if find src -name "*.cs" -exec grep -l "class $entity" {} \; > /dev/null; then
        echo "  ✅ 找到协议实体: $entity"
    else
        echo "  ❌ 未找到协议实体: $entity"
    fi
done

echo ""
echo "📊 3. 仓储模式检查:"
echo "----------------------------------------"

# 检查仓储类
repositories=("AlarmRepository" "SettingRepository")
for repo in "${repositories[@]}"; do
    if [ -f "src/IndustrialControlHMI/Services/$repo.cs" ]; then
        echo "  ✅ 找到仓储类: $repo"
    else
        echo "  ⚠️  未找到仓储类: $repo"
    fi
done

# 检查协议相关仓储
protocol_repos=("IProtocolConfigurationRepository" "IDeviceRepository" "IPointMappingRepository")
for repo in "${protocol_repos[@]}"; do
    if find src -name "*.cs" -exec grep -l "$repo" {} \; > /dev/null; then
        echo "  ✅ 找到协议仓储接口: $repo"
    else
        echo "  ❌ 未找到协议仓储接口: $repo"
    fi
done

echo ""
echo "📊 4. 数据库迁移检查:"
echo "----------------------------------------"

# 检查迁移文件
if find . -name "*.Designer.cs" -o -name "*.resx" | grep -q "Migration"; then
    echo "  ✅ 找到数据库迁移文件"
else
    echo "  ⚠️  未找到数据库迁移文件（可能需要运行 Add-Migration）"
fi

# 检查数据库初始化
if grep -r "EnsureCreated\|Migrate" src/ --include="*.cs" > /dev/null; then
    echo "  ✅ 找到数据库初始化代码"
else
    echo "  ⚠️  未找到数据库初始化代码"
fi

echo ""
echo "📊 5. 依赖注入配置检查:"
echo "----------------------------------------"

# 检查DI配置
if grep -r "AddDbContext\|AddScoped.*DbContext" src/ --include="*.cs" > /dev/null; then
    echo "  ✅ 找到DbContext依赖注入配置"
else
    echo "  ⚠️  未找到DbContext依赖注入配置"
fi

if grep -r "AddScoped.*Repository" src/ --include="*.cs" > /dev/null; then
    echo "  ✅ 找到Repository依赖注入配置"
else
    echo "  ⚠️  未找到Repository依赖注入配置"
fi

echo ""
echo "📊 6. 配置文件检查:"
echo "----------------------------------------"

# 检查appsettings.json
if [ -f "config/appsettings.json" ]; then
    echo "  ✅ 找到配置文件: config/appsettings.json"
    
    # 检查数据库配置
    if grep -q "ConnectionString" "config/appsettings.json"; then
        echo "  ✅ 配置文件中包含数据库连接字符串"
        db_config=$(grep -A2 -B2 "ConnectionString" "config/appsettings.json")
        echo "  配置内容:"
        echo "$db_config" | sed 's/^/    /'
    else
        echo "  ⚠️  配置文件中未找到数据库连接字符串"
    fi
else
    echo "  ❌ 未找到配置文件"
fi

echo ""
echo "📊 7. 数据库使用情况检查:"
echo "----------------------------------------"

# 检查哪些地方使用了数据库
echo "  数据库使用位置:"
db_usage_count=$(grep -r "DbContext\|_dbContext\|SaveChanges\|ToListAsync\|FindAsync" src/ --include="*.cs" | wc -l)
echo "  共找到 $db_usage_count 处数据库操作"

# 检查关键功能是否使用数据库
key_features=("报警管理" "设置管理" "协议配置" "历史数据")
for feature in "${key_features[@]}"; do
    if grep -r -i "$feature" src/ --include="*.cs" | grep -q "DbContext\|Repository"; then
        echo "  ✅ $feature 使用数据库"
    else
        echo "  ❌ $feature 未使用数据库"
    fi
done

echo ""
echo "📊 8. 协议相关数据存储检查:"
echo "----------------------------------------"

# 检查协议配置是否保存到数据库
protocol_usage_patterns=(
    "ProtocolConfiguration.*Save"
    "Device.*Add"
    "PointMapping.*Insert"
    "CommunicationLog.*Log"
)

for pattern in "${protocol_usage_patterns[@]}"; do
    if grep -r -i "$pattern" src/ --include="*.cs" > /dev/null; then
        echo "  ✅ 找到协议数据存储: $pattern"
    else
        echo "  ❌ 未找到协议数据存储: $pattern"
    fi
done

echo ""
echo "========================================"
echo "🔍 数据库状态总结"
echo "----------------------------------------"

echo "✅ 已实现的功能:"
echo "  1. 基础数据库架构（SQLite + EF Core）"
echo "  2. 报警记录存储（AlarmRecord + AlarmRepository）"
echo "  3. 系统设置存储（Setting + SettingRepository）"
echo "  4. 数据库上下文配置（AppDbContext）"

echo ""
echo "❌ 缺失的功能:"
echo "  1. 协议配置存储（ProtocolConfiguration 实体）"
echo "  2. 设备信息存储（Device 实体）"
echo "  3. 点位映射存储（PointMapping 实体）"
echo "  4. 通讯日志存储（CommunicationLog 实体）"
echo "  5. 历史数据存储（高频采集数据）"
echo "  6. 数据库迁移配置"
echo "  7. 协议相关仓储实现"
echo "  8. 数据库备份恢复功能"

echo ""
echo "⚠️  需要修复的问题:"
echo "  1. 当前使用内存数据库，数据不持久化"
echo "  2. 缺少协议相关数据模型"
echo "  3. 缺少数据访问层（仓储模式不完整）"
echo "  4. 缺少数据库初始化代码"

echo ""
echo "🚀 建议的改进:"
echo "  1. 修复数据库连接字符串，使用文件数据库"
echo "  2. 创建协议相关数据模型（4个核心实体）"
echo "  3. 实现完整的数据访问层"
echo "  4. 添加数据库迁移支持"
echo "  5. 集成到现有MVVM架构"

echo ""
echo "📅 实施优先级:"
echo "  高优先级:"
echo "    1. 修复数据库持久化问题（内存→文件）"
echo "    2. 创建协议配置存储（ProtocolConfiguration）"
echo "    3. 集成到通信设置界面"
echo ""
echo "  中优先级:"
echo "    4. 创建设备和点位映射存储"
echo "    5. 实现通讯日志记录"
echo "    6. 添加数据库迁移"
echo ""
echo "  低优先级:"
echo "    7. 实现历史数据存储"
echo "    8. 添加备份恢复功能"
echo "    9. 性能优化和监控"

echo ""
echo "✅ 检查完成！"