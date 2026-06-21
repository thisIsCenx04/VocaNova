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
| F058 | Vocabulary detail & sense mgmt | `feature/dashboard/vocab-detail` | ⬜ | |
| F059 | Vocabulary CSV import | `feature/dashboard/vocab-import` | ⬜ | |
| F061 | Topic management | `feature/dashboard/topic-management` | ⬜ | |
| F060 | User management (+ G4/G8 khung) | `feature/dashboard/user-management` | ⬜ | |
| F062 | Statistics | `feature/dashboard/statistics` | ⬜ | |
| F063 | KNN management (5 lookup) | `feature/dashboard/knn-management` | ⬜ | |
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

---

## Gap chờ API (An làm) — dashboard đã dựng khung, tự sáng đèn khi có

| # | Gap | Ảnh hưởng page |
|---|---|---|
| G1 | `GET /api/admin/words` (status + includeDeleted) | F057 (đang dùng `/api/words` tạm) |
| G2 | `GET /api/admin/stats/mastery-distribution` | F056 pie |
| G3 | sessions/ngày trong `stats/dashboard` | F056 line (đang dùng `total_count`) |
| G4/G8 | user lock-unlock / create-update | F060 |
| G5 | user test-history / activity cho admin | F060 |
| G6 | admin topic list includeDeleted | F061 |
| G7 | stats granularity | F062 |
| G9 | admin-accounts CRUD | FD-04 |
| G10a | `GET /api/admin/roles` | FE-11 (roles read-only) |

---

## Tiếp theo
**F058 — Vocabulary Detail & Sense Management** (nhánh `feature/dashboard/vocab-detail`): trang chi tiết 1 từ — info + ảnh + audio + accordion senses CRUD inline AJAX + examples + relations view-only. API đã đủ.
