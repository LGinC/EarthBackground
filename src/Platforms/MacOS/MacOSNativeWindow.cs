using System;
using System.Runtime.InteropServices;

namespace EarthBackground.Platforms.MacOS
{
    internal static class MacOSNativeWindow
    {
        private const long DesktopWallpaperWindowLevel = -2147483623;
        private const ulong BorderlessStyleMask = 0;
        private const ulong CanJoinAllSpaces = 1UL << 0;
        private const ulong Stationary = 1UL << 4;
        private const ulong IgnoresCycle = 1UL << 6;
        private const ulong WallpaperCollectionBehavior = CanJoinAllSpaces | Stationary | IgnoresCycle;

        public static void ConfigureAsWallpaperWindow(IntPtr nsWindow)
        {
            if (nsWindow == IntPtr.Zero)
            {
                return;
            }

            ObjC.Send(nsWindow, "setStyleMask:", BorderlessStyleMask);
            ObjC.Send(nsWindow, "setHasShadow:", false);
            ObjC.Send(nsWindow, "setIgnoresMouseEvents:", true);
            ObjC.Send(nsWindow, "setCollectionBehavior:", WallpaperCollectionBehavior);
            ObjC.Send(nsWindow, "setLevel:", DesktopWallpaperWindowLevel);
            ObjC.Send(nsWindow, "orderBack:", IntPtr.Zero);
        }

        public static bool IsVisible(IntPtr nsWindow)
        {
            if (nsWindow == IntPtr.Zero)
            {
                return true;
            }

            const ulong visible = 1;
            var occlusionState = ObjC.SendUInt64(nsWindow, "occlusionState");
            return (occlusionState & visible) != 0;
        }

        private static class ObjC
        {
            public static void Send(IntPtr receiver, string selector, long value)
            {
                objc_msgSend_int64(receiver, sel_registerName(selector), value);
            }

            public static void Send(IntPtr receiver, string selector, ulong value)
            {
                objc_msgSend_uint64(receiver, sel_registerName(selector), value);
            }

            public static void Send(IntPtr receiver, string selector, bool value)
            {
                objc_msgSend_bool(receiver, sel_registerName(selector), value);
            }

            public static void Send(IntPtr receiver, string selector, IntPtr value)
            {
                objc_msgSend_intptr(receiver, sel_registerName(selector), value);
            }

            public static ulong SendUInt64(IntPtr receiver, string selector)
            {
                return objc_msgSend_return_uint64(receiver, sel_registerName(selector));
            }

            [DllImport("/usr/lib/libobjc.A.dylib")]
            private static extern IntPtr sel_registerName(string name);

            [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
            private static extern void objc_msgSend_int64(IntPtr receiver, IntPtr selector, long value);

            [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
            private static extern void objc_msgSend_uint64(IntPtr receiver, IntPtr selector, ulong value);

            [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
            private static extern void objc_msgSend_bool(IntPtr receiver, IntPtr selector, bool value);

            [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
            private static extern void objc_msgSend_intptr(IntPtr receiver, IntPtr selector, IntPtr value);

            [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
            private static extern ulong objc_msgSend_return_uint64(IntPtr receiver, IntPtr selector);
        }
    }
}
