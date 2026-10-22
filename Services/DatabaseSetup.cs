using Microsoft.Data.Sqlite;
namespace DivershocksApi.Services;
public static class DatabaseSetup
{
    public static void Initialize(string connectionString)
    {
        using var connection = new SqliteConnection(connectionString); connection.Open();
        using var check = connection.CreateCommand(); check.CommandText = "PRAGMA user_version";
        var version = Convert.ToInt32(check.ExecuteScalar());
        if (version >= 2) return;
        if (version == 1)
        {
            using var migrate = connection.CreateCommand();
            migrate.CommandText = "ALTER TABLE Productos ADD COLUMN Activo INTEGER NOT NULL DEFAULT 1; PRAGMA user_version=2;";
            migrate.ExecuteNonQuery();
            return;
        }
        // Preserve the original database before upgrading its movement constraint.
        var path = connection.DataSource;
        if (File.Exists(path) && new FileInfo(path).Length > 0)
        {
            using var backup = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path + ".before-v1-" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff") + ".bak" }.ToString());
            backup.Open(); connection.BackupDatabase(backup);
        }
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand(); command.Transaction = transaction;
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS Productos (Id INTEGER PRIMARY KEY AUTOINCREMENT, Referencia TEXT NOT NULL, Nombre TEXT NOT NULL, Categoria TEXT, Foto TEXT, Precio REAL, Activo INTEGER NOT NULL DEFAULT 1);
            CREATE TABLE IF NOT EXISTS Variantes (Id INTEGER PRIMARY KEY AUTOINCREMENT, ProductoId INTEGER NOT NULL REFERENCES Productos(Id) ON DELETE RESTRICT, Talla TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS Movimientos (Id INTEGER PRIMARY KEY AUTOINCREMENT, VarianteId INTEGER NOT NULL REFERENCES Variantes(Id) ON DELETE RESTRICT, Tipo TEXT NOT NULL, Cantidad INTEGER NOT NULL, Fecha TEXT NOT NULL);
            CREATE UNIQUE INDEX IF NOT EXISTS UX_Referencia ON Productos(trim(Referencia) COLLATE NOCASE);
            CREATE UNIQUE INDEX IF NOT EXISTS UX_ProductoTalla ON Variantes(ProductoId, trim(Talla) COLLATE NOCASE);
            CREATE TABLE Movimientos_v1 (Id INTEGER PRIMARY KEY AUTOINCREMENT, VarianteId INTEGER NOT NULL REFERENCES Variantes(Id) ON DELETE RESTRICT, Tipo TEXT NOT NULL CHECK(Tipo IN ('EntradaAlmacen','TrasladoTienda','Venta','AjusteStock')), Cantidad INTEGER NOT NULL CHECK((Tipo='AjusteStock' AND Cantidad<>0) OR (Tipo<>'AjusteStock' AND Cantidad>0)), Fecha TEXT NOT NULL);
            INSERT INTO Movimientos_v1 SELECT Id, VarianteId, Tipo, Cantidad, Fecha FROM Movimientos;
            DROP TABLE Movimientos;
            ALTER TABLE Movimientos_v1 RENAME TO Movimientos;
            CREATE INDEX IX_Movimientos_VarianteId ON Movimientos(VarianteId);
            PRAGMA user_version=2;
            """;
        command.ExecuteNonQuery(); transaction.Commit();
    }
}
