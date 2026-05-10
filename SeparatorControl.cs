using System.ComponentModel;

namespace UltraBar;

internal sealed class SeparatorControl : Control
{
    private bool hovered;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public DockEdge DockEdge { get; set; }

    public SeparatorControl()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.SupportsTransparentBackColor |
            ControlStyles.UserPaint,
            true);

        Cursor = Cursors.Default;
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
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var graphics = e.Graphics;
        graphics.Clear(GetSurfaceColor());
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        var lineColor = hovered
            ? Color.FromArgb(142, 150, 164)
            : Color.FromArgb(88, 94, 106);

        using var pen = new Pen(lineColor, 1.5f)
        {
            StartCap = System.Drawing.Drawing2D.LineCap.Round,
            EndCap = System.Drawing.Drawing2D.LineCap.Round
        };

        if (DockEdge is DockEdge.Left or DockEdge.Right)
        {
            var y = Height / 2;
            var margin = Math.Max(3, Width / 14);
            graphics.DrawLine(pen, margin, y, Width - margin, y);
        }
        else
        {
            var x = Width / 2;
            var margin = Math.Max(3, Height / 14);
            graphics.DrawLine(pen, x, margin, x, Height - margin);
        }
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
}
