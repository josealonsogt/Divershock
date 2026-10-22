using DivershocksApi.Models;
using DivershocksApi.Services;
using DivershocksApi.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
var builder = WebApplication.CreateBuilder(args);
var cs = new SqliteConnectionStringBuilder(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=diverShocks.db");
if (!Path.IsPathRooted(cs.DataSource)) cs.DataSource = Path.Combine(builder.Environment.ContentRootPath, cs.DataSource);
cs.ForeignKeys = true;
DatabaseSetup.Initialize(cs.ToString());
builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlite(cs.ToString()));
builder.Services.AddScoped<StockService>(); builder.Services.AddScoped<ProductoService>(); builder.Services.AddScoped<MovimientoService>();
builder.Services.AddHttpClient<CatalogImportService>();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.WithOrigins("http://localhost:5173", "http://127.0.0.1:5173").AllowAnyMethod().AllowAnyHeader()));
var app = builder.Build();
app.Use(async (context, next) => {
    try { await next(context); }
    catch (Exception ex) when (ex is ArgumentException or KeyNotFoundException or DbUpdateException or SqliteException)
    {
        var status = ex is KeyNotFoundException ? 404 : ex is DbUpdateException or SqliteException ? 409 : 400;
        var message = status == 409 ? "No se pudo guardar: datos duplicados o base de datos ocupada. Actualiza y vuelve a intentarlo." : ex.Message;
        await Results.Problem(message, statusCode: status, title: "No se pudo completar la operación").ExecuteAsync(context);
    }
});
if (app.Environment.IsDevelopment()) app.UseCors();
app.UseDefaultFiles(); app.UseStaticFiles();
app.MapPost("/admin/importar-catalogo", async (string? clave, IConfiguration configuration, CatalogImportService importer) =>
{
    var expected = configuration["CatalogImportKey"];
    if (string.IsNullOrWhiteSpace(expected) || clave != expected) return Results.Unauthorized();
    try { return Results.Ok(await importer.Importar()); }
    catch (HttpRequestException ex) { return Results.Problem("No se pudo descargar el catálogo: " + ex.Message, statusCode: 502); }
});
// Keep the document's routes and the original /api aliases.
foreach (var prefix in new[] { "", "/api" })
{
    var routes = app.MapGroup(prefix);
    routes.MapGet("/productos", async (ProductoService s) => await s.ObtenerTodosConStock());
    routes.MapPost("/productos", async (ProductoRequestDto d, ProductoService s) => { await s.Guardar(null,d); return Results.Ok("OK"); });
    routes.MapPut("/productos/{id:int}", async (int id, ProductoRequestDto d, ProductoService s) => { await s.Guardar(id,d); return Results.Ok("OK"); });
    routes.MapDelete("/productos/{id:int}", async (int id, ProductoService s) => { await s.Borrar(id); return Results.Ok("OK"); });
    routes.MapPost("/productos/{id:int}/variantes", async (int id, string talla, ProductoService s) => Results.Ok(await s.AnadirTalla(id,talla)));
    routes.MapGet("/stock", async (StockService s) => await s.CalcularStockTotal());
    routes.MapPost("/movimientos", async (MovimientoRequestDto d, MovimientoService s) => { await s.Registrar(d); return Results.Ok("OK"); });
    routes.MapGet("/movimientos/{id:int}", async (int id, MovimientoService s) => await s.ObtenerHistorial(id));
    routes.MapDelete("/movimientos/{id:int}", async (int id, MovimientoService s) => { await s.Borrar(id); return Results.Ok("OK"); });
    routes.MapDelete("/movimientos/variante/{varianteId:int}", async (int varianteId, MovimientoService s) => { await s.BorrarHistorial(varianteId); return Results.Ok("OK"); });
    routes.MapDelete("/movimientos", async (MovimientoService s) => { await s.BorrarTodoElHistorial(); return Results.Ok("OK"); });
    routes.MapGet("/movimientos", async (int? limite, MovimientoService s) => await s.ObtenerUltimos(limite ?? 12));
}
app.MapFallbackToFile("index.html");
app.Run();
