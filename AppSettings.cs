using System.Text.Json.Serialization;

namespace UltraBar;

public enum DockEdge
{
    Left,
    Top,
    Right,
    Bottom
}

public enum ToolbarItemKind
{
    Shortcut,
    Separator
}

public enum ShortcutDisplayMode
{
    Icon,
    Text,
    IconAndText
}

public enum ShortcutDisplayOverride
{
    Inherit,
    Icon,
    Text,
    IconAndText
}

public sealed class AppSettings
{
    public DockEdge DockEdge { get; set; } = DockEdge.Top;

    public int Thickness { get; set; } = 72;

    public int ButtonSize { get; set; } = 56;

    public int ImagePadding { get; set; } = 10;

    public int ButtonMargin { get; set; } = 8;

    public string BarBackgroundColor { get; set; } = "#24262B";

    public int BarTransparency { get; set; }

    public ShortcutDisplayMode ShortcutDisplayMode { get; set; } = ShortcutDisplayMode.Icon;

    public List<ShortcutItem> Shortcuts { get; set; } = [];

    public void Sanitize()
    {
        Thickness = Math.Clamp(Thickness, 48, 420);
        ButtonSize = Math.Clamp(ButtonSize, 32, 128);
        ImagePadding = Math.Clamp(ImagePadding, 0, Math.Max(0, (ButtonSize / 2) - 4));
        ButtonMargin = Math.Clamp(ButtonMargin, 0, 32);
        BarTransparency = Math.Clamp(BarTransparency, 0, 85);

        try
        {
            _ = ColorTranslator.FromHtml(BarBackgroundColor);
        }
        catch
        {
            BarBackgroundColor = "#24262B";
        }
    }

    public Color GetBarBackgroundColor()
    {
        try
        {
            return ColorTranslator.FromHtml(BarBackgroundColor);
        }
        catch
        {
            return Color.FromArgb(36, 38, 43);
        }
    }

    public double GetOpacity()
    {
        return Math.Clamp((100 - BarTransparency) / 100d, 0.15d, 1d);
    }
}

public sealed class ShortcutItem
{
    public ToolbarItemKind Kind { get; set; } = ToolbarItemKind.Shortcut;

    public string Name { get; set; } = "";

    public string Path { get; set; } = "";

    public ShortcutDisplayOverride DisplayMode { get; set; } = ShortcutDisplayOverride.Inherit;

    [JsonIgnore]
    public bool IsSeparator => Kind == ToolbarItemKind.Separator;

    [JsonIgnore]
    public bool Exists => IsSeparator || File.Exists(Path) || Directory.Exists(Path);
}
