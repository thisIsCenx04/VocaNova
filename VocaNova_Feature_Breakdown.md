# VocaNova — Feature Breakdown (Commit-level)

> **Mục đích:** Mỗi feature = 1 branch + 1–3 commit rõ ràng.  
> **Team:** An (DevOps) · Huy (Backend) · Nhut (Mobile) · Tan (Dashboard)

---

## Git Workflow

### Branch Naming
```
feature/{module}/{tên-feature}
fix/{module}/{tên-lỗi}
chore/{mô-tả}

Ví dụ:
  feature/auth/jwt-token-service
  feature/quiz/sm2-algorithm
  feature/mobile-auth/login-screen
  fix/quiz/distractor-duplicate-bug
  chore/setup-cicd
```

### Commit Convention (Conventional Commits)
```
feat(module): mô tả ngắn gọn
fix(module): mô tả lỗi
test(module): thêm unit tests
refactor(module): không thay đổi behavior
chore: cấu hình, tooling

Ví dụ:
  feat(auth): add JWT token generation with 15-min expiry
  feat(quiz): implement SM-2 spaced repetition algorithm
  test(auth): add unit tests for OTP rate limiting
  fix(dictionary): normalize word_key before insert
```

### Branch Strategy
```
main          ← production only, merge sau demo
dev           ← integration, mọi PR merge vào đây
feature/*     ← làm việc hàng ngày
```

### PR Rule
- 1 feature = 1 PR vào `dev`
- PR title = tên feature (ví dụ: `[Auth] JWT Token Service`)
- Tự review trước khi assign cho leader
- Không merge nếu CI fail

---

## Legend

| Ký hiệu | Ý nghĩa |
|---|---|
| 🔴 | Bắt buộc xong trước các feature khác (blocker) |
| 🟡 | Phụ thuộc 1–2 feature trước |
| 🟢 | Có thể làm song song |
| ⏱ | Ước tính giờ (1 người) |
| 👤 | Assignee |

---

## PHASE 0 — Shared Kernel

> **Phải hoàn thành 100% trước khi bất kỳ module nào bắt đầu.**

---

### F001 — Solution Structure & NuGet Setup
```
Branch:    feature/setup/solution-structure
Assignee:  An
Thời gian: 2h
Phụ thuộc: —
```
**Làm gì:** Tạo solution với 3 project (`VocaNova.API`, `VocaNova.Dashboard`, `VocaNova.Tests`), cài NuGet packages cốt lõi, cấu hình `appsettings.json` mẫu.

**Done khi:**
- [ ] `VocaNova.sln` có 3 project, build thành công
- [ ] `appsettings.json` có section: `ConnectionStrings`, `JwtSettings`, `Redis`, `AiGrading`, `RateLimit`
- [ ] `.gitignore` đúng cho .NET + Flutter
- [ ] `README.md` có hướng dẫn setup local

---

### F002 — DbContext & Entity Scaffolding
```
Branch:    feature/setup/dbcontext-entities
Assignee:  An + Huy
Thời gian: 3h
Phụ thuộc: F001
```
**Làm gì:** Scaffold `VocaNovaDbContext` từ DB đã có (31 entity), tạo `IEntityTypeConfiguration<T>` riêng cho từng entity quan trọng.

**Done khi:**
- [ ] `dotnet ef dbcontext scaffold` chạy thành công
- [ ] Tất cả 31 entity có file trong `Infrastructure/Persistence/Configurations/`
- [ ] Index quan trọng: `words.word_key` (UNIQUE), `user_auth.phone` (UNIQUE), `user_auth.google_uid` (UNIQUE NULLABLE), `user_list_words(user_id, list_id, word_id)` (UNIQUE)
- [ ] `VocaNovaDbContext` inject vào DI trong `Program.cs`

---

### F003 — Result Pattern 🔴
```
Branch:    feature/shared/result-pattern
Assignee:  Huy
Thời gian: 1.5h
Phụ thuộc: F001
```
**Làm gì:** Implement `Result<T>` class và `PagedResult<T>`.

**Done khi:**
- [ ] `Result<T>` có static factories: `Ok()`, `Fail()`, `NotFound()`, `Conflict()`, `Forbidden()`
- [ ] `Result<T>` có property `IsSuccess`, `Value`, `Error`, `StatusCode`
- [ ] `PagedResult<T>` có `Items`, `Page`, `Limit`, `TotalItems`, `TotalPages`
- [ ] Unit test: verify status codes của từng factory

---

### F004 — API Response Formatter & Exception Middleware 🔴
```
Branch:    feature/shared/api-response-formatter
Assignee:  Huy
Thời gian: 2h
Phụ thuộc: F003
```
**Làm gì:** `ApiResponseFormatter` (wrap mọi response về shape thống nhất) + `ExceptionMiddleware`.

**Done khi:**
- [ ] Response shape: `{ success, data, message, errors[], pagination? }`
- [ ] `ExceptionMiddleware` bắt tất cả unhandled exception, log internal, trả 500 (không expose stack trace)
- [ ] Controller extension method: `OkResult(data)`, `CreatedResult(data)`, `ErrorResult(result)`
- [ ] Test bằng Swagger: endpoint lỗi trả đúng format

---

### F005 — Enums & Constants
```
Branch:    feature/shared/enums-constants
Assignee:  Huy
Thời gian: 1h
Phụ thuộc: F001
```
**Làm gì:** Khai báo toàn bộ enums dạng `const string` (vì DB lưu string).

**Done khi:**
- [ ] `QuestionType` (1, 2, 3)
- [ ] `TestMode` (standard, timed, challenge, elimination)
- [ ] `ScopeType` (all, date_range, start_date, end_date)
- [ ] `WordOrder` (newest, oldest, random)
- [ ] `AnswerMethod` (multiple_choice, exact_typing, ai_typing)
- [ ] `AddMethod` (manual, search, random_topic, random_synonym, random_antonym)
- [ ] `UserStatus` (active, locked, deleted)
- [ ] `AudioStatus` (pending, uploaded, tts_generated, missing, deleted)
- [ ] `AppSettings` static class cho các configurable values (MaxListsPerUser = 50, AiPassThreshold = 0.75...)

---

### F006 — Custom FluentValidation Validators 🔴
```
Branch:    feature/shared/custom-validators
Assignee:  Huy
Thời gian: 2h
Phụ thuộc: F001
```
**Làm gì:** Tập hợp validators tái sử dụng.

**Done khi:**
- [ ] `VietnamesePhoneValidator`: regex `^(0[3-9]\d{8})$`
- [ ] `StrongPasswordValidator`: ≥ 8 chars, ≥ 1 uppercase, ≥ 1 lowercase, ≥ 1 digit
- [ ] `CefrLevelValidator`: null hoặc ∈ {A1, A2, B1, B2, C1, C2}
- [ ] `EnumStringValidator<T>`: generic, validate string thuộc tập cho trước
- [ ] `DateRangeValidator`: from ≤ to, khoảng ≤ 365 ngày
- [ ] Đăng ký `FluentValidation` vào DI (`AddFluentValidationAutoValidation`)
- [ ] Unit test: mỗi validator 3 test cases (valid, invalid, edge)

---

### F007 — String & Queryable Extensions 🔴
```
Branch:    feature/shared/extensions
Assignee:  Huy
Thời gian: 1.5h
Phụ thuộc: F001
```
**Làm gì:** Utility extensions dùng xuyên suốt hệ thống.

**Done khi:**
- [ ] `NormalizeWord(this string s)`: `s.Trim().ToLowerInvariant()`
- [ ] `NormalizeAnswer(this string s)`: trim + bỏ dấu câu cuối + lowercase
- [ ] `ToPagedResultAsync<T>(this IQueryable<T>, int page, int limit)`: tự động Skip/Take + count, trả `PagedResult<T>`
- [ ] `MaskPhone(this string phone)`: `09x****xx90` format
- [ ] Unit test: NormalizeWord ("  Hello  " → "hello"), ToPagedResult (page 2, limit 5 → đúng offset)

---

### F008 — Global Query Filters (Soft Delete) 🔴
```
Branch:    feature/shared/global-query-filters
Assignee:  Huy
Thời gian: 1.5h
Phụ thuộc: F002
```
**Làm gì:** Cấu hình EF Core Global Query Filter cho tất cả entity có soft delete.

**Done khi:**
- [ ] Filter áp dụng cho: `UserList`, `UserListWord`, `Topic`, `Word`, `WordSense`, `WordExample`, `WordAudioAsset`
- [ ] Test: query `UserList` mặc định không thấy `status='deleted'`
- [ ] Test: `dbContext.UserLists.IgnoreQueryFilters()` thấy cả deleted
- [ ] Comment trong code: danh sách entity và filter logic

---

### F009 — Audit Log Middleware 🔴
```
Branch:    feature/shared/audit-log-middleware
Assignee:  Huy
Thời gian: 2h
Phụ thuộc: F002, F008
```
**Làm gì:** Middleware tự động ghi `audit_logs` cho mọi write request vào `/api/admin/*`.

**Done khi:**
- [ ] Intercept `POST/PUT/PATCH/DELETE /api/admin/*`
- [ ] Ghi: `user_id`, `action` (Create/Update/Delete), `entity_type`, `entity_id`, `ip_address`, `payload_before` (JSON), `payload_after` (JSON), `created_at`
- [ ] `payload_before/after` null nếu không applicable
- [ ] Audit log ghi bất đồng bộ (không block response)
- [ ] Test: sau khi gọi `PUT /api/admin/words/1`, có record trong `audit_logs`

---

### F010 — JWT Auth & Swagger Setup 🔴
```
Branch:    feature/setup/jwt-swagger-setup
Assignee:  An
Thời gian: 2h
Phụ thuộc: F001
```
**Làm gì:** Cấu hình JWT middleware + Swagger với Bearer auth.

**Done khi:**
- [ ] JWT middleware validate token, extract claims (user_id, role)
- [ ] `[Authorize]` attribute hoạt động
- [ ] `[Authorize(Roles = "admin,super_admin")]` hoạt động
- [ ] Swagger UI có "Authorize" button, sau khi nhập token có thể gọi protected endpoint
- [ ] Role-based policy: `Admin`, `SuperAdmin`, `User`

---

## PHASE 1 — Backend: Module Auth (M1)

---

### F011 — BCrypt Password Hashing Utility
```
Branch:    feature/auth/bcrypt-helper
Assignee:  Huy
Thời gian: 1h
Phụ thuộc: F001
```
**Done khi:**
- [ ] `PasswordHelper.Hash(password)`: BCrypt cost 12
- [ ] `PasswordHelper.Verify(password, hash)`: bool
- [ ] `TokenHelper.HashSha256(rawToken)`: dùng cho refresh token storage
- [ ] Unit test: hash + verify, wrong password returns false

---

### F012 — JWT Token Service
```
Branch:    feature/auth/jwt-token-service
Assignee:  Huy
Thời gian: 2h
Phụ thuộc: F010, F011
```
**Done khi:**
- [ ] `GenerateAccessToken(userId, role)`: JWT, expiry 15 phút, claims: `sub`, `role`, `jti`
- [ ] `GenerateRefreshToken()`: UUID v4 raw string
- [ ] `ValidateAccessToken(token)`: trả `ClaimsPrincipal?` (null nếu invalid/expired)
- [ ] Refresh token lưu DB = SHA256(raw) — raw chỉ trả về client
- [ ] Unit test: generate → validate → extract userId đúng

---

