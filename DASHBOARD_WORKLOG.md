# VocaNova Dashboard — Worklog (Lịch sử làm việc)

> Đi kèm [`DASHBOARD_PLAN.md`](DASHBOARD_PLAN.md). **Mục đích:** ghi lại việc ĐÃ làm để lần sau dò nhanh, tránh làm trùng / tốn token.
> **Quy ước:** mỗi feature = 1 nhánh `feature/dashboard/<tên>`; xong 1 mốc → cập nhật bảng + ghi log (file đụng tới, kết quả build, commit). Code từng bước nhỏ, build sau mỗi bước.
> **Trạng thái:** ⬜ chưa làm · 🔄 đang làm · ✅ xong · ⏸ chờ API/defer

---

## Bảng tiến độ

| Mã | Mô tả | Nhánh | Trạng thái | Ngày |
|---|---|---|---|---|
| F055 | Cookie auth + layout + login | `feature/dashboard/auth-layout` | ✅ | (trước) |
| F055.1 | Authenticated API client + auto-refresh | `feature/dashboard/api-client` | ✅ | 21/06 |
| F055.2 | Theme dark/light (R1) | `feature/dashboard/theme` | ✅ | 21/06 |
| F055.3 | i18n EN/VI (R2) | `feature/dashboard/i18n` | ✅ | 21/06 |
| F055.4 | Shared UI components + Chart.js | `feature/dashboard/shared-ui` | ✅ | 21/06 |
| F056 | Overview (stat cards + charts) | `feature/dashboard/overview` | ✅ | 21/06 |
| F057 | Vocabulary list & filter | `feature/dashboard/vocab-list` | ✅ | 21/06 |
| F058 | Vocabulary detail & sense mgmt | `feature/dashboard/vocab-detail` | ✅ | 22/06 |
| F059 | Vocabulary CSV import | `feature/dashboard/vocab-import` | ✅ | 22/06 |
| F061 | Topic management | `feature/dashboard/topic-management` | ✅ | 23/06 |
| F060 | User management (+ G4/G8 khung) | `feature/dashboard/user-management` | ✅ | 23/06 |
| F062 | Statistics | `feature/dashboard/statistics` | ✅ | 23/06 |
| F063 | KNN management (5 lookup) | `feature/dashboard/knn-management` | ✅ | 23/06 |
| FD-01 | Forgot password | `feature/dashboard/forgot-password` | ⬜ | |
| FD-02 | Profile & Settings | `feature/dashboard/profile-settings` | ⬜ | |
| FD-03 | Activity logs | `feature/dashboard/activity-logs` | ⬜ | |
| FD-04 | Admin accounts (khung) | `feature/dashboard/admin-accounts` | ⬜ | |

> **Không làm v1:** FE-11 permissions (❌), FE-07 video / G11 (⏸), FE-08 auto-suggest media / G12 (⏸). Xem `DASHBOARD_PLAN.md` §0.

---

## Môi trường

- DB: **MySQL local** `vocanova` (root, `127.0.0.1:3306`). API đọc `.env` root → `MYSQL_CONNECTION_STRING` + `MYSQL_SERVER_VERSION=auto`.
- API chạy `http://localhost:5013` · Dashboard chạy `http://localhost:5236` (BaseUrl trỏ 5013).
- API cần **Redis** `localhost:6379` đang chạy.
- Chart.js: chạy `libman restore` trong `src/VocaNova.Dashboard` để có `wwwroot/lib/chart/chart.umd.min.js`.

---

## Đã hoàn thành (theo thứ tự)

