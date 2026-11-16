namespace Oasis.Posto.Dominio;

/// <summary>Uma linha da lista do dia: quem é, quanto pesa e por quê.</summary>
public sealed record ItemBuscaAtiva(PessoaAcompanhada Pessoa, int Pontuacao, SituacaoNaLista Situacao, string Motivo);

/// <summary>
/// Regras da busca ativa: em dia de calor, o posto liga primeiro para quem corre mais risco.
/// Pontuação (0 a 7): +2 mora sozinha, +2 condição de atenção, +2 com 80 anos ou mais
/// (+1 de 65 a 79), +1 sem contato com sucesso nas últimas 24 horas.
/// Cada parcela é um <see cref="CriterioDePontuacao"/>; a pontuação é a soma de todos.
/// Contato obrigatório do dia: Perigo, todos; Alerta, pontuação 3 ou mais; Atenção, 5 ou mais; Normal, ninguém.
/// </summary>
public static class BuscaAtiva
{
    /// <summary>Os critérios que formam a pontuação. A lista é da classe base; cada item calcula do seu jeito.</summary>
    public static IReadOnlyList<CriterioDePontuacao> Criterios { get; } =
    [
        new CriterioMoraSozinha(),
        new CriterioCondicaoDeAtencao(),
        new CriterioIdade(),
        new CriterioSemContatoRecente(),
    ];

    /// <summary>Maior pontuação possível: a soma do máximo de cada critério.</summary>
    public static int PontuacaoMaxima => Criterios.Sum(c => c.PontosMaximos);

    public static int Pontuar(PessoaAcompanhada pessoa, DateTime agora) =>
        Criterios.Sum(criterio => criterio.Pontuar(pessoa, agora));

    public static int PontuacaoMinima(NivelRisco nivel) => nivel switch
    {
        NivelRisco.Perigo => 0,
        NivelRisco.Alerta => 3,
        NivelRisco.Atencao => 5,
        _ => int.MaxValue,
    };

    /// <summary>O nível do dia é o pior entre as estações; sem leitura nenhuma, Normal.</summary>
    public static NivelRisco NivelDoDia(IEnumerable<NivelRisco?> niveisDasEstacoes) =>
        niveisDasEstacoes.Where(n => n.HasValue).Select(n => n!.Value).DefaultIfEmpty(NivelRisco.Normal).Max();

    public static bool ContatadaHoje(PessoaAcompanhada pessoa, DateTime agora) =>
        pessoa.Contatos.Any(c => c.Sucesso && c.DataHora.Date == agora.Date);

    public static IReadOnlyList<ItemBuscaAtiva> MontarLista(IEnumerable<PessoaAcompanhada> pessoas, NivelRisco nivel, DateTime agora)
    {
        var minimo = PontuacaoMinima(nivel);
        return pessoas
            .Select(p =>
            {
                var pontuacao = Pontuar(p, agora);
                var situacao = ContatadaHoje(p, agora)
                    ? SituacaoNaLista.JaContatada
                    : pontuacao >= minimo ? SituacaoNaLista.LigarHoje : SituacaoNaLista.Opcional;
                return new ItemBuscaAtiva(p, pontuacao, situacao, Motivo(p, agora));
            })
            .OrderBy(i => i.Situacao) // LigarHoje, depois JaContatada, depois Opcional
            .ThenByDescending(i => i.Pontuacao)
            .ThenBy(i => i.Pessoa.Apelido, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public static string Motivo(PessoaAcompanhada pessoa, DateTime agora)
    {
        var partes = new List<string>();
        if (pessoa.MoraSozinha) partes.Add("mora sozinha");
        if (pessoa.CondicaoDeAtencao) partes.Add("condição de atenção");
        partes.Add($"{pessoa.Idade} anos");

        var ultimo = pessoa.UltimoContato;
        if (ultimo is null)
            partes.Add("nunca contatada");
        else if (!ultimo.Sucesso && ultimo.DataHora.Date == agora.Date)
            partes.Add($"não atendeu às {ultimo.DataHora:HH:mm}, tente de novo");
        else if (pessoa.UltimoContatoComSucesso is { } sucesso)
            partes.Add(DescreverTempo(agora - sucesso));
        else
            partes.Add("nunca atendeu");

        return string.Join(" · ", partes);
    }

    private static string DescreverTempo(TimeSpan tempo) => tempo.TotalHours switch
    {
        < 1 => "contatada há menos de 1 hora",
        < 24 => $"contatada há {(int)tempo.TotalHours} h",
        < 48 => "sem contato desde ontem",
        _ => $"sem contato há {(int)tempo.TotalDays} dias",
    };
}
