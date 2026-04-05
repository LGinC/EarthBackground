using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace EarthBackground
{
    public static class AutoStart
    {
        public static bool Set(string key, bool enabled)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return false;

            RegistryKey? runKey = null;
            try
            {
                string path = System.Reflection.Assembly.GetExecutingAssembly().Location;
                // For single-file publish, use process path
                path = System.Environment.ProcessPath ?? path;

                runKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
                if (enabled)
                {
                    runKey?.SetValue(key, path);
                }
                else
                {
                    runKey?.DeleteValue(key, throwOnMissingValue: false);
                }
                return true;
            }
            catch (Exception e)
            {
                Trace.WriteLine(e.Message);
                return false;
            }
            finally
            {
                runKey?.Close();
            }
        }
    }
}
