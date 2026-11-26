using System.Text.Json.Serialization;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Chuỗi kết nối lấy từ cấu hình (appsettings.json hoặc biến môi trường ConnectionStrings__EduHub),
// không ghi mật khẩu thật vào mã nguồn.
var connectionString = builder.Configuration.GetConnectionString("EduHub")
    ?? throw new InvalidOperationException("Thiếu cấu hình ConnectionStrings:EduHub");
builder.Services.AddSingleton(NpgsqlDataSource.Create(connectionString));

// Không xuất các trường null (ví dụ memberName) trong JSON trả về
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull);

var app = builder.Build();

// Tạo response lỗi theo Problem Details (RFC 9457), Content-Type: application/problem+json
static IResult Problem(HttpContext ctx, int status, string slug, string title, string detail) =>
    Results.Problem(
        type: $"https://eduhub.example.com/problems/{slug}",
        title: title,
        statusCode: status,
        detail: detail,
        instance: ctx.Request.Path);

// 1. GET /api/v1/courses/{courseId}/registrations: xem danh sách đăng ký của một khoá học
app.MapGet("/api/v1/courses/{courseId:int}/registrations",
    async (int courseId, HttpContext ctx, NpgsqlDataSource db) =>
{
    await using var conn = await db.OpenConnectionAsync();

    await using (var check = new NpgsqlCommand("SELECT 1 FROM courses WHERE id = @courseId", conn))
    {
        check.Parameters.AddWithValue("courseId", courseId);
        if (await check.ExecuteScalarAsync() is null)
            return Problem(ctx, 404, "course-not-found", "Course not found",
                $"Không tìm thấy khoá học có id {courseId}.");
    }

    var list = new List<RegistrationDto>();
    await using var cmd = new NpgsqlCommand(@"
        SELECT r.id, r.member_id, m.full_name, r.course_id, r.status
        FROM registrations r
        JOIN members m ON m.id = r.member_id
        WHERE r.course_id = @courseId
        ORDER BY r.id", conn);
    cmd.Parameters.AddWithValue("courseId", courseId);

    await using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        list.Add(new RegistrationDto(
            reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2),
            reader.GetInt32(3), reader.GetString(4)));
    }
    return Results.Ok(list);
});

