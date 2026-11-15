using System.Text.Json;
using Oasis.Posto.Dominio;

namespace Oasis.Posto.Dados;

/// <summary>Formato salvo no arquivo (dentro do cofre). Separado do domínio para o domínio continuar encapsulado.</summary>
internal sealed class ArquivoDoPosto
{
    public int Versao { get; set; } = 1;
    public string UrlApi { get; set; } = "http://localhost:8000";
    public string ChavePosto { get; set; } = "";
    public List<PessoaSalva> Pessoas { get; set; } = [];
}

internal sealed class PessoaSalva
{
    public Guid Id { get; set; }
    public string Apelido { get; set; } = "";
    public int Idade { get; set; }
    public bool MoraSozinha { get; set; }
    public bool CondicaoDeAtencao { get; set; }
    public string Telefone { get; set; } = "";
    public string? Observacao { get; set; }
    public List<ContatoSalvo> Contatos { get; set; } = [];
}

internal sealed class ContatoSalvo
{
    public DateTime DataHora { get; set; }
    public ResultadoContato Resultado { get; set; }
    public string? Nota { get; set; }
}

/// <summary>
/// Tudo o que o posto guarda neste computador: pessoas acompanhadas, endereço da API e chave do posto.
/// Persistência básica: um arquivo JSON criptografado, aberto com a senha do posto.
/// </summary>
public sealed class SessaoDoPosto
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = false };
    private readonly CofreDeArquivo _cofre;
    private readonly List<PessoaAcompanhada> _pessoas;

    private SessaoDoPosto(CofreDeArquivo cofre, string urlApi, string chavePosto, List<PessoaAcompanhada> pessoas)
    {
        _cofre = cofre;
        UrlApi = urlApi;
        ChavePosto = chavePosto;
        _pessoas = pessoas;
    }

    public string UrlApi { get; private set; }
    public string ChavePosto { get; private set; }
    public IReadOnlyList<PessoaAcompanhada> Pessoas => _pessoas;

    public static string CaminhoPadrao =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Oasis", "posto.dados");

    public static SessaoDoPosto CriarNova(string caminho, string senha)
    {
        var sessao = new SessaoDoPosto(CofreDeArquivo.Criar(caminho, senha), "http://localhost:8000", "", []);
        sessao.Salvar();
        return sessao;
    }

    public static SessaoDoPosto Abrir(string caminho, string senha)
    {
        var (cofre, conteudo) = CofreDeArquivo.Abrir(caminho, senha);
        var arquivo = JsonSerializer.Deserialize<ArquivoDoPosto>(conteudo, Json) ?? new ArquivoDoPosto();
        var pessoas = arquivo.Pessoas
            .Select(p => PessoaAcompanhada.Restaurar(
                p.Id, p.Apelido, p.Idade, p.MoraSozinha, p.CondicaoDeAtencao, p.Telefone, p.Observacao,
                p.Contatos.Select(c => new RegistroContato(c.DataHora, c.Resultado, c.Nota))))
            .ToList();
        return new SessaoDoPosto(cofre, arquivo.UrlApi, arquivo.ChavePosto, pessoas);
    }

    public void Adicionar(PessoaAcompanhada pessoa)
    {
        _pessoas.Add(pessoa);
        Salvar();
    }

    public void Remover(Guid id)
    {
        _pessoas.RemoveAll(p => p.Id == id);
        Salvar();
    }

    public void AlterarConexao(string urlApi, string chavePosto)
    {
        if (!Uri.TryCreate(urlApi.Trim(), UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            throw new ErroDeValidacao(new Dictionary<string, string> { ["url"] = "Use um endereço como http://192.168.0.10:8000" });
        UrlApi = urlApi.Trim().TrimEnd('/');
        ChavePosto = chavePosto.Trim();
        Salvar();
    }

    public void Salvar()
    {
        var arquivo = new ArquivoDoPosto
        {
            UrlApi = UrlApi,
            ChavePosto = ChavePosto,
            Pessoas = _pessoas.Select(p => new PessoaSalva
            {
                Id = p.Id,
                Apelido = p.Apelido,
                Idade = p.Idade,
                MoraSozinha = p.MoraSozinha,
                CondicaoDeAtencao = p.CondicaoDeAtencao,
                Telefone = p.Telefone,
                Observacao = p.Observacao,
                Contatos = p.Contatos.Select(c => new ContatoSalvo { DataHora = c.DataHora, Resultado = c.Resultado, Nota = c.Nota }).ToList(),
            }).ToList(),
        };
        _cofre.Gravar(JsonSerializer.SerializeToUtf8Bytes(arquivo, Json));
    }
}
