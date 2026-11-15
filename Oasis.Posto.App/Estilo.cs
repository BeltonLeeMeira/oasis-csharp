using Oasis.Posto.Dominio;

namespace Oasis.Posto.App;

/// <summary>Cores e fontes do painel: letra grande e contraste alto para uso rápido no posto.</summary>
internal static class Estilo
{
    public static readonly Color Fundo = Color.FromArgb(0xFF, 0xFA, 0xF3);
    public static readonly Color Primaria = Color.FromArgb(0x7A, 0x2E, 0x0E);
    public static readonly Color Texto = Color.FromArgb(0x1D, 0x1B, 0x18);
    public static readonly Color Suave = Color.FromArgb(0x5B, 0x54, 0x4B);
    public static readonly Color Erro = Color.FromArgb(0xA1, 0x12, 0x1B);
    public static readonly Color LinhaLigar = Color.FromArgb(0xFB, 0xEB, 0xEB);
    public static readonly Color LinhaFeita = Color.FromArgb(0xE5, 0xF4, 0xE4);

    public static readonly Font Normal = new("Segoe UI", 11F);
    public static readonly Font Negrito = new("Segoe UI", 11F, FontStyle.Bold);
    public static readonly Font Titulo = new("Segoe UI", 16F, FontStyle.Bold);
    public static readonly Font Destaque = new("Segoe UI", 14F, FontStyle.Bold);

    public static Color CorDoNivel(NivelRisco nivel) => nivel switch
    {
        NivelRisco.Atencao => Color.FromArgb(0x8A, 0x5A, 0x00),
        NivelRisco.Alerta => Color.FromArgb(0xB3, 0x47, 0x00),
        NivelRisco.Perigo => Color.FromArgb(0xA1, 0x12, 0x1B),
        _ => Color.FromArgb(0x1F, 0x6F, 0x43),
    };

    public static Button BotaoPrincipal(string texto)
    {
        var botao = new Button
        {
            Text = texto,
            AutoSize = true,
            MinimumSize = new Size(170, 42),
            BackColor = Primaria,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = Negrito,
            Margin = new Padding(0, 6, 10, 6),
            Cursor = Cursors.Hand,
        };
        botao.FlatAppearance.BorderSize = 0;
        return botao;
    }

    public static Button BotaoSecundario(string texto)
    {
        var botao = new Button
        {
            Text = texto,
            AutoSize = true,
            MinimumSize = new Size(150, 42),
            BackColor = Color.White,
            ForeColor = Primaria,
            FlatStyle = FlatStyle.Flat,
            Font = Negrito,
            Margin = new Padding(0, 6, 10, 6),
            Cursor = Cursors.Hand,
        };
        botao.FlatAppearance.BorderColor = Primaria;
        botao.FlatAppearance.BorderSize = 2;
        return botao;
    }

    public static Label Rotulo(string texto, Font? fonte = null, Color? cor = null) => new()
    {
        Text = texto,
        AutoSize = true,
        Font = fonte ?? Normal,
        ForeColor = cor ?? Texto,
        Margin = new Padding(0, 8, 10, 4),
    };

    /// <summary>Tabela somente leitura, uma linha selecionada por vez, colunas ocupando a largura.</summary>
    public static DataGridView Tabela()
    {
        var tabela = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            MultiSelect = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowHeadersVisible = false,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            EnableHeadersVisualStyles = false,
            Font = Normal,
        };
        tabela.ColumnHeadersDefaultCellStyle.Font = Negrito;
        tabela.ColumnHeadersDefaultCellStyle.BackColor = Fundo;
        tabela.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        tabela.RowTemplate.Height = 34;
        tabela.DefaultCellStyle.SelectionBackColor = Primaria;
        tabela.DefaultCellStyle.SelectionForeColor = Color.White;
        return tabela;
    }
}
