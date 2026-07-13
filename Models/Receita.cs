namespace ReceitasAtelie.Models;

public class Receita
{
    public int Id { get; set; }
    public int IdCategoria { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string CaminhoImagem { get; set; } = string.Empty;
    public string CaminhoArquivo { get; set; } = string.Empty;
    public DateTime DataCadastro { get; set; } = DateTime.Now;
}