using System;

namespace ReceitasAtelie.Models
{
    public class Receita
    {
        public int Id { get; set; }
        public int IdCategoria { get; set; }
        // Propriedades de Categoria
        public int CategoriaId { get; set; }
        public string CategoriaNome { get; set; }
        // Propriedades de Mídia e Arquivos
        public string CaminhoImagemCapa { get; set; }
        public string CaminhoArquivoApoio { get; set; }

        // Texto formatado vindo do RichTextBox
        public string TextoFormatado { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string CaminhoTexto { get; set; } = string.Empty; // Novo campo
        public string? CaminhoImagem { get; set; }
        public string? CaminhoArquivo { get; set; }
        public string DataCadastro { get; set; } = string.Empty;
    }
}