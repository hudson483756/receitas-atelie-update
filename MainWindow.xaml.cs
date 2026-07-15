using ReceitasAtelie.Models;
using ReceitasAtelie.ViewModels;
using ReceitasAtelie.Views;
using System;
using System.Windows;
using System.Windows.Controls;

namespace ReceitasAtelie
{
    public partial class MainWindow : Window
    {
        private bool _isMenuExpanded = false;
        private MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            this.DataContext = _viewModel;

            // Carrega as receitas iniciais na tela
            AtualizarListaCards();
        }

        private void BtnHamburguer_Click(object sender, RoutedEventArgs e)
        {
            if (TvMenus.Visibility == Visibility.Collapsed)
            {
                TvMenus.Visibility = Visibility.Visible;
                BtnAdicionarMenu.Visibility = Visibility.Visible;
                BtnExcluirMenu.Visibility = Visibility.Visible;
                MenuColumn.Width = new GridLength(250);
            }
            else
            {
                TvMenus.Visibility = Visibility.Collapsed;
                BtnAdicionarMenu.Visibility = Visibility.Collapsed;
                BtnExcluirMenu.Visibility = Visibility.Collapsed;
                MenuColumn.Width = GridLength.Auto;
            }
        }

        // Método adicionado para responder ao clique do botão de atualizar (BtnAtualizar)
        private void BtnAtualizar_Click(object sender, RoutedEventArgs e)
        {
            // Limpa a barra de pesquisa
            TxtPesquisa.Text = string.Empty;

            // Como no WPF não é trivial limpar a seleção do TreeView sem quebrar bindings de propriedades somente leitura, 
            // se houver um item selecionado, nós recarregamos todas as receitas por padrão na ViewModel
            if (TvMenus.SelectedItem != null)
            {
                _viewModel.CarregarReceitas();
            }
            else
            {
                AtualizarListaCards();
            }
        }

        private void BtnExcluirMenu_Click(object sender, RoutedEventArgs e)
        {
            if (TvMenus.SelectedItem is CategoriaNodeViewModel nodoSelecionado)
            {
                _viewModel.ExcluirCategoria(nodoSelecionado);
                // Após excluir a categoria, limpa o filtro e atualiza os cards
                AtualizarListaCards();
            }
            else
            {
                MessageBox.Show("Por favor, selecione uma categoria na árvore lateral primeiro para poder excluí-la.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // Evento disparado ao selecionar uma categoria na árvore lateral (TreeView)
        private void TvMenus_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            AtualizarListaCards();
        }

        // Evento do botão de adicionar categorias do menu lateral
        private void BtnAdicionarMenu_Click(object sender, RoutedEventArgs e)
        {
            var cadastroWindow = new CadastroCategoriaWindow(_viewModel);
            cadastroWindow.Owner = this;
            cadastroWindow.ShowDialog();
        }

        private void BtnNovaReceita_Click(object sender, RoutedEventArgs e)
        {
            var telaCadastro = new CadastroReceitaWindow(_viewModel);
            telaCadastro.Owner = this;

            if (telaCadastro.ShowDialog() == true)
            {
                AtualizarListaCards();
            }
        }

        private void CardReceita_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button botao && botao.Tag is Receita receitaSelecionada)
            {
                AbrirVisualizacaoReceita(receitaSelecionada);
            }
            else if (sender is FrameworkElement elemento && elemento.DataContext is Receita receitaDoContexto)
            {
                AbrirVisualizacaoReceita(receitaDoContexto);
            }
        }

        // Método agora funcional para carregar as receitas respeitando a categoria selecionada
        private void AtualizarListaCards()
        {
            if (_viewModel != null)
            {
                if (TvMenus.SelectedItem is CategoriaNodeViewModel categoriaSelecionada)
                {
                    // Carrega apenas as receitas da categoria clicada
                    _viewModel.CarregarReceitas(categoriaSelecionada.Id);
                }
                else
                {
                    // Se nenhuma categoria estiver selecionada, traz tudo por padrão
                    _viewModel.CarregarReceitas();
                }
            }
        }

        private void AbrirVisualizacaoReceita(Receita receitaSelecionada)
        {
            if (receitaSelecionada == null) return;

            try
            {
                VisualizarReceitaWindow janelaVisualizar = new VisualizarReceitaWindow(receitaSelecionada, _viewModel);
                janelaVisualizar.Owner = this;

                bool? resultado = janelaVisualizar.ShowDialog();

                if (resultado == true)
                {
                    _viewModel.CarregarCategorias();
                    AtualizarListaCards();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao abrir a receita: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}