using DivershocksApi.Models;
using DivershocksApi.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
namespace DivershocksApi.Services;
public class MovimientoService(AppDbContext db, StockService stocks)
{
    public async Task Registrar(MovimientoRequestDto dto)
    {
        if (dto.Tipo is not ("EntradaAlmacen" or "TrasladoTienda" or "Venta" or "AjusteStock")) throw new ArgumentException("Tipo de movimiento no válido.");
        if (dto.Cantidad == 0 || (dto.Cantidad < 0 && dto.Tipo != "AjusteStock")) throw new ArgumentException("La cantidad debe ser mayor que cero; solo un ajuste admite cantidades negativas.");
        await db.Database.OpenConnectionAsync();
        // BEGIN IMMEDIATE reserves SQLite's writer before reading stock, across processes too.
        using var transaction = ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: false);
        await db.Database.UseTransactionAsync(transaction);
        if (!await db.Variantes.AnyAsync(v => v.Id == dto.VarianteId)) throw new KeyNotFoundException("Talla no encontrada.");
        var stock = (await stocks.Resumen()).GetValueOrDefault(dto.VarianteId, new(0,0));
        var disponible = dto.Tipo == "Venta" ? stock.StockTienda : stock.StockAlmacen;
        if ((dto.Tipo is "Venta" or "TrasladoTienda" && dto.Cantidad > disponible) || (dto.Tipo == "AjusteStock" && stock.StockAlmacen + dto.Cantidad < 0)) throw new ArgumentException($"Solo hay {disponible} pares disponibles. No se puede dejar stock negativo.");
        db.Movimientos.Add(new Movimiento { VarianteId = dto.VarianteId, Tipo = dto.Tipo, Cantidad = dto.Cantidad, Fecha = DateTimeOffset.UtcNow.ToString("O") });
        await db.SaveChangesAsync(); await transaction.CommitAsync();
    }
    public async Task<List<Movimiento>> ObtenerHistorial(int id)
    {
        if (!await db.Variantes.AnyAsync(v => v.Id == id)) throw new KeyNotFoundException("Talla no encontrada.");
        return await db.Movimientos.AsNoTracking().Where(m => m.VarianteId == id).OrderByDescending(m => m.Id).ToListAsync();
    }

    public async Task<object> ObtenerUltimos(int limite = 12)
    {
        limite = Math.Clamp(limite, 1, 50);
        return await (from m in db.Movimientos.AsNoTracking()
                      join v in db.Variantes.AsNoTracking() on m.VarianteId equals v.Id
                      join p in db.Productos.AsNoTracking() on v.ProductoId equals p.Id
                      orderby m.Id descending
                      select new { m.Id, m.VarianteId, Producto = p.Nombre, p.Referencia, Talla = v.Talla, m.Tipo, m.Cantidad, m.Fecha })
            .Take(limite).ToListAsync();
    }

    public async Task Borrar(int id)
    {
        var movimiento = await db.Movimientos.FindAsync(id) ?? throw new KeyNotFoundException("Movimiento no encontrado.");
        db.Movimientos.Remove(movimiento);
        await db.SaveChangesAsync();
    }

    public async Task BorrarHistorial(int varianteId)
    {
        if (!await db.Variantes.AnyAsync(v => v.Id == varianteId)) throw new KeyNotFoundException("Talla no encontrada.");
        var movimientos = await db.Movimientos.Where(m => m.VarianteId == varianteId).ToListAsync();
        db.Movimientos.RemoveRange(movimientos);
        await db.SaveChangesAsync();
    }

    public async Task BorrarTodoElHistorial()
    {
        var movimientos = await db.Movimientos.ToListAsync();
        db.Movimientos.RemoveRange(movimientos);
        await db.SaveChangesAsync();
    }
}
