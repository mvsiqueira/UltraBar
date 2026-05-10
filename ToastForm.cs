namespace UltraBar;

internal sealed class ToastForm : Form
{
    private readonly Label textLabel = new();

    public ToastForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        BackColor = Color.FromArgb(22, 24, 29);
        Opacity = 0.92;
        Width = 150;
        Height = 38;

        textLabel.Dock = DockStyle.Fill;
        textLabel.ForeColor = Color.White;
        textLabel.Font = new Font(Font, FontStyle.Bold);
        textLabel.TextAlign = ContentAlignment.MiddleCenter;
        Controls.Add(textLabel);
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            const int WS_EX_TOOLWINDOW = 0x00000080;
            const int WS_EX_NOACTIVATE = 0x08000000;

            var createParams = base.CreateParams;
            createParams.ExStyle |= WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
            return createParams;
        }
    }

    public void SetText(string text)
    {
        textLabel.Text = text;
    }
}
