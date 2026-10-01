using DivershocksApi.Models;
using Microsoft.EntityFrameworkCore;

namespace DivershocksApi.Services;

public class StockService
{
    private readonly AppDbContext _db;

    // Recibimos el contexto que permite consultar la base de datos.
    public StockService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<object>> CalcularStockTotal()
    {
        // Leemos los datos guardados en las tres tablas.
        var productos = await _db.Productos.ToListAsync();
        var variantes = await _db.Variantes.ToListAsync();
        var movimientos = await _db.Movimientos.ToListAsync();

        var resumen = new List<object>();

        // Calculamos el stock de cada talla de cada producto.
        foreach (var variante in variantes)
        {
            var producto = productos.First(
                producto => producto.Id == variante.ProductoId
            );

            int stockAlmacen = 0;
            int stockTienda = 0;

            foreach (var movimiento in movimientos)
            {
                // Saltamos los movimientos que pertenecen a otra variante.
                if (movimiento.VarianteId != variante.Id)
                {
                    continue;
                }

                switch (movimiento.Tipo)
                {
                    case "EntradaAlmacen":
                        stockAlmacen += movimiento.Cantidad;
                        break;

                    case "TrasladoTienda":
                        // Sale del almacén y entra en la tienda.
                        stockAlmacen -= movimiento.Cantidad;
                        stockTienda += movimiento.Cantidad;
                        break;

                    case "Venta":
                        stockTienda -= movimiento.Cantidad;
                        break;

                    case "AjusteStock":
                        // Positivo añade pares; negativo descuenta pares.
                        stockAlmacen += movimiento.Cantidad;
                        break;
                }
            }

            // Añadimos el resultado de esta variante a la respuesta.
            resumen.Add(new
            {
                Nombre = producto.Nombre,
                Talla = variante.Talla,
                StockAlmacen = stockAlmacen,
                StockTienda = stockTienda
            });
        }

        return resumen;
    }
}