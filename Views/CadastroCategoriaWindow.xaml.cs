using System;
using System.Collections.Generic;
using System.Windows;
using ReceitasAtelie.ViewModels;

namespace ReceitasAtelie.Views
{
    public partial class CadastroCategoriaWindow : Window
    {
        private MainViewModel _viewModel;

        public CadastroCategoriaWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;

            var listaPlana = new List<object>();
            ObterCategoriasLineares(viewModel.CategoriasRaiz, listaPlana);

            CboCategoriasPai.ItemsSource = listaPlana;
        }

        private void ObterCategoriasLineares(System.Collections.IEnumerable nodos, List<object> lista)
        {
            foreach (CategoriaNodeViewModel nodo in nodos)
            {
                lista.Add(new { Id = nodo.Id, Nome = nodo.Nome });
                if (nodo.Subcategorias.Count > 0)
                {
                    ObterCategoriasLineares(nodo.Subcategorias, lista);
                }
            }
        }

        private void BtnSalvar_Click(object sender, RoutedEventArgs e)
        {
            string nome = TxtNome.Text;
            int? idPai = (int?)CboCategoriasPai.SelectedValue;

            if (!string.IsNullOrWhiteSpace(nome))
            {
                _viewModel.SalvarCategoria(nome, idPai);
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Por favor, insira um nome válido para o menu.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}