# Refactor Plan

This is an incremental implementation plan from the verified CURRENT architecture to the accepted TARGET architecture. It does not authorize source moves, database changes, package changes, or Docker implementation by itself.

## Transition

| CURRENT | TARGET |
|---|---|
| Mixed feature folders with incomplete boundaries plus shared `Infrastructure` | Corrected `Features/<Feature>/{Controllers,Contracts,Mappings,BLL,DAL}` slices plus consolidated shared `Infrastructure` |
| Mixed HTTP/business/persistence DTO ownership | Request/Response Contracts, BLL models, DAL entities/provider models |
| Incomplete logical layer boundaries | Presentation -> BLL abstractions; DAL -> BLL abstractions |
| Redis/provider interfaces owned with Infrastructure | BLL-owned ports with DAL implementations |
| Compose-backed Docker foundation | Operational Compose-backed MySQL/Pomelo persistence |

Public routes, methods, authorization, HTTP behavior, response envelopes, and JSON schemas remain compatibility surfaces throughout migration.

## Verified CURRENT boundary problems

- The ordered feature folders now use feature-first Presentation/BLL/DAL slices. Progress, KNN, Quiz, and SuperAdmin test compatibility adapters have been retired; remaining test-local compatibility surfaces are Auth `CompatAuthRepository`, Dictionary and Lists compatibility adapters, and broad aliases in `GlobalUsings.cs`.
- Cross-feature helpers still include framework-specific primitives, including `Common/Results/Result.cs` status-code mapping and `Common/Extensions/QueryableExtensions.cs` EF Core paging support.
- Some Presentation controllers still reference shared authentication/auditing support under `Infrastructure`; the feature BLL boundaries are provider/framework independent.
- Feature BLL/DAL registrations are grouped behind `AddBLL()` and `AddDAL(configuration)` in `DependencyInjection/VocaNovaServiceCollectionExtensions.cs`; `Program.cs` still owns HTTP framework setup and middleware order.
- Dashboard has client-local wire/view models and a broad API client; it correctly has no API reference/database access.
- Mobile REST gateways are named `*ApiService` under feature `data/services`, with wire/cache DTOs under `data/dtos` and app-domain types under `domain/models`.
- MySQL/Pomelo/database-first are operational and are now the accepted long-term persistence direction. Docker/Compose foundation exists with a MySQL service aligned to that target; schema creation remains database-first and is not automated by Compose.

### Remaining-feature audit (CURRENT)

- Lists/personal topics: controllers use Bearer authorization and the `user_id` claim; public DTOs retain their existing explicit JSON names and paged responses. Reads and ordinary single-row writes are comparatively small, but random additions save once per selected word and personal-topic creation can save the reserved `__topic__:` list before saving membership. Those write paths have no encompassing transaction and can partially complete. The 10-minute `user-lists:v2:{userId}` cache and its mutation invalidation are compatibility constraints.
- Dictionary administration: the write/admin side now uses the feature-first Controllers/Contracts/Mappings/BLL/DAL slice and BLL-owned repository/storage/cache ports. Word/sense creation with examples, topic/link writes, and CSV rows still cross the same independent saves; CSV import retains row-level outcomes. Word writes invalidate detail entries but do not invalidate the 5-minute word-search cache. Sense delete/restore now uses the reviewed MySQL `word_senses.status` soft-delete state.
- Quiz/AI grading: Quiz owns session, pool, question, submission, result/history, wrong-word, SRS, grader, AI-cache, and Gemini paths. Answer, session, and SM-2 progress changes are staged into one EF save, but an AI-cache hit-counter update or new Gemini-result write can save independently before it. Redis quiz pools use `quiz-pool:{sessionId}:{listId|all}` with a two-hour TTL; AI results produced by Gemini use a seven-day MySQL cache, and provider failure falls back to normalized exact matching.
- KNN/runtime configuration: recommendation, profile-vector, learning-history, admin lookup, runtime-setting, rebuild, and hosted-job paths now use the feature-first KNN slice with BLL-owned ports and DAL/infrastructure implementations. Redis topic and word recommendation keys are user-scoped; word recommendation TTL now uses `Knn:Learning:CacheTtlMinutes`. Vector weights use the non-expiring `runtime-settings:knn:vector-weights` fallback. The singleton rebuild service creates scopes for scoped repositories. The hosted job captures its interval from startup options, while only vector weights are read through the runtime settings service; changing other KNN values at runtime is not equivalent to changing vector weights.
- Auth: feature-first Auth is live. `AuthService` uses BLL-owned repository/provider/cache/rate-limit ports plus the shared `IApplicationTransactionManager`; profile cache entries use `user:{userId}` with a five-minute TTL.
- Admin/SuperAdmin: feature-first Admin/SuperAdmin is live. Admin reporting/user-management uses BLL-owned repository ports; status changes stage account state and token revocation in one save and invalidate the profile cache. SuperAdmin account and role services use BLL-owned repository ports and the shared transaction manager for multi-entity mutations, then invalidate affected profile caches after commit.
- Consumers: Mobile directly consumes Auth, Lists/personal topics, Quiz, and KNN recommendation/onboarding routes through feature data `*ApiService` classes. Dashboard directly consumes Dictionary administration, KNN/runtime settings, AI-grading settings, Admin users/statistics, and SuperAdmin account/role routes through its API client and feature controllers/models. These manually maintained consumers must be included in contract verification for each slice.

## Staged migration

### Phase 0 - Documentation and architecture baseline

- Goal: freeze accepted decisions and verified current behavior.
- Changes: synchronize documents; record routes/contracts/authorization and current persistence/runtime configuration.
- Compatibility: no application changes.
- Verification: documentation audit, source comparison, Markdown-only diff.
- Risk/rollback: revert documentation only if a claim is disproved; do not alter source to make a claim true.

### Phase 1 - Canonical skeleton and architecture tests

- Goal: create enforceable layer boundaries without moving all features.
- Changes: add empty/minimal target folders as required by the first pilot, dependency tests, and framework-neutral BLL result/error primitives.
- Compatibility: keep legacy registration and namespaces operational during transition.
- Verification: solution build/tests plus tests forbidding BLL references to DAL/EF/ASP.NET/provider SDKs.
- Risk/rollback: avoid empty abstraction inventory; introduce only structures used by the pilot.

Status: Completed for the first meaningful slice. Notifications introduced only the layer folders and framework-neutral primitives it uses. Architecture tests enforce that BLL does not reference Presentation, DAL, Infrastructure, EF Core, or ASP.NET Core. Global registration wrappers were later introduced after the ordered feature slices were migrated.

### Phase 2 - One small feature end-to-end

- Goal: validate the target pattern using Notifications or another low-risk feature.
- Changes: establish `Features/<Feature>` Presentation/BLL/DAL boundaries, split Contracts/BLL models, create a BLL service/port and DAL repository/mapping, and retain transitional DI.
- Compatibility: preserve exact route, method, authorization, envelope, and JSON fields.
- Verification: route/contract snapshots, mapping/unit/integration tests, Dashboard/Mobile consumer tests where applicable.
- Risk/rollback: retain the prior implementation behind a small reversible change until equivalence is proven.

Status: Completed with Notifications and re-homed under ADR-018. The controller, Request/Response Contracts, Presentation mappings, BLL service/models/result/persistence port, DAL repository/mapping, and transitional DI are in the corrected feature-first slice. Focused tests cover behavior, EF query semantics, authentication, JSON/pagination compatibility, and dependency direction. MySQL/Pomelo, the existing schema, and Docker configuration were intentionally unchanged.

### Phase 3 - Extract Contracts and BLL models feature by feature

- Goal: remove ambiguous DTO ownership.
- Changes: classify every current DTO as Request, Response, BLL model/command, DAL projection/provider model, cache representation, or internal type; add Presentation mappings.
- Compatibility: do not rename wire fields or routes merely because CLR types move.
- Verification: serialization and controller tests plus manual-client model comparison.
- Risk/rollback: migrate one feature at a time; shared DTOs may require temporary adapters.

Status: Completed for the ordered feature units. Notifications, Progress, all Dictionary and Lists/personal-topic endpoints, Quiz/AI grading, KNN/runtime configuration, Auth, and Admin/SuperAdmin have explicit Request/Response Contracts, Presentation mappings, and framework-neutral BLL models/results. Existing request DTOs used mixed snake_case and legacy camelCase query names; migrated Contracts preserve the actual wire names rather than normalizing them opportunistically.

### Phase 4 - Move abstractions to BLL and implementations to DAL

- Goal: invert dependencies correctly.
- Changes: place persistence/cache/auth/storage/provider ports in `BLL/Abstractions`; move EF/Redis/provider implementations under DAL; add DAL mapping.
- Compatibility: preserve cache keys/TTL/invalidation and provider behavior.
- Verification: architecture tests, feature tests, Redis-unavailable tests, provider contract tests.
- Risk/rollback: do not mechanically move interfaces that expose entities/Contracts; redesign their signatures with adapters.

Status: Completed for the ordered feature units. Notifications owns its persistence port in BLL with an EF implementation in DAL. Progress owns summary/analytics persistence ports and its summary-cache port in BLL, with EF and Redis implementations in DAL. Dictionary reads and administration own persistence, cache, and storage ports in BLL, with EF, Redis, and Cloudinary implementations outside BLL. Lists/personal-topic queries and mutations own their persistence and user-list cache ports in BLL, with EF and Redis implementations in DAL. Quiz/AI grading, KNN/runtime configuration, Auth, and Admin/SuperAdmin also use BLL-owned repository/provider/cache ports with DAL/shared Infrastructure implementations. Existing Progress, Dictionary, Lists, Quiz, AI-grading, and KNN cache keys, TTLs, payloads, unavailable-Redis fallback, and applicable invalidation behavior are protected by tests.

### Phase 5 - Remove EF/infrastructure dependencies from BLL

- Goal: make BLL framework/provider independent.
- Changes: replace direct DbContext in Auth/SuperAdmin with BLL ports and an explicit transaction boundary; replace `IFormFile`, ASP.NET status codes, EF entities, and infrastructure-owned settings/ports in services.
- Compatibility: preserve auth atomicity, token rotation, upload behavior, quiz/SRS consistency, and error-to-HTTP mappings.
- Verification: dependency tests, security/auth tests, transaction-sensitive tests, full .NET suite.
- Risk/rollback: Auth and quiz are high risk; migrate after smaller features establish the pattern.

### Phase 6 - PostgreSQL persistence preparation (not applicable)

Status: Not applicable. ADR-016 keeps MySQL/Pomelo/database-first as both CURRENT and TARGET, so no PostgreSQL provider/schema/data transition will be designed.

### Phase 7 - MySQL-to-PostgreSQL cutover (not applicable)

Status: Not applicable. The provider cutover and Code First migration workflow were superseded by ADR-016.

### Phase 8 - Introduce Dockerfiles and Docker Compose

- Goal: provide the accepted backend environment.
- Changes: API/Dashboard Dockerfiles, `.dockerignore`, Compose services `mysql`, `redis`, `api`, `dashboard`, health checks, dependencies, exposed ports, and the named `mysql_data` volume.
- Compatibility: preserve non-Docker local execution; external providers remain HTTPS dependencies; Mobile remains outside Docker.
- Verification: `docker compose up --build`, `ps`, health checks, Dashboard -> API, API -> MySQL/Redis, persistence across app-container recreation.
- Risk/rollback: keep host workflow documented until container workflow is stable; never bake secrets into images.

Status: Completed for Docker alignment. Dockerfiles, Compose, health checks, service DNS, exposed ports, non-persistent Redis, `mysql`, `mysql_data`, and the API's existing MySQL environment keys are implemented. Compose does not create VocaNova tables; loading an existing compatible schema remains an operational database-first step.

### Phase 9 - Update Dashboard/Mobile integration configuration

- Goal: make clients select appropriate host/container endpoints.
- Changes: Dashboard uses internal `api:<port>` in Compose; Mobile documentation/configuration uses host-exposed API addresses for emulator/device environments.
- Compatibility: retain current local launch-profile support.
- Verification: Dashboard auth/refresh and feature calls in Compose; Mobile calls exposed API from emulator/physical device.
- Risk/rollback: distinguish browser/host/container DNS contexts to avoid `localhost` mistakes.

### Phase 10 - Retire MySQL artifacts (not applicable)

Status: Not applicable. Pomelo, MySQL environment keys, `scripts/scaffold-mysql.ps1`, and the MySQL schema workflow remain required by the accepted target.

### Phase 11 - Final verification and documentation synchronization

- Goal: demonstrate completion of the accepted architecture.
- Changes: remove transitional namespaces/adapters/registrations; finish canonical paths; update every CURRENT statement.
- Compatibility: compare route/JSON/authorization snapshots and all client consumers.
- Verification: architecture tests, full .NET and Flutter checks, MySQL/Redis/Docker integration, git/documentation audit.
- Risk/rollback: remove compatibility adapters only after no consumers remain.

## Feature migration order

A practical order was Notifications/Progress, public Dictionary reads, read-only Lists/personal-topic queries, single-save Lists mutations, Dictionary administration, Quiz/AI grading, KNN/runtime configuration, Auth, then Admin/SuperAdmin. All ordered feature migration units are implemented in source, Docker MySQL Compose alignment is complete, and `AddBLL()`/`AddDAL(configuration)` grouping is CURRENT. Final cleanup remains for retirement of the remaining test compatibility surfaces after they are no longer needed.

## Executable specification for the remaining migration units (TARGET)

The seven units below are ordered implementation gates. A later session implements one unit, completes its checklist, removes only the legacy types made unreachable by that unit, and then proceeds. Every listed path is relative to `src/VocaNova.API` and follows ADR-018's corrected feature-first layout. Notifications, Progress, public Dictionary reads, and Lists Units 1-2 have already been re-homed. All endpoints continue using the shared `{ success, data, message, errors, pagination? }` envelope; legacy `PagedResult<T>` values remain inside `data` unless a listed controller already uses envelope-level pagination.

### Execution status (CURRENT)

| Unit | Status | Reason |
|---|---|---|
| 1. Read-only Lists/personal-topic queries | Completed | Implemented with the resolved BLL lookup-result design and re-homed under ADR-018; full .NET and Mobile compatibility suites are green. |
| 2. Single-save Lists mutations | Completed | Implemented and re-homed under ADR-018. CURRENT independent-save ordering and partial-write risk are deliberately preserved; no Lists transaction design was accepted or introduced. |
| 3. Dictionary administration | Completed | Feature-first administration is live; the reviewed WordSense status SQL was applied and re-scaffolded, and 537/537 .NET tests pass. |
| 4. Quiz and AI grading | Completed | Feature-first Quiz/AI grading is live with transaction, cache, provider, route, and client compatibility verified. |
| 5. KNN/runtime configuration | Completed | Feature-first KNN/runtime configuration is live; routes/contracts/cache keys/runtime vector behavior are preserved, and word recommendation TTL now uses `Knn:Learning:CacheTtlMinutes`. |
| 6. Auth | Completed | Feature-first Auth is live with BLL-owned persistence/provider/cache/rate-limit ports, EF transaction manager, route/JSON/envelope compatibility, and 572/572 .NET tests passing. |
| 7. Admin/SuperAdmin | Completed | Feature-first Admin/SuperAdmin is live. The remaining Admin BLL EF-entity leak was removed, Admin repository namespaces now match their BLL/DAL folders, and Admin/SuperAdmin architecture tests cover routes, service boundaries, DAL port implementation, and BLL source independence. |

#### ADR-018 re-home outcome (CURRENT)

- Completed on 2026-08-20 before Unit 3. Notifications, Progress, public Dictionary reads, and Lists Units 1-2 moved from the superseded top-level layer roots into `Features/<Feature>/{Controllers,Contracts,Mappings,BLL,DAL}`.
- Repository/cache ports remain BLL-owned. Feature repository implementations/mappings moved into feature DAL slices; Redis cache implementations moved under shared `Infrastructure/Caching`; cross-feature `PagedCollection` moved to `Common/Models`.
- No route, method, authorization, status/message/envelope, JSON field, cache key/TTL/invalidation, database, provider, or transaction behavior changed. Unit 2's two non-atomic partial-write sequences remain protected by regression tests.
- Pre-move verification passed 518 .NET tests. Post-move verification passed 524 tests, including feature-first layout and per-feature BLL boundary assertions. Unit 3 subsequently passed 537 tests; Unit 4 passed 565 tests. Unit 5 passed 569 .NET tests and the full Flutter suite. Unit 6 now passes 572 .NET tests.

### Unit 1 - Read-only Lists and personal-topic queries

#### Target file manifest

