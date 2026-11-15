using System.ComponentModel;
using Oasis.Posto.Dados;

namespace Oasis.Posto.App;

/// <summary>
/// Primeira tela: cria a senha do posto (primeiro uso) ou abre o arquivo criptografado.
/// Depois de 5 senhas erradas, espera 30 segundos antes de deixar tentar de novo.
/// </summary>
internal sealed class FormSenha : Form
{
    private const int TentativasAntesDeEsperar = 5;
    private readonly string _caminho;
    private readonly bool _primeiroUso;
    private int _erros;

    private readonly TextBox _senha = new() { UseSystemPasswordChar = true, Width = 340, Font = Estilo.Normal };
    private readonly TextBox _confirmacao = new() { UseSystemPasswordChar = true, Width = 340, Font = Estilo.Normal };
    private readonly Label _mensagem = Estilo.Rotulo("", cor: Estilo.Erro);
    private readonly Button _entrar = Estilo.BotaoPrincipal("Entrar");

    public FormSenha(string caminho)
    {
        _caminho = caminho;
        _primeiroUso = !CofreDeArquivo.Existe(caminho);
        MontarTela();
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public SessaoDoPosto? Sessao { get; private set; }

    private void MontarTela()
    {
        Text = "Oásis · Painel do Posto";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Estilo.Fundo;
        Font = Estilo.Normal;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(24);

        var painel = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, WrapContents = false };
        painel.Controls.Add(Estilo.Rotulo("Painel do Posto", Estilo.Titulo, Estilo.Primaria));
        painel.Controls.Add(Estilo.Rotulo(
            _primeiroUso
                ? "Primeiro uso: crie a senha do posto (mínimo de 8 caracteres)." + Environment.NewLine + "Ela protege os dados das pessoas acompanhadas e não pode ser recuperada."
                : "Digite a senha do posto para abrir os dados.",
            cor: Estilo.Suave));
        painel.Controls.Add(Estilo.Rotulo("Senha"));
        painel.Controls.Add(_senha);
        if (_primeiroUso)
        {
            painel.Controls.Add(Estilo.Rotulo("Repita a senha"));
            painel.Controls.Add(_confirmacao);
        }
        painel.Controls.Add(_mensagem);
        painel.Controls.Add(_entrar);
        Controls.Add(painel);

        AcceptButton = _entrar;
        _entrar.Click += async (_, _) => await EntrarAsync();
    }

    private async Task EntrarAsync()
    {
        _mensagem.Text = "";
        var senha = _senha.Text;
        if (_primeiroUso)
        {
            if (senha.Length < CofreDeArquivo.TamanhoMinimoSenha)
            {
                _mensagem.Text = $"Use pelo menos {CofreDeArquivo.TamanhoMinimoSenha} caracteres.";
                return;
            }
            if (senha != _confirmacao.Text)
            {
                _mensagem.Text = "As duas senhas não são iguais.";
                return;
            }
        }

        _entrar.Enabled = false;
        _entrar.Text = "Abrindo...";
        try
        {
            // A derivação da chave é lenta de propósito (dificulta adivinhar a senha): roda fora da tela.
            Sessao = _primeiroUso
                ? await Task.Run(() => SessaoDoPosto.CriarNova(_caminho, senha))
                : await Task.Run(() => SessaoDoPosto.Abrir(_caminho, senha));
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (SenhaIncorretaException)
        {
            _erros++;
            _senha.Clear();
            if (_erros >= TentativasAntesDeEsperar)
            {
                _mensagem.Text = "Muitas tentativas. Aguarde 30 segundos.";
                await Task.Delay(TimeSpan.FromSeconds(30));
                _erros = 0;
                _mensagem.Text = "Pode tentar de novo.";
            }
            else
            {
                _mensagem.Text = "Senha incorreta.";
            }
        }
        catch (Exception erro) when (erro is IOException or UnauthorizedAccessException)
        {
            _mensagem.Text = "Não foi possível abrir o arquivo do posto: " + erro.Message;
        }
        finally
        {
            _entrar.Enabled = true;
            _entrar.Text = "Entrar";
        }
    }
}
