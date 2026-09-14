CREATE TABLE IF NOT EXISTS "AppAssetAuditSessions" (
    "Id" uuid NOT NULL PRIMARY KEY,
    "Title" character varying(256) NOT NULL,
    "AuditCode" character varying(64) NOT NULL,
    "ScopeDepartment" character varying(128),
    "ScopeLocation" character varying(128),
    "Status" integer NOT NULL DEFAULT 1,
    "StartDate" timestamp without time zone NOT NULL,
    "CompletedDate" timestamp without time zone,
    "TotalExpectedCount" integer NOT NULL DEFAULT 0,
    "ScannedCount" integer NOT NULL DEFAULT 0,
    "MatchedCount" integer NOT NULL DEFAULT 0,
    "DisplacedCount" integer NOT NULL DEFAULT 0,
    "MissingCount" integer NOT NULL DEFAULT 0,
    "Notes" text,
    "TenantId" uuid,
    "ExtraProperties" text,
    "ConcurrencyStamp" character varying(40),
    "CreationTime" timestamp without time zone NOT NULL,
    "CreatorId" uuid,
    "LastModificationTime" timestamp without time zone,
    "LastModifierId" uuid,
    "IsDeleted" boolean NOT NULL DEFAULT false,
    "DeleterId" uuid,
    "DeletionTime" timestamp without time zone
);

CREATE TABLE IF NOT EXISTS "AppAssetAuditItems" (
    "Id" uuid NOT NULL PRIMARY KEY,
    "AuditSessionId" uuid NOT NULL,
    "AssetId" uuid NOT NULL,
    "ExpectedLocation" character varying(128),
    "ExpectedAssignedToUserId" uuid,
    "ExpectedAssignedToUserName" character varying(128),
    "ScannedLocation" character varying(128),
    "ScannedAssignedToUserId" uuid,
    "ScannedAssignedToUserName" character varying(128),
    "ResultStatus" integer NOT NULL DEFAULT 1,
    "ScannedTime" timestamp without time zone,
    "ScannedByUserId" uuid,
    "ScannedByUserName" character varying(128),
    "Notes" text,
    "IsReconciled" boolean NOT NULL DEFAULT false,
    "ReconciledTime" timestamp without time zone,
    "TenantId" uuid,
    "CreationTime" timestamp without time zone NOT NULL,
    "CreatorId" uuid,
    CONSTRAINT "FK_AppAssetAuditItems_AppAssetAuditSessions" FOREIGN KEY ("AuditSessionId") REFERENCES "AppAssetAuditSessions" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_AppAssetAuditItems_AppAssets" FOREIGN KEY ("AssetId") REFERENCES "AppAssets" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_AppAssetAuditSessions_Status" ON "AppAssetAuditSessions" ("Status");
CREATE INDEX IF NOT EXISTS "IX_AppAssetAuditSessions_AuditCode" ON "AppAssetAuditSessions" ("AuditCode");
CREATE INDEX IF NOT EXISTS "IX_AppAssetAuditItems_AuditSessionId" ON "AppAssetAuditItems" ("AuditSessionId");
CREATE INDEX IF NOT EXISTS "IX_AppAssetAuditItems_AssetId" ON "AppAssetAuditItems" ("AssetId");
CREATE INDEX IF NOT EXISTS "IX_AppAssetAuditItems_ResultStatus" ON "AppAssetAuditItems" ("ResultStatus");