```text
Features/Lists/Controllers/ListsController.cs
Features/Lists/Controllers/PersonalTopicsController.cs
Features/Lists/Contracts/Requests/ListWordsRequest.cs
Features/Lists/Contracts/Requests/PersonalTopicListRequest.cs
Features/Lists/Contracts/Responses/UserListResponse.cs
Features/Lists/Contracts/Responses/ListWordResponse.cs
Features/Lists/Contracts/Responses/PersonalTopicResponse.cs
Features/Lists/Mappings/ListQueryMappings.cs
Features/Lists/BLL/Models/ListQueries.cs
Features/Lists/BLL/Models/UserListSummary.cs
Features/Lists/BLL/Models/ListWord.cs
Features/Lists/BLL/Models/PersonalTopic.cs
Features/Lists/BLL/Models/ListResult.cs
Features/Lists/BLL/Services/IListQueryService.cs
Features/Lists/BLL/Services/ListQueryService.cs
Features/Lists/BLL/Services/IPersonalTopicQueryService.cs
Features/Lists/BLL/Services/PersonalTopicQueryService.cs
Features/Lists/BLL/Abstractions/IListQueryRepository.cs
Features/Lists/BLL/Abstractions/IPersonalTopicQueryRepository.cs
Features/Lists/BLL/Abstractions/IUserListCache.cs
Features/Lists/DAL/Repositories/ListQueryRepository.cs
Features/Lists/DAL/Repositories/PersonalTopicQueryRepository.cs
Features/Lists/DAL/Mappings/ListPersistenceMappings.cs
Infrastructure/Caching/Lists/UserListCacheEntry.cs
Infrastructure/Caching/Lists/RedisUserListCache.cs
```

During this unit the target controllers contain only their GET actions; the legacy controllers retain non-conflicting mutation actions. Unit 2 moves those actions into the same target controller files and removes the legacy controllers.

#### Current-type disposition

| CURRENT type | TARGET disposition |
|---|---|
| `ListWordsQuery` | HTTP `ListWordsRequest`; Presentation maps to BLL `ListWordsQuery`. Preserve `page`, `limit`. |
| implicit personal-topic `wordId` query parameter | HTTP `PersonalTopicListRequest` with `wordId`; map to BLL `PersonalTopicQuery`. |
| `UserListDto` | BLL `UserListSummary`, HTTP `UserListResponse`, DAL `UserListCacheEntry`. Preserve `list_id`, `list_name`, `word_count`, `created_at`. |
| `ListWordDto` | BLL `ListWord`, HTTP `ListWordResponse`. Preserve `word_id`, `word`, `primary_meaning`, `correct_count`, `wrong_count`, `note`, `added_at`. |
| `PersonalTopicDto` | BLL `PersonalTopic`, HTTP `PersonalTopicResponse`. Preserve `topic_id`, `list_id`, `name`, `name_vi`, `icon`, `word_count`, `contains_word`. |
| `UserListOwnershipDto` used by read ownership checks | BLL `ListOwnership` in `ListQueries.cs`; the type is introduced in Unit 1 and reused by Unit 2. It is never serialized. |
| `IUserListService.GetByUserAsync/GetWordsAsync` | `IListQueryService.GetListsAsync/GetWordsAsync`; the remaining mutation members move in Unit 2. |
| `IPersonalTopicService.GetTopicsAsync/GetWordsAsync` | `IPersonalTopicQueryService.GetTopicsAsync/GetWordsAsync`; mutation members move in Unit 2. |
| read members of `IUserListRepository` | `IListQueryRepository`; no HTTP types in signatures. |
| read members of `IPersonalTopicRepository` | `IPersonalTopicQueryRepository`; no HTTP types in signatures. |
| `UserListRepository`, `PersonalTopicRepository` read members | `ListQueryRepository`, `PersonalTopicQueryRepository`; mutation members move to Unit 2 implementations. |
| `IUserListCache`/`RedisUserListCache` | BLL-owned `IUserListCache` and DAL `RedisUserListCache`; cache payload becomes `UserListCacheEntry`. |
| `ListsController`, `PersonalTopicsController` GET actions | feature Presentation controllers listed above; `[Authorize]` and `user_id` extraction remain Presentation behavior. |

#### BLL ports and service surface

```csharp
IListQueryService.GetListsAsync(uint userId, CancellationToken) -> ListResult<IReadOnlyCollection<UserListSummary>>
IListQueryService.GetWordsAsync(uint userId, uint listId, ListWordsQuery query, CancellationToken) -> ListResult<PagedCollection<ListWord>>
IPersonalTopicQueryService.GetTopicsAsync(uint userId, PersonalTopicQuery query, CancellationToken) -> ListResult<IReadOnlyCollection<PersonalTopic>>
IPersonalTopicQueryService.GetWordsAsync(uint userId, uint topicId, ListWordsQuery query, CancellationToken) -> ListResult<PagedCollection<ListWord>>

IListQueryRepository.GetListsAsync(uint userId, CancellationToken) -> IReadOnlyCollection<UserListSummary>
IListQueryRepository.GetOwnedListWordsAsync(uint userId, uint listId, int page, int limit, CancellationToken) -> ListLookupResult<PagedCollection<ListWord>>
IPersonalTopicQueryRepository.GetTopicsAsync(uint userId, uint? containsWordId, CancellationToken) -> ListLookupResult<IReadOnlyCollection<PersonalTopic>>
IPersonalTopicQueryRepository.GetTopicWordsAsync(uint userId, uint topicId, int page, int limit, CancellationToken) -> ListLookupResult<PagedCollection<ListWord>>
IUserListCache.GetAsync(uint userId, CancellationToken) -> IReadOnlyCollection<UserListSummary>?
IUserListCache.SetAsync(uint userId, IReadOnlyCollection<UserListSummary> lists, CancellationToken)
IUserListCache.RemoveAsync(uint userId, CancellationToken)
```

`Features/Lists/BLL/Models/ListResult.cs` contains both framework-neutral result shapes. The literal source precedents are `Features/Dictionary/BLL/Models/DictionaryResult.cs` and `Features/Progress/BLL/Models/ProgressResult.cs`: each defines an `ErrorKind` enum plus a sealed generic result with a private constructor, `IsSuccess`, `Value`, `Error`, `ErrorKind`, and named static factories. `Features/Notifications/BLL/Models/NotificationListResult.cs` is the earlier non-generic/single-error-kind variant of the same private-constructor and named-factory convention; Lists uses the Dictionary/Progress shape because Presentation must distinguish multiple failures.

```csharp
enum ListErrorKind { Validation, Unauthorized, NotFound, Forbidden }
ListResult<T>.Success(T value)
ListResult<T>.ValidationFailure(string error)
ListResult<T>.Unauthorized(string error)
ListResult<T>.NotFound(string error)
ListResult<T>.Forbidden(string error)

enum ListLookupErrorKind { ListNotFound, ListForbidden, WordNotFound, TopicNotFound }
ListLookupResult<T>.Success(T value)
ListLookupResult<T>.ListNotFound()
ListLookupResult<T>.ListForbidden()
ListLookupResult<T>.WordNotFound()
ListLookupResult<T>.TopicNotFound()
```

The lookup result is BLL-owned and contains no HTTP status. DAL reports the explicit persistence outcome; BLL maps `ListNotFound`, `WordNotFound`, and `TopicNotFound` to `ListErrorKind.NotFound` with the exact CURRENT messages, and maps `ListForbidden` to `ListErrorKind.Forbidden` with `You do not have access to this list.` Presentation maps `ListErrorKind` to 400/401/404/403 exactly as the existing Progress controller maps its BLL error kinds.

`GetOwnedListWordsAsync` returns `ListNotFound` for zero, missing, deleted, or reserved list IDs; `ListForbidden` for an active ordinary list owned by another user; and `Success` only for the owner's active ordinary list. `IPersonalTopicQueryRepository.GetTopicsAsync` returns `WordNotFound` when a supplied `containsWordId` is not an active word, while a valid word that appears in none of the user's personal-topic lists returns `Success` with normal topics whose `ContainsWord` values are false. `GetTopicWordsAsync` returns `TopicNotFound` only when the active system topic does not exist; an existing topic with no personal list returns a successful empty page.

DAL owns EF projection and cache serialization; BLL owns pagination validation, lookup-outcome-to-business-error mapping, and exact messages.

DI remains: query services and EF repositories scoped; `RedisUserListCache` singleton.

#### Resolved design decision - query failure signals

Status: Resolved.

- CURRENT behavior is confirmed from source: list reads return `404 List not found.` for zero, missing, deleted, or reserved lists and `403 You do not have access to this list.` for foreign active ordinary lists; personal-topic filtering returns `404 Word not found.` for a supplied invalid/inactive word ID.
- DECISION: use the BLL-owned `ListLookupResult<T>` and `ListLookupErrorKind` shape above at both query repository boundaries. This preserves distinct lookup states without ASP.NET status codes in BLL or DAL contracts.
- Unit 2 reuses the same result for mutation ownership lookup so its ownership checks cannot collapse 403 and 404.

#### Compatibility gate

- Preserve `[Authorize]`, the `user_id` claim, and exact GET routes: `/api/lists`, `/api/lists/{id:uint}/words`, `/api/personal-topics`, `/api/personal-topics/{topicId:uint}/words`.
- Preserve query names/defaults: `page`, `limit`, and `wordId`. Preserve all JSON names listed in the disposition table and keep paged objects inside `data`.
- Preserve Redis key `${Redis.InstanceName}user-lists:v2:{userId}`, 10-minute TTL, snake_case payload, and null/no-op behavior when Redis connection fails.
- Mobile compatibility files now live under `lib/features/lists/data/services/lists_api_service.dart`, with DTOs under `lib/features/lists/data/dtos`, domain models under `lib/features/lists/domain/models`, their notifier/screen consumers, and dictionary add-to-list callers.

#### Test gate

- Add `tests/VocaNova.Tests/Architecture/ListQueryArchitectureTests.cs` for Presentation -> BLL and DAL -> BLL direction and forbidden BLL dependencies.
- Split/extend `UserListFeatureTests.cs` and `PersonalTopicFeatureTests.cs` with GET route, `[Authorize]`, missing/invalid `user_id`, status/message, JSON-name, pagination-in-`data`, ownership/not-found, EF query, cache hit/miss/set, exact key/TTL/payload, and Redis-unavailable tests.
- Add `tests/VocaNova.Tests/Lists/ListControllerContractTests.cs` and `ListCacheCompatibilityTests.cs`.
- Run Mobile `lists_crud_api_service_test.dart`, `lists_api_service_test.dart`, Lists notifier tests, and Lists screen tests before Unit 2.

#### Implementation outcome (CURRENT)

- Completed on 2026-08-20. The Unit 1 manifest is implemented for GET behavior; Unit 2 has since added mutation actions to the same canonical controllers and completed the Lists migration.
- Legacy endpoint-facing read service members, GET controller actions, unreferenced repository read members, `ListWordsQuery`, and the Infrastructure-owned user-list cache were removed after the canonical query path was verified. Unit 2 subsequently migrated the remaining repository behavior and removed `Features/Lists`.
- Architecture, BLL/DAL behavior, controller contract, and Redis compatibility tests are present. `dotnet build VocaNova.sln` and all 500 .NET tests passed; all selected Mobile Lists API-service/notifier/screen tests passed unchanged after the Mobile rename.

### Unit 2 - Single-save Lists mutations

#### Target file manifest

```text
Features/Lists/Controllers/ListsController.cs                         # add mutation actions to Unit 1 file
Features/Lists/Controllers/PersonalTopicsController.cs                 # add mutation actions to Unit 1 file
Features/Lists/Contracts/Requests/CreateListRequest.cs
Features/Lists/Contracts/Requests/UpdateListRequest.cs
Features/Lists/Contracts/Requests/AddListWordRequest.cs
Features/Lists/Contracts/Requests/AddRandomListWordsRequest.cs
Features/Lists/Contracts/Requests/UpdateListWordNoteRequest.cs
Features/Lists/Contracts/Requests/AddPersonalTopicWordRequest.cs
Features/Lists/Contracts/Responses/AddRandomListWordsResponse.cs
Features/Lists/Contracts/Requests/CreateListRequestValidator.cs
Features/Lists/Contracts/Requests/UpdateListRequestValidator.cs
Features/Lists/Contracts/Requests/AddListWordRequestValidator.cs
Features/Lists/Contracts/Requests/AddRandomListWordsRequestValidator.cs
Features/Lists/Contracts/Requests/UpdateListWordNoteRequestValidator.cs
Features/Lists/Contracts/Requests/AddPersonalTopicWordRequestValidator.cs
Features/Lists/Mappings/ListMutationMappings.cs
Features/Lists/BLL/Models/ListCommands.cs
Features/Lists/BLL/Models/ListMutationModels.cs
Features/Lists/BLL/Services/IListMutationService.cs
Features/Lists/BLL/Services/ListMutationService.cs
Features/Lists/BLL/Services/IPersonalTopicMutationService.cs
Features/Lists/BLL/Services/PersonalTopicMutationService.cs
Features/Lists/BLL/Abstractions/IListMutationRepository.cs
Features/Lists/BLL/Abstractions/IPersonalTopicMutationRepository.cs
Features/Lists/DAL/Repositories/ListMutationRepository.cs
Features/Lists/DAL/Repositories/PersonalTopicMutationRepository.cs
Features/Lists/DAL/Mappings/ListMutationPersistenceMappings.cs
```

Unit 1 response/cache files are reused. Validators stay in Presentation because they validate HTTP Contracts.

#### Current-type disposition

| CURRENT type | TARGET disposition |
|---|---|
| `CreateListRequest` | same-named HTTP Contract -> BLL `CreateListCommand`; preserve `list_name`. |
| `UpdateListRequest` | HTTP Contract -> BLL `UpdateListCommand`; preserve `list_name`. |
| `AddListWordRequest` | HTTP Contract -> BLL `AddListWordCommand`; preserve `word_id`, `add_method`, `note`. |
| `AddRandomListWordsRequest` | HTTP Contract -> BLL `AddRandomListWordsCommand`; preserve `topic_id`, `count`, `method`. |
| `UpdateListWordNoteRequest` | HTTP Contract -> BLL `UpdateListWordNoteCommand`; preserve `note`. |
| `AddPersonalTopicWordRequest` | HTTP Contract -> BLL `AddPersonalTopicWordCommand`; preserve `word_id`, `note`. |
| `AddRandomListWordsResultDto` | BLL `AddRandomListWordsResult` + HTTP `AddRandomListWordsResponse`; preserve `added_count`, `words`. |
| `UserListOwnershipDto` | Unit 1 BLL `ListOwnership`; reused by Unit 2 and never serialized. |
| `ListWordStateDto` | internal BLL `ListWordState`; never serialized. |
| six current validators | same-named Presentation validators beside the feature Request Contracts. |
| mutation members of `IUserListService`/`UserListService` | `IListMutationService`/`ListMutationService`. |
| mutation members of `IPersonalTopicService`/`PersonalTopicService` | `IPersonalTopicMutationService`/`PersonalTopicMutationService`. |
| mutation members of the two current repositories | the two BLL ports and DAL implementations listed above. |

The concrete `UserListRepository` and `PersonalTopicRepository` cease to exist after their remaining members move to `ListMutationRepository` and `PersonalTopicMutationRepository`.

#### BLL ports and preserved mutation design

```csharp
IListMutationRepository.CreateAsync(uint userId, CreateListCommand, CancellationToken) -> UserListSummary
IListMutationRepository.CountActiveAsync(uint userId, CancellationToken) -> int
IListMutationRepository.ListNameExistsAsync(uint userId, string listName, uint? excludingListId, CancellationToken) -> bool
IListMutationRepository.GetOwnershipAsync(uint userId, uint listId, CancellationToken) -> ListLookupResult<ListOwnership>
IListMutationRepository.ActiveWordExistsAsync(uint wordId, CancellationToken) -> bool
IListMutationRepository.UpdateAsync(uint userId, uint listId, UpdateListCommand, CancellationToken) -> UserListSummary?
IListMutationRepository.SoftDeleteAsync(uint userId, uint listId, CancellationToken) -> bool
IListMutationRepository.FindListWordAsync(uint userId, uint listId, uint wordId, CancellationToken) -> ListWordState?
IListMutationRepository.AddWordAsync(uint userId, uint listId, AddListWordCommand, CancellationToken) -> ListWord
IListMutationRepository.RestoreWordAsync(uint userId, uint listId, AddListWordCommand, CancellationToken) -> ListWord?
IListMutationRepository.GetRandomTopicWordIdsAsync(uint userId, uint listId, uint? topicId, int count, CancellationToken) -> IReadOnlyCollection<uint>
IListMutationRepository.GetRandomRelationWordIdsAsync(uint userId, uint listId, string relationType, int count, CancellationToken) -> IReadOnlyCollection<uint>
IListMutationRepository.RemoveWordAsync(uint userId, uint listId, uint wordId, CancellationToken) -> bool
IListMutationRepository.UpdateNoteAsync(uint userId, uint listId, uint wordId, string? note, CancellationToken) -> ListWord?
IPersonalTopicMutationRepository.GetTopicsAsync(uint userId, uint? wordId, CancellationToken) -> IReadOnlyCollection<PersonalTopic>
IPersonalTopicMutationRepository.TopicExistsAsync(uint topicId, CancellationToken) -> bool
IPersonalTopicMutationRepository.WordBelongsToTopicAsync(uint topicId, uint wordId, CancellationToken) -> bool
IPersonalTopicMutationRepository.FindActiveListIdAsync(uint userId, uint topicId, CancellationToken) -> uint?
IPersonalTopicMutationRepository.GetOrCreateListIdAsync(uint userId, uint topicId, CancellationToken) -> uint
```

