using Microsoft.EntityFrameworkCore;
using TaskManager.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Enum se u JSON-u šalje i vraća kao tekst ("ToDo"), a ne kao broj (0)
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Registracija baze: EF Core koristi SQLite, a connection string se čita iz appsettings.json
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseHttpsRedirection();

// Poslužuje statičke fajlove iz foldera wwwroot (HTML stranica)
app.UseDefaultFiles(); 
app.UseStaticFiles();  

app.UseAuthorization();

app.UseAuthorization();

app.MapControllers();

app.Run();

// Potrebno da testni projekat može pokrenuti aplikaciju (WebApplicationFactory<Program>)
public partial class Program { }