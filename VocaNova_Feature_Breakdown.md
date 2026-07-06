# VocaNova — Feature Breakdown (Commit-level)

> **Purpose:** Each feature = 1 branch + 1–3 clear commits.  
> **Team:** An (DevOps) · Huy (Backend) · Nhut (Mobile) · Tan (Dashboard)

---

## Git Workflow

### Branch Naming
```
feature/{module}/{feature-name}
fix/{module}/{bug-name}
chore/{description}

Examples:
  feature/auth/jwt-token-service
  feature/quiz/sm2-algorithm
  feature/mobile-auth/login-screen
  fix/quiz/distractor-duplicate-bug
  chore/setup-cicd
```

### Commit Convention (Conventional Commits)
```
feat(module): short description
fix(module): bug description
test(module): add unit tests
refactor(module): no behavior change
chore: configuration, tooling

Examples:
  feat(auth): add JWT token generation with 15-min expiry
  feat(quiz): implement SM-2 spaced repetition algorithm
  test(auth): add unit tests for OTP rate limiting
  fix(dictionary): normalize word_key before insert
```

### Branch Strategy
```
main          ← production only, merge after demo
dev           ← integration, every PR merges here
feature/*     ← daily work
```

### PR Rule
- 1 feature = 1 PR into `dev`
- PR title = feature name (e.g. `[Auth] JWT Token Service`)
- Self-review before assigning to the leader
- Do not merge if CI fails

---

## Legend

| Symbol | Meaning |
|---|---|
| 🔴 | Must be finished before other features (blocker) |
| 🟡 | Depends on 1–2 prior features |
| 🟢 | Can be done in parallel |
| ⏱ | Estimated hours (1 person) |
| 👤 | Assignee |

---

## Global UI Convention — Popup Notifications 🔴

> **Applies to BOTH Dashboard AND Mobile.** Every **action-result notification** (success / error) must use a
> **centered popup** matching the reference screenshot: dimmed overlay + centered card + round icon (green ✓ for
> success, red ⚠ for error) + **title** ("Success" / "Failed") + a message line + an **OK** button. **Do NOT use
> SnackBar / corner toast / inline alert banner** for this kind of notification.

**General rules:**
- **Success / Error result** (after create/update/delete/restore, login/logout failure, password change, upload...) → **centered popup** (icon + title + message + OK).
- **Confirmation before a dangerous action** (delete, lock account...) → a separate **confirm modal** (Cancel + action), not a result popup.
- **Persistent states** (offline, loading, empty) → **still use inline banner/skeleton** (this is not a "result notification", keep as-is).
- Messages must go through i18n (Dashboard `@T[...]`; Mobile translation table/localization) — shown in the currently selected language.

**Implementation:**
- **Dashboard:** reuse the existing `#result-modal` in `_Layout`, driven by `TempData["UserSuccess"]` / `TempData["UserError"]`, message translated via `@T[...]`. Pages that still show inline `alert`s must be migrated to this popup for consistency.
- **Mobile:** create a shared widget `AppResultDialog` (or `showResultDialog(context, success, message)`) — a centered `AlertDialog` with a ✓/⚠ icon, title, message, and OK button. Every screen replaces `ScaffoldMessenger`/`SnackBar` result notifications with this widget.

---

## PHASE 0 — Shared Kernel

> **Must be 100% complete before any module starts.**

---

### F001 — Solution Structure & NuGet Setup
```
Branch:    feature/setup/solution-structure
Assignee:  An
Time:      2h
Depends on: —
```
**What to do:** Create the solution with 3 projects (`VocaNova.API`, `VocaNova.Dashboard`, `VocaNova.Tests`), install core NuGet packages, configure a sample `appsettings.json`.

**Done when:**
- [ ] `VocaNova.sln` has 3 projects, builds successfully
- [ ] `appsettings.json` has sections: `ConnectionStrings`, `JwtSettings`, `Redis`, `AiGrading`, `RateLimit`
- [ ] `.gitignore` correct for .NET + Flutter
- [ ] `README.md` has local setup instructions

---

### F002 — DbContext & Entity Scaffolding
```
Branch:    feature/setup/dbcontext-entities
Assignee:  An + Huy
Time:      3h
Depends on: F001
```
**What to do:** Scaffold `VocaNovaDbContext` from the existing DB (31 entities), create a dedicated `IEntityTypeConfiguration<T>` for each important entity.

**Done when:**
- [ ] `dotnet ef dbcontext scaffold` runs successfully
- [ ] All 31 entities have a file in `Infrastructure/Persistence/Configurations/`
- [ ] Important indexes: `words.word_key` (UNIQUE), `user_auth.phone` (UNIQUE), `user_auth.google_uid` (UNIQUE NULLABLE), `user_list_words(user_id, list_id, word_id)` (UNIQUE)
- [ ] `VocaNovaDbContext` injected into DI in `Program.cs`

---

### F003 — Result Pattern 🔴
```
Branch:    feature/shared/result-pattern
Assignee:  Huy
Time:      1.5h
Depends on: F001
```
**What to do:** Implement the `Result<T>` class and `PagedResult<T>`.

**Done when:**
- [ ] `Result<T>` has static factories: `Ok()`, `Fail()`, `NotFound()`, `Conflict()`, `Forbidden()`
- [ ] `Result<T>` has properties `IsSuccess`, `Value`, `Error`, `StatusCode`
- [ ] `PagedResult<T>` has `Items`, `Page`, `Limit`, `TotalItems`, `TotalPages`
- [ ] Unit test: verify the status codes of each factory

---

### F004 — API Response Formatter & Exception Middleware 🔴
```
Branch:    feature/shared/api-response-formatter
Assignee:  Huy
Time:      2h
Depends on: F003
```
**What to do:** `ApiResponseFormatter` (wrap every response into a unified shape) + `ExceptionMiddleware`.

**Done when:**
- [ ] Response shape: `{ success, data, message, errors[], pagination? }`
- [ ] `ExceptionMiddleware` catches all unhandled exceptions, logs internally, returns 500 (no stack trace exposed)
- [ ] Controller extension methods: `OkResult(data)`, `CreatedResult(data)`, `ErrorResult(result)`
- [ ] Test via Swagger: an error endpoint returns the correct format

---

### F005 — Enums & Constants
```
Branch:    feature/shared/enums-constants
Assignee:  Huy
Time:      1h
Depends on: F001
```
**What to do:** Declare all enums as `const string` (since the DB stores strings).

**Done when:**
- [ ] `QuestionType` (1, 2, 3)
- [ ] `TestMode` (standard, timed, challenge, elimination)
- [ ] `ScopeType` (all, date_range, start_date, end_date)
- [ ] `WordOrder` (newest, oldest, random)
- [ ] `AnswerMethod` (multiple_choice, exact_typing, ai_typing)
- [ ] `AddMethod` (manual, search, random_topic, random_synonym, random_antonym)
- [ ] `UserStatus` (active, locked, deleted)
- [ ] `AudioStatus` (pending, uploaded, tts_generated, missing, deleted)
- [ ] `AppSettings` static class for configurable values (MaxListsPerUser = 50, AiPassThreshold = 0.75...)

---

### F006 — Custom FluentValidation Validators 🔴
```
Branch:    feature/shared/custom-validators
Assignee:  Huy
Time:      2h
Depends on: F001
```
**What to do:** A collection of reusable validators.

**Done when:**
- [ ] `VietnamesePhoneValidator`: regex `^(0[3-9]\d{8})$`
- [ ] `StrongPasswordValidator`: ≥ 8 chars, ≥ 1 uppercase, ≥ 1 lowercase, ≥ 1 digit
- [ ] `CefrLevelValidator`: null or ∈ {A1, A2, B1, B2, C1, C2}
- [ ] `EnumStringValidator<T>`: generic, validate the string belongs to a given set
- [ ] `DateRangeValidator`: from ≤ to, range ≤ 365 days
- [ ] Register `FluentValidation` in DI (`AddFluentValidationAutoValidation`)
- [ ] Unit test: 3 test cases per validator (valid, invalid, edge)

---

### F007 — String & Queryable Extensions 🔴
```
Branch:    feature/shared/extensions
Assignee:  Huy
Time:      1.5h
Depends on: F001
```
**What to do:** Utility extensions used throughout the system.

**Done when:**
- [ ] `NormalizeWord(this string s)`: `s.Trim().ToLowerInvariant()`
- [ ] `NormalizeAnswer(this string s)`: trim + strip trailing punctuation + lowercase
- [ ] `ToPagedResultAsync<T>(this IQueryable<T>, int page, int limit)`: auto Skip/Take + count, returns `PagedResult<T>`
- [ ] `MaskPhone(this string phone)`: `09x****xx90` format
- [ ] Unit test: NormalizeWord ("  Hello  " → "hello"), ToPagedResult (page 2, limit 5 → correct offset)

---

### F008 — Global Query Filters (Soft Delete) 🔴
```
Branch:    feature/shared/global-query-filters
Assignee:  Huy
Time:      1.5h
Depends on: F002
```
**What to do:** Configure EF Core Global Query Filters for all entities that support soft delete.

**Done when:**
- [ ] Filter applied to: `UserList`, `UserListWord`, `Topic`, `Word`, `WordSense`, `WordExample`, `WordAudioAsset`
- [ ] Test: querying `UserList` by default does not show `status='deleted'`
- [ ] Test: `dbContext.UserLists.IgnoreQueryFilters()` shows deleted too
- [ ] Comment in code: the list of entities and the filter logic

---

### F009 — Audit Log Middleware 🔴
```
Branch:    feature/shared/audit-log-middleware
Assignee:  Huy
Time:      2h
Depends on: F002, F008
```
**What to do:** Middleware that automatically writes to `audit_logs` for every write request to `/api/admin/*`.

**Done when:**
- [ ] Intercept `POST/PUT/PATCH/DELETE /api/admin/*`
- [ ] Record: `user_id`, `action` (Create/Update/Delete), `entity_type`, `entity_id`, `ip_address`, `payload_before` (JSON), `payload_after` (JSON), `created_at`
- [ ] `payload_before/after` null when not applicable
- [ ] Audit log written asynchronously (does not block the response)
- [ ] Test: after calling `PUT /api/admin/words/1`, there is a record in `audit_logs`

---

### F010 — JWT Auth & Swagger Setup 🔴
```
Branch:    feature/setup/jwt-swagger-setup
Assignee:  An
Time:      2h
Depends on: F001
```
**What to do:** Configure JWT middleware + Swagger with Bearer auth.

**Done when:**
- [ ] JWT middleware validates the token, extracts claims (user_id, role)
- [ ] `[Authorize]` attribute works
- [ ] `[Authorize(Roles = "admin,super_admin")]` works
- [ ] Swagger UI has an "Authorize" button; after entering a token you can call a protected endpoint
- [ ] Role-based policies: `Admin`, `SuperAdmin`, `User`

---

## PHASE 1 — Backend: Module Auth (M1)

---

### F011 — BCrypt Password Hashing Utility
```
Branch:    feature/auth/bcrypt-helper
Assignee:  Huy
Time:      1h
Depends on: F001
```
**Done when:**
- [ ] `PasswordHelper.Hash(password)`: BCrypt cost 12
- [ ] `PasswordHelper.Verify(password, hash)`: bool
- [ ] `TokenHelper.HashSha256(rawToken)`: used for refresh token storage
- [ ] Unit test: hash + verify, wrong password returns false

---

### F012 — JWT Token Service
```
Branch:    feature/auth/jwt-token-service
Assignee:  Huy
Time:      2h
Depends on: F010, F011
```
**Done when:**
- [ ] `GenerateAccessToken(userId, role)`: JWT, expiry 15 minutes, claims: `sub`, `role`, `jti`
- [ ] `GenerateRefreshToken()`: UUID v4 raw string
- [ ] `ValidateAccessToken(token)`: returns `ClaimsPrincipal?` (null if invalid/expired)
- [ ] Refresh token stored in DB = SHA256(raw) — raw is only returned to the client
- [ ] Unit test: generate → validate → extract userId correctly

---

