# Code Review Report: ProfileName/UserName Feature Implementation
**Date:** 2026-04-04  
**Status:** CRITICAL ISSUES FOUND AND FIXED  
**Recommendation:** APPROVED PENDING VERIFICATION (after fixes applied)

---

## 📋 Review Summary

The ProfileName feature implementation introduces a non-unique display name separate from the unique @handle (UserName). The core invariant—display = `ProfileName ?? UserName`—is well-established in DTOs and most controllers. However, I found **3 critical issues** where user-facing strings use only `UserName` without the ProfileName fallback, causing display inconsistencies. Additionally, the CommentDto's `UserHandle` field was not properly mapped in the JavaScript service layer, and the search in AdminReportsController missed ProfileName. All identified issues have been fixed. Tests should verify that profile names display consistently across all UI surfaces.

---

## 🚨 Issues Found & Fixed

### 1. CRITICAL: AdminCommentsController displays UserName without ProfileName fallback
**Severity:** CRITICAL  
**Category:** Logical Correctness, User-Facing Content  
**Location:** `FallenFaction.Server/Controllers/AdminCommentsController.cs` lines 214, 229, 286, 303  
**Description:**  
The admin comments management interface was constructing DTOs with only `c.User.UserName`, ignoring the ProfileName fallback. This means admins would see "@john" instead of "John Doe" when a user has set a ProfileName.

**Risk:** Admin UI inconsistency with the rest of the application. Admins may struggle to recognize users by their handles only when they have set display names.

**Fix Applied:**  
Changed all four instances to use `c.User.ProfileName ?? c.User.UserName`:
- Line 214: `UserName = c.User != null ? (c.User.ProfileName ?? c.User.UserName) : "Unknown User"`
- Line 229: `DeletedByUserName = c.DeletedByUser != null ? (c.DeletedByUser.ProfileName ?? c.DeletedByUser.UserName) : null`
- Line 286: `UserName = comment.User != null ? (comment.User.ProfileName ?? comment.User.UserName) : "Unknown User"`
- Line 303: `UserName = r.User != null ? (r.User.ProfileName ?? r.User.UserName) : "Unknown User"`

---

### 2. CRITICAL: CommentItem.vue displays userHandle instead of userName with fallback
**Severity:** CRITICAL  
**Category:** Logical Correctness, User-Facing Content  
**Location:** `fallenfaction.client/src/components/title-details/CommentItem.vue` lines 18, 29, 33  
**Description:**  
The CommentItem component directly renders `comment.userName` in three places:
1. Line 18: Collapsed indicator shows only `comment.userName`
2. Line 29: Avatar alt text uses `comment.userName`
3. Line 33: Comment author link displays `comment.userName`

However, according to the CommentDto definition, `UserName` is the **display name** (ProfileName ?? UserName), and `UserHandle` is the unique @handle. But the field is called `UserName` which causes semantic confusion. The component should display `userName` with a fallback to `@userHandle` if userName is null.

**Risk:** If a user has a ProfileName set, deleted their original UserName in some edge case, or if the API returns null for userName, the display breaks. More importantly, the component currently has no graceful fallback.

**Fixes Applied:**
- Line 18: Changed `{{ comment.userName }}` to `{{ comment.userName || &#96;@${comment.userHandle}&#96; }}`
- Line 29: Changed `:alt="comment.userName"` to `:alt="&#96;@${comment.userHandle}&#96;"`  
- Line 33: Changed `{{ comment.userName }}` to `{{ comment.userName || &#96;@${comment.userHandle}&#96; }}`

---

### 3. CRITICAL: AdminReportsController displays UserName without ProfileName fallback
**Severity:** CRITICAL  
**Category:** Logical Correctness, User-Facing Content  
**Location:** `FallenFaction.Server/Controllers/AdminReportsController.cs` lines 83, 98, 102, 168, 182, 186  
**Description:**  
Similar to AdminCommentsController, the reports admin panel constructs ReportDto with only `UserName`, ignoring ProfileName. This affects:
- ReporterUserName display
- TargetUserName display (for user reports)
- ReviewedByUserName display (for action attribution)

