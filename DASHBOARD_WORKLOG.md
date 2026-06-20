# VocaNova Dashboard — Worklog (Lịch sử làm việc)

> Đi kèm [`DASHBOARD_PLAN.md`](DASHBOARD_PLAN.md). **Mục đích:** ghi lại từng việc ĐÃ làm để lần sau làm tiếp thì dò nhanh, **tránh làm trùng / tốn token**.
> Quy ước: làm xong 1 piece → cập nhật bảng + ghi 1 dòng log (ngày, file đụng tới, kết quả build). Code theo **từng bước nhỏ**, không làm 1 lượt.
>
> Trạng thái: ⬜ chưa làm · 🔄 đang làm · ✅ xong · ⏸ tạm dừng/chờ API

---

## Bảng tiến độ

| Bước | Mô tả | Branch | Trạng thái | Ngày | Ghi chú |
|---|---|---|---|---|---|
| F055 | Cookie auth + layout + login | `feature/dashboard/auth-layout` | ✅ | (trước) | đã có sẵn |
| **F055.1** | **Authenticated API client + refresh** | `feature/dashboard/api-client` | ✅ (S5 verify ở F056) | 2026-06-21 | code xong, build sạch |
| F055.2 | Theme dark/light scaffold (R1) | `feature/dashboard/theme` | ✅ | 2026-06-21 | tokens + `<html data-theme>` từ cookie + toggle topbar |
| F055.3 | i18n EN/VI scaffold (R2) | `feature/dashboard/i18n` | ✅ | 2026-06-21 | localization + `_LangSwitch` + nav/login localized |
| F055.4 | Shared components + Chart.js | `feature/dashboard/shared-ui` | ✅ | 2026-06-21 | CSS + partials + modals/toast/JS; Chart.js cần `libman restore` |
| F056 | Overview (stat cards + charts) | `feature/dashboard/overview` | ⬜ | | cần F055.1 |
| F057 | Vocabulary list & filter | `feature/dashboard/vocab-list` | ⬜ | | API word list 🟥 G1 (An) |
| F058 | Vocabulary detail & sense mgmt | `feature/dashboard/vocab-detail` | ⬜ | | |
| F059 | Vocabulary CSV import | `feature/dashboard/vocab-import` | ⬜ | | |
| F061 | Topic management | `feature/dashboard/topic-management` | ⬜ | | làm trước F060 để chốt CRUD modal |
| F060 | User management (+ G4 lock/unlock, G8 create/update — khung) | `feature/dashboard/user-management` | ⬜ | | API: An |
| F062 | Statistics | `feature/dashboard/statistics` | ⬜ | | |
| F063 | KNN management (5 lookup) | `feature/dashboard/knn-management` | ⬜ | | |
| FE-02 | Forgot password | `feature/dashboard/forgot-password` | ⬜ | | API có sẵn |
| FE-15/16 | Profile & Settings | `feature/dashboard/profile-settings` | ⬜ | | API có sẵn |
| FE-04 | Activity logs | `feature/dashboard/activity-logs` | ⬜ | | API có sẵn |
| FE-10 | Admin accounts (khung) | `feature/dashboard/admin-accounts` | ⬜ | | API: An (G9) |

> **FE-11 permissions, FE-07 video (G11), FE-08 auto-suggest (G12):** ❌/⏸ không làm v1 (xem plan §0).

---

## F055.1 — Sub-steps (API client)

- [x] **S1 — Shared API contracts + JSON** · `Services/Api/ApiJson.cs`, `Services/Api/ApiResult.cs` ✅ build sạch
- [x] **S2 — `BearerTokenHandler`** (gắn token + refresh 401 + retry) · `Services/Api/BearerTokenHandler.cs` ✅ build sạch
- [x] **S3 — `IVocaNovaApiClient` + `VocaNovaApiClient`** (GET/GetPaged/POST/PUT/PATCH/DELETE/Form) · `Services/Api/*.cs` ✅ build sạch
- [x] **S4 — DI trong `Program.cs`** (`AddHttpContextAccessor`, `AddTransient<BearerTokenHandler>`, `AddHttpClient<IVocaNovaApiClient,VocaNovaApiClient>().AddHttpMessageHandler<BearerTokenHandler>()`) + refactor `DashboardAuthService` dùng `ApiJson.Default` ✅ build sạch
- [ ] **S5 — Smoke test (manual):** verify khi làm F056 — chạy cả API + Dashboard, login → gọi `GET /api/admin/stats/dashboard`, xác nhận 200 + auto-refresh sau 15 phút

---

## Nhật ký (mới nhất ở trên)