### F013 — Auth Repository & DTOs
```
Branch:    feature/auth/auth-dtos-repository
Assignee:  Huy
Time:      1.5h
Depends on: F002
```
**Done when:**
- [ ] DTOs: `RegisterRequest`, `LoginRequest`, `GoogleLoginRequest`, `TokenResponse`, `UserProfileDto`, `LearningProfileDto`
- [ ] Validators: `RegisterRequestValidator`, `LoginRequestValidator`, `OtpSendRequestValidator`
- [ ] `IAuthRepository` interface with methods: `FindByPhone`, `FindByGoogleUid`, `CreateUser`, `CreateRefreshToken`, `RevokeToken`

---

### F014 — Register Endpoint
```
Branch:    feature/auth/register
Assignee:  Huy
Time:      2.5h
Depends on: F006, F011, F012, F013
```
**Done when:**
- [ ] `POST /api/auth/register` works
- [ ] Validate: VN phone, strong password, display_name 2–150
- [ ] Check phone duplicate (only check users with `status != 'deleted'`)
- [ ] Create `users` + `user_auth` + `user_profiles` in one transaction
- [ ] After registration: return `201 Created` with `TokenResponse`
- [ ] Unit test: happy path, phone dup, weak password

---

### F015 — Login Endpoint
```
Branch:    feature/auth/login
Assignee:  Huy
Time:      2h
Depends on: F012, F013
```
**Done when:**
- [ ] `POST /api/auth/login` works
- [ ] BCrypt verify password
- [ ] Check `users.status`: locked → 403, deleted → 401
- [ ] Create access token + refresh token, store in `refresh_tokens`
- [ ] Return `TokenResponse`
- [ ] Unit test: wrong password, locked user, deleted user, success

---

### F016 — Google OAuth Login
```
Branch:    feature/auth/google-oauth
Assignee:  Huy
Time:      2.5h
Depends on: F012, F013
```
**Done when:**
- [ ] `POST /api/auth/google` with `{ id_token }`
- [ ] Verify the Google id_token via `Google.Apis.Auth`
- [ ] If `google_uid` already exists → normal login
- [ ] If not → create a new user (phone = null)
- [ ] If `google_email` matches another user's phone → 409 Conflict
- [ ] Unit test: new user, existing user, email conflict

---

### F017 — Refresh Token Endpoint
```
Branch:    feature/auth/refresh-token
Assignee:  Huy
Time:      1.5h
Depends on: F012, F013
```
**Done when:**
- [ ] `POST /api/auth/refresh` with `{ refresh_token }`
- [ ] SHA256-hash the input → look up in `refresh_tokens`
- [ ] Check `revoked_at` (null = still usable) + `expires_at`
- [ ] Revoke the old token, create a new one (token rotation)
- [ ] Unit test: expired token, revoked token, success

---

### F018 — Logout + Profile Endpoints
```
Branch:    feature/auth/logout-profile
Assignee:  Huy
Time:      1.5h
Depends on: F012, F013
```
**Done when:**
- [ ] `POST /api/auth/logout`: revoke the current refresh token (from body or header)
- [ ] `GET /api/auth/me`: return `UserProfileDto` (with learning profile)
- [ ] `PUT /api/auth/me/profile`: update display_name, avatar_url
- [ ] `PUT /api/auth/me/learning-profile`: update the 5 onboarding fields (validate FK)
- [ ] Redis cache `vocanova:user:{id}` TTL 5 minutes; invalidate when the profile updates

---

### F019 — OTP Service
```
Branch:    feature/auth/otp-service
Assignee:  Huy
Time:      2.5h
Depends on: F002, F009
```
**Done when:**
- [ ] `POST /api/auth/otp/send`: rate limit 1 OTP/minute/phone (check `created_at` of the most recent OTP)
- [ ] Generate a random 6-digit code, TTL 5 minutes
- [ ] `POST /api/auth/otp/verify`: increment `verify_attempt_count`, check expired, check `is_used`, check max 5 attempts
- [ ] After successful verification: `is_used = true`
- [ ] SMS: stub logs to console (real Twilio uses the `ISmsProvider` interface)
- [ ] Unit test: expired OTP, max attempts (6th attempt → reject), already used

---

### F020 — Forgot & Reset Password
```
Branch:    feature/auth/forgot-reset-password
Assignee:  Huy
Time:      2h
Depends on: F011, F019
```
**Done when:**
- [ ] `POST /api/auth/forgot-password`: send an OTP with `purpose = 'reset'`
- [ ] `POST /api/auth/reset-password`: verify OTP → hash new password → update `user_auth`
- [ ] OTP usable only once (`is_used = true` right after a successful reset)
- [ ] Unit test: reset with a correct OTP, reset with an expired OTP

---

### F021 — Auth Rate Limiting
```
Branch:    feature/auth/rate-limiting
Assignee:  An
Time:      1.5h
Depends on: F009, F019
```
**Done when:**
- [ ] `POST /api/auth/otp/send`: 1 req/minute/IP → 429
- [ ] `POST /api/auth/login`: 10 req/minute/IP → 429
- [ ] The 429 response has a `Retry-After` header
- [ ] Test: 11 consecutive logins → the 11th receives 429

---

## PHASE 1 — Backend: Module Dictionary (M2)

---

### F022 — Word Search Endpoint
```
Branch:    feature/dictionary/word-search
Assignee:  Huy
Time:      2.5h
Depends on: F007, F008
```
**Done when:**
- [ ] `GET /api/words?q=&page=&limit=&cefr=&topicId=&isPhrase=` (anonymous)
- [ ] Query uses `word_key LIKE {NormalizeWord(q)}%`
- [ ] Filter: `cefr_level`, `topic_id` (JOIN `word_topics`), `is_phrase`
- [ ] Return `PagedResult<WordSummaryDto>`: word_id, word, phonetic, cefr, primary_meaning (sense[0].vi), image_url
- [ ] Cache: `vocanova:word-search:{query}:{page}:{filters}` TTL 5 minutes
- [ ] Unit test: search "run", filter by topic, empty results

---

### F023 — Word Detail Endpoint
```
Branch:    feature/dictionary/word-detail
Assignee:  Huy
Time:      2h
Depends on: F022
```
**Done when:**
- [ ] `GET /api/words/{id}` (anonymous)
- [ ] Eager load: senses → examples, relations (with `related_word_id` nullable), audio (filter by status), derived_forms, idioms, topics
- [ ] `RelationDto.linked_word_id` = null if the word is not yet in the DB
- [ ] Audio: only return the URL if `status IN ('uploaded', 'tts_generated')`
- [ ] Cache: `vocanova:word:{id}` TTL 30 minutes
- [ ] 404 if the word is soft-deleted (handled by the Global Query Filter)

---

### F024 — Topics Endpoints
```
Branch:    feature/dictionary/topics
Assignee:  Huy
Time:      1.5h
Depends on: F008
```
**Done when:**
- [ ] `GET /api/topics`: list of active topics with `word_count`
- [ ] `GET /api/topics/{id}/words?page=&limit=`: words by topic (paginated)
- [ ] Cache: `vocanova:topics` TTL 60 minutes
- [ ] Cache: `vocanova:topic-words:{id}:{page}` TTL 10 minutes

---

### F025 — Admin Word CRUD
```
Branch:    feature/dictionary/admin-word-crud
Assignee:  Huy
Time:      3h
Depends on: F022, F009
```
**Done when:**
- [ ] `POST /api/admin/words`: create a new word, `word_key` auto = `word.NormalizeWord()`
- [ ] `PUT /api/admin/words/{id}`: update word metadata
- [ ] Validator: `CreateWordRequest` (word length, cefr valid)
- [ ] Invalidate the word cache on update
- [ ] Audit log written via middleware
- [ ] Unit test: create success, create duplicate word_key → 409

---

### F026 — Admin Soft Delete + Restore Word
```
Branch:    feature/dictionary/admin-word-softdelete
Assignee:  Huy
Time:      1.5h
Depends on: F025
```
**Done when:**
- [ ] `DELETE /api/admin/words/{id}`: SuperAdmin only, `words.status = 'deleted'`
- [ ] `PATCH /api/admin/words/{id}/restore`: SuperAdmin only, `status = 'active'`
- [ ] The delete endpoint uses `.IgnoreQueryFilters()` to find deleted records too
- [ ] Invalidate the cache after soft delete/restore

---

### F027 — Admin Sense CRUD (Cascade Soft Delete)
```
Branch:    feature/dictionary/admin-sense-crud
Assignee:  Huy
Time:      2.5h
Depends on: F025
```
**Done when:**
- [ ] `POST /api/admin/words/{id}/senses`: add a new sense
- [ ] `PUT /api/admin/words/{id}/senses/{senseId}`: update a sense
- [ ] `DELETE /api/admin/words/{id}/senses/{senseId}`: soft delete the sense (`is_deleted=1`) + cascade soft delete all `word_examples` of that sense
- [ ] `PATCH /api/admin/words/{id}/senses/{senseId}/restore`: restore the sense (does NOT auto-restore examples)
- [ ] Unit test: delete sense → examples cascade-deleted; restore sense → examples remain deleted

---

### F028 — Admin Topic CRUD (With Guard)
```
Branch:    feature/dictionary/admin-topic-crud
Assignee:  Huy
Time:      2h
Depends on: F024
```
**Done when:**
- [ ] `POST /api/admin/topics`, `PUT /api/admin/topics/{id}`
- [ ] `DELETE /api/admin/topics/{id}`: **block** if there are still `word_topics` with a word `status='active'` → 409
- [ ] `PATCH /api/admin/topics/{id}/restore`
- [ ] Unit test: delete a topic with words → 409; delete a topic without words → success

---

### F029 — Bulk CSV Import Words
```
Branch:    feature/dictionary/bulk-import-csv
Assignee:  Huy
Time:      3h
Depends on: F025, F027
```
**Done when:**
- [ ] `POST /api/admin/words/import` (multipart/form-data)
- [ ] CSV format: `word, cefr_level, phonetic_uk, phonetic_us, word_class, english_definition, vietnamese_meaning`
- [ ] Each row validated independently — an invalid row → record into errors[], **do not stop the import**
- [ ] If `word_key` already exists → add a new sense to that word (do not create a duplicate word)
- [ ] Response: `{ imported_words, imported_senses, skipped, errors: [{row, column, message}] }`
- [ ] Unit test: a 10-row file (3 invalid) → import 7, errors has exactly 3 entries

---

## PHASE 1 — Backend: Module Learning List (M3)

---

### F030 — Get & Create List
```
Branch:    feature/list/get-create-list
Assignee:  Huy
Time:      2h
Depends on: F007, F008
```
**Done when:**
- [ ] `GET /api/lists`: the current user's lists (status='active')
- [ ] `POST /api/lists`: create a list, check max 50 lists, check the name is not duplicated (case-insensitive)
- [ ] `UserListDto`: list_id, list_name, word_count, created_at
- [ ] Cache: `vocanova:user-lists:{user_id}` TTL 10 minutes
- [ ] Unit test: create OK, create when already 50 lists → 400, create with a duplicate name → 409

---

### F031 — Update & Delete List
```
Branch:    feature/list/update-delete-list
Assignee:  Huy
Time:      1.5h
Depends on: F030
```
**Done when:**
- [ ] `PUT /api/lists/{id}`: rename, check for duplicates
- [ ] `DELETE /api/lists/{id}`: soft delete the list + **cascade** soft delete all of the list's `user_list_words`
- [ ] Verify ownership: a user can only delete their own list → 403 otherwise
- [ ] `user_word_progress` is NOT affected
- [ ] Unit test: cascade soft delete, verify all 10 words are deleted after the list is deleted

---

### F032 — List Words: Get & Add Manual
```
Branch:    feature/list/words-get-add
Assignee:  Huy
Time:      2.5h
Depends on: F023, F031
```
**Done when:**
- [ ] `GET /api/lists/{id}/words?page=`: with `correct_count`, `wrong_count` from `user_list_word_stats`
- [ ] `POST /api/lists/{id}/words`: add a word (body: `word_id`, `add_method`, `note`)
- [ ] Check the word exists (404 if not)
- [ ] If the word is already active in the list → 409; if already deleted → restore (`status='active'`)
- [ ] `note` max 1000 characters
- [ ] Unit test: add dup (active), add dup (deleted → restore), add a non-existent word

---

