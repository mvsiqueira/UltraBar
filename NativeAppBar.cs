using System.Runtime.InteropServices;

namespace UltraBar;

internal sealed class NativeAppBar
{
    private const int ABM_NEW = 0x00000000;
    private const int ABM_REMOVE = 0x00000001;
    private const int ABM_QUERYPOS = 0x00000002;
    private const int ABM_SETPOS = 0x00000003;

    private const int ABE_LEFT = 0;
    private const int ABE_TOP = 1;
    private const int ABE_RIGHT = 2;
    private const int ABE_BOTTOM = 3;

    private readonly Form owner;
    private bool registered;

    public NativeAppBar(Form owner)
    {
        this.owner = owner;
    }

    public void Register()
    {
        if (registered || owner.Handle == IntPtr.Zero)
        {
            return;
        }

        var data = CreateData();
        SHAppBarMessage(ABM_NEW, ref data);
        registered = true;
    }

    public void Unregister()
    {
        if (!registered)
        {
            return;
        }

        var data = CreateData();
        SHAppBarMessage(ABM_REMOVE, ref data);
        registered = false;
    }

    public void SetPosition(DockEdge edge, int thickness)
    {
        Register();

        var screenBounds = Screen.FromControl(owner).Bounds;
        var data = CreateData();
        data.uEdge = ToNativeEdge(edge);
        data.rc = ToRect(screenBounds);

        switch (edge)
        {
            case DockEdge.Left:
                data.rc.Right = data.rc.Left + thickness;
                break;
            case DockEdge.Right:
                data.rc.Left = data.rc.Right - thickness;
                break;
            case DockEdge.Top:
                data.rc.Bottom = data.rc.Top + thickness;
                break;
            case DockEdge.Bottom:
                data.rc.Top = data.rc.Bottom - thickness;
                break;
        }

        SHAppBarMessage(ABM_QUERYPOS, ref data);

        switch (edge)
        {
            case DockEdge.Left:
                data.rc.Right = data.rc.Left + thickness;
                break;
            case DockEdge.Right:
                data.rc.Left = data.rc.Right - thickness;
                break;
            case DockEdge.Top:
                data.rc.Bottom = data.rc.Top + thickness;
                break;
            case DockEdge.Bottom:
                data.rc.Top = data.rc.Bottom - thickness;
                break;
        }

        SHAppBarMessage(ABM_SETPOS, ref data);
        owner.Bounds = Rectangle.FromLTRB(data.rc.Left, data.rc.Top, data.rc.Right, data.rc.Bottom);
    }

    private APPBARDATA CreateData()
    {
        return new APPBARDATA
        {
            cbSize = Marshal.SizeOf<APPBARDATA>(),
            hWnd = owner.Handle
        };
    }

    private static int ToNativeEdge(DockEdge edge)
    {
        return edge switch
        {
            DockEdge.Left => ABE_LEFT,
            DockEdge.Right => ABE_RIGHT,
            DockEdge.Bottom => ABE_BOTTOM,
            _ => ABE_TOP
        };
    }

    private static RECT ToRect(Rectangle rectangle)
    {
        return new RECT
        {
            Left = rectangle.Left,
            Top = rectangle.Top,
            Right = rectangle.Right,
            Bottom = rectangle.Bottom
        };
    }

    [DllImport("shell32.dll", CallingConvention = CallingConvention.StdCall)]
    private static extern IntPtr SHAppBarMessage(int dwMessage, ref APPBARDATA pData);

    [StructLayout(LayoutKind.Sequential)]
    private struct APPBARDATA
    {
        public int cbSize;
        public IntPtr hWnd;
        public int uCallbackMessage;
        public int uEdge;
        public RECT rc;
        public IntPtr lParam;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}
