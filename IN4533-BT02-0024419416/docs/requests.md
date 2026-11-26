# Kiểm thử API bằng curl

Điều kiện: đã chạy `sql/01-schema.sql` và `sql/02-seed.sql` trên PostgreSQL, và API đang chạy tại `http://localhost:5000` (xem `README.md`). Chạy các lệnh **theo thứ tự** dưới đây vì lệnh sau dựa vào trạng thái do lệnh trước để lại.

## 4 lệnh tương ứng 4 chức năng

### 1. Xem danh sách đăng ký của khoá học 1

```bash
curl -i http://localhost:5000/api/v1/courses/1/registrations
```

Mã dự kiến: **200 OK**, body là mảng JSON gồm 3 phiếu (id 1, 3, 7).

### 2. Tạo đăng ký (thành viên 5 đăng ký khoá 4)

```bash
curl -i -X POST http://localhost:5000/api/v1/courses/4/registrations \
     -H "Content-Type: application/json" -d '{"memberId": 5}'
```

Mã dự kiến: **201 Created**, có header `Location: /api/v1/registrations/8`, body `{"id":8,"memberId":5,"courseId":4,"status":"pending"}`.

### 3. Xác nhận đăng ký (phiếu 8 vừa tạo)

```bash
curl -i -X PATCH http://localhost:5000/api/v1/registrations/8 \
     -H "Content-Type: application/json" -d '{"status": "confirmed"}'
```

Mã dự kiến: **200 OK**, body có `"status":"confirmed"`.

### 4. Huỷ đăng ký (phiếu 8)

```bash
curl -i -X DELETE http://localhost:5000/api/v1/registrations/8
```

Mã dự kiến: **204 No Content**, không có body. Trong CSDL phiếu 8 chuyển sang `status = 'cancelled'`.

## Các lệnh kiểm tra lỗi

| Mục đích | Lệnh | Mã dự kiến |
|----------|------|-----------|
| Trùng đăng ký (thành viên 3 đã đăng ký khoá 2) | `curl -i -X POST http://localhost:5000/api/v1/courses/2/registrations -H "Content-Type: application/json" -d '{"memberId": 3}'` | **409 Conflict** (`application/problem+json`) |
| Khoá học không tồn tại | `curl -i http://localhost:5000/api/v1/courses/99/registrations` | **404 Not Found** |
| Thành viên không tồn tại | `curl -i -X POST http://localhost:5000/api/v1/courses/1/registrations -H "Content-Type: application/json" -d '{"memberId": 99}'` | **404 Not Found** |
| Body thiếu `memberId` | `curl -i -X POST http://localhost:5000/api/v1/courses/1/registrations -H "Content-Type: application/json" -d '{}'` | **400 Bad Request** |
| Trạng thái không hợp lệ | `curl -i -X PATCH http://localhost:5000/api/v1/registrations/1 -H "Content-Type: application/json" -d '{"status": "abc"}'` | **400 Bad Request** |
| Xác nhận phiếu đã huỷ (phiếu 4 là `cancelled`) | `curl -i -X PATCH http://localhost:5000/api/v1/registrations/4 -H "Content-Type: application/json" -d '{"status": "confirmed"}'` | **409 Conflict** |
| Phiếu không tồn tại | `curl -i -X DELETE http://localhost:5000/api/v1/registrations/999` | **404 Not Found** |

> Trên Windows PowerShell, `curl` là bí danh của `Invoke-WebRequest`; hãy dùng `curl.exe` thay cho `curl`, và dùng dấu `^` hoặc dấu backtick thay cho `\` để xuống dòng. Với `-d`, bao JSON bằng nháy kép và thoát dấu nháy kép bên trong: `-d "{\"memberId\": 5}"`.

## Chạy lại từ đầu

Dữ liệu bị thay đổi sau khi thử. Để đưa CSDL về trạng thái ban đầu:

```bash
psql -U postgres -d eduhub -f sql/01-schema.sql -f sql/02-seed.sql
```
