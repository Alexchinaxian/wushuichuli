# IndustrialControlHMI - Git版本控制指南

## Git仓库状态
- **仓库位置**: `D:\上位机开发`
- **当前分支**: master
- **初始提交**: 已完成
- **文件数量**: 76个文件已提交

## 基本Git命令

### 查看状态
```bash
git status
```

### 查看提交历史
```bash
git log --oneline
```

### 添加文件到暂存区
```bash
# 添加单个文件
git add 文件名

# 添加所有修改的文件
git add .

# 添加特定类型的文件
git add *.cs
```

### 提交更改
```bash
# 提交并添加描述
git commit -m "描述修改内容"

# 提交并打开编辑器输入详细描述
git commit
```

### 查看差异
```bash
# 查看工作区与暂存区的差异
git diff

# 查看暂存区与最新提交的差异
git diff --staged

# 查看特定文件的差异
git diff 文件名
```

## 分支管理

### 创建新分支
```bash
# 创建并切换到新分支
git checkout -b 新分支名

# 从特定提交创建分支
git checkout -b 新分支名 提交哈希
```

### 切换分支
```bash
git checkout 分支名
```

### 合并分支
```bash
# 切换到目标分支
git checkout master

# 合并分支
git merge 新分支名
```

### 删除分支
```bash
# 删除已合并的分支
git branch -d 分支名

# 强制删除分支
git branch -D 分支名
```

## 远程仓库（如需配置）

### 添加远程仓库
```bash
git remote add origin 远程仓库URL
```

### 推送到远程仓库
```bash
git push -u origin master
```

### 从远程拉取更新
```bash
git pull origin master
```

## 开发工作流程

### 日常开发流程
1. **开始工作前**：`git status` 查看当前状态
2. **创建功能分支**：`git checkout -b feature/功能描述`
3. **开发功能**：编写代码，定期提交
4. **提交更改**：`git add .` 然后 `git commit -m "描述"`
5. **合并到主分支**：
   ```bash
   git checkout master
   git pull origin master  # 如果有远程仓库
   git merge feature/功能描述
   git branch -d feature/功能描述
   ```

### 提交信息规范
使用有意义的提交信息：
```
类型: 简要描述

详细描述：
- 修改了哪些内容
- 为什么修改
- 可能的影响

类型包括：
- feat: 新功能
- fix: 修复bug
- docs: 文档更新
- style: 代码格式调整
- refactor: 代码重构
- test: 测试相关
- chore: 构建过程或辅助工具变动
```

示例：
```
feat: 添加Modbus通信模块

- 实现Modbus TCP通信功能
- 添加数据采集和解析逻辑
- 支持连接状态监控

相关文件：
- Services/ModbusService.cs
- Models/ModbusData.cs
```

## 忽略文件配置
已配置`.gitignore`文件，忽略以下内容：
- 构建输出文件（bin/, obj/）
- 用户特定文件（*.user）
- 临时文件
- IDE配置文件
- NuGet包目录

## 故障排除

### 撤销更改
```bash
# 撤销工作区的修改
git checkout -- 文件名

# 撤销暂存区的文件
git reset HEAD 文件名

# 撤销最近一次提交（创建新提交）
git revert HEAD

# 撤销最近一次提交（删除提交）
git reset --hard HEAD^
```

### 查看文件历史
```bash
# 查看文件的修改历史
git log --oneline 文件名

# 查看文件的详细修改
git blame 文件名
```

## 高级功能

### 储藏更改
```bash
# 储藏当前修改
git stash

# 查看储藏列表
git stash list

# 恢复储藏
git stash pop

# 应用储藏但不删除
git stash apply
```

### 标签管理
```bash
# 创建标签
git tag v1.0.0

# 查看标签
git tag

# 推送标签到远程
git push origin --tags
```

## 注意事项
1. **定期提交**：不要积累大量修改一次性提交
2. **有意义的提交信息**：方便后续查阅历史
3. **使用分支**：每个功能或修复使用独立分支
4. **保持主分支稳定**：主分支应始终处于可编译状态
5. **忽略生成文件**：不要提交构建输出和临时文件

## 下一步
1. 配置远程仓库（GitHub、GitLab等）
2. 设置CI/CD流水线
3. 配置代码审查流程
4. 添加自动化测试