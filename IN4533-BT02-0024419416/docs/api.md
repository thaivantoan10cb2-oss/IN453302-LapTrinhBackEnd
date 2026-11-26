# Thiết kế REST API – EduHub mini (registrations)

Base URL: `http://localhost:5000/api/v1`. Dữ liệu trao đổi là JSON (UTF-8), tên trường dùng camelCase.

## 1. Bảng thiết kế

| # | Chức năng | Method | URI | Mã thành công | Mã lỗi có thể gặp |
|---|-----------|--------|-----|---------------|-------------------|
| 1 | Xem danh sách đăng ký của một khoá học | `GET` | `/api/v1/courses/{courseId}/registrations` | `200 OK` | `404` khoá học không tồn tại; `400` `courseId` không phải số nguyên |
| 2 | Tạo đăng ký | `POST` | `/api/v1/courses/{courseId}/registrations` | `201 Created` (kèm header `Location`) | `400` body sai hoặc thiếu `memberId`; `404` khoá học hoặc thành viên không tồn tại; `409` thành viên đã đăng ký khoá này, hoặc khoá đã đủ chỗ; `415` thiếu `Content-Type: application/json` |
| 3 | Xác nhận đăng ký | `PATCH` | `/api/v1/registrations/{id}` với body `{"status": "confirmed"}` | `200 OK` | `400` `status` không hợp lệ; `404` phiếu không tồn tại; `409` phiếu đã `cancelled` nên không xác nhận được |
| 4 | Huỷ đăng ký | `DELETE` | `/api/v1/registrations/{id}` | `204 No Content` | `404` phiếu không tồn tại |

## 2. Quy ước thiết kế

- **URI chỉ dùng danh từ số nhiều** (`courses`, `registrations`); hành động nằm ở method HTTP, không đặt động từ như `/confirm` hay `/cancel` vào URI.
- **Tạo và xem** là tài nguyên con của khoá học (`/courses/{courseId}/registrations`) vì phiếu luôn thuộc về một khoá. **Xác nhận và huỷ** thao tác trên một phiếu cụ thể nên dùng `/registrations/{id}`.
- **PATCH** dùng để xác nhận vì chỉ sửa một phần của tài nguyên (trường `status`). Xác nhận phiếu đã `confirmed` thì vẫn trả `200` (idempotent).
- **DELETE** thực hiện huỷ mềm: bản ghi được giữ lại, `status` chuyển thành `cancelled`. Gọi lại nhiều lần vẫn trả `204`.
- Mọi lỗi trả về theo **Problem Details (RFC 9457)** với `Content-Type: application/problem+json`.
- Phiếu mới luôn có `status = "pending"`.

## 3. JSON mẫu cho chức năng tạo đăng ký

### 3.1. Request tạo đăng ký (thành công)

Thành viên 5 chưa đăng ký khoá 4 trong dữ liệu mẫu nên yêu cầu này hợp lệ.

```http
POST /api/v1/courses/4/registrations HTTP/1.1
Content-Type: application/json

{ "memberId": 5 }
```

### 3.2. Response thành công

```http
HTTP/1.1 201 Created
Location: /api/v1/registrations/8
Content-Type: application/json

{
  "id": 8,
  "memberId": 5,
  "courseId": 4,
  "status": "pending"
}
```

### 3.3. Response lỗi: trùng đăng ký (Problem Details)

Request: `POST /api/v1/courses/2/registrations` với body `{ "memberId": 3 }`. Thành viên 3 đã có phiếu ở khoá 2 nên vi phạm `UNIQUE (member_id, course_id)`.

```http
HTTP/1.1 409 Conflict
Content-Type: application/problem+json

{
  "type": "https://eduhub.example.com/problems/duplicate-registration",
  "title": "Duplicate registration",
  "status": 409,
  "detail": "Thành viên 3 đã đăng ký khoá học 2, mỗi thành viên chỉ được đăng ký một khoá học một lần.",
  "instance": "/api/v1/courses/2/registrations"
}
```

Ý nghĩa các trường theo RFC 9457:

| Trường | Ý nghĩa |
|--------|---------|
| `type` | URI định danh loại lỗi (chỉ là định danh, không bắt buộc phải mở được) |
| `title` | Tóm tắt ngắn, cùng loại lỗi thì giống nhau |
| `status` | Mã HTTP, trùng với mã trong dòng trạng thái |
| `detail` | Mô tả lỗi cụ thể của lần gọi này |
| `instance` | URI của lần gọi gây ra lỗi |

### 3.4. Response lỗi khác (tham khảo)

`404 Not Found` khi khoá học không tồn tại:

```json
{
  "type": "https://eduhub.example.com/problems/course-not-found",
  "title": "Course not found",
  "status": 404,
  "detail": "Không tìm thấy khoá học có id 99.",
  "instance": "/api/v1/courses/99/registrations"
}
```

`400 Bad Request` khi body thiếu `memberId`:

```json
{
  "type": "https://eduhub.example.com/problems/invalid-request",
  "title": "Invalid request",
  "status": 400,
  "detail": "memberId là bắt buộc và phải là số nguyên dương.",
  "instance": "/api/v1/courses/2/registrations"
}
```

## 4. Response mẫu cho chức năng xem danh sách

```http
GET /api/v1/courses/1/registrations
HTTP/1.1 200 OK
Content-Type: application/json

[
  { "id": 1, "memberId": 1, "memberName": "Nguyễn Văn An", "courseId": 1, "status": "confirmed" },
  { "id": 3, "memberId": 2, "memberName": "Trần Thị Bình", "courseId": 1, "status": "confirmed" },
  { "id": 7, "memberId": 4, "memberName": "Phạm Minh Dũng", "courseId": 1, "status": "cancelled" }
]
```