### F033 — List Words: Add Random
```
Branch:    feature/list/words-add-random
Assignee:  Huy
Time:      2h
Depends on: F032
```
**Done when:**
- [ ] `POST /api/lists/{id}/words/random` (body: topic_id?, count, method)
- [ ] `random_topic`: randomly take `count` words by topic (excluding words already in the list)
- [ ] `random_synonym` / `random_antonym`: only take from `word_relations` with `is_quiz_eligible=true`
- [ ] `count` max 50
- [ ] If not enough words → add as many as available, no error
- [ ] Unit test: random_topic filter (exclude existing), count limit

---

### F034 — List Words: Remove & Note
```
Branch:    feature/list/words-remove-note
Assignee:  Huy
Time:      1h
Depends on: F032
```
**Done when:**
- [ ] `DELETE /api/lists/{id}/words/{wordId}`: soft delete (`status='deleted'`)
- [ ] `PATCH /api/lists/{id}/words/{wordId}/note`: update the note
- [ ] `user_word_progress` is NOT affected when a word is removed

---

## PHASE 1 — Backend: Module Quiz (M4)

---

### F035 — Quiz Word Pool Builder
```
Branch:    feature/quiz/word-pool-builder
Assignee:  Huy
Time:      3h
Depends on: F032
```
**Done when:**
- [ ] `QuizSessionBuilder.BuildPoolAsync(userId, request)`:
  - `scope_type = 'all'`: all active `user_list_words`
  - `scope_type = 'date_range'`: filter by `added_at` within the range
  - `scope_type = 'start_date'`: from that date onward
  - `scope_type = 'end_date'`: up to that date
  - Optional: filter by `topic_ids`
- [ ] Apply `word_order`: newest (sort added_at DESC), oldest (ASC), random (shuffle)
- [ ] Apply `word_limit` if present
- [ ] Pool size ≥ 4 if `multiple_choice` (needs distractors) → if not enough: `Result.Fail("Not enough words to create the quiz")`
- [ ] Unit test: scope all, scope date_range, pool < 4 with multiple choice

---

### F036 — Question Builder & Distractor Generator
```
Branch:    feature/quiz/question-builder
Assignee:  Huy
Time:      2.5h
Depends on: F035
```
**Done when:**
- [ ] `BuildQuestion(wordId, questionType)`:
  - Type 1 (WordToMeaning): `display_content = word`, `expected_answer = vietnamese_meaning`
  - Type 2 (MeaningToWord): `display_content = vietnamese_meaning`, `expected_answer = word`
  - Type 3 (Description): `display_content = english_definition`, `expected_answer = word`
- [ ] Distractor generation: 3 words in the same topic or same word_class, NOT matching the expected_answer
- [ ] `choices[]` shuffled (expected answer at a random position)
- [ ] Unit test: distractors don't match the answer, choices has exactly 4 elements

---

### F037 — Create Quiz Session Endpoint
```
Branch:    feature/quiz/create-session
Assignee:  Huy
Time:      2h
Depends on: F035, F036
```
**Done when:**
- [ ] `POST /api/quiz/sessions`:
  - Validate `CreateSessionRequest` (mode+lives, mode+time cross-field)
  - Build pool → validate
  - Save `test_sessions` + `test_session_topics`
  - Return `QuizSessionDto` + the first question (`QuestionDto`)
- [ ] Session `status = 'in_progress'`
- [ ] Unit test: timed mode missing time_limit → 400, elimination mode missing lives → 400

---

### F038 — Exact Typing & Multiple Choice Grader
```
Branch:    feature/quiz/exact-multiple-grader
Assignee:  Huy
Time:      2h
Depends on: F007, F037
```
**Done when:**
- [ ] `IAnswerGrader` interface with `GradeAsync(answer, expected, acceptedAnswers[]) → GradeResult`
- [ ] `ExactTypingGrader`: both sides use `NormalizeAnswer()` → string comparison
- [ ] `MultipleChoiceGrader`: direct comparison (no normalization)
- [ ] `accepted_answers` (JSON array): if user_answer matches any → correct
- [ ] Unit test: exact_typing case-insensitive, trailing punctuation ignored

---

### F039 — SM-2 SRS Algorithm
```
Branch:    feature/quiz/sm2-algorithm
Assignee:  Huy
Time:      2.5h
Depends on: F002
```
**Done when:**
- [ ] `SrsService.UpdateProgressAsync(userId, wordId, isCorrect)`:
  - Upsert `user_word_progress` (insert if missing, update if present)
  - Correct: compute ease_factor, new interval, increment consecutive_correct
  - Wrong: reset interval=1, consecutive_correct=0, `is_in_wrong_list=true`
  - Mastery: increase when `consecutive_correct >= 5`
- [ ] `next_review_at` updated based on the interval
- [ ] Unit test:
  - 5 consecutive correct → `mastery_level` +1
  - 1 wrong after 4 correct → `consecutive_correct = 0`
  - ease_factor never drops below 1.3

---

### F040 — Submit Answer Endpoint
```
Branch:    feature/quiz/submit-answer
Assignee:  Huy
Time:      2.5h
Depends on: F038, F039
```
**Done when:**
- [ ] `POST /api/quiz/sessions/{id}/answer` (body: word_id, user_answer)
- [ ] Route to the correct grader based on `test_sessions.answer_method`
- [ ] AI_typing → call `AiGradingService` (Module 6, stubbed first)
- [ ] After grading: upsert `test_answers`, update SM-2, update session stats
- [ ] Return `AnswerResultDto` + `next_question` (null if this is the last one)
- [ ] If the session `status != 'in_progress'` → 409

---

### F041 — Finish Session & Result
```
Branch:    feature/quiz/finish-result
Assignee:  Huy
Time:      2h
Depends on: F040
```
**Done when:**
- [ ] `POST /api/quiz/sessions/{id}/finish`: set `status='abandoned'`, compute partial stats
- [ ] `GET /api/quiz/sessions/{id}/result`: load the full session + all `test_answers`
- [ ] Compute: `accuracy`, `duration_sec` (ended_at - started_at), `max_streak`, `score`
- [ ] Session auto-completes when the last question is submitted (no need to call finish)

---

### F042 — Quiz History & Wrong Words
```
Branch:    feature/quiz/history-wrong-words
Assignee:  Huy
Time:      1.5h
Depends on: F041
```
**Done when:**
- [ ] `GET /api/quiz/history?page=`: session history (paginated, newest first)
- [ ] `GET /api/quiz/wrong-words?page=`: words with `is_in_wrong_list=true`, sort wrong_count DESC
- [ ] `DELETE /api/quiz/wrong-words/{wordId}`: set `is_in_wrong_list=false`, do NOT delete the record
- [ ] Unit test: wrong-words only shows words with the flag true

---

## PHASE 1 — Backend: Module Progress (M5)

---

### F043 — Progress Summary Endpoint
```
Branch:    feature/progress/summary
Assignee:  Huy
Time:      2.5h
Depends on: F041
```
**Done when:**
- [ ] `GET /api/progress/summary`
- [ ] Streak: count consecutive days with a session (check day gaps)
- [ ] Accuracy 7 days: correct/total from `test_answers` in the last 7 days
- [ ] Total words in progress: COUNT DISTINCT word_id in `user_word_progress`
- [ ] Cache: `vocanova:progress-summary:{user_id}` TTL 15 minutes
- [ ] Unit test: streak with a gap breaking the streak, streak when today has no session yet

---

### F044 — Progress Chart & Mastery
```
Branch:    feature/progress/chart-mastery
Assignee:  Huy
Time:      2h
Depends on: F043
```
**Done when:**
- [ ] `GET /api/progress/chart?granularity=daily|weekly|monthly`
- [ ] daily (30 days), weekly (12 weeks), monthly (6 months)
- [ ] `GET /api/progress/mastery-breakdown`: COUNT per mastery_level (0–5)
- [ ] `GET /api/progress/weakest-words?limit=20`: `is_in_wrong_list=true`, sort wrong_count DESC
- [ ] `GET /api/progress/words/{wordId}`: progress detail for one word

---

## PHASE 1 — Backend: Module AI Grading (M6)

---

### F045 — AI Grading Cache Lookup
```
Branch:    feature/ai-grading/cache-lookup
Assignee:  Huy
Time:      2h
Depends on: F007, F002
```
**Done when:**
- [ ] `IAiGradingService` interface: `GradeAsync(wordId, questionType, userAnswer, expectedAnswer) → AiGradingResult`
- [ ] `cache_key = SHA256("{wordId}:{questionType}:{NormalizeAnswer(userAnswer)}")`
- [ ] Cache hit: `expires_at > NOW()` → increment `hit_count`, return the result
- [ ] Unit test: cache hit → no API call

---

### F046 — Gemini API Integration
```
Branch:    feature/ai-grading/gemini-integration
Assignee:  Huy
Time:      2.5h
Depends on: F045
```
**Done when:**
- [ ] `IGeminiClient` interface (easy to mock in tests)
- [ ] Prompt template: return JSON `{ score: float, explanation: string, suggestion: string }`
- [ ] Parse the response, validate `score` in [0.0, 1.0]
- [ ] Cache miss: call the API → store cache TTL 7 days
- [ ] Fallback on API failure or parse error: `{ score: 0.0, explanation: "AI is unavailable" }`
- [ ] Wire into F040 (`AiTypingGrader` uses this service)
- [ ] Unit test: API failure → fallback score 0.0

---

## PHASE 1 — Backend: Module KNN (M7)

> **Two parallel KNN flows in the system:**
>
> | Flow | Goal | Feature vector | Output | Trigger |
> |---|---|---|---|---|
> | **KNN Onboarding** (F048) | New user picks study topics | 5-dim one-hot profile from `user_learning_profiles` | Topic suggestions | On-demand after onboarding |
> | **KNN Learning** (F049, FE-57) | Behavior-based word suggestions | N-dim topic accuracy from `test_answers` | Word suggestions | Background job 24h |
>
> F050 provides the admin controls (FE-19) that the F063 Dashboard calls.

---

### F047 — KNN: Configuration Setup & Existing Table Verification
```
Branch:    feature/knn/knn-config
Assignee:  Huy
Time:      1.5h
Depends on: F002
```
**What to do:** Confirm the existing tables are sufficient to run both KNN flows and set up config. Do NOT create new tables since the schema is finalized.

**Done when:**
- [ ] Confirm EF entities + `IEntityTypeConfiguration` map the source tables correctly:
  - `user_learning_profiles` (age_range_id, region_id, occupation_id, education_level_id, learning_purpose_id, created_at, updated_at)
  - `user_topic_preferences` (user_id, topic_id, source, status, created_at) — `source` supports the value `'knn_suggested'`
  - `age_ranges`, `regions`, `occupations`, `education_levels`, `learning_purposes` (onboarding lookup tables)
  - `test_answers` (session_id, word_id, is_correct) — used for the topic accuracy vector
  - `word_topics` (word_id, topic_id) — used to JOIN topics from test_answers
  - `user_word_progress` (user_id, word_id, mastery_level, srs_interval, ease_factor...)
  - `user_list_words` (user_id, list_id, word_id, status)
- [ ] Word recommendations (KNN Learning) store results in **Redis** instead of the DB: key `vocanova:knn-words:{user_id}`, value JSON array `WordRecommendationItem[]`, TTL 24h. Do NOT create a `recommendations` table.
- [ ] KNN config bound via `KnnOptions` from **`.env`/environment configuration** — do NOT hardcode in `appsettings.json`, do NOT create a `knn_model_configs` table:
  ```json
  "Knn": {
    "Onboarding": { "KValue": 5, "DefaultTopicLimit": 10, "MinSimilarity": 0.1, "CacheTtlMinutes": 30 },
    "Learning":   { "KValue": 5, "MinSessions": 5, "MinSimilarity": 0.1, "RecommendationCount": 50, "RebuildIntervalHours": 24, "CacheTtlMinutes": 60 }
  }
  ```
- [ ] `KnnOptions` strongly-typed class, injected via `IOptions<KnnOptions>`
- [ ] `WordRecommendationItem` record: `{ WordId, Word, PhoneticUk, PrimaryMeaning, ImageUrl, CefrLevel, Score }`
- [ ] Smoke test: can read `user_learning_profiles` + `user_topic_preferences` of seed users

---

### F048 — KNN Onboarding: Profile-Based Topic Recommendation
```
Branch:    feature/knn/onboarding-topic-recommendation
Assignee:  Huy
Time:      3.5h
Depends on: F047, F024 (topics list)
```
**What to do:** Cold-start KNN — use the learning profile (age/region/occupation/education/purpose) to suggest topics for a new user who has no test data yet. Triggered right after the user completes onboarding (F071).

