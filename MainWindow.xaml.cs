using System.Windows;
using ReceitasAtelie.ViewModels;
using ReceitasAtelie.Views;

namespace ReceitasAtelie;

public partial class MainWindow : Window
{
    private bool _isMenuExpanded = false;
    private MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel();
        DataContext = _viewModel;

        // Vincula o evento do botão de adicionar categorias
        BtnAdicionarMenu.Click += BtnAdicionarMenu_Click;
    }

    private void BtnHamburguer_Click(object sender, RoutedEventArgs e)
    {
        if (TvMenus.Visibility == Visibility.Collapsed)
        {
            TvMenus.Visibility = Visibility.Visible;
            BtnAdicionarMenu.Visibility = Visibility.Visible;
            BtnExcluirMenu.Visibility = Visibility.Visible; // Mostra o botão de excluir
            MenuColumn.Width = new GridLength(250);
        }
        else
        {
            TvMenus.Visibility = Visibility.Collapsed;
            BtnAdicionarMenu.Visibility = Visibility.Collapsed;
            BtnExcluirMenu.Visibility = Visibility.Collapsed; // Esconde o botão de excluir
            MenuColumn.Width = GridLength.Auto;
        }
    }

    private void BtnExcluirMenu_Click(object sender, RoutedEventArgs e)
    {
        // Pega o item que está selecionado atualmente na TreeView
        if (TvMenus.SelectedItem is CategoriaNodeViewModel nodoSelecionado)
        {
            if (DataContext is ReceitasAtelie.ViewModels.MainViewModel viewModel)
            {
                viewModel.ExcluirCategoria(nodoSelecionado);
            }
        }
        else
        {
            MessageBox.Show("Por favor, selecione uma categoria na árvore lateral primeiro para poder excluí-la.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void BtnAdicionarCategoria_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is ReceitasAtelie.ViewModels.MainViewModel viewModel)
        {
            var janelaCadastro = new ReceitasAtelie.Views.CadastroCategoriaWindow(viewModel);
            janelaCadastro.Owner = this;
            janelaCadastro.ShowDialog();
        }
    }

    // Este é o método que estava faltando ou com nome diferente:
    private void BtnAdicionarMenu_Click(object sender, RoutedEventArgs e)
    {
        var cadastroWindow = new CadastroCategoriaWindow(_viewModel);
        cadastroWindow.Owner = this;
        cadastroWindow.ShowDialog();
    }
}