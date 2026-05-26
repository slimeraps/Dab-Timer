using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DabTimerInstaller
{
    internal static class Program
    {
        private const string ProductName = "Dab Timer";
        private const string ProductVersion = "1.0";
        private const string Publisher = "slimeraps";
        private const string AppResourceName = "DabTimerInstaller.Payload.Dab Timer.exe";
        private const string AppExecutableName = "Dab Timer.exe";
        private const string UninstallerName = "Uninstall Dab Timer.exe";
        private const string ShortcutName = "Dab Timer.lnk";
        private const string UninstallRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\DabTimer";
        private const int MoveFileDelayUntilReboot = 0x00000004;

        [STAThread]
        private static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            bool silent = HasArgument(args, "/silent");

            try
            {
                if (HasArgument(args, "/uninstall"))
                {
                    Uninstall(silent);
                    return;
                }

                Install(silent);
            }
            catch (Exception ex)
            {
                Environment.ExitCode = 1;
                if (silent)
                {
                    File.WriteAllText(Path.Combine(Path.GetTempPath(), "DabTimerSetup.log"), ex.ToString());
                }
                else
                {
                    MessageBox.Show(ex.Message, ProductName + " Setup", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool MoveFileEx(string existingFileName, string newFileName, int flags);

        private static void Install(bool silent)
        {
            string installDirectory = GetInstallDirectory();
            Directory.CreateDirectory(installDirectory);

            string appPath = Path.Combine(installDirectory, AppExecutableName);
            ExtractResource(AppResourceName, appPath);

            string uninstallerPath = Path.Combine(installDirectory, UninstallerName);
            File.Copy(Assembly.GetExecutingAssembly().Location, uninstallerPath, true);

            CreateShortcut(GetStartMenuShortcutPath(), appPath, installDirectory);
            CreateShortcut(GetDesktopShortcutPath(), appPath, installDirectory);
            WriteUninstallRegistry(installDirectory, appPath, uninstallerPath);

            if (silent)
            {
                return;
            }

            DialogResult result = MessageBox.Show(
                "Dab Timer was installed successfully.\n\nLaunch it now?",
                ProductName + " Setup",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);

            if (result == DialogResult.Yes)
            {
                Process.Start(appPath);
            }
        }

        private static void Uninstall(bool silent)
        {
            if (!silent)
            {
                DialogResult result = MessageBox.Show(
                    "Remove Dab Timer from this computer?",
                    ProductName + " Uninstall",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result != DialogResult.Yes)
                {
                    return;
                }
            }

            DeleteIfExists(GetStartMenuShortcutPath());
            DeleteIfExists(GetDesktopShortcutPath());

            using (RegistryKey root = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64))
            using (RegistryKey key = root.OpenSubKey(UninstallRegistryKey, true))
            {
                if (key != null)
                {
                    root.DeleteSubKeyTree(UninstallRegistryKey);
                }
            }

            string installDirectory = GetInstallDirectory();
            string appPath = Path.Combine(installDirectory, AppExecutableName);
            string uninstallerPath = Path.Combine(installDirectory, UninstallerName);
            DeleteIfExists(appPath);
            MoveFileEx(uninstallerPath, null, MoveFileDelayUntilReboot);

            try
            {
                Directory.Delete(installDirectory, false);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }

            if (!silent)
            {
                MessageBox.Show(
                    "Dab Timer was removed.",
                    ProductName + " Uninstall",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }

        private static string GetInstallDirectory()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Programs",
                "Dab Timer");
        }

        private static string GetStartMenuShortcutPath()
        {
            string directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
                "Programs",
                "Dab Timer");
            Directory.CreateDirectory(directory);
            return Path.Combine(directory, ShortcutName);
        }

        private static string GetDesktopShortcutPath()
        {
            string desktopDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            if (!string.IsNullOrEmpty(desktopDirectory) && Directory.Exists(desktopDirectory))
            {
                return Path.Combine(desktopDirectory, ShortcutName);
            }

            string oneDrive = Environment.GetEnvironmentVariable("OneDrive");
            if (!string.IsNullOrEmpty(oneDrive))
            {
                string oneDriveDesktop = Path.Combine(oneDrive, "Desktop");
                if (Directory.Exists(oneDriveDesktop))
                {
                    return Path.Combine(oneDriveDesktop, ShortcutName);
                }
            }

            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Desktop",
                ShortcutName);
        }

        private static void ExtractResource(string resourceName, string outputPath)
        {
            using (Stream input = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                if (input == null)
                {
                    throw new InvalidOperationException("Missing installer payload: " + resourceName);
                }

                using (FileStream output = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
                {
                    input.CopyTo(output);
                }
            }
        }

        private static void CreateShortcut(string shortcutPath, string targetPath, string workingDirectory)
        {
            Type shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType == null)
            {
                return;
            }

            object shell = Activator.CreateInstance(shellType);
            object shortcut = shellType.InvokeMember(
                "CreateShortcut",
                BindingFlags.InvokeMethod,
                null,
                shell,
                new object[] { shortcutPath });

            Type shortcutType = shortcut.GetType();
            shortcutType.InvokeMember("TargetPath", BindingFlags.SetProperty, null, shortcut, new object[] { targetPath });
            shortcutType.InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, shortcut, new object[] { workingDirectory });
            shortcutType.InvokeMember("IconLocation", BindingFlags.SetProperty, null, shortcut, new object[] { targetPath + ",0" });
            shortcutType.InvokeMember("Description", BindingFlags.SetProperty, null, shortcut, new object[] { ProductName });
            shortcutType.InvokeMember("Save", BindingFlags.InvokeMethod, null, shortcut, null);
        }

        private static void WriteUninstallRegistry(string installDirectory, string appPath, string uninstallerPath)
        {
            using (RegistryKey root = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64))
            using (RegistryKey key = root.CreateSubKey(UninstallRegistryKey))
            {
                if (key == null)
                {
                    return;
                }

                key.SetValue("DisplayName", ProductName);
                key.SetValue("DisplayVersion", ProductVersion);
                key.SetValue("Publisher", Publisher);
                key.SetValue("InstallLocation", installDirectory);
                key.SetValue("DisplayIcon", appPath);
                key.SetValue("UninstallString", "\"" + uninstallerPath + "\" /uninstall");
                key.SetValue("NoModify", 1, RegistryValueKind.DWord);
                key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
                key.SetValue("EstimatedSize", EstimateInstalledSizeKb(installDirectory), RegistryValueKind.DWord);
            }
        }

        private static int EstimateInstalledSizeKb(string directory)
        {
            long bytes = 0;
            foreach (string file in Directory.GetFiles(directory, "*", SearchOption.AllDirectories))
            {
                bytes += new FileInfo(file).Length;
            }

            return (int)Math.Max(1, bytes / 1024);
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private static bool HasArgument(string[] args, string expected)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], expected, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
