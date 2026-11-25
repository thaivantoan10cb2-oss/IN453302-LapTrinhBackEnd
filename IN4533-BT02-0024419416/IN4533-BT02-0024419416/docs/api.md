# Thiết kế REST API – EduHub mini (registrations)

Tiền tố chung: `/api/v1`. Dữ liệu trao đổi: `application/json`; lỗi: `application/problem+json` (RFC 9457).

## 2.1. Bảng thiết kế

| # | Chức năng | Method | URI | Mã thành công | Mã lỗi có thể gặp |
|---|-----------|--------|-----|---------------|-------------------|
| 1 | Xem danh sách đăng ký của một khoá học | GET | `/api/v1/courses/{courseId}/registrations` | 200 OK | 404 Not Found (không có khoá học) |
| 2 | Tạo đăng ký | POST | `/api/v1/courses/{courseId}/registrations` | 201 Created (kèm header `Location`) | 400 Bad Request (thiếu/sai `memberId`); 404 Not Found (khoá học hoặc thành viên không tồn tại); 409 Conflict (đã đăng ký khoá này, hoặc khoá đã đủ chỗ) |
| 3 | Xác nhận đăng ký (body `{"status":"confirmed"}`) | PATCH | `/api/v1/registrations/{id}` | 200 OK | 400 Bad Request (`status` thiếu/không hợp lệ); 404 Not Found (không có phiếu); 409 Conflict (phiếu đã `confirmed` hoặc đã `cancelled`) |
| 4 | Huỷ đăng ký (body `{"status":"cancelled"}`) | PATCH | `/api/v1/registrations/{id}` | 200 OK | 400 Bad Request; 404 Not Found; 409 Conflict (phiếu đã `cancelled`) |

### Giải thích lựa chọn thiết kế

- URI chỉ chứa danh từ số nhiều (`courses`, `registrations`), không có động từ như `confirm`, `cancel`. Hành động được biểu diễn bằng method + thay đổi trạng thái của tài nguyên.
- Tạo đăng ký lồng dưới `courses/{courseId}` vì phiếu thuộc về một khoá học; `memberId` nằm trong body.
- Xác nhận/huỷ dùng `PATCH` vì chỉ cập nhật một phần (trường `status`). Huỷ là "huỷ mềm" (đổi `status` thành `cancelled`, giữ lịch sử), nên không dùng `DELETE`.
- Chuyển trạng thái hợp lệ: `pending → confirmed`, `pending → cancelled`, `confirmed → cancelled`. `cancelled` là trạng thái cuối; mọi chuyển khác trả 409.
- Ràng buộc `UNIQUE (member_id, course_id)` ở CSDL được ánh xạ thành 409 khi tạo trùng.

## 2.2. JSON mẫu cho chức năng "Tạo đăng ký"

### Request

```http
POST /api/v1/courses/2/registrations
Content-Type: application/json

{ "memberId": 3 }
```

### Response thành công

```http
HTTP/1.1 201 Created
Location: /api/v1/registrations/7
Content-Type: application/json

{
  "id": 7,
  "memberId": 3,
  "courseId": 2,
  "status": "pending"
}
```

### Response lỗi – trùng đăng ký (Problem Details, RFC 9457)

```http
HTTP/1.1 409 Conflict
Content-Type: application/problem+json

{
  "type": "https://eduhub.example.com/problems/duplicate-registration",
  "title": "Duplicate registration",
  "status": 409,
  "detail": "Member 3 has already registered for course 2.",
  "instance": "/api/v1/courses/2/registrations"
}
```