**Done when:**
- [ ] `KnnOnboardingService.ComputeProfileVectorAsync(userId)`:
  - One-hot encode 5 groups: `age_ranges`, `regions`, `occupations`, `education_levels`, `learning_purposes`
  - Number of dimensions = total active records across the 5 lookup tables
  - If the user is missing a group → all dimensions of that group = 0.0, no exception thrown
  - Only encode records with `status = 'active'`
- [ ] `KnnOnboardingService.CosineSimilarity(double[] a, double[] b)`:
  - Return 0.0 if both are all-zero (avoid division by zero)
  - Unit test: identical → 1.0; zero vector → 0.0; orthogonal → 0.0
- [ ] `KnnOnboardingService.RecommendTopicsAsync(userId, limit)`:
  - Compute the current user's vector; if all-zero → jump straight to fallback
  - Compute cosine similarity against all users with at least 1 non-zero profile dimension
  - Take the K nearest neighbors (from `KnnOptions.Onboarding.KValue`), filter `similarity < MinSimilarity`
  - Aggregate topics from the neighbors' `user_topic_preferences` (`status='active'`, `source IN ('user_selected','onboarding')`)
  - Score each topic = `SUM(similarity_of_the_neighbor_who_has_that_topic)`
  - Exclude topics the current user already has with `status='active'` in `user_topic_preferences`
  - **Fallback** when there are not enough neighbors or the vector is all-zero: return the top N topics by highest frequency across all `user_topic_preferences` system-wide
  - Return `List<TopicRecommendationDto>`: topic_id, topic_name, topic_name_vi, icon, word_count, recommendation_score
- [ ] `GET /api/recommendations/topics?limit=10`:
  - Cache: `vocanova:knn-topics:{user_id}` TTL `KnnOptions.Onboarding.CacheTtlMinutes`
  - Return `[]` if the user has no profile yet (NOT an error)
- [ ] `POST /api/recommendations/topics/{topicId}/accept`:
  - Upsert `user_topic_preferences(user_id, topic_id, source='knn_suggested', status='active')`
  - If the record exists → update `source='knn_suggested'`
  - Invalidate cache `vocanova:knn-topics:{user_id}`
- [ ] Invalidate cache `vocanova:knn-topics:{user_id}` when the user's `user_learning_profiles` change (called from F018)
- [ ] Unit test: user missing profile → fallback returns valid results; recommendation excludes the topic the user already chose; accept → invalidate cache

---

### F049 — KNN Learning: Behavior-Based Word Recommendation (FE-57)
```
Branch:    feature/knn/learning-word-recommendation
Assignee:  Huy
Time:      4.5h
Depends on: F047, F041 (test_answers exist), F044 (user_word_progress with mastery_level)
```
**What to do:** Behavior-based KNN (FE-57) — topic accuracy from quiz history to suggest words. Results stored in Redis (no new table). A background job writes to Redis, the API reads from Redis.

**Done when:**
- [ ] `KnnLearningService.ComputeTopicAccuracyVectorAsync(userId)`:
  - For each active topic in the `topics` table: `accuracy_i = SUM(ta.is_correct) / COUNT(ta.answer_id)` from `test_answers ta` JOIN `test_sessions ts` JOIN `word_topics wt` WHERE `ts.user_id = userId AND wt.topic_id = i`
  - Topic with no data for the user → accuracy = 0.0 (not null)
  - User with < `KnnOptions.Learning.MinSessions` sessions → return `Result.Fail`, do NOT throw
- [ ] `KnnMathHelper.CosineSimilarity(double[] a, double[] b)`: shared utility, returns 0.0 if any vector is all-zero
- [ ] `KnnLearningService.FindKNearestAsync(userId, vector, k)`:
  - Load eligible users (≥ MinSessions, status='active', excluding the user themselves)
  - Compute cosine similarity against each user; filter `similarity < MinSimilarity`, take the top K
- [ ] `KnnLearningService.GenerateWordRecommendationsAsync(userId)`:
  - Call `ComputeTopicAccuracyVectorAsync` → if Fail → log and return (do not crash the job)
  - For each neighbor: take word_ids with `mastery_level >= 3` from `user_word_progress`
  - Score: `score_word = SUM(similarity_of_the_neighbor_who_has_the_word)`
  - Exclude word_ids the user already has in `user_list_words` (`status='active'`)
  - Sort DESC, take the top `KnnOptions.Learning.RecommendationCount`
  - **Store in Redis** (not the DB): key `vocanova:knn-words:{userId}`, value = JSON serialize of `List<WordRecommendationItem>`, TTL = `KnnOptions.Learning.RebuildIntervalHours` hours
- [ ] `GET /api/recommendations/words?limit=10`:
  - Read from the Redis key `vocanova:knn-words:{userId}`
  - On Redis miss → return `[]` (not 404, do not recompute)
  - Deserialize → JOIN the `words` table for the latest info (phonetic, image_url)
  - Return `WordRecommendationDto[]`: word_id, word, phonetic_uk, primary_meaning, image_url, cefr_level, score
- [ ] Unit test:
  - User < MinSessions → GenerateWordRecommendationsAsync returns early, the Redis key is not written
  - CosineSimilarity zero vector → 0.0, no exception
  - Neighbor has a word the user already owns → excluded
  - After Generate → Redis has the correct key with the correct TTL

---

### F050 — KNN Background Job & Onboarding Lookup Admin (FE-19)
```
Branch:    feature/knn/background-job-admin
Assignee:  Huy
Time:      3h
Depends on: F049, F048
```
**What to do:** An IHostedService that periodically rebuilds word recommendations + admin APIs to manage the 5 onboarding lookup tables used by KNN. KNN algorithm config is still read from `KnnOptions` via `.env`/configuration; do NOT create or CRUD a `knn_model_configs` table.

**Done when:**
- [ ] `KnnWordRecommendationJob : IHostedService`:
  - `PeriodicTimer` every `KnnOptions.Learning.RebuildIntervalHours` hours
  - Get all eligible users (≥ MinSessions, `status='active'`)
  - Call `KnnLearningService.GenerateWordRecommendationsAsync(userId)` sequentially (not parallel, to avoid DB overload)
  - An error on one user does NOT stop the whole job (try-catch per user, logged separately)
  - After finishing: store a timestamp in Redis `vocanova:knn-last-rebuild` (infinite TTL)
  - Log: number of users processed / skipped / errored, total run time
- [ ] Admin: `GET /api/admin/knn/config` — return the current config from `IOptions<KnnOptions>`:
  - onboarding: `k_value`, `default_topic_limit`, `min_similarity`, `cache_ttl_minutes`
  - learning: `k_value`, `min_sessions`, `min_similarity`, `recommendation_count`, `rebuild_interval_hours`, `cache_ttl_minutes`
  - read-only; to change config, edit the `.env`/deployment config then restart the app
- [ ] Admin lookup APIs for KNN onboarding, CRUD/soft delete/restore on the existing tables:
  - `GET/POST/PUT/DELETE/PATCH restore /api/admin/knn/age-ranges`
  - `GET/POST/PUT/DELETE/PATCH restore /api/admin/knn/regions`
  - `GET/POST/PUT/DELETE/PATCH restore /api/admin/knn/occupations`
  - `GET/POST/PUT/DELETE/PATCH restore /api/admin/knn/education-levels`
  - `GET/POST/PUT/DELETE/PATCH restore /api/admin/knn/learning-purposes`
- [ ] Each lookup management must have the full standard backend structure:
  - Dedicated response DTO, dedicated create/update request, dedicated query filter.
  - FluentValidation validator for create/update/query.
  - Repository interface + implementation.
  - Service interface + implementation, handling business rules and `Result<T>`.
  - Admin controller endpoint using `ControllerResultExtensions`.
  - DI registration in `Program.cs`.
  - Unit tests for the validator, service/repository, and important endpoint behavior.
- [ ] `GET` list for each lookup:
  - Support `page`, `limit`, `q`, `status`, `includeDeleted`.
  - Pagination per `AppSettings.DefaultPageLimit`/`MaxPageLimit`.
  - Case-insensitive search by `name`; regions additionally search by `code`.
  - Stable sort: `display_order`, `name`, id if the table has display_order; otherwise `name`, id.
  - By default return only `status='active'`; `includeDeleted=true` is admin/super_admin only, for restore management.
- [ ] `GET {id}` detail for each lookup:
  - Return 404 if not found.
  - By default do not return deleted unless `includeDeleted=true`.
- [ ] `POST` create for each lookup:
  - Trim input.
  - Set `status='active'`.
  - Block a duplicate active `name` within the same lookup table, case-insensitive.
  - Regions block a duplicate `code`, case-insensitive, including deleted if the DB unique constraint forbids duplicates.
  - Return 409 on duplicate, 400 on validation failure.
- [ ] `PUT {id}` update for each lookup:
  - Do not allow updating a deleted record unless restored first.
  - Block a duplicate active `name` on rename.
  - Regions block a duplicate `code` on code change.
  - Do not allow a region to pick itself or a descendant as `parent_id`.
  - Return 404 if not found, 409 on duplicate/conflict.
- [ ] Lookup delete rule:
  - Soft delete via `status='deleted'`.
  - Do not hard-delete a lookup row because `user_learning_profiles` references it via FK.
  - Delete idempotency: if already deleted, return 404 or conflict per the existing admin pattern, but do not mutate data.
  - It is possible to delete a lookup still used by users because this is a soft delete; the KNN vector only encodes active records, so a user referencing a deleted lookup is treated as a missing dimension.
  - No migration needed; use the existing schema.
- [ ] `PATCH {id}/restore` rule:
  - Restore via `status='active'`.
  - Block restore if it would create a duplicate active `name` or a duplicate `code` for regions.
  - Return 404 if the id doesn't exist, 409 on conflict.
- [ ] Lookup create/update validators:
  - Common `name`: required, trimmed, max per EF mapping (`50` for age range, `100` for the other tables).
  - Common `status`: not accepted from the client on create/update; status only changes via delete/restore.
  - `age_ranges`: `min_age >= 0`, `max_age >= 0`, `min_age <= max_age` when both are set, `display_order >= 0`.
  - `regions`: `code` required, trimmed, max `10`, allowed chars `[A-Z0-9_-]` after uppercase normalization; `parent_id` optional, must exist and be active, no self-reference, no cycle.
  - `occupations`: `description` optional max `255`.
  - `education_levels`: `description` optional max `255`, `display_order >= 0`.
  - `learning_purposes`: `description` optional max `255`.
- [ ] Invalidate cache `vocanova:knn-topics:{user_id}` when a lookup changes:
  - at minimum, clear for the affected users if they can be identified from `user_learning_profiles`
  - or clear the entire KNN topic cache namespace if a cache namespace scan is implemented
- [ ] Admin: `POST /api/admin/knn/trigger-rebuild`:
  - Rate limit: 1 req/5 minutes/admin
  - Call the rebuild service async (fire-and-forget, do not await the request)
  - Return `202 Accepted` immediately: `{ message: "Rebuilding, please wait...", triggered_at: NOW() }`
- [ ] Admin: `GET /api/admin/knn/rebuild-status`:
  - Read `vocanova:knn-last-rebuild` from Redis
  - Return `{ last_rebuild_at: DateTime?, is_running: bool }`
  - Used by F063 to display "Last rebuilt: X hours ago"
- [ ] `KnnConfigDto`: current onboarding/learning config values from `KnnOptions`
- [ ] `KnnRebuildStatusDto`: `last_rebuild_at`, `is_running`
- [ ] Lookup DTOs for the 5 onboarding groups for the F063 dashboard:
  - `AgeRangeDto`: `age_range_id`, `name`, `min_age`, `max_age`, `display_order`, `status`
  - `RegionDto`: `region_id`, `name`, `code`, `parent_id`, `parent_name`, `status`
  - `OccupationDto`: `occupation_id`, `name`, `description`, `status`
  - `EducationLevelDto`: `education_level_id`, `name`, `description`, `display_order`, `status`
  - `LearningPurposeDto`: `learning_purpose_id`, `name`, `description`, `status`
