# Backend Service Catalog

## Scope and status

Everything in the feature inventory below is **CURRENT** and was checked against API controllers, service/repository registrations and implementations, persistence entities, integrations, and relevant tests. It is not a target endpoint proposal. All controller responses use the shared `{ success, data, message, errors, pagination? }` envelope unless an endpoint has no body.

Public routes, HTTP methods, authorization behavior, JSON schemas, and envelopes are compatibility surfaces. The TARGET layer refactor changes ownership and mapping, not endpoint behavior, unless a separate task explicitly authorizes an API change.

## Auth (CURRENT)

Purpose: Registration, password and Google login, rotating refresh tokens, logout, OTP verification/recovery, account/profile management, avatar upload, and learning-profile updates.

- Controllers: `Features/Auth/Controllers/AuthController.cs`
- Services: `Features/Auth/BLL/Services/IAuthService`, `Features/Auth/BLL/Services/AuthService`
- Repositories: BLL-owned `IAuthAccountRepository`, `IRefreshTokenRepository`, and `IOtpRepository` implemented by `Features/Auth/DAL/Repositories/AuthAccountRepository`, `RefreshTokenRepository`, and `OtpRepository`
- Database: `User`, `Role`, `UserAuth`, `UserProfile`, `UserLearningProfile`, lookup entities, `RefreshToken`, `OtpVerification`, `UserTopicPreference`
- Integrations: BLL-owned ports for JWT, Google ID-token validation, OTP generation, SMS, password/refresh-token hashing, Cloudinary avatars, Redis profile/KNN cache, and in-memory auth rate limits; shared Infrastructure provides the concrete implementations.
- Important flows: `AuthService` uses BLL-owned repository/provider ports and `IApplicationTransactionManager` instead of direct EF/Infrastructure dependencies. Registration, new Google-account creation, refresh-token rotation, password reset, and account deletion use explicit EF transactions through the shared transaction manager and invalidate caches after commit. Only refresh-token hashes are stored. Profile responses use the five-minute `user:{userId}` Redis cache.
- Authorization: registration/login/Google/refresh/OTP/recovery are anonymous; logout and `/me` operations require Bearer authentication.

| Endpoint | Controller | Service/use case |
|---|---|---|
| `POST /api/auth/register` | AuthController | `RegisterAsync` |
| `POST /api/auth/login` | AuthController | `LoginAsync` |
| `POST /api/auth/google` | AuthController | `GoogleLoginAsync` |
| `POST /api/auth/refresh` | AuthController | `RefreshTokenAsync` |
| `POST /api/auth/logout` | AuthController | `LogoutAsync` |
| `POST /api/auth/otp/send` | AuthController | `SendOtpAsync` |
| `POST /api/auth/otp/verify` | AuthController | `VerifyOtpAsync` |
| `POST /api/auth/forgot-password` | AuthController | `ForgotPasswordAsync` |
| `POST /api/auth/reset-password/verify-otp` | AuthController | `VerifyResetOtpAsync` |
| `POST /api/auth/reset-password` | AuthController | `ResetPasswordAsync` |
| `PUT /api/auth/me/password` | AuthController | `ChangePasswordAsync` |
| `GET /api/auth/me` | AuthController | `GetProfileAsync` |
| `DELETE /api/auth/me` | AuthController | `DeleteAccountAsync` |
| `PUT /api/auth/me/profile` | AuthController | `UpdateProfileAsync` |
| `POST /api/auth/me/avatar` | AuthController | `UploadAvatarAsync` |
| `PUT /api/auth/me/learning-profile` | AuthController | `UpdateLearningProfileAsync` |

## Dictionary and vocabulary administration (CURRENT)

Purpose: Public word search/detail/daily word, public topics, and admin word/topic CRUD, CSV import, senses, examples, images, audio, deletion, and restore.

