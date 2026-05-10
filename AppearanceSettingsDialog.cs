namespace UltraBar;

internal sealed class AppearanceSettingsDialog : Form
{
    private readonly NumericUpDown buttonSizeInput = new();
    private readonly NumericUpDown imagePaddingInput = new();
    private readonly NumericUpDown buttonMarginInput = new();
    private readonly NumericUpDown barTransparencyInput = new();
    private readonly ComboBox shortcutDisplayModeInput = new();
    private readonly Button backgroundColorButton = new();
    private Color backgroundColor;

    public int ButtonSize => (int)buttonSizeInput.Value;

    public int ImagePadding => (int)imagePaddingInput.Value;

    public int ButtonMargin => (int)buttonMarginInput.Value;

    public int BarTransparency => (int)barTransparencyInput.Value;

    public string BarBackgroundColor => ColorTranslator.ToHtml(backgroundColor);

    public ShortcutDisplayMode ShortcutDisplayMode => shortcutDisplayModeInput.SelectedIndex switch
    {
        1 => ShortcutDisplayMode.Text,
        2 => ShortcutDisplayMode.IconAndText,
        _ => ShortcutDisplayMode.Icon
    };

    public AppearanceSettingsDialog(AppSettings settings)
    {
        Text = "Aparência";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(380, 292);

        backgroundColor = settings.GetBarBackgroundColor();

        buttonSizeInput.Minimum = 32;
        buttonSizeInput.Maximum = 128;
        buttonSizeInput.Value = settings.ButtonSize;

        imagePaddingInput.Minimum = 0;
        imagePaddingInput.Maximum = 48;
        imagePaddingInput.Value = settings.ImagePadding;

        buttonMarginInput.Minimum = 0;
        buttonMarginInput.Maximum = 32;
        buttonMarginInput.Value = settings.ButtonMargin;

        barTransparencyInput.Minimum = 0;
        barTransparencyInput.Maximum = 85;
        barTransparencyInput.Value = settings.BarTransparency;

        shortcutDisplayModeInput.DropDownStyle = ComboBoxStyle.DropDownList;
        shortcutDisplayModeInput.Items.AddRange(["Ícone", "Texto", "Ícone + texto"]);
        shortcutDisplayModeInput.SelectedIndex = settings.ShortcutDisplayMode switch
        {
            ShortcutDisplayMode.Text => 1,
            ShortcutDisplayMode.IconAndText => 2,
            _ => 0
        };

        backgroundColorButton.Text = "Escolher...";
        backgroundColorButton.BackColor = backgroundColor;
        backgroundColorButton.ForeColor = GetReadableTextColor(backgroundColor);
        backgroundColorButton.Dock = DockStyle.Fill;
        backgroundColorButton.Click += (_, _) => ChooseBackgroundColor();

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(14),
            ColumnCount = 2,
            RowCount = 7
        };

        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 68));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        AddRow(layout, 0, "Tamanho do botão", buttonSizeInput);
        AddRow(layout, 1, "Margem interna do botão", imagePaddingInput);
        AddRow(layout, 2, "Margem entre botões", buttonMarginInput);
        AddRow(layout, 3, "Exibição padrão", shortcutDisplayModeInput);
        AddRow(layout, 4, "Cor de fundo", backgroundColorButton);
        AddRow(layout, 5, "Transparência da barra (%)", barTransparencyInput);

        var buttons = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 8, 0, 0)
        };

        var okButton = new Button
        {
            Text = "OK",
            DialogResult = DialogResult.OK,
            Width = 86
        };
        var cancelButton = new Button
        {
            Text = "Cancelar",
            DialogResult = DialogResult.Cancel,
            Width = 86
        };

        buttons.Controls.Add(okButton);
        buttons.Controls.Add(cancelButton);
        layout.Controls.Add(buttons, 0, 6);
        layout.SetColumnSpan(buttons, 2);

        AcceptButton = okButton;
        CancelButton = cancelButton;
        Controls.Add(layout);
    }

    private static void AddRow(TableLayoutPanel layout, int row, string label, NumericUpDown input)
    {
        AddRow(layout, row, label, (Control)input);
        input.Dock = DockStyle.Fill;
    }

    private static void AddRow(TableLayoutPanel layout, int row, string label, Control input)
    {
        layout.Controls.Add(new Label
        {
            Text = label,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, row);

        input.Dock = DockStyle.Fill;
        layout.Controls.Add(input, 1, row);
    }

    private void ChooseBackgroundColor()
    {
        using var dialog = new ColorDialog
        {
            Color = backgroundColor,
            FullOpen = true
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        backgroundColor = dialog.Color;
        backgroundColorButton.BackColor = backgroundColor;
        backgroundColorButton.ForeColor = GetReadableTextColor(backgroundColor);
    }

    private static Color GetReadableTextColor(Color color)
    {
        var luminance = ((0.299 * color.R) + (0.587 * color.G) + (0.114 * color.B)) / 255;
        return luminance > 0.55 ? Color.Black : Color.White;
    }
}