### F013 — Auth Repository & DTOs
```
Branch:    feature/auth/auth-dtos-repository
Assignee:  Huy
Thời gian: 1.5h
Phụ thuộc: F002
```
**Done khi:**
- [ ] DTOs: `RegisterRequest`, `LoginRequest`, `GoogleLoginRequest`, `TokenResponse`, `UserProfileDto`, `LearningProfileDto`
- [ ] Validators: `RegisterRequestValidator`, `LoginRequestValidator`, `OtpSendRequestValidator`
- [ ] `IAuthRepository` interface với các method: `FindByPhone`, `FindByGoogleUid`, `CreateUser`, `CreateRefreshToken`, `RevokeToken`

---

### F014 — Register Endpoint
```
Branch:    feature/auth/register
Assignee:  Huy
Thời gian: 2.5h
Phụ thuộc: F006, F011, F012, F013
```
**Done khi:**
- [ ] `POST /api/auth/register` hoạt động
- [ ] Validate: phone VN, password strong, display_name 2–150
- [ ] Check phone duplicate (chỉ check user `status != 'deleted'`)
- [ ] Tạo `users` + `user_auth` + `user_profiles` trong 1 transaction
- [ ] Sau đăng ký: trả `201 Created` với `TokenResponse`
- [ ] Unit test: happy path, phone dup, weak password

---

### F015 — Login Endpoint
```
Branch:    feature/auth/login
Assignee:  Huy
Thời gian: 2h
Phụ thuộc: F012, F013
```
**Done khi:**
- [ ] `POST /api/auth/login` hoạt động
- [ ] BCrypt verify password
- [ ] Check `users.status`: locked → 403, deleted → 401
- [ ] Tạo access token + refresh token, lưu `refresh_tokens`
- [ ] Trả `TokenResponse`
- [ ] Unit test: wrong password, locked user, deleted user, success

---

### F016 — Google OAuth Login
```
Branch:    feature/auth/google-oauth
Assignee:  Huy
Thời gian: 2.5h
Phụ thuộc: F012, F013
```
**Done khi:**
- [ ] `POST /api/auth/google` với `{ id_token }`
- [ ] Verify Google id_token qua `Google.Apis.Auth`
- [ ] Nếu `google_uid` đã tồn tại → login bình thường
- [ ] Nếu chưa → tạo user mới (phone = null)
- [ ] Nếu `google_email` trùng phone user khác → 409 Conflict
- [ ] Unit test: new user, existing user, email conflict

---

### F017 — Refresh Token Endpoint
```
Branch:    feature/auth/refresh-token
Assignee:  Huy
Thời gian: 1.5h
Phụ thuộc: F012, F013
```
**Done khi:**
- [ ] `POST /api/auth/refresh` với `{ refresh_token }`
- [ ] SHA256 hash input → tìm trong `refresh_tokens`
- [ ] Check `revoked_at` (null = còn dùng được) + `expires_at`
- [ ] Revoke token cũ, tạo token mới (token rotation)
- [ ] Unit test: expired token, revoked token, success

---

### F018 — Logout + Profile Endpoints
```
Branch:    feature/auth/logout-profile
Assignee:  Huy
Thời gian: 1.5h
Phụ thuộc: F012, F013
```
**Done khi:**
- [ ] `POST /api/auth/logout`: revoke current refresh token (from body hoặc header)
- [ ] `GET /api/auth/me`: trả `UserProfileDto` (kèm learning profile)
- [ ] `PUT /api/auth/me/profile`: cập nhật display_name, avatar_url
- [ ] `PUT /api/auth/me/learning-profile`: cập nhật 5 onboarding fields (validate FK)
- [ ] Redis cache `vocanova:user:{id}` TTL 5 phút; invalidate khi update profile

---

### F019 — OTP Service
```
Branch:    feature/auth/otp-service
Assignee:  Huy
Thời gian: 2.5h
Phụ thuộc: F002, F009
```
**Done khi:**
- [ ] `POST /api/auth/otp/send`: rate limit 1 OTP/phút/phone (check `created_at` của OTP gần nhất)
- [ ] Generate 6 chữ số ngẫu nhiên, TTL 5 phút
- [ ] `POST /api/auth/otp/verify`: tăng `verify_attempt_count`, check expired, check `is_used`, check max 5 lần
- [ ] Sau verify thành công: `is_used = true`
- [ ] SMS: stub log ra console (real Twilio dùng interface `ISmsProvider`)
- [ ] Unit test: expired OTP, max attempts (6th attempt → reject), already used

---

### F020 — Forgot & Reset Password
```
Branch:    feature/auth/forgot-reset-password
Assignee:  Huy
Thời gian: 2h
Phụ thuộc: F011, F019
```
**Done khi:**
- [ ] `POST /api/auth/forgot-password`: gửi OTP với `purpose = 'reset'`
- [ ] `POST /api/auth/reset-password`: verify OTP → hash new password → update `user_auth`
- [ ] OTP chỉ dùng được 1 lần (`is_used = true` ngay sau reset thành công)
- [ ] Unit test: reset với OTP đúng, reset với OTP expired

---

### F021 — Auth Rate Limiting
```
Branch:    feature/auth/rate-limiting
Assignee:  An
Thời gian: 1.5h
Phụ thuộc: F009, F019
```
**Done khi:**
- [ ] `POST /api/auth/otp/send`: 1 req/phút/IP → 429
- [ ] `POST /api/auth/login`: 10 req/phút/IP → 429
- [ ] Response 429 có `Retry-After` header
- [ ] Test: 11 lần login liên tiếp → lần 11 nhận 429

---

## PHASE 1 — Backend: Module Dictionary (M2)

---

### F022 — Word Search Endpoint
```
Branch:    feature/dictionary/word-search
Assignee:  Huy
Thời gian: 2.5h
Phụ thuộc: F007, F008
```
**Done khi:**
- [ ] `GET /api/words?q=&page=&limit=&cefr=&topicId=&isPhrase=` (anonymous)
- [ ] Query dùng `word_key LIKE {NormalizeWord(q)}%`
- [ ] Filter: `cefr_level`, `topic_id` (JOIN `word_topics`), `is_phrase`
- [ ] Trả `PagedResult<WordSummaryDto>`: word_id, word, phonetic, cefr, primary_meaning (sense[0].vi), image_url
- [ ] Cache: `vocanova:word-search:{query}:{page}:{filters}` TTL 5 phút
- [ ] Unit test: search "run", filter by topic, empty results

---

### F023 — Word Detail Endpoint
```
Branch:    feature/dictionary/word-detail
Assignee:  Huy
Thời gian: 2h
Phụ thuộc: F022
```
**Done khi:**
- [ ] `GET /api/words/{id}` (anonymous)
- [ ] Eager load: senses → examples, relations (với `related_word_id` nullable), audio (filter status), derived_forms, idioms, topics
- [ ] `RelationDto.linked_word_id` = null nếu từ chưa có trong DB
- [ ] Audio: chỉ trả URL nếu `status IN ('uploaded', 'tts_generated')`
- [ ] Cache: `vocanova:word:{id}` TTL 30 phút
- [ ] 404 nếu word bị soft delete (Global Query Filter xử lý)

---

### F024 — Topics Endpoints
```
Branch:    feature/dictionary/topics
Assignee:  Huy
Thời gian: 1.5h
Phụ thuộc: F008
```
**Done khi:**
- [ ] `GET /api/topics`: danh sách topics active kèm `word_count`
- [ ] `GET /api/topics/{id}/words?page=&limit=`: từ theo topic (paginated)
- [ ] Cache: `vocanova:topics` TTL 60 phút
- [ ] Cache: `vocanova:topic-words:{id}:{page}` TTL 10 phút

---

### F025 — Admin Word CRUD
```
Branch:    feature/dictionary/admin-word-crud
Assignee:  Huy
Thời gian: 3h
Phụ thuộc: F022, F009
```
**Done khi:**
- [ ] `POST /api/admin/words`: tạo từ mới, `word_key` auto = `word.NormalizeWord()`
- [ ] `PUT /api/admin/words/{id}`: cập nhật metadata từ
- [ ] Validator: `CreateWordRequest` (word length, cefr valid)
- [ ] Invalidate cache word khi update
- [ ] Audit log ghi qua middleware
- [ ] Unit test: create success, create duplicate word_key → 409

---

### F026 — Admin Soft Delete + Restore Word
```
Branch:    feature/dictionary/admin-word-softdelete
Assignee:  Huy
Thời gian: 1.5h
Phụ thuộc: F025
```
**Done khi:**
- [ ] `DELETE /api/admin/words/{id}`: SuperAdmin only, `words.status = 'deleted'`
- [ ] `PATCH /api/admin/words/{id}/restore`: SuperAdmin only, `status = 'active'`
- [ ] Endpoint xóa dùng `.IgnoreQueryFilters()` để tìm cả record đã xóa
- [ ] Invalidate cache sau soft delete/restore

---

### F027 — Admin Sense CRUD (Cascade Soft Delete)
```
Branch:    feature/dictionary/admin-sense-crud
Assignee:  Huy
Thời gian: 2.5h
Phụ thuộc: F025
```
**Done khi:**
- [ ] `POST /api/admin/words/{id}/senses`: thêm sense mới
- [ ] `PUT /api/admin/words/{id}/senses/{senseId}`: cập nhật sense
- [ ] `DELETE /api/admin/words/{id}/senses/{senseId}`: soft delete sense (`is_deleted=1`) + cascade soft delete tất cả `word_examples` của sense đó
- [ ] `PATCH /api/admin/words/{id}/senses/{senseId}/restore`: restore sense (KHÔNG tự restore examples)
- [ ] Unit test: delete sense → examples bị cascade deleted; restore sense → examples vẫn deleted

---

### F028 — Admin Topic CRUD (Với Guard)
```
Branch:    feature/dictionary/admin-topic-crud
Assignee:  Huy
Thời gian: 2h
Phụ thuộc: F024
```
**Done khi:**
- [ ] `POST /api/admin/topics`, `PUT /api/admin/topics/{id}`
- [ ] `DELETE /api/admin/topics/{id}`: **chặn** nếu còn `word_topics` với word `status='active'` → 409
- [ ] `PATCH /api/admin/topics/{id}/restore`
- [ ] Unit test: delete topic có word → 409; delete topic không word → success

---

### F029 — Bulk CSV Import Words
```
Branch:    feature/dictionary/bulk-import-csv
Assignee:  Huy
Thời gian: 3h
Phụ thuộc: F025, F027
```
**Done khi:**
- [ ] `POST /api/admin/words/import` (multipart/form-data)
- [ ] CSV format: `word, cefr_level, phonetic_uk, phonetic_us, word_class, english_definition, vietnamese_meaning`
- [ ] Mỗi row validate độc lập — row lỗi → ghi errors[], **không dừng import**
- [ ] Nếu `word_key` đã tồn tại → thêm sense mới vào word đó (không tạo duplicate word)
- [ ] Response: `{ imported_words, imported_senses, skipped, errors: [{row, column, message}] }`
- [ ] Unit test: file 10 row (3 lỗi) → import 7, errors có đúng 3 entries

---

## PHASE 1 — Backend: Module Learning List (M3)

---

### F030 — Get & Create List
```
Branch:    feature/list/get-create-list
Assignee:  Huy
Thời gian: 2h
Phụ thuộc: F007, F008
```
**Done khi:**
- [ ] `GET /api/lists`: danh sách lists của user hiện tại (status='active')
- [ ] `POST /api/lists`: tạo list, check tối đa 50 lists, check tên không trùng (case-insensitive)
- [ ] `UserListDto`: list_id, list_name, word_count, created_at
- [ ] Cache: `vocanova:user-lists:{user_id}` TTL 10 phút
- [ ] Unit test: create OK, create khi đã 50 lists → 400, create tên trùng → 409

