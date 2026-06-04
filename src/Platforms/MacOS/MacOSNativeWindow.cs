using System;
using System.Runtime.InteropServices;

namespace EarthBackground.Platforms.MacOS
{
    internal static class MacOSNativeWindow
    {
        private const ulong BorderlessStyleMask = 0;
        private const ulong CanJoinAllSpaces = 1UL << 0;
        private const ulong Stationary = 1UL << 4;
        private const ulong IgnoresCycle = 1UL << 6;
        private const ulong WallpaperCollectionBehavior = CanJoinAllSpaces | Stationary | IgnoresCycle;
        private const int DesktopWindowLevelKey = 2;

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
            ObjC.Send(nsWindow, "setLevel:", GetDesktopWindowLevel());
            ObjC.Send(nsWindow, "orderFront:", IntPtr.Zero);
        }

        public static bool IsVisible(IntPtr nsWindow)
        {
            return nsWindow != IntPtr.Zero;
        }

        private static long GetDesktopWindowLevel()
        {
            return CGWindowLevelForKey(DesktopWindowLevelKey);
        }

        [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
        private static extern int CGWindowLevelForKey(int key);

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

        }
    }
}