- Public read controllers: `Features/Dictionary/Controllers/WordsController` and `TopicsController`
- Public read services: BLL `IWordReadService`/`WordReadService`, `ITopicReadService`/`TopicReadService`
- Public read persistence: BLL-owned `IWordReadRepository` and `ITopicReadRepository`, implemented by DAL `WordReadRepository` and `TopicReadRepository`
- Administration/write side: `Features/Dictionary/Controllers/AdminWordsController` and `AdminTopicsController`; BLL `IWordAdminService`/`WordAdminService` and `ITopicAdminService`/`TopicAdminService`; BLL-owned repository/storage/cache ports implemented by feature DAL repositories and shared Infrastructure providers
- Database: `Word`, `WordSense`, `WordExample`, `WordAudioAsset`, `WordDerivedForm`, `WordIdiom`, `WordRelation`, `Topic`, `WordTopic`; list/user references are consulted for invalidation and deletion rules
- Integrations: Redis word-search/detail/topic/list caches; Cloudinary image/audio storage
- Important flows: public reads and administration use BLL-owned cache ports with shared Redis implementations. Word writes invalidate word-detail and affected user-list entries but do not clear word-search entries; topic mutations invalidate the topics list and only the applicable topic-word pages. Word/topic/audio/sense rows use soft-delete status. Sense delete/restore verifies the `wordId`/`senseId` pair, saves once, and invalidates word detail. Admin word deletion/restoration requires SuperAdmin. Existing CSV/example/topic-link multi-save ordering and Cloudinary-before-relational-save ordering remain unchanged.
- Authorization: word/topic reads are anonymous; admin endpoints require the Admin policy, with word delete/restore additionally requiring SuperAdmin.

| Endpoint | Controller | Service/use case |
|---|---|---|
| `GET /api/words` | WordsController | `SearchAsync` |
| `GET /api/words/{id}` | WordsController | `GetByIdAsync` |
| `GET /api/words/daily` | WordsController | `GetDailyAsync` |
| `GET /api/topics` | TopicsController | `GetTopicsAsync` |
| `GET /api/topics/{id}/words` | TopicsController | `GetWordsAsync` |
| `GET /api/admin/words` | AdminWordsController | `SearchAsync` |
| `POST /api/admin/words` | AdminWordsController | `CreateAsync` |
| `PUT /api/admin/words/{id}` | AdminWordsController | `UpdateAsync` |
| `POST /api/admin/words/import` | AdminWordsController | `ImportCsvAsync` |
| `POST /api/admin/words/{id}/audio` | AdminWordsController | `UploadAudioAsync` |
| `DELETE /api/admin/words/{id}/audio/{audioId}` | AdminWordsController | `SoftDeleteAudioAsync` |
| `POST /api/admin/words/{id}/image` | AdminWordsController | `UploadImageAsync` |
| `PUT /api/admin/words/{id}/image` | AdminWordsController | `UpdateImageUrlAsync` |
| `DELETE /api/admin/words/{id}` | AdminWordsController | `SoftDeleteAsync` |
| `PATCH /api/admin/words/{id}/restore` | AdminWordsController | `RestoreAsync` |
| `POST /api/admin/words/{id}/senses` | AdminWordsController | `CreateSenseAsync` |
| `PUT /api/admin/words/{id}/senses/{senseId}` | AdminWordsController | `UpdateSenseAsync` |
| `DELETE /api/admin/words/{id}/senses/{senseId}` | AdminWordsController | `SoftDeleteSenseAsync` |
| `PATCH /api/admin/words/{id}/senses/{senseId}/restore` | AdminWordsController | `RestoreSenseAsync` |
| `GET /api/admin/topics` | AdminTopicsController | `ListAsync` |
| `POST /api/admin/topics` | AdminTopicsController | `CreateAsync` |
| `POST /api/admin/topics/{id}/words` | AdminTopicsController | `AddWordsAsync` |
| `PUT /api/admin/topics/{id}` | AdminTopicsController | `UpdateAsync` |
| `DELETE /api/admin/topics/{id}` | AdminTopicsController | `SoftDeleteAsync` |
| `PATCH /api/admin/topics/{id}/restore` | AdminTopicsController | `RestoreAsync` |

## Lists and personal topics (CURRENT)

Purpose: User vocabulary-list CRUD, word membership/notes, random additions, and topic-backed personal collections.

