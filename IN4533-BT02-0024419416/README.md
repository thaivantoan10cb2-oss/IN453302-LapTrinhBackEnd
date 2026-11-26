# IN4533-BT02-MSSV: EduHub mini (PostgreSQL và REST API)

- **Họ và tên:** Võ Văn Toàn
- **MSSV:** 0024417563
- **Học phần:** Lập trình BackEnd (IN4533), Trường Đại học Đồng Tháp
- **Bài tập:** 02, học kỳ 1, năm học 2026 – 2027

## 1. Cách cài PostgreSQL

> Sửa mục này cho đúng với cách bạn **thực sự** đã cài. Dưới đây là cách dùng Docker.

Cài PostgreSQL bằng Docker (không cần cài trực tiếp lên máy):

```bash
docker run --name eduhub-pg -e POSTGRES_PASSWORD=<mat_khau_cua_ban> -p 5432:5432 -d postgres:16
docker exec -it eduhub-pg psql -U postgres -c "CREATE DATABASE eduhub;"
```

Kiểm tra kết nối bằng `psql`:

```bash
docker exec -it eduhub-pg psql -U postgres -d eduhub -c "SELECT version();"
```

Ảnh chụp màn hình kết nối thành công: `docs/images/01-ket-noi-postgres.png`.

## 2. Chạy cơ sở dữ liệu từ đầu bằng ba tệp SQL

Từ thư mục gốc của kho:

```bash
psql -U postgres -d eduhub -f sql/01-schema.sql   # tạo 3 bảng (chạy lại được nhiều lần)
psql -U postgres -d eduhub -f sql/02-seed.sql     # dữ liệu mẫu
psql -U postgres -d eduhub -f sql/03-queries.sql  # 3 truy vấn (a), (b), (c)
```

Nếu dùng Docker, thêm tiền tố `docker exec -i eduhub-pg` và đưa nội dung tệp vào bằng `<`:

```bash
docker exec -i eduhub-pg psql -U postgres -d eduhub < sql/01-schema.sql
```

Kết quả mong đợi của `03-queries.sql`:

| Truy vấn | Kết quả |
|----------|---------|
| (a) | Lê Hoàng Cường ; Nguyễn Văn An và Trần Thị Bình |
| (b) | Back End = 3, PostgreSQL = 2, RESTful API = 2, Docker cơ bản = 0 |
| (c) | Võ Thị Em |

## 3. Chạy API (ASP.NET Core Minimal API)

Yêu cầu: .NET SDK 8 trở lên. Nếu dùng SDK mới hơn, đổi `TargetFramework` trong `src/EduHub.Api/EduHub.Api.csproj` cho khớp (ví dụ `net10.0`).

Không ghi mật khẩu thật vào `appsettings.json`. Đặt chuỗi kết nối bằng biến môi trường:

```bash
# Linux / macOS
export ConnectionStrings__EduHub="Host=localhost;Port=5432;Database=eduhub;Username=postgres;Password=<mat_khau_cua_ban>"

# Windows PowerShell
$env:ConnectionStrings__EduHub = "Host=localhost;Port=5432;Database=eduhub;Username=postgres;Password=<mat_khau_cua_ban>"
```

Chạy:

```bash
cd src/EduHub.Api
dotnet restore
dotnet run
```

API lắng nghe tại `http://localhost:5000`. Các endpoint đã cài đặt: cả 4 chức năng trong `docs/api.md` (GET danh sách, POST tạo, PATCH xác nhận, DELETE huỷ). Lệnh thử nằm ở `docs/requests.md`.

## 4. Cấu trúc kho

```
IN4533-BT02-MSSV/
  sql/01-schema.sql   sql/02-seed.sql   sql/03-queries.sql
  docs/api.md         docs/requests.md  docs/images/
  src/EduHub.Api/     (Program.cs, EduHub.Api.csproj, appsettings.json)
  README.md
```

## 5. Ảnh chụp màn hình (thư mục `docs/images/`)

Xem danh sách ảnh cần chụp trong `docs/images/README.md`.
