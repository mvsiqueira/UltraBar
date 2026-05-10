using System.ComponentModel;

namespace UltraBar;

internal sealed class ToolbarPanel : Panel
{
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Rectangle? DropIndicatorBounds { get; set; }

    public ToolbarPanel()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.UserPaint,
            true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        if (DropIndicatorBounds is not { } bounds)
        {
            return;
        }

        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        using var brush = new SolidBrush(Color.FromArgb(78, 166, 255));
        using var pen = new Pen(Color.FromArgb(138, 203, 255), 1);
        e.Graphics.FillRectangle(brush, bounds);
        e.Graphics.DrawRectangle(pen, bounds);
    }
}
