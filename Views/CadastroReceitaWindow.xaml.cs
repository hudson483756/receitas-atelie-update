using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Microsoft.Win32;
using ReceitasAtelie.ViewModels;

namespace ReceitasAtelie.Views
{
    public partial class CadastroReceitaWindow : Window
    {
        private MainViewModel _viewModel;

        public CadastroReceitaWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;

            // 1. Configurar as Fontes e selecionar a padrão (Segoe UI)
            CboFontes.SelectedItem = new FontFamily("Segoe UI");

            // 2. Configurar o Tamanho de Fonte padrão
            CboTamanhoFonte.SelectedItem = 14.0;

            // 3. Carrega as categorias na ComboBox de forma plana
            var listaPlana = new List<object>();
            ObterCategoriasLineares(viewModel.CategoriasRaiz, listaPlana);
            CboCategorias.ItemsSource = listaPlana;
        }

        private void ObterCategoriasLineares(System.Collections.IEnumerable index, List<object> lista)
        {
            foreach (CategoriaNodeViewModel nodo in index)
            {
                lista.Add(new { Id = nodo.Id, Nome = nodo.Nome, IdPai = nodo.IdPai });
                if (nodo.Subcategorias.Count > 0)
                {
                    ObterCategoriasLineares(nodo.Subcategorias, lista);
                }
            }
        }

        // --- CONTROLES DE FORMATAÇÃO DO TEXTO ---

        private void CboFontes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RtbTextoReceita == null || CboFontes.SelectedItem == null) return;
            FontFamily fonte = (FontFamily)CboFontes.SelectedItem;
            RtbTextoReceita.Selection.ApplyPropertyValue(TextElement.FontFamilyProperty, fonte);
        }

        private void CboTamanhoFonte_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RtbTextoReceita == null) return;

            double tamanho;
            if (CboTamanhoFonte.SelectedItem is double t)
            {
                tamanho = t;
            }
            else if (double.TryParse(CboTamanhoFonte.Text, out double digitado))
            {
                tamanho = digitado;
            }
            else
            {
                return;
            }

            RtbTextoReceita.Selection.ApplyPropertyValue(TextElement.FontSizeProperty, tamanho);
        }

        // Método acionado quando a cor selecionada no ColorPicker é alterada
        private void CpTexto_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (RtbTextoReceita == null || !e.NewValue.HasValue) return;

            // Cria um SolidColorBrush com a cor escolhida pelo usuário no ColorPicker
            SolidColorBrush corBrush = new SolidColorBrush(e.NewValue.Value);
            RtbTextoReceita.Selection.ApplyPropertyValue(TextElement.ForegroundProperty, corBrush);
        }

        // --- BUSCA DE ANEXOS ---

        private void BtnBuscarArquivo_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Documentos (*.pdf;*.docx;*.txt)|*.pdf;*.docx;*.txt|Todos os arquivos (*.*)|*.*",
                Title = "Selecione o arquivo de apoio"
            };

            if (dialog.ShowDialog() == true)
            {
                TxtCaminhoArquivo.Text = dialog.FileName;
            }
        }

        private void BtnBuscarImagem_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Imagens (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png",
                Title = "Selecione a imagem de capa"
            };

            if (dialog.ShowDialog() == true)
            {
                TxtCaminhoImagem.Text = dialog.FileName;
            }
        }

        // --- SALVAR ---

        private void BtnSalvar_Click(object sender, RoutedEventArgs e)
        {
            string titulo = TxtTitulo.Text.Trim();
            int? idCategoria = (int?)CboCategorias.SelectedValue;
            string caminhoAnexo = TxtCaminhoArquivo.Text;
            string caminhoImagem = TxtCaminhoImagem.Text;

            if (string.IsNullOrWhiteSpace(titulo))
            {
                MessageBox.Show("Por favor, insira o título da receita.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (idCategoria == null)
            {
                MessageBox.Show("Por favor, selecione uma categoria.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Exportar o RichTextBox para String RTF
            TextRange documentoInteiro = new TextRange(RtbTextoReceita.Document.ContentStart, RtbTextoReceita.Document.ContentEnd);
            string rtfConteudo = "";

            using (MemoryStream ms = new MemoryStream())
            {
                documentoInteiro.Save(ms, DataFormats.Rtf);
                ms.Position = 0;
                using (StreamReader sr = new StreamReader(ms))
                {
                    rtfConteudo = sr.ReadToEnd();
                }
            }

            bool sucesso = _viewModel.SalvarReceita(titulo, idCategoria.Value, rtfConteudo, caminhoAnexo, caminhoImagem);

            if (sucesso)
            {
                DialogResult = true;
                Close();
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}