- Controllers: `Features/Lists/Controllers/ListsController.cs` and `PersonalTopicsController.cs`
- Services: BLL `IListQueryService`/`ListQueryService`, `IListMutationService`/`ListMutationService`, `IPersonalTopicQueryService`/`PersonalTopicQueryService`, and `IPersonalTopicMutationService`/`PersonalTopicMutationService`
- Repositories: BLL-owned query/mutation ports implemented by DAL `ListQueryRepository`, `ListMutationRepository`, `PersonalTopicQueryRepository`, and `PersonalTopicMutationRepository`
- Layer ownership: Presentation, BLL, and feature DAL code live under `Features/Lists`; Redis implementations remain consolidated under shared `Infrastructure/Caching`
- Database: `UserList`, `UserListWord`, `UserListWordStat`, `Word`, `WordRelation`, `WordTopic`, `Topic`, `UserTopicPreference`
- Integrations: Redis user-list cache
- Important flows: list ownership is checked for every mutation through an explicit lookup result that preserves missing/deleted/reserved-list 404 versus foreign-list 403; personal-topic collections are encoded as reserved list names prefixed `__topic__:`; list/list-word deletion is soft. The `user-lists:v2:{userId}` Redis entry has a 10-minute TTL. Create/rename/delete and membership writes invalidate after successful saves, while note-only updates retain the CURRENT behavior of not invalidating that summary cache. Random additions save and invalidate once per added word, while personal-topic get-or-create can save the reserved list before membership; neither sequence has an encompassing transaction, so partial completion remains possible.
- Authorization: all endpoints require an authenticated user.

| Endpoint | Controller | Service/use case |
|---|---|---|
| `GET /api/lists` | ListsController | `GetListsAsync` |
| `POST /api/lists` | ListsController | `CreateAsync` |
| `PUT /api/lists/{id}` | ListsController | `UpdateAsync` |
| `DELETE /api/lists/{id}` | ListsController | `SoftDeleteAsync` |
| `GET /api/lists/{id}/words` | ListsController | `GetWordsAsync` |
| `POST /api/lists/{id}/words` | ListsController | `AddWordAsync` |
| `POST /api/lists/{id}/words/random` | ListsController | `AddRandomWordsAsync` |
| `DELETE /api/lists/{id}/words/{wordId}` | ListsController | `RemoveWordAsync` |
| `PATCH /api/lists/{id}/words/{wordId}/note` | ListsController | `UpdateWordNoteAsync` |
| `GET /api/personal-topics` | PersonalTopicsController | `GetTopicsAsync` |
| `GET /api/personal-topics/{topicId}/words` | PersonalTopicsController | `GetWordsAsync` |
| `POST /api/personal-topics/{topicId}/words` | PersonalTopicsController | `AddWordAsync` |
| `DELETE /api/personal-topics/{topicId}/words/{wordId}` | PersonalTopicsController | `RemoveWordAsync` |

## Quiz, SRS, and AI grading (CURRENT)

Purpose: Build quiz pools/questions, create sessions, grade answers, update spaced-repetition progress, finish/result history, wrong-word review, and AI semantic grading.

- Controller: `Features/Quiz/Controllers/QuizSessionsController`
- Services: feature BLL session/builder/question/submission/result/history services, `SrsService`, exact and multiple-choice graders, and the AI-grading BLL `CachedAiGradingService`
- Layer ownership: Quiz and AI-grading Presentation/BLL/DAL code lives under `Features/Quiz` and `Features/AiGrading`; repository/cache/provider ports are BLL-owned. Redis quiz caching, Gemini transport/provider implementation, and runtime configuration adapters remain under shared `Infrastructure`.
- Repositories: BLL-owned quiz pool/session/question/submission/result/history/SRS and AI-cache ports implemented by feature DAL EF repositories
- Database: `TestSession`, `TestSessionTopic`, `TestAnswer`, `UserWordProgress`, `Word` and dictionary children, `UserListWord`, `AiGradingCache`
- Integrations: Redis quiz-pool/progress caches and Gemini
- Important flows: answer, session, and SM-2/SRS changes are staged and persisted by one `SaveChangesAsync`; AI cache hit/write accounting can save independently before that relational unit. Quiz pools use a two-hour Redis entry keyed by session and `list_id` (or `all`). Gemini-produced results use a seven-day MySQL cache; Gemini failure degrades to normalized exact matching and is not cached as an AI result.
- Authorization: all endpoints require an authenticated user.

