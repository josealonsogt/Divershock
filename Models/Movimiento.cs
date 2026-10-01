namespace DivershocksApi.Models;

public class Movimiento
{
    public int Id { get; set; }
    public int VarianteId { get; set; }
    public string Tipo { get; set; } = string.Empty; 
    public int Cantidad { get; set; }
    public string Fecha { get; set; } = string.Empty;
}