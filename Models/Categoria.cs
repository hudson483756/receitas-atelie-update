namespace ReceitasAtelie.Models;

public class Categoria
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public int? IdPai { get; set; }
}