---

### F031 — Update & Delete List
```
Branch:    feature/list/update-delete-list
Assignee:  Huy
Thời gian: 1.5h
Phụ thuộc: F030
```
**Done khi:**
- [ ] `PUT /api/lists/{id}`: đổi tên, check duplicate
- [ ] `DELETE /api/lists/{id}`: soft delete list + **cascade** soft delete toàn bộ `user_list_words` của list
- [ ] Verify ownership: user chỉ được xóa list của mình → 403 nếu sai
- [ ] `user_word_progress` KHÔNG bị ảnh hưởng
- [ ] Unit test: cascade soft delete, verify 10 words đều deleted sau khi list deleted

---

### F032 — List Words: Get & Add Manual
```
Branch:    feature/list/words-get-add
Assignee:  Huy
Thời gian: 2.5h
Phụ thuộc: F023, F031
```
**Done khi:**
- [ ] `GET /api/lists/{id}/words?page=`: kèm `correct_count`, `wrong_count` từ `user_list_word_stats`
- [ ] `POST /api/lists/{id}/words`: add từ (body: `word_id`, `add_method`, `note`)
- [ ] Kiểm tra word tồn tại (404 nếu không)
- [ ] Nếu từ đã active trong list → 409; nếu từ đã deleted → restore (`status='active'`)
- [ ] `note` tối đa 1000 ký tự
- [ ] Unit test: add dup (active), add dup (deleted → restore), add word không tồn tại

---

### F033 — List Words: Add Random
```
Branch:    feature/list/words-add-random
Assignee:  Huy
Thời gian: 2h
Phụ thuộc: F032
```
**Done khi:**
- [ ] `POST /api/lists/{id}/words/random` (body: topic_id?, count, method)
- [ ] `random_topic`: lấy ngẫu nhiên `count` từ theo topic (trừ từ đã có trong list)
- [ ] `random_synonym` / `random_antonym`: chỉ lấy từ `word_relations` có `is_quiz_eligible=true`
- [ ] `count` tối đa 50
- [ ] Nếu không đủ từ → thêm bao nhiêu có bấy nhiêu, không báo lỗi
- [ ] Unit test: random_topic filter (exclude existing), count limit

---

### F034 — List Words: Remove & Note
```
Branch:    feature/list/words-remove-note
Assignee:  Huy
Thời gian: 1h
Phụ thuộc: F032
```
**Done khi:**
- [ ] `DELETE /api/lists/{id}/words/{wordId}`: soft delete (`status='deleted'`)
- [ ] `PATCH /api/lists/{id}/words/{wordId}/note`: cập nhật note
- [ ] `user_word_progress` KHÔNG bị ảnh hưởng khi xóa từ

---

## PHASE 1 — Backend: Module Quiz (M4)

---

### F035 — Quiz Word Pool Builder
```
Branch:    feature/quiz/word-pool-builder
Assignee:  Huy
Thời gian: 3h
Phụ thuộc: F032
```
**Done khi:**
- [ ] `QuizSessionBuilder.BuildPoolAsync(userId, request)`:
  - `scope_type = 'all'`: toàn bộ `user_list_words` active
  - `scope_type = 'date_range'`: filter theo `added_at` trong range
  - `scope_type = 'start_date'`: từ ngày đó trở đi
  - `scope_type = 'end_date'`: đến ngày đó
  - Optional: filter theo `topic_ids`
- [ ] Apply `word_order`: newest (sort added_at DESC), oldest (ASC), random (shuffle)
- [ ] Apply `word_limit` nếu có
- [ ] Pool size ≥ 4 nếu `multiple_choice` (cần distractor) → nếu không đủ: `Result.Fail("Không đủ từ để tạo bài kiểm tra")`
- [ ] Unit test: scope all, scope date_range, pool < 4 với multiple choice

---

### F036 — Question Builder & Distractor Generator
```
Branch:    feature/quiz/question-builder
Assignee:  Huy
Thời gian: 2.5h
Phụ thuộc: F035
```
**Done khi:**
- [ ] `BuildQuestion(wordId, questionType)`:
  - Type 1 (WordToMeaning): `display_content = word`, `expected_answer = vietnamese_meaning`
  - Type 2 (MeaningToWord): `display_content = vietnamese_meaning`, `expected_answer = word`
  - Type 3 (Description): `display_content = english_definition`, `expected_answer = word`
- [ ] Distractor generation: 3 từ cùng topic hoặc cùng word_class, KHÔNG trùng expected_answer
- [ ] `choices[]` shuffle (expected answer ở vị trí random)
- [ ] Unit test: distractor không trùng answer, choices có đúng 4 phần tử

---

### F037 — Create Quiz Session Endpoint
```
Branch:    feature/quiz/create-session
Assignee:  Huy
Thời gian: 2h
Phụ thuộc: F035, F036
```
**Done khi:**
- [ ] `POST /api/quiz/sessions`:
  - Validate `CreateSessionRequest` (mode+lives, mode+time cross-field)
  - Build pool → validate
  - Lưu `test_sessions` + `test_session_topics`
  - Trả `QuizSessionDto` + câu hỏi đầu tiên (`QuestionDto`)
- [ ] Session `status = 'in_progress'`
- [ ] Unit test: timed mode thiếu time_limit → 400, elimination mode thiếu lives → 400

---

### F038 — Exact Typing & Multiple Choice Grader
```
Branch:    feature/quiz/exact-multiple-grader
Assignee:  Huy
Thời gian: 2h
Phụ thuộc: F007, F037
```
**Done khi:**
- [ ] `IAnswerGrader` interface với `GradeAsync(answer, expected, acceptedAnswers[]) → GradeResult`
- [ ] `ExactTypingGrader`: cả 2 phía dùng `NormalizeAnswer()` → so sánh string
- [ ] `MultipleChoiceGrader`: so sánh trực tiếp (không normalize)
- [ ] `accepted_answers` (JSON array): nếu user_answer khớp bất kỳ → correct
- [ ] Unit test: exact_typing case-insensitive, trailing punctuation ignored

---

### F039 — SM-2 SRS Algorithm
```
Branch:    feature/quiz/sm2-algorithm
Assignee:  Huy
Thời gian: 2.5h
Phụ thuộc: F002
```
**Done khi:**
- [ ] `SrsService.UpdateProgressAsync(userId, wordId, isCorrect)`:
  - Upsert `user_word_progress` (insert nếu chưa có, update nếu có)
  - Đúng: tính ease_factor, interval mới, tăng consecutive_correct
  - Sai: reset interval=1, consecutive_correct=0, `is_in_wrong_list=true`
  - Mastery: tăng khi `consecutive_correct >= 5`
- [ ] `next_review_at` cập nhật theo interval
- [ ] Unit test:
  - Đúng 5 lần liên tiếp → `mastery_level` tăng 1
  - Sai 1 lần sau 4 lần đúng → `consecutive_correct = 0`
  - ease_factor không xuống dưới 1.3

---

### F040 — Submit Answer Endpoint
```
Branch:    feature/quiz/submit-answer
Assignee:  Huy
Thời gian: 2.5h
Phụ thuộc: F038, F039
```
**Done khi:**
- [ ] `POST /api/quiz/sessions/{id}/answer` (body: word_id, user_answer)
- [ ] Route tới đúng grader theo `test_sessions.answer_method`
- [ ] AI_typing → gọi `AiGradingService` (Module 6, sẽ stub trước)
- [ ] Sau grading: upsert `test_answers`, update SM-2, update session stats
- [ ] Trả `AnswerResultDto` + `next_question` (null nếu đây là câu cuối)
- [ ] Nếu session `status != 'in_progress'` → 409

---

### F041 — Finish Session & Result
```
Branch:    feature/quiz/finish-result
Assignee:  Huy
Thời gian: 2h
Phụ thuộc: F040
```
**Done khi:**
- [ ] `POST /api/quiz/sessions/{id}/finish`: set `status='abandoned'`, tính partial stats
- [ ] `GET /api/quiz/sessions/{id}/result`: load full session + tất cả `test_answers`
- [ ] Tính: `accuracy`, `duration_sec` (ended_at - started_at), `max_streak`, `score`
- [ ] Session auto-complete khi submit câu cuối (không cần gọi finish)

---

### F042 — Quiz History & Wrong Words
```
Branch:    feature/quiz/history-wrong-words
Assignee:  Huy
Thời gian: 1.5h
Phụ thuộc: F041
```
**Done khi:**
- [ ] `GET /api/quiz/history?page=`: lịch sử sessions (paginated, newest first)
- [ ] `GET /api/quiz/wrong-words?page=`: từ `is_in_wrong_list=true`, sort wrong_count DESC
- [ ] `DELETE /api/quiz/wrong-words/{wordId}`: set `is_in_wrong_list=false`, KHÔNG xóa record
- [ ] Unit test: wrong-words chỉ hiện từ có flag true

---

## PHASE 1 — Backend: Module Progress (M5)

---

### F043 — Progress Summary Endpoint
```
Branch:    feature/progress/summary
Assignee:  Huy
Thời gian: 2.5h
Phụ thuộc: F041
```
**Done khi:**
- [ ] `GET /api/progress/summary`
- [ ] Streak: đếm ngày liên tiếp có session (check gap ngày)
- [ ] Accuracy 7 ngày: correct/total từ `test_answers` trong 7 ngày gần nhất
- [ ] Total words in progress: COUNT DISTINCT word_id trong `user_word_progress`
- [ ] Cache: `vocanova:progress-summary:{user_id}` TTL 15 phút
- [ ] Unit test: streak với gap phá streak, streak ngày hôm nay chưa làm

---

### F044 — Progress Chart & Mastery
```
Branch:    feature/progress/chart-mastery
Assignee:  Huy
Thời gian: 2h
Phụ thuộc: F043
```
**Done khi:**
- [ ] `GET /api/progress/chart?granularity=daily|weekly|monthly`
- [ ] daily (30 ngày), weekly (12 tuần), monthly (6 tháng)
- [ ] `GET /api/progress/mastery-breakdown`: COUNT per mastery_level (0–5)
- [ ] `GET /api/progress/weakest-words?limit=20`: `is_in_wrong_list=true`, sort wrong_count DESC
- [ ] `GET /api/progress/words/{wordId}`: chi tiết progress 1 từ

---

## PHASE 1 — Backend: Module AI Grading (M6)

---

### F045 — AI Grading Cache Lookup
```
Branch:    feature/ai-grading/cache-lookup
Assignee:  Huy
Thời gian: 2h
Phụ thuộc: F007, F002
```
**Done khi:**
- [ ] `IAiGradingService` interface: `GradeAsync(wordId, questionType, userAnswer, expectedAnswer) → AiGradingResult`
- [ ] `cache_key = SHA256("{wordId}:{questionType}:{NormalizeAnswer(userAnswer)}")`
- [ ] Cache hit: `expires_at > NOW()` → tăng `hit_count`, trả kết quả
- [ ] Unit test: cache hit → không gọi API

---

### F046 — Gemini API Integration
```
Branch:    feature/ai-grading/gemini-integration
Assignee:  Huy
Thời gian: 2.5h
Phụ thuộc: F045
```
**Done khi:**
- [ ] `IGeminiClient` interface (dễ mock trong test)
- [ ] Prompt template: trả JSON `{ score: float, explanation: string, suggestion: string }`
- [ ] Parse response, validate `score` trong [0.0, 1.0]
- [ ] Cache miss: gọi API → lưu cache TTL 7 ngày
- [ ] Fallback nếu API fail hoặc parse error: `{ score: 0.0, explanation: "AI không khả dụng" }`
- [ ] Gắn vào F040 (`AiTypingGrader` dùng service này)
- [ ] Unit test: API fail → fallback score 0.0

