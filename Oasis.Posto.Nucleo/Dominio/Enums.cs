namespace Oasis.Posto.Dominio;

/// <summary>Nível de risco de calor, nas mesmas faixas da API (NOAA/NWS em °C).</summary>
public enum NivelRisco
{
    Normal = 0,
    Atencao = 1,
    Alerta = 2,
    Perigo = 3,
}

/// <summary>Como terminou uma ligação da busca ativa.</summary>
public enum ResultadoContato
{
    Atendeu,
    NaoAtendeu,
    Encaminhado,
}

/// <summary>Situação de uma pessoa na lista do dia.</summary>
public enum SituacaoNaLista
{
    LigarHoje,
    JaContatada,
    Opcional,
}

public static class Textos
{
    public static string Nome(this NivelRisco nivel) => nivel switch
    {
        NivelRisco.Atencao => "Atenção",
        NivelRisco.Alerta => "Alerta",
        NivelRisco.Perigo => "Perigo",
        _ => "Normal",
    };

    public static string Nome(this ResultadoContato resultado) => resultado switch
    {
        ResultadoContato.Atendeu => "Atendeu e está bem",
        ResultadoContato.NaoAtendeu => "Não atendeu",
        _ => "Encaminhado (visita, UBS ou SAMU)",
    };

    public static string Nome(this SituacaoNaLista situacao) => situacao switch
    {
        SituacaoNaLista.LigarHoje => "Ligar hoje",
        SituacaoNaLista.JaContatada => "Já contatada",
        _ => "Opcional",
    };
}