`GetOwnershipAsync` uses Unit 1's BLL-owned `ListLookupResult<T>`: zero, missing, deleted, and reserved lists yield `ListNotFound`; a foreign active ordinary list yields `ListForbidden`; only an owned active ordinary list yields `Success(ListOwnership)`. `ListMutationService` maps those outcomes to the same exact 404/403 messages as Unit 1 before invoking any mutation operation. Remaining nullable/bool mutation returns occur only after successful ownership/resource prechecks and represent the one documented operation's concurrent disappearance or success state; they do not replace the ownership result.

`ListMutationService.AddRandomWordsAsync` validates ownership/method/count and selects candidate IDs, then calls the ordinary `AddWordAsync` path for each selected word in order. Each add or restore performs its own `SaveChangesAsync` and successful cache removal. There is no encompassing transaction: an exception after an earlier word save leaves that earlier membership committed. This CURRENT partial-write behavior and save/invalidation ordering are compatibility requirements for this structural migration.

`PersonalTopicMutationService.AddWordAsync` validates topic/word membership, calls `GetOrCreateListIdAsync`, and then uses the ordinary list-membership repository methods. `GetOrCreateListIdAsync` can save a newly created `__topic__:{topicId}` list before the separate membership save. There is no encompassing transaction: a membership-save failure can leave the reserved list committed, and cache removal does not occur until membership succeeds. No uniqueness constraint or transaction behavior is introduced.

DI remains: mutation services and both EF repositories are scoped; the Unit 1 cache singleton is reused. A future change to either multi-save boundary requires a separate accepted transaction design and compatibility task.

#### Compatibility gate

- Preserve all Unit 1 constraints plus exact mutation routes and methods from the Lists table in `SERVICE_CATALOG.md`.
- Literal mutation surface: `POST /api/lists`; `PUT|DELETE /api/lists/{id:uint}`; `POST /api/lists/{id:uint}/words`; `POST /api/lists/{id:uint}/words/random`; `DELETE /api/lists/{id:uint}/words/{wordId:uint}`; `PATCH /api/lists/{id:uint}/words/{wordId:uint}/note`; `POST /api/personal-topics/{topicId:uint}/words`; `DELETE /api/personal-topics/{topicId:uint}/words/{wordId:uint}`.
- Preserve `[Authorize]`, `user_id`, current success/error status mapping, messages, request/response JSON names, `__topic__:{topicId}`, soft-delete status behavior, random count maximum, and ownership checks.
- Preserve 10-minute cache entries and exact CURRENT invalidation timing. Create/rename/delete and membership add/restore/remove invalidate after their successful save; random-add invalidates once per successfully added word; personal-topic add invalidates only after membership succeeds; note-only update does not invalidate the list-summary cache. No atomicity behavior changes.
- Protect Mobile Lists, dictionary add-to-list, personal-topic, and quiz-source consumers; no client model or route changes are permitted.

#### Test gate

- Add `ListMutationArchitectureTests.cs`, `ListMutationTransactionTests.cs`, and `ListMutationControllerContractTests.cs` under the existing test feature/architecture folders.
- Extend `UserListFeatureTests.cs` and `PersonalTopicFeatureTests.cs` for every mutation, validation error, ownership failure, restore path, soft delete, exact messages/status/envelope/JSON, and cache invalidation timing.
- Partial-write regression tests force a failure on the second random-word save and after reserved-list creation. They assert that the earlier random membership or reserved list remains committed, and that cache removal retains the CURRENT per-success timing.
- Architecture tests assert that Unit 2 BLL has no DAL, EF Core, ASP.NET Core, Infrastructure, transaction, or atomic-operation dependency. Controller tests assert every mutation route, verb, authorization rule, exact status/message/envelope, and JSON field.
- Run the same Mobile tests as Unit 1 plus dictionary add-to-list and quiz source-selection tests.

#### Implementation outcome (CURRENT)

- Completed on 2026-08-20. All Lists/personal-topic mutation actions, Contracts, validators, mappings, BLL services/models/results/ports, and DAL repositories/mappings use the corrected `Features/Lists` slice. The older mixed Lists implementation was removed only after focused equivalence checks passed; ADR-018 later re-homed the already-layered files without changing behavior.
- `ListLookupResult<ListOwnership>` preserves the distinct missing/deleted/reserved-list 404 and foreign-list 403 outcomes for mutation ownership checks. Routes, authorization, request/response JSON, messages, envelopes, soft-delete behavior, random selection behavior, and cache timing remain compatible.
- The non-atomic random-add and personal-topic get-or-create paths remain deliberately unchanged. Tests prove the earlier membership or reserved list survives an induced later save failure; this unit does not fix or conceal that partial-write risk.
- `dotnet build VocaNova.sln` passed with no warnings or errors; all 518 .NET tests and all 162 Flutter tests passed. `flutter analyze` still reports five pre-existing issues in `test/app/router/app_router_test.dart` (one dependency-info finding and four unused imports), outside Unit 2 scope.

### Unit 3 - Dictionary administration and WordSense soft delete

#### Target file manifest

```text
Features/Dictionary/Controllers/AdminWordsController.cs
Features/Dictionary/Controllers/AdminTopicsController.cs
Features/Dictionary/Contracts/Requests/AdminWordQueryRequest.cs
Features/Dictionary/Contracts/Requests/AdminTopicQueryRequest.cs
Features/Dictionary/Contracts/Requests/CreateWordRequest.cs
Features/Dictionary/Contracts/Requests/UpdateWordRequest.cs
Features/Dictionary/Contracts/Requests/ImportWordsRequest.cs
Features/Dictionary/Contracts/Requests/UploadWordImageRequest.cs
Features/Dictionary/Contracts/Requests/UpdateWordImageRequest.cs
Features/Dictionary/Contracts/Requests/UploadWordAudioRequest.cs
Features/Dictionary/Contracts/Requests/CreateSenseRequest.cs
Features/Dictionary/Contracts/Requests/UpdateSenseRequest.cs
Features/Dictionary/Contracts/Requests/SenseExampleRequest.cs
Features/Dictionary/Contracts/Requests/CreateTopicRequest.cs
Features/Dictionary/Contracts/Requests/UpdateTopicRequest.cs
Features/Dictionary/Contracts/Requests/AddTopicWordsRequest.cs
Features/Dictionary/Contracts/Responses/AdminWordListItemResponse.cs
Features/Dictionary/Contracts/Responses/AdminTopicResponse.cs
Features/Dictionary/Contracts/Responses/BulkImportResponse.cs
Features/Dictionary/Contracts/Responses/BulkImportErrorResponse.cs
Features/Dictionary/Contracts/Responses/WordAudioResponse.cs
Features/Dictionary/Contracts/Requests/CreateWordRequestValidator.cs
Features/Dictionary/Contracts/Requests/UpdateWordRequestValidator.cs
Features/Dictionary/Contracts/Requests/CreateSenseRequestValidator.cs
Features/Dictionary/Contracts/Requests/UpdateSenseRequestValidator.cs
Features/Dictionary/Contracts/Requests/CreateTopicRequestValidator.cs
Features/Dictionary/Contracts/Requests/UpdateTopicRequestValidator.cs
Features/Dictionary/Mappings/DictionaryAdminMappings.cs
Features/Dictionary/BLL/Models/DictionaryAdminQueries.cs
Features/Dictionary/BLL/Models/DictionaryAdminCommands.cs
Features/Dictionary/BLL/Models/AdminWordListItem.cs
Features/Dictionary/BLL/Models/AdminTopic.cs
Features/Dictionary/BLL/Models/BulkImportResult.cs
Features/Dictionary/BLL/Models/StoredMedia.cs
Features/Dictionary/BLL/Models/DictionaryResult.cs                 # existing result extended/reused
Features/Dictionary/BLL/Services/IWordAdminService.cs
Features/Dictionary/BLL/Services/WordAdminService.cs
Features/Dictionary/BLL/Services/ITopicAdminService.cs
Features/Dictionary/BLL/Services/TopicAdminService.cs
Features/Dictionary/BLL/Abstractions/IWordAdminRepository.cs
Features/Dictionary/BLL/Abstractions/ITopicAdminRepository.cs
Features/Dictionary/BLL/Abstractions/IWordImageStorage.cs
Features/Dictionary/BLL/Abstractions/IWordAudioStorage.cs
Features/Dictionary/DAL/Repositories/WordAdminRepository.cs
Features/Dictionary/DAL/Repositories/TopicAdminRepository.cs
Features/Dictionary/DAL/Mappings/DictionaryAdminPersistenceMappings.cs
Infrastructure/Storage/CloudinaryWordImageStorage.cs
Infrastructure/Storage/CloudinaryWordAudioStorage.cs
```

The existing public read `WordDetailResponse`, `WordSummaryResponse`, `TopicSummaryResponse`, BLL read models, and shared Infrastructure cache implementations are reused rather than duplicated. Admin writes depend on the existing BLL-owned Dictionary cache ports for invalidation.

#### Current-type disposition

| CURRENT type(s) | TARGET disposition |
|---|---|
| `AdminWordQuery`, `AdminTopicQuery` | same concepts as HTTP `*Request` and BLL `AdminWordQuery`/`AdminTopicQuery`; preserve legacy camelCase `topicId`, `wordType`, `includeDeleted`, `sortBy`, `sortDirection` and snake_case fields where already declared. |
| `CreateWordRequest`, `UpdateWordRequest`, `CreateSenseRequest`, `UpdateSenseRequest`, `CreateTopicRequest`, `UpdateTopicRequest`, `AddTopicWordsRequest`, `UpdateWordImageRequest` | same-named HTTP Contracts -> corresponding BLL commands. Preserve every current `[JsonPropertyName]`. |
| `SenseExampleInput` | HTTP `SenseExampleRequest` -> BLL `SenseExampleInput`; preserve `example_id`, `example_en`, `example_vi`. |
| `ImportWordsRequest`, `UploadWordImageRequest`, `UploadWordAudioRequest` | multipart Presentation Contracts containing `IFormFile`; map file metadata/content stream to BLL `UploadedContent`. Preserve form names `File` and `Accent`. |
| `AdminWordListItemDto` | BLL `AdminWordListItem` + HTTP `AdminWordListItemResponse`; preserve `word_id`, `word`, `cefr`, `phonetic`, `status`, `image_url`, `primary_meaning`, `topics`, `word_type`. |
| `AdminTopicDto` | BLL `AdminTopic` + HTTP `AdminTopicResponse`; preserve `topic_id`, `topic_name`, `topic_name_vi`, `icon`, `status`, `word_count`. |
| `BulkImportResultDto`, `BulkImportErrorDto` | BLL `BulkImportResult`/`BulkImportError` + HTTP responses; preserve `imported_words`, `imported_senses`, `skipped`, `errors`, `updated_words`, `imported_topics`, `imported_examples`, and error `row`, `column`, `message`. |
| `WordAudioDto` | BLL `WordAudio` + HTTP `WordAudioResponse`; preserve `audio_id`, `accent`, `source`, `url`, `status`. |
| `WordDetailDto`, `WordSenseDto`, `WordExampleDto`, `WordRelationDto`, `WordDerivedFormDto`, `WordIdiomDto`, `WordTopicDto`, `WordSummaryDto`, `TopicSummaryDto` | map to the existing BLL Dictionary read models and existing HTTP responses; extend those model/response files only for fields already present in CURRENT payloads. Do not create parallel admin DTOs. |
| `WordSearchQuery`, `TopicWordsQuery` | already replaced by public read Contracts/BLL queries; remove legacy copies after admin callers stop referencing them. |
| `IWordService`/`WordService`, `ITopicService`/`TopicService` | admin members become `IWordAdminService`/`WordAdminService` and `ITopicAdminService`/`TopicAdminService`; public read members remain in the existing read services. |
| `IWordRepository`/`WordRepository`, `ITopicRepository`/`TopicRepository` | admin members become the BLL ports and DAL implementations above; public read members remain in existing DAL read repositories. |
| six Dictionary validators in `Features/Admin/Validators` | same-named Presentation validators beside `Features/Dictionary/Contracts/Requests`. |
| `IAudioStorage`, `AudioStorageResult`, and Dictionary use of `IImageStorage`/`ImageStorageResult` | BLL Dictionary storage ports/models and shared Infrastructure Cloudinary implementations listed above. Unit 6 removed the temporary generic Auth avatar storage interface/result; Auth now uses BLL `IAvatarStorage`, `UploadedContent`, and `StoredMedia` with `CloudinaryAvatarStorage`. |

#### BLL ports

`IWordAdminRepository` exposes provider-neutral operations for admin search, key/existence checks, create/update metadata, import lookup/upsert, word status, referencing user IDs, image URL, audio upsert/status, sense create/update/status, topic-name resolution, and word-topic insertion. Inputs and outputs are BLL queries/commands/models; it never exposes `Word`, `WordSense`, `WordAudioAsset`, or `IFormFile`. `ITopicAdminRepository` similarly exposes admin list, name/existence checks, create/update/status, active-word check, word-ID validation, and word-link replacement/addition using BLL models.

```csharp
IWordAdminRepository.SearchAsync(AdminWordQuery, CancellationToken) -> PagedCollection<AdminWordListItem>
IWordAdminRepository.WordKeyExistsAsync(string wordKey, uint? excludingId, CancellationToken) -> bool
IWordAdminRepository.WordExistsAsync(uint wordId, bool includeDeleted, CancellationToken) -> bool
IWordAdminRepository.SenseExistsAsync(uint wordId, uint senseId, bool includeDeleted, CancellationToken) -> bool
IWordAdminRepository.FindWordIdByKeyAsync(string wordKey, CancellationToken) -> uint?
IWordAdminRepository.CreateAsync(CreateWordCommand, CancellationToken) -> WordDetail
IWordAdminRepository.CreateWithSenseAsync(CreateWordCommand, CreateSenseCommand, CancellationToken) -> WordDetail
IWordAdminRepository.UpdateMetadataAsync(uint wordId, UpdateWordCommand, CancellationToken) -> WordDetail?
IWordAdminRepository.UpdateMissingImportMetadataAsync(uint wordId, ImportWordMetadata, CancellationToken) -> bool?
IWordAdminRepository.SetWordStatusAsync(uint wordId, string status, CancellationToken) -> bool
IWordAdminRepository.GetReferencingUserIdsAsync(uint wordId, CancellationToken) -> IReadOnlyCollection<uint>
IWordAdminRepository.SetImageUrlAsync(uint wordId, string? url, CancellationToken) -> WordDetail?
IWordAdminRepository.UpsertAudioAsync(uint wordId, StoredMedia media, string? accent, CancellationToken) -> WordAudio?
IWordAdminRepository.SetAudioStatusAsync(uint wordId, uint audioId, string status, CancellationToken) -> bool
IWordAdminRepository.CreateSenseAsync(uint wordId, CreateSenseCommand, CancellationToken) -> WordSense?
IWordAdminRepository.UpdateSenseAsync(uint wordId, uint senseId, UpdateSenseCommand, CancellationToken) -> WordSense?
IWordAdminRepository.SetSenseStatusAsync(uint wordId, uint senseId, string status, CancellationToken) -> bool
IWordAdminRepository.FindActiveTopicIdsByNamesAsync(IReadOnlyCollection<string>, CancellationToken) -> IReadOnlyDictionary<string,uint>
IWordAdminRepository.AddTopicsAsync(uint wordId, IReadOnlyCollection<uint> topicIds, CancellationToken) -> int
ITopicAdminRepository.ListAsync(AdminTopicQuery, CancellationToken) -> IReadOnlyCollection<AdminTopic>
ITopicAdminRepository.ExistsAsync(uint topicId, bool includeDeleted, CancellationToken) -> bool
ITopicAdminRepository.NameExistsAsync(string name, string? nameVi, uint? excludingId, CancellationToken) -> bool
ITopicAdminRepository.WordIdsExistAsync(IReadOnlyCollection<uint>, CancellationToken) -> bool
ITopicAdminRepository.CreateAsync(CreateTopicCommand, CancellationToken) -> TopicSummary
ITopicAdminRepository.UpdateAsync(uint topicId, UpdateTopicCommand, CancellationToken) -> TopicSummary?
ITopicAdminRepository.AddWordsAsync(uint topicId, IReadOnlyCollection<uint>, CancellationToken) -> int
ITopicAdminRepository.HasActiveWordsAsync(uint topicId, CancellationToken) -> bool
ITopicAdminRepository.SetStatusAsync(uint topicId, string status, CancellationToken) -> bool
IWordImageStorage.UploadAsync(UploadedContent content, CancellationToken) -> StoredMedia
IWordAudioStorage.UploadAsync(UploadedContent content, string? accent, CancellationToken) -> StoredMedia
```

