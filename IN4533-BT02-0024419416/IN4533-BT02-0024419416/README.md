# IN4533-BT02-0024419416 – EduHub mini

- **Họ tên:** Thái Văn Toàn
- **MSSV:** 0024419416
- **Học phần:** Lập trình Back End (IN4533) – Trường Đại học Đồng Tháp

## 1. Cách cài PostgreSQL (đã dùng: Docker)

```bash
docker run --name eduhub-pg -e POSTGRES_PASSWORD=eduhub_dev_123 -p 5432:5432 -d postgres:16
docker exec -it eduhub-pg psql -U postgres -c "CREATE DATABASE eduhub;"
docker exec -it eduhub-pg psql -U postgres -d eduhub     # kết nối thành công → chụp màn hình
```

Ảnh kết nối thành công: `docs/images/01-ket-noi-psql.png`.

> `eduhub_dev_123` chỉ là mật khẩu thử nghiệm của container Docker chạy cục bộ trên máy cá nhân, không phải mật khẩu thật.

## 2. Chạy ba tệp SQL từ đầu

```bash
docker cp sql eduhub-pg:/sql
docker exec eduhub-pg psql -U postgres -d eduhub -f /sql/01-schema.sql
docker exec eduhub-pg psql -U postgres -d eduhub -f /sql/02-seed.sql
docker exec eduhub-pg psql -U postgres -d eduhub -f /sql/03-queries.sql
```

(Nếu máy có sẵn `psql`: `psql -h localhost -U postgres -d eduhub -f sql/01-schema.sql`, …)

Kết quả truy vấn kỳ vọng với dữ liệu mẫu:
(a) 3 dòng: Nguyễn Văn An – Cơ sở dữ liệu; Nguyễn Văn An – Lập trình Back End; Trần Thị Bình – Lập trình Back End.
(b) Lập trình Back End 3; Cơ sở dữ liệu 2; Lập trình Web 2; Trí tuệ nhân tạo 0.
(c) Võ Hoàng Em.

## 3. Thiết kế và kiểm thử API

- Thiết kế: [`docs/api.md`](docs/api.md)
- Lệnh curl: [`docs/requests.md`](docs/requests.md)

## 4. Chạy Minimal API (.NET 8, Npgsql)

PowerShell:

```powershell
cd src/EduHub.Api
$env:ConnectionStrings__EduHub = "Host=localhost;Port=5432;Database=eduhub;Username=postgres;Password=eduhub_dev_123"
dotnet run --urls http://localhost:5000
```

Bash: `export ConnectionStrings__EduHub="Host=localhost;Port=5432;Database=eduhub;Username=postgres;Password=..."`

Endpoint đã cài: GET/POST `/api/v1/courses/{courseId}/registrations`, PATCH `/api/v1/registrations/{id}`, GET `/api/v1/registrations/{id}`.
Ảnh gọi thử: `docs/images/02-get-registrations.png`, `docs/images/03-post-registration.png`.
