using Oasis.Posto.Dados;

namespace Oasis.Posto.App;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        using var login = new FormSenha(SessaoDoPosto.CaminhoPadrao);
        if (login.ShowDialog() != DialogResult.OK || login.Sessao is null)
            return;

        Application.Run(new FormPrincipal(login.Sessao));
    }
}
