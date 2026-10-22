using DivershocksApi.Models;
using DivershocksApi.DTOs;
using Microsoft.EntityFrameworkCore;
namespace DivershocksApi.Services;
public class ProductoService(AppDbContext db, StockService stocks)
{
    public async Task<object> ObtenerTodosConStock()
    {
        var productos = await db.Productos.AsNoTracking().Where(p => p.Activo).OrderBy(p => p.Nombre).ToListAsync();
        var variantes = (await db.Variantes.AsNoTracking().ToListAsync()).ToLookup(v => v.ProductoId);
        var stock = await stocks.Resumen();
        return productos.Select(p => new { p.Id, p.Referencia, p.Nombre, p.Categoria, p.Foto, p.Precio,
            Tallas = variantes[p.Id].Select(v => new { VarianteId = v.Id, v.Talla, stock.GetValueOrDefault(v.Id, new(0,0)).StockAlmacen, stock.GetValueOrDefault(v.Id, new(0,0)).StockTienda }) });
    }
    public async Task Guardar(int? id, ProductoRequestDto dto)
    {
        var referencia = (dto.Referencia ?? "").Trim();
        var nombre = (dto.Nombre ?? "").Trim();
        if (referencia.Length == 0 || nombre.Length == 0) throw new ArgumentException("Referencia y nombre son obligatorios.");
        if (!double.IsFinite(dto.Precio) || dto.Precio < 0) throw new ArgumentException("El precio no puede ser negativo.");
        if (await db.Productos.AnyAsync(p => p.Id != id && p.Referencia.ToUpper() == referencia.ToUpper())) throw new ArgumentException("Ya existe un producto con esa referencia.");
        var tallas = (dto.Tallas ?? []).Select(t => (t ?? "").Trim()).ToList();
        if (tallas.Any(t => t.Length == 0) || tallas.Distinct(StringComparer.OrdinalIgnoreCase).Count() != tallas.Count) throw new ArgumentException("Las tallas no pueden estar vacías ni repetidas.");
        var producto = id.HasValue ? await db.Productos.FindAsync(id.Value) ?? throw new KeyNotFoundException("Producto no encontrado.") : new Producto();
        producto.Referencia = referencia; producto.Nombre = nombre; producto.Categoria = (dto.Categoria ?? "").Trim(); producto.Foto = (dto.Foto ?? "").Trim(); producto.Precio = Math.Round(dto.Precio, 2);
        if (producto.Foto.Length > 0 && (!Uri.TryCreate(producto.Foto, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))) throw new ArgumentException("La foto debe ser una URL http o https.");
        await using var transaction = await db.Database.BeginTransactionAsync();
        if (!id.HasValue) db.Productos.Add(producto);
        await db.SaveChangesAsync();
        if (!id.HasValue) { db.Variantes.AddRange(tallas.Select(t => new Variante { ProductoId = producto.Id, Talla = t })); await db.SaveChangesAsync(); }
        await transaction.CommitAsync();
    }
    public async Task<Variante> AnadirTalla(int id, string talla)
    {
        talla = (talla ?? "").Trim();
        if (talla.Length == 0) throw new ArgumentException("Escribe una talla.");
        if (!await db.Productos.AnyAsync(p => p.Id == id)) throw new KeyNotFoundException("Producto no encontrado.");
        if (await db.Variantes.AnyAsync(v => v.ProductoId == id && v.Talla.ToUpper() == talla.ToUpper())) throw new ArgumentException("Esta talla ya existe.");
        var variante = new Variante { ProductoId = id, Talla = talla }; db.Variantes.Add(variante); await db.SaveChangesAsync(); return variante;
    }

    public async Task Borrar(int id)
    {
        var producto = await db.Productos.FindAsync(id) ?? throw new KeyNotFoundException("Producto no encontrado.");
        producto.Activo = false;
        await db.SaveChangesAsync();
    }
}