---

## PHASE 1 — Backend: Module KNN (M7)

> **Hai luồng KNN song song trong hệ thống:**
>
> | Luồng | Mục tiêu | Feature vector | Output | Trigger |
> |---|---|---|---|---|
> | **KNN Onboarding** (F048) | User mới chọn chủ đề học | Profile 5 chiều one-hot từ `user_learning_profiles` | Topic gợi ý | On-demand sau onboarding |
> | **KNN Learning** (F049, FE-57) | Gợi ý từ vựng theo hành vi | Topic accuracy N chiều từ `test_answers` | Word gợi ý | Background job 24h |
>
> F050 cung cấp admin controls (FE-19) để F063 Dashboard gọi.

---

### F047 — KNN: Configuration Setup & Existing Table Verification
```
Branch:    feature/knn/knn-config
Assignee:  Huy
Thời gian: 1.5h
Phụ thuộc: F002
```
**Làm gì:** Xác nhận các bảng hiện có đủ để chạy cả hai luồng KNN và thiết lập config. KHÔNG tạo bảng mới vì schema đã chốt.

**Done khi:**
- [ ] Xác nhận EF entity + `IEntityTypeConfiguration` đã map đúng các bảng nguồn:
  - `user_learning_profiles` (age_range_id, region_id, occupation_id, education_level_id, learning_purpose_id, created_at, updated_at)
  - `user_topic_preferences` (user_id, topic_id, source, status, created_at) — `source` hỗ trợ giá trị `'knn_suggested'`
  - `age_ranges`, `regions`, `occupations`, `education_levels`, `learning_purposes` (bảng lookup onboarding)
  - `test_answers` (session_id, word_id, is_correct) — dùng cho topic accuracy vector
  - `word_topics` (word_id, topic_id) — dùng để JOIN topic từ test_answers
  - `user_word_progress` (user_id, word_id, mastery_level, srs_interval, ease_factor...)
  - `user_list_words` (user_id, list_id, word_id, status)
- [ ] Word recommendation (KNN Learning) lưu kết quả vào **Redis** thay vì DB: key `vocanova:knn-words:{user_id}`, value JSON array `WordRecommendationItem[]`, TTL 24h. KHÔNG tạo bảng `recommendations`.
- [ ] KNN config bind qua `KnnOptions` từ **`.env`/environment configuration** — KHÔNG hardcode trong `appsettings.json`, KHÔNG tạo bảng `knn_model_configs`:
  ```json
  "Knn": {
    "Onboarding": { "KValue": 5, "DefaultTopicLimit": 10, "MinSimilarity": 0.1, "CacheTtlMinutes": 30 },
    "Learning":   { "KValue": 5, "MinSessions": 5, "MinSimilarity": 0.1, "RecommendationCount": 50, "RebuildIntervalHours": 24, "CacheTtlMinutes": 60 }
  }
  ```
- [ ] `KnnOptions` strongly-typed class, inject qua `IOptions<KnnOptions>`
- [ ] `WordRecommendationItem` record: `{ WordId, Word, PhoneticUk, PrimaryMeaning, ImageUrl, CefrLevel, Score }`
- [ ] Smoke test: đọc được `user_learning_profiles` + `user_topic_preferences` của seed users

---

### F048 — KNN Onboarding: Profile-Based Topic Recommendation
```
Branch:    feature/knn/onboarding-topic-recommendation
Assignee:  Huy
Thời gian: 3.5h
Phụ thuộc: F047, F024 (topics list)
```
**Làm gì:** Cold-start KNN — dùng hồ sơ học (age/region/occupation/education/purpose) để gợi ý topic cho user mới chưa có test data. Kích hoạt ngay sau khi user hoàn thành onboarding (F071).

**Done khi:**
- [ ] `KnnOnboardingService.ComputeProfileVectorAsync(userId)`:
  - One-hot encode 5 nhóm: `age_ranges`, `regions`, `occupations`, `education_levels`, `learning_purposes`
  - Số chiều = tổng số records active trong 5 bảng lookup
  - User thiếu nhóm nào → toàn bộ chiều của nhóm đó = 0.0, không throw lỗi
  - Chỉ encode records có `status = 'active'`
- [ ] `KnnOnboardingService.CosineSimilarity(double[] a, double[] b)`:
  - Trả 0.0 nếu cả hai đều all-zero (tránh division by zero)
  - Unit test: identical → 1.0; zero vector → 0.0; orthogonal → 0.0
- [ ] `KnnOnboardingService.RecommendTopicsAsync(userId, limit)`:
  - Tính vector user hiện tại; nếu all-zero → nhảy thẳng vào fallback
  - Tính cosine similarity với tất cả users có ít nhất 1 chiều profile ≠ 0
  - Lấy K neighbors gần nhất (từ `KnnOptions.Onboarding.KValue`), filter `similarity < MinSimilarity`
  - Tổng hợp topics từ `user_topic_preferences` của neighbors (`status='active'`, `source IN ('user_selected','onboarding')`)
  - Score mỗi topic = `SUM(similarity_của_neighbor_có_topic_đó)`
  - Loại topics user hiện tại đã có `status='active'` trong `user_topic_preferences`
  - **Fallback** khi không đủ neighbors hoặc vector all-zero: trả top N topics theo tần suất xuất hiện nhiều nhất trong `user_topic_preferences` toàn hệ thống
  - Trả `List<TopicRecommendationDto>`: topic_id, topic_name, topic_name_vi, icon, word_count, recommendation_score
- [ ] `GET /api/recommendations/topics?limit=10`:
  - Cache: `vocanova:knn-topics:{user_id}` TTL `KnnOptions.Onboarding.CacheTtlMinutes`
  - Trả `[]` nếu user chưa có profile (KHÔNG phải lỗi)
- [ ] `POST /api/recommendations/topics/{topicId}/accept`:
  - Upsert `user_topic_preferences(user_id, topic_id, source='knn_suggested', status='active')`
  - Nếu đã có record → cập nhật `source='knn_suggested'`
  - Invalidate cache `vocanova:knn-topics:{user_id}`
- [ ] Invalidate cache `vocanova:knn-topics:{user_id}` khi `user_learning_profiles` của user thay đổi (gọi từ F018)
- [ ] Unit test: user thiếu profile → fallback trả kết quả hợp lệ; recommendation loại đúng topic user đã chọn; accept → invalidate cache

---

### F049 — KNN Learning: Behavior-Based Word Recommendation (FE-57)
```
Branch:    feature/knn/learning-word-recommendation
Assignee:  Huy
Thời gian: 4.5h
Phụ thuộc: F047, F041 (test_answers tồn tại), F044 (user_word_progress với mastery_level)
```
**Làm gì:** Behavior-based KNN (FE-57) — topic accuracy từ lịch sử làm bài để gợi ý từ vựng. Kết quả lưu Redis (không tạo bảng mới). Background job ghi vào Redis, API đọc từ Redis.

**Done khi:**
- [ ] `KnnLearningService.ComputeTopicAccuracyVectorAsync(userId)`:
  - Với mỗi topic active trong `topics` table: `accuracy_i = SUM(ta.is_correct) / COUNT(ta.answer_id)` từ `test_answers ta` JOIN `test_sessions ts` JOIN `word_topics wt` WHERE `ts.user_id = userId AND wt.topic_id = i`
  - Topic user chưa có data → accuracy = 0.0 (không phải null)
  - User có < `KnnOptions.Learning.MinSessions` sessions → trả `Result.Fail`, KHÔNG throw exception
- [ ] `KnnMathHelper.CosineSimilarity(double[] a, double[] b)`: shared utility, trả 0.0 nếu any vector all-zero
- [ ] `KnnLearningService.FindKNearestAsync(userId, vector, k)`:
  - Load eligible users (≥ MinSessions, status='active', loại chính user đó)
  - Tính cosine similarity với từng user; filter `similarity < MinSimilarity`, lấy top K
- [ ] `KnnLearningService.GenerateWordRecommendationsAsync(userId)`:
  - Gọi `ComputeTopicAccuracyVectorAsync` → nếu Fail → log và return (không crash job)
  - Với mỗi neighbor: lấy word_ids có `mastery_level >= 3` từ `user_word_progress`
  - Score: `score_word = SUM(similarity_của_neighbor_có_word)`
  - Loại word_ids user đã có trong `user_list_words` (`status='active'`)
  - Sort DESC, lấy top `KnnOptions.Learning.RecommendationCount`
  - **Lưu vào Redis** (không phải DB): key `vocanova:knn-words:{userId}`, value = JSON serialize `List<WordRecommendationItem>`, TTL = `KnnOptions.Learning.RebuildIntervalHours` giờ
- [ ] `GET /api/recommendations/words?limit=10`:
  - Đọc từ Redis key `vocanova:knn-words:{userId}`
  - Nếu Redis miss → trả `[]` (không phải 404, không tự tính lại)
  - Deserialize → JOIN `words` table để lấy thông tin mới nhất (phonetic, image_url)
  - Trả `WordRecommendationDto[]`: word_id, word, phonetic_uk, primary_meaning, image_url, cefr_level, score
- [ ] Unit test:
  - User < MinSessions → GenerateWordRecommendationsAsync return sớm, Redis key không được ghi
  - CosineSimilarity zero vector → 0.0, không exception
  - Neighbor có word user đã sở hữu → bị loại
  - Sau Generate → Redis có key đúng, TTL đúng

---

### F050 — KNN Background Job & Onboarding Lookup Admin (FE-19)
```
Branch:    feature/knn/background-job-admin
Assignee:  Huy
Thời gian: 3h
Phụ thuộc: F049, F048
```
**Làm gì:** IHostedService rebuild word recommendations định kỳ + admin APIs quản lý 5 bảng lookup onboarding dùng cho KNN. Config thuật toán KNN vẫn đọc từ `KnnOptions` qua `.env`/configuration; KHÔNG tạo hoặc CRUD bảng `knn_model_configs`.

**Done khi:**
- [ ] `KnnWordRecommendationJob : IHostedService`:
  - `PeriodicTimer` mỗi `KnnOptions.Learning.RebuildIntervalHours` giờ
  - Lấy tất cả eligible users (≥ MinSessions, `status='active'`)
  - Gọi `KnnLearningService.GenerateWordRecommendationsAsync(userId)` tuần tự (không parallel để tránh DB overload)
  - Lỗi 1 user KHÔNG dừng toàn bộ job (try-catch per user, log riêng)
  - Sau khi xong: lưu timestamp vào Redis `vocanova:knn-last-rebuild` (TTL vô hạn)
  - Log: số users xử lý / bỏ qua / lỗi, tổng thời gian chạy
- [ ] Admin: `GET /api/admin/knn/config` — trả config hiện tại từ `IOptions<KnnOptions>`:
  - onboarding: `k_value`, `default_topic_limit`, `min_similarity`, `cache_ttl_minutes`
  - learning: `k_value`, `min_sessions`, `min_similarity`, `recommendation_count`, `rebuild_interval_hours`, `cache_ttl_minutes`
  - read-only; muốn đổi config thì sửa `.env`/deployment config rồi restart app
