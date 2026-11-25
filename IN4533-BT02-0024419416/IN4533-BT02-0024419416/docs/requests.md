# Lệnh kiểm thử API (curl)

Giả sử API chạy tại `http://localhost:5000` (xem README). Dữ liệu theo `sql/02-seed.sql`
(khoá học 2 = "Cơ sở dữ liệu"; thành viên 5 chưa đăng ký khoá nào).
Trên Windows CMD, thay `\` cuối dòng bằng `^` và dùng `\"` trong JSON, hoặc dùng Git Bash / Postman.

## 1. Xem danh sách đăng ký của khoá học 2 — dự kiến 200 OK

```bash
curl -i http://localhost:5000/api/v1/courses/2/registrations
```

## 2. Tạo đăng ký (thành viên 5 vào khoá 2) — dự kiến 201 Created

```bash
curl -i -X POST http://localhost:5000/api/v1/courses/2/registrations \
  -H "Content-Type: application/json" -d '{"memberId": 5}'
```

Chạy lại đúng lệnh này lần nữa — dự kiến 409 Conflict (trùng đăng ký, `application/problem+json`).

## 3. Xác nhận đăng ký (phiếu mới tạo, giả sử id = 8) — dự kiến 200 OK

```bash
curl -i -X PATCH http://localhost:5000/api/v1/registrations/8 \
  -H "Content-Type: application/json" -d '{"status": "confirmed"}'
```

## 4. Huỷ đăng ký (phiếu id = 8) — dự kiến 200 OK

```bash
curl -i -X PATCH http://localhost:5000/api/v1/registrations/8 \
  -H "Content-Type: application/json" -d '{"status": "cancelled"}'
```

## Các trường hợp lỗi bổ sung

| Lệnh | Dự kiến |
|------|---------|
| `curl -i http://localhost:5000/api/v1/courses/999/registrations` | 404 Not Found |
| `curl -i -X POST http://localhost:5000/api/v1/courses/2/registrations -H "Content-Type: application/json" -d '{}'` | 400 Bad Request |
| `curl -i -X POST http://localhost:5000/api/v1/courses/2/registrations -H "Content-Type: application/json" -d '{"memberId": 999}'` | 404 Not Found |
| `curl -i -X PATCH http://localhost:5000/api/v1/registrations/8 -H "Content-Type: application/json" -d '{"status": "confirmed"}'` (sau khi đã huỷ) | 409 Conflict |
| `curl -i -X PATCH http://localhost:5000/api/v1/registrations/8 -H "Content-Type: application/json" -d '{"status": "abc"}'` | 400 Bad Request |
