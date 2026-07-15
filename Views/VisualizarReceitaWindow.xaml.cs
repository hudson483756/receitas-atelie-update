using Microsoft.Win32;
using ReceitasAtelie.Models;
using ReceitasAtelie.ViewModels;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ReceitasAtelie.Views
{
    public partial class VisualizarReceitaWindow : Window
    {
        private Receita _receita;
        private MainViewModel _mainViewModel; // Armazena a referência para a ViewModel principal
        private string _novoCaminhoImagem;
        private string _novoCaminhoArquivo;
        private bool _modoEdicao = false;

        // CONSTRUTOR: Aceita a receita e a ViewModel principal
        public VisualizarReceitaWindow(Receita receita, MainViewModel viewModel)
        {
            InitializeComponent();
            _receita = receita;
            _mainViewModel = viewModel; // Atribui a ViewModel injetada

            CarregarCategorias();
            PreencherCampos();
            AlternarModo(editar: false);
        }

        // 1. CARREGAMENTO DOS DADOS
        private void CarregarCategorias()
        {
            // Utiliza diretamente a ViewModel injetada no construtor
            if (_mainViewModel != null)
            {
                CboCategorias.ItemsSource = _mainViewModel.Categorias;
            }
            // Fallback de segurança caso a injeção falhe por algum motivo
            else if (Application.Current.MainWindow.DataContext is MainViewModel mainVm)
            {
                _mainViewModel = mainVm;
                CboCategorias.ItemsSource = mainVm.Categorias;
            }
        }

        private void PreencherCampos()
        {
            // Título e Categoria
            TxtBlockTitulo.Text = _receita.Titulo;
            TxtBoxTitulo.Text = _receita.Titulo;

            TxtBlockCategoria.Text = _receita.CategoriaNome;
            CboCategorias.SelectedValue = _receita.CategoriaId;

            // Imagem de Capa (Corrigido para evitar travamento do arquivo físico)
            if (!string.IsNullOrEmpty(_receita.CaminhoImagemCapa) && File.Exists(_receita.CaminhoImagemCapa))
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad; // Libera o arquivo imediatamente após carregar
                    bitmap.UriSource = new Uri(_receita.CaminhoImagemCapa, UriKind.Absolute);
                    bitmap.EndInit();
                    ImgCapa.Source = bitmap;
                }
                catch
                {
                    CarregarImagemPlaceholder();
                }
            }
            else
            {
                CarregarImagemPlaceholder();
            }

            // Arquivo de Apoio
            if (!string.IsNullOrEmpty(_receita.CaminhoArquivoApoio) && File.Exists(_receita.CaminhoArquivoApoio))
            {
                TxtNomeArquivo.Text = Path.GetFileName(_receita.CaminhoArquivoApoio);
                TxtCaminhoArquivo.Text = _receita.CaminhoArquivoApoio;
                StackAcoesArquivo.Visibility = Visibility.Visible;
            }
            else
            {
                TxtNomeArquivo.Text = "Nenhum arquivo anexado";
                TxtCaminhoArquivo.Text = string.Empty;
                StackAcoesArquivo.Visibility = Visibility.Collapsed;
            }

            // Carrega o RichText (RTF) salvo no banco ou arquivo
            CarregarTextoFormatado();
        }

        private void CarregarImagemPlaceholder()
        {
            // Imagem padrão caso não exista arquivo
            ImgCapa.Source = new BitmapImage(new Uri("pack://application:,,,/Assets/placeholder-receita.png", UriKind.Absolute));
        }

        private void CarregarTextoFormatado()
        {
            if (string.IsNullOrEmpty(_receita.TextoFormatado)) return;

            try
            {
                var range = new TextRange(RtbTextoReceita.Document.ContentStart, RtbTextoReceita.Document.ContentEnd);
                using (var memoryStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(_receita.TextoFormatado)))
                {
                    range.Load(memoryStream, DataFormats.Rtf);
                }
            }
            catch
            {
                // Caso não consiga carregar como RTF, exibe como texto puro
                var range = new TextRange(RtbTextoReceita.Document.ContentStart, RtbTextoReceita.Document.ContentEnd);
                range.Text = _receita.TextoFormatado;
            }
        }

        // 2. ALTERNADOR DINÂMICO (LEITURA VS EDICÃO)
        private void AlternarModo(bool editar)
        {
            _modoEdicao = editar;

            // Visibilidade do Título
            TxtBlockTitulo.Visibility = editar ? Visibility.Collapsed : Visibility.Visible;
            TxtBoxTitulo.Visibility = editar ? Visibility.Visible : Visibility.Collapsed;

            // Visibilidade da Categoria
            TxtBlockCategoria.Visibility = editar ? Visibility.Collapsed : Visibility.Visible;
            CboCategorias.Visibility = editar ? Visibility.Visible : Visibility.Collapsed;

            // Controles de Imagem e Anexo
            BtnAlterarImagem.Visibility = editar ? Visibility.Visible : Visibility.Collapsed;
            GridEdicaoArquivo.Visibility = editar ? Visibility.Visible : Visibility.Collapsed;

            // Só mostra o nome do arquivo/botão abrir se não estiver editando ou se houver arquivo
            TxtNomeArquivo.Visibility = editar ? Visibility.Collapsed : Visibility.Visible;
            StackAcoesArquivo.Visibility = (!editar && !string.IsNullOrEmpty(_receita.CaminhoArquivoApoio)) ? Visibility.Visible : Visibility.Collapsed;

            // Modo do RichTextBox
            RtbTextoReceita.IsReadOnly = !editar;
            BarraFormatacao.Visibility = editar ? Visibility.Visible : Visibility.Collapsed;

            // Botões de Ação Inferiores
            BtnEditar.Visibility = editar ? Visibility.Collapsed : Visibility.Visible;
            BtnSalvarEdicao.Visibility = editar ? Visibility.Visible : Visibility.Collapsed;
            BtnCancelarEdicao.Visibility = editar ? Visibility.Visible : Visibility.Collapsed;
        }

        // 3. EVENTOS DOS BOTÕES

        private void CpCorTexto_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (RtbTextoReceita == null || !_modoEdicao) return;

            // Verifica se uma cor válida foi selecionada
            if (e.NewValue.HasValue)
            {
                // Cria um pincel (Brush) com a cor selecionada
                var brush = new SolidColorBrush(e.NewValue.Value);

                // Aplica ao texto selecionado no RichTextBox
                if (!RtbTextoReceita.Selection.IsEmpty)
                {
                    RtbTextoReceita.Selection.ApplyPropertyValue(TextElement.ForegroundProperty, brush);
                }
            }
        }
        private void BtnEditar_Click(object sender, RoutedEventArgs e)
        {
            AlternarModo(editar: true);
        }

        private void BtnCancelarEdicao_Click(object sender, RoutedEventArgs e)
        {
            _novoCaminhoImagem = null;
            _novoCaminhoArquivo = null;
            PreencherCampos(); // Desfaz alterações na tela
            AlternarModo(editar: false);
        }

        private void BtnSalvarEdicao_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtBoxTitulo.Text))
            {
                MessageBox.Show("O título da receita é obrigatório.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Atualiza os dados do objeto Receita
            _receita.Titulo = TxtBoxTitulo.Text;

            if (CboCategorias.SelectedItem is CategoriaNodeViewModel categoriaSelecionada)
            {
                _receita.CategoriaId = categoriaSelecionada.Id;
                _receita.CategoriaNome = categoriaSelecionada.Nome;
            }

            // Captura o RichText (RTF) formatado
            var range = new TextRange(RtbTextoReceita.Document.ContentStart, RtbTextoReceita.Document.ContentEnd);
            using (var memoryStream = new MemoryStream())
            {
                range.Save(memoryStream, DataFormats.Rtf);
                _receita.TextoFormatado = System.Text.Encoding.UTF8.GetString(memoryStream.ToArray());
            }

            // Lógica para mover a nova imagem se ela foi alterada
            if (!string.IsNullOrEmpty(_novoCaminhoImagem))
            {
                string pastaImagens = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ImagensReceitas");
                Directory.CreateDirectory(pastaImagens);
                string destino = Path.Combine(pastaImagens, Path.GetFileName(_novoCaminhoImagem));

                try
                {
                    File.Copy(_novoCaminhoImagem, destino, true);
                    _receita.CaminhoImagemCapa = destino;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erro ao salvar nova imagem: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            // Lógica para mover o novo arquivo se ele foi alterado
            if (!string.IsNullOrEmpty(_novoCaminhoArquivo))
            {
                string pastaArquivos = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ArquivosApoio");
                Directory.CreateDirectory(pastaArquivos);
                string destino = Path.Combine(pastaArquivos, Path.GetFileName(_novoCaminhoArquivo));

                try
                {
                    File.Copy(_novoCaminhoArquivo, destino, true);
                    _receita.CaminhoArquivoApoio = destino;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erro ao salvar arquivo de apoio: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            // Chamada para salvar as alterações no banco de dados usando a ViewModel injetada de forma segura
            if (_mainViewModel != null)
            {
                _mainViewModel.SalvarAlteracoesReceita(_receita);
            }

            MessageBox.Show("Receita atualizada com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);

            AlternarModo(editar: false);
            PreencherCampos();
            this.DialogResult = true; // Avisa a MainWindow para atualizar os cards
        }

        private void BtnVoltar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // 4. TRATAMENTO DOS ANEXOS
        private void BtnAlterarImagem_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog op = new OpenFileDialog();
            op.Title = "Selecione a Imagem de Capa";
            op.Filter = "Imagens (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png";

            if (op.ShowDialog() == true)
            {
                _novoCaminhoImagem = op.FileName;

                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad; // Carrega temporariamente sem travar a imagem de origem
                    bitmap.UriSource = new Uri(_novoCaminhoImagem, UriKind.Absolute);
                    bitmap.EndInit();
                    ImgCapa.Source = bitmap;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Não foi possível carregar a imagem selecionada: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnBuscarArquivo_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog op = new OpenFileDialog();
            op.Title = "Selecione o Arquivo de Apoio";
            op.Filter = "Documentos (*.pdf;*.docx;*.txt)|*.pdf;*.docx;*.txt|Todos os Arquivos (*.*)|*.*";

            if (op.ShowDialog() == true)
            {
                _novoCaminhoArquivo = op.FileName;
                TxtCaminhoArquivo.Text = _novoCaminhoArquivo;
            }
        }

        private void BtnAbrirAnexo_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_receita.CaminhoArquivoApoio) && File.Exists(_receita.CaminhoArquivoApoio))
            {
                try
                {
                    // Abre o anexo no visualizador padrão do sistema (PDF, Word, etc.)
                    Process.Start(new ProcessStartInfo(_receita.CaminhoArquivoApoio) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Não foi possível abrir o arquivo: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("O arquivo de apoio não foi encontrado ou foi excluído.", "Erro", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // 5. SELETORES DE FORMATAÇÃO DO TEXTO
        private void CboFontes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CboFontes.SelectedItem != null && _modoEdicao)
            {
                RtbTextoReceita.Selection.ApplyPropertyValue(Inline.FontFamilyProperty, CboFontes.SelectedItem);
            }
        }

        private void CboTamanhoFonte_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CboTamanhoFonte.SelectedItem != null && _modoEdicao)
            {
                RtbTextoReceita.Selection.ApplyPropertyValue(Inline.FontSizeProperty, CboTamanhoFonte.SelectedItem);
            }
        }

        // 6. EVENTOS DE INTERAÇÃO COM A IMAGEM (TELA CHEIA / CONTEXT MENU)

        // Unificado: Trata o clique na imagem (abre menu context com clique esquerdo ou tela cheia com 2 cliques)
        private void ImgCapa_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                AbrirImagemFullscreen();
            }
            else if (ImgCapa.ContextMenu != null)
            {
                ImgCapa.ContextMenu.IsOpen = true;
            }
        }

        // Unificado: Evento do Menu de Contexto para Visualizar Foto
        private void MenuVisualizarFoto_Click(object sender, RoutedEventArgs e)
        {
            AbrirImagemFullscreen();
        }

        // Lógica para carregar a imagem no overlay e exibi-lo
        private void AbrirImagemFullscreen()
        {
            if (ImgCapa.Source != null)
            {
                ImgFullscreen.Source = ImgCapa.Source;
                GridFullscreen.Visibility = Visibility.Visible;
            }
        }

        // Fechar ao clicar no botão Fechar
        private void BtnFecharFullscreen_Click(object sender, RoutedEventArgs e)
        {
            GridFullscreen.Visibility = Visibility.Collapsed;
            ImgFullscreen.Source = null;
        }

        // Fechar ao clicar em qualquer parte escura do fundo
        private void GridFullscreen_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Garante que só fecha se clicar no fundo, não na imagem em si
            if (e.OriginalSource == GridFullscreen)
            {
                GridFullscreen.Visibility = Visibility.Collapsed;
                ImgFullscreen.Source = null;
            }
        }
    }
}