- [ ] Admin lookup APIs cho KNN onboarding, CRUD/soft delete/restore trên các bảng hiện có:
  - `GET/POST/PUT/DELETE/PATCH restore /api/admin/knn/age-ranges`
  - `GET/POST/PUT/DELETE/PATCH restore /api/admin/knn/regions`
  - `GET/POST/PUT/DELETE/PATCH restore /api/admin/knn/occupations`
  - `GET/POST/PUT/DELETE/PATCH restore /api/admin/knn/education-levels`
  - `GET/POST/PUT/DELETE/PATCH restore /api/admin/knn/learning-purposes`
- [ ] Mỗi lookup management phải có đủ cấu trúc backend chuẩn:
  - DTO response riêng, request create/update riêng, query filter riêng.
  - FluentValidation validator cho create/update/query.
  - Repository interface + implementation.
  - Service interface + implementation, xử lý rule nghiệp vụ và `Result<T>`.
  - Controller admin endpoint dùng `ControllerResultExtensions`.
  - Đăng ký DI trong `Program.cs`.
  - Unit tests cho validator, service/repository, và endpoint behavior quan trọng.
- [ ] `GET` list cho mỗi lookup:
  - Support `page`, `limit`, `q`, `status`, `includeDeleted`.
  - Pagination theo `AppSettings.DefaultPageLimit`/`MaxPageLimit`.
  - Search case-insensitive theo `name`; riêng regions search thêm `code`.
  - Sort ổn định: `display_order`, `name`, id nếu table có display_order; còn lại `name`, id.
  - Mặc định chỉ trả `status='active'`; `includeDeleted=true` chỉ admin/super_admin dùng để quản lý restore.
- [ ] `GET {id}` detail cho mỗi lookup:
  - Trả 404 nếu không tồn tại.
  - Mặc định không trả deleted trừ khi `includeDeleted=true`.
- [ ] `POST` create cho mỗi lookup:
  - Trim input.
  - Set `status='active'`.
  - Chặn duplicate active `name` trong cùng lookup table, case-insensitive.
  - Regions chặn duplicate `code`, case-insensitive, kể cả deleted nếu DB unique không cho trùng.
  - Trả 409 khi duplicate, 400 khi validation fail.
- [ ] `PUT {id}` update cho mỗi lookup:
  - Không cho update record đang deleted, trừ khi restore trước.
  - Chặn duplicate active `name` khi đổi tên.
  - Regions chặn duplicate `code` khi đổi code.
  - Không cho regions chọn chính nó hoặc descendant làm `parent_id`.
  - Trả 404 nếu không tồn tại, 409 nếu duplicate/conflict.
- [ ] Lookup delete rule:
  - Soft delete bằng `status='deleted'`.
  - Không xóa cứng lookup row vì `user_learning_profiles` đang tham chiếu FK.
  - Delete idempotency: nếu đã deleted thì trả 404 hoặc conflict theo pattern admin hiện có, nhưng không mutate dữ liệu.
  - Có thể delete lookup đang được user sử dụng vì đây là soft delete; vector KNN chỉ encode records active, user đang tham chiếu deleted lookup sẽ được xem là missing dimension.
  - Không cần migration; dùng schema hiện có.
- [ ] `PATCH {id}/restore` rule:
  - Restore bằng `status='active'`.
  - Chặn restore nếu sẽ tạo duplicate active `name` hoặc duplicate `code` với regions.
  - Trả 404 nếu id không tồn tại, 409 nếu conflict.
- [ ] Lookup create/update validators:
  - Common `name`: required, trim, max theo EF mapping (`50` cho age range, `100` cho các bảng còn lại).
  - Common `status`: không nhận từ client khi create/update; status chỉ đổi qua delete/restore.
  - `age_ranges`: `min_age >= 0`, `max_age >= 0`, `min_age <= max_age` khi cả hai có giá trị, `display_order >= 0`.
  - `regions`: `code` required, trim, max `10`, allowed chars `[A-Z0-9_-]` sau normalize uppercase; `parent_id` optional, phải tồn tại active, không tự tham chiếu, không tạo cycle.
  - `occupations`: `description` optional max `255`.
  - `education_levels`: `description` optional max `255`, `display_order >= 0`.
  - `learning_purposes`: `description` optional max `255`.
- [ ] Invalidate cache `vocanova:knn-topics:{user_id}` khi lookup đổi:
  - tối thiểu clear theo affected users nếu có thể xác định từ `user_learning_profiles`
  - hoặc clear toàn bộ KNN topic cache namespace nếu triển khai cache namespace scan
- [ ] Admin: `POST /api/admin/knn/trigger-rebuild`:
  - Rate limit: 1 req/5 phút/admin
  - Gọi rebuild service async (fire-and-forget, không await request)
  - Trả ngay `202 Accepted`: `{ message: "Đang rebuild, vui lòng chờ...", triggered_at: NOW() }`
- [ ] Admin: `GET /api/admin/knn/rebuild-status`:
  - Đọc `vocanova:knn-last-rebuild` từ Redis
  - Trả `{ last_rebuild_at: DateTime?, is_running: bool }`
  - Dùng bởi F063 để hiển thị "Last rebuilt: X giờ trước"
- [ ] `KnnConfigDto`: current onboarding/learning config values from `KnnOptions`
- [ ] `KnnRebuildStatusDto`: `last_rebuild_at`, `is_running`
- [ ] Lookup DTOs cho 5 nhóm onboarding để F063 dashboard dùng:
  - `AgeRangeDto`: `age_range_id`, `name`, `min_age`, `max_age`, `display_order`, `status`
  - `RegionDto`: `region_id`, `name`, `code`, `parent_id`, `parent_name`, `status`
  - `OccupationDto`: `occupation_id`, `name`, `description`, `status`
  - `EducationLevelDto`: `education_level_id`, `name`, `description`, `display_order`, `status`
  - `LearningPurposeDto`: `learning_purpose_id`, `name`, `description`, `status`
- [ ] Audit log ghi qua middleware cho `POST/PUT/DELETE/PATCH /api/admin/knn/*`
- [ ] Unit test:
  - trigger-rebuild 2 lần trong 5 phút → lần 2 nhận 429.
  - job xử lý 1 user lỗi → user tiếp theo vẫn chạy.
  - config endpoint trả đúng `KnnOptions`.
  - lookup list/search/status/includeDeleted/pagination đúng.
  - lookup create/update/delete/restore đúng từng bảng.
  - duplicate name/code trả 409.
  - invalid age range/region parent/cycle trả 400.
  - delete/restore invalidates KNN topic recommendation cache.

---

## PHASE 1 — Backend: Module Media (M8)

**Quyết định provider M8:**
- Word image assets upload lên **Cloudinary**. API chỉ lưu delivery URL vào `words.image_url`; Cloudinary credentials/config đọc từ `.env`/environment configuration.
- Word audio assets lưu trên **Amazon S3** và phát qua **Amazon CloudFront**. API lưu public/CDN URL vào `word_audio_assets.storage_url`; không lưu file local trong repo/runtime server.
- Không cần migration cho M8 nếu dữ liệu chỉ cần URL hiện có (`words.image_url`, `word_audio_assets.storage_url`).
- Không hardcode provider keys trong `appsettings.json`; thêm key vào `.env.example`, bind qua Options.

---

### F051 — Audio Upload & Soft Delete
```
Branch:    feature/media/audio-upload
Assignee:  Huy
Thời gian: 2h
Phụ thuộc: F002
```
**Done khi:**
- [ ] `POST /api/admin/words/{id}/audio` (multipart/form-data)
- [ ] Request nhận `accent` (`uk`/`us`) và file audio.
- [ ] Validate MIME: `audio/mpeg`, `audio/wav`, `audio/ogg` — max 5MB.
- [ ] Upload file lên Amazon S3 bucket theo key chuẩn: `words/{word_id}/audio/{accent}/{yyyyMMddHHmmss}-{safeFileName}`.
- [ ] Delivery URL trả về nên là CloudFront URL nếu `AudioStorage__CloudFrontBaseUrl` được cấu hình; fallback S3 object URL chỉ dùng cho dev.
- [ ] Insert `word_audio_assets` với `word_id`, `accent`, `source='uploaded'`, `storage_url`, `status='uploaded'`, `created_at`.
- [ ] `DELETE /api/admin/words/{id}/audio/{audioId}`: soft delete (`status='deleted'`)
- [ ] Delete không xóa object S3 ngay trong request; chỉ soft delete DB. Cleanup object cứng là job/phạm vi riêng nếu cần.
- [ ] `IAudioStorage` interface + `S3AudioStorage` implementation.
- [ ] Config qua `.env.example`:
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
Thời gian: 1.5h
Phụ thuộc: F051
```
**Done khi:**
- [ ] `POST /api/admin/words/{id}/image` (multipart/form-data) upload image lên Cloudinary.
- [ ] Validate MIME: `image/jpeg`, `image/png`, `image/webp` — max 5MB.
- [ ] Upload vào Cloudinary folder/key chuẩn: `vocanova/words/{word_id}`.
- [ ] Lưu Cloudinary secure delivery URL vào `words.image_url`.
- [ ] `PUT /api/admin/words/{id}/image`: vẫn hỗ trợ set URL thủ công nếu admin cần reuse URL có sẵn.
- [ ] Validate manual `image_url` là URL hợp lệ và chỉ cho `https`.
- [ ] `POST /api/admin/words/{id}/image/suggest` bị hạ xuống optional/later; không dùng Pixabay/Unsplash là provider mặc định cho upload chính.
- [ ] `IImageStorage` interface + `CloudinaryImageStorage` implementation.
- [ ] Config qua `.env.example`:
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
Thời gian: 2h
Phụ thuộc: F020
```
**Done khi:**
- [ ] `GET /api/admin/users?page=&status=&search=`: dùng `.IgnoreQueryFilters()`, filter theo status/search phone/name
- [ ] `GET /api/admin/users/{id}`: chi tiết user + learning profile
- [ ] `PATCH /api/admin/users/{id}/deactivate`: SuperAdmin only — soft delete + revoke all refresh tokens
- [ ] `PATCH /api/admin/users/{id}/restore`: SuperAdmin only
- [ ] Unit test: deactivate → tokens revoked; search by phone

---

### F054 — Admin Stats Endpoints
```
Branch:    feature/admin/stats
Assignee:  Huy
Thời gian: 2.5h
Phụ thuộc: F043
```
**Done khi:**
- [ ] `GET /api/admin/stats/dashboard`: total_users, total_words, sessions_today, avg_accuracy_7d (cache 5 phút)
- [ ] `GET /api/admin/stats/demographics`: GROUP BY age_range, occupation, education_level
- [ ] `GET /api/admin/stats/learning`: top 20 từ hay sai nhất toàn hệ thống, accuracy trend 30 ngày
- [ ] `GET /api/admin/audit-logs?page=&userId=&entity=`: paginated

---

## PHASE 2 — Dashboard MVC

---

### F055 — Dashboard Cookie Auth & Layout
```
Branch:    feature/dashboard/auth-layout
Assignee:  Tan
Thời gian: 3h
Phụ thuộc: F015
```
**Done khi:**
- [ ] Cookie-based auth setup trong `Program.cs`
- [ ] `AuthController.Login`: dùng `AuthService` (shared service layer), set cookie
- [ ] `AuthController.Logout`: clear cookie
- [ ] `_Layout.cshtml`: sidebar với icons, responsive
- [ ] Role-based sidebar: Admin không thấy tab "Admin Accounts"
- [ ] Redirect: `/login` nếu chưa auth; redirect `/dashboard` sau login

---

