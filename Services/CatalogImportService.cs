using System.Net.Http.Json;
using System.Text.Json.Serialization;
using DivershocksApi.Models;
using Microsoft.EntityFrameworkCore;

namespace DivershocksApi.Services;

public sealed class CatalogImportService(AppDbContext db, HttpClient http)
{
    private const string SourceUrl = "https://www.diversocks.com/collections/todos/products.json?limit=250";

    public async Task<ImportResult> Importar()
    {
        var catalog = await http.GetFromJsonAsync<ShopifyCatalog>(SourceUrl)
            ?? throw new InvalidOperationException("La tienda no devolvió un catálogo válido.");

        var imported = 0;
        await using var transaction = await db.Database.BeginTransactionAsync();
        foreach (var item in catalog.Products)
        {
            var reference = $"WEB-{item.Id}";
            var product = await db.Productos.FirstOrDefaultAsync(p => p.Referencia == reference);
            if (product is null)
            {
                product = new Producto { Referencia = reference };
                db.Productos.Add(product);
            }

            product.Activo = true;
            product.Nombre = string.IsNullOrWhiteSpace(item.Title) ? reference : item.Title.Trim();
            product.Categoria = string.IsNullOrWhiteSpace(item.ProductType) ? "Catálogo web" : item.ProductType.Trim();
            product.Foto = item.Images.FirstOrDefault()?.Src ?? string.Empty;
            product.Precio = item.Variants.FirstOrDefault()?.Price ?? 0;
            await db.SaveChangesAsync();

            foreach (var remoteVariant in item.Variants)
            {
                var size = (remoteVariant.Title ?? remoteVariant.Option1 ?? string.Empty).Trim();
                if (size.Length == 0) continue;
                var exists = await db.Variantes.AnyAsync(v => v.ProductoId == product.Id && v.Talla.ToUpper() == size.ToUpper());
                if (!exists) db.Variantes.Add(new Variante { ProductoId = product.Id, Talla = size });
            }
            await db.SaveChangesAsync();
            imported++;
        }

        await transaction.CommitAsync();
        return new ImportResult(imported, catalog.Products.Count);
    }

    public record ImportResult(int ProductosImportados, int ProductosRecibidos);

    private sealed class ShopifyCatalog
    {
        [JsonPropertyName("products")] public List<ShopifyProduct> Products { get; set; } = new();
    }

    private sealed class ShopifyProduct
    {
        [JsonPropertyName("id")] public long Id { get; set; }
        [JsonPropertyName("title")] public string Title { get; set; } = string.Empty;
        [JsonPropertyName("product_type")] public string ProductType { get; set; } = string.Empty;
        [JsonPropertyName("variants")] public List<ShopifyVariant> Variants { get; set; } = new();
        [JsonPropertyName("images")] public List<ShopifyImage> Images { get; set; } = new();
    }

    private sealed class ShopifyVariant
    {
        [JsonPropertyName("title")] public string Title { get; set; } = string.Empty;
        [JsonPropertyName("option1")] public string Option1 { get; set; } = string.Empty;
        [JsonPropertyName("price")] public double Price { get; set; }
    }

    private sealed class ShopifyImage
    {
        [JsonPropertyName("src")] public string Src { get; set; } = string.Empty;
    }
}