The service preserves row-by-row CSV results and current multi-save semantics except where WordSense status makes the formerly unsupported operations functional. Cloudinary upload remains before relational URL persistence; provider cleanup/compensation is not added by this layer move.

DI remains: admin services/repositories scoped; Redis Dictionary/List cache implementations and Cloudinary storage implementations singleton.

#### WordSense schema implementation

Before Unit 3, `Word.Status` and `Topic.Status` had active/deleted mappings and filters while `WordSense` had no status property or query filter. Unit 3 applied the following exact reviewed SQL to the source-of-truth MySQL schema:

The future schema task must add exactly `scripts/add-word-sense-status.sql` with:

```sql
ALTER TABLE `word_senses`
    ADD COLUMN `status` varchar(20) NOT NULL DEFAULT 'active' COMMENT 'active/deleted'
        AFTER `vietnamese_meaning`,
    ADD INDEX `idx_senses_status` (`status`);
```

CURRENT: `scripts/scaffold-mysql.ps1` was run after the SQL application. The reviewed model includes `WordSense.Status`; `WordSenseConfiguration` contains the exact column/default/comment/index mapping; and `VocaNovaDbContext.OnModelCreating` applies `Status != UserStatus.Deleted`. Admin include-deleted/status paths use `IgnoreQueryFilters()` narrowly. `SoftDeleteSenseAsync` sets `deleted`; restore sets `active`; both verify `wordId` ownership, save once, and invalidate the word-detail cache after commit.

#### Compatibility gate

- Preserve every Dictionary admin route/method in `SERVICE_CATALOG.md`; Admin policy remains on all routes and SuperAdmin remains required specifically for word delete/restore.
- Literal word surface: `GET|POST /api/admin/words`; `PUT|DELETE /api/admin/words/{id:uint}`; `POST /api/admin/words/import`; `POST /api/admin/words/{id:uint}/audio`; `DELETE /api/admin/words/{id:uint}/audio/{audioId:uint}`; `POST /api/admin/words/{id:uint}/image`; `PUT /api/admin/words/{id:uint}/image`; `PATCH /api/admin/words/{id:uint}/restore`; `POST /api/admin/words/{id:uint}/senses`; `PUT|DELETE /api/admin/words/{id:uint}/senses/{senseId:uint}`; `PATCH /api/admin/words/{id:uint}/senses/{senseId:uint}/restore`.
- Literal topic surface: `GET|POST /api/admin/topics`; `POST /api/admin/topics/{id:uint}/words`; `PUT|DELETE /api/admin/topics/{id:uint}`; `PATCH /api/admin/topics/{id:uint}/restore`.
- Preserve multipart names, all JSON names listed above and in the existing public Dictionary Contracts, current messages/status/envelope, CSV row-error behavior, and paged object placement.
- Exact nested Dictionary response fields remain: word detail `word_id`, `word`, `word_key`, `cefr`, `phonetic_uk`, `phonetic_us`, `image_url`, `is_phrase`, `senses`, `examples`, `relations`, `audio`, `derived_forms`, `idioms`, `topics`, `status`, `created_at`, `updated_at`; sense `sense_id`, `order`, `word_class`, `english_definition`, `vietnamese_meaning`, `examples`, `relations`; example `example_id`, `sense_id`, `example_en`, `example_vi`, `order`; relation `relation_id`, `sense_id`, `relation_type`, `related_word`, `linked_word_id`, `is_quiz_eligible`; audio `audio_id`, `accent`, `source`, `url`, `status`; derived form `derived_id`, `derived_word`, `linked_word_id`, `word_class`; idiom `idiom_id`, `idiom_text`, `meaning_en`, `meaning_vi`; topic `topic_id`, `name`, `name_vi`, `icon`; summary `word_id`, `word`, `phonetic`, `cefr`, `primary_meaning`, `image_url`.
- Preserve keys/TTLs: `word-search:{q|_}:{page}:{limit}:{cefr|_}:{topicId|_}:{isPhrase|_}` 5 minutes, `word:{wordId}` 30 minutes, `topics` 60 minutes, `topic-words:{topicId}:{page}:{limit}` 10 minutes, plus Lists cache keys from Units 1-2. Preserve CURRENT invalidation coverage; do not silently add global word-search invalidation.
- Dashboard Vocabulary/Topics controllers, `Data/Dtos/Dictionary`, views, and JavaScript must parse unchanged payloads.

#### Test gate

- Add `DictionaryAdminArchitectureTests.cs`, `DictionaryAdminControllerContractTests.cs`, `DictionaryAdminCacheInvalidationTests.cs`, and MySQL `WordSenseSoftDeleteIntegrationTests.cs`.
- Keep and extend `AdminWordCrudFeatureTests.cs`, `AdminWordListFeatureTests.cs`, `TopicFeatureTests.cs`, `DictionaryCacheCompatibilityTests.cs`, and `DictionaryControllerContractTests.cs` for all routes, policies, JSON, pagination, CSV outcomes, Cloudinary mapping/failure, cache invalidation, and Redis degradation.
- Schema test verifies the new column type/default/comment/index and global filter; delete hides the sense from normal detail, admin include-deleted can see it, restore returns it, and unrelated senses remain visible.
- Run Dashboard Dictionary controller/DTO tests and manually verify vocabulary/topic pages before Unit 4.

#### Implementation outcome (CURRENT)

- Completed on 2026-08-20. Dictionary administration now occupies the Unit 3 feature-first manifest; legacy admin Dictionary controllers, mixed DTOs, services, repositories, validators, cache adapters, and audio-storage adapter were removed after equivalence verification.
- `scripts/add-word-sense-status.sql` was reviewed, applied to MySQL, and followed by `scripts/scaffold-mysql.ps1`. Schema metadata and soft-delete/filter/restore behavior are covered by a real-MySQL test that rolls back its data changes.
- Routes, authorization, messages, envelopes, pagination placement, JSON/form names, CSV outcomes, independent-save ordering, Cloudinary ordering, Redis keys/TTLs/invalidation, and Dashboard wire parsing are preserved. No global word-search invalidation or transaction redesign was added.
- `dotnet build VocaNova.sln` succeeds with zero warnings/errors and `dotnet test VocaNova.sln` passes 537/537 tests.

### Unit 4 - Quiz and AI grading

#### Target file manifest

```text
Features/Quiz/Controllers/QuizSessionsController.cs
Features/AiGrading/Controllers/AdminAiGradingController.cs
Features/Quiz/Contracts/Requests/CreateSessionRequest.cs
Features/Quiz/Contracts/Requests/SubmitAnswerRequest.cs
Features/Quiz/Contracts/Requests/QuizHistoryRequest.cs
Features/Quiz/Contracts/Requests/WrongWordsRequest.cs
Features/AiGrading/Contracts/Requests/UpdateAiGradingConfigRequest.cs
Features/Quiz/Contracts/Responses/CreateSessionResponse.cs
Features/Quiz/Contracts/Responses/QuizSessionResponse.cs
Features/Quiz/Contracts/Responses/QuestionResponse.cs
Features/Quiz/Contracts/Responses/AnswerResponse.cs
Features/Quiz/Contracts/Responses/QuizResultResponse.cs
Features/Quiz/Contracts/Responses/TestAnswerResponse.cs
Features/Quiz/Contracts/Responses/QuizHistoryItemResponse.cs
Features/Quiz/Contracts/Responses/WrongWordResponse.cs
Features/AiGrading/Contracts/Responses/AiGradingConfigResponse.cs
Features/AiGrading/Contracts/Responses/AiGradingConnectionTestResponse.cs
Features/Quiz/Contracts/Requests/CreateSessionRequestValidator.cs
Features/Quiz/Contracts/Requests/SubmitAnswerRequestValidator.cs
Features/AiGrading/Contracts/Requests/UpdateAiGradingConfigRequestValidator.cs
Features/Quiz/Mappings/QuizMappings.cs
Features/AiGrading/Mappings/AiGradingMappings.cs
Features/Quiz/BLL/Models/QuizCommands.cs
Features/Quiz/BLL/Models/QuizModels.cs
Features/Quiz/BLL/Models/QuizPersistenceModels.cs
Features/AiGrading/BLL/Models/AiGradingModels.cs
Features/AiGrading/BLL/Models/AiGradingConfiguration.cs
Features/Quiz/BLL/Models/QuizOperationResult.cs
Features/AiGrading/BLL/Models/AiGradingOperationResult.cs
Features/Quiz/BLL/Services/IQuizSessionService.cs
Features/Quiz/BLL/Services/QuizSessionService.cs
Features/Quiz/BLL/Services/IQuizSessionBuilder.cs
Features/Quiz/BLL/Services/QuizSessionBuilder.cs
Features/Quiz/BLL/Services/IQuizQuestionBuilder.cs
Features/Quiz/BLL/Services/QuizQuestionBuilder.cs
Features/Quiz/BLL/Services/IQuizSubmissionService.cs
Features/Quiz/BLL/Services/QuizSubmissionService.cs
Features/Quiz/BLL/Services/IQuizResultService.cs
Features/Quiz/BLL/Services/QuizResultService.cs
Features/Quiz/BLL/Services/IQuizHistoryService.cs
Features/Quiz/BLL/Services/QuizHistoryService.cs
Features/Quiz/BLL/Services/ISrsService.cs
Features/Quiz/BLL/Services/SrsService.cs
Features/Quiz/BLL/Services/IAnswerGrader.cs
Features/Quiz/BLL/Services/ExactTypingGrader.cs
Features/Quiz/BLL/Services/MultipleChoiceGrader.cs
Features/Quiz/BLL/Services/AcceptedAnswersParser.cs
Features/Quiz/BLL/Services/QuizSessionStatisticsCalculator.cs
Features/AiGrading/BLL/Services/IAiGradingService.cs
Features/AiGrading/BLL/Services/CachedAiGradingService.cs
Features/AiGrading/BLL/Services/IAiGradingConfigurationService.cs
Features/AiGrading/BLL/Services/AiGradingConfigurationService.cs
Features/Quiz/BLL/Abstractions/IQuizSessionRepository.cs
Features/Quiz/BLL/Abstractions/IQuizPoolRepository.cs
Features/Quiz/BLL/Abstractions/IQuizQuestionRepository.cs
Features/Quiz/BLL/Abstractions/IQuizSubmissionRepository.cs
Features/Quiz/BLL/Abstractions/IQuizResultRepository.cs
Features/Quiz/BLL/Abstractions/IQuizHistoryRepository.cs
Features/Quiz/BLL/Abstractions/ISrsRepository.cs
Features/AiGrading/BLL/Abstractions/IAiGradingCacheRepository.cs
Features/Quiz/BLL/Abstractions/IQuizPoolCache.cs
Features/AiGrading/BLL/Abstractions/IAiGradingProvider.cs
Common/Abstractions/Configuration/IRuntimeConfigWriter.cs
Common/Abstractions/Configuration/IRuntimeSettingsStore.cs
Features/Quiz/DAL/Repositories/QuizSessionRepository.cs
Features/Quiz/DAL/Repositories/QuizPoolRepository.cs
Features/Quiz/DAL/Repositories/QuizQuestionRepository.cs
Features/Quiz/DAL/Repositories/QuizSubmissionRepository.cs
Features/Quiz/DAL/Repositories/QuizResultRepository.cs
Features/Quiz/DAL/Repositories/QuizHistoryRepository.cs
Features/Quiz/DAL/Repositories/SrsRepository.cs
Features/AiGrading/DAL/Repositories/AiGradingCacheRepository.cs
Features/Quiz/DAL/Mappings/QuizPersistenceMappings.cs
Features/AiGrading/DAL/Mappings/AiGradingPersistenceMappings.cs
Infrastructure/Caching/Quiz/QuizPoolCacheEntry.cs
Infrastructure/Caching/Quiz/RedisQuizPoolCache.cs
Infrastructure/ExternalServices/Gemini/GeminiAiGradingProvider.cs
Infrastructure/ExternalServices/Gemini/IGeminiClient.cs
Infrastructure/ExternalServices/Gemini/GeminiClient.cs
Infrastructure/ExternalServices/Gemini/GeminiSettings.cs
Infrastructure/ExternalServices/Gemini/GeminiGradingResponse.cs
Infrastructure/Configuration/EnvFileRuntimeConfigWriter.cs
Infrastructure/Configuration/RedisRuntimeSettingsStore.cs
```

#### Current-type disposition

- HTTP requests: `CreateSessionRequest`, `SubmitAnswerRequest`, `QuizHistoryQuery`, `WrongWordsQuery`, and `UpdateAiGradingConfigRequest` become the same concepts under Contracts (`*Query` becomes `*Request`). Preserve all existing explicit snake_case names; history/wrong query names remain `page` and `limit` by model binding.
- HTTP responses: `CreateSessionResponse`, `QuizSessionDto`, `QuestionDto`, `AnswerResultDto`, `QuizResultDto`, `TestAnswerResultDto`, `QuizHistoryItemDto`, `WrongWordDto`, `AiGradingConfigDto`, and `AiGradingConnectionTestDto` become the corresponding `*Response` files above with their existing JSON names unchanged.
- Internal BLL models: `BuildQuizPoolRequest` -> `BuildQuizPoolCommand`; `QuizPoolWordDto` -> `QuizPoolWord`; `QuizQuestionWordDto` -> `QuizQuestionWord`; `GradeResult` -> `AnswerGrade`; `AiGradingResult` -> `AiGrade`; `UserWordProgressDto` -> `UserWordProgress`; `CachedAiGradingResult` -> `CachedAiGrade`; `AiGradingSettings` -> `AiGradingConfiguration`. None are HTTP Contracts.
- `GeminiGradingResponseDto` becomes DAL-only `GeminiGradingResponse`. `WordRecommendation` and Dictionary data consumed by Quiz remain BLL models or DAL projections, never HTTP types.
- Current service interfaces/classes map one-for-one to the target services, except `IQuizSubmitService`/`QuizSubmitService` become `IQuizSubmissionService`/`QuizSubmissionService`, `AcceptedAnswersJsonParser` becomes `AcceptedAnswersParser`, and `QuizSessionStatsCalculator` becomes `QuizSessionStatisticsCalculator`. `StubAiGradingService` remains test-only and implements the BLL provider/service port from the test project.
- Repository rename ledger: `IQuizWordPoolRepository`/`QuizWordPoolRepository` -> `IQuizPoolRepository`/`QuizPoolRepository`; `IQuizSubmitRepository`/`QuizSubmitRepository` -> `IQuizSubmissionRepository`/`QuizSubmissionRepository`; the session/question/result/history/SRS and AI-cache pairs keep their concepts at the listed target paths. `IAiGradingConfigService`/`AiGradingConfigService` -> `IAiGradingConfigurationService`/`AiGradingConfigurationService`; `KnnRuntimeConfigService` was handled separately in Unit 5.
- The seven Quiz repository pairs, SRS pair, AI cache pair, Quiz cache pair, three graders/provider services, Gemini client/provider, configuration service, runtime writer/store, three validators, and two controllers move to the exact paths above. Repository signatures are redesigned to return BLL persistence models rather than EF `TestSession`/`UserWordProgress` entities.

#### BLL port boundaries and persistence semantics

The seven Quiz persistence ports retain current use cases: pool candidates; session creation; question/distractor reads; submission state/answer staging; result session/answer reads and finish staging; history/wrong-word reads and clear; SRS progress load/stage. `IQuizSubmissionRepository.SaveSubmissionAsync(QuizSubmissionChanges, CancellationToken)` accepts one BLL change aggregate containing answer, session counters/status, and progress changes; DAL applies it to tracked entities and calls `SaveChangesAsync` once. This preserves the current atomic relational unit without exposing EF entities.

