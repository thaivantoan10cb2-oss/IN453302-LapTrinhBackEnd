using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Chuỗi kết nối lấy từ biến môi trường ConnectionStrings__EduHub (KHÔNG ghi mật khẩu vào mã nguồn).
var connStr = builder.Configuration.GetConnectionString("EduHub")
    ?? throw new InvalidOperationException(
        "Thiếu chuỗi kết nối. Đặt biến môi trường ConnectionStrings__EduHub.");
builder.Services.AddSingleton(NpgsqlDataSource.Create(connStr));

var app = builder.Build();

const string ProblemBase = "https://eduhub.example.com/problems/";
string[] validStatuses = { "pending", "confirmed", "cancelled" };

IResult Problem(int status, string slug, string title, string detail, string instance) =>
    Results.Problem(type: ProblemBase + slug, title: title, statusCode: status,
                    detail: detail, instance: instance);

// ---------- 1. GET danh sách đăng ký của một khoá học ----------
app.MapGet("/api/v1/courses/{courseId:int}/registrations",
    async (int courseId, NpgsqlDataSource db, HttpRequest req) =>
{
    await using var conn = await db.OpenConnectionAsync();

    if (!await Exists(conn, "SELECT 1 FROM courses WHERE id = @id", courseId))
        return Problem(404, "course-not-found", "Course not found",
                       $"Course {courseId} does not exist.", req.Path);

    await using var cmd = new NpgsqlCommand(
        "SELECT id, member_id, course_id, status FROM registrations " +
        "WHERE course_id = @courseId ORDER BY id", conn);
    cmd.Parameters.AddWithValue("courseId", courseId);

    var list = new List<RegistrationDto>();
    await using var rd = await cmd.ExecuteReaderAsync();
    while (await rd.ReadAsync())
        list.Add(new RegistrationDto(rd.GetInt32(0), rd.GetInt32(1), rd.GetInt32(2), rd.GetString(3)));

    return Results.Ok(list);
});

// ---------- 2. POST tạo đăng ký ----------
app.MapPost("/api/v1/courses/{courseId:int}/registrations",
    async (int courseId, CreateRegistrationRequest body, NpgsqlDataSource db, HttpRequest req) =>
{
    if (body.MemberId is null or <= 0)
        return Problem(400, "invalid-request", "Invalid request",
                       "memberId is required and must be a positive integer.", req.Path);
    int memberId = body.MemberId.Value;

    await using var conn = await db.OpenConnectionAsync();

    if (!await Exists(conn, "SELECT 1 FROM courses WHERE id = @id", courseId))
        return Problem(404, "course-not-found", "Course not found",
                       $"Course {courseId} does not exist.", req.Path);
    if (!await Exists(conn, "SELECT 1 FROM members WHERE id = @id", memberId))
        return Problem(404, "member-not-found", "Member not found",
                       $"Member {memberId} does not exist.", req.Path);

    // Khoá đã đủ chỗ? (không tính phiếu đã huỷ)
    await using (var cap = new NpgsqlCommand(
        "SELECT c.capacity <= (SELECT COUNT(*) FROM registrations r " +
        "WHERE r.course_id = c.id AND r.status <> 'cancelled') FROM courses c WHERE c.id = @id", conn))
    {
        cap.Parameters.AddWithValue("id", courseId);
        if ((bool)(await cap.ExecuteScalarAsync())!)
            return Problem(409, "course-full", "Course is full",
                           $"Course {courseId} has no remaining seats.", req.Path);
    }

    try
    {
        await using var cmd = new NpgsqlCommand(
            "INSERT INTO registrations (member_id, course_id) VALUES (@m, @c) " +
            "RETURNING id, member_id, course_id, status", conn);
        cmd.Parameters.AddWithValue("m", memberId);
        cmd.Parameters.AddWithValue("c", courseId);

        await using var rd = await cmd.ExecuteReaderAsync();
        await rd.ReadAsync();
        var dto = new RegistrationDto(rd.GetInt32(0), rd.GetInt32(1), rd.GetInt32(2), rd.GetString(3));
        return Results.Created($"/api/v1/registrations/{dto.Id}", dto);
    }
    catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
    {
        return Problem(409, "duplicate-registration", "Duplicate registration",
                       $"Member {memberId} has already registered for course {courseId}.", req.Path);
    }
});

// ---------- 3 + 4. PATCH xác nhận / huỷ đăng ký ----------
app.MapPatch("/api/v1/registrations/{id:int}",
    async (int id, UpdateStatusRequest body, NpgsqlDataSource db, HttpRequest req) =>
{
    if (body.Status is null || !validStatuses.Contains(body.Status) || body.Status == "pending")
        return Problem(400, "invalid-status", "Invalid status",
                       "status must be 'confirmed' or 'cancelled'.", req.Path);

    await using var conn = await db.OpenConnectionAsync();

    string? current;
    await using (var get = new NpgsqlCommand("SELECT status FROM registrations WHERE id = @id", conn))
    {
        get.Parameters.AddWithValue("id", id);
        current = (string?)await get.ExecuteScalarAsync();
    }
    if (current is null)
        return Problem(404, "registration-not-found", "Registration not found",
                       $"Registration {id} does not exist.", req.Path);

    // pending -> confirmed | cancelled ; confirmed -> cancelled ; cancelled là trạng thái cuối
    bool allowed = (current, body.Status) switch
    {
        ("pending", "confirmed") => true,
        ("pending", "cancelled") => true,
        ("confirmed", "cancelled") => true,
        _ => false
    };
    if (!allowed)
        return Problem(409, "invalid-transition", "Invalid status transition",
                       $"Cannot change status from '{current}' to '{body.Status}'.", req.Path);

    await using var upd = new NpgsqlCommand(
        "UPDATE registrations SET status = @s WHERE id = @id " +
        "RETURNING id, member_id, course_id, status", conn);
    upd.Parameters.AddWithValue("s", body.Status);
    upd.Parameters.AddWithValue("id", id);
    await using var rd = await upd.ExecuteReaderAsync();
    await rd.ReadAsync();
    return Results.Ok(new RegistrationDto(rd.GetInt32(0), rd.GetInt32(1), rd.GetInt32(2), rd.GetString(3)));
});

// ---------- (phụ) GET một phiếu — dùng cho header Location ----------
app.MapGet("/api/v1/registrations/{id:int}",
    async (int id, NpgsqlDataSource db, HttpRequest req) =>
{
    await using var conn = await db.OpenConnectionAsync();
    await using var cmd = new NpgsqlCommand(
        "SELECT id, member_id, course_id, status FROM registrations WHERE id = @id", conn);
    cmd.Parameters.AddWithValue("id", id);
    await using var rd = await cmd.ExecuteReaderAsync();
    if (!await rd.ReadAsync())
        return Problem(404, "registration-not-found", "Registration not found",
                       $"Registration {id} does not exist.", req.Path);
    return Results.Ok(new RegistrationDto(rd.GetInt32(0), rd.GetInt32(1), rd.GetInt32(2), rd.GetString(3)));
});

app.Run();

static async Task<bool> Exists(NpgsqlConnection conn, string sql, int id)
{
    await using var cmd = new NpgsqlCommand(sql, conn);
    cmd.Parameters.AddWithValue("id", id);
    return await cmd.ExecuteScalarAsync() is not null;
}

record RegistrationDto(int Id, int MemberId, int CourseId, string Status);
record CreateRegistrationRequest(int? MemberId);
record UpdateStatusRequest(string? Status);