- [ ] Audit log written via middleware for `POST/PUT/DELETE/PATCH /api/admin/knn/*`
- [ ] Unit test:
  - trigger-rebuild twice within 5 minutes → the 2nd receives 429.
  - the job hits an error on one user → the next user still runs.
  - the config endpoint returns the correct `KnnOptions`.
  - lookup list/search/status/includeDeleted/pagination correct.
  - lookup create/update/delete/restore correct for each table.
  - duplicate name/code → 409.
  - invalid age range/region parent/cycle → 400.
  - delete/restore invalidates the KNN topic recommendation cache.

---

## PHASE 1 — Backend: Module Media (M8)

**M8 provider decision:**
- Word image assets are uploaded to **Cloudinary**. The API only stores the delivery URL in `words.image_url`; Cloudinary credentials/config are read from `.env`/environment configuration.
- Word audio assets are stored on **Amazon S3** and served via **Amazon CloudFront**. The API stores the public/CDN URL in `word_audio_assets.storage_url`; no local files are kept in the repo/runtime server.
- No migration needed for M8 if the data only needs the existing URLs (`words.image_url`, `word_audio_assets.storage_url`).
- Do not hardcode provider keys in `appsettings.json`; add keys to `.env.example`, bind via Options.

---

### F051 — Audio Upload & Soft Delete
```
Branch:    feature/media/audio-upload
Assignee:  Huy
Time:      2h
Depends on: F002
```
**Done when:**
- [ ] `POST /api/admin/words/{id}/audio` (multipart/form-data)
- [ ] Request accepts `accent` (`uk`/`us`) and the audio file.
- [ ] Validate MIME: `audio/mpeg`, `audio/wav`, `audio/ogg` — max 5MB.
- [ ] Upload the file to the Amazon S3 bucket with the standard key: `words/{word_id}/audio/{accent}/{yyyyMMddHHmmss}-{safeFileName}`.
- [ ] The returned delivery URL should be a CloudFront URL if `AudioStorage__CloudFrontBaseUrl` is configured; the S3 object URL fallback is dev-only.
- [ ] Insert `word_audio_assets` with `word_id`, `accent`, `source='uploaded'`, `storage_url`, `status='uploaded'`, `created_at`.
- [ ] `DELETE /api/admin/words/{id}/audio/{audioId}`: soft delete (`status='deleted'`)
- [ ] Delete does not remove the S3 object within the request; only soft-deletes in the DB. Hard object cleanup is a separate job/scope if needed.
- [ ] `IAudioStorage` interface + `S3AudioStorage` implementation.
- [ ] Config via `.env.example`:
  - `AudioStorage__Provider=S3`
  - `AudioStorage__BucketName=`
  - `AudioStorage__Region=`
  - `AudioStorage__AccessKey=`
  - `AudioStorage__SecretKey=`
  - `AudioStorage__CloudFrontBaseUrl=`
- [ ] Unit tests: MIME/size invalid, upload saves `storage_url`, delete soft-deletes only, S3 key generation deterministic/safe.

---

### F052 — Cloudinary Image Upload & URL Update
```
Branch:    feature/media/cloudinary-image
Assignee:  Huy
Time:      1.5h
Depends on: F051
```
**Done when:**
- [ ] `POST /api/admin/words/{id}/image` (multipart/form-data) uploads the image to Cloudinary.
- [ ] Validate MIME: `image/jpeg`, `image/png`, `image/webp` — max 5MB.
- [ ] Upload into the standard Cloudinary folder/key: `vocanova/words/{word_id}`.
- [ ] Store the Cloudinary secure delivery URL in `words.image_url`.
- [ ] `PUT /api/admin/words/{id}/image`: still supports setting the URL manually if the admin needs to reuse an existing URL.
- [ ] Validate the manual `image_url` is a valid URL and only allow `https`.
- [ ] `POST /api/admin/words/{id}/image/suggest` is downgraded to optional/later; do not use Pixabay/Unsplash as the default provider for the primary upload.
- [ ] `IImageStorage` interface + `CloudinaryImageStorage` implementation.
- [ ] Config via `.env.example`:
  - `Cloudinary__CloudName=`
  - `Cloudinary__ApiKey=`
  - `Cloudinary__ApiSecret=`
  - `Cloudinary__Folder=vocanova/words`
- [ ] Unit tests: invalid MIME/size, Cloudinary upload result persisted to `words.image_url`, manual URL validation.

---

## PHASE 1 — Backend: Module Admin API (M9)

---

### F053 — Admin User Management
```
Branch:    feature/admin/user-management
Assignee:  Huy
Time:      2h
Depends on: F020
```
**Done when:**
- [ ] `GET /api/admin/users?page=&status=&search=`: uses `.IgnoreQueryFilters()`, filter by status/search phone/name
- [ ] `GET /api/admin/users/{id}`: user detail + learning profile
- [ ] `PATCH /api/admin/users/{id}/deactivate`: SuperAdmin only — soft delete + revoke all refresh tokens
- [ ] `PATCH /api/admin/users/{id}/restore`: SuperAdmin only
- [ ] Unit test: deactivate → tokens revoked; search by phone

---

### F054 — Admin Stats Endpoints
```
Branch:    feature/admin/stats
Assignee:  Huy
Time:      2.5h
Depends on: F043
```
**Done when:**
- [ ] `GET /api/admin/stats/dashboard`: total_users, total_words, sessions_today, avg_accuracy_7d (cache 5 minutes)
- [ ] `GET /api/admin/stats/demographics`: GROUP BY age_range, occupation, education_level
- [ ] `GET /api/admin/stats/learning`: top 20 most-missed words system-wide, accuracy trend 30 days
- [ ] `GET /api/admin/audit-logs?page=&userId=&entity=`: paginated

---

## PHASE 2 — Dashboard MVC

---

### F055 — Dashboard Cookie Auth & Layout
```
Branch:    feature/dashboard/auth-layout
Assignee:  Tan
Time:      3h
Depends on: F015
```
**Done when:**
- [ ] Cookie-based auth setup in `Program.cs`
- [ ] `AuthController.Login`: uses `AuthService` (shared service layer), sets the cookie
- [ ] `AuthController.Logout`: clears the cookie
- [ ] `_Layout.cshtml`: sidebar with icons, responsive
- [ ] Role-based sidebar: Admin does not see the "Admin Accounts" tab
- [ ] Redirect: `/login` if not authenticated; redirect `/dashboard` after login

---

### F056 — Dashboard Overview Page
```
Branch:    feature/dashboard/overview
Assignee:  Tan
Time:      2.5h
Depends on: F054, F055
```
**Done when:**
- [ ] 4 stat cards: Users, Words, Sessions today, Accuracy 7 days
- [ ] Line chart (Chart.js): sessions/day — last 7 days
- [ ] Pie chart: system-wide mastery level distribution
- [ ] Auto-refresh every 5 minutes (JavaScript `setInterval`)

---

### F057 — Vocabulary List & Filter
```
Branch:    feature/dashboard/vocab-list
Assignee:  Tan
Time:      3h
Depends on: F022, F024, F055
```
**Done when:**
- [ ] DataTable with: search (word name), filter CEFR level, filter topic, filter status
- [ ] "Show deleted" toggle → uses `.IgnoreQueryFilters()`
- [ ] Each row: word, CEFR badge, topic chips, status badge, Edit/Delete/Restore buttons
- [ ] Delete button confirm dialog
- [ ] Restore button (only shown when viewing deleted)
- [ ] Server-side pagination (not client-side DataTable)

---

### F058 — Vocabulary Detail & Sense Management
```
Branch:    feature/dashboard/vocab-detail
Assignee:  Tan
Time:      3.5h
Depends on: F057
```
**Done when:**
- [ ] `Vocabulary/Detail.cshtml`: show word info + image + audio player
- [ ] Senses accordion: each sense has inline Edit/Delete buttons (AJAX)
- [ ] Add-new-sense form (AJAX, no page reload)
- [ ] Examples list inline within the sense, with add/remove
- [ ] Relations table: synonym/antonym (view only here)
- [ ] Audio section: list audio assets (UK/US), upload new, delete (confirm)

---

### F059 — Vocabulary CSV Import UI
```
Branch:    feature/dashboard/vocab-import
Assignee:  Tan
Time:      2h
Depends on: F029, F057
```
**Done when:**
- [ ] `Vocabulary/Import.cshtml`: drag-drop zone + file picker
- [ ] Preview: sample template CSV download link
- [ ] After upload: a result table — imported/skipped/errors
- [ ] Error rows: highlighted red with columns "Row #", "Column", "Message"
- [ ] "Download errors as CSV" button

---

### F060 — User Management Pages
```
Branch:    feature/dashboard/user-management
Assignee:  Tan
Time:      3h
Depends on: F053, F055
```
**Done when:**
- [ ] `Users/Index.cshtml`: list users, filter status (active/locked/deleted), search phone/name
- [ ] "Show deleted" toggle
- [ ] Status badge: green (active) / orange (locked) / red (deleted)
- [ ] `Users/Detail.cshtml`: tabs — Profile | Learning Profile | Test History | Activity Log
- [ ] Deactivate button (SuperAdmin) with a confirm modal
- [ ] Restore button (SuperAdmin), only shown when the user is deleted

---

### F061 — Topic Management Page
```
Branch:    feature/dashboard/topic-management
Assignee:  Tan
Time:      2h
Depends on: F028, F055
```
**Done when:**
- [ ] `Topics/Index.cshtml`: DataTable, inline CRUD
- [ ] Delete button: **disabled** if `word_count > 0`, tooltip "Cannot delete — {N} words still use this topic"
- [ ] Restore button for deleted topics
- [ ] Inline edit: icon, topic_name, topic_name_vi

---

### F062 — Statistics Page
```
Branch:    feature/dashboard/statistics
Assignee:  Tan
Time:      3h
Depends on: F044, F055
```
**Done when:**
- [ ] Sessions-over-time chart: granularity dropdown (daily/weekly/monthly), AJAX chart update
- [ ] Accuracy trend chart
- [ ] Top 20 wrong words: table with word, wrong_count, % accuracy
- [ ] Demographics: 3 charts (age range, occupation, education level)

---

### F063 — KNN Management Page
```
Branch:    feature/dashboard/knn-management
Assignee:  Tan
Time:      1.5h
Depends on: F050, F055
```
**Done when:**
- [ ] Sidebar/menu "KNN Management" with sub-items:
  - AgeRange Name Management
  - Regions Management
  - Occupation Management
  - Education Levels Management
  - Learning Purposes Management
- [ ] Each lookup page has a table + CRUD + soft delete/restore:
  - Age ranges: name, min_age, max_age, display_order, status
  - Regions: name, code, parent, status
  - Occupations: name, description, status
  - Education levels: name, description, display_order, status
  - Learning purposes: name, description, status
- [ ] Each lookup page must have full management UX:
  - Search box, status filter, "Show deleted" toggle, server-side pagination.
  - Create modal/form, edit modal/form, inline validation messages per the API validator.
  - Delete confirmation modal, restore action only shown when viewing deleted.
  - Duplicate/conflict from the API clearly shown in the form/table.
  - Loading/empty/error states.
  - Do not allow editing `status` directly; status only via Delete/Restore.
- [ ] Dashboard form validation must mirror the backend validators:
  - Age ranges: valid min/max age, non-negative display_order.
  - Regions: code format, parent not self-referencing.
  - Description max length 255.
  - Name required and correct max length per table.
- [ ] Show the current KNN model config read-only on the overview page: K value, min sessions, recommendation count, min similarity, rebuild interval, cache TTL
- [ ] Hint the operator to edit `.env`/deployment config to change model settings
- [ ] "Trigger Rebuild" button with a loading state (AJAX)
- [ ] Show "Last rebuilt: X hours ago"

---

### F063A — Admin Profile Page
```
Branch:    feature/dashboard/admin-profile
Assignee:  Tan
Time:      2h
Depends on: F018, F055
```
**What to do:** A profile page for the currently logged-in admin (`/profile`) — view & update personal info, avatar, and change password. The API already exists, no new endpoints needed.

**Reused API (already in `AuthController`):**
- `GET /api/auth/me` — load the current profile.
- `PUT /api/auth/me/profile` — update `display_name`, `avatar_url`.
- `POST /api/auth/me/avatar` — upload the avatar.
- `PUT /api/auth/me/password` — change password (current + new).