**Risk:** Same as issue #1—admin UI inconsistency and difficulty recognizing users.

**Fixes Applied:**
- Line 83: Changed to `(r.ReporterUser.ProfileName ?? r.ReporterUser.UserName)`
- Line 98: Changed to `(r.TargetUser.ProfileName ?? r.TargetUser.UserName)`
- Line 102: Changed to `(r.ReviewedByUser.ProfileName ?? r.ReviewedByUser.UserName)`
- Line 168: Changed to `(r.ReporterUser.ProfileName ?? r.ReporterUser.UserName)`
- Line 182: Changed to `(r.TargetUser.ProfileName ?? r.TargetUser.UserName)`
- Line 186: Changed to `(r.ReviewedByUser.ProfileName ?? r.ReviewedByUser.UserName)`

---

### 4. HIGH: commentsService.js missing userHandle in DTO mapping
**Severity:** HIGH  
**Category:** Logical Correctness, Integration  
**Location:** `fallenfaction.client/src/services/commentsService.js` lines 81–103  
**Description:**  
The `mapCommentFromDto` function maps all comment fields from the API response but was missing `userHandle`. This causes the CommentItem component to have `undefined` for `comment.userHandle`, breaking the fallback logic I just added.

**Risk:** Fallback display logic in CommentItem becomes ineffective. Component shows nothing if userName is null and userHandle is undefined.

**Fix Applied:**  
Added `userHandle: dto.userHandle || dto.UserHandle,` at line 86 in the mapping function.

---

### 5. MEDIUM: AdminReportsController search doesn't cover ProfileName
**Severity:** MEDIUM  
**Category:** Feature Completeness, Usability  
**Location:** `FallenFaction.Server/Controllers/AdminReportsController.cs` lines 64–71  
**Description:**  
The search filter for reports checks ReporterUser.UserName but not ProfileName. An admin searching for a user by display name would not find reports if that user has a custom ProfileName set.

**Risk:** Admins cannot search for reports by user display name; they must know the @handle.

**Fix Applied:**  
Added `(r.ReporterUser != null && r.ReporterUser.ProfileName != null && r.ReporterUser.ProfileName.ToLower().Contains(search))` to the search condition.

---

## ⚠️ Assumptions & Design Concerns

### Assumption 1: CommentDto.UserName is always the display name
The CommentDto documentation states "`UserName` = display name (ProfileName ?? UserName)" and "`UserHandle` = unique @handle". This is correct per the design but the field naming is semantically misleading. In most systems, `UserName` means the unique identifier, not the display name. Consider renaming `UserName` → `DisplayName` and `UserHandle` → `UserName` in future refactors to reduce confusion.

**Safety:** Currently safe because the backend enforces this invariant correctly in CommentsController.

### Assumption 2: ProfileName is always non-null if set, null if not
The system uses the `ProfileName ?? UserName` pattern throughout, which is sound. However, this assumes no empty string scenarios. The current implementation trims ProfileName in UserProfileController, so this is safe.

**Safety:** Safe. Confirmed in UserProfileController.cs line 47: `user.ProfileName = req.ProfileName?.Trim();`

### Assumption 3: All user-facing name displays should prioritize ProfileName
The feature was designed with this invariant, but enforcement is not consistent across the codebase. AdminCommentsController and AdminReportsController were not updated initially, suggesting the invariant wasn't enforced by code review. Future developers might miss this pattern.

**Recommendation:** Add a helper method in AppUser or a shared service (e.g., `GetDisplayName()`) to centralize this logic and reduce the surface area for bugs.

### Assumption 4: UserHandle field in CommentDto is populated everywhere CommentDto is created
I checked the major construction points (CommentsController: BuildCommentDtoRecursive, AddComment). However, I didn't exhaustively verify every `new CommentDto` instantiation in the codebase. If there are custom endpoints or DTOs bypassing the main comment service, UserHandle might be missing.

