using System;
using System.Diagnostics;
using System.Windows.Forms;
using Microsoft.Win32;

namespace EarthBackground
{
    public static class AutoStart
    {
        public static bool Set(string key, bool enabled)
        {
            RegistryKey? runKey = null;
            try
            {
                string path = Application.ExecutablePath;
                runKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
                runKey?.SetValue(key, path);
                if (!enabled)
                {
                    runKey?.DeleteValue(key);
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
                if (runKey != null)
                {
                    runKey.Close();
                }
            }
        }
    }
}