| Endpoint | Controller | Service/use case |
|---|---|---|
| `GET /api/quiz/history` | QuizSessionsController | `GetHistoryAsync` |
| `GET /api/quiz/wrong-words` | QuizSessionsController | `GetWrongWordsAsync` |
| `DELETE /api/quiz/wrong-words/{wordId}` | QuizSessionsController | `ClearWrongWordAsync` |
| `POST /api/quiz/sessions` | QuizSessionsController | `CreateSessionAsync` |
| `POST /api/quiz/sessions/{id}/answer` | QuizSessionsController | `SubmitAnswerAsync` |
| `POST /api/quiz/sessions/{id}/finish` | QuizSessionsController | `FinishSessionAsync` |
| `GET /api/quiz/sessions/{id}/result` | QuizSessionsController | `GetResultAsync` |

## Progress (CURRENT)

Purpose: Per-user summary, time-series chart, mastery distribution, weakest words, and word-level learning details.

- Controller: `ProgressController`
- Services: `IProgressSummaryService`/`ProgressSummaryService`, `IProgressAnalyticsService`/`ProgressAnalyticsService`
- Layer ownership: Presentation, BLL, and feature repository/mapping code live under `Features/Progress`; the Redis summary-cache implementation remains under shared `Infrastructure/Caching`
- Repositories: BLL-owned progress summary/analytics ports implemented by DAL EF repositories
- Database: `TestSession`, `TestAnswer`, `UserWordProgress`, `Word`
- Integrations: Redis summary cache with the existing key, 15-minute TTL, degraded fallback, and Quiz-triggered invalidation
- Business rules: BLL calculates streaks, accuracy, chart periods/buckets, mastery levels, weakest-word results, validation, and missing word-progress behavior. SRS/SM-2 writes remain owned by Quiz and are outside this read-only feature.
- Transactions: none in Progress; existing Quiz single-save answer/session/SRS persistence semantics are unchanged.
- Authorization: all endpoints require an authenticated user.

| Endpoint | Controller | Service/use case |
|---|---|---|
| `GET /api/progress/summary` | ProgressController | `GetSummaryAsync` |
| `GET /api/progress/chart` | ProgressController | `GetChartAsync` |
| `GET /api/progress/mastery-breakdown` | ProgressController | `GetMasteryBreakdownAsync` |
| `GET /api/progress/weakest-words` | ProgressController | `GetWeakestWordsAsync` |
| `GET /api/progress/words/{wordId}` | ProgressController | `GetWordProgressAsync` |

## KNN recommendations and configuration (CURRENT)

Purpose: Profile-vector onboarding recommendations, learning-history word recommendations, personal-topic suggestions, lookup-catalog administration, vector-weight configuration, and scheduled/manual rebuilds.

- Controllers: `Features/Knn/Controllers/RecommendationsController`, `Features/Knn/Controllers/AdminKnnController`
- BLL services/ports: onboarding, learning, profile-vector builder, runtime configuration, rebuild, admin lookup, KNN cache, and admin trigger rate-limit abstractions
- DAL/infrastructure implementations: KNN profile/learning/admin lookup EF repositories, Redis KNN caches, hosted rebuild job, and in-memory admin trigger rate limiter
- Database: learning-profile lookup tables, `UserLearningProfile`, `UserTopicPreference`, topics/words, test activity, progress, and lists
- Integrations: Redis recommendation/rebuild/runtime settings caches; `.env` runtime config writer
- Important flows: the learning-profile catalog is anonymous for registration; recommendations require authentication and read `user_id`; manual rebuild is in-memory rate-limited. The singleton rebuild service creates a scope for scoped learning work. The hosted job captures its interval from startup options; vector weights alone use the non-expiring runtime-settings fallback and can be resolved through the watched `.env`/Redis path. Topic recommendation cache TTL uses onboarding configuration; word recommendation cache TTL uses `Knn:Learning:CacheTtlMinutes`.
- Authorization: recommendation endpoints require authentication except `learning-profile-options`; `/api/admin/knn/**` requires the Admin policy.