**Done when:** ✅ **COMPLETED 2026-07-06 (code, build 0 errors)**
- [x] `/profile` page (`ProfileController` + `Views/Profile/Index.cshtml`): 2-column layout (identity card + forms), theme-aware, all labels via `@T[]`. CSS `.profile-*` in `site.css`.
- [x] Display from `GET /api/auth/me`: avatar (image or initial), `display_name`, `phone` (masked, read-only), `role` (badge), `status`. **NOTE:** `UserProfileDto` does NOT have `created_at`/`last_login` → those 2 fields were dropped (only real fields are shown).
- [x] Edit `display_name` + `avatar_url` → `PUT /api/auth/me/profile`; after saving, **re-GET /me then re-issue the cookie** (`RefreshIdentityAsync`) so the topbar name/avatar update immediately.
- [x] Update avatar: file upload via `POST /api/auth/me/avatar` (multipart field `file`), accept png/jpeg/webp.
- [x] Change password via `PUT /api/auth/me/password`: current + new + confirm form; validation mirrors `StrongPasswordValidator` (≥8, upper/lower/digit) + confirm match.
- [x] "Profile" link from the topbar account block (`_Layout`: `<div>` → `<a href="/profile">`).
- [x] Validation mirrors the backend (`display_name` 2–150; strong password); maps errors 400/401/403.
- [x] Result popup reuses `#result-modal` (uses `TempData["UserSuccess"]`/`["UserError"]` like Users; messages translated via `@T[]`).
- [x] BFF proxy via `IVocaNovaApiClient` (+ `BearerTokenHandler`), `[Authorize]` + `[ValidateAntiForgeryToken]` on every POST.
- [ ] **Remaining (needs a running app):** visual QA — view/edit profile, upload avatar, change password, verify the topbar updates immediately + i18n/theme.

---

## PHASE 2.5 — Dashboard Revisions (adjustments after review)

> **Context:** After the dashboard finished F055–F063, a real review found 4 areas to adjust:
> alphabetical search (a–z), verify vocabulary CRUD, complete the language translation, and the Settings page
> bug where it does not switch language/theme by itself. Each item below = 1 revision branch (`fix/…` or `feature/…`).
>
> **Note on the current i18n architecture (verified in code):**
> - `Services/Localization/Translator.cs`: reads the cookie `VocaNova.Dashboard.Language`, registered **Scoped** in `Program.cs`
>   (recreated per request → re-reads the cookie). The source strings in the views are **English**; when `Language == "vi"` it maps
>   to Vietnamese via `TranslationTable.Vietnamese`, and falls back to English on a missing key.
> - Because the Translator is Scoped, **every page switches language correctly** right after the cookie changes. A page that "doesn't switch"
>   is due to **hardcoded bilingual** strings in the view that bypass `@T[]` (see R04).

---

### R01 — Vocabulary Search: verify & add an alphabetical (A–Z) index
```
Branch:    fix/dashboard/vocab-search-alphabet
Assignee:  Tan
Time:      2h
Depends on: F057
```
**Current state (verified):**
- `VocabularyController.Index` receives `q` → `WordListFilter.Q` → calls the API `GET /api/words?q=` (F022 uses `word_key LIKE {NormalizeWord(q)}%`).
- This is a **prefix search**: typing any single character `a`–`z` returns words that **start with** that character → technically a–z already works.
- There is **no** alphabetical index bar (A B C … Z) for quick filtering; the user has to type.

**Decision (finalized 2026-07-06):** support **both** — keep the prefix search (free-text search box) **and** add an A–Z bar for quick filtering by first letter.

