using Guna.UI2.AnimatorNS;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RoPro
{
    public partial class ClientSettings: Form
    {
        private readonly string roProDirectory;
        private readonly string versionsDirectory;

        private string currentRobloxVersion;
        private string availableRobloxVersion;
        private string versionPath;

        private const string RobloxClientName = "RobloxPlayerBeta.exe";
        private const string VersionUrl = "http://setup.roblox.com/version";
        private const string DeployHistoryUrl = "http://setup.roblox.com/DeployHistory.txt";
        private const string CdnBaseUrl = "https://setup.rbxcdn.com/";

        public ClientSettings()
        {
            InitializeComponent();
            roProDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OmniBlox");
            versionsDirectory = Path.Combine(roProDirectory, "Versions");
            Directory.CreateDirectory(versionsDirectory);
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

        private void TransferBackupFiles()
        {
            string backupFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OmniBlox", "backup_files");

            if (Directory.Exists(backupFolderPath))
            {
                CopyFilesAndDirectories(backupFolderPath, versionPath);
            }
            else
            {
                MessageBox.Show("Backup folder not found: " + backupFolderPath, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CopyFilesAndDirectories(string sourceDir, string destDir)
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
                CopyFilesAndDirectories(directory, destSubDir);
            }
        }
        private AppConfig LoadConfig()
        {
            string configPath = Path.Combine(Application.StartupPath, "config.json");

            if (File.Exists(configPath))
            {
                string json = File.ReadAllText(configPath);
                return JsonConvert.DeserializeObject<AppConfig>(json);
            }

            return new AppConfig();
        }

        private async Task<int?> LoadFpsSetting()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string basePath = Path.Combine(appData, "OmniBlox");

            string modPath = Path.Combine(basePath, "Modifications", "ClientSettings", "ClientAppSettings.json");
            int? foundFps = null;

            try
            {
                string latestVersion = await getversion();
                string versionPath = Path.Combine(basePath, "versions", latestVersion, "ClientSettings", "ClientAppSettings.json");

                foreach (var path in new[] { modPath, versionPath })
                {
                    if (File.Exists(path))
                    {
                        string json = File.ReadAllText(path);
                        if (string.IsNullOrWhiteSpace(json)) continue;

                        var jObj = Newtonsoft.Json.Linq.JObject.Parse(json);
                        if (jObj["DFIntTaskSchedulerTargetFps"] != null)
                        {
                            foundFps = (int?)jObj["DFIntTaskSchedulerTargetFps"];
                            break;
                        }
                    }
                }
            }
            catch
            {
            }

            return foundFps;
        }
        private AppConfig config;

        private async void ClientSettings_Load(object sender, EventArgs e)
        {
            int? loadedFps = await LoadFpsSetting();
            if (loadedFps.HasValue)
                guna2TextBox1.Text = loadedFps.Value.ToString();
            this.Location = new Point(
                (Screen.PrimaryScreen.WorkingArea.Width - this.Width) / 2,
                (Screen.PrimaryScreen.WorkingArea.Height - this.Height) / 2
            );
            LoadCursorImage();

            try

            {
                copyfiles();
                config = LoadConfig();
                currentRobloxVersion = await getcurrentversion();
                availableRobloxVersion = await getversion();
                versionPath = Path.Combine(versionsDirectory, availableRobloxVersion);
                if (!File.Exists(Path.Combine(versionPath, RobloxClientName)))
                {
                    await downloadRoblox(availableRobloxVersion);
                    await extractRoblox(availableRobloxVersion);
                    TransferBackupFiles();
                }
            }
            catch (Exception)
            {
            }
        }
        private void LoadCursorImage()
        {
            try
            {
                string modCursorFolder = Path.Combine(roProDirectory, "Modifications", "content", "textures", "Cursors", "KeyboardMouse");
                string arrowCursorPath = Path.Combine(modCursorFolder, "ArrowCursor.png");
                if (File.Exists(arrowCursorPath))
                {
                    using (Image img = Image.FromFile(arrowCursorPath))
                    {
                        cursorImage.Image = new Bitmap(img);
                        cursorImage.SizeMode = PictureBoxSizeMode.StretchImage;
                    }
                }
                else
                {
                    MessageBox.Show("Cursor image not found in the Modifications folder.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading cursor image: " + ex.Message);
            }
        }


        private async Task downloadRoblox(string versionId)
        {
            string zipUrl = $"{CdnBaseUrl}{versionId}-RobloxApp.zip";
            string zipPath = Path.Combine(versionsDirectory, $"{versionId}.zip");
            using (WebClient webClient = new WebClient())
            {
                webClient.DownloadProgressChanged += (sender, e) =>
                {
                };
                await webClient.DownloadFileTaskAsync(zipUrl, zipPath);
            }
        }

        private async Task extractRoblox(string versionId)
        {
            string zipPath = Path.Combine(versionsDirectory, $"{versionId}.zip");
            string extractPath = Path.Combine(versionsDirectory, versionId);
            Directory.CreateDirectory(extractPath);
            ZipFile.ExtractToDirectory(zipPath, extractPath);
            await Task.Delay(200);
            File.Delete(zipPath);
        }

        private async void exitBtn_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
            await Task.Delay(100);
            Application.Exit();
        }

        private void guna2TextBox1_TextChanged(object sender, EventArgs e)
        {
            UpdateFpsFFlag(guna2TextBox1.Text);
        }

        public class AppConfig
        {
            public int DFIntTaskSchedulerTargetFps { get; set; }
        }


        private async void UpdateFpsFFlag(string input)
        {
            if (!int.TryParse(input, out int fps)) return;

            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string basePath = Path.Combine(appData, "OmniBlox");

            string modPath = Path.Combine(basePath, "Modifications", "ClientSettings", "ClientAppSettings.json");

            try
            {
                string latestVersion = await getversion();
                string versionPath = Path.Combine(basePath, "Versions", latestVersion, "ClientSettings", "ClientAppSettings.json");

                foreach (string path in new[] { modPath, versionPath })
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path));

                    if (!File.Exists(path) || string.IsNullOrWhiteSpace(File.ReadAllText(path)))
                    {
                        File.WriteAllText(path, "{}");
                    }

                    string json = File.ReadAllText(path);
                    Newtonsoft.Json.Linq.JObject jObj;

                    try
                    {
                        jObj = Newtonsoft.Json.Linq.JObject.Parse(json);
                    }
                    catch
                    {
                        jObj = new Newtonsoft.Json.Linq.JObject();
                    }

                    jObj["DFIntTaskSchedulerTargetFps"] = fps;
                    File.WriteAllText(path, jObj.ToString(Newtonsoft.Json.Formatting.Indented));
                }
            }
            catch (Exception)
            {
                
            }
        }



        private void guna2Button1_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("explorer.exe", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OmniBlox"));
        }

        private void guna2Button2_Click(object sender, EventArgs e)
        {
            MainForm mainform = new MainForm();
            mainform.Show();
            this.Hide();
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {

        }

        private void guna2Button3_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "PNG files (*.png)|*.png";
                openFileDialog.Title = "Select Cursor PNG";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string selectedImagePath = openFileDialog.FileName;
                    Image originalImage = Image.FromFile(selectedImagePath);
                    Image resizedImage = ResizeImage(originalImage, 40, 40);
                    string modCursorFolder = Path.Combine(roProDirectory, "Modifications", "content", "textures", "Cursors", "KeyboardMouse");
                    Directory.CreateDirectory(modCursorFolder);
                    resizedImage.Save(Path.Combine(modCursorFolder, "ArrowCursor.png"), ImageFormat.Png);
                    resizedImage.Save(Path.Combine(modCursorFolder, "ArrowFarCursor.png"), ImageFormat.Png);
                    foreach (string versionFolder in Directory.GetDirectories(Path.Combine(roProDirectory, "versions"), "version-*"))
                    {
                        string cursorTargetFolder = Path.Combine(versionFolder, "content", "textures", "Cursors", "KeyboardMouse");
                        Directory.CreateDirectory(cursorTargetFolder);
                        resizedImage.Save(Path.Combine(cursorTargetFolder, "ArrowCursor.png"), ImageFormat.Png);
                        resizedImage.Save(Path.Combine(cursorTargetFolder, "ArrowFarCursor.png"), ImageFormat.Png);
                    }
                    cursorImage.Image = resizedImage;
                    cursorImage.SizeMode = PictureBoxSizeMode.StretchImage;
                }
            }
        }
        private Image ResizeImage(Image image, int width, int height)
        {
            Bitmap resizedImage = new Bitmap(image, new Size(width, height));
            return resizedImage;
        }

        private void guna2Button4_Click(object sender, EventArgs e)
        {
            try
            {
                string backupFolder = Path.Combine(roProDirectory, "backup_files", "content", "textures", "Cursors", "KeyboardMouse");
                string arrowCursor = Path.Combine(backupFolder, "ArrowCursor.png");
                string arrowFarCursor = Path.Combine(backupFolder, "ArrowFarCursor.png");

                if (!File.Exists(arrowCursor) || !File.Exists(arrowFarCursor))
                {
                    return;
                }
                foreach (string versionFolder in Directory.GetDirectories(Path.Combine(roProDirectory, "versions"), "version-*"))
                {
                    string cursorTargetFolder = Path.Combine(versionFolder, "content", "textures", "Cursors", "KeyboardMouse");
                    Directory.CreateDirectory(cursorTargetFolder);

                    File.Copy(arrowCursor, Path.Combine(cursorTargetFolder, "ArrowCursor.png"), true);
                    File.Copy(arrowFarCursor, Path.Combine(cursorTargetFolder, "ArrowFarCursor.png"), true);
                }
                cursorImage.Image = Image.FromFile(arrowCursor);
                cursorImage.SizeMode = PictureBoxSizeMode.StretchImage;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to restore cursors: " + ex.Message);
            }
        }
    }
}
