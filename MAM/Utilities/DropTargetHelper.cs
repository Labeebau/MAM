using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("4657278B-411B-11d2-839A-00C04FD918D0")]
interface IDropTargetHelper
{
    void DragEnter(IntPtr hwndTarget, IDataObject pDataObject, ref POINT ppt, int effect);
    void DragLeave();
    void DragOver(ref POINT ppt, int effect);
    void Drop(IDataObject pDataObject, ref POINT ppt, int effect);
    void Show(bool fShow);
}

[ComImport]
[Guid("4657278A-411B-11d2-839A-00C04FD918D0")]
class DragDropHelper
{
}

[StructLayout(LayoutKind.Sequential)]
struct POINT
{
    public int X;
    public int Y;
}

static class NativeMethods
{
    [DllImport("user32.dll")]
    public static extern bool GetCursorPos(out POINT lpPoint);
}
[Flags]
public enum DragDropEffects
{
    None = 0,
    Copy = 1,
    Move = 2,
    Link = 4,
    Scroll = unchecked((int)0x80000000),
    All = Copy | Move | Link | Scroll
}
