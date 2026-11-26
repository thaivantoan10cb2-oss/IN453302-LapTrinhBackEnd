-- sql/02-seed.sql
-- Dữ liệu mẫu: 5 thành viên, 4 khoá học, 7 phiếu đăng ký (đủ 3 trạng thái)
-- Chạy sau 01-schema.sql:  psql -U postgres -d eduhub -f sql/02-seed.sql

INSERT INTO members (full_name, email) VALUES
  ('Nguyễn Văn An',   'an.nguyen@example.com'),    -- id 1
  ('Trần Thị Bình',   'binh.tran@example.com'),    -- id 2
  ('Lê Hoàng Cường',  'cuong.le@example.com'),     -- id 3
  ('Phạm Minh Dũng',  'dung.pham@example.com'),    -- id 4
  ('Võ Thị Em',       'em.vo@example.com');        -- id 5 (chưa đăng ký khoá nào)

INSERT INTO courses (title, capacity) VALUES
  ('Lập trình Back End với ASP.NET Core', 30),     -- id 1
  ('Cơ sở dữ liệu PostgreSQL',            25),     -- id 2
  ('Thiết kế RESTful API',                20),     -- id 3
  ('Docker cơ bản',                       15);     -- id 4 (chưa có phiếu nào, để thấy tác dụng của LEFT JOIN)

-- Cột status bỏ trống thì nhận giá trị mặc định 'pending'
INSERT INTO registrations (member_id, course_id, status) VALUES
  (1, 1, 'confirmed'),
  (1, 2, 'pending'),
  (2, 1, 'confirmed'),
  (2, 3, 'cancelled'),
  (3, 2, 'confirmed'),
  (3, 3, 'pending'),
  (4, 1, 'cancelled');