```csharp
IQuizPoolRepository.GetCandidatesAsync(uint userId, BuildQuizPoolCommand, CancellationToken) -> IReadOnlyCollection<QuizPoolWord>
IQuizSessionRepository.CreateAsync(uint userId, CreateQuizSessionCommand, IReadOnlyCollection<uint> topicIds, int questionCount, CancellationToken) -> QuizSession
IQuizQuestionRepository.FindQuestionWordAsync(uint wordId, CancellationToken) -> QuizQuestionWord?
IQuizQuestionRepository.GetDistractorsAsync(uint excludedWordId, string wordClass, IReadOnlyCollection<uint> topicIds, CancellationToken) -> IReadOnlyCollection<QuizQuestionWord>
IQuizSubmissionRepository.GetStateAsync(uint userId, uint sessionId, uint wordId, CancellationToken) -> QuizSubmissionState?
IQuizSubmissionRepository.SaveSubmissionAsync(QuizSubmissionChanges changes, CancellationToken)
IQuizResultRepository.GetSessionAsync(uint userId, uint sessionId, CancellationToken) -> QuizResultState?
IQuizResultRepository.SaveFinishAsync(QuizFinishChanges changes, CancellationToken)
IQuizResultRepository.GetAnswersAsync(uint sessionId, CancellationToken) -> IReadOnlyCollection<TestAnswerResult>
IQuizHistoryRepository.GetHistoryAsync(uint userId, int page, int limit, CancellationToken) -> PagedCollection<QuizHistoryItem>
IQuizHistoryRepository.GetWrongWordsAsync(uint userId, int page, int limit, CancellationToken) -> PagedCollection<WrongWord>
IQuizHistoryRepository.ClearWrongWordAsync(uint userId, uint wordId, CancellationToken) -> bool
ISrsRepository.FindAsync(uint userId, uint wordId, CancellationToken) -> UserWordProgress?
ISrsRepository.Stage(UserWordProgress progress)
IQuizPoolCache.GetAsync(uint sessionId, uint? listId, CancellationToken) -> IReadOnlyCollection<QuizPoolWord>?
IQuizPoolCache.SetAsync(uint sessionId, uint? listId, IReadOnlyCollection<QuizPoolWord>, CancellationToken)
IQuizPoolCache.RemoveAsync(uint sessionId, uint? listId, CancellationToken)
IAiGradingCacheRepository.FindValidAndRecordHitAsync(AiGradeCacheKey key, DateTime now, CancellationToken) -> CachedAiGrade?
IAiGradingCacheRepository.SaveAsync(CachedAiGrade result, CancellationToken)
IAiGradingProvider.GradeAsync(AiGradeRequest request, CancellationToken) -> AiGrade
IAiGradingProvider.TestConnectionAsync(AiGradingConfiguration configuration, CancellationToken) -> AiGradingConnectionTest
IRuntimeSettingsStore.GetAsync<T>(string key, CancellationToken) -> T?
IRuntimeSettingsStore.SetAsync<T>(string key, T value, CancellationToken)
IRuntimeSettingsStore.RemoveAsync(string key, CancellationToken)
IRuntimeConfigWriter.WriteAsync<T>(IReadOnlyDictionary<string, string?> values, string fallbackKey, T fallbackValue, CancellationToken) -> RuntimeConfigTarget
```

`IAiGradingCacheRepository` exposes `FindValidAndRecordHitAsync(AiGradeCacheKey, DateTime now, CancellationToken)` and `SaveAsync(CachedAiGrade, CancellationToken)`. Its MySQL save remains separate from Quiz submission. `IAiGradingProvider.GradeAsync(AiGradeRequest, CancellationToken)` is provider-neutral; `GeminiClient` is DAL-internal. Runtime config ports use provider-neutral dictionaries/models and remain BLL-owned.

DI remains: Quiz services/repositories/graders and AI cache repository scoped; `RedisQuizPoolCache`, runtime writer/store, and `AiGradingConfigurationService` singleton; Gemini client/provider use the existing typed-`HttpClient` lifetime.

#### Compatibility gate

- Preserve `[Authorize]`, `user_id`, and all seven Quiz routes in `SERVICE_CATALOG.md`; preserve Admin policy and all four `/api/admin/settings/ai-grading` routes.
- Literal Quiz surface: `GET /api/quiz/history`; `GET /api/quiz/wrong-words`; `DELETE /api/quiz/wrong-words/{wordId:uint}`; `POST /api/quiz/sessions`; `POST /api/quiz/sessions/{id:uint}/answer`; `POST /api/quiz/sessions/{id:uint}/finish`; `GET /api/quiz/sessions/{id:uint}/result`.
- Literal AI administration surface: `GET|PUT /api/admin/settings/ai-grading`; `POST /api/admin/settings/ai-grading/reset`; `POST /api/admin/settings/ai-grading/test`.
- Preserve every current Quiz snake_case field: session creation fields, answer fields, question/choices, result/answer history, wrong-word fields, and AI score/explanation/suggestion. Preserve admin AI fields listed in `AiGradingConfigDtos.cs`, including `fallback_models`, retry/timeout/threshold, storage metadata, and connection-test fields.
- Exact Quiz request fields: create `mode`, `question_type`, `scope_type`, `scope_date_from`, `scope_date_to`, `topic_ids`, `word_order`, `word_limit`, `time_limit_sec`, `lives`, `answer_method`, `list_id`; submit `word_id`, `user_answer`, `list_id`; history/wrong queries `page`, `limit`.
- Exact Quiz response fields: create `session`, `first_question`; session `session_id`, `answer_method`, `mode`, `question_type`, `scope_type`, `scope_date_from`, `scope_date_to`, `word_order`, `word_limit`, `time_limit_sec`, `lives`, `question_count`, `status`, `started_at`, `topic_ids`, `list_id`; question `word_id`, `sense_id`, `question_type`, `display_content`, `expected_answer`, `choices`; answer `session_id`, `word_id`, `is_correct`, `expected_answer`, `correct_count`, `wrong_count`, `score`, `ai_score`, `ai_explanation`, `ai_suggestion`, `next_question`; result `session_id`, `status`, `correct_count`, `wrong_count`, `question_count`, `answered_count`, `accuracy`, `duration_sec`, `max_streak`, `score`, `started_at`, `ended_at`, `answers`; result answer `answer_id`, `word_id`, `sense_id`, `question_number`, `question_type`, `display_content`, `expected_answer`, `user_answer`, `is_correct`, `ai_score`, `ai_explanation`, `ai_suggestion`; history `session_id`, `answer_method`, `mode`, `question_type`, `question_count`, `correct_count`, `wrong_count`, `accuracy`, `score`, `max_streak`, `status`, `started_at`, `ended_at`; wrong word `word_id`, `word`, `primary_meaning`, `test_count`, `correct_count`, `wrong_count`, `mastery_level`, `last_wrong_at`, `next_review_at`.
- Exact AI administration fields: configuration `provider`, `endpoint`, `model`, `fallback_models`, `max_attempts`, `retry_base_delay_ms`, `attempt_timeout_seconds`, `pass_threshold`, `has_api_key`, `api_key_hint`, `storage`, `can_write_env_file`, `supported_providers`; update request uses the same configurable fields plus `api_key`; connection test `succeeded`, `model`, `elapsed_ms`, `message`.
- Preserve Redis key `${prefix}quiz-pool:{sessionId}:{listId|all}`, two-hour TTL, cache payload, and null/no-op degradation. Preserve Progress summary invalidation timing on session creation and successful answer save.
- Preserve MySQL AI cache SHA-256 key inputs, seven-day validity, hit accounting, Gemini retry/model fallback, timeout behavior, pass threshold, and normalized exact-match fallback without caching fallback as AI output.
- Preserve Mobile Quiz API-service/notifier/screen behavior and Dashboard AI Settings models/controller/view/JavaScript contracts.

#### Test gate

- Add `QuizArchitectureTests.cs`, `AiGradingArchitectureTests.cs`, `QuizControllerContractTests.cs`, and `AiGradingControllerContractTests.cs`.
- Keep and extend all files under `tests/VocaNova.Tests/Quiz` and `AiGrading`; explicitly assert one relational submission save, independent AI-cache saves, SM-2 calculations, completion modes, pool key/TTL/payload/removal, Progress invalidation after persistence, Gemini degradation, and Redis/provider failures.
- Contract snapshots cover every route, policy, `user_id`, status/message/envelope, query default, JSON field, and paged object.
- Run Mobile quiz API-service/application/presentation tests and Dashboard Settings DTO/controller tests before Unit 5.

#### Implementation outcome (CURRENT)

- Completed on 2026-08-20. Quiz and AI grading now occupy the Unit 4 feature-first manifest; the old mixed DTO/service/repository paths and the Admin-owned AI-grading controller/validator were removed after reference and compatibility checks.
- Presentation maps the unchanged Quiz and AI-administration HTTP Contracts to framework-neutral BLL commands/models/results. Quiz and AI repository/cache/provider interfaces are BLL-owned; EF implementations and persistence mappings are feature DAL, while Redis, Gemini, and runtime configuration adapters remain shared Infrastructure. The implemented question-repository port carries the source-required preferred word class and topic IDs so distractor selection remains identical; the AI-provider port also carries the existing Admin connection-test operation.
- Submission ordering is unchanged under ADR-017: a cache hit counter or new Gemini cache row can save independently first; answer, session, and SRS changes are then staged and persisted through exactly one `SaveSubmissionAsync` save. No encompassing transaction or different save order was introduced.
- Quiz pool key `${prefix}quiz-pool:{sessionId}:{listId|all}`, two-hour TTL, payload/removal/degraded behavior, Progress invalidation timing, seven-day MySQL AI cache, hit/miss accounting, Gemini retries/model fallback/timeout, pass threshold, and non-cached normalized exact-match degradation remain unchanged.
- Route/policy/envelope/JSON tests, save-order and post-persistence invalidation regression tests, BLL boundary tests, all 565 .NET tests, 32 focused Mobile Quiz tests, the 162-test serial Flutter suite, and the Dashboard AI settings client compatibility test pass. Dashboard and Mobile production code remained unchanged.

### Unit 5 - KNN recommendations and runtime configuration

#### Target file manifest

```text
Features/Knn/Controllers/RecommendationsController.cs
Features/Knn/Controllers/AdminKnnController.cs
Features/Knn/Contracts/Requests/SelectOnboardingTopicsRequest.cs
Features/Knn/Contracts/Requests/KnnLookupRequest.cs
Features/Knn/Contracts/Requests/SaveAgeRangeRequests.cs
Features/Knn/Contracts/Requests/SaveRegionRequests.cs
Features/Knn/Contracts/Requests/SaveOccupationRequests.cs
Features/Knn/Contracts/Requests/SaveEducationLevelRequests.cs
Features/Knn/Contracts/Requests/SaveLearningPurposeRequests.cs
Features/Knn/Contracts/Requests/UpdateKnnVectorWeightsRequest.cs
Features/Knn/Contracts/Responses/LearningProfileOptionsResponse.cs
Features/Knn/Contracts/Responses/TopicRecommendationResponse.cs
Features/Knn/Contracts/Responses/PersonalTopicRecommendationResponse.cs
Features/Knn/Contracts/Responses/WordRecommendationResponse.cs
Features/Knn/Contracts/Responses/KnnLookupResponses.cs
Features/Knn/Contracts/Responses/KnnConfigurationResponse.cs
Features/Knn/Contracts/Responses/KnnRebuildResponse.cs
Features/Knn/Contracts/Requests/SelectOnboardingTopicsRequestValidator.cs
Features/Knn/Contracts/Requests/KnnLookupRequestValidator.cs
Features/Knn/Contracts/Requests/KnnLookupRequestValidators.cs
Features/Knn/Contracts/Requests/UpdateKnnVectorWeightsRequestValidator.cs
Features/Knn/Mappings/KnnMappings.cs
Features/Knn/Mappings/AdminKnnMappings.cs
Features/Knn/BLL/Models/KnnOptions.cs
Features/Knn/BLL/Models/KnnProfileModels.cs
Features/Knn/BLL/Models/KnnLearningModels.cs
Features/Knn/BLL/Models/KnnRecommendationModels.cs
Features/Knn/BLL/Models/KnnAdminModels.cs
Features/Knn/BLL/Models/KnnOperationResult.cs
Features/Knn/BLL/Services/IKnnOnboardingService.cs
Features/Knn/BLL/Services/KnnOnboardingService.cs
Features/Knn/BLL/Services/IKnnLearningService.cs
Features/Knn/BLL/Services/KnnLearningService.cs
Features/Knn/BLL/Services/IKnnRuntimeConfigurationService.cs
Features/Knn/BLL/Services/KnnRuntimeConfigurationService.cs
Features/Knn/BLL/Services/IKnnRebuildService.cs
Features/Knn/BLL/Services/KnnRebuildService.cs
Features/Knn/BLL/Services/IAdminKnnLookupService.cs
Features/Knn/BLL/Services/AdminKnnLookupService.cs
Features/Knn/BLL/Services/KnnProfileVectorBuilder.cs
Features/Knn/BLL/Services/KnnMath.cs
Features/Knn/BLL/Abstractions/IKnnProfileRepository.cs
Features/Knn/BLL/Abstractions/IKnnLearningRepository.cs
Features/Knn/BLL/Abstractions/IAdminKnnLookupRepository.cs
Features/Knn/BLL/Abstractions/IKnnTopicRecommendationCache.cs
Features/Knn/BLL/Abstractions/IKnnWordRecommendationCache.cs
Features/Knn/BLL/Abstractions/IKnnRebuildStateCache.cs
Features/Knn/BLL/Abstractions/IAdminKnnTriggerRateLimiter.cs
Features/Knn/DAL/Repositories/KnnProfileRepository.cs
Features/Knn/DAL/Repositories/KnnLearningRepository.cs
Features/Knn/DAL/Repositories/AdminKnnLookupRepository.cs
Features/Knn/DAL/Mappings/KnnPersistenceMappings.cs
Infrastructure/Caching/Knn/KnnTopicRecommendationCacheEntry.cs
Infrastructure/Caching/Knn/KnnWordRecommendationCacheEntry.cs
Infrastructure/Caching/Knn/RedisKnnTopicRecommendationCache.cs
Infrastructure/Caching/Knn/RedisKnnWordRecommendationCache.cs
Infrastructure/Caching/Knn/RedisKnnRebuildStateCache.cs
Infrastructure/RateLimiting/InMemoryAdminKnnTriggerRateLimiter.cs
Infrastructure/HostedServices/KnnWordRecommendationJob.cs
```

This unit reuses the Unit 4 BLL runtime writer/store abstractions and DAL implementations.

#### Current-type disposition

- Public Contracts: `SelectOnboardingTopicsRequest` remains a request with `topic_ids`. `LearningProfileOptionDto`/`LearningProfileOptionsDto`, `TopicRecommendationDto`, `PersonalTopicRecommendationWordDto`, `PersonalTopicRecommendationDto`, and `WordRecommendationDto` become the corresponding response types and preserve all current snake_case fields.
- Admin Contracts: every type in `AdminKnnLookupDtos.cs` is split by boundary. `KnnLookupQuery` -> `KnnLookupRequest`; five create/update request pairs remain Requests; `AgeRangeDto`, `RegionDto`, `OccupationDto`, `EducationLevelDto`, `LearningPurposeDto`, `KnnConfigDto`, `KnnOnboardingConfigDto`, `KnnLearningConfigDto`, `KnnVectorWeightsDto`, `KnnVectorConfigDto`, and `TriggerKnnRebuildResponse` become Responses. Preserve all declared JSON fields, including `include_deleted`, `sort_by`, `cache_ttl_minutes`, `rebuild_interval_hours`, `storage`, and `can_write_env_file`.
- The five request pairs are exactly `CreateAgeRangeRequest`/`UpdateAgeRangeRequest`, `CreateRegionRequest`/`UpdateRegionRequest`, `CreateOccupationRequest`/`UpdateOccupationRequest`, `CreateEducationLevelRequest`/`UpdateEducationLevelRequest`, and `CreateLearningPurposeRequest`/`UpdateLearningPurposeRequest`. Their validators remain exactly `CreateAgeRangeRequestValidator`, `UpdateAgeRangeRequestValidator`, `CreateRegionRequestValidator`, `UpdateRegionRequestValidator`, `CreateOccupationRequestValidator`, `UpdateOccupationRequestValidator`, `CreateEducationLevelRequestValidator`, `UpdateEducationLevelRequestValidator`, `CreateLearningPurposeRequestValidator`, and `UpdateLearningPurposeRequestValidator`; `KnnLookupQueryValidator` becomes `KnnLookupRequestValidator`.
- Internal BLL models: `KnnLearningProfileDto`, `KnnLookupDimensionsDto`, `KnnMasteredWordDto`, `KnnNeighborDto`, `KnnNeighborWordDto`, `KnnProfileVectorSourceDto`, `KnnTopicAnswerStatsDto`, `KnnTopicPreferenceDto`, `NeighborPersonalTopicDto`, and `WordRecommendationItem` become same-concept records without `Dto`. `KnnRebuildStatusDto` becomes `KnnRebuildStatus`. `KnnOptions`, `KnnOnboardingOptions`, `KnnLearningOptions`, and `KnnVectorOptions` remain configuration models under BLL.
- Current KNN services/repositories map one-for-one to the target service/port/implementation files. `IKnnRuntimeConfigService`/`KnnRuntimeConfigService` become `IKnnRuntimeConfigurationService`/`KnnRuntimeConfigurationService`; `KnnMathHelper` becomes `KnnMath`; hosted `KnnWordRecommendationJob` moves to `Infrastructure/HostedServices` because it coordinates process hosting/scheduling and scoped KNN work.
- `AdminKnnLookupService` and its repository pair move into the KNN BLL/DAL folders. All validators in `AdminKnnLookupValidators.cs`, including the internal reusable rule classes, move to the two exact Presentation validation files above.
- Three Infrastructure KNN cache interfaces become BLL ports; Redis implementations move to `Infrastructure/Caching/Knn` and preserve the existing JSON payload by serializing the corresponding BLL recommendation models directly. `IAdminKnnTriggerRateLimiter` becomes a BLL port with the existing in-memory infrastructure implementation.

