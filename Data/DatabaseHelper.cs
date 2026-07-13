using Microsoft.Data.Sqlite;

namespace ReceitasAtelie.Data
{
    public static class DatabaseHelper
    {
        public static void InicializarBanco()
        {
            string connectionString = DatabaseConfig.GetConnectionString();

            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            string queryCategorias = """
                CREATE TABLE IF NOT EXISTS Categorias (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Nome TEXT NOT NULL,
                    IdPai INTEGER,
                    FOREIGN KEY (IdPai) REFERENCES Categorias (Id) ON DELETE CASCADE
                );
                """;

            string queryReceitas = """
                CREATE TABLE IF NOT EXISTS Receitas (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    IdCategoria INTEGER NOT NULL,
                    Titulo TEXT NOT NULL,
                    CaminhoImagem TEXT,
                    CaminhoArquivo TEXT,
                    DataCadastro TEXT NOT NULL,
                    FOREIGN KEY (IdCategoria) REFERENCES Categorias (Id) ON DELETE CASCADE
                );
                """;

            using (var command = new SqliteCommand(queryCategorias, connection))
            {
                command.ExecuteNonQuery();
            }

            using (var command = new SqliteCommand(queryReceitas, connection))
            {
                command.ExecuteNonQuery();
            }
        }
    }
}