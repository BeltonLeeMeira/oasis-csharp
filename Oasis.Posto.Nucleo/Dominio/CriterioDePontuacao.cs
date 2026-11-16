namespace Oasis.Posto.Dominio;

/// <summary>
/// Um critério da pontuação da busca ativa. É a classe base: cada subclasse responde, do seu jeito,
/// quantos pontos uma pessoa soma. A <see cref="BuscaAtiva"/> percorre a lista de critérios sem saber
/// qual é qual (polimorfismo), então incluir um critério novo não muda o cálculo.
/// </summary>
public abstract class CriterioDePontuacao
{
    /// <summary>Nome curto do critério, para telas e relatórios.</summary>
    public abstract string Nome { get; }

    /// <summary>Maior pontuação que este critério pode dar.</summary>
    public abstract int PontosMaximos { get; }

    /// <summary>Pontos que a pessoa soma neste critério, de 0 a <see cref="PontosMaximos"/>.</summary>
    public abstract int Pontuar(PessoaAcompanhada pessoa, DateTime agora);

    /// <summary>Verdadeiro quando a pessoa soma algum ponto neste critério.</summary>
    public bool SeAplica(PessoaAcompanhada pessoa, DateTime agora) => Pontuar(pessoa, agora) > 0;

    public override string ToString() => $"{Nome} (até {PontosMaximos})";
}

/// <summary>
/// Critério de sim ou não: quem atende soma todos os pontos, quem não atende soma zero.
/// As subclasses só dizem quando a pessoa atende.
/// </summary>
public abstract class CriterioSimOuNao : CriterioDePontuacao
{
    protected abstract bool Atende(PessoaAcompanhada pessoa, DateTime agora);

    public sealed override int Pontuar(PessoaAcompanhada pessoa, DateTime agora) =>
        Atende(pessoa, agora) ? PontosMaximos : 0;
}

/// <summary>Quem mora sozinha não tem ninguém por perto para perceber que passou mal.</summary>
public sealed class CriterioMoraSozinha : CriterioSimOuNao
{
    public override string Nome => "Mora sozinha";
    public override int PontosMaximos => 2;
    protected override bool Atende(PessoaAcompanhada pessoa, DateTime agora) => pessoa.MoraSozinha;
}

/// <summary>Condição de atenção no calor, indicada pela equipe de saúde.</summary>
public sealed class CriterioCondicaoDeAtencao : CriterioSimOuNao
{
    public override string Nome => "Condição de atenção";
    public override int PontosMaximos => 2;
    protected override bool Atende(PessoaAcompanhada pessoa, DateTime agora) => pessoa.CondicaoDeAtencao;
}

/// <summary>Sem ligação atendida nas últimas 24 horas (ou nunca contatada).</summary>
public sealed class CriterioSemContatoRecente : CriterioSimOuNao
{
    private static readonly TimeSpan Limite = TimeSpan.FromHours(24);

    public override string Nome => "Sem contato recente";
    public override int PontosMaximos => 1;
    protected override bool Atende(PessoaAcompanhada pessoa, DateTime agora) =>
        pessoa.UltimoContatoComSucesso is not { } ultimo || agora - ultimo > Limite;
}

/// <summary>Idade: não é sim ou não, tem duas faixas. Por isso herda direto da classe base.</summary>
public sealed class CriterioIdade : CriterioDePontuacao
{
    public override string Nome => "Idade";
    public override int PontosMaximos => 2;

    public override int Pontuar(PessoaAcompanhada pessoa, DateTime agora) => pessoa.Idade switch
    {
        >= 80 => 2,
        >= 65 => 1,
        _ => 0,
    };
}
