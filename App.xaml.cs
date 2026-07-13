using System.Windows;
using ReceitasAtelie.Data;

namespace ReceitasAtelie;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Garante a criação das pastas e das tabelas do banco no início do programa
        DatabaseHelper.InicializarBanco();
    }
}