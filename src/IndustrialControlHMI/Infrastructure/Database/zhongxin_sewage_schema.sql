-- 中信国际污水项目 — SQLite 参考 DDL（与 EF Core AppDbContext 模型一致，便于迁移至 SQL Server 等）
-- 应用运行时由 EnsureCreated 自动建表；本文件供评审、运维与异构库迁移。

PRAGMA foreign_keys = ON;

-- 设备主数据
CREATE TABLE IF NOT EXISTS "Equipments" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Equipments" PRIMARY KEY AUTOINCREMENT,
    "Code" TEXT NOT NULL,
    "DisplayName" TEXT NOT NULL,
    "UnitId" TEXT NOT NULL,
    "UnitTitle" TEXT NULL,
    "Category" TEXT NOT NULL,
    "SortOrder" INTEGER NOT NULL,
    "Remark" TEXT NULL,
    "CreatedUtc" TEXT NOT NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Equipments_Code" ON "Equipments" ("Code");
CREATE INDEX IF NOT EXISTS "IX_Equipments_UnitId" ON "Equipments" ("UnitId");

-- PLC 点位映射（由点位表解析或内置默认同步）
CREATE TABLE IF NOT EXISTS "PointMappings" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_PointMappings" PRIMARY KEY AUTOINCREMENT,
    "ExternalId" TEXT NULL,
    "RegisterAddress" TEXT NOT NULL,
    "VariableName" TEXT NOT NULL,
    "DataType" TEXT NOT NULL,
    "Purpose" TEXT NOT NULL,
    "UnitId" TEXT NOT NULL,
    "EquipmentName" TEXT NOT NULL,
    "EquipmentId" INTEGER NULL,
    "AlarmHigh" REAL NULL,
    "AlarmLow" REAL NULL,
    "HistoryEnabled" INTEGER NOT NULL,
    "SuggestedIntervalMs" INTEGER NOT NULL,
    "Source" TEXT NOT NULL,
    "CreatedUtc" TEXT NOT NULL,
    "UpdatedUtc" TEXT NOT NULL,
    CONSTRAINT "FK_PointMappings_Equipments_EquipmentId" FOREIGN KEY ("EquipmentId") REFERENCES "Equipments" ("Id") ON DELETE SET NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_PointMappings_RegisterAddress" ON "PointMappings" ("RegisterAddress");
CREATE INDEX IF NOT EXISTS "IX_PointMappings_Purpose" ON "PointMappings" ("Purpose");
CREATE INDEX IF NOT EXISTS "IX_PointMappings_UnitId" ON "PointMappings" ("UnitId");

-- 高频历史采样（批量插入 + 按时间与点位查询）
CREATE TABLE IF NOT EXISTS "PointHistorySamples" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_PointHistorySamples" PRIMARY KEY AUTOINCREMENT,
    "PointMappingId" INTEGER NOT NULL,
    "TimestampUtc" TEXT NOT NULL,
    "ValueReal" REAL NOT NULL,
    "Quality" INTEGER NOT NULL,
    CONSTRAINT "FK_PointHistorySamples_PointMappings_PointMappingId" FOREIGN KEY ("PointMappingId") REFERENCES "PointMappings" ("Id") ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS "IX_PointHistorySamples_PointMappingId_TimestampUtc" ON "PointHistorySamples" ("PointMappingId", "TimestampUtc");

-- 报警阈值规则（与点位一对一）
CREATE TABLE IF NOT EXISTS "AlarmRules" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_AlarmRules" PRIMARY KEY AUTOINCREMENT,
    "PointMappingId" INTEGER NOT NULL,
    "RuleName" TEXT NOT NULL,
    "Enabled" INTEGER NOT NULL,
    "HighThreshold" REAL NULL,
    "LowThreshold" REAL NULL,
    "Hysteresis" REAL NULL,
    "Severity" TEXT NOT NULL,
    "MessageTemplate" TEXT NULL,
    "CreatedUtc" TEXT NOT NULL,
    "ModifiedUtc" TEXT NULL,
    CONSTRAINT "FK_AlarmRules_PointMappings_PointMappingId" FOREIGN KEY ("PointMappingId") REFERENCES "PointMappings" ("Id") ON DELETE CASCADE
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_AlarmRules_PointMappingId" ON "AlarmRules" ("PointMappingId");

-- 运行报表模板（JSON 定义）
CREATE TABLE IF NOT EXISTS "ReportTemplates" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_ReportTemplates" PRIMARY KEY AUTOINCREMENT,
    "Name" TEXT NOT NULL,
    "Slug" TEXT NOT NULL,
    "Category" TEXT NOT NULL,
    "Description" TEXT NULL,
    "DefinitionJson" TEXT NOT NULL,
    "IsActive" INTEGER NOT NULL,
    "CreatedUtc" TEXT NOT NULL,
    "ModifiedUtc" TEXT NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_ReportTemplates_Slug" ON "ReportTemplates" ("Slug");
CREATE INDEX IF NOT EXISTS "IX_ReportTemplates_Category" ON "ReportTemplates" ("Category");

-- 报警记录（与现有 AlarmRecord 实体一致）
CREATE TABLE IF NOT EXISTS "AlarmRecords" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_AlarmRecords" PRIMARY KEY AUTOINCREMENT,
    "PointMappingId" INTEGER NULL,
    "ParameterName" TEXT NOT NULL,
    "AlarmType" TEXT NOT NULL,
    "Threshold" REAL NOT NULL,
    "ActualValue" REAL NOT NULL,
    "Message" TEXT NULL,
    "Status" TEXT NOT NULL,
    "OccurrenceTime" TEXT NOT NULL,
    "AcknowledgedTime" TEXT NULL,
    "ClearedTime" TEXT NULL
);
CREATE INDEX IF NOT EXISTS "IX_AlarmRecords_Status" ON "AlarmRecords" ("Status");
CREATE INDEX IF NOT EXISTS "IX_AlarmRecords_OccurrenceTime" ON "AlarmRecords" ("OccurrenceTime");
CREATE INDEX IF NOT EXISTS "IX_AlarmRecords_ParameterName_AlarmType" ON "AlarmRecords" ("ParameterName", "AlarmType");
CREATE INDEX IF NOT EXISTS "IX_AlarmRecords_PointMappingId" ON "AlarmRecords" ("PointMappingId");

CREATE TABLE IF NOT EXISTS "Settings" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Settings" PRIMARY KEY AUTOINCREMENT,
    "Category" TEXT NOT NULL,
    "Key" TEXT NOT NULL,
    "Value" TEXT NOT NULL,
    "DataType" TEXT NOT NULL,
    "Description" TEXT NULL,
    "LastModified" TEXT NOT NULL,
    "ModifiedBy" TEXT NOT NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Settings_Category_Key" ON "Settings" ("Category", "Key");
