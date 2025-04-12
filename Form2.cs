using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RoPro
{
    public partial class LoadingScreen : Form
    {
        private const string RobloxClientName = "RobloxPlayerBeta.exe";
        private const string VersionUrl = "http://setup.roblox.com/version";
        private const string DeployHistoryUrl = "http://setup.roblox.com/DeployHistory.txt";
        private const string CdnBaseUrl = "https://setup.rbxcdn.com/";

        private readonly string roProDirectory;
        private readonly string versionsDirectory;

        private string currentRobloxVersion;
        private string availableRobloxVersion;
        private string versionPath;
        private string gameJoinUrl;

        public LoadingScreen(string gameUrl)
        {
            InitializeComponent();
            roProDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OmniBlox");
            versionsDirectory = Path.Combine(roProDirectory, "Versions");
            Directory.CreateDirectory(versionsDirectory);
            gameJoinUrl = gameUrl;
        }

        private async void LoadingScreen_Load(object sender, EventArgs e)
        {
            this.Location = new Point(
                (Screen.PrimaryScreen.WorkingArea.Width - this.Width) / 2,
                (Screen.PrimaryScreen.WorkingArea.Height - this.Height) / 2
            );
            await Task.Delay(500);
            StartPosition = FormStartPosition.CenterScreen;
            copyfiles();
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

        private async Task<string> getcurrentversion()
        {
            using (HttpClient client = new HttpClient())
            {
                return (await client.GetStringAsync(VersionUrl)).Trim();
            }
        }

        private async Task<string> getversion()
        {
            using (HttpClient client = new HttpClient())
            {
                string deployText = await client.GetStringAsync(DeployHistoryUrl);
                string[] lines = deployText.Split('\n');
                for (int i = lines.Length - 1; i >= 0; i--)
                {
                    if (lines[i].Contains("WindowsPlayer"))
                    {
                        Match match = Regex.Match(lines[i], @"version-\S+");
                        if (match.Success) return match.Value.Trim();
                    }
                }
            }
            throw new Exception("Could not find a valid available Roblox version.");
        }

        private async Task downloadRoblox(string versionId)
        {
            string zipUrl = $"{CdnBaseUrl}{versionId}-RobloxApp.zip";
            string zipPath = Path.Combine(versionsDirectory, $"{versionId}.zip");
            using (WebClient webClient = new WebClient())
            {
                webClient.DownloadProgressChanged += (sender, e) =>
                {
                    guna2ProgressBar1.Style = ProgressBarStyle.Blocks;
                    guna2ProgressBar1.Value = e.ProgressPercentage;
                };
                await webClient.DownloadFileTaskAsync(zipUrl, zipPath);
            }
        }

        private async Task extractRoblox(string versionId)
        {
            guna2ProgressBar1.Style = ProgressBarStyle.Marquee;
            string zipPath = Path.Combine(versionsDirectory, $"{versionId}.zip");
            string extractPath = Path.Combine(versionsDirectory, versionId);
            Directory.CreateDirectory(extractPath);
            ZipFile.ExtractToDirectory(zipPath, extractPath);
            await Task.Delay(200);
            File.Delete(zipPath);
        }


        private void TransferBackupFiles()
        {
            string backupFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OmniBlox", "backup_files");

            if (Directory.Exists(backupFolderPath))
            {
                CfileD(backupFolderPath, versionPath);
            }
            else
            {
                MessageBox.Show("Backup folder not found: " + backupFolderPath, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void TransferBackupFilesC(string versionId)
        {
            string backupFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OmniBlox", "backup_files");
            string targetPath = Path.Combine(versionsDirectory, versionId);

            if (Directory.Exists(backupFolderPath))
            {
                CfileD(backupFolderPath, targetPath);
            }
            else
            {
                MessageBox.Show("Backup folder not found: " + backupFolderPath, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void CfileD(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(destDir, Path.GetFileName(file));
                try
                {
                    File.Copy(file, destFile, true);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to copy file: {file} - {ex.Message}");
                }
            }
            foreach (var directory in Directory.GetDirectories(sourceDir))
            {
                string destSubDir = Path.Combine(destDir, Path.GetFileName(directory));
                CfileD(directory, destSubDir);
            }
        }
        private void applymods()
        {
            try
            {
                string modClientSettingsPath = Path.Combine(roProDirectory, "Modifications", "ClientSettings");
                string targetClientSettingsPath = Path.Combine(versionPath, "ClientSettings");

                if (Directory.Exists(modClientSettingsPath))
                {
                    CopyDirectory(modClientSettingsPath, targetClientSettingsPath);
                }
                string modCursorFolder = Path.Combine(roProDirectory, "Modifications", "content", "textures", "Cursors", "KeyboardMouse");

                if (Directory.Exists(modCursorFolder))
                {
                    string[] cursorFiles = { "ArrowCursor.png", "ArrowFarCursor.png" };
                    foreach (string cursorFile in cursorFiles)
                    {
                        string moddedCursorPath = Path.Combine(modCursorFolder, cursorFile);
                        if (File.Exists(moddedCursorPath))
                        {
                            string targetCursorPath = Path.Combine(versionPath, "content", "textures", "Cursors", "KeyboardMouse");
                            Directory.CreateDirectory(targetCursorPath);
                            File.Copy(moddedCursorPath, Path.Combine(targetCursorPath, cursorFile), true);
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }


        public async void LaunchRobloxDefault()
        {
            StartPosition = FormStartPosition.CenterScreen;
            label1.Text = "";
            copyfiles();

            try
            {
                currentRobloxVersion = await getcurrentversion();
                availableRobloxVersion = await getversion();
                versionPath = Path.Combine(versionsDirectory, availableRobloxVersion);

                if (Directory.Exists(versionPath) && File.Exists(Path.Combine(versionPath, RobloxClientName)))
                {
                    applymods();
                    label1.Text = "";
                }
                else
                {
                    label1.Left = 220;
                    label1.Text = "Downloading RobloxApp.zip...";
                    await downloadRoblox(availableRobloxVersion);
                    await extractRoblox(availableRobloxVersion);
                    TransferBackupFiles();
                    await Task.Delay(2000);
                    applymods();
                    label1.Text = "";
                }

                launch(gameJoinUrl);
                await Task.Delay(2000);
                Hide();
                await Task.Delay(1000);
                Environment.Exit(0);
                await Task.Delay(400);
                Application.Exit();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        public async void LaunchRobloxCustomVersion(string version)
        {
            StartPosition = FormStartPosition.CenterScreen;
            label1.Text = "";
            copyfiles();

            try
            {
                versionPath = Path.Combine(versionsDirectory, version);

                if (Directory.Exists(versionPath) && File.Exists(Path.Combine(versionPath, RobloxClientName)))
                {
                    applymods();
                    label1.Text = "";
                }
                else
                {
                    label1.Left = 182;
                    label1.Text = $"Downloading {version}...";
                    await downloadRoblox(version);
                    await Task.Delay(1000);
                    await extractRoblox(version);
                    await Task.Delay(1000);
                    TransferBackupFilesC(version);
                    await Task.Delay(2000);
                    applymods();
                    label1.Text = "";
                }

                launchRobloxC(version);
                await Task.Delay(2000);
                Hide();
                await Task.Delay(1000);
                Environment.Exit(0);
                await Task.Delay(400);
                Application.Exit();
            }
            catch (Exception ex)
            {
                label1.Text = "An error occurred.";
                MessageBox.Show($"Error: {ex.Message}");
            }
        }
        private async void launch(string url)
        {
            string exePath = Path.Combine(versionPath, RobloxClientName);
            if (!File.Exists(exePath))
            {
                label1.Left = 220;
                label1.Text = "Downloading RobloxApp.zip...";
                await downloadRoblox(availableRobloxVersion);
                await extractRoblox(availableRobloxVersion);
                TransferBackupFiles();
                await Task.Delay(2000);
                applymods();
                label1.Text = "";
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = url,
                UseShellExecute = true,
                WorkingDirectory = versionPath
            });
            await Task.Delay(2000);
            Hide();
            await Task.Delay(1000);
            Environment.Exit(0);
            await Task.Delay(400);
            Application.Exit();
        }

        private void launchRobloxC(string versionid)
        {
            string versionPath = Path.Combine(versionsDirectory, versionid);
            string exePath = Path.Combine(versionPath, RobloxClientName);

            if (!File.Exists(exePath))
            {
                throw new FileNotFoundException("Roblox not installed", exePath);
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = true,
                WorkingDirectory = versionPath
            });
        }

        private async void exitBtn_Click(object sender, EventArgs e)
        {
            try
            {
                foreach (var process in Process.GetProcessesByName("RobloxPlayerBeta"))
                {
                    process.Kill();
                }
                foreach (var process in Process.GetProcessesByName("eurotrucks2"))
                {
                    process.Kill();
                }
                Environment.Exit(0);
                await Task.Delay(1000);
                Application.Exit();
            }
            catch (Exception)
            {
            }
        }
    }
}
