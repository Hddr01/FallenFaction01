-- ═══════════════════════════════════════════════════════════════════════════
-- FallenFaction — Performance Indexes
-- Run once against FallenFactionDB (safe to re-run: all use IF NOT EXISTS)
--
-- These indexes cover the queries that showed up slow in Sentry traces.
-- ═══════════════════════════════════════════════════════════════════════════

-- 1. GetChaptersForTitle  (GET /api/Titles/{titleId}/chapters)
--    Query: WHERE TitleId = X ORDER BY VolumeNumber DESC, ChapterNumber DESC
--    Trace showed 1.2–1.5 s → table scan on large chapter sets
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Chapters_TitleId_Volume_Chapter'
      AND object_id = OBJECT_ID('Chapters')
)
CREATE INDEX IX_Chapters_TitleId_Volume_Chapter
    ON Chapters (TitleId, VolumeNumber DESC, ChapterNumber DESC)
    INCLUDE (Name, ReleaseDate, TeamId, IsAILocked, CharacterCount, CreatedDate);
GO

-- 2. GetRecentUpdates + GetPopularTitles  (homepage queries)
--    Both filter WHERE IsAvailable = 1 and sort by chapter ReleaseDate
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Titles_IsAvailable_TitleCategory'
      AND object_id = OBJECT_ID('Titles')
)
CREATE INDEX IX_Titles_IsAvailable_TitleCategory
    ON Titles (IsAvailable, TitleCategory)
    INCLUDE (OriginalTitle, EnglishTitle, CoverImagePath, Type, ReleaseDate, Description);
GO

-- 3. Chapters.ReleaseDate — used by RecentUpdates ORDER BY
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Chapters_TitleId_ReleaseDate'
      AND object_id = OBJECT_ID('Chapters')
)
CREATE INDEX IX_Chapters_TitleId_ReleaseDate
    ON Chapters (TitleId, ReleaseDate DESC)
    INCLUDE (ChapterNumber, VolumeNumber, Name, TeamId);
GO

-- 4. Comments — used by GetCommentStats
--    WHERE TitleId = X AND IsDeleted = 0
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Comments_TitleId_IsDeleted'
      AND object_id = OBJECT_ID('Comments')
)
CREATE INDEX IX_Comments_TitleId_IsDeleted
    ON Comments (TitleId, IsDeleted)
    INCLUDE (ParentCommentId, PostedDate);
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Comments_ChapterId_IsDeleted'
      AND object_id = OBJECT_ID('Comments')
)
CREATE INDEX IX_Comments_ChapterId_IsDeleted
    ON Comments (ChapterId, IsDeleted)
    INCLUDE (ParentCommentId, PostedDate);
GO

-- 5. PendingChapters — admin list, ordered by CreatedDate
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_PendingChapters_CreatedDate'
      AND object_id = OBJECT_ID('PendingChapters')
)
CREATE INDEX IX_PendingChapters_CreatedDate
    ON PendingChapters (CreatedDate DESC)
    INCLUDE (Name, VolumeNumber, ChapterNumber, TitleId, PendingTitleId, TeamId, UpdatedByUserId, CharacterCount, OriginalChapterId);
GO

-- 6. AspNetUsers online status — used by visibility polling
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_AspNetUsers_IsOnline_LastActive'
      AND object_id = OBJECT_ID('AspNetUsers')
)
CREATE INDEX IX_AspNetUsers_IsOnline_LastActive
    ON AspNetUsers (IsOnline, LastActive);
GO

PRINT 'All performance indexes applied successfully.';