### F055.1 — Authenticated API client + auto-refresh
- `Services/Api/ApiJson.cs` — JSON SnakeCaseLower dùng chung.
- `Services/Api/ApiResult.cs` — `ApiEnvelope<T>`, `PaginationInfo`, `ApiResult<T>`, `PagedApiResult<T>`.
- `Services/Api/BearerTokenHandler.cs` — gắn Bearer từ cookie; 401 → `POST /api/auth/refresh` → `StoreTokens`+`SignInAsync` → retry 1 lần; buffer body cho POST/PUT; refresh fail giữ 401.
- `Services/Api/IVocaNovaApiClient.cs` + `VocaNovaApiClient.cs` — Get/GetPaged/Post/Put/Patch/Delete/PostForm; map envelope → result; lỗi mạng/parse → fail mềm.
- `Program.cs` — `AddHttpContextAccessor` + `AddTransient<BearerTokenHandler>` + `AddHttpClient<IVocaNovaApiClient,VocaNovaApiClient>().AddHttpMessageHandler<BearerTokenHandler>()`; `DashboardAuthService` dùng `ApiJson.Default`.
- Commit: `feat(dashboard): add authenticated API client with auto token refresh`
- *(S5 smoke-test: xác nhận khi chạy F056 — login → gọi stats API 200 + không văng sau 15')*

### F055.2 — Theme dark/light (R1)
- `site.css`: tách token `[data-theme="light"]`/`[data-theme="dark"]` + status + thang CEFR + `--accent-soft`/`--auth-gradient`.
- `_Layout.cshtml`: `<html data-theme>` đọc cookie `VocaNova.Dashboard.Theme` server-side.
- `_theme-toggle` (moon/sun) ở topbar + `site.js` đổi theme & ghi cookie; màu hardcode → biến; transition + reduced-motion.
- Commit: `feat(dashboard): add dark/light theme with toggle and cookie persistence`

### F055.3 — i18n EN/VI (R2)
- `Program.cs`: `AddLocalization` + `AddViewLocalization` + `UseRequestLocalization` (en, vi; default en; culture cookie).
- `SharedResource.cs` + `Resources/SharedResource.{en,vi}.resx`; `_ViewImports` inject `IStringLocalizer<SharedResource> L`.
- `CultureController` (POST `/culture/set` + antiforgery) + `Views/Shared/_LangSwitch.cshtml` (dropdown EN/VI topbar).
- Localize `_Layout` (nav, đăng xuất, theme) + `Login.cshtml`.
- Commit: `feat(dashboard): add EN/VI localization with language switch`

### F055.4 — Shared UI components + Chart.js
- `site.css`: CSS toàn bộ component (stat card, badge status+CEFR, filter bar, data table sticky, pagination, state block + skeleton, modal theming, toast, chart card).
- `Models/Components/ComponentViewModels.cs` (+ `BadgeModifiers`); partials `Components/_Badge`, `_StatCard`, `_StateBlock`, `_Pagination`, `_ChartCard`, `_Toast`, `_ConfirmModal`, `_FormModal`.
- `site.js`: `vnToast`, confirm-modal (`[data-confirm-url]`), form-modal AJAX (`[data-form-url]`).
- `libman.json` (chart.js 4.4.1) — cần `libman restore`. Sidebar thêm KNN + Activity Logs (link `#` tạm).
- Commit: `feat(dashboard): add shared UI components (cards, table, badges, modals, toast, charts)`

### F056 — Overview
- `Models/Api/Stats/AdminStatsDtos.cs` (dashboard/learning/trend/wrong-word, `[JsonPropertyName]` tường minh).
- `DashboardController` (`Index` + `/dashboard/stats-json` auto-refresh) + `DashboardOverviewViewModel`.
- `Views/Dashboard/Index.cshtml`: 4 stat card + line chart (Chart.js, `total_count` proxy G3) + pie mastery = empty (G2) + auto-refresh 5'.
- Commit: `feat(dashboard): add overview page with stats and activity chart`

### F057 — Vocabulary list & filter
- `Models/Api/Dictionary/DictionaryDtos.cs` (`WordSummaryDto`, `TopicSummaryDto`).
- `VocabularyController` (`Index` + `Delete`/`Restore`) + `VocabularyListViewModel`/`Query`. Word list **fallback** `api/admin/words` (G1) → `api/words` khi 404.
- `Views/Vocabulary/Index.cshtml`: filter (search/CEFR/topic/toggle deleted) + data table (CEFR badge, status badge, actions) + pagination. Delete/Restore chỉ `super_admin`, qua `_ConfirmModal` + toast.
- Sidebar Vocabulary → `/vocabulary`.
- Commit: `feat(dashboard): add vocabulary list with filters and soft-delete`

### F058 — Vocabulary detail & sense management
- `Models/Api/Dictionary/DictionaryDtos.cs`: thêm DTO detail khớp `GET /api/words/{id}` (senses + examples + relations + audio + topics); `VocabularyDetailViewModel` giữ capability flags cho phần API còn thiếu.
- `VocabularyController`: detail BFF + AJAX create/update sense; dựng sẵn action delete/restore sense theo contract; upload/delete audio, upload ảnh và cập nhật image URL — tất cả qua `IVocaNovaApiClient`.
- `Views/Vocabulary/Detail.cshtml`: bento header theo Figma (word/image/CEFR/topic + audio UK/US), accordion timeline senses edit inline, examples inline read-only, relations table view-only, media upload và confirm xóa audio. Video tiếp tục ẩn theo G11.
- `wwwroot/js/vocabulary-detail.js`: submit form AJAX + toast + audio quick-play; `site.css`: layout responsive, dark/light token, focus/reduced-motion theo nền sẵn có.
- `Resources/SharedResource.{en,vi}.resx`: toàn bộ chuỗi mới có EN+VI.
- **Khung chờ API:** nút xóa sense disabled vì API route hiện trả `Sense soft delete is not supported by current database schema` (G13); examples hiển thị được nhưng nút thêm disabled do chưa có mutation endpoints (G14). Action/proxy và vị trí UI đã sẵn để bật khi API hoàn thiện.
- Verify: `dotnet build src/VocaNova.Dashboard/VocaNova.Dashboard.csproj --no-restore` → **0 warning, 0 error**. HTTP smoke: `/vocabulary/1` → 302 `/login?ReturnUrl=...`; `/login` → 200. Không chạy smoke dữ liệu thật vì API `:5013` không hoạt động trong phiên.
- Commit: `feat(dashboard): add vocabulary detail and sense management`

### F059 — Vocabulary CSV import
- `Models/Api/Dictionary/DictionaryDtos.cs`: thêm `BulkImportResultDto`/`BulkImportErrorDto` khớp response `imported_words`, `imported_senses`, `skipped`, `errors`; `VocabularyImportViewModel` giữ giới hạn file/template URL.
- `VocabularyController`: `GET/POST /vocabulary/import`; validate `.csv` + 5 MB, gửi multipart qua `IVocaNovaApiClient` tới `POST /api/admin/words/import`, trả JSON rõ success/message/data/errors cho AJAX.
- `Views/Vocabulary/Import.cshtml`: pipeline 3 bước, drag-drop + file picker, hướng dẫn đúng contract bảy cột, download template, summary imported/skipped/errors và bảng lỗi Row#/Column/Message. `Views/Vocabulary/Index.cshtml` thêm lối vào màn import.
- `wwwroot/js/vocabulary-import.js`: validate file phía client, upload AJAX, render report an toàn bằng `textContent`, highlight error rows và xuất `vocabulary-import-errors.csv` có BOM UTF-8; xóa file đã chọn sau thành công để tránh import trùng khi bấm lại.
- `wwwroot/templates/words-import-template.csv`: header chính xác `word,cefr_level,phonetic_uk,phonetic_us,word_class,english_definition,vietnamese_meaning` + dữ liệu mẫu nhiều nghĩa; `site.css` responsive/dark-light/reduced-motion; resource EN+VI đầy đủ.
- API **đã đủ**, không thêm gap. Đã đối chiếu trực tiếp `WordService.RequiredCsvColumns()`; lưu ý contract thật dùng `cefr_level`.
- Verify: Dashboard build **0 warning, 0 error**; `AdminWordCrudFeatureTests.ImportCsvAsync_Should_Import_Valid_Rows_And_Collect_Row_Errors` **pass 1/1**; JS syntax + RESX XML pass; HTTP `/vocabulary/import` → 302 login đúng policy, template → **200 text/csv**. Không smoke import dữ liệu thật vì API `:5013` không chạy và browser tích hợp không kết nối được trong phiên.
- Commit: `feat(dashboard): add vocabulary CSV import workflow`

### F061 — Topic management
- `Models/Topics/TopicViewModels.cs`: `TopicListViewModel` (+ cờ `RestoreAvailable`=false do G6) + `TopicFormViewModel` (create/edit dùng chung).
- `TopicsController`: `Index` (GET `/api/topics`), create/edit qua AJAX `_FormModal` (`GET _TopicForm` → `POST /api/admin/topics` & `PUT /api/admin/topics/{id}`), delete (`DELETE`, bắt 409 hiển thị toast), restore (khung — PATCH có sẵn). Validate mirror backend: name bắt buộc ≤50, name_vi ≤50, icon ≤20.
- `Views/Topics/Index.cshtml`: data-table (icon, name, name_vi, word_count badge, actions) + nút "New topic"; nút Delete **disabled khi `word_count>0`** kèm tooltip "{0} từ"; empty/error state. `Views/Topics/_TopicForm.cshtml`: form modal create/edit.
- Sidebar Topics → `/topics`. Resource EN+VI đầy đủ (`Topic.*`, `Topics.Eyebrow`, `Toast.Topic*`).
- **Khung chờ API:** nút **Restore không hiện** vì G6 (chưa liệt kê được topic đã xóa) — action `POST /topics/{id}/restore` + cờ `RestoreAvailable` đã sẵn, chỉ cần bật khi có admin topic list `includeDeleted`.
- Verify: `dotnet build` Dashboard → **0 warning, 0 error**.
- Commit: `feat(dashboard): add topic management with CRUD and delete guard`

### F060 — User management
- `Models/Api/Users/AdminUserDtos.cs`: `AdminUserSummaryDto` / `AdminUserDetailDto` / `AdminUserLearningProfileDto` khớp đúng API (`AdminUsersController` + `AdminUserDtos`); map qua `ApiJson.Default` SnakeCaseLower (không field số → không cần `[JsonPropertyName]`).
- `Models/Users/UserViewModels.cs`: `UserListQuery` (search/status/page/limit) + `UserListViewModel` (StatusOptions active/locked/deleted) + `UserDetailViewModel` (cờ `LockUnlockAvailable`/`EditAvailable`/`HistoryAvailable`=false do G4/G8/G5).
- `UsersController`: `Index` (`GET /api/admin/users?search=&status=&page=&limit=`, lọc status không hợp lệ để giữ trang sống), `Detail` (`GET /api/admin/users/{id}`), `Deactivate`/`Restore` (`PATCH .../deactivate|restore`, super_admin — API enforce policy, 403 hiện qua toast). Khung sẵn `Lock`/`Unlock` theo contract G4.
- `Views/Users/Index.cshtml`: filter bar (search + dropdown status) + data-table (avatar/tên, phone mono, role, status badge, last login) + pagination; empty/error state.
- `Views/Users/Detail.cshtml`: header (avatar, role, status badge) + 4 CSS-only tab (radio, không JS): Profile · Learning profile · Test history · Activity log. Deactivate/Restore (super_admin) qua `_ConfirmModal`; nút **Edit (G8)** + **Lock/Unlock (G4)** disabled tooltip "Sắp có"; tab Test history/Activity (G5) = `_StateBlock` Empty; Learning profile null → Empty. Ẩn nút super-only với admin thường (R3).
- `site.css`: `.user-cell`, `.user-detail-header`, `.user-detail-avatar`, `.tabs`/`.tab-*` (CSS radio tabs), `.kv-grid` — dark/light token + responsive + reduced-motion. Sidebar Users → `/users`. Resource EN+VI đầy đủ (`Users.*`, `User.*`, `Common.View`, `Common.ComingSoon`, `Toast.User*`).
- **Khung chờ API:** G4 (lock/unlock) + G8 (create/update user) → nút disabled "Sắp có"; G5 (test-history/activity admin) → tab Empty. Cờ trong `UserDetailViewModel` + action proxy (Lock/Unlock) đã sẵn, bật khi An deploy.
- Verify: `dotnet build` Dashboard → **0 warning, 0 error**.
- Commit: `feat(dashboard): add user management with detail tabs and status actions` (PR #89).

### F062 — Statistics
- `Models/Api/Stats/AdminStatsDtos.cs`: thêm `AdminDemographicsDto` (`age_ranges`/`occupations`/`education_levels`) + `AdminDemographicGroupDto` (`id`,`name`,`user_count`), `[JsonPropertyName]` tường minh. DTO learning (`AdminLearningStatsDto`: `top_wrong_words` + `accuracy_trend`) tái dùng từ F056.
- `Models/Statistics/StatisticsViewModel.cs`: giữ learning + demographics + cờ loaded; cờ `GranularityAvailable`=false do G7.
- `StatisticsController`: `Index` (`/statistics`) gọi `GET /api/admin/stats/learning` + `GET /api/admin/stats/demographics`, fail mềm → state error giữ trang sống.
- `Views/Statistics/Index.cshtml`: chart combo accuracy(%) line + activity volume bar (2 trục); bảng **Top wrong words** (rank, word link→/vocabulary/{id}, wrong, attempts, accuracy badge ok/warn/err); 3 doughnut demographics (age/occupation/education). Mỗi block có empty/error state riêng. Dropdown granularity (G7) **disabled** tooltip "Sắp có", fix daily.
- `site.css`: `.stats-section-title` (tái dùng `.chart-card`/`.chart-canvas-wrap`/`.data-table`). Sidebar Statistics → `/statistics`. Resource EN+VI đầy đủ (`Statistics.*`, `Stats.*`).
- API **đã đủ** cho list/trend/demographics — chỉ G7 (granularity) còn thiếu → khung dropdown sẵn.
- Verify: `dotnet build` Dashboard → **0 warning, 0 error**.
- Commit: *(chờ user yêu cầu)*

### F063 — KNN management
- `Models/Api/Knn/KnnDtos.cs`: `KnnItemDto` **hợp nhất** cho cả 5 lookup (gộp id riêng từng bảng về `Id`; có name/code/parent/min-max age/display_order/description/status) + `KnnConfigDto`/`KnnOnboardingConfigDto`/`KnnLearningConfigDto` + `KnnRebuildStatusDto`. Map SnakeCaseLower (không field số).
- `Models/Knn/KnnViewModels.cs`: `KnnTypeDescriptor` + registry `KnnTypes.All` (5 type: key=route+path API, cờ HasCode/HasParent/HasAge/HasDisplayOrder/HasDescription, NameMaxLength 50/100) — **driver** đổi cột bảng + field form, tránh lặp. + `KnnListQuery`/`KnnListViewModel`/`KnnFormViewModel`/`KnnOverviewViewModel`.
- `KnnController` (generic theo `{type}`): `Overview` (`/knn` → config + rebuild-status), `TriggerRebuild` (`POST trigger-rebuild`, bắt 202/429), `Index` (`GET {type}?q=&include_deleted=&page=&limit=`), create/edit AJAX `_FormModal` (`POST/PUT {type}` — body dựng theo type), delete (bắt 409), restore (PATCH). Validate **mirror backend** (`AdminKnnLookupValidators`): name bắt buộc ≤50(age)/≤100, region code ≤10 + regex `^[A-Za-z0-9_-]+$`, age min≤max & ≥0, description ≤255, display_order ≥0. Unknown type → 404.
- Views: `Knn/Overview.cshtml` (rebuild status badge + nút Trigger loading + config read-only 2 cột onboarding/learning), `Knn/Index.cshtml` (filter search + toggle deleted, data-table cột động theo descriptor, edit/delete/restore), `Knn/_KnnForm.cshtml` (field hiện theo type, region có dropdown parent loại chính nó), `Knn/_KnnNav.cshtml` (pill sub-nav: Overview + 5 lookup). Region code `.mono`.
- `site.css`: `.knn-nav`/`.knn-nav__link` pill (dark/light + reduced-motion), tái dùng `.detail-panel`/`.kv-grid`/`.data-table`. Sidebar KNN → `/knn`. Resource EN+VI đầy đủ (`Knn.*`).
- **Quyết định UI:** "submenu 5 lookup" hiện thực bằng **pill sub-nav** trong khu vực KNN (sidebar phẳng → không lồng menu) — sạch hơn và đủ ý. API KNN **đã đủ toàn bộ** (CRUD + soft delete/restore + config + rebuild) → không thêm gap.
- Verify: `dotnet build` Dashboard → **0 warning, 0 error** (Razor views compile lúc build).
- Commit: *(chờ user yêu cầu)*

---

## Gap chờ API (An làm) — dashboard đã dựng khung, tự sáng đèn khi có

| # | Gap | Ảnh hưởng page |
|---|---|---|
| G1 | `GET /api/admin/words` (status + includeDeleted) | F057 (đang dùng `/api/words` tạm) |
| G2 | `GET /api/admin/stats/mastery-distribution` | F056 pie |
| G3 | sessions/ngày trong `stats/dashboard` | F056 line (đang dùng `total_count`) |
| G4/G8 | user lock-unlock / create-update | F060 |
| G5 | user test-history / activity cho admin | F060 |
| G6 | `GET /api/admin/topics?includeDeleted=` (liệt kê topic đã xóa) | F061 — **khung restore đã dựng**: action `POST /topics/{id}/restore` + cờ `RestoreAvailable` sẵn, bật khi có endpoint |
| G7 | `GET /api/admin/stats/learning?granularity=daily\|weekly\|monthly` | F062 — **khung dropdown đã dựng**: select disabled "Sắp có", cờ `GranularityAvailable` sẵn, bật khi có param |
| G9 | admin-accounts CRUD | FD-04 |
| G10a | `GET /api/admin/roles` | FE-11 (roles read-only) |
| G13 | Sense soft-delete/restore: routes đã có nhưng service luôn fail do schema `word_senses` chưa có trạng thái xóa | F058 (nút delete/restore disabled; proxy action đã dựng) |
| G14 | Example mutation: cần `POST /api/admin/words/{id}/senses/{senseId}/examples` + `DELETE .../examples/{exampleId}` | F058 (đang view-only; nút Add disabled) |

---

## Tiếp theo
**FD-01 — Forgot Password** (nhánh `feature/dashboard/forgot-password`, API có sẵn): `/forgot-password` 3 bước — nhập phone (`POST /api/auth/forgot-password`) → nhập OTP + mật khẩu mới (`/api/auth/otp/verify` + `/api/auth/reset-password`); hiển thị rate-limit/expired rõ; quay lại `/login` sau reset.
