// Mirrors FallenFaction.Server/DTOs/AI/TranslationPipelineDtos.cs (camelCase JSON).

export interface GlossaryTerm {
  sourceTerm: string;
  translation: string;
  category: string;
  notes?: string | null;
  isLocked?: boolean;
}

export interface PreviousChapterSummary {
  volumeNumber: number;
  chapterNumber: number;
  title?: string | null;
  summary: string;
}

export interface ClaimedJob {
  jobId: number;
  titleId: number;
  titleName: string;
  originalTitle?: string | null;
  volumeNumber: number;
  chapterNumber: number;
  sourceChapterTitle?: string | null;
  sourceText: string;
  sourceLanguage: string;
  /** Absent on servers that predate per-title target languages; treat as "en". */
  targetLanguage?: string;
  styleGuide?: string | null;
  synopsis?: string | null;
  leaseExpiresAt: string;
  attempt: number;
  glossary: GlossaryTerm[];
  previousChapters: PreviousChapterSummary[];
}

export interface CompleteJobPayload {
  translatedTitle: string;
  content: string;
  summary?: string;
  newGlossaryTerms: GlossaryTerm[];
  qaScore?: number;
  qaFlagged: boolean;
  qaReport?: string;
  modelInfo?: string;
}

export type IssueType =
  | "omission" | "mistranslation" | "glossary" | "untranslated" | "addition" | "fluency" | "formatting";
export type IssueSeverity = "critical" | "major" | "minor";

export interface ReviewIssue {
  type: IssueType;
  severity: IssueSeverity;
  sourceExcerpt: string;
  translationExcerpt: string;
  suggestion: string;
}

export interface ReviewResult {
  score: number;
  issues: ReviewIssue[];
  summary: string;
}
