using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Data.Sqlite;
using ReceitasAtelie.Data;
using ReceitasAtelie.Models;

namespace ReceitasAtelie.ViewModels
{
    public class MainViewModel
    {
        public ObservableCollection<CategoriaNodeViewModel> CategoriasRaiz { get; set; } = new();

        public MainViewModel()
        {
            CarregarCategorias();
        }

        public void CarregarCategorias()
        {
            CategoriasRaiz.Clear();
            var todasCategorias = new List<Categoria>();

            string connectionString = DatabaseConfig.GetConnectionString();
            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            string query = "SELECT Id, Nome, IdPai FROM Categorias ORDER BY Nome;";
            using var command = new SqliteCommand(query, connection);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                todasCategorias.Add(new Categoria
                {
                    Id = reader.GetInt32(0),
                    Nome = reader.GetString(1),
                    IdPai = reader.IsDBNull(2) ? null : reader.GetInt32(2)
                });
            }

            var dicionarioNodos = todasCategorias.ToDictionary(c => c.Id, c => new CategoriaNodeViewModel(c));

            foreach (var nodo in dicionarioNodos.Values)
            {
                if (nodo.IdPai == null)
                {
                    CategoriasRaiz.Add(nodo);
                }
                else if (dicionarioNodos.TryGetValue(nodo.IdPai.Value, out var nodoPai))
                {
                    nodoPai.Subcategorias.Add(nodo);
                }
            }
        }

        public void SalvarCategoria(string nome, int? idPai)
        {
            if (string.IsNullOrWhiteSpace(nome)) return;

            try
            {
                string connectionString = DatabaseConfig.GetConnectionString();
                using var connection = new SqliteConnection(connectionString);
                connection.Open();

                string query = """
        INSERT INTO Categorias (Nome, IdPai) 
        VALUES ($nome, $idPai);
        """;

                using var command = new SqliteCommand(query, connection);
                command.Parameters.AddWithValue("$nome", nome.Trim());
                command.Parameters.AddWithValue("$idPai", (object?)idPai ?? DBNull.Value);

                command.ExecuteNonQuery();

                // --- LÓGICA DE CRIAÇÃO DE PASTAS NO MEUS DOCUMENTOS ---
                // 1. Caminho base fixo
                string meusDocumentos = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string caminhoBase = System.IO.Path.Combine(meusDocumentos, "MemoriasAtelie", "Fotos", "Receitas");

                string caminhoFinal;

                if (idPai == null)
                {
                    // Categoria Raiz: Documents\MemoriasAtelie\Fotos\Receitas\NomeDaCategoria
                    caminhoFinal = System.IO.Path.Combine(caminhoBase, nome.Trim());
                }
                else
                {
                    // Subcategoria: Precisamos descobrir o nome da categoria pai para montar o caminho correto
                    var nodoPai = EncontrarNoPorId(CategoriasRaiz, idPai.Value);
                    if (nodoPai != null)
                    {
                        // Documents\MemoriasAtelie\Fotos\Receitas\NomeDoPai\NomeDaSubcategoria
                        caminhoFinal = System.IO.Path.Combine(caminhoBase, nodoPai.Nome, nome.Trim());
                    }
                    else
                    {
                        // Caso de segurança se não achar o pai no cache atual
                        caminhoFinal = System.IO.Path.Combine(caminhoBase, nome.Trim());
                    }
                }

                // 2. Cria a pasta fisicamente (se já existir, o .NET ignora automaticamente)
                if (!System.IO.Directory.Exists(caminhoFinal))
                {
                    System.IO.Directory.CreateDirectory(caminhoFinal);
                }
                // ------------------------------------------------------

                // Recarrega a árvore do banco atualizada na tela
                CarregarCategorias();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Erro ao salvar categoria/pasta: {ex.Message}", "Erro", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        // Método auxiliar necessário para buscar o nome do pai antes do recarregamento completo
        private CategoriaNodeViewModel EncontrarNoPorId(System.Collections.IEnumerable nodos, int idBuscado)
        {
            foreach (CategoriaNodeViewModel nodo in nodos)
            {
                if (nodo.Id == idBuscado) return nodo;

                var encontrado = EncontrarNoPorId(nodo.Subcategorias, idBuscado);
                if (encontrado != null) return encontrado;
            }
            return null;
        }

        public void ExcluirCategoria(CategoriaNodeViewModel nodo)
        {
            if (nodo == null) return;

            var resultado = System.Windows.MessageBox.Show(
                $"Tem certeza que deseja excluir a categoria '{nodo.Nome}' e TODAS as suas subcategorias/pastas físicas?",
                "Confirmar Exclusão",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (resultado != System.Windows.MessageBoxResult.Yes) return;

            try
            {
                // 1. Descobrir o caminho da pasta antes de apagar do banco para sabermos o nome do pai se houver
                string meusDocumentos = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string caminhoBase = System.IO.Path.Combine(meusDocumentos, "MemoriasAtelie", "Fotos", "Receitas");
                string caminhoPasta;

                if (nodo.IdPai == null)
                {
                    caminhoPasta = System.IO.Path.Combine(caminhoBase, nodo.Nome);
                }
                else
                {
                    var nodoPai = EncontrarNoPorId(CategoriasRaiz, nodo.IdPai.Value);
                    caminhoPasta = nodoPai != null
                        ? System.IO.Path.Combine(caminhoBase, nodoPai.Nome, nodo.Nome)
                        : System.IO.Path.Combine(caminhoBase, nodo.Nome);
                }

                // 2. Apagar do Banco de Dados SQLite
                string connectionString = DatabaseConfig.GetConnectionString();
                using var connection = new SqliteConnection(connectionString);
                connection.Open();

                // O PRAGMA garante que o SQLite respeite a chave estrangeira e execute o CASCADE
                using (var pragmaCmd = new SqliteCommand("PRAGMA foreign_keys = ON;", connection))
                {
                    pragmaCmd.ExecuteNonQuery();
                }

                string query = "DELETE FROM Categorias WHERE Id = $id;";
                using var command = new SqliteCommand(query, connection);
                command.Parameters.AddWithValue("$id", nodo.Id);
                command.ExecuteNonQuery();

                // 3. Apagar a pasta física no Windows (e tudo dentro dela)
                if (System.IO.Directory.Exists(caminhoPasta))
                {
                    System.IO.Directory.Delete(caminhoPasta, true); // true força a exclusão recursiva de subpastas e arquivos
                }

                // 4. Atualiza a árvore na tela
                CarregarCategorias();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Erro ao excluir categoria/pasta: {ex.Message}", "Erro", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

    }
}