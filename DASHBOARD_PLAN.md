# VocaNova Dashboard — Kế hoạch xây dựng (Build Plan)

> **Project:** SEP490_19 · VocaNova — Admin Dashboard (ASP.NET Core MVC, .NET 8)
> **Assignee:** Tan (Dashboard) · **Phase:** 2 (`F055`–`F063`) + bổ sung scope từ Report 1 (`FE-01`–`FE-19`)
> **File chính thức:** `E:\HK9\Do An\Mobile\VocaNova\DASHBOARD_PLAN.md` (repo root — single source of truth).
> **Mục tiêu:** Hoàn thiện dashboard quản trị theo UI Figma, kết nối `VocaNova.API` đã có sẵn, và khai báo *khung (skeleton contract)* cho phần API còn thiếu để dashboard chạy được ngay và "sáng đèn" khi backend bổ sung.
>
> Prose tiếng Việt + thuật ngữ kỹ thuật tiếng Anh (đồng bộ `VocaNova_Feature_Breakdown.md`).

---

## ⚠️ Hai lưu ý nguồn

1. **Figma** (`Admin - Dashboard`) là **private** → công cụ không mở/đọc được (HTTP 403). Phần Design System dựng trên tokens **đã có trong `src/VocaNova.Dashboard/wwwroot/css/site.css`** (commit ở `F055`) + feature breakdown. Đối chiếu Figma theo [§9](#9-figma-reconciliation-checklist) trước khi code; **Figma thắng** nếu lệch.
2. **Report 1** (Project Introduction) định nghĩa scope `FE-01`–`FE-19` (Admin Dashboard) và ràng buộc `LI-01`–`LI-13`. Bản kế hoạch này đã đối chiếu — xem [§0 Rule bắt buộc](#0-rule-bắt-buộc-từ-report-1) và [§6b phần bổ sung](#6b-trang-bổ-sung-từ-report-1-fe). Một số `FE` vượt phạm vi breakdown `F055`–`F063` và **chưa có API** → ghi rõ ở [§7](#7-api-gaps--skeleton-contracts) và [§11 bảng đối chiếu](#11-bảng-đối-chiếu-fe-01fe-19).

---

## Mục lục
0. [Rule bắt buộc từ Report 1](#0-rule-bắt-buộc-từ-report-1)
1. [Hiện trạng](#1-hiện-trạng-current-state-audit)
2. [Kiến trúc Dashboard](#2-kiến-trúc-dashboard)
3. [Foundation: Authenticated API Client](#3-foundation-authenticated-api-client)
4. [Design System](#4-design-system)
5. [Component Inventory](#5-component-inventory)
6. [Kế hoạch theo từng trang (F056–F063)](#6-kế-hoạch-theo-từng-trang) · [6b. Trang bổ sung từ Report 1](#6b-trang-bổ-sung-từ-report-1-fe)
7. [API Gaps & Skeleton Contracts](#7-api-gaps--skeleton-contracts)
8. [Thứ tự thực hiện](#8-thứ-tự-thực-hiện)
9. [Figma Reconciliation Checklist](#9-figma-reconciliation-checklist)
10. [Copywriting conventions](#10-copywriting-conventions)
11. [Bảng đối chiếu FE-01–FE-19](#11-bảng-đối-chiếu-fe-01fe-19)

---

## 0. Rule bắt buộc từ Report 1

Các ràng buộc dưới đây **không phải tùy chọn** — phải kiến trúc từ đầu, retrofit sau rất tốn công.

- **R1 — Dark + Light theme (LI-03, FE-18): BẮT BUỘC ✅ (team chốt).** Dashboard phải có cả 2 theme. Thiết kế token theo `[data-theme="dark"]` / `[data-theme="light"]` ngay trong `site.css`; toggle ở topbar; lưu lựa chọn trong cookie (`VocaNova.Dashboard.Theme`) để SSR set `data-theme` trên `<html>` ngay lần đầu (tránh nhấp nháy). → ảnh hưởng trực tiếp [§4 Design System](#4-design-system).
- **R2 — Song ngữ EN + VI (LI-02, FE-17): BẮT BUỘC ✅ (team chốt).** Không hardcode chuỗi UI. Dùng .NET localization (`IStringLocalizer` + `Resources/*.resx`), `RequestLocalizationMiddleware`, culture cookie; language switch ở topbar. Mọi label/empty/error đi qua resource key. Bố trí ngay từ trang đầu (`F056`) — không retrofit. *(Lưu ý: 2 ràng buộc này áp dụng cho MỌI trang → tăng giờ từng trang; estimate dashboard cần cộng thêm.)*
- **R3 — 4 roles (LI-01): Super Admin / Admin / User / Guest — có sẵn trong bảng `roles`** (`role_id`: 1=super_admin, 2=admin, 3=user, 4=guest). Roles là **data-driven** (đọc từ DB), không hardcode. Dashboard chỉ cho `admin` + `super_admin` đăng nhập (đã chặn ở `DashboardAuthService`); User/Guest → không vào. Sidebar + actions phân quyền theo role (đã có `canManageAdminAccounts` cho `super_admin`). Tính năng "Super Admin only" (xóa từ, deactivate/restore user, admin accounts) phải ẩn UI với `admin` thường **đồng thời** dựa policy phía API (không chỉ ẩn UI).
- **R4 — Admin tự quản lý profile + settings (FE-15, FE-16).** Trang Profile (đổi display_name, avatar, mật khẩu) + Settings (theme, ngôn ngữ). → [§6b](#6b-trang-bổ-sung-từ-report-1-fe).
- **R5 — Multimedia gồm image + audio + VIDEO (LI-05, FE-07).** Kế hoạch media phải tính cả **video** (API hiện chỉ có image + audio → [§7 G11](#7-api-gaps--skeleton-contracts)).
- **R6 — Auto-suggest image/video (FE-08, LI-08)** phụ thuộc 3rd-party API → làm khung, có empty/disabled state ([§7 G12](#7-api-gaps--skeleton-contracts)).
- **R7 — AI semantic eval không đảm bảo 100% (LI-06).** Chỗ dashboard hiển thị kết quả AI grading phải ghi rõ "đánh giá tương đối", không coi là tuyệt đối.

### Quyết định scope v1 đã chốt (2026-06-20)
- ✅ **Dark/Light + EN/VI:** bắt buộc (R1/R2).
- ✅ **Roles:** data-driven từ bảng `roles` (4 role). FE-11 v1 = roles **list read-only** (+ optional gán role cho user); **BỎ HẲN phần permission khỏi v1** — quyền đã gắn theo role (admin có full CRUD, super_admin thêm quyền hệ thống), không dựng module permissions, không làm ma trận.
- ✅ **FE-10 Admin Account:** **vào v1 dạng KHUNG** — Tan dựng UI + ViewModel + gọi `IVocaNovaApiClient` theo contract [G9](#7-api-gaps--skeleton-contracts); **API do An làm sau**, ráp vào không phải sửa UI.
- ✅ **FE-03:** Admin có **Lock/Unlock user (G4)** + **Create/Update user (G8)** trong v1.
- ⏸ **FE-07 video (G11) + FE-08 auto-suggest media (G12): phase sau.**
- ❗ **API toàn bộ gap còn lại do An đảm nhiệm.** Tan luôn dựng khung trước theo contract; endpoint deploy khớp contract là page tự sáng đèn.

---

## 1. Hiện trạng (Current State Audit)

### Đã xong (`F055` — Auth & Layout)
- **Cookie auth** (`Program.cs`): scheme `Cookies`, cookie `VocaNova.Dashboard.Auth`, sliding 30 phút, `LoginPath=/login`, `FallbackPolicy = RequireAuthenticatedUser`.
- **`AuthController`**: `/login` (GET+POST), `/logout` (POST). Login gọi `POST /api/auth/login` → `GET /api/auth/me`, **chỉ cho `admin`/`super_admin`**, lưu claims + `access_token`/`refresh_token` vào cookie qua `properties.StoreTokens(...)`.
- **`DashboardAuthService`** (typed `HttpClient`): đọc envelope, map `TokenResponse` + `UserProfileDto`.
- **`_Layout.cshtml`**: app-shell 260px sidebar (dark) + content + sticky topbar, responsive. Sidebar: Dashboard / Vocabulary / Topics / Statistics / Users / Admin Accounts (super_admin). **Đa số link `href="#"` (chưa nối).**
- **`site.css`**: tokens + app-shell + login + responsive đã hoàn chỉnh.

### Chưa làm
- Mọi trang nghiệp vụ (`F056`–`F063`) + các trang bổ sung từ Report 1.
- **Chưa có authenticated API client** ([§3](#3-foundation-authenticated-api-client) — blocker).
- **Chưa có theme switching + i18n** (R1, R2).
- Sidebar thiếu: KNN Management, Activity Logs, Roles & Permissions, Profile/Settings, Vocabulary submenu, theme + language toggle.

### API (`VocaNova.API`) — envelope chuẩn
```jsonc
{ "success": true, "data": <T|[]>, "message": "Success.", "errors": [], "pagination": null }
{ "success": true, "data": [ ... ], "pagination": { "page":1, "limit":20, "totalItems":100, "totalPages":5 } }
{ "success": false, "data": null, "message": "...", "errors": ["..."] }
```
- JWT Bearer. `TokenResponse = { access_token, refresh_token, expires_in, token_type }`. JSON snake_case.
- Policy `Admin` (= `admin`∨`super_admin`), `SuperAdmin` (= `super_admin`). Roles: `user`/`admin`/`super_admin`.

---

## 2. Kiến trúc Dashboard

Dashboard = **BFF mỏng (server-side MVC)**: Razor view render HTML; data lấy từ `VocaNova.API` qua typed `HttpClient`. Token nằm server-side trong auth cookie (không lộ ra browser). AJAX gọi **action MVC của dashboard**, action đó mới gọi API.

```
Browser ──(cookie)──> Dashboard MVC ──(Bearer access_token từ cookie)──> VocaNova.API
   ▲                       │
   └── AJAX gọi action MVC ─┘   (proxy, không lộ token)
```

### Cấu trúc thư mục
```
VocaNova.Dashboard/
├─ Services/
│  ├─ Auth/                 (đã có)
│  └─ Api/                  ◀ MỚI
│     ├─ IVocaNovaApiClient.cs / VocaNovaApiClient.cs
│     ├─ ApiResult.cs / PagedApiResult.cs
│     ├─ BearerTokenHandler.cs       # gắn token + refresh 401
│     └─ ApiJson.cs                  # SnakeCaseLower options
├─ Models/Api/             ◀ MỚI  (DTO mirror API, snake_case)
├─ Resources/             ◀ MỚI  (R2: SharedResource.en.resx / .vi.resx)
├─ Controllers/           ◀ MỚI  (Vocabulary, Topics, Users, Statistics, Knn/*, AdminAccounts, Roles, ActivityLogs, Profile)
├─ ViewModels/            ◀ MỚI
└─ Views/                 ◀ MỚI
```

### Nguyên tắc
- **API DTO ≠ ViewModel** — map trước khi vào view.
- `JsonSerializerOptions` dùng chung: `PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower` + `PropertyNameCaseInsensitive`.
- Mọi call admin đi qua `IVocaNovaApiClient`.
- Lỗi API → ViewModel state (Loading / Empty / Error / Data), không throw lên user.
- **Mọi chuỗi UI qua `IStringLocalizer`** (R2). **Mọi màu qua CSS variable theo theme** (R1).

---

## 3. Foundation: Authenticated API Client

> 🔴 **Blocker — phải xong trước F056.** `F055.1` · Branch `feature/dashboard/api-client`

**Vấn đề:** login lưu `access_token` (TTL **15 phút**) + `refresh_token` (cookie **30 phút**) → sau 15 phút token hết hạn nhưng cookie còn sống → call `/api/admin/*` sẽ **401**. Cần client tự refresh. Hiện **chưa có gì dùng token để gọi admin API**.

**Done khi:**
- [ ] `BearerTokenHandler : DelegatingHandler` — lấy `access_token` từ `HttpContext.GetTokenAsync`, gắn `Authorization: Bearer`; nếu **401** → `POST /api/auth/refresh { refresh_token }` → cập nhật token trong cookie (re-`SignInAsync`) → **retry 1 lần**; refresh fail → 401 → redirect `/login`. Dùng `IHttpContextAccessor` (`AddHttpContextAccessor()`).
- [ ] `VocaNovaApiClient` (typed `HttpClient`, `BaseAddress` từ `DashboardApiOptions`): `GetAsync<T>`, `GetPagedAsync<T>`, `PostAsync<T>`, `PutAsync`, `PatchAsync`, `DeleteAsync`, `PostFormAsync` (multipart). Trả `ApiResult<T>` / `PagedApiResult<T>` — không throw cho 4xx.
- [ ] `IVocaNovaApiClient` gom theo module (Words/Topics/Users/Stats/Knn/...) — dễ mock.
- [ ] DI: `AddHttpClient<IVocaNovaApiClient, VocaNovaApiClient>().AddHttpMessageHandler<BearerTokenHandler>()`.
- [ ] `ApiJson.Default = { SnakeCaseLower, PropertyNameCaseInsensitive }`; refactor `DashboardAuthService` dùng chung.

---

## 4. Design System

Đây là *công cụ vận hành nội bộ*: đẹp = mật độ thông tin rõ, trạng thái nhất quán, đọc số nhanh. Dồn toàn bộ điểm nhấn vào **MỘT** thứ: thang màu CEFR.

### 4.1 Color tokens (R1: tách theme)
Giữ tokens `site.css`, bổ sung semantic + CEFR, và **tách theo theme**:
```css
:root, [data-theme="light"] {
  --bg:#f6f7fb; --surface:#fff; --ink:#19202a; --muted:#667085; --line:#d9dee8;
  --nav:#111827; --nav-soft:#202b3d;
  --accent:#0f766e; --accent-strong:#115e59; --danger:#b42318;
  --shadow:0 16px 36px rgba(15,23,42,.08);
  /* status */
  --ok:#067647; --ok-bg:#ecfdf3; --warn:#b54708; --warn-bg:#fffaeb;
  --err:#b42318; --err-bg:#fef3f2; --info:#175cd3; --info-bg:#eff8ff;
  /* SIGNATURE: thang khó CEFR A1→C2 */
  --cefr-a1:#15803d; --cefr-a1-bg:#edfcf2;  --cefr-a2:#0f766e; --cefr-a2-bg:#effcf9;
  --cefr-b1:#0369a1; --cefr-b1-bg:#eff8ff;  --cefr-b2:#4338ca; --cefr-b2-bg:#eef0ff;
  --cefr-c1:#7c3aed; --cefr-c1-bg:#f5f0ff;  --cefr-c2:#9333ea; --cefr-c2-bg:#faf0ff;
}
[data-theme="dark"] {
  --bg:#0d1117; --surface:#161b22; --ink:#e6edf3; --muted:#8b949e; --line:#2d333b;
  --nav:#010409; --nav-soft:#21262d;
  --accent:#2dd4bf; --accent-strong:#5eead4; --danger:#ff7b72;
  --shadow:0 16px 36px rgba(0,0,0,.4);
  --ok:#3fb950; --ok-bg:#0f2a18; --warn:#d29922; --warn-bg:#2b2008;
  --err:#ff7b72; --err-bg:#2d1513; --info:#58a6ff; --info-bg:#0d2136;
  /* CEFR trên nền tối: text tint nhạt, bg đậm */
  --cefr-a1:#56d364; --cefr-a1-bg:#0f2a18;  --cefr-a2:#2dd4bf; --cefr-a2-bg:#0c2b27;
  --cefr-b1:#58a6ff; --cefr-b1-bg:#0d2136;  --cefr-b2:#a5b4fc; --cefr-b2-bg:#1a1b3a;
  --cefr-c1:#c4b5fd; --cefr-c1-bg:#241a3a;  --cefr-c2:#d8b4fe; --cefr-c2-bg:#2a1a3a;
}
```
> Toggle theme: thêm `data-theme` vào `<html>` (server-side đọc cookie `VocaNova.Dashboard.Theme`, mặc định `light`). Toggle JS chỉ đổi attribute + ghi cookie → không reload.

### 4.2 Typography
- **Inter** cho UI/body (đã load). Không thêm display face.
- **Tabular numerals** mọi nơi số thẳng cột: `.stat-value, .data-table td, .badge-count { font-variant-numeric: tabular-nums; }`.
- **Mono** cho mã/ID (region `code`, user_id, audit `entity_type`/`entity_id`): `.mono { font-family: ui-monospace, "JetBrains Mono", monospace; }`.
- Scale: h1 `1.8rem/800`, h2 `1.15rem/700`, body `1rem`, caption `.82rem/--muted`, eyebrow `.75rem/800/uppercase/--accent`.

### 4.3 Signature — CEFR difficulty ladder
Thứ DUY NHẤT được "nổi". CEFR (A1→C2) là trục cốt lõi của hệ thống học từ; thang màu tăng dần (green→teal→blue→indigo→violet→purple) cho phép admin **đọc độ khó bằng màu** khi quét bảng. Dùng ở: badge CEFR (vocab table/detail), chú thích chart theo level, filter chip. Mọi thứ khác giữ trung tính + 1 accent teal. *(Chanel rule: thêm màu thứ hai "cho đẹp" → bỏ.)*

### 4.4 Foundations
Radius `8px`, control height `44px`, shadow chỉ ở surface nổi. Spacing `4/8/12/16/20/28`. Focus ring teal (đã có) — **không bỏ**. Respect `prefers-reduced-motion`.

---

## 5. Component Inventory

| Component | Mô tả | Dùng ở |
|---|---|---|
| `_StatCard` | eyebrow + số lớn (tabular) + delta | F056, F062 |
| `_DataTable` | server-side paginate, sort header, row actions, sticky header | F057, F060, F061, F063, FE-04/10/11 |
| `_FilterBar` | search + dropdown filters + toggle "Hiện đã xóa" | các list |
| `_Pagination` | map `pagination{...}` → control | các list |
| `_Badge` | status (active/locked/deleted) + CEFR (A1–C2) | F057, F060, F061 |
| `_ConfirmModal` / `_FormModal` | xác nhận delete · create/edit AJAX | mọi mutation |
| `_Toast` | success/error sau action | mọi mutation |
| `_ChartCard` | khung chart + dropdown granularity (Chart.js) | F056, F062 |
| `_StateBlock` | Loading (skeleton) / Empty (CTA) / Error (retry) / Data | mọi page |
| `_ThemeToggle` / `_LangSwitch` | R1/R2 — topbar | _Layout |

**Thư viện:** Bootstrap 5 (có); **Chart.js** self-host vào `wwwroot/lib` (không CDN). Mọi list/chart xử lý đủ Loading·Empty·Error: Empty = lời mời + nút; Error = nói rõ lỗi + nút thử lại.

---

## 6. Kế hoạch theo từng trang

> ✅ endpoint đã verify · 🟥 thiếu/cần khung ([§7](#7-api-gaps--skeleton-contracts)).

### F056 — Overview
- ✅ `GET /api/admin/stats/dashboard` → 4 `_StatCard`. ✅ `GET /api/admin/stats/learning.accuracy_trend` → line chart. 🟥 pie mastery toàn hệ thống (G2).
- Auto-refresh 5 phút. Done: 4 card thật, line chart, pie (hoặc empty), refresh, reduced-motion.

### F057 — Vocabulary List & Filter
- 🟥 `GET /api/admin/words?...&status=&includeDeleted=` (G1) — tạm dùng `GET /api/words` (active), disable toggle deleted. ✅ `GET /api/topics`.
- `_FilterBar` (search, CEFR, topic, toggle deleted) + `_DataTable` (word · CEFR badge · topic chips · status badge · Edit/Delete/Restore). Paginate server-side.

### F058 — Vocabulary Detail & Sense Management
- ✅ `GET /api/words/{id}`; senses `POST/PUT/DELETE/PATCH .../senses`; audio `POST/DELETE .../audio`; image `POST/PUT .../image`. 🟥 video (G11).
- Header (word, phonetic, CEFR badge, image, audio UK/US) → accordion senses (Edit/Delete AJAX) → add-sense form → examples inline → relations (view-only) → audio section (+ video section khi G11 sẵn).

### F059 — Vocabulary CSV Import
- ✅ `POST /api/admin/words/import` → `{ imported_words, imported_senses, skipped, errors[]{row,column,message} }`.
- Drag-drop + file picker; tải template (`wwwroot/templates/words-import-template.csv`); bảng kết quả; error rows đỏ; "Download errors as CSV".

### F060 — User Management
- ✅ `GET /api/admin/users?status=&q=&page=`; ✅ `GET /api/admin/users/{id}`; ✅ `PATCH .../deactivate|restore` (SuperAdmin). 🟥 lock/unlock (G4); 🟥 create/update user (G8); 🟥 test-history/activity per-user (G5).
- `Index`: list + filter status + search + toggle deleted; status badge. `Detail`: tabs **Profile | Learning Profile | Test History | Activity Log**. Nút Deactivate/Restore (SuperAdmin, confirm). Lock/Unlock + Create/Edit user disable kèm tooltip "Sắp có" tới khi G4/G8 sẵn.

### F061 — Topic Management
- ✅ `GET /api/topics` (+word_count); ✅ admin `POST/PUT/DELETE/PATCH restore /api/admin/topics`. 🟥 admin topic list includeDeleted (G6).
- `_DataTable` + CRUD `_FormModal` (icon, topic_name, topic_name_vi). Delete **disabled** khi `word_count>0` (tooltip). Restore deleted.

### F062 — Statistics
- ✅ `GET /api/admin/stats/learning` (top_wrong_words + accuracy_trend); ✅ `GET /api/admin/stats/demographics` (age/occupation/education). 🟥 granularity param (G7).
- Chart sessions/time (dropdown granularity AJAX) + accuracy trend + bảng Top 20 wrong words + 3 demographics chart.

### F063 — KNN Management
- ✅ `GET /api/admin/knn/config|rebuild-status`, `POST .../trigger-rebuild`; ✅ 5 lookup CRUD (`age-ranges|regions|occupations|education-levels|learning-purposes`).
- Submenu 5 trang lookup: `_FilterBar` + `_DataTable` + `_FormModal` + `_ConfirmModal` + restore. Trang tổng quan: config read-only + ghi chú "sửa `.env` rồi restart" + nút Trigger Rebuild (loading) + "Last rebuilt: X giờ trước".
- **Validation mirror backend:** name required + max (age 50, còn lại 100); description ≤255; age `min≤max`, `display_order≥0`; region `code` `[A-Z0-9_-]`, parent không tự tham chiếu/cycle. Không sửa `status` trực tiếp.

---

## 6b. Trang bổ sung từ Report 1 (FE)

> Các trang này có trong scope Report 1 nhưng **không nằm trong `F055`–`F063`**. Cần chốt với leader (An) xem đưa vào v1 không; phần lớn **chưa có API** → làm khung.

### FE-02 — Forgot Password (Dashboard)  🟡
- ✅ API có: `POST /api/auth/forgot-password` + `/api/auth/otp/verify` + `/api/auth/reset-password` (OTP theo phone). Dashboard **chưa có UI**.
- `/forgot-password`: nhập phone → gửi OTP → nhập OTP + mật khẩu mới → reset. Done: 3 bước; hiển thị rate-limit/expired rõ.

### FE-15 / FE-16 — Profile & Settings (của chính admin)  🟢
- ✅ API: `GET /api/auth/me`, `PUT /api/auth/me/profile`, `POST /api/auth/me/avatar`, `PUT /api/auth/me/password`.
- `/profile`: xem/sửa display_name, upload avatar, đổi mật khẩu. `/settings`: theme (R1) + ngôn ngữ (R2). Done: cập nhật profile/avatar/password; toggle theme + language lưu cookie.

### FE-04 — Activity Logs  🟢
- ✅ `GET /api/admin/audit-logs?user_id=&entity_type=&page=`.
- `/activity-logs`: `_DataTable` (created_at, user, action, entity_type `.mono`, entity_id, ip) + filter user/entity_type. Cũng dùng cho tab "Activity Log" trong F060 Detail (G5).

### FE-10 — Admin Account Management  ✅ v1 (khung) · (SuperAdmin)
- **Vào v1 dạng KHUNG.** Tan dựng đầy đủ UI (list + create/edit modal + delete confirm) + ViewModel + gọi `IVocaNovaApiClient` theo contract [G9](#7-api-gaps--skeleton-contracts); khi chưa có API → `_StateBlock` Empty/disabled. **API do An làm sau** → ráp vào không sửa UI. Sidebar "Admin Accounts" (super_admin) trỏ vào đây.

### FE-11 — Role & Permission Management  🟡/🟥 (SuperAdmin)
- **Roles: ĐÃ CÓ bảng `roles`** (4 role: super_admin/admin/user/guest) — nhưng **chưa có entity-CRUD endpoint** để dashboard gọi (chưa có `RolesController`). → cần `GET /api/admin/roles` (đọc bảng, dễ).
- **Permissions: CHƯA CÓ GÌ** — không có bảng permissions, không có role_permission mapping, không có entity/endpoint. → G10.
- **Quyết định v1 ✅:** trang Roles **list read-only** 4 role từ bảng (+ optional: gán role cho 1 user = đổi `role_id`). **BỎ HẲN "permission CRUD + assign permission"** khỏi v1 (quyền đã gắn theo role; admin có full CRUD). Không làm ma trận, không module RBAC động.

---

## 7. API Gaps & Skeleton Contracts

> Quy ước: dashboard **code trước theo contract** + `_StateBlock` Empty/disabled; backend trả 404/501 → page không vỡ; khi endpoint khớp contract deploy → page tự sáng đèn. **API toàn bộ gap dưới đây do An làm** (Tan chỉ dựng khung). `✅v1` = đã chốt vào v1; `⏸sau` = phase sau.

| # | Gap | Cần ở | Contract | Dashboard tạm xử lý |
|---|---|---|---|---|
| G1 | Admin word list (status+includeDeleted+search/cefr/topic) | `AdminWordsController` | `GET /api/admin/words?q=&cefr=&topicId=&status=&includeDeleted=&page=&limit=` → `PagedResult<WordSummaryDto>`(+status) | dùng `GET /api/words`; disable toggle deleted |
| G2 | Mastery distribution toàn hệ thống | `AdminStatsController` | `GET /api/admin/stats/mastery-distribution` → `[{mastery_level:0..5,count}]` | pie Empty |
| G3 | Sessions/ngày (overview line) | `AdminStatsController` | thêm `sessions_trend_7d:[{date,count}]` vào `stats/dashboard` | dùng `accuracy_trend.total_count` |
| G4 | `✅v1` User lock/unlock (status `locked`) | `AdminUsersController` | `PATCH /api/admin/users/{id}/lock` + `/unlock` (SuperAdmin) → `UserProfileDto` | nút disable + tooltip tới khi có |
| G5 | User test-history + activity cho admin | `AdminUsersController` | `GET /api/admin/users/{id}/test-history?page=` + `/activity?page=` | 2 tab Empty |
| G6 | Admin topic list includeDeleted | `AdminTopicsController` | `GET /api/admin/topics?q=&includeDeleted=&page=&limit=` → `PagedResult<TopicSummaryDto>` | dùng `GET /api/topics`; ẩn restore |
| G7 | Stats granularity | `AdminStatsController` | `GET /api/admin/stats/learning?granularity=daily\|weekly\|monthly` | fix 1 mức; dropdown disable |
| **G8** | `✅v1` **User Create/Update (FE-03)** | `AdminUsersController` | `POST /api/admin/users` + `PUT /api/admin/users/{id}` → `UserProfileDto` | nút Create/Edit disable tới khi có |
| **G9** | `✅v1 khung` **Admin Account CRUD (FE-10)** | mới `AdminAccountsController` | `GET/POST/PUT/DELETE /api/admin/admin-accounts` (SuperAdmin) | trang đầy đủ + Empty tới khi có |
| **G10a** | **Roles list (FE-11)** — bảng `roles` đã có, thiếu endpoint | mới `RolesController` | `GET /api/admin/roles` → `[{role_id,role_name}]` (read bảng); optional `PATCH /api/admin/users/{id}/role` gán role | trang Roles read-only |
| ~~G10b~~ | ~~Permissions (FE-11)~~ | — | — | ❌ **KHÔNG làm v1 (đã chốt bỏ)** |
| **G11** | `⏸sau` **Video multimedia (FE-07, LI-05)** | `AdminWordsController` | `POST /api/admin/words/{id}/video` (multipart) + `DELETE .../video/{id}` | ẩn video section |
| **G12** | `⏸sau` **Auto-suggest image/video (FE-08, LI-08)** | mới `MediaSuggestController` | `GET /api/admin/words/{id}/suggest-media?type=image\|video` → `[{url,thumbnail,source}]` (3rd-party) | nút "Gợi ý" disable |

**Quy ước "framework để chạy về sau":** tạo sẵn ViewModel + view + action gọi qua `IVocaNovaApiClient` tới URL contract; 404/501 → `_StateBlock` Empty/Disabled; backend deploy khớp contract là page tự hoạt động, không sửa UI.

---

## 8. Thứ tự thực hiện

```
0.  F055.1  API Client + refresh ........... 🔴 blocker
0b. R1 theme tokens + R2 i18n scaffold ..... làm cùng foundation (rẻ khi sớm, đắt khi muộn)
1.  Shared components (_DataTable, _Badge, _FilterBar, _StateBlock, _Toast, _Modal, _ChartCard, _ThemeToggle, _LangSwitch, Chart.js)
2.  F056 Overview ......... chốt pattern envelope/paginate/i18n/theme
3.  F057 → F058 → F059 .... module Vocabulary
4.  F061 Topic ............ chốt CRUD modal pattern → tái dùng F063
5.  F060 User
6.  F062 Statistics ....... tái dùng _ChartCard
7.  F063 KNN (5 lookup) ... nhân bản pattern F061
8.  FE-02 Forgot · FE-15/16 Profile/Settings · FE-04 Activity Logs   (đã có API)
9.  FE-10 Admin Accounts · FE-11 Roles  (khung, chờ G9/G10)
```
**Sidebar cuối:** Dashboard · Vocabulary(List/Import) · Topics · Users · Statistics · KNN Management · Activity Logs · *(super)* Admin Accounts · *(super)* Roles & Permissions · Profile/Settings; topbar có Theme toggle + Language switch. Phân quyền theo R3.

---

## 9. Figma Reconciliation Checklist
- [ ] Accent đúng teal `#0f766e`? Sidebar dark `#111827`, width 260px?
- [ ] **Dark theme** Figma có palette riêng? (đối chiếu §4.1 `[data-theme="dark"]`)
- [ ] CEFR badge — Figma có màu theo level chưa? (có → dùng Figma; chưa → giữ thang signature)
- [ ] Stat card / table (zebra? sticky? row height?) / badge style
- [ ] Font Inter? tabular nums? · Spacing main padding (28px) · card gap
- [ ] Vị trí Theme toggle + Language switch trên topbar
> Cần match pixel: gửi screenshot/export Overview, Vocab list, Vocab detail, 1 lookup KNN.

---

## 10. Copywriting conventions
- Active voice, đúng hành động: "Lưu thay đổi" (không "Submit"); xóa → toast "Đã xóa từ".
- Đặt tên theo thứ người dùng điều khiển ("Khóa tài khoản", không "set status=locked").
- Empty = lời mời + nút hành động. Error = nói rõ cái gì hỏng + nút thử lại (hiện `errors[]` từ API). Không xin lỗi mơ hồ.
- Nhất quán nhãn xuyên flow. Sentence case, không filler. Mỗi phần tử một nhiệm vụ.
- **R2:** mọi chuỗi qua resource key — viết copy cho cả EN + VI ngay khi tạo.

---

## 11. Bảng đối chiếu FE-01–FE-19

| FE | Tính năng (Report 1) | Trạng thái | Plan |
|---|---|---|---|
| FE-01 | Login | ✅ done | F055 |
| FE-02 | Forgot password | 🟡 API có, UI thiếu | §6b |
| FE-03 | User mgmt (Create/View/Update/Lock-Unlock/Delete) | ✅ v1 (View/Deactivate/Restore có API; Create/Update G8 + Lock/Unlock G4 → khung, An làm API) | F060 |
| FE-04 | Users' activity logs | ✅ API có | §6b |
| FE-05 | Vocabulary bank (CRUD+Import) | ✅ (list 🟥G1) | F057–F059 |
| FE-06 | Vocab metadata (word class/meaning/phonetic/example) | ✅ senses/examples | F058 |
| FE-07 | Multimedia (image/audio/**video**) | image+audio ✅ v1; video ⏸ phase sau (G11) | F058 |
| FE-08 | Auto suggest image/video | ⏸ phase sau (G12) | F058 |
| FE-09 | Topic & category mgmt | ✅ (includeDeleted 🟥G6) | F061 |
| FE-10 | Admin account mgmt | ✅ v1 khung (API: An làm sau — G9) | §6b |
| FE-11 | Role & permission mgmt | ✅ v1: roles list read-only (G10a, An làm endpoint); **permissions bỏ khỏi v1** | §6b |
| FE-12 | Dashboard statistics | ✅ | F056 |
| FE-13 | Learning stats + most incorrect | ✅ | F062 |
| FE-14 | Demographics | ✅ | F062 |
| FE-15 | Manage profile | ✅ API có | §6b |
| FE-16 | Manage settings | 🟢 theme/lang | §6b |
| FE-17 | EN + VI | 🔴 R2 bắt buộc ✅ (team chốt) | §0 |
| FE-18 | Dark + Light | 🔴 R1 bắt buộc ✅ (team chốt) | §0/§4 |
| FE-19 | KNN model mgmt | ✅ | F063 |

---

*File chính thức tại repo root. Bước tiếp: chốt scope §6b + rule §0 với leader → `F055.1` (API client + theme/i18n scaffold).*
