namespace UltraBar;

public partial class Form1 : Form, IMessageFilter
{
    private readonly AppSettings settings;
    private readonly NativeAppBar appBar;
    private readonly ToolbarPanel shortcutPanel = new();
    private readonly Panel resizeGrip = new();
    private readonly ToolTip toolTip = new();
    private readonly ToastForm sizeToast = new();
    private readonly System.Windows.Forms.Timer sizeToastTimer = new();
    private readonly System.Windows.Forms.Timer buttonSizeSaveTimer = new();
    private readonly System.Windows.Forms.Timer buttonSizeApplyTimer = new();
    private bool resizing;
    private ShortcutItem? draggedItem;
    private Point dragStartPoint;
    private bool dragStarted;
    private bool suppressNextClick;
    private int dropIndicatorIndex = -1;
    private int pendingButtonSizeDelta;

    private const int MinThickness = 48;
    private const int MaxThickness = 420;
    private const int PanelPadding = 8;
    private const int ButtonSizeStep = 2;
    private const int MinButtonSize = 32;
    private const int MaxButtonSize = 128;
    private const int WM_MOUSEWHEEL = 0x020A;

    public Form1()
    {
        InitializeComponent();
        settings = SettingsStore.Load();
        settings.Sanitize();
        appBar = new NativeAppBar(this);

        ConfigureWindow();
        ConfigureShortcutPanel();
        ConfigureResizeGrip();
        ConfigureSizeToast();
        ConfigureContextMenu();
        Application.AddMessageFilter(this);
        RenderShortcuts();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ApplyDock();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        Application.RemoveMessageFilter(this);
        sizeToastTimer.Stop();
        if (buttonSizeApplyTimer.Enabled || pendingButtonSizeDelta != 0)
        {
            buttonSizeApplyTimer.Stop();
            ApplyQueuedButtonSizeDelta();
        }

        if (buttonSizeSaveTimer.Enabled)
        {
            buttonSizeSaveTimer.Stop();
            SettingsStore.Save(settings);
        }

        sizeToast.Close();
        appBar.Unregister();
        base.OnFormClosing(e);
    }

    public bool PreFilterMessage(ref Message m)
    {
        if (m.Msg != WM_MOUSEWHEEL || ModifierKeys != Keys.Control || !Bounds.Contains(Cursor.Position))
        {
            return false;
        }

        var delta = (short)((m.WParam.ToInt64() >> 16) & 0xffff);
        QueueButtonSizeDelta(delta);
        return true;
    }

    private void ConfigureWindow()
    {
        StartPosition = FormStartPosition.Manual;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        MinimumSize = new Size(48, 48);
        AllowDrop = true;
        ApplyAppearance();

        DragEnter += OnDragEnter;
        DragDrop += OnDragDrop;
    }

    private void ConfigureShortcutPanel()
    {
        shortcutPanel.Dock = DockStyle.Fill;
        shortcutPanel.BackColor = Color.Transparent;
        shortcutPanel.Padding = new Padding(PanelPadding);
        shortcutPanel.Layout += (_, _) => ArrangeShortcuts();
        shortcutPanel.AllowDrop = true;
        shortcutPanel.DragEnter += OnToolbarDragEnter;
        shortcutPanel.DragOver += OnToolbarDragOver;
        shortcutPanel.DragDrop += OnToolbarDragDrop;
        shortcutPanel.DragLeave += OnToolbarDragLeave;
        Controls.Add(shortcutPanel);
    }

    private void ConfigureResizeGrip()
    {
        resizeGrip.BackColor = Color.Transparent;
        resizeGrip.Width = 6;
        resizeGrip.Height = 6;
        resizeGrip.MouseDown += OnResizeGripMouseDown;
        resizeGrip.MouseMove += OnResizeGripMouseMove;
        resizeGrip.MouseUp += OnResizeGripMouseUp;
        Controls.Add(resizeGrip);
        resizeGrip.BringToFront();
    }