### F056 — Dashboard Overview Page
```
Branch:    feature/dashboard/overview
Assignee:  Tan
Thời gian: 2.5h
Phụ thuộc: F054, F055
```
**Done khi:**
- [ ] 4 stat cards: Users, Words, Sessions hôm nay, Accuracy 7 ngày
- [ ] Line chart (Chart.js): sessions/ngày — 7 ngày gần nhất
- [ ] Pie chart: mastery level distribution toàn hệ thống
- [ ] Auto-refresh mỗi 5 phút (JavaScript `setInterval`)

---

### F057 — Vocabulary List & Filter
```
Branch:    feature/dashboard/vocab-list
Assignee:  Tan
Thời gian: 3h
Phụ thuộc: F022, F024, F055
```
**Done khi:**
- [ ] DataTable với: search (tên từ), filter CEFR level, filter topic, filter status
- [ ] Toggle "Hiện đã xóa" → dùng `.IgnoreQueryFilters()`
- [ ] Mỗi row: word, CEFR badge, topic chips, status badge, nút Edit/Delete/Restore
- [ ] Delete button confirm dialog
- [ ] Restore button (chỉ hiện khi đang xem deleted)
- [ ] Paginate server-side (không client-side DataTable)

---

### F058 — Vocabulary Detail & Sense Management
```
Branch:    feature/dashboard/vocab-detail
Assignee:  Tan
Thời gian: 3.5h
Phụ thuộc: F057
```
**Done khi:**
- [ ] `Vocabulary/Detail.cshtml`: hiển thị word info + ảnh + audio player
- [ ] Accordion senses: mỗi sense có nút Edit/Delete inline (AJAX)
- [ ] Form thêm sense mới (AJAX, không reload trang)
- [ ] Examples list inline trong sense, có thêm/xóa
- [ ] Relations table: synonym/antonym (view only ở đây)
- [ ] Audio section: list audio assets (UK/US), upload mới, delete (confirm)

---

### F059 — Vocabulary CSV Import UI
```
Branch:    feature/dashboard/vocab-import
Assignee:  Tan
Thời gian: 2h
Phụ thuộc: F029, F057
```
**Done khi:**
- [ ] `Vocabulary/Import.cshtml`: drag-drop zone + file picker
- [ ] Preview: sample template CSV download link
- [ ] Sau upload: bảng kết quả — imported/skipped/errors
- [ ] Error rows: highlight đỏ với cột "Row #", "Column", "Message"
- [ ] Nút "Download errors as CSV"

---

### F060 — User Management Pages
```
Branch:    feature/dashboard/user-management
Assignee:  Tan
Thời gian: 3h
Phụ thuộc: F053, F055
```
**Done khi:**
- [ ] `Users/Index.cshtml`: list users, filter status (active/locked/deleted), search phone/name
- [ ] Toggle "Hiện đã xóa"
- [ ] Status badge: green (active) / orange (locked) / red (deleted)
- [ ] `Users/Detail.cshtml`: tabs — Profile | Learning Profile | Test History | Activity Log
- [ ] Nút Deactivate (SuperAdmin) với confirm modal
- [ ] Nút Restore (SuperAdmin), chỉ hiện khi user deleted

---

### F061 — Topic Management Page
```
Branch:    feature/dashboard/topic-management
Assignee:  Tan
Thời gian: 2h
Phụ thuộc: F028, F055
```
**Done khi:**
- [ ] `Topics/Index.cshtml`: DataTable, CRUD inline
- [ ] Delete button: **disabled** nếu `word_count > 0`, tooltip "Không thể xóa — còn {N} từ đang dùng topic này"
- [ ] Restore button cho deleted topics
- [ ] Inline edit: icon, topic_name, topic_name_vi

---

### F062 — Statistics Page
```
Branch:    feature/dashboard/statistics
Assignee:  Tan
Thời gian: 3h
Phụ thuộc: F044, F055
```
**Done khi:**
- [ ] Chart sessions theo thời gian: dropdown granularity (daily/weekly/monthly), AJAX cập nhật chart
- [ ] Chart accuracy trend
- [ ] Top 20 wrong words: table với word, wrong_count, % accuracy
- [ ] Demographics: 3 charts (age range, occupation, education level)

---

### F063 — KNN Management Page
```
Branch:    feature/dashboard/knn-management
Assignee:  Tan
Thời gian: 1.5h
Phụ thuộc: F050, F055
```
**Done khi:**
- [ ] Sidebar/menu "KNN Management" có các mục con:
  - AgeRange Name Management
  - Regions Management
  - Occupation Management
  - Education Levels Management
  - Learning Purposes Management
- [ ] Mỗi trang lookup có table + CRUD + soft delete/restore:
  - Age ranges: name, min_age, max_age, display_order, status
  - Regions: name, code, parent, status
  - Occupations: name, description, status
  - Education levels: name, description, display_order, status
  - Learning purposes: name, description, status
- [ ] Mỗi trang lookup phải có UX quản lý đầy đủ:
  - Search box, status filter, toggle "Hiện đã xóa", server-side pagination.
  - Create modal/form, edit modal/form, inline validation message theo API validator.
  - Delete confirmation modal, restore action chỉ hiện khi đang xem deleted.
  - Duplicate/conflict từ API hiển thị rõ trong form/table.
  - Loading/empty/error states.
  - Không cho sửa `status` trực tiếp; status chỉ qua Delete/Restore.
- [ ] Form validation trên Dashboard phải mirror backend validators:
  - Age ranges: min/max age hợp lệ, display_order không âm.
  - Regions: code format, parent không tự tham chiếu.
  - Description max length 255.
  - Name required và max length đúng từng bảng.
- [ ] Hiển thị KNN model config hiện tại dạng read-only trong trang tổng quan: K value, min sessions, recommendation count, min similarity, rebuild interval, cache TTL
- [ ] Gợi ý operator sửa `.env`/deployment config để thay đổi model settings
- [ ] "Trigger Rebuild" button với loading state (AJAX)
- [ ] Hiển thị "Last rebuilt: X giờ trước"

---

## PHASE 2.5 — Dashboard Revisions (Điều chỉnh sau rà soát)

> **Bối cảnh:** Sau khi dashboard hoàn tất F055–F063, rà soát thực tế phát hiện 4 nhóm cần điều chỉnh:
> tìm kiếm theo bảng chữ cái (a–z), kiểm chứng CRUD từ vựng, hoàn thiện dịch ngôn ngữ, và lỗi trang Settings
> không tự đổi ngôn ngữ/giao diện. Mỗi mục dưới đây = 1 branch điều chỉnh (`fix/…` hoặc `feature/…`).
>
> **Ghi chú kiến trúc i18n hiện tại (đã kiểm chứng trong code):**
> - `Services/Localization/Translator.cs`: đọc cookie `VocaNova.Dashboard.Language`, đăng ký **Scoped** trong `Program.cs`
>   (mỗi request tạo mới → đọc lại cookie). Chuỗi gốc trong view là **tiếng Anh**; khi `Language == "vi"` thì map
>   sang tiếng Việt qua `TranslationTable.Vietnamese`, thiếu key thì fallback về tiếng Anh.
> - Vì Translator là Scoped, **mọi trang đều đổi ngôn ngữ đúng** ngay sau khi cookie đổi. Trang nào "không đổi"
>   là do chuỗi bị **hardcode song ngữ** trong view, không đi qua `@T[]` (xem R04).

---

### R01 — Vocabulary Search: kiểm tra & bổ sung chỉ mục bảng chữ cái (A–Z)
```
Branch:    fix/dashboard/vocab-search-alphabet
Assignee:  Tan
Thời gian: 2h
Phụ thuộc: F057
```
**Hiện trạng (đã kiểm chứng):**
- `VocabularyController.Index` nhận `q` → `WordListFilter.Q` → gọi API `GET /api/words?q=` (F022 dùng `word_key LIKE {NormalizeWord(q)}%`).
- Đây là **prefix search**: gõ 1 ký tự bất kỳ `a`–`z` sẽ ra các từ **bắt đầu** bằng ký tự đó → về mặt kỹ thuật a–z đã hoạt động.
- **Chưa có** thanh chỉ mục bảng chữ cái (A B C … Z) để lọc nhanh; người dùng phải tự gõ.

**Quyết định (đã chốt 2026-07-06):** hỗ trợ **cả hai** — giữ prefix search (ô tìm kiếm tự do) **và** thêm thanh A–Z để lọc nhanh theo chữ cái đầu.

**Done khi:** ✅ **HOÀN THÀNH 2026-07-06**
- [x] Xác nhận: search 1 ký tự `a`…`z` trả đúng từ bắt đầu bằng ký tự đó (API `word_key LIKE q%`, kiểm chứng qua `VocabularyController.Index` → `WordListFilter.Q`).
- [x] Thêm thanh chỉ mục A–Z trong `Views/Vocabulary/Index.cshtml` (ngay dưới `filter-bar`): nút "All/Tất cả" + 26 nút `A`…`Z` (helper `LetterUrl`).
- [x] Bấm 1 chữ cái = điều hướng `/vocabulary?q={char}` (giữ nguyên cefr, wordType, topic, status, includeDeleted; page reset về 1).
- [x] Chữ cái đang chọn được highlight (`selectedLetter` = `Model.Q` khi đúng 1 ký tự chữ); "All" sáng khi không có `q`.
- [x] Chỉ A–Z (từ vựng tiếng Anh).
- [x] Nhãn/aria đi qua `@T[]` (thêm key `All`, `Filter by first letter` vào `TranslationTable`).
- [x] Responsive: `.alpha-bar { flex-wrap: wrap; }` trong `site.css`; style `.alpha-link` theme-aware (dùng biến `--accent`/`--surface`/`--border`).
- [x] Build dashboard: 0 error.

---

### R02 — Vocabulary CRUD: kiểm chứng toàn luồng & bịt lỗ hổng
```
Branch:    fix/dashboard/vocab-crud-verify
Assignee:  Tan
Thời gian: 2.5h
Phụ thuộc: F057, F058
```
**Hiện trạng (đã kiểm chứng trong `VocabularyController.cs`):**
- **Create** (`/vocabulary/create`): tạo word → tạo từng sense (Word type + nghĩa EN/VI + 1 ví dụ/sense). OK.
- **Edit** (`/vocabulary/{id}/edit`): PUT metadata + cập nhật sense hiện có + thêm nghĩa mới + gom ví dụ theo block + toggle Active = delete/restore. OK.
- **Delete/Restore** (`/vocabulary/delete`, `/restore`): soft delete/restore, giữ filter qua `returnUrl`. OK.
- Ví dụ (examples) đã được lưu (commit `1f061e2 feat(dictionary): persist sense examples on create/update`).

**Quyết định (đã chốt 2026-07-06): TẠM VÔ HIỆU HÓA nút xóa (sense/ví dụ).** ✅ **ĐÃ LÀM 2026-07-06**
- [x] Xóa ví dụ **đã lưu** ở màn Edit: nút ✕ chuyển `disabled` + class `.is-locked` + tooltip "Xóa ví dụ đã lưu tạm thời chưa được hỗ trợ." (`Edit.cshtml`). Nút ✕ trên dòng ví dụ **mới thêm** (chưa lưu) vẫn hoạt động để hủy dòng nháp.
- [x] JS `vocabulary-edit.js` bỏ qua nút `disabled`/`.is-locked`; CSS `site.css` làm mờ nút khóa.
- [x] Xóa sense: **không có nút xóa sense trong UI** (Edit chỉ thêm/sửa, Detail chỉ xem) → đã "không hỗ trợ" sẵn, không cần thêm gì.
- [x] Giữ nguyên luồng thêm/sửa; chỉ chặn xóa để tránh mất dữ liệu. Không xóa code, chỉ disable — dễ mở lại sau.

