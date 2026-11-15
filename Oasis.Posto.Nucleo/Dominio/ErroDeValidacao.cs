namespace Oasis.Posto.Dominio;

/// <summary>Um ou mais campos inválidos. A tela mostra cada mensagem ao lado do campo.</summary>
public sealed class ErroDeValidacao : Exception
{
    public ErroDeValidacao(IReadOnlyDictionary<string, string> erros)
        : base(string.Join(" ", erros.Values))
    {
        Erros = erros;
    }

    public IReadOnlyDictionary<string, string> Erros { get; }
}
