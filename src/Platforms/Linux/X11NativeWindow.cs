using System;
using System.Runtime.InteropServices;

namespace EarthBackground.Platforms.Linux
{
    internal static class X11NativeWindow
    {
        private const int PropModeReplace = 0;
        private const int Success = 0;
        private const int AtomFormat = 32;
        private const long CWOverrideRedirect = 1L << 9;
        private static readonly IntPtr XAAtom = new(4);
        private static readonly IntPtr XAWindow = new(33);

        public static void ConfigureAsWallpaperWindow(IntPtr xid, int x, int y, int width, int height)
        {
            if (xid == IntPtr.Zero)
            {
                return;
            }

            var display = XOpenDisplay(IntPtr.Zero);
            if (display == IntPtr.Zero)
            {
                throw new InvalidOperationException("XOpenDisplay failed. Linux dynamic wallpaper requires an X11 session.");
            }

            try
            {
                var root = XDefaultRootWindow(display);
                var desktopWindow = FindDesktopWindow(display, root);
                SetAtomProperty(display, xid, "_NET_WM_WINDOW_TYPE", "_NET_WM_WINDOW_TYPE_DESKTOP");
                SetAtomProperty(
                    display,
                    xid,
                    "_NET_WM_STATE",
                    "_NET_WM_STATE_BELOW",
                    "_NET_WM_STATE_SKIP_TASKBAR",
                    "_NET_WM_STATE_SKIP_PAGER",
                    "_NET_WM_STATE_STICKY");

                XMoveResizeWindow(display, xid, x, y, (uint)Math.Max(width, 1), (uint)Math.Max(height, 1));
                var attributes = new XSetWindowAttributes
                {
                    OverrideRedirect = 1
                };
                XChangeWindowAttributes(display, xid, new IntPtr(CWOverrideRedirect), ref attributes);

                if (desktopWindow != IntPtr.Zero)
                {
                    XReparentWindow(display, xid, root, x, y);
                    var stack = new[] { xid, desktopWindow };
                    var handle = GCHandle.Alloc(stack, GCHandleType.Pinned);
                    try
                    {
                        XRestackWindows(display, handle.AddrOfPinnedObject(), stack.Length);
                    }
                    finally
                    {
                        handle.Free();
                    }
                }
                else
                {
                    XReparentWindow(display, xid, root, x, y);
                    XLowerWindow(display, xid);
                }

                XMapRaised(display, xid);
                XFlush(display);
            }
            finally
            {
                XCloseDisplay(display);
            }
        }

        private static IntPtr FindDesktopWindow(IntPtr display, IntPtr root)
        {
            var clientList = GetWindowArrayProperty(display, root, "_NET_CLIENT_LIST_STACKING");
            if (clientList.Length == 0)
            {
                clientList = GetWindowArrayProperty(display, root, "_NET_CLIENT_LIST");
            }

            for (var i = clientList.Length - 1; i >= 0; i--)
            {
                var window = clientList[i];
                if (window != IntPtr.Zero && HasAtomPropertyValue(display, window, "_NET_WM_WINDOW_TYPE", "_NET_WM_WINDOW_TYPE_DESKTOP"))
                {
                    return window;
                }
            }

            foreach (var window in QueryChildWindows(display, root))
            {
                if (window != IntPtr.Zero && HasAtomPropertyValue(display, window, "_NET_WM_WINDOW_TYPE", "_NET_WM_WINDOW_TYPE_DESKTOP"))
                {
                    return window;
                }
            }

            return IntPtr.Zero;
        }

        private static IntPtr[] QueryChildWindows(IntPtr display, IntPtr window)
        {
            if (!XQueryTree(display, window, out _, out _, out var children, out var childCount) || children == IntPtr.Zero)
            {
                return Array.Empty<IntPtr>();
            }

            try
            {
                var count = checked((int)childCount);
                var values = new IntPtr[count];
                for (var i = 0; i < count; i++)
                {
                    values[i] = Marshal.ReadIntPtr(children, i * IntPtr.Size);
                }

                return values;
            }
            finally
            {
                XFree(children);
            }
        }

        private static bool HasAtomPropertyValue(IntPtr display, IntPtr window, string propertyName, string atomName)
        {
            var expected = XInternAtom(display, atomName, false);
            if (expected == IntPtr.Zero)
            {
                return false;
            }

            var values = GetAtomArrayProperty(display, window, propertyName);
            foreach (var value in values)
            {
                if (value == expected)
                {
                    return true;
                }
            }

            return false;
        }

        private static IntPtr[] GetWindowArrayProperty(IntPtr display, IntPtr window, string propertyName)
            => GetIntPtrArrayProperty(display, window, propertyName, XAWindow);

        private static IntPtr[] GetAtomArrayProperty(IntPtr display, IntPtr window, string propertyName)
            => GetIntPtrArrayProperty(display, window, propertyName, XAAtom);

