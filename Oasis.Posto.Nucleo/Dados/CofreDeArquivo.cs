using System.Security.Cryptography;
using System.Text;

namespace Oasis.Posto.Dados;

public sealed class SenhaIncorretaException() : Exception("Senha incorreta ou arquivo do posto alterado.");

/// <summary>
/// Guarda bytes num arquivo criptografado com AES-256-GCM. A chave sai da senha do posto (PBKDF2-SHA256)
/// e nunca é gravada. O GCM também detecta qualquer alteração no arquivo.
/// Formato: "OASIS1" | sal (16) | nonce (12) | tag (16) | dados cifrados.
/// </summary>
public sealed class CofreDeArquivo
{
    private static readonly byte[] Assinatura = "OASIS1"u8.ToArray();
    private const int TamanhoSal = 16;
    private const int TamanhoNonce = 12;
    private const int TamanhoTag = 16;
    private const int Iteracoes = 600_000;
    public const int TamanhoMinimoSenha = 8;

    private readonly byte[] _chave;
    private readonly byte[] _sal;

    private CofreDeArquivo(string caminho, byte[] chave, byte[] sal)
    {
        Caminho = caminho;
        _chave = chave;
        _sal = sal;
    }

    public string Caminho { get; }

    public static bool Existe(string caminho) => File.Exists(caminho);

    public static CofreDeArquivo Criar(string caminho, string senha)
    {
        if (senha.Length < TamanhoMinimoSenha)
            throw new ArgumentException($"A senha precisa ter pelo menos {TamanhoMinimoSenha} caracteres.", nameof(senha));
        var sal = RandomNumberGenerator.GetBytes(TamanhoSal);
        return new CofreDeArquivo(caminho, DerivarChave(senha, sal), sal);
    }

    /// <summary>Abre o cofre e devolve o conteúdo. Senha errada ou arquivo mexido: SenhaIncorretaException.</summary>
    public static (CofreDeArquivo Cofre, byte[] Conteudo) Abrir(string caminho, string senha)
    {
        var arquivo = File.ReadAllBytes(caminho);
        var minimo = Assinatura.Length + TamanhoSal + TamanhoNonce + TamanhoTag;
        if (arquivo.Length < minimo || !arquivo.AsSpan(0, Assinatura.Length).SequenceEqual(Assinatura))
            throw new SenhaIncorretaException();

        var posicao = Assinatura.Length;
        var sal = arquivo.AsSpan(posicao, TamanhoSal).ToArray();
        posicao += TamanhoSal;
        var nonce = arquivo.AsSpan(posicao, TamanhoNonce);
        posicao += TamanhoNonce;
        var tag = arquivo.AsSpan(posicao, TamanhoTag);
        posicao += TamanhoTag;
        var cifrado = arquivo.AsSpan(posicao);

        var chave = DerivarChave(senha, sal);
        var conteudo = new byte[cifrado.Length];
        try
        {
            using var aes = new AesGcm(chave, TamanhoTag);
            aes.Decrypt(nonce, cifrado, tag, conteudo);
        }
        catch (AuthenticationTagMismatchException)
        {
            throw new SenhaIncorretaException();
        }
        return (new CofreDeArquivo(caminho, chave, sal), conteudo);
    }

    /// <summary>Grava com um nonce novo a cada vez, num arquivo temporário trocado no fim (não corrompe se faltar luz).</summary>
    public void Gravar(byte[] conteudo)
    {
        var nonce = RandomNumberGenerator.GetBytes(TamanhoNonce);
        var tag = new byte[TamanhoTag];
        var cifrado = new byte[conteudo.Length];
        using (var aes = new AesGcm(_chave, TamanhoTag))
        {
            aes.Encrypt(nonce, conteudo, cifrado, tag);
        }

        var pasta = Path.GetDirectoryName(Path.GetFullPath(Caminho));
        if (!string.IsNullOrEmpty(pasta)) Directory.CreateDirectory(pasta);
        var temporario = Caminho + ".tmp";
        using (var saida = File.Create(temporario))
        {
            saida.Write(Assinatura);
            saida.Write(_sal);
            saida.Write(nonce);
            saida.Write(tag);
            saida.Write(cifrado);
        }
        File.Move(temporario, Caminho, overwrite: true);
    }

    private static byte[] DerivarChave(string senha, byte[] sal) =>
        Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(senha), sal, Iteracoes, HashAlgorithmName.SHA256, 32);
}
