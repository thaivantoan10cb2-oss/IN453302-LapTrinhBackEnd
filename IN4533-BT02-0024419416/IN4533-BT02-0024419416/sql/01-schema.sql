-- sql/01-schema.sql : tạo 3 bảng cho EduHub mini (PostgreSQL 14+)
-- Chạy trên CSDL rỗng; có thể chạy lại nhiều lần nhờ DROP ... IF EXISTS.

DROP TABLE IF EXISTS registrations;
DROP TABLE IF EXISTS courses;
DROP TABLE IF EXISTS members;

CREATE TABLE members (
    id        INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    full_name VARCHAR(100) NOT NULL,
    email     VARCHAR(150) NOT NULL UNIQUE
);

CREATE TABLE courses (
    id       INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    title    VARCHAR(150) NOT NULL,
    capacity INT NOT NULL CHECK (capacity > 0)
);

CREATE TABLE registrations (
    id            INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    member_id     INT NOT NULL REFERENCES members(id),
    course_id     INT NOT NULL REFERENCES courses(id),
    status        VARCHAR(20) NOT NULL DEFAULT 'pending',
    registered_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    CONSTRAINT ck_registrations_status
        CHECK (status IN ('pending', 'confirmed', 'cancelled')),
    CONSTRAINT uq_registrations_member_course
        UNIQUE (member_id, course_id)
);