        private static IntPtr[] GetIntPtrArrayProperty(IntPtr display, IntPtr window, string propertyName, IntPtr expectedType)
        {
            var property = XInternAtom(display, propertyName, true);
            if (property == IntPtr.Zero)
            {
                return Array.Empty<IntPtr>();
            }

            var status = XGetWindowProperty(
                display,
                window,
                property,
                IntPtr.Zero,
                new IntPtr(4096),
                false,
                expectedType,
                out var actualType,
                out var actualFormat,
                out var itemCount,
                out _,
                out var data);

            if (status != Success || data == IntPtr.Zero)
            {
                return Array.Empty<IntPtr>();
            }

            try
            {
                if (actualType != expectedType || actualFormat != AtomFormat || itemCount == UIntPtr.Zero)
                {
                    return Array.Empty<IntPtr>();
                }

                var count = checked((int)itemCount);
                var values = new IntPtr[count];
                for (var i = 0; i < count; i++)
                {
                    values[i] = Marshal.ReadIntPtr(data, i * IntPtr.Size);
                }

                return values;
            }
            finally
            {
                XFree(data);
            }
        }

        private static void SetAtomProperty(IntPtr display, IntPtr window, string propertyName, params string[] atomNames)
        {
            var property = XInternAtom(display, propertyName, false);
            var atomType = XInternAtom(display, "ATOM", false);
            var atoms = new IntPtr[atomNames.Length];
            for (var i = 0; i < atomNames.Length; i++)
            {
                atoms[i] = XInternAtom(display, atomNames[i], false);
            }

            var handle = GCHandle.Alloc(atoms, GCHandleType.Pinned);
            try
            {
                XChangeProperty(
                    display,
                    window,
                    property,
                    atomType,
                    AtomFormat,
                    PropModeReplace,
                    handle.AddrOfPinnedObject(),
                    atoms.Length);
            }
            finally
            {
                handle.Free();
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct XSetWindowAttributes
        {
            public IntPtr BackgroundPixmap;
            public UIntPtr BackgroundPixel;
            public IntPtr BorderPixmap;
            public UIntPtr BorderPixel;
            public int BitGravity;
            public int WinGravity;
            public int BackingStore;
            public UIntPtr BackingPlanes;
            public UIntPtr BackingPixel;
            public int SaveUnder;
            public IntPtr EventMask;
            public IntPtr DoNotPropagateMask;
            public int OverrideRedirect;
            public IntPtr Colormap;
            public IntPtr Cursor;
        }

        [DllImport("libX11.so.6")]
        private static extern IntPtr XOpenDisplay(IntPtr displayName);

        [DllImport("libX11.so.6")]
        private static extern int XCloseDisplay(IntPtr display);

        [DllImport("libX11.so.6")]
        private static extern IntPtr XDefaultRootWindow(IntPtr display);

        [DllImport("libX11.so.6")]
        private static extern bool XQueryTree(
            IntPtr display,
            IntPtr window,
            out IntPtr rootReturn,
            out IntPtr parentReturn,
            out IntPtr childrenReturn,
            out uint nchildrenReturn);

        [DllImport("libX11.so.6")]
        private static extern IntPtr XInternAtom(IntPtr display, string atomName, bool onlyIfExists);

        [DllImport("libX11.so.6")]
        private static extern int XChangeProperty(
            IntPtr display,
            IntPtr window,
            IntPtr property,
            IntPtr type,
            int format,
            int mode,
            IntPtr data,
            int elementCount);

        [DllImport("libX11.so.6")]
        private static extern int XGetWindowProperty(
            IntPtr display,
            IntPtr window,
            IntPtr property,
            IntPtr longOffset,
            IntPtr longLength,
            bool delete,
            IntPtr reqType,
            out IntPtr actualTypeReturn,
            out int actualFormatReturn,
            out UIntPtr nitemsReturn,
            out UIntPtr bytesAfterReturn,
            out IntPtr propReturn);

        [DllImport("libX11.so.6")]
        private static extern int XChangeWindowAttributes(
            IntPtr display,
            IntPtr window,
            IntPtr valueMask,
            ref XSetWindowAttributes attributes);

        [DllImport("libX11.so.6")]
        private static extern int XMoveResizeWindow(
            IntPtr display,
            IntPtr window,
            int x,
            int y,
            uint width,
            uint height);

        [DllImport("libX11.so.6")]
        private static extern int XReparentWindow(
            IntPtr display,
            IntPtr window,
            IntPtr parent,
            int x,
            int y);

        [DllImport("libX11.so.6")]
        private static extern int XLowerWindow(IntPtr display, IntPtr window);

        [DllImport("libX11.so.6")]
        private static extern int XMapRaised(IntPtr display, IntPtr window);

        [DllImport("libX11.so.6")]
        private static extern int XRestackWindows(IntPtr display, IntPtr windows, int nwindows);

        [DllImport("libX11.so.6")]
        private static extern int XFlush(IntPtr display);

        [DllImport("libX11.so.6")]
        private static extern int XFree(IntPtr data);
    }
}