// 2. POST /api/v1/courses/{courseId}/registrations: tạo đăng ký
app.MapPost("/api/v1/courses/{courseId:int}/registrations",
    async (int courseId, CreateRegistrationRequest? body, HttpContext ctx, NpgsqlDataSource db) =>
{
    if (body is null || body.MemberId <= 0)
        return Problem(ctx, 400, "invalid-request", "Invalid request",
            "memberId là bắt buộc và phải là số nguyên dương.");

    await using var conn = await db.OpenConnectionAsync();

    // Số chỗ còn lại của khoá học (NULL nếu khoá không tồn tại)
    await using (var cap = new NpgsqlCommand(@"
        SELECT c.capacity - (SELECT COUNT(*) FROM registrations
                             WHERE course_id = c.id AND status <> 'cancelled')
        FROM courses c WHERE c.id = @courseId", conn))
    {
        cap.Parameters.AddWithValue("courseId", courseId);
        var remaining = await cap.ExecuteScalarAsync();
        if (remaining is null)
            return Problem(ctx, 404, "course-not-found", "Course not found",
                $"Không tìm thấy khoá học có id {courseId}.");
        if (Convert.ToInt64(remaining) <= 0)
            return Problem(ctx, 409, "course-full", "Course is full",
                $"Khoá học {courseId} đã đủ chỗ.");
    }

    await using (var mem = new NpgsqlCommand("SELECT 1 FROM members WHERE id = @memberId", conn))
    {
        mem.Parameters.AddWithValue("memberId", body.MemberId);
        if (await mem.ExecuteScalarAsync() is null)
            return Problem(ctx, 404, "member-not-found", "Member not found",
                $"Không tìm thấy thành viên có id {body.MemberId}.");
    }

    try
    {
        await using var ins = new NpgsqlCommand(@"
            INSERT INTO registrations (member_id, course_id)
            VALUES (@memberId, @courseId)
            RETURNING id, status", conn);
        ins.Parameters.AddWithValue("memberId", body.MemberId);
        ins.Parameters.AddWithValue("courseId", courseId);

        await using var reader = await ins.ExecuteReaderAsync();
        await reader.ReadAsync();
        var dto = new RegistrationDto(reader.GetInt32(0), body.MemberId, null, courseId, reader.GetString(1));
        return Results.Created($"/api/v1/registrations/{dto.Id}", dto);
    }
    catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
    {
        // Vi phạm UNIQUE (member_id, course_id)
        return Problem(ctx, 409, "duplicate-registration", "Duplicate registration",
            $"Thành viên {body.MemberId} đã đăng ký khoá học {courseId}, mỗi thành viên chỉ được đăng ký một khoá học một lần.");
    }
});

// 3. PATCH /api/v1/registrations/{id}: xác nhận đăng ký (status = confirmed)
app.MapPatch("/api/v1/registrations/{id:int}",
    async (int id, UpdateStatusRequest? body, HttpContext ctx, NpgsqlDataSource db) =>
{
    if (body is null || body.Status != "confirmed")
        return Problem(ctx, 400, "invalid-request", "Invalid request",
            "status chỉ nhận giá trị \"confirmed\". Để huỷ đăng ký hãy dùng DELETE.");

    await using var conn = await db.OpenConnectionAsync();

    await using var cmd = new NpgsqlCommand(@"
        UPDATE registrations SET status = 'confirmed'
        WHERE id = @id AND status <> 'cancelled'
        RETURNING id, member_id, course_id, status", conn);
    cmd.Parameters.AddWithValue("id", id);

    await using (var reader = await cmd.ExecuteReaderAsync())
    {
        if (await reader.ReadAsync())
            return Results.Ok(new RegistrationDto(
                reader.GetInt32(0), reader.GetInt32(1), null, reader.GetInt32(2), reader.GetString(3)));
    }

    // Không cập nhật được: hoặc phiếu không tồn tại, hoặc phiếu đã bị huỷ
    await using var exists = new NpgsqlCommand("SELECT 1 FROM registrations WHERE id = @id", conn);
    exists.Parameters.AddWithValue("id", id);
    if (await exists.ExecuteScalarAsync() is null)
        return Problem(ctx, 404, "registration-not-found", "Registration not found",
            $"Không tìm thấy phiếu đăng ký có id {id}.");

    return Problem(ctx, 409, "registration-cancelled", "Registration already cancelled",
        $"Phiếu đăng ký {id} đã bị huỷ nên không thể xác nhận.");
});

// 4. DELETE /api/v1/registrations/{id}: huỷ đăng ký (huỷ mềm, giữ bản ghi với status = cancelled)
app.MapDelete("/api/v1/registrations/{id:int}",
    async (int id, HttpContext ctx, NpgsqlDataSource db) =>
{
    await using var conn = await db.OpenConnectionAsync();
    await using var cmd = new NpgsqlCommand(
        "UPDATE registrations SET status = 'cancelled' WHERE id = @id", conn);
    cmd.Parameters.AddWithValue("id", id);

    var affected = await cmd.ExecuteNonQueryAsync();
    return affected == 0
        ? Problem(ctx, 404, "registration-not-found", "Registration not found",
            $"Không tìm thấy phiếu đăng ký có id {id}.")
        : Results.NoContent();
});

app.Run();

// Các kiểu dữ liệu (record) dùng cho request và response
record CreateRegistrationRequest(int MemberId);
record UpdateStatusRequest(string? Status);
record RegistrationDto(int Id, int MemberId, string? MemberName, int CourseId, string Status);
