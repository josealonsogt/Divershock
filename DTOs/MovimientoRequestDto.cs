namespace DivershocksApi.DTOs;

// Este es el "papelito amarillo" que rellena React.
public class MovimientoRequestDto
{
    public int VarianteId { get; set; }  // Ej: 1 (Talla 36-40 del Barefoot)
    public string Tipo { get; set; } = string.Empty; // Ej: "Venta" o "TrasladoTienda"
    public int Cantidad { get; set; }    // Ej: 2 pares
}