    private void ConfigureSizeToast()
    {
        sizeToastTimer.Interval = 900;
        sizeToastTimer.Tick += (_, _) =>
        {
            sizeToastTimer.Stop();
            sizeToast.Hide();
        };

        buttonSizeSaveTimer.Interval = 450;
        buttonSizeSaveTimer.Tick += (_, _) =>
        {
            buttonSizeSaveTimer.Stop();
            SettingsStore.Save(settings);
        };

        buttonSizeApplyTimer.Interval = 35;
        buttonSizeApplyTimer.Tick += (_, _) => ApplyQueuedButtonSizeDelta();
    }

    private void ConfigureContextMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Adicionar atalho...", null, (_, _) => AddShortcutWithDialog());
        menu.Items.Add("Adicionar separador", null, (_, _) => AddSeparator());
        menu.Items.Add("Aparência...", null, (_, _) => ShowAppearanceSettings());

        var dockMenu = new ToolStripMenuItem("Dockar");
        dockMenu.DropDownItems.Add("Esquerda", null, (_, _) => ChangeDock(DockEdge.Left));
        dockMenu.DropDownItems.Add("Topo", null, (_, _) => ChangeDock(DockEdge.Top));
        dockMenu.DropDownItems.Add("Direita", null, (_, _) => ChangeDock(DockEdge.Right));
        dockMenu.DropDownItems.Add("Base", null, (_, _) => ChangeDock(DockEdge.Bottom));
        menu.Items.Add(dockMenu);

        menu.Items.Add("Abrir arquivo de configuração", null, (_, _) => OpenSettingsFile());
        menu.Items.Add("Sair", null, (_, _) => Close());

