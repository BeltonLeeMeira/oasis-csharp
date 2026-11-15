using System.ComponentModel;
using Oasis.Posto.Dominio;

namespace Oasis.Posto.App;

/// <summary>Cadastro e edição de uma pessoa acompanhada, com os erros de validação ao lado de cada campo.</summary>
internal sealed class FormPessoa : Form
{
    private readonly PessoaAcompanhada? _existente;

    private readonly TextBox _apelido = new() { Width = 360, MaxLength = 40 };
    private readonly NumericUpDown _idade = new() { Minimum = ValidadorPessoa.IdadeMinima, Maximum = ValidadorPessoa.IdadeMaxima, Value = 75, Width = 100 };
    private readonly CheckBox _moraSozinha = new() { Text = "Mora sozinha", AutoSize = true };
    private readonly CheckBox _condicao = new() { Text = "Tem condição de atenção no calor (indicada pela equipe de saúde)", AutoSize = true };
    private readonly TextBox _telefone = new() { Width = 220, PlaceholderText = "(11) 91234-5678" };
    private readonly TextBox _observacao = new() { Width = 360, MaxLength = ValidadorPessoa.TamanhoMaximoObservacao, PlaceholderText = "Ex.: prefere ligação depois das 10h" };
    private readonly ErrorProvider _erros = new() { BlinkStyle = ErrorBlinkStyle.NeverBlink };
    private readonly Button _salvar = Estilo.BotaoPrincipal("Salvar");
    private readonly Button _cancelar = Estilo.BotaoSecundario("Cancelar");

    public FormPessoa(PessoaAcompanhada? existente = null)
    {
        _existente = existente;
        MontarTela();
        if (existente is not null)
        {
            _apelido.Text = existente.Apelido;
            _idade.Value = existente.Idade;
            _moraSozinha.Checked = existente.MoraSozinha;
            _condicao.Checked = existente.CondicaoDeAtencao;
            _telefone.Text = existente.Telefone;
            _observacao.Text = existente.Observacao ?? "";
        }
    }

    /// <summary>Pessoa criada (no cadastro novo). Na edição, a própria pessoa recebida é atualizada.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public PessoaAcompanhada? Resultado { get; private set; }

    private void MontarTela()
    {
        Text = _existente is null ? "Nova pessoa acompanhada" : "Editar pessoa acompanhada";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Estilo.Fundo;
        Font = Estilo.Normal;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(24);

        var painel = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, WrapContents = false };
        painel.Controls.Add(Estilo.Rotulo("Como gosta de ser chamada"));
        painel.Controls.Add(_apelido);
        painel.Controls.Add(Estilo.Rotulo("Idade"));
        painel.Controls.Add(_idade);
        painel.Controls.Add(_moraSozinha);
        painel.Controls.Add(_condicao);
        painel.Controls.Add(Estilo.Rotulo("Telefone para a busca ativa"));
        painel.Controls.Add(_telefone);
        painel.Controls.Add(Estilo.Rotulo("Observação (opcional)"));
        painel.Controls.Add(_observacao);
        painel.Controls.Add(Estilo.Rotulo("Protótipo: use só dados fictícios.", cor: Estilo.Suave));

        var botoes = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true };
        botoes.Controls.Add(_salvar);
        botoes.Controls.Add(_cancelar);
        painel.Controls.Add(botoes);
        Controls.Add(painel);

        AcceptButton = _salvar;
        CancelButton = _cancelar;
        _cancelar.DialogResult = DialogResult.Cancel;
        _salvar.Click += (_, _) => Salvar();
    }

    private void Salvar()
    {
        foreach (var campo in new Control[] { _apelido, _idade, _telefone, _observacao })
            _erros.SetError(campo, "");

        try
        {
            var idade = (int)_idade.Value;
            if (_existente is null)
            {
                Resultado = PessoaAcompanhada.Criar(_apelido.Text, idade, _moraSozinha.Checked, _condicao.Checked, _telefone.Text, _observacao.Text);
            }
            else
            {
                _existente.Atualizar(_apelido.Text, idade, _moraSozinha.Checked, _condicao.Checked, _telefone.Text, _observacao.Text);
                Resultado = _existente;
            }
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (ErroDeValidacao erro)
        {
            MostrarErro(erro, ValidadorPessoa.CampoApelido, _apelido);
            MostrarErro(erro, ValidadorPessoa.CampoIdade, _idade);
            MostrarErro(erro, ValidadorPessoa.CampoTelefone, _telefone);
            MostrarErro(erro, ValidadorPessoa.CampoObservacao, _observacao);
        }
    }

    private void MostrarErro(ErroDeValidacao erro, string campo, Control controle)
    {
        if (erro.Erros.TryGetValue(campo, out var mensagem))
            _erros.SetError(controle, mensagem);
    }
}
