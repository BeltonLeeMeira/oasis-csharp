using System.ComponentModel;
using Oasis.Posto.Dominio;

namespace Oasis.Posto.App;

/// <summary>Registra o resultado de uma ligação da busca ativa.</summary>
internal sealed class FormContato : Form
{
    private readonly RadioButton _atendeu = new() { Text = ResultadoContato.Atendeu.Nome(), AutoSize = true, Checked = true };
    private readonly RadioButton _naoAtendeu = new() { Text = ResultadoContato.NaoAtendeu.Nome(), AutoSize = true };
    private readonly RadioButton _encaminhado = new() { Text = ResultadoContato.Encaminhado.Nome(), AutoSize = true };
    private readonly TextBox _nota = new() { Width = 380, MaxLength = ValidadorPessoa.TamanhoMaximoObservacao, PlaceholderText = "Opcional. Ex.: pediu para ligar à tarde" };
    private readonly Button _registrar = Estilo.BotaoPrincipal("Registrar");
    private readonly Button _cancelar = Estilo.BotaoSecundario("Cancelar");

    public FormContato(PessoaAcompanhada pessoa)
    {
        Text = "Registrar ligação";
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
        painel.Controls.Add(Estilo.Rotulo(pessoa.Apelido, Estilo.Destaque, Estilo.Primaria));
        painel.Controls.Add(Estilo.Rotulo($"Telefone: {pessoa.Telefone}"));
        painel.Controls.Add(Estilo.Rotulo("Como foi a ligação?", Estilo.Negrito));
        painel.Controls.Add(_atendeu);
        painel.Controls.Add(_naoAtendeu);
        painel.Controls.Add(_encaminhado);
        painel.Controls.Add(Estilo.Rotulo("Anotação"));
        painel.Controls.Add(_nota);
        var botoes = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true };
        botoes.Controls.Add(_registrar);
        botoes.Controls.Add(_cancelar);
        painel.Controls.Add(botoes);
        Controls.Add(painel);

        AcceptButton = _registrar;
        CancelButton = _cancelar;
        _cancelar.DialogResult = DialogResult.Cancel;
        _registrar.DialogResult = DialogResult.OK;
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ResultadoContato Resultado =>
        _naoAtendeu.Checked ? ResultadoContato.NaoAtendeu : _encaminhado.Checked ? ResultadoContato.Encaminhado : ResultadoContato.Atendeu;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Nota => _nota.Text;
}
