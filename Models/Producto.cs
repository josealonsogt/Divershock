
namespace DivershocksApi.Models;

public class Producto
{
    public int Id { get; set; }
    public string Referencia { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public string Foto { get; set; } = string.Empty;
    public double Precio { get; set; }
    public bool Activo { get; set; } = true;
}
