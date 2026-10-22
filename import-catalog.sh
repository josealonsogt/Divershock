#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "$0")" && pwd)"
DB_PATH="$ROOT_DIR/diverShocks.db"
CATALOG_URL="https://www.diversocks.com/collections/todos/products.json?limit=250"
TEMP_DIR="$(mktemp -d "$ROOT_DIR/.catalog-import.XXXXXX")"

cleanup() {
  rm -rf "$TEMP_DIR"
}
trap cleanup EXIT

if [[ ! -f "$DB_PATH" ]]; then
  echo "No se encuentra la base de datos: $DB_PATH" >&2
  exit 1
fi

echo "Descargando catálogo público..."
curl --fail --location --silent --show-error "$CATALOG_URL" \
  --output "$TEMP_DIR/catalog.json"

PRODUCT_COUNT="$(jq '.products | length' "$TEMP_DIR/catalog.json")"
echo "Productos recibidos: $PRODUCT_COUNT"

jq -r --arg quote "'" '
  def q: ($quote + (tostring | gsub($quote; $quote + $quote)) + $quote);
  "BEGIN IMMEDIATE;",
  (.products[] as $product |
    ("WEB-" + ($product.id | tostring)) as $reference |
    ("INSERT INTO Productos (Referencia, Nombre, Categoria, Foto, Precio, Activo) VALUES (" +
      ($reference | q) + "," +
      ((if (($product.title // "") | length) > 0 then $product.title else $reference end) | gsub("^[[:space:]]+|[[:space:]]+$"; "") | q) + "," +
      ((if (($product.product_type // "") | length) > 0 then $product.product_type else "Catálogo web" end) | q) + "," +
      (($product.images[0].src // "") | q) + "," +
      (((($product.variants[0].price // "0") | tonumber) | tostring)) +
      ",1) ON CONFLICT(Referencia) DO UPDATE SET Nombre=excluded.Nombre, Categoria=excluded.Categoria, Foto=excluded.Foto, Precio=excluded.Precio, Activo=1;"),
    ($product.variants[]? |
      ((.title // .option1 // "") | gsub("^[[:space:]]+|[[:space:]]+$"; "")) as $size |
      if ($size | length) > 0 then
        "INSERT OR IGNORE INTO Variantes (ProductoId, Talla) SELECT Id," + ($size | q) + " FROM Productos WHERE Referencia=" + ($reference | q) + " AND NOT EXISTS (SELECT 1 FROM Variantes WHERE ProductoId=Productos.Id AND upper(Talla)=upper(" + ($size | q) + "));"
      else empty end
    )
  ),
  "COMMIT;"
' "$TEMP_DIR/catalog.json" | sqlite3 "$DB_PATH"

echo "Importación terminada."
echo "Productos de catálogo: $(sqlite3 "$DB_PATH" "SELECT COUNT(*) FROM Productos WHERE Referencia LIKE 'WEB-%';")"
echo "Variantes de catálogo: $(sqlite3 "$DB_PATH" "SELECT COUNT(*) FROM Variantes WHERE ProductoId IN (SELECT Id FROM Productos WHERE Referencia LIKE 'WEB-%');")"