| Endpoint | Controller | Service/use case |
|---|---|---|
| `GET /api/recommendations/topics` | RecommendationsController | `RecommendTopicsAsync` |
| `GET /api/recommendations/words` | RecommendationsController | `GetWordRecommendationsAsync` |
| `GET /api/recommendations/personal-topics` | RecommendationsController | `RecommendPersonalTopicsAsync` |
| `GET /api/recommendations/learning-profile-options` | RecommendationsController | `GetLearningProfileOptionsAsync` |
| `PUT /api/recommendations/topics/selection` | RecommendationsController | `SelectTopicsAsync` |
| `POST /api/recommendations/topics/{topicId}/accept` | RecommendationsController | `AcceptTopicAsync` |
| `GET /api/admin/knn/config` | AdminKnnController | effective KNN config/status |
| `PUT /api/admin/knn/config/vector-weights` | AdminKnnController | `UpdateVectorWeightsAsync` |
| `POST /api/admin/knn/config/vector-weights/reset` | AdminKnnController | `ResetVectorWeightsAsync` |
| `GET /api/admin/knn/rebuild-status` | AdminKnnController | `GetStatusAsync` |
| `POST /api/admin/knn/trigger-rebuild` | AdminKnnController | `TriggerRebuild` |

Admin lookup CRUD repeats this route set for each of `age-ranges`, `regions`, `occupations`, `education-levels`, and `learning-purposes`:

| Endpoint pattern | Controller | Service/use case |
|---|---|---|
| `GET /api/admin/knn/{lookup}` | AdminKnnController | paged lookup list |
| `GET /api/admin/knn/{lookup}/{id}` | AdminKnnController | lookup detail |
| `POST /api/admin/knn/{lookup}` | AdminKnnController | create lookup |
| `PUT /api/admin/knn/{lookup}/{id}` | AdminKnnController | update lookup |
| `DELETE /api/admin/knn/{lookup}/{id}` | AdminKnnController | soft-delete lookup |
| `PATCH /api/admin/knn/{lookup}/{id}/restore` | AdminKnnController | restore lookup |

## Notifications (CURRENT)

Purpose: Derive user notifications from deleted words referenced by user lists. There is no notification table and no server-side read mutation.

- Controller: `NotificationsController`
- Service/repository: `INotificationService`/`NotificationService`, `INotificationRepository`/`NotificationRepository`
- Layer ownership: Presentation, BLL, and feature repository/mapping code live under `Features/Notifications`
- Database: deleted `Word` references in active `UserListWord` rows or any `UserWordProgress` row; current provider remains MySQL/Pomelo
- Endpoint: `GET /api/notifications` -> `ListAsync`
- Client behavior: Mobile tracks read notification IDs locally per device.
- Authorization: the endpoint requires an authenticated user.

## Admin reporting and user management (CURRENT)

Purpose: Dashboard statistics, demographics, learning/activity trends, audit-log browsing, user detail/history/topics, and role-aware account lock/restore.

- Controllers: `AdminStatsController`, `AdminUsersController`
- Services: admin stats and user services
- Repositories: admin stats and user repositories
- Database: users/profiles/auth/roles, sessions/answers/progress, topics/preferences, refresh tokens, and audit logs
- Integrations: short-lived ASP.NET memory cache for statistics and five-minute Redis profile-cache invalidation
- Authorization: all endpoints require the Admin policy; service rules remain role-aware for lock/restore operations.

| Endpoint | Controller | Service/use case |
|---|---|---|
| `GET /api/admin/stats/dashboard` | AdminStatsController | dashboard totals |
| `GET /api/admin/stats/demographics` | AdminStatsController | demographics |
| `GET /api/admin/stats/learning` | AdminStatsController | learning statistics |
| `GET /api/admin/stats/sessions-trend` | AdminStatsController | session trend |
| `GET /api/admin/stats/mastery-distribution` | AdminStatsController | mastery distribution |
| `GET /api/admin/stats/activity-trend` | AdminStatsController | activity trend |
| `GET /api/admin/audit-logs` | AdminStatsController | paged audit logs |
| `GET /api/admin/users` | AdminUsersController | user list |
| `GET /api/admin/users/{id}` | AdminUsersController | user detail |
| `GET /api/admin/users/{id}/test-history` | AdminUsersController | test history |
| `GET /api/admin/users/{id}/topics` | AdminUsersController | topic preferences |
| `PATCH /api/admin/users/{id}/deactivate` | AdminUsersController | role-aware lock |
| `PATCH /api/admin/users/{id}/restore` | AdminUsersController | role-aware restore |

