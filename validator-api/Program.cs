using System.Text.RegularExpressions;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapPost("/api/ValidateEmail", async (HttpRequest req) =>
{
    var input = await JsonSerializer.DeserializeAsync<Input>(req.Body) ?? new Input(null);
    bool ok = !string.IsNullOrWhiteSpace(input.value) &&
              Regex.IsMatch(input.value!, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    return Results.Json(new ValidationResult(ok, ok ? "Email is valid" : "Invalid email format"));
});

app.MapPost("/api/ValidatePhoneNumber", async (HttpRequest req) =>
{
    var input = await JsonSerializer.DeserializeAsync<Input>(req.Body) ?? new Input(null);
    bool ok = !string.IsNullOrWhiteSpace(input.value) &&
              Regex.IsMatch(input.value!, @"^(\(\d{3}\)\s?\d{3}-\d{4}|\d{3}-\d{3}-\d{4})$");
    return Results.Json(new ValidationResult(ok, ok ? "Phone number is valid" : "Invalid phone number format"));
});

app.Run();

record Input(string? value);
record ValidationResult(bool IsValid, string Message);
