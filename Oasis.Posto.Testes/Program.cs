// Testes do núcleo do Painel do Posto, sem pacotes externos.
// Rodar: dotnet run --project Oasis.Posto.Testes

using Oasis.Posto.Dominio;

var falhas = 0;
var total = 0;

void Teste(string nome, Action corpo)
{
    total++;
    try
    {
        corpo();
        Console.WriteLine($"  ok    {nome}");
    }
    catch (Exception e)
    {
        falhas++;
        Console.WriteLine($"  FALHA {nome}: {e.Message}");
    }
}

void Confirmar(bool condicao, string mensagem)
{
    if (!condicao) throw new InvalidOperationException(mensagem);
}

void Lanca<TExcecao>(Action acao) where TExcecao : Exception
{
    try
    {
        acao();
    }
    catch (TExcecao)
    {
        return;
    }
    throw new InvalidOperationException($"esperava {typeof(TExcecao).Name}");
}

var agora = new DateTime(2026, 11, 12, 14, 0, 0);
PessoaAcompanhada Pessoa(string apelido, int idade, bool sozinha, bool condicao) =>
    PessoaAcompanhada.Criar(apelido, idade, sozinha, condicao, "(11) 91234-5678", null);

Console.WriteLine("Validação");
Teste("cadastro válido passa", () => Confirmar(ValidadorPessoa.Validar("Dona Cida", 82, "(11) 91234-5678", null).Count == 0, "tinha erros"));
Teste("aponta cada campo inválido", () =>
{
    var erros = ValidadorPessoa.Validar("C3", 45, "11912345678", new string('x', 130));
    Confirmar(erros.Keys.Order().SequenceEqual(["apelido", "idade", "observacao", "telefone"]), string.Join(",", erros.Keys));
});
Teste("Criar com dado inválido lança ErroDeValidacao", () => Lanca<ErroDeValidacao>(() => Pessoa("", 82, true, false)));

Console.WriteLine("Pontuação e lista do dia");
Teste("pontuação máxima é 7", () => Confirmar(BuscaAtiva.Pontuar(Pessoa("Seu Zé", 85, true, true), agora) == 7, "esperava 7"));
Teste("65 a 79 anos soma 1", () => Confirmar(BuscaAtiva.Pontuar(Pessoa("Dona Ana", 70, false, false), agora) == 2, "esperava 2 (idade + sem contato)"));
Teste("contato nas últimas 24 h tira 1 ponto", () =>
{
    var p = Pessoa("Dona Ana", 70, false, false);
    p.RegistrarContato(ResultadoContato.Atendeu, agora.AddHours(-3), null);
    Confirmar(BuscaAtiva.Pontuar(p, agora) == 1, "esperava 1");
});
Teste("nível do dia é o pior entre as estações", () =>
    Confirmar(BuscaAtiva.NivelDoDia([NivelRisco.Atencao, null, NivelRisco.Perigo]) == NivelRisco.Perigo, "esperava Perigo"));
Teste("sem leituras, o nível do dia é Normal", () => Confirmar(BuscaAtiva.NivelDoDia([]) == NivelRisco.Normal, "esperava Normal"));
Teste("no Alerta, liga para quem tem 3 pontos ou mais, na ordem da pontuação", () =>
{
    var lista = BuscaAtiva.MontarLista([Pessoa("Dona Ana", 66, false, false), Pessoa("Seu Zé", 85, true, true), Pessoa("Dona Lia", 81, true, false)], NivelRisco.Alerta, agora);
    Confirmar(lista.Select(i => i.Pessoa.Apelido).SequenceEqual(["Seu Zé", "Dona Lia", "Dona Ana"]), "ordem errada");
    Confirmar(lista[0].Situacao == SituacaoNaLista.LigarHoje && lista[2].Situacao == SituacaoNaLista.Opcional, "situações erradas");
});
Teste("no Perigo, todos entram em Ligar hoje", () =>
    Confirmar(BuscaAtiva.MontarLista([Pessoa("Dona Ana", 66, false, false)], NivelRisco.Perigo, agora)[0].Situacao == SituacaoNaLista.LigarHoje, "esperava Ligar hoje"));
Teste("não atendeu continua em Ligar hoje, com o motivo", () =>
{
    var p = Pessoa("Seu Zé", 85, true, true);
    p.RegistrarContato(ResultadoContato.NaoAtendeu, agora.AddHours(-1), null);
    var item = BuscaAtiva.MontarLista([p], NivelRisco.Alerta, agora)[0];
    Confirmar(item.Situacao == SituacaoNaLista.LigarHoje && item.Motivo.Contains("não atendeu às 13:00"), item.Motivo);
});
Teste("atendeu hoje vira Já contatada", () =>
{
    var p = Pessoa("Seu Zé", 85, true, true);
    p.RegistrarContato(ResultadoContato.Atendeu, agora.AddHours(-1), "está bem");
    Confirmar(BuscaAtiva.MontarLista([p], NivelRisco.Perigo, agora)[0].Situacao == SituacaoNaLista.JaContatada, "esperava Já contatada");
});

Console.WriteLine();
Console.WriteLine(falhas == 0 ? $"Todos os {total} testes passaram." : $"{falhas} de {total} testes falharam.");
return falhas == 0 ? 0 : 1;
