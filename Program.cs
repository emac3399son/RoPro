using Microsoft.Win32;
using System;
using System.IO;
using System.Windows.Forms;
using IWshRuntimeLibrary;
using File = System.IO.File;

namespace RoPro
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                string roProPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OmniBlox", "OmniBlox.exe");
                if (File.Exists(roProPath))
                {
                    using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\roblox-player"))
                    {
                        if (key != null)
                        {
                            key.SetValue("", "URL:Roblox Protocol");
                            key.SetValue("URL Protocol", "");
                            using (RegistryKey commandKey = key.CreateSubKey(@"shell\open\command"))
                            {
                                commandKey.SetValue("", $"\"{roProPath}\" \"%1\"");
                            }
                        }
                    }
                    CreateStartMenuShortcut(roProPath);
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                if (args.Length > 0 && args[0].StartsWith("roblox-player:"))
                {
                    string gameUrl = args[0];
                    LoadingScreen loadingScreen = new LoadingScreen(gameUrl);
                    loadingScreen.LaunchRobloxDefault();
                    Application.Run(loadingScreen);
                }
                else
                {
                    Application.Run(new MainForm());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unhandled exception: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void CreateStartMenuShortcut(string roProPath)
        {
            try
            {
                string startMenuPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
                    "Programs",
                    "OmniBlox.lnk"
                );

                if (!File.Exists(startMenuPath))
                {
                    var shell = new IWshRuntimeLibrary.WshShell();
                    IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(startMenuPath);
                    shortcut.TargetPath = roProPath;
                    shortcut.WorkingDirectory = Path.GetDirectoryName(roProPath);
                    shortcut.Description = "OmniBlox Launcher";
                    shortcut.Save();

                    Console.WriteLine("Start menu shortcut created successfully.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to create start menu shortcut:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

    }
}
