# VocaNova Dashboard — Kế hoạch xây dựng (task-level)

> **Project:** SEP490_19 · VocaNova — Admin Dashboard (ASP.NET Core MVC, .NET 8) · **Assignee:** Tan
> **File chính thức:** repo root. Tiến độ ghi ở [`DASHBOARD_WORKLOG.md`](DASHBOARD_WORKLOG.md). Đồng bộ form với [`VocaNova_Feature_Breakdown.md`](VocaNova_Feature_Breakdown.md).
> Mỗi feature = 1 branch + 1–vài commit. Convention: `feature/dashboard/{tên}` · `feat(dashboard): mô tả` · PR vào `dev`.

**Legend:** 🔴 blocker · ⬜ chưa làm · 🔄 đang làm · ✅ xong · ⏸ chờ API/defer · 🟥 cần API (An làm — xem [Phần C](#c-api-gaps--skeleton-contracts)).

---

## A. Ràng buộc bắt buộc (áp dụng MỌI feature)

- **R1 — Dark + Light theme (LI-03/FE-18) ✅ team chốt:** token theo `[data-theme]`, toggle topbar, lưu cookie `VocaNova.Dashboard.Theme`, SSR set `data-theme` trên `<html>`.
- **R2 — Song ngữ EN + VI (LI-02/FE-17) ✅ team chốt:** `IStringLocalizer` + `Resources/*.resx` + `RequestLocalizationMiddleware` + culture cookie. KHÔNG hardcode chuỗi.
- **R3 — 4 roles (LI-01):** bảng `roles` (1=super_admin, 2=admin, 3=user, 4=guest). Dashboard chỉ cho `admin`+`super_admin`. Tính năng "super only" ẩn UI **và** chặn ở API policy.
- **R4 — Admin tự quản lý profile/settings** (FD-02).
- **R5 — Media: image+audio (v1)**, video ⏸ phase sau (G11).
- **R6 — Auto-suggest media (FE-08)** ⏸ phase sau (G12).
- **R7 — AI grading hiển thị "đánh giá tương đối"** (LI-06).
- **API ownership:** mọi endpoint còn thiếu **do An làm**. Tan dựng khung theo contract; chưa có API → `_StateBlock` Empty/disabled, không vỡ trang.

---

## B. Design tokens (tham chiếu cho mục "Done khi")

**Color** (giữ token `site.css`, thêm semantic + CEFR, tách theme):
```css
:root, [data-theme="light"]{
  --bg:#f6f7fb; --surface:#fff; --ink:#19202a; --muted:#667085; --line:#d9dee8;
  --nav:#111827; --nav-soft:#202b3d; --accent:#0f766e; --accent-strong:#115e59; --danger:#b42318;
  --ok:#067647;--ok-bg:#ecfdf3; --warn:#b54708;--warn-bg:#fffaeb; --err:#b42318;--err-bg:#fef3f2; --info:#175cd3;--info-bg:#eff8ff;
  --cefr-a1:#15803d;--cefr-a1-bg:#edfcf2; --cefr-a2:#0f766e;--cefr-a2-bg:#effcf9; --cefr-b1:#0369a1;--cefr-b1-bg:#eff8ff;
  --cefr-b2:#4338ca;--cefr-b2-bg:#eef0ff; --cefr-c1:#7c3aed;--cefr-c1-bg:#f5f0ff; --cefr-c2:#9333ea;--cefr-c2-bg:#faf0ff;
}
[data-theme="dark"]{
  --bg:#0d1117; --surface:#161b22; --ink:#e6edf3; --muted:#8b949e; --line:#2d333b;
  --nav:#010409; --nav-soft:#21262d; --accent:#2dd4bf; --accent-strong:#5eead4; --danger:#ff7b72;
  --ok:#3fb950;--ok-bg:#0f2a18; --warn:#d29922;--warn-bg:#2b2008; --err:#ff7b72;--err-bg:#2d1513; --info:#58a6ff;--info-bg:#0d2136;
  --cefr-a1:#56d364;--cefr-a1-bg:#0f2a18; --cefr-a2:#2dd4bf;--cefr-a2-bg:#0c2b27; --cefr-b1:#58a6ff;--cefr-b1-bg:#0d2136;
  --cefr-b2:#a5b4fc;--cefr-b2-bg:#1a1b3a; --cefr-c1:#c4b5fd;--cefr-c1-bg:#241a3a; --cefr-c2:#d8b4fe;--cefr-c2-bg:#2a1a3a;
}
```
- **Type:** Inter (body). Tabular nums cho số (`.stat-value`,`.data-table td`). Mono cho mã/ID (`.mono`).
- **Signature:** thang màu CEFR A1→C2 — điểm nhấn DUY NHẤT; phần còn lại trung tính.
- **Foundations:** radius 8px, control 44px, focus ring teal, respect `prefers-reduced-motion`.

---

## C. API Gaps — Skeleton Contracts (do An làm; Tan dựng khung)

| # | Gap | Cần ở | Contract | Trạng thái |
|---|---|---|---|---|
| G1 | Admin word list (status+includeDeleted) | `AdminWordsController` | `GET /api/admin/words?q=&cefr=&topicId=&status=&includeDeleted=&page=&limit=` → `PagedResult<WordSummaryDto>`(+status) | 🟥 v1 |
| G2 | Mastery distribution toàn hệ thống | `AdminStatsController` | `GET /api/admin/stats/mastery-distribution` → `[{mastery_level:0..5,count}]` | 🟥 v1 |
| G3 | Sessions/ngày (overview line) | `AdminStatsController` | `sessions_trend_7d:[{date,count}]` trong `stats/dashboard` | 🟥 v1 |
| G4 | User lock/unlock | `AdminUsersController` | `PATCH /api/admin/users/{id}/lock` + `/unlock` (Super) → `UserProfileDto` | 🟥 v1 |
| G5 | User test-history + activity (admin) | `AdminUsersController` | `GET /api/admin/users/{id}/test-history?page=` + `/activity?page=` | 🟥 v1 |
| G6 | Admin topic list includeDeleted | `AdminTopicsController` | `GET /api/admin/topics?q=&includeDeleted=&page=&limit=` → `PagedResult<TopicSummaryDto>` | 🟥 v1 |
| G7 | Stats granularity | `AdminStatsController` | `GET /api/admin/stats/learning?granularity=daily\|weekly\|monthly` | 🟥 v1 |
| G8 | User Create/Update (FE-03) | `AdminUsersController` | `POST /api/admin/users` + `PUT /api/admin/users/{id}` → `UserProfileDto` | 🟥 v1 |
| G9 | Admin Account CRUD (FE-10) | mới `AdminAccountsController` | `GET/POST/PUT/DELETE /api/admin/admin-accounts` (Super) | 🟥 v1 (khung) |
| G10a | Roles list (FE-11) | mới `RolesController` | `GET /api/admin/roles` → `[{role_id,role_name}]`; opt `PATCH /api/admin/users/{id}/role` | 🟥 v1 |
| ~~G10b~~ | ~~Permissions~~ | — | — | ❌ bỏ khỏi v1 |
| G11 | Video multimedia (FE-07) | `AdminWordsController` | `POST /api/admin/words/{id}/video` + `DELETE .../video/{id}` | ⏸ sau |
| G12 | Auto-suggest media (FE-08) | mới `MediaSuggestController` | `GET /api/admin/words/{id}/suggest-media?type=` | ⏸ sau |

---

# D. Features

> Kiến trúc: BFF mỏng — Razor view + typed `HttpClient` → API. AJAX gọi action MVC (proxy), token nằm cookie server-side. API DTO ≠ ViewModel. Mọi call qua `IVocaNovaApiClient`.

---

### F055.1 — Authenticated API Client + Auto-Refresh  🔴
```
Branch:    feature/dashboard/api-client
Assignee:  Tan
Thời gian: 4h
Phụ thuộc: F055
Trạng thái: ✅ (S5 verify ở F056)
```
**Done khi:**
- [x] `Services/Api/ApiJson.cs` — `JsonSerializerOptions` SnakeCaseLower dùng chung
- [x] `Services/Api/ApiResult.cs` — `ApiEnvelope<T>`, `PaginationInfo`, `ApiResult<T>`, `PagedApiResult<T>`
- [x] `BearerTokenHandler` — gắn Bearer từ cookie; 401 → `POST /api/auth/refresh` → `StoreTokens`+`SignInAsync` → retry 1 lần; refresh fail giữ 401
- [x] `IVocaNovaApiClient` + `VocaNovaApiClient` — Get/GetPaged/Post/Put/Patch/Delete/PostForm; map envelope → result; lỗi mạng/parse → fail mềm
- [x] DI `Program.cs`: `AddHttpContextAccessor` + `AddTransient<BearerTokenHandler>` + `AddHttpClient<IVocaNovaApiClient,VocaNovaApiClient>().AddHttpMessageHandler<BearerTokenHandler>()`; `DashboardAuthService` dùng `ApiJson.Default`
- [ ] S5 smoke test (manual, ở F056): login → `GET /api/admin/stats/dashboard` 200 + auto-refresh sau 15'

---

### F055.2 — Theme Dark/Light Scaffold (R1)  🔴
```
Branch:    feature/dashboard/theme
Assignee:  Tan
Thời gian: 2h
Phụ thuộc: F055
Trạng thái: ⬜
```
**Done khi:**
- [ ] Tách token `site.css` thành `[data-theme="light"]` + `[data-theme="dark"]` (theo [Phần B](#b-design-tokens-tham-chiếu-cho-mục-done-khi))
- [ ] `<html data-theme="@theme">` — đọc cookie `VocaNova.Dashboard.Theme` server-side (mặc định `light`), không nhấp nháy
- [ ] `_ThemeToggle` ở topbar; JS đổi `data-theme` + ghi cookie, không reload
- [ ] Verify mọi màu dùng CSS variable (không hardcode hex trong view)
- [ ] Respect `prefers-reduced-motion` cho transition theme

---

### F055.3 — i18n EN/VI Scaffold (R2)  🔴
```
Branch:    feature/dashboard/i18n
Assignee:  Tan
Thời gian: 2.5h
Phụ thuộc: F055
Trạng thái: ⬜
```
**Done khi:**
- [ ] `AddLocalization` + `Resources/SharedResource.{en,vi}.resx` + `RequestLocalizationMiddleware` (supported: en, vi)
- [ ] Culture cookie (`.AspNetCore.Culture`); `_LangSwitch` ở topbar
- [ ] `IStringLocalizer`/`IViewLocalizer` inject vào view; chuỗi `_Layout` + Login chuyển sang resource key
- [ ] Quy ước: feature sau mọi chuỗi mới đều qua resource key (EN+VI)

---

### F055.4 — Shared UI Components  🔴
```
Branch:    feature/dashboard/shared-ui
Assignee:  Tan
Thời gian: 4h
Phụ thuộc: F055.1, F055.2, F055.3
Trạng thái: ⬜
```
**Done khi:**
- [ ] `_StatCard`, `_DataTable` (server paginate, sort, sticky header, row actions), `_FilterBar` (search+filter+toggle deleted), `_Pagination`
- [ ] `_Badge` (status active/locked/deleted + CEFR A1–C2), `_ConfirmModal`, `_FormModal` (AJAX), `_Toast`
- [ ] `_ChartCard` + **Chart.js self-host** vào `wwwroot/lib` (không CDN)
- [ ] `_StateBlock` (Loading skeleton / Empty CTA / Error retry / Data) — mọi list/chart dùng
- [ ] Cập nhật `_Layout` sidebar: nối link thật + Vocabulary submenu + KNN + Activity Logs + (super) Admin Accounts; phân quyền theo R3

---

### F056 — Overview
```
Branch:    feature/dashboard/overview
Assignee:  Tan
Thời gian: 2.5h
Phụ thuộc: F055.1, F055.4
Trạng thái: ⬜
```
**Done khi:**
- [ ] 4 `_StatCard` từ `GET /api/admin/stats/dashboard` (total_users, total_words, sessions_today, avg_accuracy_7d)
- [ ] Line chart 7 ngày từ `GET /api/admin/stats/learning.accuracy_trend` (🟥 G3 nếu muốn series sessions/ngày riêng)
- [ ] Pie mastery distribution — 🟥 G2; chưa có API → `_StateBlock` Empty
- [ ] Auto-refresh 5' (AJAX action `Dashboard/StatsJson`)
- [ ] **Smoke test F055.1**: xác nhận gọi API có token + auto-refresh

---

### F057 — Vocabulary List & Filter
```
Branch:    feature/dashboard/vocab-list
Assignee:  Tan
Thời gian: 3h
Phụ thuộc: F055.4
Trạng thái: ⬜
```
**Done khi:**
- [ ] `_DataTable`: word · CEFR badge · topic chips · status badge · actions (Edit/Delete/Restore), paginate server-side
- [ ] `_FilterBar`: search từ + dropdown CEFR + dropdown topic (`GET /api/topics`) + toggle "Hiện đã xóa"
- [ ] Nguồn data: 🟥 `GET /api/admin/words` (G1); tạm dùng `GET /api/words` (active), disable toggle deleted
- [ ] Delete confirm; Restore chỉ hiện khi xem deleted

---

### F058 — Vocabulary Detail & Sense Management
```
Branch:    feature/dashboard/vocab-detail
Assignee:  Tan
Thời gian: 3.5h
Phụ thuộc: F057
Trạng thái: ⬜
```
**Done khi:**
- [ ] Header: word, phonetic, CEFR badge, image, audio player UK/US (`GET /api/words/{id}`)
- [ ] Accordion senses: Edit/Delete inline AJAX (`POST/PUT/DELETE /api/admin/words/{id}/senses[/{senseId}]`, `PATCH .../restore`)
- [ ] Form thêm sense (AJAX); examples inline trong sense (thêm/xóa)
- [ ] Relations table synonym/antonym — view-only
- [ ] Audio section: list + upload + delete confirm (`POST/DELETE .../audio`); image upload (`POST/PUT .../image`)
- [ ] Video section ⏸ ẩn tới khi có G11

---

### F059 — Vocabulary CSV Import
```
Branch:    feature/dashboard/vocab-import
Assignee:  Tan
Thời gian: 2h
Phụ thuộc: F057
Trạng thái: ⬜
```
**Done khi:**
- [ ] Drag-drop + file picker; tải template `wwwroot/templates/words-import-template.csv`
- [ ] `POST /api/admin/words/import` (multipart) → bảng kết quả imported/skipped/errors
- [ ] Error rows highlight đỏ (cột Row#/Column/Message); nút "Download errors as CSV"

---

### F061 — Topic Management
```
Branch:    feature/dashboard/topic-management
Assignee:  Tan
Thời gian: 2h
Phụ thuộc: F055.4
Trạng thái: ⬜
```
**Done khi:** *(làm trước F060 để chốt CRUD-modal pattern, tái dùng cho F063)*
- [ ] `_DataTable` + CRUD `_FormModal` (icon, topic_name, topic_name_vi) — `POST/PUT /api/admin/topics`
- [ ] Delete **disabled** khi `word_count>0` (tooltip "còn {N} từ"); API trả 409 hiện rõ
- [ ] Restore deleted — 🟥 cần G6 (admin topic list includeDeleted); tạm dùng `GET /api/topics`

---

### F060 — User Management
```
Branch:    feature/dashboard/user-management
Assignee:  Tan
Thời gian: 3h
Phụ thuộc: F055.4, F061
Trạng thái: ⬜
```
**Done khi:**
- [ ] `Index`: list + filter status (active/locked/deleted) + search phone/name + toggle deleted (`GET /api/admin/users`), status badge
- [ ] `Detail` tabs: Profile | Learning Profile | Test History | Activity Log (`GET /api/admin/users/{id}`; 🟥 G5 cho 2 tab cuối → Empty)
- [ ] Deactivate/Restore (Super, confirm) — `PATCH .../deactivate|restore`
- [ ] Lock/Unlock (🟥 G4) + Create/Edit user (🟥 G8) → nút disable tooltip "Sắp có" tới khi API có
- [ ] Ẩn nút super-only với admin thường (R3)

---

### F062 — Statistics
```
Branch:    feature/dashboard/statistics
Assignee:  Tan
Thời gian: 3h
Phụ thuộc: F055.4, F056
Trạng thái: ⬜
```
**Done khi:**
- [ ] Chart sessions/time + accuracy trend (`GET /api/admin/stats/learning`); dropdown granularity 🟥 G7 (tạm fix 1 mức)
- [ ] Bảng Top 20 wrong words (word, wrong_count, % accuracy)
- [ ] Demographics 3 chart: age/occupation/education (`GET /api/admin/stats/demographics`)

---

### F063 — KNN Management
```
Branch:    feature/dashboard/knn-management
Assignee:  Tan
Thời gian: 4h
Phụ thuộc: F061
Trạng thái: ⬜
```
**Done khi:**
- [ ] Submenu 5 lookup CRUD + soft delete/restore (`/api/admin/knn/{age-ranges|regions|occupations|education-levels|learning-purposes}`): mỗi trang `_FilterBar`+`_DataTable`+`_FormModal`+`_ConfirmModal`
- [ ] Cột: age(name,min,max,display_order,status)·region(name,`code`mono,parent,status)·occupation/edu/purpose(name,description[,display_order],status)
- [ ] Validation mirror backend: name max (age50/khác100), description ≤255, age min≤max, region code `[A-Z0-9_-]` + parent không cycle; không sửa status trực tiếp; 409 hiện rõ
- [ ] Trang tổng quan: config read-only (`GET .../config`) + nút Trigger Rebuild (`POST .../trigger-rebuild`, loading) + "Last rebuilt: X" (`GET .../rebuild-status`)

---

### FD-01 — Forgot Password (Dashboard)
```
Branch:    feature/dashboard/forgot-password
Assignee:  Tan
Thời gian: 1.5h
Phụ thuộc: F055
Trạng thái: ⬜  (API có sẵn)
```
**Done khi:**
- [ ] `/forgot-password` 3 bước: nhập phone → `POST /api/auth/forgot-password`; nhập OTP+mật khẩu mới → `/api/auth/otp/verify` + `/api/auth/reset-password`
- [ ] Hiển thị rate-limit/expired rõ; quay lại `/login` sau reset

---

### FD-02 — Profile & Settings (FE-15/16)
```
Branch:    feature/dashboard/profile-settings
Assignee:  Tan
Thời gian: 2h
Phụ thuộc: F055.1, F055.2, F055.3
Trạng thái: ⬜  (API có sẵn)
```
**Done khi:**
- [ ] `/profile`: xem/sửa display_name (`PUT /api/auth/me/profile`), upload avatar (`POST /api/auth/me/avatar`), đổi mật khẩu (`PUT /api/auth/me/password`)
- [ ] `/settings`: chọn theme (R1) + ngôn ngữ (R2), lưu cookie

---

### FD-03 — Activity Logs (FE-04)
```
Branch:    feature/dashboard/activity-logs
Assignee:  Tan
Thời gian: 1.5h
Phụ thuộc: F055.4
Trạng thái: ⬜  (API có sẵn)
```
**Done khi:**
- [ ] `/activity-logs`: `_DataTable` (created_at, user, action, entity_type `.mono`, entity_id, ip) + filter user_id/entity_type (`GET /api/admin/audit-logs`)
- [ ] Tái dùng cho tab "Activity Log" của F060 (khi G5 sẵn)

---

### FD-04 — Admin Account Management (FE-10)  (khung)
```
Branch:    feature/dashboard/admin-accounts
Assignee:  Tan (UI) · An (API G9)
Thời gian: 2h
Phụ thuộc: F055.4, F061
Trạng thái: ⬜  (super_admin)
```
**Done khi:**
- [ ] UI đầy đủ: list + create/edit `_FormModal` + delete confirm, gọi theo contract G9 (`/api/admin/admin-accounts`)
- [ ] Chưa có API → `_StateBlock` Empty/disabled; khi An deploy G9 → tự sáng đèn, không sửa UI
- [ ] Sidebar "Admin Accounts" (super_admin) trỏ vào đây

> **FE-11 Roles:** roles list read-only (cần G10a). **Permissions: ❌ bỏ khỏi v1** (quyền gắn theo role).
> **FE-07 video / FE-08 auto-suggest: ⏸ phase sau** (G11/G12).

---

## E. Thứ tự đề xuất
`F055.1 ✅ → F055.2 → F055.3 → F055.4 → F056 → F057 → F058 → F059 → F061 → F060 → F062 → F063 → FD-01 → FD-02 → FD-03 → FD-04`

## F. Copywriting (mọi feature)
Active voice ("Lưu thay đổi"); tên theo thứ người dùng điều khiển ("Khóa tài khoản"); Empty = lời mời + nút; Error = nói rõ lỗi + nút thử lại (hiện `errors[]`); nhãn nhất quán xuyên flow; sentence case; **mọi chuỗi qua resource key EN+VI (R2)**.
