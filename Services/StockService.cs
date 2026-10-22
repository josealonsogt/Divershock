using DivershocksApi.Models;
using Microsoft.EntityFrameworkCore;
namespace DivershocksApi.Services;
public class StockService(AppDbContext db)
{
    public async Task<Dictionary<int, Stock>> Resumen() => await db.Movimientos.AsNoTracking()
        .GroupBy(m => m.VarianteId).Select(g => new { Id = g.Key,
            Almacen = g.Sum(m => m.Tipo == "EntradaAlmacen" || m.Tipo == "AjusteStock" ? (long)m.Cantidad : m.Tipo == "TrasladoTienda" ? -(long)m.Cantidad : 0),
            Tienda = g.Sum(m => m.Tipo == "TrasladoTienda" ? (long)m.Cantidad : m.Tipo == "Venta" ? -(long)m.Cantidad : 0)
        }).ToDictionaryAsync(x => x.Id, x => new Stock(x.Almacen, x.Tienda));
    public record Stock(long StockAlmacen, long StockTienda);
    public async Task<object> CalcularStockTotal()
    {
        var stock = await Resumen();
        var filas = await (from v in db.Variantes.AsNoTracking() join p in db.Productos on v.ProductoId equals p.Id select new { v.Id, p.Nombre, v.Talla }).ToListAsync();
        return filas.Select(v => new { VarianteId = v.Id, v.Nombre, v.Talla, stock.GetValueOrDefault(v.Id, new(0,0)).StockAlmacen, stock.GetValueOrDefault(v.Id, new(0,0)).StockTienda });
    }
}
