using System;
using System.Runtime.InteropServices;

namespace EarthBackground.Platforms.Linux
{
    internal static class X11NativeWindow
    {
        private const int PropModeReplace = 0;
        private const int AtomFormat = 32;
        private const long CWOverrideRedirect = 1L << 9;

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
                XReparentWindow(display, xid, root, x, y);
                var attributes = new XSetWindowAttributes
                {
                    OverrideRedirect = 1
                };
                XChangeWindowAttributes(display, xid, new IntPtr(CWOverrideRedirect), ref attributes);
                XLowerWindow(display, xid);
                XFlush(display);
            }
            finally
            {
                XCloseDisplay(display);
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
        private static extern int XFlush(IntPtr display);
    }
}
