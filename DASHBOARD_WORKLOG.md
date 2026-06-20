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
| F055.3 | i18n EN/VI scaffold (R2) | `feature/dashboard/i18n` | ⬜ | | `IStringLocalizer` + `.resx` + middleware |
| — | Shared components (_DataTable, _Badge, _FilterBar, _StateBlock, _Toast, _Modal, _ChartCard) | `feature/dashboard/shared-ui` | ⬜ | | Chart.js self-host |
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
