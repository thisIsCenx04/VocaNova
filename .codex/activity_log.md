
## 2026-06-13 - F055 Dashboard Cookie Auth & Layout
- Yêu cầu: tạo nơi lưu lịch sử hoạt động persistent memory để mất context vẫn đọc lại được.
- File memory: .codex/activity_log.md.
- Đã đọc VocaNova_Feature_Breakdown.md: Tan phụ trách MVC Dashboard F055-F063; F055 phụ thuộc F015.
- F055 Done khi: cookie auth trong Program.cs; AuthController Login/Logout; _Layout sidebar icons responsive; Admin không thấy tab Admin Accounts; redirect /login khi chưa auth và /dashboard sau login.
- API đã có POST /api/auth/login, POST /api/auth/logout, GET /api/auth/me; Dashboard sẽ dùng HTTP client gọi API thay vì tự truy cập DB.

### Kết quả triển khai
- Thêm Dashboard cookie auth trong `src/VocaNova.Dashboard/Program.cs` với fallback policy yêu cầu authenticated user, `/login` cho anonymous, route mặc định `/dashboard`.
- Thêm `DashboardAuthService` gọi `POST /api/auth/login`, `GET /api/auth/me`, `POST /api/auth/logout`; chỉ cho role `admin` và `super_admin` vào Dashboard.
- Thêm `AuthController` login/logout, `DashboardController` và các view `Views/Auth/Login.cshtml`, `Views/Dashboard/Index.cshtml`.
- Thay `_Layout.cshtml` bằng sidebar responsive; `Admin Accounts` chỉ hiện với `super_admin`.
- Thêm CSS dashboard trong `wwwroot/css/site.css`.
- Thêm appsettings `VocaNovaApi:BaseUrl`: production `https://localhost:7069`, development `http://localhost:5013`.
- Thêm tests `tests/VocaNova.Tests/Dashboard/DashboardAuthServiceTests.cs`.
- Verify: `dotnet build VocaNova.sln` pass 0 warning; `dotnet test VocaNova.sln --no-build` pass 164/164.
- Local Dashboard đã start tại `http://localhost:5236`; `GET /login` trả 200. Muốn login thật cần API chạy tại `http://localhost:5013`.

