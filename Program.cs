using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// ====================== ДАННЫЕ ======================
var notes = new List<Note>();
var nextId = 1;

// ====================== ДИАГНОСТИЧЕСКИЕ ЭНДПОИНТЫ ======================
app.MapGet("/health", () => Results.Ok(new { status = "ok", time = DateTime.UtcNow }));

app.MapGet("/version", (IConfiguration config) =>
{
    var name = config["App:Name"] ?? "IsLabApp";
    var version = config["App:Version"] ?? "1.0-lab4";
    return Results.Ok(new { name, version });
});

app.MapGet("/db/ping", async (IConfiguration config) =>
{
    var connectionString = config.GetConnectionString("Mssql");

    if (string.IsNullOrEmpty(connectionString))
    {
        return Results.Ok(new
        {
            status = "error",
            message = "Connection string is not configured"
        });
    }

    try
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        return Results.Ok(new
        {
            status = "ok",
            message = "Successfully connected to MS SQL Server",
            serverVersion = connection.ServerVersion
        });
    }
    catch (Exception ex)
    {
        return Results.Ok(new
        {
            status = "error",
            message = ex.Message,
            hint = "Убедитесь, что SQL Server запущен и строка подключения верная"
        });
    }
});

// ====================== CRUD ЗАМЕТОК ======================
app.MapGet("/api/notes", () => notes);

app.MapGet("/api/notes/{id}", (int id) =>
    notes.FirstOrDefault(n => n.Id == id) is Note n ? Results.Ok(n) : Results.NotFound());

app.MapPost("/api/notes", (NoteCreate input) =>
{
    if (string.IsNullOrWhiteSpace(input.Title))
        return Results.BadRequest(new { error = "Title is required" });

    var note = new Note(nextId++, input.Title.Trim(), input.Text?.Trim() ?? "", DateTime.UtcNow);
    notes.Add(note);
    return Results.Created($"/api/notes/{note.Id}", note);
});

app.MapDelete("/api/notes/{id}", (int id) =>
{
    var index = notes.FindIndex(n => n.Id == id);
    if (index == -1) return Results.NotFound();
    notes.RemoveAt(index);
    return Results.NoContent();
});

// ====================== ЗАПУСК ======================
app.Run();

// ====================== МОДЕЛИ — ТОЛЬКО В КОНЦЕ ======================
record Note(int Id, string Title, string Text, DateTime CreatedAt);
record NoteCreate(string Title, string? Text);