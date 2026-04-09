using Microsoft.Data.SqlClient;

// =============================================
// ИСПОЛНЯЕМЫЙ КОД (top-level statements)
// =============================================

var builder = WebApplication.CreateBuilder(args);

// Добавляем поддержку Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// ====================== ДИАГНОСТИЧЕСКИЕ ЭНДПОИНТЫ ======================

app.MapGet("/health", () =>
    Results.Ok(new { status = "ok", time = DateTime.UtcNow }));

app.MapGet("/version", (IConfiguration config) =>
{
    var appName = config["App:Name"] ?? "IsLabApp";
    var appVersion = config["App:Version"] ?? "1.0-lab4";

    return Results.Ok(new { name = appName, version = appVersion });
});

// ====================== ПРОВЕРКА ПОДКЛЮЧЕНИЯ К БД ======================

app.MapGet("/db/ping", async (IConfiguration config) =>
{
    var connectionString = config.GetConnectionString("Mssql");

    if (string.IsNullOrEmpty(connectionString))
        return Results.Ok(new { status = "error", message = "Connection string not configured" });

    try
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        return Results.Ok(new { status = "ok", message = "Successfully connected to MS SQL Server" });
    }
    catch (Exception ex)
    {
        return Results.Ok(new { status = "error", message = ex.Message });
    }
});

// ====================== CRUD ЗАМЕТОК ======================

var notes = new List<Note>();
var nextId = 1;

app.MapGet("/api/notes", () => notes);

app.MapGet("/api/notes/{id}", (int id) =>
{
    var note = notes.FirstOrDefault(n => n.Id == id);
    return note is not null ? Results.Ok(note) : Results.NotFound();
});

app.MapPost("/api/notes", (NoteCreate input) =>
{
    if (string.IsNullOrWhiteSpace(input.Title))
        return Results.BadRequest(new { error = "Title is required" });

    var note = new Note(
        Id: nextId++,
        Title: input.Title.Trim(),
        Text: input.Text?.Trim() ?? "",
        CreatedAt: DateTime.UtcNow
    );

    notes.Add(note);
    return Results.Created($"/api/notes/{note.Id}", note);
});

app.MapDelete("/api/notes/{id}", (int id) =>
{
    var index = notes.FindIndex(n => n.Id == id);
    if (index == -1)
        return Results.NotFound();

    notes.RemoveAt(index);
    return Results.NoContent();
});

// ====================== ЗАПУСК ПРИЛОЖЕНИЯ ======================
app.Run();

// =============================================
// ОБЪЯВЛЕНИЯ ТИПОВ — ТОЛЬКО В САМОМ КОНЦЕ ФАЙЛА!
// =============================================

record Note(int Id, string Title, string Text, DateTime CreatedAt);
record NoteCreate(string Title, string Text);