**Done when:** ✅ **COMPLETED 2026-07-06**
- [x] Confirmed: searching a single `a`…`z` character returns words starting with it (API `word_key LIKE q%`, verified via `VocabularyController.Index` → `WordListFilter.Q`).
- [x] Added an A–Z index bar in `Views/Vocabulary/Index.cshtml` (just below `filter-bar`): an "All" button + 26 `A`…`Z` buttons (helper `LetterUrl`).
- [x] Clicking a letter = navigate to `/vocabulary?q={char}` (preserving cefr, wordType, topic, status, includeDeleted; page reset to 1).
- [x] The selected letter is highlighted (`selectedLetter` = `Model.Q` when it's exactly one letter); "All" is active when there's no `q`.
- [x] A–Z only (English vocabulary).
- [x] Labels/aria go through `@T[]` (added keys `All`, `Filter by first letter` to `TranslationTable`).
- [x] Responsive: `.alpha-bar { flex-wrap: wrap; }` in `site.css`; `.alpha-link` style is theme-aware (uses `--accent`/`--surface`/`--border`).
- [x] Dashboard build: 0 errors.

---

### R02 — Vocabulary CRUD: verify the whole flow & close gaps
```
Branch:    fix/dashboard/vocab-crud-verify
Assignee:  Tan
Time:      2.5h
Depends on: F057, F058
```
**Current state (verified in `VocabularyController.cs`):**
- **Create** (`/vocabulary/create`): create the word → create each sense (Word type + EN/VI meaning + 1 example/sense). OK.
- **Edit** (`/vocabulary/{id}/edit`): PUT metadata + update existing senses + add new meaning + collect examples per block + Active toggle = delete/restore. OK.
- **Delete/Restore** (`/vocabulary/delete`, `/restore`): soft delete/restore, preserves filters via `returnUrl`. OK.
- Examples are persisted (commit `1f061e2 feat(dictionary): persist sense examples on create/update`).

**Decision (finalized 2026-07-06): TEMPORARILY DISABLE the delete button (sense/example).** ✅ **DONE 2026-07-06**
- [x] Deleting a **saved** example on the Edit screen: the ✕ button becomes `disabled` + `.is-locked` class + tooltip "Removing saved examples is temporarily disabled." (`Edit.cshtml`). The ✕ on a **newly added** (unsaved) example row still works to discard a draft row.
- [x] JS `vocabulary-edit.js` skips `disabled`/`.is-locked` buttons; CSS `site.css` mutes the locked button.
- [x] Sense delete: **there is no delete-sense control in the UI** (Edit only adds/updates, Detail is read-only) → already "unsupported", nothing to add.
- [x] Keep the add/update flow; only block deletes to avoid data loss. Code not removed, only disabled — easy to re-enable later.

**Verified by reading code (`VocabularyController` + views):**
- [x] Create: create the word → create each sense (Word type + EN/VI meaning + example). Duplicate `word_key` → 409 "That word already exists."; missing `word` → "Word is required.".
- [x] Edit: PUT metadata + update existing senses + add new meaning + collect examples per block; Active toggle off→`deleted`, on→`active` (calls the Delete/Restore API).
- [x] Delete/Restore from the list: soft delete/restore, preserves filters via `returnUrl`.
- [x] Permissions: `canManage = role is "admin" or "super_admin"` → hides the Edit/Delete/Restore buttons; the API still blocks with 401/403.
- [x] Error messages mapped by status 400/409/403 in the controller.
- ⚠️ **Note (not the delete button):** clearing the "English meaning"/"English example" field to blank then Save makes the controller skip that block (`ExamplesForBlock`/sense loop skip on empty) → it may silently not update/save. This is an edit path, out of scope for locking the delete button; noted for a possible tightening in a later revision.

**Remaining — needs a running app for visual confirmation (log into `VocaNova_Activity_History.md`):**
- [ ] Run Create/Edit/Delete/Restore end-to-end in the UI against a real API + DB, take screenshots.
- [ ] Confirm the ✕ button on a saved example is muted + cannot delete; the ✕ on a newly added row can still discard.

---

### R03 — Complete the language translation (i18n coverage)
```
Branch:    feature/dashboard/i18n-coverage
Assignee:  Tan
Time:      2.5h
Depends on: F055
```
**Current state (verified):**
- The translation mechanism exists and is **correct** (`Translator` + `TranslationTable`, Scoped). The problem is **insufficient coverage**: some strings are hardcoded and bypass `@T[]`, so they don't switch by language.
- Examples found:
  - `Views/Vocabulary/Index.cshtml` around line 31: the page subtitle before `@Model.TotalItems @T["word(s)."]` was hardcoded Vietnamese (not going through `@T[]`).
  - `Views/Settings/Index.cshtml`: theme/language card labels and action buttons were hardcoded bilingual (see R04).
  - `data-confirm="Delete '@item.Word'? …"` (Index.cshtml) was hardcoded English.

**Done when:** ✅ **COMPLETED 2026-07-06 (except Settings — belongs to R04)**
- [x] Swept all `Views/**/*.cshtml` via grep for Vietnamese characters + attributes (placeholder/title/data-confirm/aria-label) + text nodes.
- [x] Wrapped the remaining hardcoded strings with `@T[...]` / `@(T.Format(...))` using English source keys:
  - `Vocabulary/Index.cshtml`: subtitle "Manage vocabulary metadata"; `data-confirm` (Format).
  - `Vocabulary/Edit.cshtml`: placeholder "Vietnamese meaning".
  - `Vocabulary/Create.cshtml`: VI example placeholder ("e.g. This word is very beautiful.").
  - `Topics/Index.cshtml`: "Topic name (VI)" (×2), "Name (VI)", cannot-delete title (Format), delete-topic data-confirm (Format).
  - `_Layout.cshtml`: aria-label "Toggle navigation" (×2), "Primary navigation".
  - `Knn/Index.cshtml` + `Knn/Lookup.cshtml`: rebuild / delete-item data-confirm.
- [x] Added ~14 new keys to `TranslationTable.Entries` (EN→VI pairs), including strings with `{0}` for `Format`.
- [x] Server messages render via `@T[toastMsg]` in `_Layout` (already in place).
- [x] Build 0 errors; the remaining grep is clean (only `Settings` belongs to R04 + the neutral "OK" button).
- [ ] **Remaining (needs a running app):** visually verify `vi`↔`en` is consistent across all pages, no mixed strings.
- ➡️ The bilingual "X / Y" strings on the Settings page are handled in **R04**.

---

### R04 — Fix: the Settings page does not switch language/theme by itself 🔴
```
Branch:    fix/dashboard/settings-not-reacting
Assignee:  Tan
Time:      2h
Depends on: R03
```
**Symptom (as reported):** when changing the language on Settings, **other pages** switch correctly, but the **Settings page itself** does not.

**Root cause (verified — NOT a Translator bug):**
- `Translator` is **Scoped** → after saving the cookie and redirecting back to `/settings`, the `@T[]` strings on the Settings page **do** switch correctly.
- But the **most prominent** parts of the Settings page were **hardcoded bilingual**, bypassing `@T[]`, so it looked like it "didn't switch":
  - `Views/Settings/Index.cshtml`: the bilingual Light/Dark theme card labels.
  - The bilingual Vietnamese/English language row labels.
  - The bilingual Cancel and Save Changes buttons.
  - `SettingsController.Save`: `TempData["SettingsSaved"]` was a hardcoded bilingual string.
- On **theme**: `settings.js` only toggles the card highlight + hidden input (not saved until Save). `_Layout` reads the theme cookie and sets `data-theme` server-side → after Save + reload, the Settings page **should** switch theme; verify the Settings-specific components (`appearance-card`, `language-row`) use theme-aware CSS variables in `site.css`, avoiding fixed colors.

**Done when:** ✅ **COMPLETED 2026-07-06**
- [x] Replaced all hardcoded bilingual labels in `Views/Settings/Index.cshtml` with `@T[]` (removed the bilingual sub-line):
  - Theme cards: `@T["Light Mode"]`, `@T["Dark Mode"]` (removed `.appearance-sub`).
  - Language rows: `@T["Vietnamese"]`, `@T["English"]` (removed `.language-sub`).
  - Buttons: `@T["Cancel"]`, `@T["Save Changes"]`.
- [x] Added keys to `TranslationTable`: `Light Mode`, `Dark Mode`, `Vietnamese`, `English`, `Save Changes`, `Changes saved.`, `Theme set to Dark./Light.`.
- [x] `SettingsController.Save`: `TempData["SettingsSaved"] = "Changes saved."` (EN key); the view renders `@T[saved]` (instead of `@saved`).
- [x] Theme: checked the CSS — `.appearance-card`/`.language-row`/`.settings-panel` are all theme-aware (`var(--surface-2)`, `--text`, `--accent`…); only `.icon-dark` is fixed by design (the moon icon chip). **No CSS change needed.**
- [x] Settings grep clean: no hardcoded Vietnamese characters and no "X / Y" strings left. Build 0 errors.
- [ ] **Remaining (needs a running app):** visually verify choosing `en`+Save → Settings fully English; `vi`+Save → fully Vietnamese; switching Dark/Light recolors correctly.
- ➡️ Skipped "apply immediately on click" — keep the current Save model (simpler, consistent after reload).

---

### R05 — Standardize popup notifications across the whole Dashboard (drop inline alerts)
```
Branch:    fix/dashboard/popup-notifications
Assignee:  Tan
Time:      1.5h
Depends on: F057, F061, R04
```
**What to do:** Per the **Global UI Convention — Popup Notifications**, migrate every **result** notification currently shown as an inline `alert` to the centered `#result-modal` popup (like the Users page). Keep the confirm modal (delete/lock) and the inline states (loading/empty).

**Current state (verified):** Users already uses the `#result-modal` popup. Still showing inline `alert`s (to migrate):
- `Views/Vocabulary/Index.cshtml`: `TempData["VocabSuccess"]` / `["VocabError"]`.
- `Views/Topics/Index.cshtml`: `TempData["TopicSuccess"]` / `["TopicError"]`.
- `Views/Settings/Index.cshtml`: `TempData["SettingsSaved"]` (alert-success).
- `Views/Vocabulary/Create.cshtml` / `Edit.cshtml`: `TempData["VocabError"]` inline.

**Done when:**
- [ ] Move the above `TempData` to the `TempData["UserSuccess"]` / `TempData["UserError"]` pair (or extend `_Layout` to also accept the existing success/error keys) → show via `#result-modal`.
- [ ] Messages go through `@T[...]` (ensure translation keys exist); green ✓ / red ⚠ icon, title "Success"/"Failed", OK button.
- [ ] Remove the `<div class="alert alert-success/danger">` blocks used for result notifications on those pages.
- [ ] Keep: the delete/lock confirm modal; loading/empty states; offline (mobile).
- [ ] Build 0 errors; visually QA that each page shows the correct popup after an action.

---

### R06 — Fix: JS-driven popups/labels ignore the selected language 🔴
```
Branch:    fix/dashboard/js-i18n-popups
Assignee:  Tan
Time:      2h
Depends on: R03, R04
```
**Symptom (reported):** with the UI in Vietnamese, the "Disable/Restore user account" confirmation modal shows English title/body/confirm button; only the "Hủy" (Cancel) button is Vietnamese. Same class of issue on a few other surfaces.

**Root cause (verified):** `Translator`/`@T[]` runs **server-side only**. Several JS files build UI text with **hardcoded English literals**, bypassing i18n, so they never switch language. In the Users modal, only "Cancel" stayed Vietnamese because it's the one piece still rendered by Razor; `users-list.js` overwrote title/body/confirm with English on open.

**Fix (Method A — server renders translations into `data-*`, JS reads them):** ✅ **COMPLETED 2026-07-06 (code, build 0 errors)**
- [x] `Users/Index.cshtml` + `users-list.js`: disable/restore modal title/body/confirm read from `data-disable-*` / `data-restore-*` on `#user-modal` (`{0}` name injected + HTML-escaped by JS).
- [x] `Vocabulary/Import.cshtml` + `vocabulary-import.js`: "Upload & import"/"Importing..." button labels + "Please choose a CSV file."/"Import failed."/"Import request failed." alerts via `data-*`.
- [x] `Vocabulary/Edit.cshtml` + `vocabulary-edit.js`: Active/Inactive toggle label via `data-active`/`data-inactive` on `#status-label`.
- [x] `Vocabulary/Detail.cshtml` + `vocabulary-detail.js`: "Request failed." toast + "Are you sure?" confirm fallback via `data-*` on `#detail-root`.
- [x] `Vocabulary/Create.cshtml` + `vocabulary-create.js`: dynamically added "Remove meaning" aria-label via `data-remove-label`.
- [x] Chart tooltip labels (legend hidden): `Statistics/Index.cshtml` + `statistics.js` (Sessions/Accuracy %/Users) and `Dashboard/Index.cshtml` + `dashboard-overview.js` (Sessions/Accuracy %) via canvas `data-label*`.
- [x] Added the missing keys to `TranslationTable` (`Restore user account`, the `{0}` disable/restore bodies, `Are you sure?`, `Request failed.`, `Please choose a CSV file.`, `Importing...`, `Import failed.`, `Import request failed.`, `Remove meaning`). Verified the view `@T[...]` keys match the table byte-for-byte (straight apostrophe).
- [x] `topics.js`/`knn.js` native `confirm()` fallback `'Are you sure?'` left as-is — their `data-confirm` is already localized by `@T`, so the fallback never renders.
- ⚠️ **Known remaining (separate, backend):** AJAX success toasts on the Detail page show the controller's message (e.g. "Sense added.") which is returned in English by the API regardless of language. Localizing those needs the controller to return keys the view/JS can translate — out of scope for this JS fix.
- [ ] **Remaining (needs a running app):** visually confirm the modal + import + edit toggle switch language after `dotnet run` + Ctrl+F5.

---

## PHASE 3 — Flutter Mobile

---

### F064 — Project Init & Theme
```
Branch:    feature/mobile-core/project-init-theme
Assignee:  Nhut
Time:      2h
Depends on: —
```
**Done when:**
- [ ] Flutter project `vocanova_mobile` created and runnable
- [ ] `pubspec.yaml` with all dependencies
- [ ] `AppColors`: primary `#B8AEFF`, background `#1C1A2E`, surface `#2A2740`, error `#FF6B6B`
- [ ] `AppTheme.dark()` + `AppTheme.light()`
- [ ] `AppTextStyles`: heading, body, caption, label
- [ ] Global font (Inter or Nunito from Google Fonts)
- [ ] **`AppResultDialog` — the shared result-notification popup widget (per the Global UI Convention)**: `showResultDialog(context, {required bool success, required String message})` → a centered `AlertDialog` with a round ✓ (green) / ⚠ (red) icon, title "Success"/"Failed", message, OK button. Every screen uses this widget for success/error notifications (do NOT use `SnackBar`).

---

### F065 — DioClient & Interceptors
```
Branch:    feature/mobile-core/dio-interceptors
Assignee:  Nhut
Time:      2.5h
Depends on: F064
```
**Done when:**
- [ ] `DioClient` singleton: base URL, connectTimeout 10s, receiveTimeout 30s
- [ ] `AuthInterceptor`:
  - `onRequest`: attach `Authorization: Bearer {token}` from `SecureStorage`
  - `onError` 401: call the refresh endpoint → retry the original request → if refresh fails → logout
- [ ] `ErrorInterceptor`: parse `errors[]` from the body → throw `AppException(message)` with a Vietnamese message
- [ ] `ApiEndpoints` class: all URLs are const strings
- [ ] Test: mock 401 → the interceptor refreshes automatically

---

### F066 — LocalStorage & SecureStorage
```
Branch:    feature/mobile-core/local-secure-storage
Assignee:  Nhut
Time:      2h
Depends on: F064
```
**Done when:**
- [ ] `LocalStorage` class (singleton, shared_preferences):
  - `getWithTtl<T>()` / `setWithTtl<T>()` — store with `{key}_saved_at` milliseconds
  - `get()` / `set()` normal (no TTL)
  - `remove()`, `clearAll()`
  - Keys: `user_profile_json`, `lists_cache_json`, `word_cache_{id}_json`, `progress_summary_json`, `search_history_json`, `app_locale`, `app_theme`
- [ ] `SecureStorage` class (flutter_secure_storage): `saveAccessToken`, `getAccessToken`, `saveRefreshToken`, `getRefreshToken`, `clearTokens`
- [ ] Unit test: TTL expired → returns null; TTL not expired → returns the value

---

### F067 — GoRouter Setup
```
Branch:    feature/mobile-core/go-router
Assignee:  Nhut
Time:      2h
Depends on: F064
```
**Done when:**
- [ ] `AppRouter` with all routes: `/login`, `/register`, `/otp`, `/onboarding`, `/home`, `/search`, `/word/:id`, `/lists`, `/list/:id`, `/quiz/config`, `/quiz/active`, `/quiz/result`, `/progress`, `/settings`, `/profile`
- [ ] `AuthGuard`: redirect `/login` if there's no token
- [ ] `RootRedirect`: check token → `/home` or `/login`
- [ ] Bottom navigation bar (Home / Search / Lists / Progress)

---

### F068 — AuthNotifier & AuthRepository
```
Branch:    feature/mobile-auth/auth-provider
Assignee:  Nhut
Time:      2.5h
Depends on: F066, F067
```
**Done when:**
- [ ] `AuthState`: status (initial/loading/authenticated/unauthenticated/error), user (UserProfile?)
- [ ] `AuthNotifier extends _$AuthNotifier`:
  - `login(phone, password)`
  - `googleLogin(idToken)`
  - `logout()`: clear SecureStorage + LocalStorage.clearAll() + navigate /login
  - `loadCurrentUser()`: GET /auth/me, cache for 1 day
- [ ] `AuthRepository`: wrap the Dio calls
- [ ] Tokens in `SecureStorage`, profile in `LocalStorage` with TTL 1 day

---

### F069 — Login & Register Screens
```
Branch:    feature/mobile-auth/login-register-screens
Assignee:  Nhut
Time:      3h
Depends on: F068
```
**Done when:**
- [ ] `LoginScreen`: phone field (VN format hint), password (toggle show/hide), "Forgot password" link, Google sign-in button
- [ ] `RegisterScreen`: phone, password, confirm password (cross-validate), display_name
- [ ] Form validation: inline error messages in Vietnamese (per-field validation errors still shown inline under each field)
- [ ] Loading state on submit (disable button, show CircularProgressIndicator)
- [ ] **Error popup notification** on API failure (`AppResultDialog` centered: ⚠ icon + title "Failed" + message + OK) — per the Global UI Convention, do **NOT** use SnackBar

---

### F070 — OTP & Forgot Password Screens
```
Branch:    feature/mobile-auth/otp-forgot-screens
Assignee:  Nhut
Time:      2.5h
Depends on: F069
```
**Done when:**
- [ ] `OtpScreen`: 6 input boxes auto-focus next, 60s resend countdown, a notice on the max 5 wrong attempts
- [ ] `ForgotPasswordScreen`: phone → OTP → new password (3 steps in 1 screen)
- [ ] OTP auto-submits when all 6 digits are entered

---

### F071 — Onboarding Screen
```
Branch:    feature/mobile-auth/onboarding-screen
Assignee:  Nhut
Time:      2.5h
Depends on: F068
```
**Done when:**
- [ ] 5 steps: Age Range, Region, Occupation, Education Level, Learning Purpose
- [ ] Each step: a list of chip selects (single select)
- [ ] Progress indicator (step X/5)
- [ ] "Skip" button (onboarding optional)
- [ ] Submit: call `PUT /auth/me/learning-profile`

---

### F072 — Word Search Screen
```
Branch:    feature/mobile-dictionary/search-screen
Assignee:  Nhut
Time:      3h
Depends on: F067
```
**Done when:**
- [ ] Search bar always visible at the top
- [ ] Debounce 300ms: only call the API 300ms after typing stops
- [ ] When the search bar is empty: show search_history (max 20 recent words, with a clear button)
- [ ] Results: `WordSummaryCard` (word, phonetic, CEFR badge, short meaning)
- [ ] Filter chips: CEFR (A1–C2), Topics (from the API)
- [ ] Loading skeleton while searching
- [ ] Offline: banner + search only within cached words + history

---

### F073 — Word Detail Screen
```
Branch:    feature/mobile-dictionary/word-detail-screen
Assignee:  Nhut
Time:      3h
Depends on: F072
```
**Done when:**
- [ ] Offline cache check before calling the API (TTL 2 hours)
- [ ] Hero section: word, phonetic UK/US (tap to switch), CEFR badge, image
- [ ] Audio player: play UK / US button (using `audioplayers`)
- [ ] Senses accordion: word_class badge, definition EN + VI, examples list
- [ ] Related words chips: synonym (green), antonym (red) — tap to navigate
- [ ] Topics chips at the bottom
- [ ] FAB: "Add to list" → open `AddToListSheet`
- [ ] "Add to word book" button saves to `word_cache_{id}` shared_pref

---

### F074 — Lists Screen & Create
```
Branch:    feature/mobile-lists/lists-screen
Assignee:  Nhut
Time:      2.5h
Depends on: F067
```
**Done when:**
- [ ] Grid view of lists: card with list_name, word_count, created_at
- [ ] FAB "+" → create-list dialog (validate the name)
- [ ] Long-press a list → actions: Rename, Delete (confirm)
- [ ] Offline: show cached lists + banner
- [ ] `ListsNotifier` optimistic update on delete (remove from UI immediately, rollback if the API fails)

---

### F075 — List Detail Screen
```
Branch:    feature/mobile-lists/list-detail-screen
Assignee:  Nhut
Time:      3h
Depends on: F074
```
**Done when:**
- [ ] Words in the list: paginated (infinite scroll)
- [ ] Each word card: word, meaning, correct/wrong count, note
- [ ] Swipe left → Delete (confirm)
- [ ] Tap a card → navigate to `WordDetailScreen`
- [ ] FAB "+ Add word" → bottom sheet `AddWordSheet` (search from the dictionary)
- [ ] "Add random" button → dialog to choose topic/method/count
- [ ] "Start quiz" button → navigate to `QuizConfigScreen` with the list pre-selected

---

### F076 — Quiz Config Screen
```
Branch:    feature/mobile-quiz/quiz-config-screen
Assignee:  Nhut
Time:      3h
Depends on: F067
```
**Done when:**
- [ ] "Word scope" section: All / From date / To date / Date range (date pickers)
- [ ] "Topic" section: multi-select topic chips or "All"
- [ ] "Mode" section: 4 buttons (Standard / Timed / Challenge / Elimination) with descriptions
- [ ] "Question type" section: Word→Meaning / Meaning→Word / Description
- [ ] "Answer method" section: Multiple Choice / Exact Typing / AI Typing
- [ ] Cross-field validation: timed → time input, elimination → lives input
- [ ] "Start" button → call the API to create a session → navigate to QuizScreen

---

### F077 — Quiz Screen: Multiple Choice
```
Branch:    feature/mobile-quiz/quiz-screen-multiple-choice
Assignee:  Nhut
Time:      3h
Depends on: F076
```
**Done when:**
- [ ] Progress bar: question X/N
- [ ] Timer countdown (timed mode)
- [ ] Lives indicator 🩷 (elimination mode)
- [ ] Question text
- [ ] 4 answer buttons: default → after submit: correct (green), wrong (red) + correct highlighted (green)
- [ ] After submit: "Next" button (or auto-advance 1.5s)
- [ ] AI explanation (if AI typing mode)
- [ ] Abandon button (confirm dialog)

---

### F078 — Quiz Screen: Typing Mode
```
Branch:    feature/mobile-quiz/quiz-screen-typing
Assignee:  Nhut
Time:      2.5h
Depends on: F077
```
**Done when:**
- [ ] TextField with the keyboard up
- [ ] Submit button (or the enter key)
- [ ] AI typing: loading indicator while waiting for AI
- [ ] Exact match: case-insensitive, strips trailing punctuation
- [ ] AI mode: show `ai_score` as % + `ai_explanation` + `ai_suggestion`

---

### F079 — Quiz Result Screen & Wrong Words
```
Branch:    feature/mobile-quiz/quiz-result-wrong-words
Assignee:  Nhut
Time:      2.5h
Depends on: F077
```
**Done when:**
- [ ] Animated score reveal (circular progress)
- [ ] Stats: correct/total, accuracy %, streak, time
- [ ] Breakdown list: per question — icon ✓/✗, word, user_answer vs expected
- [ ] "Review wrong answers" button → WrongWordsScreen
- [ ] "Retry" button → QuizConfigScreen
- [ ] `WrongWordsScreen`: list of wrong words, swipe to remove from the wrong list, "Test again" button

---

### F080 — Progress Overview Screen
```
Branch:    feature/mobile-progress/progress-overview
Assignee:  Nhut
Time:      3h
Depends on: F067
```
**Done when:**
- [ ] Streak card: flame icon + day count, longest streak
- [ ] Accuracy gauge (last 7 days)
- [ ] Word counts: "X words learning", "Y words mastered"
- [ ] Sessions count (this month)
- [ ] Offline: show cached progress_summary + banner (TTL 15 minutes)

---

### F081 — Progress Charts Screen
```
Branch:    feature/mobile-progress/charts-screen
Assignee:  Nhut
Time:      2.5h
Depends on: F080
```
**Done when:**
- [ ] Line chart sessions/day (30 days) — using `fl_chart`
- [ ] Granularity dropdown: daily/weekly/monthly
- [ ] Mastery breakdown: horizontal bar chart per level 0–5
- [ ] Weakest words: top 10 list with a progress bar (accuracy rate)

---

### F082 — Settings & Profile Screen
```
Branch:    feature/mobile-settings/settings-profile
Assignee:  Nhut
Time:      2h
Depends on: F067
```
**Done when:**
- [ ] `SettingsScreen`: Language (VI/EN), Theme (Dark/Light), Notifications (stub), App version
- [ ] Language switch: save `app_locale`, restart the locale
- [ ] Theme switch: save `app_theme`, apply immediately
- [ ] `ProfileScreen`: avatar (placeholder), display_name edit, phone (masked), learning profile edit link
- [ ] Logout button (confirm dialog) → clear all storage → navigate /login

---

### F083 — Offline Handling & Connectivity
```
Branch:    feature/mobile-core/offline-handling
Assignee:  Nhut
Time:      2.5h
Depends on: F066, F065
```
**Done when:**
- [ ] `ConnectivityProvider` using a `connectivity_plus` stream → `isOnline: bool`
- [ ] `OfflineBanner` widget: shows a yellow banner "You are offline" when offline
- [ ] Dictionary: offline → use `word_cache_{id}` + search history (no API calls)
- [ ] Quiz: offline → disable "Start" + tooltip "Network connection required"
- [ ] Lists: offline → show cached lists, disable Add/Delete
- [ ] Cache warming after login: prefetch user_profile + lists + progress_summary

---

## PHASE 4 — Post-plan Additions

---

### F084 — Word-Deletion Notifications (backend)
```
Branch:    feature/notifications/word-deleted
Assignee:  Huy
Time:      3h
Depends on: F025 (word soft delete), F032 (list words), F039 (progress)
```
**What to do:** When an admin **soft-deletes** a vocabulary word, notify every end-user linked to that word (has it in a list or has learning progress). Vocabulary delete stays a **soft delete** (reversible via restore) — this only adds the user notifications. Requires a new `notifications` table (additive; run `database/notifications.sql`).

**Done when:** ✅ **COMPLETED 2026-07-07 (backend, build 0 errors, tests 312/312)**
- [x] New `notifications` table DDL in `database/notifications.sql` (additive, no existing table changed). `Notification` entity + `NotificationConfiguration` + `DbSet` (auto-applied via `ApplyConfigurationsFromAssembly`).
- [x] `INotificationRepository`/`NotificationRepository`: add-range, list-by-user (paged, unread filter), unread-count, mark-read, mark-all-read, and the affected-users query (`user_list_words` active ∪ `user_word_progress` by `word_id`).
- [x] `INotificationService`/`NotificationService`: `NotifyWordDeletedAsync(wordId)` builds one row per affected user (`type='word_deleted'`, `ref_type='word'`, `ref_id=wordId`, Vietnamese title/message) + the read APIs.
- [x] Hooked into `WordService.SoftDeleteAsync` (best-effort try/catch — a notify failure never fails the delete). Injected as an optional ctor param so existing tests/constructors stay valid.
- [x] `NotificationsController` (`[Authorize]`, end-user): `GET /api/notifications` (paged, `unreadOnly`), `GET /api/notifications/unread-count`, `PATCH /api/notifications/{id}/read`, `PATCH /api/notifications/read-all`.
- [x] Registered repo + service in `Program.cs` DI. `dotnet build` 0 errors; `dotnet test` **312/312**.
- [ ] **Operator step:** run `database/notifications.sql` on the MySQL database before using it.
- ⚠️ Notes: message stored in Vietnamese (learner audience) with a stable `type`/`ref_id` so mobile can localize/link; "mobile notification" here = **in-app stored notifications** (not FCM push — push is a separate infra task if wanted). Restore does not currently send a "word restored" notification (add later if desired).

---

### F085 — Mobile Notifications Screen
```
Branch:    feature/mobile-notifications/screen
Assignee:  Nhut
Time:      3h
Depends on: F084, F067
```
**What to do:** Show the F084 notifications to end-users in the Flutter app.

**Done when:** ✅ **COMPLETED 2026-07-07 (Flutter, analyze clean, tests 121/121)**
- [x] Home bell (`home_screen.dart` `_NotificationButton` → `ConsumerWidget`) shows the unread badge from `notificationsUnreadCountProvider` (`GET /api/notifications/unread-count`); tapping opens `/notifications` (was pointing at `/settings`). Badge is resilient (`.asData?.value ?? 0`; provider swallows errors → 0 when offline/not-ready).
- [x] `NotificationsScreen`: paginated list (`GET /api/notifications`, infinite scroll via scroll listener), unread rows tinted + dot, pull-to-refresh, empty/loading/error states.
- [x] Tap a notification → optimistic `markRead` (`PATCH /{id}/read`) + if `ref_type=='word'` navigate to the word detail.
- [x] "Đọc tất cả" (mark all read) app-bar action (`PATCH /api/notifications/read-all`), shown only when unread > 0.
- [x] Feature-first structure matching the app: `domain/app_notification.dart`, `data/notifications_repository.dart` (+ `NotificationsPage`), `application/notifications_state.dart` + `notifications_notifier.dart` (Riverpod), `presentation/notifications_screen.dart`. Endpoints added to `api_endpoints.dart`; `/notifications` route in `app_routes.dart` + `app_router.dart`. User-facing strings in Vietnamese.
- [x] Verify: `flutter analyze` → no issues; `flutter test` → 121/121 (added a `ProviderScope` + unread-count override to `home_screen_test.dart` since the bell is now a Consumer).
- [x] **Toolchain fixed:** `build_runner` failed under Dart 3.10 (`'dart compile' does not support build hooks`) because it AOT-compiles its build script. Fix = **`dart run build_runner build --force-jit`** (JIT mode skips the failing AOT step). `notifications_notifier.g.dart` is now **real generated code** (the earlier hand-written stub was replaced). Note: the generator names the class provider `notificationsProvider` (it strips the `Notifier` suffix).
- [ ] (Optional/later) real push via FCM — separate infra task (not needed for in-app notifications).

---

## Feature Count Summary

| Phase | Module | Features |
|---|---|---|
| Phase 0 | Shared Kernel | F001–F010 (10 features) |
| Phase 1 | M1 Auth | F011–F021 (11 features) |
| Phase 1 | M2 Dictionary | F022–F029 (8 features) |
| Phase 1 | M3 Learning List | F030–F034 (5 features) |
| Phase 1 | M4 Quiz | F035–F042 (8 features) |
| Phase 1 | M5 Progress | F043–F044 (2 features) |
| Phase 1 | M6 AI Grading | F045–F046 (2 features) |
| Phase 1 | M7 KNN | F047–F050 (4 features) |
| Phase 1 | M8 Media | F051–F052 (2 features) |
| Phase 1 | M9 Admin | F053–F054 (2 features) |
| Phase 2 | Dashboard | F055–F063 + F063A Admin Profile (10 features) |
| Phase 3 | Mobile | F064–F083 (20 features) |
| **Total** | | **84 features** |

---

## Total Time Estimate

| Assignee | Total features | Estimated hours |
|---|---|---|
| An (DevOps) | F001, F002, F009, F010, F021 | ~14h |
| Huy (Backend) | F003–F008, F011–F054 (excluding F002, F009–F010, F021) | ~120h |
| Tan (Dashboard) | F055–F063, F063A | ~27h |
| Nhut (Mobile) | F064–F083 | ~65h |

> Huy carries the heaviest load — An should help with some backend tasks in Phase 1 if possible.

---

## Quick Reference: Feature → Branch → Commit

```bash
# Full example workflow for F039 (SM-2 Algorithm)

git checkout dev
git pull origin dev
git checkout -b feature/quiz/sm2-algorithm

# code...

git add .
git commit -m "feat(quiz): implement SM-2 spaced repetition algorithm"
git commit -m "test(quiz): add unit tests for SM-2 edge cases"

git push origin feature/quiz/sm2-algorithm
# Create PR: [Quiz] SM-2 Spaced Repetition Algorithm → dev
```
