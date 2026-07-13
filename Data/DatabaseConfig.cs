using System.IO;

namespace ReceitasAtelie.Data;

public static class DatabaseConfig
{
    public static string GetConnectionString()
    {
        // Pega o caminho da pasta "Documentos" do usuário logado no Windows
        string meusDocumentos = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        // Combina os caminhos para criar: Documents\MemoriasAtelie\BancoDados
        string pastaBanco = Path.Combine(meusDocumentos, "MemoriasAtelie", "BancoDados");

        // Se a pasta não existir no PC do usuário, o programa cria ela agora
        if (!Directory.Exists(pastaBanco))
        {
            Directory.CreateDirectory(pastaBanco);
        }

        string caminhoCompletoBanco = Path.Combine(pastaBanco, "receitas.db");

        // Retorna a string de conexão que o SQLite precisa
        return $"Data Source={caminhoCompletoBanco}";
    }
}