using System;
using System.IO;
using System.IO.Compression;
using System.Diagnostics;

namespace ZeroHub.Helper
{
    public static class Driver
    {
        //TODO: Change to Inf customization style install
        static Driver()
        {
            Profile = TapProfile.Default;
        }

        public static TapProfile Profile
        {
            get; private set;
        }

        public static void SetDriver(TapProfile DriverProfile,byte[] DriverZipFile)
        {
            Profile = DriverProfile;
            Driver.DriverZipFile = DriverZipFile;
        }

        public static void Install()
        {
            Extract();
            Execute("install OemVista.inf "+Profile.TapName);
        }

        public static void Update()
        {
            Extract();
            Execute("update OemVista.inf " + Profile.TapName);
        }

        public static void Uninstall()
        {
            Extract();
            Execute("remove " + Profile.TapName);
        }

        public static void Extract()
        {
            Directory.CreateDirectory(Profile.DriverPath);
            ZipArchive files = new ZipArchive(new MemoryStream(DriverZipFile), ZipArchiveMode.Read);
            foreach (ZipArchiveEntry entry in files.Entries)
                entry.ExtractToFile(Path.Combine(Profile.DriverPath, entry.FullName), true);
        }

        private static void Execute(string Command)
        {
            var process = new Process();
            process.EnableRaisingEvents = true;
            process.StartInfo = new ProcessStartInfo(Path.Combine(Profile.DriverPath + "tapinstall.exe"));
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.Verb = "runas";
            process.StartInfo.Arguments = Command;
            process.StartInfo.WorkingDirectory = Profile.DriverPath;
            process.Start();
            process.WaitForExit();
        }

        private static byte[] DriverZipFile = Environment.Is64BitOperatingSystem ? DriverFiles.amd64 : DriverFiles.i386;
    }

    public static class Cmd
    {
        public static string Execute(string Command,bool Hide = true)
        {
            var process = new Process();
            process.StartInfo.WindowStyle = Hide?ProcessWindowStyle.Hidden:ProcessWindowStyle.Normal;
            process.StartInfo.RedirectStandardOutput = Hide;
            process.StartInfo.UseShellExecute = !Hide;
            process.StartInfo.CreateNoWindow = Hide;
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = "/C " + Command;
            process.Start();
            process.WaitForExit();
            return Hide?process.StandardOutput.ReadToEnd():"";
        }

        public static void ExecuteAdmin(string Command)
        {
            var process = new Process();
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.UseShellExecute = true;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Verb = "runas";
            process.StartInfo.Arguments = "/C " + Command;
            process.Start();
            process.WaitForExit();
        }
    }
}
