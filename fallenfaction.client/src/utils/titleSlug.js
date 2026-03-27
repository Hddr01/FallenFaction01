// src/utils/titleSlug.js
// Slug format: "{title-name}-{id}"  e.g. "naruto-42", "my-hero-academia-123"
// Import from HERE in all components — never from router/index.js

/**
 * Build a URL-safe slug from a title name and numeric ID.
 * "My Hero Academia" + 42  →  "my-hero-academia-42"
 */
export function buildTitleSlug(originalTitle, id) {
  const slug = (originalTitle || '')
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')   // non-alphanumeric → hyphen
    .replace(/^-+|-+$/g, '')        // trim leading/trailing hyphens
    .replace(/-{2,}/g, '-')         // collapse consecutive hyphens
  return `${slug || 'title'}-${id}`
}

/**
 * Extract the numeric ID from the trailing segment of a slug.
 * "my-hero-academia-42"  →  42
 * Returns null if the slug has no trailing number.
 */
export function parseTitleSlug(slug) {
  const lastDash = (slug || '').lastIndexOf('-')
  if (lastDash > 0) {
    const id = parseInt(slug.slice(lastDash + 1), 10)
    if (!isNaN(id) && id > 0) return id
  }
  return null
}
