using Microsoft.EntityFrameworkCore;

namespace DivershocksApi.Models;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Aquí le decimos a C# cuáles son las tablas que hay en SQLite
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<Variante> Variantes => Set<Variante>();
    public DbSet<Movimiento> Movimientos => Set<Movimiento>();
}
