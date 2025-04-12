using Microsoft.Win32;
using System;
using System.Windows.Forms;

public static class ProtocolHandler
{
    public static void Register()
    {
        try
        {
            string exePath = Application.ExecutablePath;

            RegistryKey key = Registry.ClassesRoot.CreateSubKey("roblox-player");
            key.SetValue("", "URL:Roblox Protocol");
            key.SetValue("URL Protocol", "");

            RegistryKey command = key.CreateSubKey(@"shell\open\command");
            command.SetValue("", $"\"{exePath}\" \"%1\"");
        }
        catch (Exception ex)
        {
            MessageBox.Show("Failed to register protocol: " + ex.Message);
        }
    }
}