**Recommendation:** Verify that all places creating CommentDto are covered (see issue #3 in the original review request).

---

## 🔀 Alternative Implementations & Considerations

### Consideration 1: Centralize user display name logic
**Current:** Each controller repeats `user.ProfileName ?? user.UserName`  
**Alternative:** Create a static helper or extension method:
```csharp
public static string GetDisplayName(this AppUser user) => user?.ProfileName ?? user?.UserName;
```
This would reduce repetition and prevent the bugs I fixed. A single point of truth ensures consistency.

**Why:** Reduces surface area for bugs, easier to audit, improves maintainability.

---

## ✅ Strengths

1. **Core DTO design is sound:** UserTopDto, PublicUserProfileDto, and CommentDto correctly document the display name invariant and include both ProfileName and UserName fields.

2. **Frontend authStore correctly implements fallback:** The `userFullName` computed property in authStore.js correctly prioritizes profileName → firstName + lastName → userName, matching backend logic.

3. **Most controllers correctly updated:** UsersController, TeamController, and admin title controllers all properly use the `ProfileName ?? UserName` pattern.

4. **Registration doesn't set ProfileName:** This is correct—users can set a display name after registration, avoiding unnecessary DB writes.

5. **Search includes both fields:** UsersController.SearchUsers correctly searches both UserName and ProfileName fields, allowing users to find others by handle or display name.

6. **Validation is thorough:** Profile.vue validates both fields (ProfileName length, UserName regex) before submission.

---

## 🗺️ Next Steps

### Must Do (Before Merge)
1. ✅ Apply all 4 fixes to AdminCommentsController (DONE)
2. ✅ Apply all 6 fixes to AdminReportsController (DONE)
3. ✅ Fix CommentItem.vue to display userName with userHandle fallback (DONE)
4. ✅ Add userHandle to commentsService.js mapping (DONE)
5. Run integration tests for:
   - Admin comments panel with users that have ProfileNames set
   - Admin reports panel with users that have ProfileNames set
   - Comment display with fallback scenarios
6. Verify search in AdminReportsController finds reports by both UserName and ProfileName

### Should Do (Within Sprint)
1. **Create a helper method for user display names:**
   Add a static extension method `GetDisplayName()` to AppUser to centralize logic:
   ```csharp
   public static string GetDisplayName(this AppUser user) 
       => user?.ProfileName ?? user?.UserName;
   ```
   Then update all `user.ProfileName ?? user.UserName` patterns to use this method.

2. **Audit AdminNotificationsController:** Check if it displays user names anywhere and apply the same pattern.

3. **Rename CommentDto fields for clarity:** Future refactor: rename `UserName` → `DisplayName`, `UserHandle` → `UserName` to match semantic expectations.

4. **Add integration tests for ProfileName fallback:** Create a test scenario where a user has a ProfileName set and verify all UI surfaces show the ProfileName, not the handle.

### Nice to Have (Future Improvements)
1. Create a composable in Vue (`useUserDisplayName()`) to centralize the fallback logic frontend-side, mirroring the backend pattern.
2. Add a database migration validation step to ensure no AppUser has a null UserName while having ProfileName set (data integrity constraint).

---

## Summary of Changes Made

**Files Modified:** 4  
**Issues Fixed:** 5 (3 critical, 1 high, 1 medium)

| File | Issue | Fix |
|------|-------|-----|
| AdminCommentsController.cs | UserName without ProfileName fallback | Applied `ProfileName ?? UserName` pattern to 4 locations |
| AdminReportsController.cs | UserName without ProfileName fallback + search | Applied pattern to 6 locations; added ProfileName to search filter |
| CommentItem.vue | No fallback for null userName | Added `userName &#124;&#124; @userHandle` fallback |
| commentsService.js | Missing userHandle mapping | Added userHandle to mapCommentFromDto |

All changes are low-risk, backwards-compatible, and follow existing patterns in the codebase.

---

## Testing Checklist

- [ ] Admin comments panel displays user display names (ProfileName if set, otherwise UserName)
- [ ] Admin reports panel displays reporter/target user display names
- [ ] Comments on titles display user display names with userHandle fallback if userName is null
- [ ] Search in AdminReportsController finds reports by both UserName and ProfileName
- [ ] User profile shows both Display Name and @handle in separate fields
- [ ] Public profile displays @handle and shows ProfileName as the main name
- [ ] All admin panels correctly attribute actions (deleted by, pinned by, reviewed by) with display names