        ContextMenuStrip = menu;
        shortcutPanel.ContextMenuStrip = menu;
    }

    private void RenderShortcuts()
    {
        shortcutPanel.SuspendLayout();
        foreach (Control control in shortcutPanel.Controls)
        {
            control.Dispose();
        }

        shortcutPanel.Controls.Clear();
        PositionResizeGrip();

        foreach (var item in settings.Shortcuts)
        {
            shortcutPanel.Controls.Add(CreateToolbarItemControl(item));
        }

        ArrangeShortcuts();
        shortcutPanel.ResumeLayout();
    }

    private void ArrangeShortcuts()
    {
        if (shortcutPanel.Controls.Count == 0)
        {
            return;
        }

        var bounds = shortcutPanel.ClientRectangle;
        var cellSize = Math.Max(1, settings.ButtonSize + settings.ButtonMargin);
        var separatorSize = GetSeparatorSize();
        var usableWidth = Math.Max(cellSize, bounds.Width - (PanelPadding * 2));
        var usableHeight = Math.Max(cellSize, bounds.Height - (PanelPadding * 2));
        var columns = Math.Max(1, usableWidth / cellSize);
        var rows = Math.Max(1, usableHeight / cellSize);

        if (IsVerticalDock())
        {
            var column = 0;
            var y = PanelPadding;

            foreach (Control control in shortcutPanel.Controls)
            {
                if (IsSeparator(control))
                {
                    if (column != 0)
                    {
                        y += cellSize;
                        column = 0;
                    }

                    control.Bounds = new Rectangle(PanelPadding, y, usableWidth, separatorSize);
                    y += separatorSize + settings.ButtonMargin;
                    continue;
                }

                control.Bounds = new Rectangle(
                    PanelPadding + (column * cellSize) + ((cellSize - settings.ButtonSize) / 2),
                    y + ((cellSize - settings.ButtonSize) / 2),
                    settings.ButtonSize,
                    settings.ButtonSize);

                column++;
                if (column >= columns)
                {
                    y += cellSize;
                    column = 0;
                }
            }

            return;
        }

        var row = 0;
        var x = PanelPadding;

        foreach (Control control in shortcutPanel.Controls)
        {
            if (IsSeparator(control))
            {
                if (row != 0)
                {
                    x += cellSize;
                    row = 0;
                }

                control.Bounds = new Rectangle(x, PanelPadding, separatorSize, usableHeight);
                x += separatorSize + settings.ButtonMargin;
                continue;
            }

            control.Bounds = new Rectangle(
                x + ((cellSize - settings.ButtonSize) / 2),
                PanelPadding + (row * cellSize) + ((cellSize - settings.ButtonSize) / 2),
                settings.ButtonSize,
                settings.ButtonSize);

            row++;
            if (row >= rows)
            {
                x += cellSize;
                row = 0;
            }
        }
    }

    private Control CreateToolbarItemControl(ShortcutItem item)
    {
        return item.IsSeparator
            ? CreateSeparator(item)
            : CreateShortcutButton(item);
    }

    private ShortcutButton CreateShortcutButton(ShortcutItem shortcut)
    {
        var button = new ShortcutButton
        {
            Width = settings.ButtonSize,
            Height = settings.ButtonSize,
            Margin = Padding.Empty,
            BackColor = Color.Transparent,
            ForeColor = Color.White,
            IconImage = TryLoadIcon(shortcut.Path),
            LabelText = shortcut.Name,
            DisplayMode = ResolveDisplayMode(shortcut),
            ImagePadding = settings.ImagePadding,
            Tag = shortcut,
            TabStop = false
        };

        button.HoverBackColor = shortcut.Exists ? Color.FromArgb(50, 54, 61) : Color.FromArgb(86, 50, 50);
        toolTip.SetToolTip(button, $"{shortcut.Name}\n{shortcut.Path}");

        button.MouseClick += (_, e) =>
        {
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            if (suppressNextClick)
            {
                suppressNextClick = false;
                return;
            }

            LaunchShortcut(shortcut);
        };
        button.ContextMenuStrip = CreateShortcutMenu(shortcut);
        ConfigureItemDrag(button, shortcut);

        return button;
    }

    private ContextMenuStrip CreateShortcutMenu(ShortcutItem shortcut)
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Abrir", null, (_, _) => LaunchShortcut(shortcut));
        menu.Items.Add(CreateDisplayModeMenu(shortcut));
        menu.Items.Add("Remover", null, (_, _) => RemoveShortcut(shortcut));
        return menu;
    }

    private ToolStripMenuItem CreateDisplayModeMenu(ShortcutItem shortcut)
    {
        var menu = new ToolStripMenuItem("Exibição");
        AddDisplayModeOption(menu, shortcut, "Usar padrão", ShortcutDisplayOverride.Inherit);
        AddDisplayModeOption(menu, shortcut, "Ícone", ShortcutDisplayOverride.Icon);
        AddDisplayModeOption(menu, shortcut, "Texto", ShortcutDisplayOverride.Text);
        AddDisplayModeOption(menu, shortcut, "Ícone + texto", ShortcutDisplayOverride.IconAndText);
        return menu;
    }

    private void AddDisplayModeOption(ToolStripMenuItem menu, ShortcutItem shortcut, string label, ShortcutDisplayOverride displayMode)
    {
        var item = new ToolStripMenuItem(label)
        {
            Checked = shortcut.DisplayMode == displayMode
        };

        item.Click += (_, _) =>
        {
            shortcut.DisplayMode = displayMode;
            SaveAndRender();
        };

        menu.DropDownItems.Add(item);
    }

    private SeparatorControl CreateSeparator(ShortcutItem separator)
    {
        var control = new SeparatorControl
        {
            Width = settings.ButtonSize,
            Height = settings.ButtonSize,
            Margin = Padding.Empty,
            DockEdge = settings.DockEdge,
            Tag = separator
        };

        control.ContextMenuStrip = CreateSeparatorMenu(separator);
        toolTip.SetToolTip(control, "Separador");
        ConfigureItemDrag(control, separator);

        return control;
    }

    private ContextMenuStrip CreateSeparatorMenu(ShortcutItem separator)
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Remover separador", null, (_, _) => RemoveShortcut(separator));
        return menu;
    }

    private void ConfigureItemDrag(Control control, ShortcutItem item)
    {
        control.AllowDrop = true;
        control.MouseDown += (_, e) =>
        {
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            draggedItem = item;
            dragStartPoint = e.Location;
            dragStarted = false;
            suppressNextClick = false;
        };

        control.MouseMove += (sender, e) =>
        {
            if (draggedItem is null || e.Button != MouseButtons.Left)
            {
                return;
            }

            var dragArea = SystemInformation.DragSize;
            var dragBox = new Rectangle(
                dragStartPoint.X - (dragArea.Width / 2),
                dragStartPoint.Y - (dragArea.Height / 2),
                dragArea.Width,
                dragArea.Height);

            if (dragBox.Contains(e.Location))
            {
                return;
            }

            dragStarted = true;
            suppressNextClick = true;
            ((Control)sender!).DoDragDrop(draggedItem, DragDropEffects.Move);
            ClearDropIndicator();
            draggedItem = null;
            dragStarted = false;
        };

        control.MouseUp += (_, _) =>
        {
            if (!dragStarted)
            {
                draggedItem = null;
            }
        };

        control.DragEnter += OnToolbarDragEnter;
        control.DragOver += OnToolbarDragOver;
        control.DragDrop += OnToolbarDragDrop;
    }

    private int GetSeparatorSize()
    {
        return Math.Clamp(settings.ButtonMargin + 14, 16, Math.Max(16, settings.ButtonSize));
    }

    private static bool IsSeparator(Control control)
    {
        return control.Tag is ShortcutItem { IsSeparator: true };
    }

    private void OnToolbarDragEnter(object? sender, DragEventArgs e)
    {
        if (draggedItem is not null)
        {
            e.Effect = DragDropEffects.Move;
            return;
        }

        if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
        {
            e.Effect = DragDropEffects.Copy;
        }
    }

    private void OnToolbarDragOver(object? sender, DragEventArgs e)
    {
        OnToolbarDragEnter(sender, e);

        if (draggedItem is null)
        {
            return;
        }

        var point = shortcutPanel.PointToClient(new Point(e.X, e.Y));
        SetDropIndicator(GetDropIndex(point));
    }

    private void OnToolbarDragDrop(object? sender, DragEventArgs e)
    {
        if (draggedItem is not null)
        {
            var point = shortcutPanel.PointToClient(new Point(e.X, e.Y));
            ReorderItem(draggedItem, GetDropIndex(point));
            draggedItem = null;
            dragStarted = false;
            ClearDropIndicator();
            return;
        }

        ClearDropIndicator();
        OnDragDrop(sender, e);
    }

    private void OnToolbarDragLeave(object? sender, EventArgs e)
    {
        ClearDropIndicator();
    }

    private void ReorderItem(ShortcutItem item, int requestedIndex)
    {
        var oldIndex = settings.Shortcuts.IndexOf(item);
        if (oldIndex < 0)
        {
            return;
        }

        settings.Shortcuts.RemoveAt(oldIndex);
        var newIndex = Math.Clamp(requestedIndex, 0, settings.Shortcuts.Count);

        settings.Shortcuts.Insert(newIndex, item);
        SaveAndRender();
    }

    private void SetDropIndicator(int index)
    {
        if (dropIndicatorIndex == index && shortcutPanel.DropIndicatorBounds is not null)
        {
            return;
        }

        dropIndicatorIndex = index;
        shortcutPanel.DropIndicatorBounds = GetDropIndicatorBounds(index);
        shortcutPanel.Invalidate();
    }

    private void ClearDropIndicator()
    {
        dropIndicatorIndex = -1;
        shortcutPanel.DropIndicatorBounds = null;
        shortcutPanel.Invalidate();
    }

    private Rectangle GetDropIndicatorBounds(int requestedIndex)
    {
        var controls = shortcutPanel.Controls
            .Cast<Control>()
            .Where(control => control.Tag is not ShortcutItem item || !ReferenceEquals(item, draggedItem))
            .ToList();

        if (controls.Count == 0)
        {
            return IsVerticalDock()
                ? new Rectangle(PanelPadding, PanelPadding, shortcutPanel.Width - (PanelPadding * 2), 4)
                : new Rectangle(PanelPadding, PanelPadding, 4, shortcutPanel.Height - (PanelPadding * 2));
        }

        var index = Math.Clamp(requestedIndex, 0, controls.Count);
        if (index < controls.Count)
        {
            return GetIndicatorBeforeControl(controls[index]);
        }

        return GetIndicatorAfterControl(controls[^1]);
    }

    private Rectangle GetIndicatorBeforeControl(Control control)
    {
        if (IsVerticalDock())
        {
            return control.Left <= PanelPadding + 2 || IsSeparator(control)
                ? new Rectangle(PanelPadding, Math.Max(PanelPadding, control.Top - 4), shortcutPanel.Width - (PanelPadding * 2), 4)
                : new Rectangle(Math.Max(PanelPadding, control.Left - 5), control.Top, 4, control.Height);
        }

        return control.Top <= PanelPadding + 2 || IsSeparator(control)
            ? new Rectangle(Math.Max(PanelPadding, control.Left - 4), PanelPadding, 4, shortcutPanel.Height - (PanelPadding * 2))
            : new Rectangle(control.Left, Math.Max(PanelPadding, control.Top - 5), control.Width, 4);
    }

    private Rectangle GetIndicatorAfterControl(Control control)
    {
        if (IsVerticalDock())
        {
            return new Rectangle(PanelPadding, Math.Min(shortcutPanel.Height - PanelPadding - 4, control.Bottom + 4), shortcutPanel.Width - (PanelPadding * 2), 4);
        }

        return new Rectangle(Math.Min(shortcutPanel.Width - PanelPadding - 4, control.Right + 4), PanelPadding, 4, shortcutPanel.Height - (PanelPadding * 2));
    }

    private int GetDropIndex(Point point)
    {
        var visualIndex = 0;

        for (var index = 0; index < shortcutPanel.Controls.Count; index++)
        {
            var control = shortcutPanel.Controls[index];
            if (control.Tag is ShortcutItem item && ReferenceEquals(item, draggedItem))
            {
                continue;
            }

            if (ShouldInsertBefore(point, control))
            {
                return visualIndex;
            }

            visualIndex++;
        }

        return visualIndex;
    }

    private bool ShouldInsertBefore(Point point, Control control)
    {
        if (IsVerticalDock())
        {
            if (point.Y < control.Top)
            {
                return true;
            }

            if (point.Y > control.Bottom)
            {
                return false;
            }

            return IsSeparator(control)
                ? point.Y < control.Top + (control.Height / 2)
                : point.X < control.Left + (control.Width / 2);
        }

        if (point.X < control.Left)
        {
            return true;
        }

        if (point.X > control.Right)
        {
            return false;
        }

        return IsSeparator(control)
            ? point.X < control.Left + (control.Width / 2)
            : point.Y < control.Top + (control.Height / 2);
    }

    private void AddShortcutWithDialog()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Escolha o aplicativo ou arquivo",
            Filter = "Aplicativos e atalhos|*.exe;*.lnk;*.bat;*.cmd;*.com|Todos os arquivos|*.*",
            CheckFileExists = true
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            AddShortcut(dialog.FileName);
        }
    }

    private void ShowAppearanceSettings()
    {
        using var dialog = new AppearanceSettingsDialog(settings);
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        settings.ButtonSize = dialog.ButtonSize;
        settings.ImagePadding = dialog.ImagePadding;
        settings.ButtonMargin = dialog.ButtonMargin;
        settings.BarBackgroundColor = dialog.BarBackgroundColor;
        settings.BarTransparency = dialog.BarTransparency;
        settings.ShortcutDisplayMode = dialog.ShortcutDisplayMode;
        SaveAndRender();
    }

    private void QueueButtonSizeDelta(int wheelDelta)
    {
        var notches = Math.Max(1, Math.Abs(wheelDelta) / 120);
        pendingButtonSizeDelta += Math.Sign(wheelDelta) * ButtonSizeStep * notches;

        if (!buttonSizeApplyTimer.Enabled)
        {
            buttonSizeApplyTimer.Start();
        }
    }

    private void ApplyQueuedButtonSizeDelta()
    {
        if (pendingButtonSizeDelta == 0)
        {
            buttonSizeApplyTimer.Stop();
            return;
        }

        var delta = pendingButtonSizeDelta;
        pendingButtonSizeDelta = 0;
        AdjustButtonSize(delta);
    }

    private void AdjustButtonSize(int delta)
    {
        var nextSize = Math.Clamp(settings.ButtonSize + delta, MinButtonSize, MaxButtonSize);
        if (nextSize == settings.ButtonSize)
        {
            ShowButtonSizeToast();
            buttonSizeApplyTimer.Stop();
            return;
        }

        settings.ButtonSize = nextSize;
        settings.Sanitize();
        ApplyButtonSizeToExistingControls();
        QueueButtonSizeSave();
        ShowButtonSizeToast();
    }

    private void ApplyButtonSizeToExistingControls()
    {
        foreach (Control control in shortcutPanel.Controls)
        {
            if (control is ShortcutButton button)
            {
                button.Width = settings.ButtonSize;
                button.Height = settings.ButtonSize;
                button.ImagePadding = settings.ImagePadding;
                button.Invalidate();
                continue;
            }

            if (control is SeparatorControl separator)
            {
                separator.DockEdge = settings.DockEdge;
                separator.Invalidate();
            }
        }

        ArrangeShortcuts();
        shortcutPanel.Invalidate(true);
    }

    private void QueueButtonSizeSave()
    {
        buttonSizeSaveTimer.Stop();
        buttonSizeSaveTimer.Start();
    }

    private void ShowButtonSizeToast()
    {
        sizeToast.SetText($"Botões: {settings.ButtonSize}px");
        PositionSizeToast();
        if (sizeToast.Visible)
        {
            sizeToast.Refresh();
        }
        else
        {
            sizeToast.Show(this);
        }

        sizeToastTimer.Stop();
        sizeToastTimer.Start();
    }

    private void PositionSizeToast()
    {
        const int margin = 24;
        var workingArea = Screen.FromControl(this).WorkingArea;
        var screenPoint = new Point(
            workingArea.Left + ((workingArea.Width - sizeToast.Width) / 2),
            workingArea.Bottom - sizeToast.Height - margin);
        sizeToast.Location = screenPoint;
    }

    private void AddShortcut(string path)
    {
        if (settings.Shortcuts.Any(item => string.Equals(item.Path, path, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        settings.Shortcuts.Add(new ShortcutItem
        {
            Name = Path.GetFileNameWithoutExtension(path),
            Path = path
        });

        SaveAndRender();
    }

    private void AddSeparator()
    {
        settings.Shortcuts.Add(new ShortcutItem
        {
            Kind = ToolbarItemKind.Separator,
            Name = "Separador"
        });

        SaveAndRender();
    }

    private void RemoveShortcut(ShortcutItem shortcut)
    {
        settings.Shortcuts.Remove(shortcut);
        SaveAndRender();
    }

    private void LaunchShortcut(ShortcutItem shortcut)
    {
        try
        {
            using var _ = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = shortcut.Path,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Não foi possível abrir o atalho", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ChangeDock(DockEdge edge)
    {
        settings.DockEdge = edge;
        SaveAndRender();
        ApplyDock();
    }

    private void SaveAndRender()
    {
        settings.Sanitize();
        SettingsStore.Save(settings);
        ApplyAppearance();
        RenderShortcuts();
    }

    private void ApplyDock()
    {
        settings.Thickness = Math.Clamp(settings.Thickness, MinThickness, MaxThickness);
        settings.Sanitize();
        appBar.SetPosition(settings.DockEdge, settings.Thickness);
        PositionResizeGrip();
        PositionSizeToast();
    }

    private void ApplyAppearance()
    {
        BackColor = settings.GetBarBackgroundColor();
        Opacity = settings.GetOpacity();
        shortcutPanel.Invalidate(true);
    }

    private bool IsVerticalDock()
    {
        return settings.DockEdge is DockEdge.Left or DockEdge.Right;
    }

    private ShortcutDisplayMode ResolveDisplayMode(ShortcutItem shortcut)
    {
        return shortcut.DisplayMode switch
        {
            ShortcutDisplayOverride.Icon => ShortcutDisplayMode.Icon,
            ShortcutDisplayOverride.Text => ShortcutDisplayMode.Text,
            ShortcutDisplayOverride.IconAndText => ShortcutDisplayMode.IconAndText,
            _ => settings.ShortcutDisplayMode
        };
    }

    private static Image? TryLoadIcon(string path)
    {
        try
        {
            var iconPath = ResolveIconPath(path);
            using var icon = Icon.ExtractAssociatedIcon(iconPath);
            return icon?.ToBitmap();
        }
        catch
        {
            return null;
        }
    }

    private static string ResolveIconPath(string path)
    {
        if (!path.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
        {
            return path;
        }

        try
        {
            var shell = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell")!);
            var shortcut = shell?.GetType().InvokeMember("CreateShortcut", System.Reflection.BindingFlags.InvokeMethod, null, shell, [path]);
            var target = shortcut?.GetType().InvokeMember("TargetPath", System.Reflection.BindingFlags.GetProperty, null, shortcut, null) as string;

            if (!string.IsNullOrWhiteSpace(target) && File.Exists(target))
            {
                return target;
            }
        }
        catch
        {
        }

        return path;
    }

    private void PositionResizeGrip()
    {
        resizeGrip.Dock = settings.DockEdge switch
        {
            DockEdge.Left => DockStyle.Right,
            DockEdge.Right => DockStyle.Left,
            DockEdge.Bottom => DockStyle.Top,
            _ => DockStyle.Bottom
        };

        resizeGrip.Cursor = IsVerticalDock() ? Cursors.SizeWE : Cursors.SizeNS;
        ArrangeShortcuts();
    }

    private void OnResizeGripMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        resizing = true;
        resizeGrip.Capture = true;
    }

    private void OnResizeGripMouseMove(object? sender, MouseEventArgs e)
    {
        if (!resizing)
        {
            return;
        }

        var screen = Screen.FromControl(this).Bounds;
        var cursor = Cursor.Position;
        var thickness = settings.DockEdge switch
        {
            DockEdge.Left => cursor.X - screen.Left,
            DockEdge.Right => screen.Right - cursor.X,
            DockEdge.Bottom => screen.Bottom - cursor.Y,
            _ => cursor.Y - screen.Top
        };

        settings.Thickness = Math.Clamp(thickness, MinThickness, MaxThickness);
        ApplyDock();
        ArrangeShortcuts();
    }

    private void OnResizeGripMouseUp(object? sender, MouseEventArgs e)
    {
        if (!resizing)
        {
            return;
        }

        resizing = false;
        resizeGrip.Capture = false;
        SettingsStore.Save(settings);
    }

    private void OpenSettingsFile()
    {
        SettingsStore.Save(settings);
        LaunchShortcut(new ShortcutItem { Name = "settings.json", Path = SettingsStore.SettingsPath });
    }

    private void OnDragEnter(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
        {
            e.Effect = DragDropEffects.Copy;
        }
    }

    private void OnDragDrop(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is not string[] paths)
        {
            return;
        }

        foreach (var path in paths.Where(File.Exists))
        {
            AddShortcut(path);
        }
    }
}
