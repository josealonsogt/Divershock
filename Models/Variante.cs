namespace DivershocksApi.Models;

public class Variante
{
    public int Id { get; set; }
    public int ProductoId { get; set; } 
    public string Talla { get; set; } = string.Empty;
}