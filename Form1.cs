using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RoPro
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            roProDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OmniBlox");
            versionsDirectory = Path.Combine(roProDirectory, "Versions");
        }
        private const string DeployHistoryUrl = "http://setup.roblox.com/DeployHistory.txt";
        private readonly string roProDirectory;
        private readonly string versionsDirectory;

        private async void exitBtn_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
            await Task.Delay(1000);
            Application.Exit();
        }


        private async void MainForm_Load(object sender, EventArgs e)
        {
            versionLauncherPanel.Top = 59;
            listBoxVersions.Top = 0;
            string deployUrl = "http://setup.roblox.com/DeployHistory.txt";
            copyfiles();
            versionLauncherPanel.Visible = false;
            listBoxVersions.Visible = false;
            await Task.Delay(200);
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string deployHistory = await client.GetStringAsync(deployUrl);
                    string[] lines = deployHistory.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

                    var windowsVersions = lines
                        .Where(line => line.Contains("WindowsPlayer") && !line.Contains("Studio") && !line.Contains("Mac"))
                        .Select(line =>
                        {
                            var match = System.Text.RegularExpressions.Regex.Match(line, @"version-[a-zA-Z0-9]+");
                            return match.Success ? match.Value : null;
                        })
                        .Where(v => v != null)
                        .Distinct()
                        .Reverse()
                        .ToList();

                    listBoxVersions.Items.Clear();
                    foreach (var version in windowsVersions)
                    {
                        listBoxVersions.Items.Add(version);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to download DeployHistory: " + ex.Message);
            }
        }

        private void launchRoblox_Click(object sender, EventArgs e)
        {

            LoadingScreen loadingScreen = new LoadingScreen(string.Empty);
            loadingScreen.Show();
            loadingScreen.LaunchRobloxDefault();
            this.Hide();
        }

        private async void copyfiles()
        {
            await Task.Run(() =>
            {
                string sourceDirectory = Application.StartupPath;
                foreach (var file in Directory.GetFiles(sourceDirectory))
                {
                    string fileName = Path.GetFileName(file);
                    string destPath = Path.Combine(roProDirectory, fileName);
                    if (!File.Exists(destPath) || !FilesAreEqual(file, destPath))
                    {
                        try
                        {
                            File.Copy(file, destPath, true);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Failed to copy file: {fileName} - {ex.Message}");
                        }
                    }
                }
                foreach (var directory in Directory.GetDirectories(sourceDirectory))
                {
                    string dirName = Path.GetFileName(directory);
                    string destDirPath = Path.Combine(roProDirectory, dirName);
                    if (!Directory.Exists(destDirPath))
                    {
                        try
                        {
                            Directory.CreateDirectory(destDirPath);
                            CopyDirectory(directory, destDirPath);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Failed to copy directory: {dirName} - {ex.Message}");
                        }
                    }
                }
            });
        }

        private void CopyDirectory(string sourceDir, string destDir)
        {
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(destDir, Path.GetFileName(file));
                if (!File.Exists(destFile) || !FilesAreEqual(file, destFile))
                {
                    try
                    {
                        File.Copy(file, destFile, true);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Failed to copy file: {file} - {ex.Message}");
                    }
                }
            }
            foreach (var subdir in Directory.GetDirectories(sourceDir))
            {
                string destSubdir = Path.Combine(destDir, Path.GetFileName(subdir));
                if (!Directory.Exists(destSubdir))
                {
                    try
                    {
                        Directory.CreateDirectory(destSubdir);
                        CopyDirectory(subdir, destSubdir);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Failed to copy subdirectory: {subdir} - {ex.Message}");
                    }
                }
            }
        }

        private bool FilesAreEqual(string file1, string file2)
        {
            FileInfo fi1 = new FileInfo(file1);
            FileInfo fi2 = new FileInfo(file2);
            return fi1.Length == fi2.Length && fi1.LastWriteTime == fi2.LastWriteTime;
        }

        private void guna2Button1_Click(object sender, EventArgs e)
        {
            ClientSettings clientSettings = new ClientSettings();
            clientSettings.Show();
            this.Hide();
        }

        private void guna2Button3_Click(object sender, EventArgs e)
        {
            Credits credits = new Credits();
            credits.Show();
        }

        private void guna2Button2_Click(object sender, EventArgs e)
        {
            try
            {
                string studioPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Roblox",
                    "Versions");
                string[] versionDirs = Directory.GetDirectories(studioPath, "version-*");

                foreach (var dir in versionDirs.OrderByDescending(d => d))
                {
                    string studioExe = Path.Combine(dir, "RobloxStudioBeta.exe");
                    if (File.Exists(studioExe))
                    {
                        System.Diagnostics.Process.Start(studioExe);
                        return;
                    }
                }

                MessageBox.Show("Roblox Studio not found in the official install path.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error launching Roblox Studio: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void guna2Button2_Click_1(object sender, EventArgs e)
        {
            versionLauncherPanel.Visible = true;
        }

        private void customLaunch_Click(object sender, EventArgs e)
        {
            string customVersion = customTextBox.Text.Trim();
            if (string.IsNullOrEmpty(customVersion))
            {
                MessageBox.Show("Please enter a valid version string.");
                return;
            }
            LoadingScreen loadingScreen = new LoadingScreen(string.Empty);
            loadingScreen.Show();
            this.Hide();
            loadingScreen.LaunchRobloxCustomVersion(customVersion);
        }

        private void guna2Button5_Click(object sender, EventArgs e)
        {
            versionLauncherPanel.Visible = false;
        }

        private void guna2Button6_Click(object sender, EventArgs e)
        {
            listBoxVersions.Visible = !listBoxVersions.Visible;
        }



        private void listBoxVersions_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBoxVersions.SelectedItem != null)
            {
                customTextBox.Text = listBoxVersions.SelectedItem.ToString();
                listBoxVersions.Visible = false;
            }
        }

        private void versionLauncherPanel_Paint(object sender, PaintEventArgs e)
        {

        }

        private void guna2Button4_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://discord.gg/xke32deb5h");
        }

        private void guna2Button3_Click_1(object sender, EventArgs e)
        {
            Credits credits = new Credits();
            credits.Show();
        }
    }
}
