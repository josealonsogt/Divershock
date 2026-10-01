using DivershocksApi.Models;
using DivershocksApi.DTOs;
using Microsoft.EntityFrameworkCore;

namespace DivershocksApi.Services;

public class MovimientoService
{
    private readonly AppDbContext _db;

    // 1. INYECCIÓN DE DEPENDENCIAS: Le damos la llave de la base de datos
    public MovimientoService(AppDbContext db)
    {
        _db = db;
    }

    // 2. LA LÓGICA DE NEGOCIO: La acción de registrar el movimiento
    public async Task<string> Registrar(MovimientoRequestDto papelito)
    {
        // Regla de oro del cocinero: Nunca te fíes de lo que viene de fuera.
        // Validamos que Laura no haya puesto -5 calcetines por error en la web.
        if (papelito.Cantidad <= 0)
        {
            return "Error: La cantidad debe ser mayor que 0";
        }

        // Validamos que la palabra que nos manda React es una de las 4 permitidas.
        var tiposPermitidos = new[] { "EntradaAlmacen", "TrasladoTienda", "Venta", "AjusteStock" };
        if (!tiposPermitidos.Contains(papelito.Tipo))
        {
            return "Error: Tipo de movimiento no válido";
        }

        // 3. TRADUCCIÓN: Pasamos los datos del "papelito" al "Ingrediente Real"
        var nuevoMovimiento = new Movimiento
        {
            VarianteId = papelito.VarianteId,
            Tipo = papelito.Tipo,
            Cantidad = papelito.Cantidad,
            Fecha = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") // El servidor pone la fecha exacta
        };

        // 4. GUARDAR EN LA DESPENSA (Base de datos)
        _db.Movimientos.Add(nuevoMovimiento);
        await _db.SaveChangesAsync();

        return "OK";
    }
}