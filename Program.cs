using DivershocksApi.Models;
using DivershocksApi.Services; // ¡Importante añadir esto arriba!
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

// CONTRATAMOS A LOS COCINEROS AQUÍ:
builder.Services.AddScoped<StockService>();

var app = builder.Build();

// ENDPOINT 1: Catálogo
app.MapGet("/api/productos", async (AppDbContext db) =>
{
    return Results.Ok(await db.Productos.ToListAsync());
});

// ENDPOINT 2: Stock (¡Mira qué limpio queda!)
app.MapGet("/api/stock", async (StockService stockService) =>
{
    var resultado = await stockService.CalcularStockTotal();
    return Results.Ok(resultado);
});

app.Run();