#### BLL ports and lifetime rules

`IKnnProfileRepository`, `IKnnLearningRepository`, and `IAdminKnnLookupRepository` retain all current conceptual operations but return BLL records, never EF lookup/profile entities. Cache ports retain user-scoped get/set/remove and rebuild timestamp operations. Runtime configuration reads/writes only vector weights through `IRuntimeConfigWriter`/`IRuntimeSettingsStore` and preserves `RuntimeConfigTarget` as a BLL enum.

```csharp
IKnnProfileRepository.GetLearningProfileAsync(uint userId, CancellationToken) -> KnnLearningProfile?
IKnnProfileRepository.GetActiveTopicPreferencesAsync(uint userId, CancellationToken) -> IReadOnlyCollection<KnnTopicPreference>
IKnnProfileRepository.GetProfileVectorSourceAsync(uint userId, CancellationToken) -> KnnProfileVectorSource?
IKnnProfileRepository.GetActiveLookupDimensionsAsync(CancellationToken) -> KnnLookupDimensions
IKnnProfileRepository.GetActiveLookupOptionsAsync(CancellationToken) -> LearningProfileOptions
IKnnProfileRepository.GetCandidateProfileSourcesAsync(uint excludedUserId, CancellationToken) -> IReadOnlyCollection<KnnProfileVectorSource>
IKnnProfileRepository.GetActiveTopicIdsAsync(uint userId, CancellationToken) -> IReadOnlyCollection<uint>
IKnnProfileRepository.GetNeighborTopicPreferencesAsync(IReadOnlyCollection<uint> userIds, IReadOnlySet<string> sources, CancellationToken) -> IReadOnlyCollection<KnnTopicPreference>
IKnnProfileRepository.GetNeighborPersonalTopicsAsync(uint currentUserId, IReadOnlyCollection<uint> neighborUserIds, int wordsPerTopic, CancellationToken) -> IReadOnlyCollection<NeighborPersonalTopic>
IKnnProfileRepository.GetFallbackTopicRecommendationsAsync(IReadOnlyCollection<uint> excludedTopicIds, int limit, CancellationToken) -> IReadOnlyCollection<TopicRecommendation>
IKnnProfileRepository.GetTopicRecommendationsByScoreAsync(IReadOnlyDictionary<uint,double> scores, int limit, CancellationToken) -> IReadOnlyCollection<TopicRecommendation>
IKnnProfileRepository.UpsertTopicPreferenceAsync(uint userId, uint topicId, string source, DateTime now, CancellationToken) -> bool
IKnnProfileRepository.ReplaceOnboardingTopicPreferencesAsync(uint userId, IReadOnlyCollection<uint> topicIds, DateTime now, CancellationToken) -> int?
IKnnLearningRepository.GetSessionCountAsync(uint userId, CancellationToken) -> int
IKnnLearningRepository.GetActiveTopicIdsAsync(CancellationToken) -> IReadOnlyList<uint>
IKnnLearningRepository.GetTopicAnswerStatsAsync(uint userId, CancellationToken) -> IReadOnlyCollection<KnnTopicAnswerStatistics>
IKnnLearningRepository.GetEligibleUserIdsAsync(int minimumSessions, uint excludingUserId, CancellationToken) -> IReadOnlyCollection<uint>
IKnnLearningRepository.GetMasteredWordsAsync(IReadOnlyCollection<uint> userIds, int minimumMastery, CancellationToken) -> IReadOnlyCollection<KnnMasteredWord>
IKnnLearningRepository.GetNeighborStudiedWordsAsync(IReadOnlyCollection<uint> userIds, IReadOnlyCollection<uint> topicIds, CancellationToken) -> IReadOnlyCollection<KnnNeighborWord>
IKnnLearningRepository.GetActiveListWordIdsAsync(uint userId, CancellationToken) -> IReadOnlyCollection<uint>
IKnnLearningRepository.GetWordRecommendationItemsAsync(IReadOnlyDictionary<uint,double> scoresByWordId, int limit, CancellationToken) -> IReadOnlyCollection<WordRecommendationItem>
IKnnLearningRepository.GetWordRecommendationsAsync(IReadOnlyDictionary<uint,double> scoresByWordId, int limit, CancellationToken) -> IReadOnlyCollection<WordRecommendation>
IAdminKnnLookupRepository.Get/Find/NameExists/Create/Update/SetStatus for each AgeRange, Region, Occupation, EducationLevel, and LearningPurpose using the corresponding BLL model/command. Each mutating repository method preserves the current immediate save behavior.
IKnnTopicRecommendationCache.Get/Set/RemoveAsync(uint userId, ..., TimeSpan ttl, CancellationToken)
IKnnWordRecommendationCache.Get/Set/RemoveAsync(uint userId, ..., TimeSpan ttl, CancellationToken)
IKnnRebuildStateCache.GetLastRebuildAtAsync/SetLastRebuildAtAsync(DateTime, CancellationToken)
IAdminKnnTriggerRateLimiter.IsAllowed(uint adminUserId, DateTime now) -> bool
```

`KnnRebuildService` remains singleton and receives `IServiceScopeFactory` only in the DAL/host orchestration layer; its scoped rebuild worker resolves `IKnnLearningService` inside each scope. `KnnWordRecommendationJob` remains hosted singleton and captures `Knn:Learning:RebuildIntervalHours` once when its timer starts. The refactor must not imply hot reload for that interval.

DI remains: onboarding/learning/admin lookup services and EF repositories scoped; three Redis caches, runtime writer/store/configuration service, rebuild service, and manual trigger limiter singleton; the recommendation job is hosted singleton. A singleton may reach scoped work only through an explicit created scope.

#### Word-cache TTL correction

No new option is introduced. `KnnLearningOptions.CacheTtlMinutes` already exists, defaults to 60, binds from `Knn:Learning:CacheTtlMinutes` / `Knn__Learning__CacheTtlMinutes`, and appears as `learning.cache_ttl_minutes` in the admin configuration response. Replace the current `TimeSpan.FromHours(_options.Learning.RebuildIntervalHours)` call in `KnnLearningService.CacheRecommendationsAsync` with `TimeSpan.FromMinutes(_options.Learning.CacheTtlMinutes)`. `RebuildIntervalHours` remains exclusively the hosted rebuild schedule. This is a TARGET bug fix and intentionally changes word-cache expiry from the current default 24 hours to the configured default 60 minutes without changing its key or payload.

#### Compatibility gate

- Preserve all recommendation and `/api/admin/knn/**` routes in `SERVICE_CATALOG.md`. Recommendations use `[Authorize]` and `user_id`, except `learning-profile-options` remains `[AllowAnonymous]`; all admin KNN routes retain Admin policy and manual trigger uses the authenticated admin ID.
- Literal recommendation surface: `GET /api/recommendations/topics`; `GET /api/recommendations/words`; `GET /api/recommendations/personal-topics`; `GET /api/recommendations/learning-profile-options`; `PUT /api/recommendations/topics/selection`; `POST /api/recommendations/topics/{topicId:uint}/accept`.
- Literal admin configuration surface: `GET /api/admin/knn/config`; `PUT /api/admin/knn/config/vector-weights`; `POST /api/admin/knn/config/vector-weights/reset`; `GET /api/admin/knn/rebuild-status`; `POST /api/admin/knn/trigger-rebuild`. For each literal lookup segment `age-ranges`, `regions`, `occupations`, `education-levels`, and `learning-purposes`, preserve `GET|POST /api/admin/knn/{segment}`, `GET|PUT|DELETE /api/admin/knn/{segment}/{id:uint}`, and `PATCH /api/admin/knn/{segment}/{id:uint}/restore`.
- Preserve all JSON names from KNN and Admin KNN DTOs and the shared envelope/pagination behavior.
- Exact recommendation fields: learning-profile option `id`, `name` and wrapper `age_ranges`, `regions`, `occupations`, `education_levels`, `learning_purposes`; topic `topic_id`, `topic_name`, `topic_name_vi`, `icon`, `word_count`, `recommendation_score`; personal-topic word `word_id`, `word`, `phonetic`, `cefr`, `primary_meaning`; personal topic `topic_id`, `name`, `name_vi`, `icon`, `word_count`, `recommendation_score`, `words`; word recommendation `word_id`, `word`, `phonetic_uk`, `primary_meaning`, `image_url`, `cefr_level`, `score`; rebuild status `last_rebuild_at`, `is_running`.
- Exact admin lookup query fields: `page`, `limit`, `q`, `status`, `include_deleted`, `sort_by`, `sort_direction`. Lookup responses preserve their identity/name/description/code/parent/display-order/status fields: age range `age_range_id`, `name`, `min_age`, `max_age`, `display_order`, `status`; region `region_id`, `name`, `code`, `parent_id`, `parent_name`, `status`; occupation `occupation_id`, `name`, `description`, `status`; education level `education_level_id`, `name`, `description`, `display_order`, `status`; learning purpose `learning_purpose_id`, `name`, `description`, `status`. Create/update requests use the corresponding mutable fields without identity/status.
- Exact admin KNN config fields: root `onboarding`, `learning`, `vector`; onboarding `k_value`, `default_topic_limit`, `min_similarity`, `cache_ttl_minutes`; learning `k_value`, `min_sessions`, `min_similarity`, `recommendation_count`, `rebuild_interval_hours`, `cache_ttl_minutes`; vector weights `age_range_weight`, `region_weight`, `occupation_weight`, `education_level_weight`, `learning_purpose_weight`, `interest_topics_weight`; vector wrapper `weights`, `defaults`, `is_overridden`, `storage`, `can_write_env_file`; trigger `message`, `triggered_at`.
- Preserve `${prefix}knn-topics:{userId}` with `Onboarding.CacheTtlMinutes`, `${prefix}knn-words:{userId}` with the corrected `Learning.CacheTtlMinutes`, `${prefix}knn-last-rebuild` without expiry, and `${prefix}runtime-settings:knn:vector-weights` without expiry plus process-local fallback. Preserve `.env` prefix `Knn__Vector__` and manual rate limiting.
- Protect Mobile home/onboarding recommendation consumers and Dashboard KNN controller/models/view/JavaScript.

#### Test gate

- Add or extend KNN tests for route/policy/claim/JSON/pagination compatibility; BLL vector math; lookup CRUD; env/Redis/local fallback; singleton scope creation; concurrent rebuild exclusion; manual rate limiting; hosted interval capture; Redis degradation; and BLL boundary enforcement.
- Add a focused test proving a configured `Knn:Learning:CacheTtlMinutes` value controls word-cache expiry and does not use `RebuildIntervalHours`.
- Run Mobile home/auth onboarding tests and Dashboard KNN tests before Unit 6.

#### Unit 5 implementation outcome (CURRENT)

- Completed on 2026-08-20. KNN recommendation, admin lookup, runtime vector configuration, rebuild, cache, hosted-job, and manual-rate-limit paths are re-homed under `Features/Knn/{Controllers,Contracts,Mappings,BLL,DAL}` plus `Infrastructure/Caching/Knn`, `Infrastructure/HostedServices`, and `Infrastructure/RateLimiting`.
- BLL owns KNN persistence/cache/rate-limit abstractions and framework-neutral models/results. EF repositories implement BLL ports under KNN DAL; Redis cache implementations and the hosted job remain shared infrastructure implementations. The BLL boundary is protected by `KnnArchitectureTests`.
- Public recommendation and `/api/admin/knn/**` routes, authorization, JSON names, envelope/pagination behavior, Redis keys, runtime vector `.env` prefix, rebuild singleton scope pattern, hosted interval capture, and manual trigger rate limiting are preserved.
- The word recommendation cache TTL fix is live: `${prefix}knn-words:{userId}` now expires by `Knn:Learning:CacheTtlMinutes`; `Knn:Learning:RebuildIntervalHours` remains only the hosted rebuild schedule. No new configuration option or schema change was introduced.
- Implementation kept existing cache payloads by serializing BLL recommendation records directly; no separate `KnnTopicRecommendationCacheEntry`, `KnnWordRecommendationCacheEntry`, or `KnnPersistenceMappings.cs` file was required after source verification.
- Verification: `dotnet build VocaNova.sln --no-restore`, `dotnet test VocaNova.sln --no-build` (569/569), and `flutter test` from `src/VocaNova.Mobile` all passed.

### Unit 6 - Auth

#### Target file manifest

```text
Features/Auth/Controllers/AuthController.cs
Features/Auth/Contracts/Requests/RegisterRequest.cs
Features/Auth/Contracts/Requests/LoginRequest.cs
Features/Auth/Contracts/Requests/GoogleLoginRequest.cs
Features/Auth/Contracts/Requests/RefreshTokenRequest.cs
Features/Auth/Contracts/Requests/OtpSendRequest.cs
Features/Auth/Contracts/Requests/OtpVerifyRequest.cs
Features/Auth/Contracts/Requests/ForgotPasswordRequest.cs
Features/Auth/Contracts/Requests/ResetPasswordRequest.cs
Features/Auth/Contracts/Requests/ChangePasswordRequest.cs
Features/Auth/Contracts/Requests/UpdateUserProfileRequest.cs
Features/Auth/Contracts/Requests/UpdateLearningProfileRequest.cs
Features/Auth/Contracts/Requests/UploadAvatarRequest.cs
Features/Auth/Contracts/Responses/TokenResponse.cs
Features/Auth/Contracts/Responses/OtpSendResponse.cs
Features/Auth/Contracts/Responses/OtpVerifyResponse.cs
Features/Auth/Contracts/Responses/UserProfileResponse.cs
Features/Auth/Contracts/Responses/LearningProfileResponse.cs
Features/Auth/Contracts/Requests/RegisterRequestValidator.cs
Features/Auth/Contracts/Requests/LoginRequestValidator.cs
Features/Auth/Contracts/Requests/GoogleLoginRequestValidator.cs
Features/Auth/Contracts/Requests/RefreshTokenRequestValidator.cs
Features/Auth/Contracts/Requests/OtpSendRequestValidator.cs
Features/Auth/Contracts/Requests/OtpVerifyRequestValidator.cs
Features/Auth/Contracts/Requests/ForgotPasswordRequestValidator.cs
Features/Auth/Contracts/Requests/ResetPasswordRequestValidator.cs
Features/Auth/Contracts/Requests/ChangePasswordRequestValidator.cs
Features/Auth/Contracts/Requests/UpdateUserProfileRequestValidator.cs
Features/Auth/Contracts/Requests/UpdateLearningProfileRequestValidator.cs
Features/Auth/Mappings/AuthMappings.cs
Features/Auth/BLL/Models/AuthCommands.cs
Features/Auth/BLL/Models/AuthModels.cs
Features/Auth/BLL/Models/AuthPersistenceModels.cs
Features/Auth/BLL/Models/AuthOperationResult.cs
Features/Auth/BLL/Services/IAuthService.cs
Features/Auth/BLL/Services/AuthService.cs
Features/Auth/BLL/Abstractions/IAuthAccountRepository.cs
Features/Auth/BLL/Abstractions/IRefreshTokenRepository.cs
Features/Auth/BLL/Abstractions/IOtpRepository.cs
Common/Abstractions/Transactions/IApplicationTransactionManager.cs
Common/Abstractions/Transactions/IApplicationTransaction.cs
Features/Auth/BLL/Abstractions/IJwtTokenService.cs
Features/Auth/BLL/Abstractions/IGoogleIdentityProvider.cs
Features/Auth/BLL/Abstractions/IOtpCodeGenerator.cs
Features/Auth/BLL/Abstractions/IPasswordHasher.cs
Features/Auth/BLL/Abstractions/IRefreshTokenHasher.cs
Features/Auth/BLL/Abstractions/ISmsSender.cs
Features/Auth/BLL/Abstractions/IAvatarStorage.cs
Features/Auth/BLL/Abstractions/IUserProfileCache.cs
Features/Auth/BLL/Abstractions/IAuthRateLimiter.cs
Features/Auth/DAL/Repositories/AuthAccountRepository.cs
Features/Auth/DAL/Repositories/RefreshTokenRepository.cs
Features/Auth/DAL/Repositories/OtpRepository.cs
Infrastructure/Persistence/Transactions/EfApplicationTransactionManager.cs
Infrastructure/Persistence/Transactions/EfApplicationTransaction.cs
Features/Auth/DAL/Mappings/AuthPersistenceMappings.cs
Infrastructure/Authentication/JwtTokenService.cs
Infrastructure/Authentication/GoogleTokenVerifier.cs
Infrastructure/Authentication/JwtSettings.cs
Infrastructure/Authentication/GoogleAuthSettings.cs
Infrastructure/Authentication/JwtAuthenticationExtensions.cs
Infrastructure/Authentication/JwtClaimsPrincipalHelper.cs
Infrastructure/Authentication/JwtTokenValidationParametersFactory.cs
Infrastructure/Authentication/BcryptPasswordHasher.cs
Infrastructure/Authentication/Sha256RefreshTokenHasher.cs
Infrastructure/Otp/RandomOtpCodeGenerator.cs
Infrastructure/Sms/ConsoleSmsProvider.cs
Infrastructure/Sms/SpeedSmsProvider.cs
Infrastructure/Sms/SpeedSmsSettings.cs
Infrastructure/Storage/CloudinaryAvatarStorage.cs
Infrastructure/Storage/CloudinarySettings.cs
Infrastructure/Caching/Auth/RedisUserProfileCache.cs
Infrastructure/RateLimiting/InMemoryAuthRateLimiter.cs
```

