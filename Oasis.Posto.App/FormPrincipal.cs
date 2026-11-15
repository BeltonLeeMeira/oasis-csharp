using Oasis.Posto.Api;
using Oasis.Posto.Dados;
using Oasis.Posto.Dominio;

namespace Oasis.Posto.App;

/// <summary>
/// Tela principal do posto comunitário, em quatro abas:
/// Hoje (busca ativa), Pessoas acompanhadas, Pontos para aprovar e Configurações.
/// </summary>
internal sealed class FormPrincipal : Form
{
    private const string UsarNivelDaApi = "Usar o nível da API";

    private readonly SessaoDoPosto _sessao;
    private NivelRisco? _nivelDaApi;
    private string _origemDoNivel = "sem leitura da API ainda";

    // Aba Hoje
    private readonly Label _nivel = Estilo.Rotulo("Normal", Estilo.Titulo);
    private readonly Label _origem = Estilo.Rotulo("", cor: Estilo.Suave);
    private readonly ComboBox _nivelManual = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220, Font = Estilo.Normal };
    private readonly Label _previsao = Estilo.Rotulo("Amanhã: sem previsão", Estilo.Negrito);
    private readonly Label _resumo = Estilo.Rotulo("", cor: Estilo.Suave);
    private readonly DataGridView _tabelaHoje = Estilo.Tabela();
    private readonly Button _atualizar = Estilo.BotaoSecundario("Atualizar da API");
    private readonly Button _registrarContato = Estilo.BotaoPrincipal("Registrar ligação");
    private readonly Button _copiarTelefone = Estilo.BotaoSecundario("Copiar telefone");

    // Aba Pessoas
    private readonly DataGridView _tabelaPessoas = Estilo.Tabela();
    private readonly Button _novaPessoa = Estilo.BotaoPrincipal("Nova pessoa");
    private readonly Button _editarPessoa = Estilo.BotaoSecundario("Editar");
    private readonly Button _removerPessoa = Estilo.BotaoSecundario("Remover");

    // Aba Pontos
    private readonly DataGridView _tabelaPontos = Estilo.Tabela();
    private readonly Button _buscarPontos = Estilo.BotaoSecundario("Buscar pendentes");
    private readonly Button _aprovar = Estilo.BotaoPrincipal("Aprovar");
    private readonly Button _rejeitar = Estilo.BotaoSecundario("Rejeitar");
    private readonly Label _infoPontos = Estilo.Rotulo("Pontos sugeridos pelo app esperam a conferência do posto antes de aparecer para todos.", cor: Estilo.Suave);

    // Aba Configurações
    private readonly TextBox _urlApi = new() { Width = 420, Font = Estilo.Normal };
    private readonly TextBox _chavePosto = new() { Width = 420, Font = Estilo.Normal, UseSystemPasswordChar = true };
    private readonly Button _testar = Estilo.BotaoSecundario("Testar conexão");
    private readonly Button _salvarConfig = Estilo.BotaoPrincipal("Salvar");
    private readonly Label _infoConfig = Estilo.Rotulo("", cor: Estilo.Suave);

    private readonly ToolStripStatusLabel _status = new() { Text = "Pronto." };
    private readonly System.Windows.Forms.Timer _relogio = new() { Interval = 60_000 };

    public FormPrincipal(SessaoDoPosto sessao)
    {
        _sessao = sessao;
        MontarTela();
        PreencherPessoas();
        PreencherHoje();
        _urlApi.Text = _sessao.UrlApi;
        _chavePosto.Text = _sessao.ChavePosto;
        _infoConfig.Text = $"Arquivo do posto (criptografado): {SessaoDoPosto.CaminhoPadrao}";
        Shown += async (_, _) => await AtualizarDaApiAsync();
        _relogio.Tick += async (_, _) => await AtualizarDaApiAsync();
        _relogio.Start();
    }

    // ------------------------------------------------------------------ tela

    private void MontarTela()
    {
        var versao = typeof(FormPrincipal).Assembly.GetName().Version;
        Text = $"Oásis · Painel do Posto v{versao?.Major}.{versao?.Minor}.{versao?.Build}";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1000, 680);
        Size = new Size(1180, 760);
        BackColor = Estilo.Fundo;
        Font = Estilo.Normal;

        var abas = new TabControl { Dock = DockStyle.Fill, Font = Estilo.Negrito, Padding = new Point(16, 6) };
        abas.TabPages.Add(MontarAbaHoje());
        abas.TabPages.Add(MontarAbaPessoas());
        abas.TabPages.Add(MontarAbaPontos());
        abas.TabPages.Add(MontarAbaConfiguracoes());

        var barra = new StatusStrip { Font = Estilo.Normal };
        barra.Items.Add(_status);

        Controls.Add(abas);
        Controls.Add(barra);
    }

    private static TabPage NovaAba(string titulo) => new(titulo) { BackColor = Estilo.Fundo, Padding = new Padding(16), Font = Estilo.Normal };

    private static TableLayoutPanel Grade(int linhas)
    {
        var grade = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = linhas };
        grade.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        return grade;
    }

    private static FlowLayoutPanel Linha(params Control[] controles)
    {
        var linha = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Dock = DockStyle.Fill, WrapContents = true };
        linha.Controls.AddRange(controles);
        return linha;
    }

    private TabPage MontarAbaHoje()
    {
        _nivelManual.Items.AddRange([UsarNivelDaApi, "Normal", "Atenção", "Alerta", "Perigo"]);
        _nivelManual.SelectedIndex = 0;
        _nivelManual.SelectedIndexChanged += (_, _) => PreencherHoje();

        _tabelaHoje.Columns.Add("situacao", "Situação");
        _tabelaHoje.Columns.Add("pontos", "Pontos");
        _tabelaHoje.Columns.Add("apelido", "Pessoa");
        _tabelaHoje.Columns.Add("idade", "Idade");
        _tabelaHoje.Columns.Add("telefone", "Telefone");
        _tabelaHoje.Columns.Add("motivo", "Por quê");
        _tabelaHoje.Columns["situacao"]!.FillWeight = 70;
        _tabelaHoje.Columns["pontos"]!.FillWeight = 40;
        _tabelaHoje.Columns["idade"]!.FillWeight = 35;
        _tabelaHoje.Columns["motivo"]!.FillWeight = 220;
        _tabelaHoje.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0) RegistrarLigacao();
        };

        _atualizar.Click += async (_, _) => await AtualizarDaApiAsync();
        _registrarContato.Click += (_, _) => RegistrarLigacao();
        _copiarTelefone.Click += (_, _) =>
        {
            if (PessoaSelecionada(_tabelaHoje) is { } pessoa)
            {
                Clipboard.SetText(pessoa.Telefone);
                _status.Text = $"Telefone de {pessoa.Apelido} copiado.";
            }
        };

        var grade = Grade(6);
        grade.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grade.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grade.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grade.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grade.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        grade.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grade.Controls.Add(Linha(Estilo.Rotulo("Calor agora:", Estilo.Destaque), _nivel, _origem), 0, 0);
        grade.Controls.Add(Linha(Estilo.Rotulo("Nível usado na lista:"), _nivelManual, _atualizar), 0, 1);
        grade.Controls.Add(_previsao, 0, 2);
        grade.Controls.Add(_resumo, 0, 3);
        grade.Controls.Add(_tabelaHoje, 0, 4);
        grade.Controls.Add(Linha(_registrarContato, _copiarTelefone, Estilo.Rotulo("Dica: dois cliques na linha também registram a ligação.", cor: Estilo.Suave)), 0, 5);

        var aba = NovaAba("Hoje");
        aba.Controls.Add(grade);
        return aba;
    }

    private TabPage MontarAbaPessoas()
    {
        _tabelaPessoas.Columns.Add("apelido", "Pessoa");
        _tabelaPessoas.Columns.Add("idade", "Idade");
        _tabelaPessoas.Columns.Add("sozinha", "Mora sozinha");
        _tabelaPessoas.Columns.Add("condicao", "Condição de atenção");
        _tabelaPessoas.Columns.Add("telefone", "Telefone");
        _tabelaPessoas.Columns.Add("ultimo", "Última ligação");
        _tabelaPessoas.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0) EditarPessoa();
        };

        _novaPessoa.Click += (_, _) => NovaPessoa();
        _editarPessoa.Click += (_, _) => EditarPessoa();
        _removerPessoa.Click += (_, _) => RemoverPessoa();

        var grade = Grade(3);
        grade.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grade.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        grade.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grade.Controls.Add(Estilo.Rotulo("Idosos que o posto acompanha nos dias de calor. Os dados ficam só neste computador, criptografados com a senha do posto. Protótipo: use dados fictícios.", cor: Estilo.Suave), 0, 0);
        grade.Controls.Add(_tabelaPessoas, 0, 1);
        grade.Controls.Add(Linha(_novaPessoa, _editarPessoa, _removerPessoa), 0, 2);

        var aba = NovaAba("Pessoas acompanhadas");
        aba.Controls.Add(grade);
        return aba;
    }

    private TabPage MontarAbaPontos()
    {
        _tabelaPontos.Columns.Add("nome", "Lugar");
        _tabelaPontos.Columns.Add("tipo", "Oferece");
        _tabelaPontos.Columns.Add("endereco", "Endereço");
        _tabelaPontos.Columns.Add("bairro", "Bairro");
        _tabelaPontos.Columns.Add("horario", "Horário");
        _tabelaPontos.Columns.Add("enviado", "Enviado em");

        _buscarPontos.Click += async (_, _) => await BuscarPontosAsync();
        _aprovar.Click += async (_, _) => await AvaliarPontoAsync("Aprovado");
        _rejeitar.Click += async (_, _) => await AvaliarPontoAsync("Rejeitado");

        var grade = Grade(3);
        grade.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grade.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        grade.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grade.Controls.Add(Linha(_infoPontos), 0, 0);
        grade.Controls.Add(_tabelaPontos, 0, 1);
        grade.Controls.Add(Linha(_buscarPontos, _aprovar, _rejeitar), 0, 2);

        var aba = NovaAba("Pontos para aprovar");
        aba.Controls.Add(grade);
        return aba;
    }

    private TabPage MontarAbaConfiguracoes()
    {
        _testar.Click += async (_, _) => await TestarConexaoAsync();
        _salvarConfig.Click += (_, _) => SalvarConfiguracao();

        var painel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        painel.Controls.Add(Estilo.Rotulo("Endereço da API Oásis"));
        painel.Controls.Add(_urlApi);
        painel.Controls.Add(Estilo.Rotulo("Ex.: http://api.oasis.local (servidor do posto) ou http://localhost:8000", cor: Estilo.Suave));
        painel.Controls.Add(Estilo.Rotulo("Chave do posto"));
        painel.Controls.Add(_chavePosto);
        painel.Controls.Add(Estilo.Rotulo("Fica guardada no arquivo criptografado, junto com os dados das pessoas.", cor: Estilo.Suave));
        painel.Controls.Add(Linha(_testar, _salvarConfig));
        painel.Controls.Add(_infoConfig);

        var aba = NovaAba("Configurações");
        aba.Controls.Add(painel);
        return aba;
    }

    // ------------------------------------------------------------- aba Hoje

    private NivelRisco NivelEmUso()
    {
        return _nivelManual.SelectedIndex switch
        {
            1 => NivelRisco.Normal,
            2 => NivelRisco.Atencao,
            3 => NivelRisco.Alerta,
            4 => NivelRisco.Perigo,
            _ => _nivelDaApi ?? NivelRisco.Normal,
        };
    }

    private void PreencherHoje()
    {
        var nivel = NivelEmUso();
        _nivel.Text = nivel.Nome();
        _nivel.ForeColor = Estilo.CorDoNivel(nivel);
        _origem.Text = _nivelManual.SelectedIndex == 0 ? $"({_origemDoNivel})" : "(escolhido à mão)";

        var lista = BuscaAtiva.MontarLista(_sessao.Pessoas, nivel, DateTime.Now);
        _tabelaHoje.Rows.Clear();
        foreach (var item in lista)
        {
            var indice = _tabelaHoje.Rows.Add(item.Situacao.Nome(), item.Pontuacao, item.Pessoa.Apelido, item.Pessoa.Idade, item.Pessoa.Telefone, item.Motivo);
            var linha = _tabelaHoje.Rows[indice];
            linha.Tag = item.Pessoa;
            linha.DefaultCellStyle.BackColor = item.Situacao switch
            {
                SituacaoNaLista.LigarHoje => Estilo.LinhaLigar,
                SituacaoNaLista.JaContatada => Estilo.LinhaFeita,
                _ => Color.White,
            };
        }

        var ligar = lista.Count(i => i.Situacao == SituacaoNaLista.LigarHoje);
        var feitas = lista.Count(i => i.Situacao == SituacaoNaLista.JaContatada);
        _resumo.Text = nivel == NivelRisco.Normal
            ? $"Dia sem risco: nenhuma ligação obrigatória. {lista.Count} pessoas acompanhadas."
            : $"{ligar} para ligar hoje · {feitas} já contatadas · mínimo de {BuscaAtiva.PontuacaoMinima(nivel)} pontos no nível {nivel.Nome()}.";
    }

    private async Task AtualizarDaApiAsync()
    {
        _atualizar.Enabled = false;
        _status.Text = "Consultando a API...";
        try
        {
            using var api = NovoCliente();
            var estacoes = await api.RiscoAgoraAsync();
            var previsao = await api.PrevisaoAmanhaAsync();
            _nivelDaApi = BuscaAtiva.NivelDoDia(estacoes.Select(e => e.Nivel));
            var pior = estacoes.Where(e => e.Nivel == _nivelDaApi && e.RecebidaEm.HasValue).OrderByDescending(e => e.RecebidaEm).FirstOrDefault();
            _origemDoNivel = pior is null
                ? "API sem leituras ainda"
                : $"API: {pior.Nome}, {pior.IndiceCalor:0.0} °C às {pior.RecebidaEm!.Value.ToLocalTime():HH:mm}";
            _previsao.Text = previsao is null
                ? "Amanhã: a previsão aparece depois da primeira leitura do dia."
                : $"Amanhã ({previsao.DataAlvo:dd/MM}): {(previsao.Risco ? "RISCO de calor forte" : "sem risco previsto")} · {Math.Min(99, Math.Round(previsao.Probabilidade * 100)):0}% de chance";
            _previsao.ForeColor = previsao?.Risco == true ? Estilo.Erro : Estilo.Texto;
            _status.Text = $"API atualizada às {DateTime.Now:HH:mm}. Próxima atualização automática em 1 minuto.";
        }
        catch (ErroApi erro)
        {
            _origemDoNivel = "API fora do ar: escolha o nível à mão";
            _status.Text = erro.Message;
        }
        finally
        {
            _atualizar.Enabled = true;
            PreencherHoje();
        }
    }

    private void RegistrarLigacao()
    {
        if (PessoaSelecionada(_tabelaHoje) is not { } pessoa)
        {
            _status.Text = "Selecione uma pessoa na lista.";
            return;
        }
        using var tela = new FormContato(pessoa);
        if (tela.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            pessoa.RegistrarContato(tela.Resultado, DateTime.Now, tela.Nota);
            _sessao.Salvar();
            _status.Text = $"Ligação para {pessoa.Apelido} registrada: {tela.Resultado.Nome()}.";
        }
        catch (ErroDeValidacao erro)
        {
            MessageBox.Show(this, erro.Message, "Não foi possível registrar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        PreencherHoje();
        PreencherPessoas();
    }

    // ---------------------------------------------------------- aba Pessoas

    private void PreencherPessoas()
    {
        _tabelaPessoas.Rows.Clear();
        foreach (var pessoa in _sessao.Pessoas.OrderBy(p => p.Apelido, StringComparer.CurrentCultureIgnoreCase))
        {
            var ultimo = pessoa.UltimoContato;
            var indice = _tabelaPessoas.Rows.Add(
                pessoa.Apelido,
                pessoa.Idade,
                pessoa.MoraSozinha ? "Sim" : "Não",
                pessoa.CondicaoDeAtencao ? "Sim" : "Não",
                pessoa.Telefone,
                ultimo is null ? "nunca" : $"{ultimo.DataHora:dd/MM HH:mm} · {ultimo.Resultado.Nome()}");
            _tabelaPessoas.Rows[indice].Tag = pessoa;
        }
    }

    private void NovaPessoa()
    {
        using var tela = new FormPessoa();
        if (tela.ShowDialog(this) == DialogResult.OK && tela.Resultado is { } pessoa)
        {
            _sessao.Adicionar(pessoa);
            _status.Text = $"{pessoa.Apelido} incluída na busca ativa.";
            PreencherPessoas();
            PreencherHoje();
        }
    }

    private void EditarPessoa()
    {
        if (PessoaSelecionada(_tabelaPessoas) is not { } pessoa)
        {
            _status.Text = "Selecione uma pessoa na lista.";
            return;
        }
        using var tela = new FormPessoa(pessoa);
        if (tela.ShowDialog(this) == DialogResult.OK)
        {
            _sessao.Salvar();
            _status.Text = $"Cadastro de {pessoa.Apelido} atualizado.";
            PreencherPessoas();
            PreencherHoje();
        }
    }

    private void RemoverPessoa()
    {
        if (PessoaSelecionada(_tabelaPessoas) is not { } pessoa) return;
        var resposta = MessageBox.Show(this, $"Remover {pessoa.Apelido} da busca ativa? O histórico de ligações também será apagado.",
            "Remover pessoa", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (resposta != DialogResult.Yes) return;
        _sessao.Remover(pessoa.Id);
        _status.Text = $"{pessoa.Apelido} removida.";
        PreencherPessoas();
        PreencherHoje();
    }

    private static PessoaAcompanhada? PessoaSelecionada(DataGridView tabela) => tabela.CurrentRow?.Tag as PessoaAcompanhada;

    // ----------------------------------------------------------- aba Pontos

    private async Task BuscarPontosAsync()
    {
        _buscarPontos.Enabled = false;
        try
        {
            using var api = NovoCliente();
            var pontos = await api.PontosPendentesAsync();
            _tabelaPontos.Rows.Clear();
            foreach (var ponto in pontos)
            {
                var indice = _tabelaPontos.Rows.Add(ponto.Nome, ponto.Tipo, ponto.Endereco, ponto.Bairro, ponto.Horario ?? "-", ponto.CriadoEm.ToLocalTime().ToString("dd/MM HH:mm"));
                _tabelaPontos.Rows[indice].Tag = ponto;
            }
            _infoPontos.Text = pontos.Count == 0 ? "Nenhum ponto esperando conferência." : $"{pontos.Count} ponto(s) esperando conferência.";
            _status.Text = "Pontos pendentes atualizados.";
        }
        catch (ErroApi erro)
        {
            _status.Text = erro.Message;
        }
        finally
        {
            _buscarPontos.Enabled = true;
        }
    }

    private async Task AvaliarPontoAsync(string status)
    {
        if (_tabelaPontos.CurrentRow?.Tag is not PontoApoio ponto)
        {
            _status.Text = "Selecione um ponto na lista.";
            return;
        }
        try
        {
            using var api = NovoCliente();
            await api.AlterarStatusPontoAsync(ponto.Id, status);
            _status.Text = $"\"{ponto.Nome}\" {(status == "Aprovado" ? "aprovado: já aparece no app" : "rejeitado")}.";
            await BuscarPontosAsync();
        }
        catch (ErroApi erro)
        {
            _status.Text = erro.Message;
        }
    }

    // ---------------------------------------------------- aba Configurações

    private ClienteApiOasis NovoCliente() => new(_sessao.UrlApi, _sessao.ChavePosto);

    private async Task TestarConexaoAsync()
    {
        using var api = new ClienteApiOasis(_urlApi.Text, _chavePosto.Text);
        _infoConfig.Text = await api.EstaNoArAsync() ? "Conexão OK: a API respondeu." : "A API não respondeu nesse endereço.";
    }

    private void SalvarConfiguracao()
    {
        try
        {
            _sessao.AlterarConexao(_urlApi.Text, _chavePosto.Text);
            _infoConfig.Text = "Configuração salva no arquivo criptografado.";
        }
        catch (ErroDeValidacao erro)
        {
            _infoConfig.Text = erro.Message;
        }
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _relogio.Stop();
        _relogio.Dispose();
        base.OnFormClosed(e);
    }
}
