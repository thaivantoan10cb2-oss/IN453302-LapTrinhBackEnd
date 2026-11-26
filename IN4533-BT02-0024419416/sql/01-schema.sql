-- sql/01-schema.sql
-- EduHub mini: tạo 3 bảng members, courses, registrations (PostgreSQL 13+)
-- Chạy:  psql -U postgres -d eduhub -f sql/01-schema.sql

-- Cho phép chạy lại nhiều lần từ đầu
DROP TABLE IF EXISTS registrations;
DROP TABLE IF EXISTS courses;
DROP TABLE IF EXISTS members;

-- Thành viên
CREATE TABLE members (
  id        INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  full_name VARCHAR(100) NOT NULL,
  email     VARCHAR(150) NOT NULL UNIQUE
);

-- Khoá học
CREATE TABLE courses (
  id       INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  title    VARCHAR(150) NOT NULL,
  capacity INT NOT NULL CHECK (capacity > 0)
);

-- Phiếu đăng ký
CREATE TABLE registrations (
  id        INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  member_id INT NOT NULL REFERENCES members(id),
  course_id INT NOT NULL REFERENCES courses(id),
  status    VARCHAR(20) NOT NULL DEFAULT 'pending',
  CONSTRAINT ck_registrations_status
    CHECK (status IN ('pending', 'confirmed', 'cancelled')),
  -- Mỗi thành viên chỉ đăng ký một khoá học một lần
  CONSTRAINT uq_registrations_member_course UNIQUE (member_id, course_id)
);
