namespace DivershocksApi.DTOs;

// Datos que recibe la API para crear o editar un producto.
// El Id nunca viene del cliente y las tallas solo se usan al crear.
public class ProductoRequestDto
{
    public string Referencia { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public string Foto { get; set; } = string.Empty;
    public double Precio { get; set; }
    public List<string> Tallas { get; set; } = new();
}