## AI grading administration (CURRENT)

Purpose: Read/update/reset effective Gemini settings and test provider connectivity. Admin changes prefer rewriting the repository `.env`; Redis is the fallback runtime store when the file cannot be written.

- Controller: `Features/AiGrading/Controllers/AdminAiGradingController`
- Services: BLL-owned `IAiGradingConfigurationService`/`AiGradingConfigurationService` and `IAiGradingProvider`; shared Infrastructure implements the provider through `GeminiAiGradingProvider`/`GeminiClient`
- Layer ownership: HTTP Contracts/mappings are feature Presentation, configuration and grading behavior are feature BLL, the MySQL cache repository is feature DAL, and Gemini/runtime configuration adapters remain shared Infrastructure.
- Integrations: watched `.env` runtime configuration, Redis fallback store, Gemini test request, and the seven-day MySQL AI-result cache
- Authorization: all endpoints require the Admin policy.

| Endpoint | Controller | Service/use case |
|---|---|---|
| `GET /api/admin/settings/ai-grading` | AdminAiGradingController | read effective configuration |
| `PUT /api/admin/settings/ai-grading` | AdminAiGradingController | update configuration |
| `POST /api/admin/settings/ai-grading/reset` | AdminAiGradingController | reset configuration |
| `POST /api/admin/settings/ai-grading/test` | AdminAiGradingController | test Gemini connectivity |

## SuperAdmin accounts and roles (CURRENT)

Purpose: Manage admin accounts and custom roles, protect built-in roles and SuperAdmin accounts, revoke tokens after sensitive changes, and assign/remove roles.

- Controllers: `Features/SuperAdmin/Controllers/SuperAdminAccountsController` and `RolesController`
- Services: BLL-owned `ISuperAdminAccountService`/`SuperAdminAccountService` and `IRoleManagementService`/`RoleManagementService`
- Repositories: BLL-owned `ISuperAdminAccountRepository` and `IRoleManagementRepository` implemented by feature DAL repositories. Mutations use the shared `IApplicationTransactionManager`, commit relational changes before profile-cache invalidation, and no SuperAdmin BLL service directly references `VocaNovaDbContext`.
- Database: `User`, `UserAuth`, `UserProfile`, `Role`, `RefreshToken`
- Integrations: Redis profile-cache invalidation
- Authorization: all endpoints require the SuperAdmin policy.

| Endpoint group | Controller | Service/use case |
|---|---|---|
| `GET/POST /api/superadmin/admins` | SuperAdminAccountsController | list/create admins |
| `GET/PUT/DELETE /api/superadmin/admins/{id}` | SuperAdminAccountsController | detail/update/delete admin |
| `PATCH /api/superadmin/admins/{id}/lock|unlock` | SuperAdminAccountsController | lock/unlock admin |
| `GET/POST /api/superadmin/roles` | RolesController | list/create roles |
| `PUT/DELETE /api/superadmin/roles/{roleId}` | RolesController | update/delete role |
| `GET /api/superadmin/roles/{roleId}/users` | RolesController | role members |
| `POST/DELETE /api/superadmin/roles/{roleId}/users/{userId}` | RolesController | assign/remove role |

## Health (CURRENT)

`GET /health` is a minimal endpoint in `Program.cs` and returns the standard success envelope with service status. It does not test MySQL, Redis, or external-provider readiness.

## Current ownership after refactoring

Endpoint behavior remains as cataloged above. Each production API feature now uses `Features/<Feature>/{Controllers,Contracts,Mappings,BLL,DAL}` as needed. Public inputs/outputs use `Contracts/Requests|Responses`; repository interfaces and other required ports remain in the feature BLL; feature-specific repository implementations/mappings live in its DAL; shared EF/Redis/provider infrastructure stays consolidated under `Infrastructure`. No endpoint was added, deleted, or renamed merely because its implementation moved.