#### Current-type disposition

- All twelve Auth request classes move to same-named Contracts; `UploadAvatarRequest` remains multipart Presentation with `IFormFile`. Preserve all current snake_case fields exactly.
- `TokenResponse`, `OtpSendResponse`, and `OtpVerifyResponse` remain response names; `UserProfileDto` -> `UserProfileResponse`; `LearningProfileDto` -> `LearningProfileResponse`. Preserve `access_token`, `refresh_token`, `expires_in`, `token_type`, `verified`, `user_id`, `display_name`, `learning_profile`, and all nested fields.
- Presentation maps requests to BLL commands and maps BLL `AuthTokenPair`, `OtpSendResult`, `OtpVerificationResult`, `UserProfile`, and `LearningProfile` to responses. Passwords/tokens remain command values and are never cache models or logs.
- `IAuthService`/`AuthService` retain use-case names but use framework-neutral results; ASP.NET `StatusCodes`, `IFormFile`, EF entities, and Infrastructure interfaces leave the service.
- `IAuthRepository` is split into the three persistence ports listed above. `User`, `Role`, `RefreshToken`, `OtpVerification`, and `UserLearningProfile` are replaced in signatures by BLL persistence models/commands.
- Concrete `AuthRepository` is split into `AuthAccountRepository`, `RefreshTokenRepository`, and `OtpRepository`; no catch-all Auth repository remains.
- Legacy `IJwtTokenService`, `IGoogleTokenVerifier`, `IOtpCodeGenerator`, `ISmsProvider`, `IImageStorage`, `IUserProfileCache`, and `IAuthRateLimiter` responsibilities move to the named BLL ports. `GoogleUserInfo`, storage results, and rate-limit result become BLL models. Existing provider classes map to the exact DAL files above.
- `Common/Security/PasswordHelper` and `TokenHelper` become BLL `IPasswordHasher`/`IRefreshTokenHasher` ports with DAL `BcryptPasswordHasher` and `Sha256RefreshTokenHasher`; BLL never calls BCrypt or hashes provider tokens through a static outer-layer helper.
- JWT validation-parameter factory/authentication registration and claims helper remain DAL/Presentation composition support; they do not enter BLL.

#### Transaction abstraction and atomicity

```csharp
IApplicationTransactionManager.BeginAsync(CancellationToken) -> IApplicationTransaction
IApplicationTransaction.SaveChangesAsync(CancellationToken) -> int
IApplicationTransaction.CommitAsync(CancellationToken)
IApplicationTransaction.RollbackAsync(CancellationToken)
IApplicationTransaction : IAsyncDisposable
```

Auth persistence/provider ports are fixed as follows:

```csharp
IAuthAccountRepository.FindByPhone/GoogleSubject/GoogleEmail/IdAsync(...) -> AuthAccount?
IAuthAccountRepository.FindRoleByNameAsync(string role, CancellationToken) -> AuthRole?
IAuthAccountRepository.StageCreateAsync(CreateAuthAccount, CancellationToken)
IAuthAccountRepository.GetProfileAsync(uint userId, CancellationToken) -> UserProfile?
IAuthAccountRepository.UpdateProfileAsync(uint userId, UpdateProfileCommand, CancellationToken) -> UserProfile?
IAuthAccountRepository.UpsertLearningProfileAsync(uint userId, UpdateLearningProfileCommand, CancellationToken) -> LearningProfile
IAuthAccountRepository.Validate/Resolve active learning-profile lookup methods -> bool or uint?
IAuthAccountRepository.UpdatePasswordAsync(uint userId, string passwordHash, CancellationToken)
IAuthAccountRepository.StageSoftDeleteAsync(uint userId, DateTime now, CancellationToken) -> bool
IRefreshTokenRepository.StageCreateAsync(uint userId, string tokenHash, DateTime expiresAt, CancellationToken)
IRefreshTokenRepository.FindByHashAsync(string hash, CancellationToken) -> RefreshTokenRecord?
IRefreshTokenRepository.FindForUpdateByHashAsync(string hash, CancellationToken) -> RefreshTokenRecord?
IRefreshTokenRepository.StageRevokeAsync(string hash, DateTime revokedAt, CancellationToken) -> bool
IRefreshTokenRepository.StageRevokeAllAsync(uint userId, DateTime revokedAt, CancellationToken) -> int
IOtpRepository.FindLatestAsync(string phone, string purpose, uint? userId, DateTime? since, CancellationToken) -> OtpRecord?
IOtpRepository.FindLatestForUpdateAsync(string phone, string purpose, uint? userId, CancellationToken) -> OtpRecord?
IOtpRepository.StageCreateAsync(OtpRecord otp, CancellationToken)
IOtpRepository.StageUsed(OtpRecord otp, DateTime usedAt)
IJwtTokenService.GenerateAccessToken(uint userId, string role) -> string
IJwtTokenService.GenerateRefreshToken() -> string
IJwtTokenService.ValidateAccessToken(string token) -> AuthPrincipal?
IGoogleIdentityProvider.VerifyAsync(string idToken, CancellationToken) -> GoogleIdentity?
IOtpCodeGenerator.Generate() -> string
IPasswordHasher.Hash(string password) -> string
IPasswordHasher.Verify(string password, string passwordHash) -> bool
IRefreshTokenHasher.Hash(string refreshToken) -> string
ISmsSender.SendOtpAsync(string phone, string code, CancellationToken)
IAvatarStorage.UploadAsync(UploadedContent content, CancellationToken) -> StoredMedia
IUserProfileCache.Get/Set/RemoveAsync(uint userId, ..., CancellationToken)
IAuthRateLimiter.Check/Record/Reset operations retain current phone/IP/action keys and return AuthRateLimitDecision
```

The DAL manager and all scoped Auth repositories share the same scoped `VocaNovaDbContext`. `BeginAsync` opens an EF transaction and its `SaveChangesAsync` delegates to that shared context. Repositories expose queries and staging methods only; no repository begins or commits its own transaction. BLL explicitly begins after external validation/provider calls and before the first relational mutation, saves through the transaction, commits only after all required saves, and invalidates caches only after commit.

- Registration: validate OTP/uniqueness first; inside one transaction re-read the OTP with the repository's DAL `FOR UPDATE` operation, stage user/auth/profile/learning profile and save, reload the created account by phone to obtain its MySQL-generated ID, stage OTP-used and hashed refresh token, save again, and commit; then return tokens. Both saves remain inside one transaction.
- New Google account: verify Google token before transaction; inside stage account/profile and save, reload by Google subject to obtain its ID, stage the hashed refresh token, save again, and commit. Existing-account Google login uses a short transaction for its refresh-token stage/save/commit.
- Refresh rotation: inside one transaction re-read the hashed token using DAL `SELECT ... FOR UPDATE`, validate active/expiry state, revoke old token, insert the new hashed token, save, and commit. The row lock makes a concurrent replay observe the committed revocation instead of producing two active descendants.
- Password reset: validate OTP first; inside re-read/lock the OTP, update password, mark OTP used, revoke active refresh tokens, save, and commit. This retains current password/OTP atomicity and makes token revocation explicit.
- Account deletion: inside one transaction load account including ignored filters as required, soft-delete account/auth state, revoke every active refresh token, save once, commit; only then remove profile and KNN recommendation caches.

Login, logout, profile update, avatar update, learning-profile update, OTP send/verify, forgot-password initiation, and password change use the ports without direct DbContext access; single-save mutations may still use the transaction manager when token/account state is coupled. Provider calls and Cloudinary uploads are never held inside a database transaction.

DI remains: Auth service, three repositories, and transaction manager scoped; profile cache and in-memory rate limiter singleton; JWT/password/token/OTP/storage/provider implementations keep their current singleton or typed-`HttpClient` registration as appropriate and must not capture scoped repositories/DbContext.

#### Compatibility gate

- Preserve all 16 Auth routes/methods in `SERVICE_CATALOG.md`. Anonymous/authenticated actions remain exactly classified; authenticated actions extract `user_id`.
- Literal anonymous surface: `POST /api/auth/register`, `/login`, `/google`, `/refresh`, `/otp/send`, `/otp/verify`, `/forgot-password`, `/reset-password/verify-otp`, and `/reset-password`. Literal authenticated surface: `POST /api/auth/logout`; `PUT /api/auth/me/password`; `GET|DELETE /api/auth/me`; `PUT /api/auth/me/profile`; `POST /api/auth/me/avatar`; `PUT /api/auth/me/learning-profile`.
- Preserve request/response snake_case, status/messages/envelope, Bearer claims (`user_id`, role), password hashing, hashed refresh-token storage, rotation/replay behavior, OTP purposes/attempts/expiry, Google admission, and soft deletion.
- Exact request fields: register `phone`, `password`, `display_name`, `otp_code`, `date_of_birth`, `region_id`, `occupation_id`, `education_level_id`; login `phone`, `password`; Google `id_token`; refresh `refresh_token`; OTP send `phone`, `purpose`; OTP verify `phone`, `otp_code`; forgot password `phone`; reset password `phone`, `otp_code`, `new_password`; change password `current_password`, `new_password`; profile update `display_name`, `avatar_url`; learning profile `age_range_id`, `region_id`, `occupation_id`, `education_level_id`, `learning_purpose_id`; avatar multipart field `File`.
- Exact response fields: token `access_token`, `refresh_token`, `expires_in`, `token_type`; OTP send `expires_in`; OTP verify `verified`; profile `user_id`, `phone`, `display_name`, `avatar_url`, `role`, `status`, `learning_profile`; nested learning profile uses the five learning-profile ID fields above.
- Preserve `${prefix}user:{userId}`, five-minute TTL, payload, invalidation after committed profile/account changes, and Redis degradation. Preserve KNN topic invalidation after account/profile changes where CURRENT code does so.
- Protect Mobile auth API-service, Google service, interceptor refresh/retry, secure-token storage, notifiers/screens/onboarding; protect Dashboard `DashboardAuthService` and `BearerTokenHandler` refresh flow.

#### Test gate

- Add `AuthArchitectureTests.cs`, `AuthControllerContractTests.cs`, `AuthTransactionTests.cs`, `AuthCacheCompatibilityTests.cs`, and `AuthProviderPortTests.cs`.
- Keep and extend every current Auth test file plus SpeedSMS tests. Add real MySQL transaction tests for forced failure at each step of registration, Google creation, rotation, reset, and deletion; assert full rollback and no cache invalidation before commit.
- Add concurrent refresh replay tests and verify only one rotation succeeds. Verify repository/BLL signatures contain no EF entity, ASP.NET, Infrastructure, or provider SDK types.
- Run Mobile auth data/application/presentation tests, shared interceptor tests, and Dashboard auth service tests before Unit 7.

#### Unit 6 implementation outcome (CURRENT)

- Completed on 2026-08-20. Auth routes, request/response contracts, validators, mapping, framework-neutral BLL models/results/services/ports, and EF DAL repositories now live under `Features/Auth/{Controllers,Contracts,Mappings,BLL,DAL}`.
- `IAuthRepository` was split into `IAuthAccountRepository`, `IRefreshTokenRepository`, and `IOtpRepository` with DAL implementations `AuthAccountRepository`, `RefreshTokenRepository`, and `OtpRepository`; no production catch-all `AuthRepository` remains.
- JWT, Google identity, OTP generation, password hashing, refresh-token hashing, SMS, avatar storage, profile cache, and auth rate limiting are BLL-owned ports implemented by shared Infrastructure classes. Legacy Infrastructure-owned Auth provider/cache/storage interfaces were removed.
- `IApplicationTransactionManager`/`IApplicationTransaction` are live under `Common/Abstractions/Transactions`, with EF implementations under `Infrastructure/Persistence/Transactions`. Auth registration, new Google-account creation, refresh-token rotation, password reset, and account deletion now use the shared transaction manager and invalidate profile/KNN caches after commit.
- Public Auth routes, authorization classification, snake_case JSON fields, shared envelope, bearer claims, hashed refresh-token storage, OTP behavior, Google admission, profile cache key/TTL, and avatar upload behavior were preserved.
- Verification: `dotnet build VocaNova.sln --no-restore` passed; `dotnet test VocaNova.sln --no-build` passed 572/572 tests, including `AuthArchitectureTests`.

### Unit 7 - Admin and SuperAdmin

#### Target file manifest

```text
Features/Admin/Controllers/AdminStatsController.cs
Features/Admin/Controllers/AdminUsersController.cs
Features/SuperAdmin/Controllers/SuperAdminAccountsController.cs
Features/SuperAdmin/Controllers/RolesController.cs
Features/Admin/Contracts/Requests/AdminAuditLogRequest.cs
Features/Admin/Contracts/Requests/AdminUserRequest.cs
Features/SuperAdmin/Contracts/Requests/AdminAccountRequest.cs
Features/SuperAdmin/Contracts/Requests/CreateAdminAccountRequest.cs
Features/SuperAdmin/Contracts/Requests/UpdateAdminAccountRequest.cs
Features/SuperAdmin/Contracts/Requests/RoleRequest.cs
Features/SuperAdmin/Contracts/Requests/SaveRoleRequest.cs
Features/Admin/Contracts/Responses/AdminStatisticsResponses.cs
Features/Admin/Contracts/Responses/AdminUserResponses.cs
Features/SuperAdmin/Contracts/Responses/AdminAccountResponse.cs
Features/SuperAdmin/Contracts/Responses/RoleResponse.cs
Features/SuperAdmin/Contracts/Responses/RoleUserResponse.cs
Features/Admin/Contracts/Requests/AdminAuditLogRequestValidator.cs
Features/Admin/Contracts/Requests/AdminUserRequestValidator.cs
Features/SuperAdmin/Contracts/Requests/AdminAccountRequestValidator.cs
Features/SuperAdmin/Contracts/Requests/CreateAdminAccountRequestValidator.cs
Features/SuperAdmin/Contracts/Requests/UpdateAdminAccountRequestValidator.cs
Features/Admin/Mappings/AdminMappings.cs
Features/SuperAdmin/Mappings/SuperAdminMappings.cs
Features/Admin/BLL/Models/AdminStatisticsModels.cs
Features/Admin/BLL/Models/AdminUserModels.cs
Features/SuperAdmin/BLL/Models/AdminAccountModels.cs
Features/SuperAdmin/BLL/Models/RoleModels.cs
Features/Admin/BLL/Models/AdminOperationResult.cs
Features/SuperAdmin/BLL/Models/SuperAdminOperationResult.cs
Features/Admin/BLL/Services/IAdminStatisticsService.cs
Features/Admin/BLL/Services/AdminStatisticsService.cs
Features/Admin/BLL/Services/IAdminUserService.cs
Features/Admin/BLL/Services/AdminUserService.cs
Features/SuperAdmin/BLL/Services/ISuperAdminAccountService.cs
Features/SuperAdmin/BLL/Services/SuperAdminAccountService.cs
Features/SuperAdmin/BLL/Services/IRoleManagementService.cs
Features/SuperAdmin/BLL/Services/RoleManagementService.cs
Features/Admin/BLL/Abstractions/IAdminStatisticsRepository.cs
Features/Admin/BLL/Abstractions/IAdminUserRepository.cs
Features/SuperAdmin/BLL/Abstractions/ISuperAdminAccountRepository.cs
Features/SuperAdmin/BLL/Abstractions/IRoleManagementRepository.cs
Features/Admin/BLL/Abstractions/IAdminStatisticsCache.cs
Common/Abstractions/Auditing/IAuditLogSink.cs
Features/Admin/DAL/Repositories/AdminStatisticsRepository.cs
Features/Admin/DAL/Repositories/AdminUserRepository.cs
Features/SuperAdmin/DAL/Repositories/SuperAdminAccountRepository.cs
Features/SuperAdmin/DAL/Repositories/RoleManagementRepository.cs
Features/Admin/DAL/Mappings/AdminPersistenceMappings.cs
Features/SuperAdmin/DAL/Mappings/SuperAdminPersistenceMappings.cs
Infrastructure/Caching/Admin/MemoryAdminStatisticsCache.cs
Infrastructure/Auditing/AuditLogRecord.cs
Infrastructure/Auditing/AuditLogQueue.cs
Infrastructure/Auditing/AuditLogBackgroundService.cs
```

Unit 6's transaction manager and user-profile cache ports/implementations are reused.

#### Current-type disposition

