# Database

## Database implementation (CURRENT)

- The API uses MySQL 8.x through EF Core 8.0.20 and `Pomelo.EntityFrameworkCore.MySql` 8.0.3.
- `EnvironmentFile` loads the repository-root `.env`; `DatabaseConnection` reads `MYSQL_CONNECTION_STRING` and `MYSQL_SERVER_VERSION`.
- `AddDAL(configuration)` registers `VocaNovaDbContext` with `UseMySql`; `Program.cs` calls the grouped BLL/DAL registration extensions.
- MySQL model settings include `utf8mb4`, `utf8mb4_unicode_ci`, and a binary collation for a word key.

`Infrastructure/Persistence/VocaNovaDbContext.cs` exposes 31 `DbSet` properties, applies configurations from the API assembly, and defines soft-delete filters for `UserList`, `UserListWord`, `Topic`, `Word`, `WordSense`, and `WordAudioAsset`.

## Entities and schema behavior (CURRENT)

| Group | Entities |
|---|---|
| Identity/authentication | `User`, `Role`, `UserAuth`, `UserProfile`, `RefreshToken`, `OtpVerification` |
| Learning profile/lookups | `UserLearningProfile`, `AgeRange`, `Region`, `Occupation`, `EducationLevel`, `LearningPurpose`, `UserTopicPreference` |
| Dictionary/topics | `Word`, `WordSense`, `WordExample`, `WordAudioAsset`, `WordDerivedForm`, `WordIdiom`, `WordRelation`, `Topic`, `WordTopic` |
| Lists | `UserList`, `UserListWord`, `UserListWordStat` |
| Quiz/progress | `TestSession`, `TestSessionTopic`, `TestAnswer`, `UserWordProgress` |
| Support | `AiGradingCache`, `AuditLog` |

Entities are persistence POCOs with navigation properties. `Infrastructure/Persistence/Configurations` owns table/column/index/relationship mapping. Many relationships use `DeleteBehavior.ClientSetNull`. Feature repositories map persistence rows to BLL models/results through feature DAL mappings or focused projections. Auth and SuperAdmin use BLL-owned repository ports plus the shared `IApplicationTransactionManager` abstraction with an EF implementation over the scoped `VocaNovaDbContext`.

No EF `Migrations` directory or migration classes exist. The schema must already exist. The three seed/reporting SQL files under `scripts/` are data scripts; `scripts/add-word-sense-status.sql` is the reviewed, applied schema synchronization script for the Unit 3 WordSense change.

`word_senses.status` is `varchar(20) NOT NULL DEFAULT 'active'` with comment `active/deleted` and index `idx_senses_status`. `WordSenseConfiguration` maps that shape, and the DbContext global filter hides `deleted` senses from normal queries. Dictionary administration can narrowly use `IgnoreQueryFilters()` for include-deleted lookup and restoration.

## Transactions (CURRENT)

- Each EF `SaveChangesAsync` is transactional for its own changes.
- Auth explicitly begins EF transactions through `IApplicationTransactionManager` for registration, new Google-account creation, refresh-token rotation, password reset, and account deletion. Auth repositories stage/query with the scoped DbContext; the transaction object saves/commits/rolls back and cache invalidation happens after commit.
- Quiz submission stages answer, session, and SM-2/SRS changes and persists them through one repository save. An AI-cache hit-counter update or new Gemini-result write may persist in an earlier independent save.
- SuperAdmin account/role mutations use the shared `IApplicationTransactionManager` for multi-entity changes, save and commit through that transaction, then invalidate affected profile cache entries after commit.
- Admin user status/token changes stage their relational changes through BLL-owned repository ports, save once per operation, and invalidate the profile cache after the save; they do not begin an explicit transaction.
- Other repository mutation methods frequently save independently. Lists random-add saves once per added word; personal-topic get-or-create can save the reserved list and membership separately; Dictionary word/sense examples, topic links, and CSV rows can also cross saves.
- Unit 2's Lists structural migration deliberately preserves those independent saves and their partial-write risk. No Lists transaction or atomicity change is currently accepted.
- There is no broad Unit of Work abstraction beyond the accepted BLL-owned explicit transaction manager used by Auth and SuperAdmin.

## Redis interaction (CURRENT)

Redis caches serialized profile, dictionary, topic, list, progress, quiz-pool, KNN recommendation, and rebuild-state data. Current TTLs include profile 5 minutes, word search 5, word detail 30, topics 60, topic words 10, user lists 10, progress summary 15, and quiz pools 2 hours. KNN topic TTL comes from onboarding configuration; KNN word TTL comes from `Knn:Learning:CacheTtlMinutes`. Rebuild state and runtime-setting fallback entries do not expire.

Mutation invalidation is feature-specific rather than universal. List writes remove the user-list summary key; Quiz create/answer paths remove the progress summary; word writes remove detail entries and affected user-list summaries but do not clear word-search entries; topic writes have narrower topics/topic-word invalidation depending on the operation. Runtime AI/KNN settings use Redis plus a process-local mirror only as fallback when `.env` cannot be written.

Redis is not the relational system of record. Cache failures are logged and normally cause uncached/database behavior rather than changing business truth.

## Schema ownership (CURRENT)

The existing MySQL database is the source of truth. `scripts/scaffold-mysql.ps1` runs forced `dotnet ef dbcontext scaffold` using Pomelo, `--use-database-names`, and `--force`, overwriting context/entity files. Run it only for explicitly authorized current-schema synchronization and review every generated diff.

## Remaining transaction work and live configuration fixes (CURRENT/TARGET)

- ADR-017 introduced BLL-owned `IApplicationTransactionManager`/`IApplicationTransaction` abstractions with an EF implementation sharing the scoped DbContext. Auth and SuperAdmin now use them for explicit relational transaction boundaries.
- KNN word recommendations retain `${prefix}knn-words:{userId}` and now use the already-defined `Knn:Learning:CacheTtlMinutes` value. `Knn:Learning:RebuildIntervalHours` remains only the hosted rebuild schedule.

## Long-term persistence direction (TARGET)

```text
BLL use case
    -> BLL persistence abstraction
        <- DAL repository
              -> EF Core 8
                  -> Pomelo
                      -> MySQL 8
```

- MySQL/Pomelo/database-first are both CURRENT and TARGET; no relational provider or schema-workflow migration is pending.
- The existing MySQL schema remains the source of truth, and `scripts/scaffold-mysql.ps1` remains the accepted synchronization tool.
- BLL owns provider-neutral persistence abstractions and business models.
- DAL owns `VocaNovaDbContext`, persistence entities/configurations/repositories, Pomelo configuration, and mapping.
- Controllers and clients never access MySQL directly.
- BLL must not reference EF Core, Pomelo, MySQL-specific APIs, DAL entities, or SQL dialect details.
- Public HTTP Contracts never expose EF entities/navigation properties.

## Docker database alignment

- CURRENT Compose provisions `mysql`, uses internal endpoint `mysql:3306`, persists MySQL data in the named `mysql_data` volume, and supplies the API's existing `MYSQL_CONNECTION_STRING` and `MYSQL_SERVER_VERSION` environment keys.
- The MySQL container can create the `MYSQL_DATABASE` database name, but VocaNova still has no EF migrations or automatic schema creation. Load an existing compatible MySQL schema before using database-backed API endpoints.
- Restarting/recreating application containers must not delete database state. Volume removal is a separate, explicit destructive action.
- `.env` may supply local secrets, is never committed, and `.env.example` contains placeholders only. Production secret management is deployment-specific.
