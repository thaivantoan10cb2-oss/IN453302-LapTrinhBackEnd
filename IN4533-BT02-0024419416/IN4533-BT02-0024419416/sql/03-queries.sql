-- sql/03-queries.sql : ba truy vấn yêu cầu của đề bài

-- (a) Họ tên thành viên và tên khoá học của các phiếu confirmed, sắp xếp theo họ tên.
SELECT m.full_name, c.title
FROM registrations r
JOIN members m ON m.id = r.member_id
JOIN courses  c ON c.id = r.course_id
WHERE r.status = 'confirmed'
ORDER BY m.full_name, c.title;

-- (b) Số phiếu đăng ký của từng khoá học, kể cả khoá chưa có phiếu nào.
SELECT c.title, COUNT(r.id) AS so_dang_ky
FROM courses c
LEFT JOIN registrations r ON r.course_id = c.id  -- LEFT JOIN (không dùng JOIN) để giữ khoá chưa có phiếu; COUNT(r.id) cho 0
GROUP BY c.id, c.title
ORDER BY c.id;

-- (c) Các thành viên chưa đăng ký khoá nào (không có bất kỳ phiếu nào, kể cả cancelled).
SELECT m.id, m.full_name, m.email
FROM members m
LEFT JOIN registrations r ON r.member_id = m.id
WHERE r.id IS NULL
ORDER BY m.id;
