using System.Collections.ObjectModel;
using ReceitasAtelie.Models;

namespace ReceitasAtelie.ViewModels;

public class CategoriaNodeViewModel
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public int? IdPai { get; set; }

    // Lista de subcategorias que pertencem a esta categoria
    public ObservableCollection<CategoriaNodeViewModel> Subcategorias { get; set; } = new();

    public CategoriaNodeViewModel(Categoria categoria)
    {
        Id = categoria.Id;
        Nome = categoria.Nome;
        IdPai = categoria.IdPai;
    }
}