**Đã kiểm chứng bằng đọc code (`VocabularyController` + views):**
- [x] Create: tạo word → tạo từng sense (Word type + nghĩa EN/VI + ví dụ). `word_key` trùng → 409 "That word already exists."; thiếu `word` → "Word is required.".
- [x] Edit: PUT metadata + cập nhật sense hiện có + thêm nghĩa mới + gom ví dụ theo block; toggle Active off→`deleted`, on→`active` (gọi Delete/Restore API).
- [x] Delete/Restore từ list: soft delete/restore, giữ filter qua `returnUrl`.
- [x] Phân quyền: `canManage = role is "admin" or "super_admin"` → ẩn nút Edit/Delete/Restore; API vẫn chặn 401/403.
- [x] Thông báo lỗi map theo status 400/409/403 trong controller.
- ⚠️ **Lưu ý (không phải nút xóa):** xóa **trắng** ô "English meaning"/"English example" rồi Save sẽ khiến controller bỏ qua block đó (`ExamplesForBlock`/vòng sense skip khi rỗng) → có thể vô tình không cập nhật/không lưu. Đây là hành vi edit, ngoài phạm vi khóa nút xóa; ghi lại để cân nhắc siết ở R sau nếu cần.

**Còn lại — cần chạy app xác nhận trực quan (ghi vào `VocaNova_Activity_History.md`):**
- [ ] Chạy end-to-end Create/Edit/Delete/Restore trên UI với API + DB thật, chụp màn hình.
- [ ] Xác nhận nút ✕ ví dụ đã lưu hiển thị mờ + không xóa được; nút ✕ dòng mới thêm vẫn hủy được.

---

### R03 — Hoàn thiện dịch ngôn ngữ (i18n coverage)
```
Branch:    feature/dashboard/i18n-coverage
Assignee:  Tan
Thời gian: 2.5h
Phụ thuộc: F055
```
**Hiện trạng (đã kiểm chứng):**
- Cơ chế dịch đã có và **đúng** (`Translator` + `TranslationTable`, Scoped). Vấn đề là **độ phủ chưa đủ**: một số chuỗi bị hardcode, không đi qua `@T[]` nên không đổi theo ngôn ngữ.
- Ví dụ đã phát hiện:
  - `Views/Vocabulary/Index.cshtml` dòng ~31: `Quản lý dữ liệu từ vựng — @Model.TotalItems @T["word(s)."]` → phần "Quản lý dữ liệu từ vựng" hardcode tiếng Việt.
  - `Views/Settings/Index.cshtml`: nhãn thẻ theme/ngôn ngữ và nút hành động hardcode song ngữ (xem R04).
  - `data-confirm="Delete '@item.Word'? …"` (Index.cshtml) hardcode tiếng Anh.

**Done khi:** ✅ **HOÀN THÀNH 2026-07-06 (trừ Settings — thuộc R04)**
- [x] Rà toàn bộ `Views/**/*.cshtml` bằng grep ký tự tiếng Việt + attribute (placeholder/title/data-confirm/aria-label) + text node.
- [x] Bọc các chuỗi hardcode còn thiếu bằng `@T[...]` / `@(T.Format(...))` với key gốc tiếng Anh:
  - `Vocabulary/Index.cshtml`: subtitle "Manage vocabulary metadata"; `data-confirm` (Format).
  - `Vocabulary/Edit.cshtml`: placeholder "Vietnamese meaning".
  - `Vocabulary/Create.cshtml`: placeholder ví dụ VI ("e.g. This word is very beautiful.").
  - `Topics/Index.cshtml`: "Topic name (VI)" (×2), "Name (VI)", title không-thể-xóa (Format), data-confirm xóa topic (Format).
  - `_Layout.cshtml`: aria-label "Toggle navigation" (×2), "Primary navigation".
  - `Knn/Index.cshtml` + `Knn/Lookup.cshtml`: data-confirm rebuild / xóa item.
- [x] Bổ sung ~14 key mới vào `TranslationTable.Entries` (cặp EN→VI), gồm cả chuỗi có `{0}` cho `Format`.
- [x] Thông báo server render qua `@T[toastMsg]` ở `_Layout` (đã có sẵn).
- [x] Build 0 error; grep còn lại sạch (chỉ `Settings` thuộc R04 + nút "OK" trung tính).
- [ ] **Còn lại (cần chạy app):** xác nhận trực quan `vi`↔`en` trên mọi trang đồng nhất, không lẫn lộn.
- ➡️ Chuỗi song ngữ "X / Y" ở trang Settings sẽ xử lý trong **R04**.

---

### R04 — Fix: trang Settings không tự đổi ngôn ngữ/giao diện 🔴
```
Branch:    fix/dashboard/settings-not-reacting
Assignee:  Tan
Thời gian: 2h
Phụ thuộc: R03
```
**Triệu chứng (theo báo cáo):** đổi ngôn ngữ ở Settings thì **các trang khác** đổi đúng, nhưng **chính trang Settings** không đổi.

**Nguyên nhân gốc (đã kiểm chứng — KHÔNG phải lỗi Translator):**
- `Translator` là **Scoped** → sau khi lưu cookie và redirect về `/settings`, các chuỗi `@T[]` trên trang Settings **đã** đổi đúng.
- Nhưng phần hiển thị **nổi bật nhất** của trang Settings lại **hardcode song ngữ**, không đi qua `@T[]`, nên trông như "không đổi":
  - `Views/Settings/Index.cshtml`: `"Chế độ Sáng"`/`"Light Mode"`, `"Chế độ Tối"`/`"Dark Mode"` (thẻ theme).
  - `"Tiếng Việt"`/`"Vietnamese"`, `"Tiếng Anh"`/`"English"` (hàng ngôn ngữ).
  - Nút `"Hủy / Cancel"`, `"Lưu thay đổi / Save Changes"`.
  - `SettingsController.Save`: `TempData["SettingsSaved"] = "Đã lưu thay đổi / Changes saved."` (song ngữ cứng).
- Về **theme**: `settings.js` chỉ đổi highlight thẻ + hidden input (chưa lưu tới khi Save). `_Layout` đọc cookie theme và set `data-theme` server-side → sau khi Save + reload, trang Settings **phải** đổi theme; cần kiểm chứng các thành phần riêng của Settings (`appearance-card`, `language-row`) có dùng biến CSS theme-aware trong `site.css` không, tránh màu cứng.

**Done khi:**
- [ ] Thay toàn bộ nhãn hardcode song ngữ trong `Views/Settings/Index.cshtml` bằng `@T[]` (một ngôn ngữ theo lựa chọn):
  - Thẻ theme: nhãn qua `@T["Light Mode"]`, `@T["Dark Mode"]` (bỏ dòng phụ song ngữ).
  - Hàng ngôn ngữ: `@T["Vietnamese"]`, `@T["English"]`.
  - Nút: `@T["Cancel"]`, `@T["Save Changes"]`.
- [ ] Thêm các key mới vào `TranslationTable`: `Light Mode`, `Dark Mode`, `Vietnamese`, `English`, `Save Changes`, `Changes saved.`…
- [ ] `SettingsController.Save`: đổi `TempData["SettingsSaved"]` sang **key tiếng Anh** (vd `"Changes saved."`) rồi render qua `@T[]` ở view, thay vì chuỗi song ngữ cứng.
- [ ] Kiểm chứng theme: đổi Dark/Light + Save → trang Settings (và mọi thẻ trong đó) đổi màu đúng nhờ `data-theme`; sửa `site.css` nếu `appearance-card`/`language-row` còn dùng màu cứng.
- [ ] Kiểm chứng ngôn ngữ: chọn `en` + Save → trang Settings hiển thị **toàn tiếng Anh**; chọn `vi` + Save → **toàn tiếng Việt**; không còn hiển thị đồng thời 2 ngôn ngữ.
- [ ] (Tùy chọn UX) Cân nhắc đổi theme/ngôn ngữ **áp dụng ngay** khi bấm (không cần Save) để phản hồi tức thì; nếu giữ mô hình Save thì đảm bảo sau reload nhất quán.

---

## PHASE 3 — Flutter Mobile

---

### F064 — Project Init & Theme
```
Branch:    feature/mobile-core/project-init-theme
Assignee:  Nhut
Thời gian: 2h
Phụ thuộc: —
```
**Done khi:**
- [ ] Flutter project `vocanova_mobile` tạo xong, run được
- [ ] `pubspec.yaml` với tất cả dependencies
- [ ] `AppColors`: primary `#B8AEFF`, background `#1C1A2E`, surface `#2A2740`, error `#FF6B6B`
- [ ] `AppTheme.dark()` + `AppTheme.light()`
- [ ] `AppTextStyles`: heading, body, caption, label
- [ ] Global font (Inter hoặc Nunito từ Google Fonts)

---

### F065 — DioClient & Interceptors
```
Branch:    feature/mobile-core/dio-interceptors
Assignee:  Nhut
Thời gian: 2.5h
Phụ thuộc: F064
```
**Done khi:**
- [ ] `DioClient` singleton: base URL, connectTimeout 10s, receiveTimeout 30s
- [ ] `AuthInterceptor`:
  - `onRequest`: gắn `Authorization: Bearer {token}` từ `SecureStorage`
  - `onError` 401: gọi refresh endpoint → retry request gốc → nếu refresh fail → logout
- [ ] `ErrorInterceptor`: parse `errors[]` từ body → ném `AppException(message)` với message tiếng Việt
- [ ] `ApiEndpoints` class: tất cả URL là const string
- [ ] Test: mock 401 → interceptor tự refresh

---

### F066 — LocalStorage & SecureStorage
```
Branch:    feature/mobile-core/local-secure-storage
Assignee:  Nhut
Thời gian: 2h
Phụ thuộc: F064
```
**Done khi:**
- [ ] `LocalStorage` class (singleton, shared_preferences):
  - `getWithTtl<T>()` / `setWithTtl<T>()` — lưu kèm `{key}_saved_at` milliseconds
  - `get()` / `set()` thường (không TTL)
  - `remove()`, `clearAll()`
  - Keys: `user_profile_json`, `lists_cache_json`, `word_cache_{id}_json`, `progress_summary_json`, `search_history_json`, `app_locale`, `app_theme`
- [ ] `SecureStorage` class (flutter_secure_storage): `saveAccessToken`, `getAccessToken`, `saveRefreshToken`, `getRefreshToken`, `clearTokens`
- [ ] Unit test: TTL expired → returns null; TTL not expired → returns value

---

### F067 — GoRouter Setup
```
Branch:    feature/mobile-core/go-router
Assignee:  Nhut
Thời gian: 2h
Phụ thuộc: F064
```
**Done khi:**
- [ ] `AppRouter` với tất cả routes: `/login`, `/register`, `/otp`, `/onboarding`, `/home`, `/search`, `/word/:id`, `/lists`, `/list/:id`, `/quiz/config`, `/quiz/active`, `/quiz/result`, `/progress`, `/settings`, `/profile`
- [ ] `AuthGuard`: redirect `/login` nếu không có token
- [ ] `RootRedirect`: check token → `/home` hoặc `/login`
- [ ] Bottom navigation bar (Home / Search / Lists / Progress)

---

### F068 — AuthNotifier & AuthRepository
```
Branch:    feature/mobile-auth/auth-provider
Assignee:  Nhut
Thời gian: 2.5h
Phụ thuộc: F066, F067
```
**Done khi:**
- [ ] `AuthState`: status (initial/loading/authenticated/unauthenticated/error), user (UserProfile?)
- [ ] `AuthNotifier extends _$AuthNotifier`:
  - `login(phone, password)`
  - `googleLogin(idToken)`
  - `logout()`: clear SecureStorage + LocalStorage.clearAll() + navigate /login
  - `loadCurrentUser()`: GET /auth/me, lưu cache 1 ngày