- `AdminAuditLogQuery` -> HTTP `AdminAuditLogRequest` + BLL `AdminAuditLogQuery`; `AdminUserQuery` -> HTTP `AdminUserRequest` + BLL query. Preserve their current mixed query names exactly: Admin user uses `includeDeleted`, `sortBy`, `sortDirection`; audit uses `user_id`.
- All response-shaped types in `AdminStatsDtos.cs` become BLL models plus members of `AdminStatisticsResponses.cs`: dashboard, demographics/group, learning/wrong-word/accuracy rows, session trend/count rows, mastery distribution/rows, activity trend/points, and audit log. Internal database aggregation rows (`AdminSessionAccuracyRow`, `AdminSessionCountRow`, `AdminMasteryCountRow`) become BLL persistence models only and are never serialized.
- The exact response-class ledger is `AdminDashboardStatsDto`, `AdminDemographicsDto`, `AdminDemographicGroupDto`, `AdminLearningStatsDto`, `AdminWrongWordDto`, `AdminAccuracyTrendPointDto`, `AdminSessionsTrendDto`, `AdminSessionTrendPointDto`, `AdminMasteryDistributionDto`, `AdminMasteryLevelDto`, `AdminActivityTrendDto`, `AdminActivityTrendPointDto`, and `AdminAuditLogDto` -> same concepts without `Dto` in BLL and the grouped HTTP response file. `AdminAuditLogQueryValidator` -> `AdminAuditLogRequestValidator`; `AdminUserQueryValidator` -> `AdminUserRequestValidator`; `AdminAccountQueryValidator` -> `AdminAccountRequestValidator`.
- `AdminUserSummaryDto`, `AdminUserTopicsDto`, `AdminTopicChipDto`, `AdminUserDetailDto`, `AdminUserTestSessionDto`, and `AdminUserLearningProfileDto` become BLL models and members of `AdminUserResponses.cs`, preserving every current snake_case field.
- `AdminAccountQuery`, `CreateAdminAccountRequest`, `UpdateAdminAccountRequest`, `RoleQuery`, and `SaveRoleRequest` become Contracts plus BLL commands/queries. `AdminAccountDto`, `RoleDto`, and `RoleUserDto` become BLL models and the three SuperAdmin responses. Preserve all current snake_case names.
- Current `IAdminStatsService`/`AdminStatsService` and `IAdminStatsRepository`/`AdminStatsRepository` map to the renamed `IAdminStatisticsService`/`AdminStatisticsService` and `IAdminStatisticsRepository`/`AdminStatisticsRepository`. Admin user pairs retain their concepts. SuperAdmin services remain use cases but gain the two BLL persistence ports; no service accesses EF or `VocaNovaDbContext`.
- Current validators move to the exact Contract validation files. `IMemoryCache` use becomes BLL `IAdminStatisticsCache` with DAL memory implementation; profile invalidation reuses Unit 6 `IUserProfileCache`.
- `IAuditLogQueue`, `AuditLogMessage`, `AuditLogQueue`, and `AuditLogBackgroundService` become the BLL `IAuditLogSink` boundary plus the three DAL auditing files above. `AuditLogMiddleware` remains Presentation and submits a provider-neutral audit record; `AuditLogHttpContextKeys` remains with the middleware.

#### BLL ports and transaction rules

`IAdminStatisticsRepository` exposes dashboard counts, demographics, wrong words, accuracy rows, session counts, mastery counts, activity data, and paged audit logs as BLL models. `IAdminUserRepository` exposes paged users, detail/history/topics, role/status lookup, token-revocation staging, and save. Status mutations remain one relational save followed by profile-cache removal.

`ISuperAdminAccountRepository` exposes account list/detail, email/phone uniqueness, role lookup, account/auth/profile staging, and active-token revocation using BLL models. `IRoleManagementRepository` exposes role list/detail/name checks, members, built-in/in-use checks, role CRUD/assignment staging, and token revocation. Both services use Unit 6's transaction manager for every mutation: begin, stage all related entities/tokens, call the transaction's `SaveChangesAsync`, commit, then invalidate profile cache. Reads do not begin transactions. This replaces direct DbContext access while preserving one atomic relational unit and prevents cache invalidation on rollback.

DI remains: Admin/SuperAdmin services and EF repositories scoped; `IMemoryCache`/`MemoryAdminStatisticsCache`, audit queue, and Redis profile cache singleton; audit writer remains hosted. Scoped transaction/repository objects must never be captured by those singletons.

```csharp
IAdminStatisticsRepository.GetDashboardAsync/GetDemographicsAsync/GetWrongWordsAsync/GetAccuracyRowsAsync/GetSessionCountsAsync/GetMasteryCountsAsync/GetActivityRowsAsync/GetAuditLogsAsync -> corresponding BLL models/PagedCollection
IAdminUserRepository.GetUsersAsync(AdminUserQuery, CancellationToken) -> PagedCollection<AdminUserSummary>
IAdminUserRepository.GetDetailAsync/GetTopicsAsync/GetTestHistoryAsync -> corresponding BLL models
IAdminUserRepository.GetStatusTargetAsync(uint userId, CancellationToken) -> AdminUserStatusTarget?
IAdminUserRepository.StageRevokeTokensAsync(uint userId, DateTime now, CancellationToken) -> int
IAdminUserRepository.SaveChangesAsync(CancellationToken)
ISuperAdminAccountRepository.GetAccountsAsync/GetAccountAsync -> PagedCollection<AdminAccount>/AdminAccount?
ISuperAdminAccountRepository.EmailExistsAsync/PhoneExistsAsync/FindAssignableRoleAsync -> bool/AuthRole?
ISuperAdminAccountRepository.StageCreate/StageUpdate/StageStatus/StageDelete/StageRevokeTokens using BLL commands
IRoleManagementRepository.GetRolesAsync/GetRoleAsync/GetUsersAsync -> corresponding BLL models
IRoleManagementRepository.NameExistsAsync/IsBuiltInAsync/IsInUseAsync -> bool
IRoleManagementRepository.StageCreate/StageUpdate/StageDelete/StageAssign/StageRemove/StageRevokeTokens using BLL commands
IAdminStatisticsCache.GetOrCreateDashboardAsync(Func<CancellationToken,Task<AdminDashboardStatistics>>, TimeSpan ttl, CancellationToken) -> AdminDashboardStatistics
IAuditLogSink.EnqueueAsync(AuditLogRecord record, CancellationToken)
```

#### Compatibility gate

- Preserve all Admin and SuperAdmin routes in `SERVICE_CATALOG.md`, Admin/SuperAdmin policies, `role` claim handling for Admin lock/restore, built-in role protection, SuperAdmin-account protection, token revocation, soft-delete/lock semantics, messages/status/envelope, and paged object placement.
- Literal Admin statistics surface: `GET /api/admin/stats/dashboard`, `/demographics`, `/learning`, `/sessions-trend`, `/mastery-distribution`, `/activity-trend`, plus `GET /api/admin/audit-logs`. Literal Admin user surface: `GET /api/admin/users`; `GET /api/admin/users/{id:uint}`; `GET /api/admin/users/{id:uint}/test-history`; `GET /api/admin/users/{id:uint}/topics`; `PATCH /api/admin/users/{id:uint}/deactivate`; `PATCH /api/admin/users/{id:uint}/restore`.
- Literal SuperAdmin surface: `GET|POST /api/superadmin/admins`; `GET|PUT|DELETE /api/superadmin/admins/{id:uint}`; `PATCH /api/superadmin/admins/{id:uint}/lock`; `PATCH /api/superadmin/admins/{id:uint}/unlock`; `GET|POST /api/superadmin/roles`; `PUT|DELETE /api/superadmin/roles/{roleId:uint}`; `GET /api/superadmin/roles/{roleId:uint}/users`; `POST|DELETE /api/superadmin/roles/{roleId:uint}/users/{userId:uint}`.
- Preserve every JSON/query name from `AdminStatsDtos.cs`, `AdminUserDtos.cs`, `AdminAccountDtos.cs`, and `RoleManagementDtos.cs`.
- Exact Admin statistics fields: dashboard `total_users`, `total_words`, `sessions_today`, `avg_accuracy_7d`; demographics `age_ranges`, `occupations`, `education_levels`, whose groups use `id`, `name`, `user_count`; learning `top_wrong_words`, `accuracy_trend`; wrong word `word_id`, `word`, `wrong_count`, `correct_count`, `total_count`, `accuracy`; accuracy point `date`, `correct_count`, `wrong_count`, `total_count`, `accuracy`; sessions trend `days`, `points`, with point `date`, `session_count`; mastery `total_words_in_progress`, `levels`, with `level`, `word_count`; activity `granularity`, `points`, with `period`, `sessions_count`, `correct_count`, `total_count`, `accuracy`; audit query `page`, `limit`, `user_id`, `entity`; audit row `log_id`, `user_id`, `action`, `entity_type`, `entity_id`, `payload_before`, `payload_after`, `ip_address`, `created_at`.
- Exact Admin user query fields remain `page`, `limit`, `status`, `search`, `includeDeleted`, `role`, `sortBy`, `sortDirection`. Summary fields are `user_id`, `phone`, `google_email`, `display_name`, `avatar_url`, `role`, `status`, `last_login_at`, `created_at`. Detail adds `username`, `updated_at`, `learning_profile`; topics use `selected`, `suggested`, and chips `topic_id`, `name`, `name_vi`; test history uses `session_id`, `answer_method`, `mode`, `question_type`, `question_count`, `correct_count`, `wrong_count`, `accuracy`, `score`, `max_streak`, `status`, `started_at`, `ended_at`; learning profile uses all ID/name pairs for age range, region, occupation, education level, and learning purpose.
- Exact SuperAdmin account query fields: `page`, `limit`, `status`, `search`, `include_deleted`, `sort_by`, `sort_direction`; create/update mutable fields `full_name`, `email`, `phone`, `password`, `status`; account response `admin_id`, `full_name`, `email`, `phone`, `role`, `status`, `created_at`, `updated_at`, `last_login_at`. Role query fields: `page`, `limit`, `search`, `type`, `sort_by`, `sort_direction`; save field `role_name`; role response `role_id`, `role_name`; role user `user_id`, `display_name`, `email`, `phone`, `status`.
- Preserve in-process key `admin:stats:dashboard` with five-minute absolute expiry and profile Redis key/TTL/invalidation from Unit 6.
- Protect Dashboard Statistics, Users, AdminAccounts, and Roles controllers/models/views/JavaScript and `IVocaNovaApiClient` calls.

#### Test gate

- Add `AdminArchitectureTests.cs`, `SuperAdminArchitectureTests.cs`, `AdminControllerContractTests.cs`, `SuperAdminControllerContractTests.cs`, and `SuperAdminTransactionTests.cs`.
- Extend current Admin/SuperAdmin feature tests for every route, policy, query/JSON/pagination field, statistic calculation/cache behavior, role-aware status guards, built-in-role protections, token revocation, cache timing, and rollback.
- MySQL integration tests inject failures between account/auth/profile/token and role/token changes and assert full rollback. Architecture tests forbid DbContext/EF/ASP.NET/Infrastructure dependencies in BLL services.
- Run Dashboard role, SuperAdmin, user/statistics DTO/controller tests and the full .NET/Flutter suites. After success, remove remaining legacy feature registrations/types and run the Phase 11 completion checks.

#### Unit 7 implementation outcome (CURRENT)

- Admin/SuperAdmin source now occupies the feature-first `Features/Admin/{Controllers,Contracts,Mappings,BLL,DAL}` and `Features/SuperAdmin/{Controllers,Contracts,Mappings,BLL,DAL}` slices. CURRENT class names retain the established `AdminStats*` names rather than the earlier proposed `AdminStatistics*` rename.
- Admin BLL repository abstractions no longer expose EF persistence entities. `IAdminUserRepository.GetStatusTargetAsync` returns the BLL-owned `AdminUserStatusTarget`, and `StageStatusAsync` stages status updates before the existing refresh-token revocation and single save.
- Admin repository namespaces now match their physical folders: BLL ports live under `Features.Admin.BLL.Abstractions`, while EF implementations live under `Features.Admin.DAL.Repositories`.
- SuperAdmin account and role services use BLL-owned repository ports and the shared transaction manager; BLL source is free of direct EF, `VocaNovaDbContext`, feature DAL, ASP.NET Core, Redis implementation, and shared Infrastructure implementation references.
- `AdminArchitectureTests` and `SuperAdminArchitectureTests` cover route/policy preservation, controller-to-service boundaries, DAL-to-BLL port implementation, and BLL source independence.
- Final Phase 11 cleanup remains: retirement of the remaining Auth/Dictionary/Lists test compatibility surfaces once tests no longer reference them and documentation synchronization as implementation changes. `AddBLL()`/`AddDAL(configuration)` grouping and Docker MySQL Compose alignment are CURRENT.

### Cross-unit design decisions

- The shared BLL transaction lifecycle and DAL EF implementation described in Units 6-7 are DECISION ADR-017. Unit 2 deliberately preserves Lists random-add and personal-topic get-or-create as independent-save sequences; no Lists transaction design is accepted. Any future atomicity change requires a separate decision and implementation task.
- Contract validators are Presentation and live beside the owning feature's Request Contracts; provider/configuration validation internal to shared infrastructure stays with the provider.
- Cache entry types are DAL-only even when their JSON must remain byte-compatible. Cache ports exchange BLL models.
- Feature BLL/DAL registrations are grouped through `AddBLL()` and `AddDAL(configuration)` after Unit 7; shared infrastructure registration remains consolidated in the DAL/composition extension rather than fragmented into feature folders.
- `VocaNovaDbContext`, all scaffolded entities/configurations, `DatabaseConnection`, the design-time factory, EF extensions, `RedisSettings`, and shared configuration/provider implementations remain under `Infrastructure`. ADR-018 explicitly rejects fragmenting core persistence and provider infrastructure across feature DAL folders.
- Final shared-Presentation cleanup keeps cross-feature response-envelope helpers and route constraints in clearly named `Common`/HTTP-support locations rather than introducing a superseded top-level `Presentation` layer root. Legacy `Common/Results/Result.cs` and `PagedResult.cs` are removed after all services use framework-neutral BLL result/pagination types. Reusable constants/string/age helpers remain in `Common` only if architecture tests prove they have no ASP.NET/EF/provider dependency.
- Database-first scaffolding retains the shared `Infrastructure/Persistence` context/entity destinations, Pomelo, `--use-database-names`, `--no-onconfiguring`, and `--force`. Reapply/review separate configurations and query filters after scaffolding, and verify a no-schema-change diff except for the separately applied WordSense status column.
- The Unit 1 query-failure signal question is resolved by the BLL-owned lookup-result decision above. A scan of Units 3-7 found no other confirmed nullable/bool port that collapses distinct CURRENT 403/404 outcomes in the same way; any later source/schema conflict must be recorded as an `Open design question` before implementation continues and must not be guessed around.

## High-risk areas

- Manual Dashboard/Mobile contracts and mixed snake_case/camelCase JSON can drift.
- Auth transactions, hashed refresh-token rotation, role safeguards, and OTP flows are security-sensitive.
- Quiz answer/session/SRS persistence must remain coherent.
- Lists random-add and personal-topic get-or-create can partially complete because they cross independent saves; do not describe the entire feature as low risk.
- Dictionary example/link/CSV paths cross independent saves, Cloudinary side effects still need compensation analysis, and word-search entries are not invalidated by admin writes. Sense delete/restore is now supported by the reviewed status schema.
- Quiz answer/session/SRS changes share one save, but AI-grading cache persistence is a separate save; quiz-pool removal timing and Redis-unavailable rebuilding must remain compatible.
- Redis cache keys, TTLs, actual invalidation coverage, and degraded mode must remain compatible; do not promise invalidation that CURRENT code does not perform.
- MySQL collations, unsigned ranges, defaults, SQL scripts, and provider LINQ behavior remain DAL concerns and require regression coverage when persistence code changes.
- KNN hosted/singleton/scope behavior and runtime `.env`/Redis settings need careful lifetime handling. The singleton rebuild service resolves scoped work through `IServiceScopeFactory`; the hosted interval is captured at startup, and only vector weights use the runtime-settings path.
- Audit middleware infers behavior from admin routes; source moves must not change routes.

## Completion criteria

- API uses corrected feature-first `Features/<Feature>/{Controllers,Contracts,Mappings,BLL,DAL}` slices, with shared persistence/providers consolidated under `Infrastructure`.
- BLL has no DAL, EF Core, Pomelo/MySQL-specific, ASP.NET HTTP, Redis implementation, or provider SDK dependency.
- MySQL/Pomelo database-first persistence remains operational, with the existing schema as source of truth and reviewed scaffolding through `scripts/scaffold-mysql.ps1`.
- Compose runs exactly `mysql`, `redis`, `api`, and `dashboard` with persistent MySQL data in `mysql_data`; Mobile stays outside Docker.
- Dashboard/Mobile remain REST-only Presentation clients.
- Public routes and JSON schemas remain compatible or have separately approved changes.