### 2026-06-21
- **F055.4 xong** (nhánh `feature/dashboard/shared-ui`):
  - step1: CSS toàn bộ component vào `site.css` (stat card, badge status+CEFR, filter bar, data table sticky, pagination, state block + skeleton, modal theming, toast, chart card).
  - step2: `Models/Components/ComponentViewModels.cs` (+ `BadgeModifiers`) + partials `Components/_Badge`, `_StatCard`, `_StateBlock`, `_Pagination`; `_ViewImports` thêm `@using ...Models.Components`.
  - step3: `_Toast` (TempData) + `_ConfirmModal` + `_FormModal` gắn vào `_Layout`; JS `site.js` (`vnToast`, confirm, form-modal AJAX); key `Common.*`/`Confirm.Title`.
  - step4: `_ChartCard` + `ChartCardViewModel` + `libman.json` (chart.js 4.4.1). **Phải chạy `libman restore`** để có `wwwroot/lib/chart/chart.umd.min.js` (cần internet máy bạn). Page nào dùng chart thì include script này trong `@@section Scripts`.
  - step5: sidebar `_Layout` thêm **KNN** + **Activity Logs** (link `#` tạm); key `Nav.Knn`/`Nav.ActivityLogs`/`Chart.*`. (Vocabulary submenu để khi có page F057/F059.)
  - Build → 0 warning, 0 error. Commit gợi ý: `feat(dashboard): add shared UI components (cards, table, badges, modals, toast, charts)`.
- **F055.3 xong** (nhánh `feature/dashboard/i18n`):
  - S1: `Program.cs` `AddLocalization`+`AddViewLocalization`+`UseRequestLocalization` (en, vi, default en, culture cookie); `SharedResource.cs`; `Resources/SharedResource.{en,vi}.resx`; `_ViewImports` inject `IStringLocalizer<SharedResource> L`.
  - S2: `CultureController` (POST `/culture/set` + antiforgery) + `Views/Shared/_LangSwitch.cshtml` (dropdown EN/VI topbar) + CSS.
  - S3: localize `_Layout` (nav + đăng xuất + theme aria) + `Login.cshtml` (title/subtitle/submit). Key `Nav.*`/`Account.*`/`Theme.*`/`Lang.*`/`Login.*`.
  - Build → 0 warning, 0 error. Commit gợi ý: `feat(dashboard): add EN/VI localization with language switch`.
- **F055.2 xong** (nhánh `feature/dashboard/theme`):
  - step1: `site.css` tách token `[data-theme="light"]`/`[data-theme="dark"]` + status + thang CEFR + `--accent-soft`/`--auth-gradient`.
  - step2: `_Layout.cshtml` đọc cookie `VocaNova.Dashboard.Theme` → `<html data-theme="...">` server-side.
  - step3: nút `#theme-toggle` ở topbar (moon/sun) + `site.js` đổi `data-theme` & ghi cookie; đổi màu hardcode (focus, avatar, auth gradient) sang biến; transition mượt + reduced-motion.
  - Build → 0 warning, 0 error. Commit gợi ý: `feat(dashboard): add theme toggle + persist`.
- Khởi tạo worklog. Bắt đầu `F055.1`.
- **S1 xong:** tạo `Services/Api/ApiJson.cs` (JSON SnakeCaseLower dùng chung) + `Services/Api/ApiResult.cs` (`ApiEnvelope<T>` internal, `PaginationInfo`, `ApiResult<T>`, `PagedApiResult<T>`). `dotnet build` Dashboard → 0 warning, 0 error.
- **S2 xong:** tạo `Services/Api/BearerTokenHandler.cs` (DelegatingHandler: gắn Bearer từ cookie → 401 thì `POST /api/auth/refresh` → `StoreTokens` + `SignInAsync` cập nhật cookie → retry request 1 lần; buffer body để retry POST/PUT; refresh fail giữ 401). Chưa wire DI (để S4). Build → 0 warning, 0 error.
- **S3 xong:** `Services/Api/IVocaNovaApiClient.cs` + `VocaNovaApiClient.cs`. Helper `GetAsync<T>` / `GetPagedAsync<T>` / `PostAsync<T>` / `PutAsync<T>` / `PatchAsync<T>` / `DeleteAsync<T>` / `PostFormAsync<T>`; deserialize `ApiEnvelope<T>` → `ApiResult<T>`/`PagedApiResult<T>`; bắt `HttpRequestException`/`TaskCanceledException`/`JsonException` → fail mềm (statusCode 0, message "Không kết nối được máy chủ"). Chưa wire DI (để S4). Build → 0 warning, 0 error.
- **S4 xong:** `Program.cs` thêm `AddHttpContextAccessor()` + `AddTransient<BearerTokenHandler>()` + `AddHttpClient<IVocaNovaApiClient,VocaNovaApiClient>(BaseAddress).AddHttpMessageHandler<BearerTokenHandler>()`; `DashboardAuthService` chuyển dùng `ApiJson.Default`. Build → 0 warning, 0 error. **→ F055.1 hoàn tất phần code.**
- **Commit milestone F055.1:** branch `feature/dashboard/api-client` · `feat(dashboard): add authenticated API client with auto token refresh`.
- Tiếp theo: **F056 — Overview** (sẽ là smoke test thực tế cho API client).
