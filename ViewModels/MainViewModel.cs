using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Data.Sqlite;
using ReceitasAtelie.Data;
using ReceitasAtelie.Models;

namespace ReceitasAtelie.ViewModels
{
    public class MainViewModel
    {
        public ObservableCollection<CategoriaNodeViewModel> CategoriasRaiz { get; set; } = new();

        // Propriedade necessária para expor uma lista plana de categorias no ComboBox de edição
        public ObservableCollection<CategoriaNodeViewModel> Categorias { get; set; } = new();

        public ObservableCollection<Receita> ReceitasExibidas { get; set; } = new();

        public MainViewModel()
        {
            CarregarCategorias();
            CarregarReceitas(); // Carrega todas por padrão ao iniciar o app
        }

        public void CarregarCategorias()
        {
            CategoriasRaiz.Clear();
            Categorias.Clear();
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
                // Popula a lista plana usada em seletores (ex: ComboBox de categorias)
                Categorias.Add(nodo);

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
                string meusDocumentos = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string caminhoBase = Path.Combine(meusDocumentos, "MemoriasAtelie", "Fotos", "Receitas");
                string caminhoFinal;

                if (idPai == null)
                {
                    caminhoFinal = Path.Combine(caminhoBase, nome.Trim());
                }
                else
                {
                    var nodoPai = EncontrarNoPorId(CategoriasRaiz, idPai.Value);
                    if (nodoPai != null)
                    {
                        caminhoFinal = Path.Combine(caminhoBase, nodoPai.Nome, nome.Trim());
                    }
                    else
                    {
                        caminhoFinal = Path.Combine(caminhoBase, nome.Trim());
                    }
                }

                if (!Directory.Exists(caminhoFinal))
                {
                    Directory.CreateDirectory(caminhoFinal);
                }

                CarregarCategorias();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Erro ao salvar categoria/pasta: {ex.Message}", "Erro", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        public void CarregarReceitas(int? idCategoriaSelecionada = null)
        {
            ReceitasExibidas.Clear();

            try
            {
                string connectionString = DatabaseConfig.GetConnectionString();
                using var connection = new SqliteConnection(connectionString);
                connection.Open();

                // Se passarmos uma categoria, filtramos por ela. Caso contrário, traz tudo.
                string query = @"
            SELECT R.Id, R.IdCategoria, R.Titulo, R.CaminhoTexto, R.CaminhoImagem, R.CaminhoArquivo, C.Nome as CategoriaNome
            FROM Receitas R
            INNER JOIN Categorias C ON R.IdCategoria = C.Id";

                if (idCategoriaSelecionada != null)
                {
                    query += " WHERE R.IdCategoria = $idCategoria";
                }

                query += " ORDER BY R.Titulo;";

                using var command = new SqliteCommand(query, connection);
                if (idCategoriaSelecionada != null)
                {
                    command.Parameters.AddWithValue("$idCategoria", idCategoriaSelecionada.Value);
                }

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    // Cria o objeto de modelo
                    var receita = new Receita
                    {
                        Id = reader.GetInt32(0),
                        CategoriaId = reader.GetInt32(1),
                        Titulo = reader.GetString(2),
                        CaminhoTexto = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                        CaminhoImagemCapa = reader.IsDBNull(4) ? null : reader.GetString(4),
                        CaminhoArquivoApoio = reader.IsDBNull(5) ? null : reader.GetString(5),
                        CategoriaNome = reader.GetString(6)
                    };

                    // Carrega o conteúdo RTF para a propriedade TextoFormatado se o arquivo físico existir
                    if (!string.IsNullOrEmpty(receita.CaminhoTexto) && File.Exists(receita.CaminhoTexto))
                    {
                        receita.TextoFormatado = File.ReadAllText(receita.CaminhoTexto, System.Text.Encoding.UTF8);
                    }

                    ReceitasExibidas.Add(receita);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Erro ao carregar receitas: {ex.Message}", "Erro", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private CategoriaNodeViewModel EncontrarNoPorId(IEnumerable nodos, int idBuscado)
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
                string meusDocumentos = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string caminhoBase = Path.Combine(meusDocumentos, "MemoriasAtelie", "Fotos", "Receitas");
                string caminhoPasta;

                if (nodo.IdPai == null)
                {
                    caminhoPasta = Path.Combine(caminhoBase, nodo.Nome);
                }
                else
                {
                    var nodoPai = EncontrarNoPorId(CategoriasRaiz, nodo.IdPai.Value);
                    caminhoPasta = nodoPai != null
                        ? Path.Combine(caminhoBase, nodoPai.Nome, nodo.Nome)
                        : Path.Combine(caminhoBase, nodo.Nome);
                }

                string connectionString = DatabaseConfig.GetConnectionString();
                using var connection = new SqliteConnection(connectionString);
                connection.Open();

                using (var pragmaCmd = new SqliteCommand("PRAGMA foreign_keys = ON;", connection))
                {
                    pragmaCmd.ExecuteNonQuery();
                }

                string query = "DELETE FROM Categorias WHERE Id = $id;";
                using var command = new SqliteCommand(query, connection);
                command.Parameters.AddWithValue("$id", nodo.Id);
                command.ExecuteNonQuery();

                if (Directory.Exists(caminhoPasta))
                {
                    Directory.Delete(caminhoPasta, true);
                }

                CarregarCategorias();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Erro ao excluir categoria/pasta: {ex.Message}", "Erro", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        public bool SalvarReceita(string titulo, int idCategoria, string rtfConteudo, string caminhoOriginalAnexo, string caminhoOriginalImagem)
        {
            try
            {
                var categoria = EncontrarNoPorId(CategoriasRaiz, idCategoria);
                if (categoria == null)
                {
                    System.Windows.MessageBox.Show("Categoria não encontrada no sistema.", "Erro", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return false;
                }

                string meusDocumentos = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string caminhoBase = Path.Combine(meusDocumentos, "MemoriasAtelie", "Fotos", "Receitas");
                string caminhoPastaCategoria;

                if (categoria.IdPai == null)
                {
                    caminhoPastaCategoria = Path.Combine(caminhoBase, categoria.Nome);
                }
                else
                {
                    var nodoPai = EncontrarNoPorId(CategoriasRaiz, categoria.IdPai.Value);
                    caminhoPastaCategoria = nodoPai != null
                        ? Path.Combine(caminhoBase, nodoPai.Nome, categoria.Nome)
                        : Path.Combine(caminhoBase, categoria.Nome);
                }

                string nomePastaReceita = string.Concat(titulo.Split(Path.GetInvalidFileNameChars())).Trim();
                string caminhoPastaReceita = Path.Combine(caminhoPastaCategoria, nomePastaReceita);

                if (!Directory.Exists(caminhoPastaReceita))
                {
                    Directory.CreateDirectory(caminhoPastaReceita);
                }

                string caminhoFinalTexto = Path.Combine(caminhoPastaReceita, $"{nomePastaReceita}.rtf");
                File.WriteAllText(caminhoFinalTexto, rtfConteudo, Encoding.UTF8);

                string? caminhoFinalImagem = null;
                if (!string.IsNullOrWhiteSpace(caminhoOriginalImagem) && File.Exists(caminhoOriginalImagem))
                {
                    string extensao = Path.GetExtension(caminhoOriginalImagem);
                    caminhoFinalImagem = Path.Combine(caminhoPastaReceita, $"capa{extensao}");
                    File.Copy(caminhoOriginalImagem, caminhoFinalImagem, true);
                }

                string? caminhoFinalAnexo = null;
                if (!string.IsNullOrWhiteSpace(caminhoOriginalAnexo) && File.Exists(caminhoOriginalAnexo))
                {
                    string nomeArquivoOriginal = Path.GetFileName(caminhoOriginalAnexo);
                    caminhoFinalAnexo = Path.Combine(caminhoPastaReceita, nomeArquivoOriginal);
                    File.Copy(caminhoOriginalAnexo, caminhoFinalAnexo, true);
                }

                string connectionString = DatabaseConfig.GetConnectionString();
                using var connection = new SqliteConnection(connectionString);
                connection.Open();

                string query = """
                INSERT INTO Receitas (IdCategoria, Titulo, CaminhoTexto, CaminhoImagem, CaminhoArquivo, DataCadastro)
                VALUES ($idCategoria, $titulo, $caminhoTexto, $caminhoImagem, $caminhoArquivo, $data);
                """;

                using var command = new SqliteCommand(query, connection);
                command.Parameters.AddWithValue("$idCategoria", idCategoria);
                command.Parameters.AddWithValue("$titulo", titulo);
                command.Parameters.AddWithValue("$caminhoTexto", caminhoFinalTexto);
                command.Parameters.AddWithValue("$caminhoImagem", (object?)caminhoFinalImagem ?? DBNull.Value);
                command.Parameters.AddWithValue("$caminhoArquivo", (object?)caminhoFinalAnexo ?? DBNull.Value);
                command.Parameters.AddWithValue("$data", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                command.ExecuteNonQuery();

                System.Windows.MessageBox.Show("Receita cadastrada e arquivos salvos com sucesso!", "Sucesso", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                return true;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Erro ao salvar a receita fisicamente: {ex.Message}", "Erro", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return false;
            }
        }

        // --- NOVO MÉTODO IMPLEMENTADO PARA EDITAR E SALVAR ALTERAÇÕES ---
        public bool SalvarAlteracoesReceita(Receita receita)
        {
            try
            {
                // 1. Localiza a pasta física antiga da receita usando o caminho salvo no banco
                string caminhoTextoAntigo = GetCaminhoTextoDoBanco(receita.Id);
                string pastaReceita = !string.IsNullOrEmpty(caminhoTextoAntigo)
                    ? Path.GetDirectoryName(caminhoTextoAntigo)
                    : string.Empty;

                // Caso não localize pelo banco, tenta estruturar com base na categoria atual selecionada
                if (string.IsNullOrEmpty(pastaReceita) || !Directory.Exists(pastaReceita))
                {
                    var categoria = EncontrarNoPorId(CategoriasRaiz, receita.CategoriaId);
                    if (categoria != null)
                    {
                        string meusDocumentos = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                        string caminhoBase = Path.Combine(meusDocumentos, "MemoriasAtelie", "Fotos", "Receitas");
                        string caminhoCategoria = categoria.IdPai == null
                            ? Path.Combine(caminhoBase, categoria.Nome)
                            : Path.Combine(caminhoBase, EncontrarNoPorId(CategoriasRaiz, categoria.IdPai.Value)?.Nome ?? "", categoria.Nome);

                        string nomePastaClean = string.Concat(receita.Titulo.Split(Path.GetInvalidFileNameChars())).Trim();
                        pastaReceita = Path.Combine(caminhoCategoria, nomePastaClean);
                        Directory.CreateDirectory(pastaReceita);
                    }
                }

                // 2. Atualiza fisicamente o arquivo .rtf com o novo texto formatado
                string nomePastaOriginal = Path.GetFileName(pastaReceita);
                string caminhoFinalTexto = Path.Combine(pastaReceita, $"{nomePastaOriginal}.rtf");
                File.WriteAllText(caminhoFinalTexto, receita.TextoFormatado, Encoding.UTF8);

                // 3. Atualiza os dados no banco de dados SQLite
                string connectionString = DatabaseConfig.GetConnectionString();
                using var connection = new SqliteConnection(connectionString);
                connection.Open();

                string query = """
                UPDATE Receitas 
                SET IdCategoria = $idCategoria, 
                    Titulo = $titulo, 
                    CaminhoTexto = $caminhoTexto, 
                    CaminhoImagem = $caminhoImagem, 
                    CaminhoArquivo = $caminhoArquivo
                WHERE Id = $id;
                """;

                using var command = new SqliteCommand(query, connection);
                command.Parameters.AddWithValue("$idCategoria", receita.CategoriaId);
                command.Parameters.AddWithValue("$titulo", receita.Titulo);
                command.Parameters.AddWithValue("$caminhoTexto", caminhoFinalTexto);
                command.Parameters.AddWithValue("$caminhoImagem", (object?)receita.CaminhoImagemCapa ?? DBNull.Value);
                command.Parameters.AddWithValue("$caminhoArquivo", (object?)receita.CaminhoArquivoApoio ?? DBNull.Value);
                command.Parameters.AddWithValue("$id", receita.Id);

                command.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Erro ao atualizar receita no banco/disco: {ex.Message}", "Erro", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return false;
            }
        }

        // Busca o caminho de texto gravado anteriormente no banco para sabermos onde atualizar o .rtf
        private string GetCaminhoTextoDoBanco(int receitaId)
        {
            try
            {
                string connectionString = DatabaseConfig.GetConnectionString();
                using var connection = new SqliteConnection(connectionString);
                connection.Open();

                string query = "SELECT CaminhoTexto FROM Receitas WHERE Id = $id;";
                using var command = new SqliteCommand(query, connection);
                command.Parameters.AddWithValue("$id", receitaId);

                var result = command.ExecuteScalar();
                return result != null ? result.ToString() : string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}