- [ ] `AuthRepository`: wrap Dio calls
- [ ] Token lưu `SecureStorage`, profile lưu `LocalStorage` với TTL 1 ngày

---

### F069 — Login & Register Screens
```
Branch:    feature/mobile-auth/login-register-screens
Assignee:  Nhut
Thời gian: 3h
Phụ thuộc: F068
```
**Done khi:**
- [ ] `LoginScreen`: phone field (VN format hint), password (toggle show/hide), "Quên mật khẩu" link, Google sign-in button
- [ ] `RegisterScreen`: phone, password, confirm password (cross-validate), display_name
- [ ] Form validation: inline error messages tiếng Việt
- [ ] Loading state khi submit (disable button, show CircularProgressIndicator)
- [ ] Error snackbar khi API fail

---

### F070 — OTP & Forgot Password Screens
```
Branch:    feature/mobile-auth/otp-forgot-screens
Assignee:  Nhut
Thời gian: 2.5h
Phụ thuộc: F069
```
**Done khi:**
- [ ] `OtpScreen`: 6 ô input tự động focus next, countdown 60s resend, thông báo khi max 5 lần sai
- [ ] `ForgotPasswordScreen`: phone → OTP → new password (3 bước trong 1 screen)
- [ ] OTP auto-submit khi nhập đủ 6 số

---

### F071 — Onboarding Screen
```
Branch:    feature/mobile-auth/onboarding-screen
Assignee:  Nhut
Thời gian: 2.5h
Phụ thuộc: F068
```
**Done khi:**
- [ ] 5 bước: Age Range, Region, Occupation, Education Level, Learning Purpose
- [ ] Mỗi bước: list chip selects (single select)
- [ ] Progress indicator (bước X/5)
- [ ] "Bỏ qua" button (onboarding optional)
- [ ] Submit: gọi `PUT /auth/me/learning-profile`

---

### F072 — Word Search Screen
```
Branch:    feature/mobile-dictionary/search-screen
Assignee:  Nhut
Thời gian: 3h
Phụ thuộc: F067
```
**Done khi:**
- [ ] Search bar luôn visible ở top
- [ ] Debounce 300ms: chỉ gọi API sau 300ms dừng gõ
- [ ] Khi search bar empty: hiện search_history (max 20 từ gần nhất, có nút xóa)
- [ ] Kết quả: `WordSummaryCard` (word, phonetic, CEFR badge, meaning ngắn)
- [ ] Filter chips: CEFR (A1–C2), Topics (từ API)
- [ ] Loading skeleton khi đang tìm
- [ ] Offline: banner + chỉ search trong cached words + history

---

### F073 — Word Detail Screen
```
Branch:    feature/mobile-dictionary/word-detail-screen
Assignee:  Nhut
Thời gian: 3h
Phụ thuộc: F072
```
**Done khi:**
- [ ] Offline cache check trước khi gọi API (TTL 2 giờ)
- [ ] Hero section: word, phonetic UK/US (tap để chuyển), CEFR badge, image
- [ ] Audio player: play UK / US button (dùng `audioplayers`)
- [ ] Senses accordion: word_class badge, definition EN + VI, examples list
- [ ] Related words chips: synonym (xanh), antonym (đỏ) — tap để navigate
- [ ] Topics chips ở bottom
- [ ] FAB: "Thêm vào danh sách" → mở `AddToListSheet`
- [ ] "Thêm vào sách từ" button lưu vào `word_cache_{id}` shared_pref

---

### F074 — Lists Screen & Create
```
Branch:    feature/mobile-lists/lists-screen
Assignee:  Nhut
Thời gian: 2.5h
Phụ thuộc: F067
```
**Done khi:**
- [ ] Grid view lists: card với list_name, word_count, created_at
- [ ] FAB "+" → dialog tạo list (validate tên)
- [ ] Long press list → actions: Rename, Delete (confirm)
- [ ] Offline: show cached lists + banner
- [ ] `ListsNotifier` optimistic update khi delete (xóa ngay UI, rollback nếu API fail)

---

### F075 — List Detail Screen
```
Branch:    feature/mobile-lists/list-detail-screen
Assignee:  Nhut
Thời gian: 3h
Phụ thuộc: F074
```
**Done khi:**
- [ ] List từ trong list: paginated (infinite scroll)
- [ ] Mỗi word card: word, meaning, correct/wrong count, note
- [ ] Swipe left → Delete (confirm)
- [ ] Tap card → navigate `WordDetailScreen`
- [ ] FAB "+ Thêm từ" → bottom sheet `AddWordSheet` (search từ dictionary)
- [ ] "Thêm ngẫu nhiên" button → dialog chọn topic/method/count
- [ ] "Bắt đầu kiểm tra" button → navigate `QuizConfigScreen` với list pre-selected

---

### F076 — Quiz Config Screen
```
Branch:    feature/mobile-quiz/quiz-config-screen
Assignee:  Nhut
Thời gian: 3h
Phụ thuộc: F067
```
**Done khi:**
- [ ] Section "Phạm vi từ": All / Từ ngày / Đến ngày / Khoảng ngày (date pickers)
- [ ] Section "Chủ đề": multi-select topic chips hoặc "Tất cả"
- [ ] Section "Chế độ": 4 buttons (Standard / Timed / Challenge / Elimination) với mô tả
- [ ] Section "Loại câu hỏi": Word→Meaning / Meaning→Word / Description
- [ ] Section "Cách trả lời": Multiple Choice / Exact Typing / AI Typing
- [ ] Validate cross-field: timed → time input, elimination → lives input
- [ ] "Bắt đầu" button → gọi API tạo session → navigate QuizScreen

---

### F077 — Quiz Screen: Multiple Choice
```
Branch:    feature/mobile-quiz/quiz-screen-multiple-choice
Assignee:  Nhut
Thời gian: 3h
Phụ thuộc: F076
```
**Done khi:**
- [ ] Progress bar: câu X/N
- [ ] Timer countdown (timed mode)
- [ ] Lives indicator 🩷 (elimination mode)
- [ ] Question text
- [ ] 4 answer buttons: default → sau submit: đúng (xanh), sai (đỏ) + đúng highlight (xanh)
- [ ] Sau submit: "Tiếp theo" button (hoặc auto-advance 1.5s)
- [ ] AI explanation (nếu AI typing mode)
- [ ] Abandon button (confirm dialog)

---

### F078 — Quiz Screen: Typing Mode
```
Branch:    feature/mobile-quiz/quiz-screen-typing
Assignee:  Nhut
Thời gian: 2.5h
Phụ thuộc: F077
```
**Done khi:**
- [ ] TextField với keyboard up
- [ ] Submit button (hoặc enter key)
- [ ] AI typing: loading indicator khi chờ AI
- [ ] Exact match: không phân biệt hoa thường, bỏ dấu câu cuối
- [ ] AI mode: hiển thị `ai_score` dạng % + `ai_explanation` + `ai_suggestion`

---

### F079 — Quiz Result Screen & Wrong Words
```
Branch:    feature/mobile-quiz/quiz-result-wrong-words
Assignee:  Nhut
Thời gian: 2.5h
Phụ thuộc: F077
```
**Done khi:**
- [ ] Animated score reveal (circular progress)
- [ ] Stats: correct/total, accuracy %, streak, time
- [ ] Breakdown list: từng câu — icon ✓/✗, word, user_answer vs expected
- [ ] "Xem lại sai" button → WrongWordsScreen
- [ ] "Làm lại" button → QuizConfigScreen
- [ ] `WrongWordsScreen`: list từ sai, swipe để bỏ khỏi wrong list, "Test lại" button

---

### F080 — Progress Overview Screen
```
Branch:    feature/mobile-progress/progress-overview
Assignee:  Nhut
Thời gian: 3h
Phụ thuộc: F067
```
**Done khi:**
- [ ] Streak card: flame icon + số ngày, longest streak
- [ ] Accuracy gauge (7 ngày gần nhất)
- [ ] Word counts: "X từ đang học", "Y từ đã thành thạo"
- [ ] Sessions count (tháng này)
- [ ] Offline: show cached progress_summary + banner (TTL 15 phút)

---

### F081 — Progress Charts Screen
```
Branch:    feature/mobile-progress/charts-screen
Assignee:  Nhut
Thời gian: 2.5h
Phụ thuộc: F080
```
**Done khi:**
- [ ] Line chart sessions/ngày (30 ngày) — dùng `fl_chart`
- [ ] Dropdown granularity: daily/weekly/monthly
- [ ] Mastery breakdown: horizontal bar chart per level 0–5
- [ ] Weakest words: top 10 list với progress bar (accuracy rate)

---

### F082 — Settings & Profile Screen
```
Branch:    feature/mobile-settings/settings-profile
Assignee:  Nhut
Thời gian: 2h
Phụ thuộc: F067
```
**Done khi:**
- [ ] `SettingsScreen`: Language (VI/EN), Theme (Dark/Light), Notifications (stub), App version
- [ ] Language switch: lưu `app_locale`, restart locale
- [ ] Theme switch: lưu `app_theme`, apply ngay
- [ ] `ProfileScreen`: avatar (placeholder), display_name edit, phone (masked), learning profile edit link
- [ ] Logout button (confirm dialog) → clear all storage → navigate /login

---

### F083 — Offline Handling & Connectivity
```
Branch:    feature/mobile-core/offline-handling
Assignee:  Nhut
Thời gian: 2.5h
Phụ thuộc: F066, F065
```
**Done khi:**
- [ ] `ConnectivityProvider` dùng `connectivity_plus` stream → `isOnline: bool`
- [ ] `OfflineBanner` widget: hiện banner vàng "Bạn đang ngoại tuyến" khi offline
- [ ] Dictionary: offline → dùng `word_cache_{id}` + search history (không gọi API)
- [ ] Quiz: offline → disable "Bắt đầu" + tooltip "Cần kết nối mạng"
- [ ] Lists: offline → show cached lists, disable Add/Delete
- [ ] Cache warming sau login: prefetch user_profile + lists + progress_summary

---

## Tổng kết Feature Count

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
| Phase 2 | Dashboard | F055–F063 (9 features) |
| Phase 3 | Mobile | F064–F083 (20 features) |
| **Total** | | **83 features** |

---

## Ước tính thời gian tổng

| Assignee | Tổng features | Tổng giờ ước tính |
|---|---|---|
| An (DevOps) | F001, F002, F009, F010, F021 | ~14h |
| Huy (Backend) | F003–F008, F011–F054 (bỏ F002, F009–F010, F021) | ~120h |
| Tan (Dashboard) | F055–F063 | ~25h |
| Nhut (Mobile) | F064–F083 | ~65h |

> Huy chịu tải nặng nhất — An nên hỗ trợ một số backend tasks trong Phase 1 nếu có thể.

---

## Quick Reference: Feature → Branch → Commit

```bash
# Ví dụ workflow hoàn chỉnh cho F039 (SM-2 Algorithm)

git checkout dev
git pull origin dev
git checkout -b feature/quiz/sm2-algorithm

# code...

git add .
git commit -m "feat(quiz): implement SM-2 spaced repetition algorithm"
git commit -m "test(quiz): add unit tests for SM-2 edge cases"

git push origin feature/quiz/sm2-algorithm
# Tạo PR: [Quiz] SM-2 Spaced Repetition Algorithm → dev
```
