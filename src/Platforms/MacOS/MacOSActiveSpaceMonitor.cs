using System;
using System.Runtime.InteropServices;

namespace EarthBackground.Platforms.MacOS
{
    internal sealed class MacOSActiveSpaceMonitor : IDisposable
    {
        private const int KCFStringEncodingUTF8 = 0x08000100;
        private const int DeliverImmediately = 4;
        private const string ActiveSpaceNotification = "com.apple.spaces.activeSpaceDidChange";

        private readonly CFNotificationCallback _callback;
        private readonly IntPtr _notificationName;
        private readonly IntPtr _center;
        private bool _disposed;

        public MacOSActiveSpaceMonitor()
        {
            _callback = OnNotification;
            _center = CFNotificationCenterGetDistributedCenter();
            _notificationName = CFStringCreateWithCString(IntPtr.Zero, ActiveSpaceNotification, KCFStringEncodingUTF8);

            if (_center != IntPtr.Zero && _notificationName != IntPtr.Zero)
            {
                CFNotificationCenterAddObserver(
                    _center,
                    IntPtr.Zero,
                    _callback,
                    _notificationName,
                    IntPtr.Zero,
                    DeliverImmediately);
            }
        }

        public event Action? ActiveSpaceChanged;

        private void OnNotification(IntPtr center, IntPtr observer, IntPtr name, IntPtr obj, IntPtr userInfo)
        {
            ActiveSpaceChanged?.Invoke();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (_center != IntPtr.Zero)
            {
                CFNotificationCenterRemoveObserver(_center, IntPtr.Zero, _notificationName, IntPtr.Zero);
            }

            if (_notificationName != IntPtr.Zero)
            {
                CFRelease(_notificationName);
            }
        }

        private delegate void CFNotificationCallback(
            IntPtr center,
            IntPtr observer,
            IntPtr name,
            IntPtr obj,
            IntPtr userInfo);

        [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
        private static extern IntPtr CFNotificationCenterGetDistributedCenter();

        [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
        private static extern void CFNotificationCenterAddObserver(
            IntPtr center,
            IntPtr observer,
            CFNotificationCallback callback,
            IntPtr name,
            IntPtr obj,
            int suspensionBehavior);

        [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
        private static extern void CFNotificationCenterRemoveObserver(
            IntPtr center,
            IntPtr observer,
            IntPtr name,
            IntPtr obj);

        [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
        private static extern IntPtr CFStringCreateWithCString(
            IntPtr allocator,
            string value,
            int encoding);

        [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
        private static extern void CFRelease(IntPtr value);
    }
}
