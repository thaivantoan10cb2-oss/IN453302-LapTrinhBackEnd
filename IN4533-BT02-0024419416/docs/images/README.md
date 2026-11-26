# Ảnh chụp màn hình cần nộp

Đề bài yêu cầu ảnh chụp thật từ máy của bạn, nên cần tự chụp rồi đặt vào thư mục này:

| Tên tệp gợi ý | Nội dung | Mục đề bài |
|---------------|----------|-----------|
| `01-ket-noi-postgres.png` | `psql` (hoặc pgAdmin/DBeaver) kết nối thành công, ví dụ kết quả `SELECT version();` | 1.1 |
| `02-ket-qua-truy-van.png` | Kết quả ba truy vấn (a), (b), (c) khi chạy `sql/03-queries.sql` | 1.4 (tuỳ chọn) |
| `03-get-registrations.png` | Kết quả `curl -i http://localhost:5000/api/v1/courses/1/registrations`, thấy `200 OK` và JSON | 3.2 |
| `04-post-registration.png` | Kết quả lệnh POST tạo đăng ký, thấy `201 Created` và header `Location` | 3.2 |
| `05-post-conflict.png` | Kết quả POST trùng đăng ký, thấy `409` và `application/problem+json` | 3.2 (tuỳ chọn) |

Xoá tệp này trước khi nộp nếu muốn.
