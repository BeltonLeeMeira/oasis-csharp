using System.Text.RegularExpressions;

namespace Oasis.Posto.Dominio;

/// <summary>Regras de preenchimento do cadastro de uma pessoa acompanhada.</summary>
public static partial class ValidadorPessoa
{
    public const int IdadeMinima = 60;
    public const int IdadeMaxima = 120;
    public const int TamanhoMaximoObservacao = 120;

    public const string CampoApelido = "apelido";
    public const string CampoIdade = "idade";
    public const string CampoTelefone = "telefone";
    public const string CampoObservacao = "observacao";

    [GeneratedRegex(@"^[\p{L} '\-]{2,40}$")]
    private static partial Regex PadraoApelido();

    // Celular (11) 91234-5678 ou fixo (11) 1234-5678.
    [GeneratedRegex(@"^\(\d{2}\) 9?\d{4}-\d{4}$")]
    private static partial Regex PadraoTelefone();

    public static Dictionary<string, string> Validar(string apelido, int idade, string telefone, string? observacao)
    {
        var erros = new Dictionary<string, string>();
        var apelidoLimpo = apelido.Trim();

        if (apelidoLimpo.Length == 0)
            erros[CampoApelido] = "Informe como a pessoa gosta de ser chamada.";
        else if (!PadraoApelido().IsMatch(apelidoLimpo))
            erros[CampoApelido] = "Use de 2 a 40 letras (sem números).";

        if (idade < IdadeMinima || idade > IdadeMaxima)
            erros[CampoIdade] = $"A busca ativa é para pessoas de {IdadeMinima} a {IdadeMaxima} anos.";

        if (!PadraoTelefone().IsMatch(telefone.Trim()))
            erros[CampoTelefone] = "Use o formato (11) 91234-5678.";

        if (observacao is not null && observacao.Trim().Length > TamanhoMaximoObservacao)
            erros[CampoObservacao] = $"Use no máximo {TamanhoMaximoObservacao} letras.";

        return erros;
    }
}
