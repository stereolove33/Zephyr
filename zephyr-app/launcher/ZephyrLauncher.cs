using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Windows.Forms;
using System.Reflection;
[assembly: AssemblyTitle("Zephyr")]
[assembly: AssemblyProduct("Zephyr")]
[assembly: AssemblyFileVersion("1.0.1.0")]
[assembly: AssemblyVersion("1.0.1.0")]
internal static class ZephyrLauncher
{
    static string Hash(string file)
    {
        using (var stream = File.OpenRead(file))
        using (var sha = SHA256.Create())
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
    }

    static void CopyIfChanged(string source, string target)
    {
        if (!File.Exists(target) || Hash(source) != Hash(target)) File.Copy(source, target, true);
    }

    [STAThread]
    static int Main(string[] args)
    {
        try
        {
            string root = AppDomain.CurrentDomain.BaseDirectory;
            var matches = Directory.GetDirectories(root).Where(d => File.Exists(Path.Combine(d, "skinfusion-customs.exe")) && File.Exists(Path.Combine(d, "SkinFusion.Menu.dll"))).ToArray();
            if (matches.Length != 1) throw new IOException("Exactly one runtime folder is required. Extract the complete Zephyr package first.");
            string runtime = matches[0];
            string injector = Path.Combine(root, "resources", "originals", "ZephyrCore.exe");
            if (Hash(injector) != "e49ce3ab78ca0e04a7a8a3b9d69091b0deadd03a25ce218c605863aeaf40b7ff") throw new IOException("The original application failed its integrity check.");
            bool original = args.Contains("--original-menu");
            string game = Path.Combine(root, "resources", original ? "originals" : "customized", "R3nzSkin.dll");
            string expected = original ? "af04b42974bb152dc712d01f137e7f82feca20fbb2d650ee1d8ad7be0f412d6e" : "d9d32bd59d6fbcd154ebec71679667fcfaa28e4a56e31ad21c6a00e3553321ca";
            if (Hash(game) != expected) throw new IOException("The menu failed its integrity check.");

            CopyIfChanged(injector, Path.Combine(runtime, "Zephyr.exe"));
            CopyIfChanged(game, Path.Combine(runtime, "R3nzSkin.dll"));
            CopyIfChanged(Path.Combine(root, "resources", "ZephyrCore.config"), Path.Combine(runtime, "Zephyr.exe.config"));
            if (args.Contains("--verify")) return 0;
            Process.Start(new ProcessStartInfo(Path.Combine(runtime, "Zephyr.exe")) { WorkingDirectory = runtime, UseShellExecute = true });
            return 0;
        }
        catch (Exception error)
        {
            if (!args.Contains("--verify")) MessageBox.Show(error.Message, "Zephyr", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }
    }
}
