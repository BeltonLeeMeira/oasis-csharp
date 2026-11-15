namespace Oasis.Posto.Dominio;

/// <summary>Uma ligação feita pelo posto.</summary>
public sealed record RegistroContato(DateTime DataHora, ResultadoContato Resultado, string? Nota)
{
    /// <summary>Atendeu ou foi encaminhada: a pessoa foi alcançada. "Não atendeu" pede nova tentativa.</summary>
    public bool Sucesso => Resultado != ResultadoContato.NaoAtendeu;
}

/// <summary>
/// Idoso acompanhado pelo posto nos dias de calor. Os dados ficam só no computador do posto,
/// criptografados (ver Dados/CofreDeArquivo.cs), e nunca vão para a API.
/// </summary>
public sealed class PessoaAcompanhada
{
    private readonly List<RegistroContato> _contatos = [];

    private PessoaAcompanhada(Guid id, string apelido, int idade, bool moraSozinha, bool condicaoDeAtencao, string telefone, string? observacao)
    {
        Id = id;
        Apelido = apelido;
        Idade = idade;
        MoraSozinha = moraSozinha;
        CondicaoDeAtencao = condicaoDeAtencao;
        Telefone = telefone;
        Observacao = observacao;
    }

    public Guid Id { get; }
    public string Apelido { get; private set; }
    public int Idade { get; private set; }
    public bool MoraSozinha { get; private set; }

    /// <summary>Marcada pela equipe de saúde quando há motivo de atenção extra no calor. Não guardamos qual.</summary>
    public bool CondicaoDeAtencao { get; private set; }

    public string Telefone { get; private set; }
    public string? Observacao { get; private set; }
    public IReadOnlyList<RegistroContato> Contatos => _contatos;

    public RegistroContato? UltimoContato => _contatos.Count == 0 ? null : _contatos.MaxBy(c => c.DataHora);

    public DateTime? UltimoContatoComSucesso =>
        _contatos.Where(c => c.Sucesso).Select(c => (DateTime?)c.DataHora).DefaultIfEmpty(null).Max();

    public static PessoaAcompanhada Criar(string apelido, int idade, bool moraSozinha, bool condicaoDeAtencao, string telefone, string? observacao)
    {
        Garantir(apelido, idade, telefone, observacao);
        return new PessoaAcompanhada(Guid.NewGuid(), apelido.Trim(), idade, moraSozinha, condicaoDeAtencao, telefone.Trim(), Limpar(observacao));
    }

    /// <summary>Reconstrói uma pessoa lida do arquivo do posto.</summary>
    public static PessoaAcompanhada Restaurar(
        Guid id, string apelido, int idade, bool moraSozinha, bool condicaoDeAtencao, string telefone, string? observacao, IEnumerable<RegistroContato> contatos)
    {
        var pessoa = new PessoaAcompanhada(id, apelido, idade, moraSozinha, condicaoDeAtencao, telefone, observacao);
        pessoa._contatos.AddRange(contatos.OrderBy(c => c.DataHora));
        return pessoa;
    }

    public void Atualizar(string apelido, int idade, bool moraSozinha, bool condicaoDeAtencao, string telefone, string? observacao)
    {
        Garantir(apelido, idade, telefone, observacao);
        Apelido = apelido.Trim();
        Idade = idade;
        MoraSozinha = moraSozinha;
        CondicaoDeAtencao = condicaoDeAtencao;
        Telefone = telefone.Trim();
        Observacao = Limpar(observacao);
    }

    public RegistroContato RegistrarContato(ResultadoContato resultado, DateTime quando, string? nota)
    {
        var limpa = Limpar(nota);
        if (limpa is { Length: > ValidadorPessoa.TamanhoMaximoObservacao })
        {
            throw new ErroDeValidacao(new Dictionary<string, string> { ["nota"] = $"Use no máximo {ValidadorPessoa.TamanhoMaximoObservacao} letras." });
        }
        var registro = new RegistroContato(quando, resultado, limpa);
        _contatos.Add(registro);
        return registro;
    }

    private static void Garantir(string apelido, int idade, string telefone, string? observacao)
    {
        var erros = ValidadorPessoa.Validar(apelido, idade, telefone, observacao);
        if (erros.Count > 0) throw new ErroDeValidacao(erros);
    }

    private static string? Limpar(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
}
