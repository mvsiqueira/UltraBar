using System.ComponentModel;

namespace UltraBar;

internal sealed class ShortcutButton : Control
{
    private bool hovered;
    private bool pressed;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Image? IconImage { get; set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string LabelText { get; set; } = "";

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ShortcutDisplayMode DisplayMode { get; set; } = ShortcutDisplayMode.Icon;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int ImagePadding { get; set; } = 10;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color HoverBackColor { get; set; } = Color.FromArgb(50, 54, 61);

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color PressedBackColor { get; set; } = Color.FromArgb(91, 99, 112);

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color BorderColor { get; set; } = Color.FromArgb(83, 88, 98);

    public ShortcutButton()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.SupportsTransparentBackColor |
            ControlStyles.UserPaint,
            true);

        Cursor = Cursors.Hand;
        TabStop = false;
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        hovered = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        hovered = false;
        pressed = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            pressed = true;
            Invalidate();
        }

        base.OnMouseDown(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        pressed = false;
        Invalidate();
        base.OnMouseUp(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var graphics = e.Graphics;
        graphics.Clear(GetSurfaceColor());

        if (pressed || hovered)
        {
            using var background = new SolidBrush(pressed ? PressedBackColor : HoverBackColor);
            graphics.FillRectangle(background, ClientRectangle);

            if (hovered)
            {
                using var border = new Pen(BorderColor);
                graphics.DrawRectangle(border, 0, 0, Width - 1, Height - 1);
            }
        }

        DrawContent(graphics);
    }

    private void DrawContent(Graphics graphics)
    {
        switch (DisplayMode)
        {
            case ShortcutDisplayMode.Text:
                DrawText(graphics, Rectangle.Inflate(ClientRectangle, -4, -4), 8.25f);
                break;
            case ShortcutDisplayMode.IconAndText:
                DrawIconAndText(graphics);
                break;
            default:
                DrawIcon(graphics, Rectangle.Inflate(ClientRectangle, -ImagePadding, -ImagePadding));
                break;
        }
    }

    private void DrawIconAndText(Graphics graphics)
    {
        var contentBounds = Rectangle.Inflate(ClientRectangle, -Math.Max(3, ImagePadding / 2), -Math.Max(3, ImagePadding / 2));
        var textHeight = Math.Max(14, Math.Min(22, Height / 3));
        var iconBounds = new Rectangle(
            contentBounds.Left,
            contentBounds.Top,
            contentBounds.Width,
            Math.Max(1, contentBounds.Height - textHeight - 2));
        var textBounds = new Rectangle(
            contentBounds.Left,
            iconBounds.Bottom + 2,
            contentBounds.Width,
            textHeight);

        DrawIcon(graphics, iconBounds);
        DrawText(graphics, textBounds, 7.25f);
    }

    private void DrawIcon(Graphics graphics, Rectangle iconBounds)
    {
        if (IconImage is null)
        {
            return;
        }

        graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

        if (iconBounds.Width <= 0 || iconBounds.Height <= 0)
        {
            return;
        }

        var scale = Math.Min(
            iconBounds.Width / (float)IconImage.Width,
            iconBounds.Height / (float)IconImage.Height);
        var width = Math.Max(1, (int)(IconImage.Width * scale));
        var height = Math.Max(1, (int)(IconImage.Height * scale));
        var target = new Rectangle(
            iconBounds.Left + ((iconBounds.Width - width) / 2),
            iconBounds.Top + ((iconBounds.Height - height) / 2),
            width,
            height);

        graphics.DrawImage(IconImage, target);
    }

    private void DrawText(Graphics graphics, Rectangle textBounds, float fontSize)
    {
        if (string.IsNullOrWhiteSpace(LabelText) || textBounds.Width <= 0 || textBounds.Height <= 0)
        {
            return;
        }

        using var font = new Font(Font.FontFamily, fontSize, FontStyle.Regular);
        TextRenderer.DrawText(
            graphics,
            LabelText,
            font,
            textBounds,
            ForeColor,
            TextFormatFlags.HorizontalCenter |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.EndEllipsis |
            TextFormatFlags.NoPrefix);
    }

    private Color GetSurfaceColor()
    {
        var parent = Parent;

        while (parent is not null)
        {
            if (parent.BackColor.A == 255)
            {
                return parent.BackColor;
            }

            parent = parent.Parent;
        }

        return Color.FromArgb(36, 38, 43);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            IconImage?.Dispose();
        }

        base.Dispose(disposing);
    }
}
