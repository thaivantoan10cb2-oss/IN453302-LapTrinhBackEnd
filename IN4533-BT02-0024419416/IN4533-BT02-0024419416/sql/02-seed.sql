-- sql/02-seed.sql : dữ liệu mẫu (5 thành viên, 4 khoá học, 7 phiếu đăng ký)
-- Chạy sau 01-schema.sql. Dùng email/tiêu đề để tra id nên không phụ thuộc giá trị id.

INSERT INTO members (full_name, email) VALUES
    ('Nguyễn Văn An',  'an.nguyen@example.com'),
    ('Trần Thị Bình',  'binh.tran@example.com'),
    ('Lê Minh Chi',    'chi.le@example.com'),
    ('Phạm Quốc Dũng', 'dung.pham@example.com'),
    ('Võ Hoàng Em',    'em.vo@example.com');      -- chưa đăng ký khoá nào

INSERT INTO courses (title, capacity) VALUES
    ('Lập trình Back End', 40),
    ('Cơ sở dữ liệu',      30),
    ('Lập trình Web',      25),
    ('Trí tuệ nhân tạo',   20);                   -- chưa có phiếu nào

INSERT INTO registrations (member_id, course_id, status)
SELECT m.id, c.id, v.status
FROM (VALUES
    ('an.nguyen@example.com',  'Lập trình Back End', 'confirmed'),
    ('binh.tran@example.com',  'Lập trình Back End', 'confirmed'),
    ('an.nguyen@example.com',  'Cơ sở dữ liệu',      'confirmed'),
    ('chi.le@example.com',     'Cơ sở dữ liệu',      'pending'),
    ('dung.pham@example.com',  'Lập trình Web',      'pending'),
    ('binh.tran@example.com',  'Lập trình Web',      'cancelled'),
    ('chi.le@example.com',     'Lập trình Back End', 'cancelled')
) AS v(email, title, status)
JOIN members m ON m.email = v.email
JOIN courses c ON c.title = v.title;
