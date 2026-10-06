using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

[assembly: AssemblyVersion("1.0.1.0")]

namespace SkinFusion
{
    [System.Security.SecurityCritical]
    public sealed class MenuDomainManager : AppDomainManager
    {
        [System.Security.SecurityCritical]
        public override void InitializeNewDomain(AppDomainSetup setup)
        {
            base.InitializeNewDomain(setup);
            try
            {
                if (!AppDomain.CurrentDomain.IsDefaultAppDomain()) return;
                Log.Write("Extensao carregada no PID " + Process.GetCurrentProcess().Id);
                TaskbarBranding.SetProcessIdentity();
                ZephyrBranding.Install();
                Application.ApplicationExit += delegate { ZephyrBranding.Remove(); };
                RenameConfig.Install();
                Application.Idle += AttachMenu;
                if (Environment.GetEnvironmentVariable("SKINFUSION_CLASSIC") != "1") EarlyWindow.Install();
            }
            catch (Exception error)
            {
                Log.Write(error.ToString());
            }
        }

        private static void AttachMenu(object sender, EventArgs args)
        {
            try
            {
                foreach (Form form in Application.OpenForms)
                {
                    if (form.IsDisposed || !form.IsHandleCreated || form.InvokeRequired) continue;
                    if (!String.Equals(form.GetType().Name, "R3nzUI", StringComparison.Ordinal)) continue;

                    Application.Idle -= AttachMenu;
                    EarlyWindow.Remove();
                    TaskbarBranding.Apply(form);
                    if (Environment.GetEnvironmentVariable("SKINFUSION_CLASSIC") == "1")
                        new CustomsMenu(form);
                    else
                    {
                        try { AttachZephyr(form); }
                        catch (Exception error)
                        {
                            Log.Write("Zephyr: " + error);
                            StartupCover.Remove(form);
                            new CustomsMenu(form);
                        }
                    }
                    return;
                }
            }
            catch (Exception error)
            {
                Application.Idle -= AttachMenu;
                Log.Write(error.ToString());
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        internal static void AttachZephyr(Form form)
        {
            new ZephyrMenu(form);
        }
    }


    internal static class TaskbarBranding
    {
        internal const string AppId = "stereolove33.Zephyr";
        private static readonly Guid propertyFormat = new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3");
        [StructLayout(LayoutKind.Sequential)]
        internal struct PropertyKey { internal Guid format; internal uint id; }
        [StructLayout(LayoutKind.Explicit, Size = 24)]
        internal struct PropertyValue
        {
            [FieldOffset(0)] internal ushort type;
            [FieldOffset(8)] internal IntPtr text;
        }
        [ComImport, Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface PropertyStore
        {
            [PreserveSig] int GetCount(out uint count);
            [PreserveSig] int GetAt(uint index, out PropertyKey key);
            [PreserveSig] int GetValue(ref PropertyKey key, out PropertyValue value);
            [PreserveSig] int SetValue(ref PropertyKey key, ref PropertyValue value);
            [PreserveSig] int Commit();
        }
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SetCurrentProcessExplicitAppUserModelID(string appId);
        [DllImport("shell32.dll")]
        internal static extern int SHGetPropertyStoreForWindow(IntPtr window, ref Guid iid, out PropertyStore store);
        [DllImport("ole32.dll")] internal static extern int PropVariantClear(ref PropertyValue value);

        internal static void SetProcessIdentity()
        {
            try { Marshal.ThrowExceptionForHR(SetCurrentProcessExplicitAppUserModelID(AppId)); }
            catch (Exception error) { Log.Write("Taskbar identity: " + error.Message); }
        }

        internal static string LauncherPath(string runtime)
        {
            string root = Directory.GetParent(runtime.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)).FullName;
            return Path.Combine(root, "Zephyr.exe");
        }

        private static void Set(PropertyStore store, uint id, string text)
        {
            var key = new PropertyKey { format = propertyFormat, id = id };
            var value = new PropertyValue { type = 31, text = Marshal.StringToCoTaskMemUni(text) };
            try { Marshal.ThrowExceptionForHR(store.SetValue(ref key, ref value)); }
            finally { Marshal.FreeCoTaskMem(value.text); }
        }

        internal static void ApplyToWindow(IntPtr window, string launcher)
        {
            PropertyStore store;
            Guid iid = typeof(PropertyStore).GUID;
            Marshal.ThrowExceptionForHR(SHGetPropertyStoreForWindow(window, ref iid, out store));
            try
            {
                // Pin the stable launcher, rather than the renamed original host.
                Set(store, 2, "\"" + launcher + "\"");
                Set(store, 4, "Zephyr");
                Set(store, 3, launcher + ",0");
                Set(store, 5, AppId);
                Marshal.ThrowExceptionForHR(store.Commit());
            }
            finally { Marshal.ReleaseComObject(store); }
        }

        internal static void Apply(Form form)
        {
            try
            {
                string launcher = LauncherPath(AppDomain.CurrentDomain.BaseDirectory);
                if (!File.Exists(launcher)) return;
                ApplyToWindow(form.Handle, launcher);
            }
            catch (Exception error) { Log.Write("Taskbar branding: " + error.Message); }
        }
    }

    // Thread-local observation of the original window, before WM_SHOWWINDOW is handled.
    // No game process or injection entry point is changed.
    internal static class EarlyWindow
    {
        private delegate IntPtr HookProc(int code, IntPtr wParam, IntPtr lParam);
        private static readonly HookProc callback = Observe;
        private static IntPtr hook;
        [StructLayout(LayoutKind.Sequential)]
        private struct WindowMessage { internal IntPtr lParam; internal IntPtr wParam; internal uint message; internal IntPtr hwnd; }
        [DllImport("user32.dll", SetLastError = true)] private static extern IntPtr SetWindowsHookEx(int kind, HookProc proc, IntPtr module, uint thread);
        [DllImport("user32.dll")] private static extern bool UnhookWindowsHookEx(IntPtr handle);
        [DllImport("user32.dll")] private static extern IntPtr CallNextHookEx(IntPtr handle, int code, IntPtr wParam, IntPtr lParam);
        [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
        internal static void Install()
        {
            hook = SetWindowsHookEx(4, callback, IntPtr.Zero, GetCurrentThreadId());
            if (hook == IntPtr.Zero) Log.Write("Early window hook unavailable; using Idle fallback.");
            Application.ApplicationExit += delegate { Remove(); };
        }
        internal static void Remove() { if (hook != IntPtr.Zero) { UnhookWindowsHookEx(hook); hook = IntPtr.Zero; } }
        private static IntPtr Observe(int code, IntPtr wParam, IntPtr lParam)
        {
            IntPtr next = CallNextHookEx(hook, code, wParam, lParam);
            if (code < 0) return next;
            try
            {
                WindowMessage message = (WindowMessage)Marshal.PtrToStructure(lParam, typeof(WindowMessage));
                if (message.message != 0x0018 || message.wParam == IntPtr.Zero) return next;
                Form form = Control.FromHandle(message.hwnd) as Form;
                if (form == null || form.GetType().Name != "R3nzUI" || form.InvokeRequired) return next;
                Remove();
                // Cover synchronously; the original controls stay enabled underneath.
                StartupCover.Install(form);
                TaskbarBranding.Apply(form);
                // Attach through Idle after the original Show/Load sequence has completed.
            }
            catch (Exception error) { Log.Write("Early window: " + error); }
            return next;
        }
    }

    internal sealed class StartupPanel : Panel
    {
        private Image logo;
        private readonly System.Windows.Forms.Timer progressTimer = new System.Windows.Forms.Timer { Interval = 30 };
        private readonly System.Diagnostics.Stopwatch shown = System.Diagnostics.Stopwatch.StartNew();
        internal int ElapsedMilliseconds { get { return (int)Math.Min(Int32.MaxValue, shown.ElapsedMilliseconds); } }
        internal StartupPanel()
        {
            DoubleBuffered = true;
            progressTimer.Tick += delegate { Invalidate(); };
            progressTimer.Start();
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ui", "zephyr-icon.png");
                if (File.Exists(path)) using (var source = Image.FromFile(path)) logo = new Bitmap(source);
            }
            catch (Exception error) { Log.Write("Startup logo: " + error.Message); }
        }
        protected override void OnPaint(PaintEventArgs args)
        {
            base.OnPaint(args);
            float dpiScale = args.Graphics.DpiX / 96f;
            if (logo != null)
            {
                RectangleF bounds = LogoBounds(ClientSize, logo.Size, args.Graphics.DpiX);
                args.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                args.Graphics.DrawImage(logo, bounds);
            }
            var bar = new RectangleF(ClientSize.Width / 2f - 60f * dpiScale, ClientSize.Height / 2f + 90f * dpiScale, 120f * dpiScale, 2f * dpiScale);
            using (var track = new SolidBrush(Color.FromArgb(37, 37, 37))) args.Graphics.FillRectangle(track, bar);
            bar.Width *= Math.Min(1f, ElapsedMilliseconds / 1500f);
            args.Graphics.FillRectangle(Brushes.White, bar);
        }
        internal static RectangleF LogoBounds(Size area, Size image, float dpi)
        {
            float box = Math.Min(144f * dpi / 96f, Math.Min(area.Width, area.Height) / 2f);
            float scale = Math.Min(box / image.Width, box / image.Height);
            float width = image.Width * scale, height = image.Height * scale;
            return new RectangleF((area.Width - width) / 2f, (area.Height - height) / 2f, width, height);
        }
        protected override void OnResize(EventArgs args) { base.OnResize(args); Invalidate(); }
        protected override void Dispose(bool disposing)
        {
            if (disposing) progressTimer.Dispose();
            if (disposing && logo != null) { logo.Dispose(); logo = null; }
            base.Dispose(disposing);
        }
    }

    internal static class StartupCover
    {
        [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr window, IntPtr after, int x, int y, int width, int height, uint flags);
        internal static void Front(Form form)
        {
            foreach (Control cover in form.Controls.Find("ZephyrStartup", false))
                SetWindowPos(cover.Handle, IntPtr.Zero, 0, 0, 0, 0, 0x0013);
        }
        internal static int Elapsed(Form form)
        {
            Control[] found = form.Controls.Find("ZephyrStartup", false);
            return found.Length == 1 && found[0] is StartupPanel ? ((StartupPanel)found[0]).ElapsedMilliseconds : 0;
        }
        internal static Panel Install(Form form)
        {
            Control[] found = form.Controls.Find("ZephyrStartup", false);
            if (found.Length != 0) return (Panel)found[0];
            var cover = new StartupPanel { Name = "ZephyrStartup", BackColor = Color.Black, Dock = DockStyle.None, Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right, Bounds = form.ClientRectangle, TabStop = false };
            form.Controls.Add(cover);
            cover.BringToFront();
            form.Text = "Zephyr";
            string icon = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ui", "zephyr.ico");
            if (File.Exists(icon)) using (var loaded = new Icon(icon)) form.Icon = (Icon)loaded.Clone();
            return cover;
        }
        internal static void Remove(Form form)
        {
            Control[] found = form.Controls.Find("ZephyrStartup", false);
            foreach (Control cover in found) { form.Controls.Remove(cover); cover.Dispose(); }
        }
    }


    internal static class ZephyrBranding
    {
        private delegate IntPtr HookProc(int code, IntPtr wParam, IntPtr lParam);
        private delegate bool EnumProc(IntPtr hwnd, IntPtr parameter);
        private static readonly HookProc callback = Observe;
        private static readonly object iconGate = new object();
        private static Icon dialogIcon;
        [ThreadStatic] private static IntPtr hook;
        [DllImport("user32.dll", SetLastError = true)] private static extern IntPtr SetWindowsHookEx(int kind, HookProc proc, IntPtr module, uint thread);
        [DllImport("user32.dll")] private static extern bool UnhookWindowsHookEx(IntPtr handle);
        [DllImport("user32.dll")] private static extern IntPtr CallNextHookEx(IntPtr handle, int code, IntPtr wParam, IntPtr lParam);
        [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(IntPtr hwnd, StringBuilder text, int capacity);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(IntPtr hwnd, StringBuilder text, int capacity);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern bool SetWindowText(IntPtr hwnd, string text);
        [DllImport("user32.dll")] private static extern bool EnumChildWindows(IntPtr hwnd, EnumProc callback, IntPtr parameter);
        [DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr hwnd, int index);
        [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")] private static extern IntPtr GetWindow(IntPtr hwnd, uint command);
        private static Icon smallDialogIcon;
        private static void ReplaceDialogIcon(IntPtr dialog)
        {
            lock (iconGate)
            {
                if (dialogIcon == null)
                {
                    string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ui", "zephyr.ico");
                    if (!File.Exists(path)) return;
                    dialogIcon = new Icon(path, new Size(32, 32));
                    smallDialogIcon = new Icon(path, new Size(16, 16));
                }
                // WM_SETICON changes the window/taskbar icon. The warning symbol is untouched.
                SendMessage(dialog, 0x0080, IntPtr.Zero, smallDialogIcon.Handle);
                SendMessage(dialog, 0x0080, new IntPtr(1), dialogIcon.Handle);
                IntPtr owner = GetWindow(dialog, 4);
                if (owner != IntPtr.Zero)
                {
                    SendMessage(owner, 0x0080, IntPtr.Zero, smallDialogIcon.Handle);
                    SendMessage(owner, 0x0080, new IntPtr(1), dialogIcon.Handle);
                }
            }
        }
        internal static void Install()
        {
            if (hook != IntPtr.Zero) return;
            hook = SetWindowsHookEx(5, callback, IntPtr.Zero, GetCurrentThreadId());
            if (hook == IntPtr.Zero) Log.Write("Zephyr: dialog branding hook unavailable.");
        }
        internal static void Remove() { if (hook != IntPtr.Zero) { UnhookWindowsHookEx(hook); hook = IntPtr.Zero; } }
        internal static string Text(string text)
        {
            if (text == null) return null;
            if (text.IndexOf("already running", StringComparison.OrdinalIgnoreCase) >= 0 &&
                System.Text.RegularExpressions.Regex.IsMatch(text, @"R3nz|Renz|SkinFusion", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                return "Zephyr is already running.";
            text = text.Replace("Customs paradas.", "Custom skins stopped.");
            text = text.Replace("Preparando customs", "Preparing custom skins");
            text = text.Replace("Pasta do aplicativo indisponivel.", "Application folder unavailable.");
            text = text.Replace("LOCALAPPDATA indisponivel.", "LOCALAPPDATA unavailable.");
            text = text.Replace("Configuracao invalida:", "Invalid configuration:");
            text = text.Replace("Pare as customs antes de alterar a biblioteca ou a pasta do jogo.", "Stop custom skins before changing the library or game folder.");
            text = text.Replace("Parando customs...", "Stopping custom skins...");
            text = text.Replace("Marque pelo menos uma custom skin antes de iniciar.", "Select at least one custom skin before starting.");
            text = text.Replace("Arquivo necessario ausente:", "Required file missing:");
            text = text.Replace("Falha interna no nucleo de customs.", "Internal custom skins component error.");
            text = text.Replace("Customs prontas. Aguardando a partida.", "Custom skins ready. Waiting for a match.");
            text = text.Replace("Partida encontrada. Carregando customs...", "Match found. Loading custom skins...");
            text = text.Replace("Modulo de customs carregado. Aguardando confirmacao.", "Custom skins module loaded. Waiting for confirmation.");
            text = text.Replace("Customs aplicadas nesta partida.", "Custom skins applied to this match.");
            text = text.Replace("Estado das customs:", "Custom skins status:");
            text = text.Replace("O LTK rejeitou arquivos da custom:", "The custom skins component rejected files:");
            text = text.Replace("Canal do LTK indisponivel.", "Custom skins communication unavailable.");
            text = text.Replace("Pedido invalido:", "Invalid request:");
            text = text.Replace("Caminho invalido:", "Invalid path:");
            text = text.Replace("Aguarde a sincronizacao das tabelas e clique em Verificar customs novamente.", "Wait for hash table synchronization, then click Check customs again.");
            text = text.Replace("A selecao de customs mudou. Atualize a lista e tente novamente.", "The custom skin selection changed. Refresh the list and try again.");
            text = text.Replace("Nao foi possivel ler ", "Unable to read ");
            return System.Text.RegularExpressions.Regex.Replace(text,
                @"R3nzSkin Injector|R3nzSkin|R3nz|Renz|SkinFusion", "Zephyr", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }
        internal static object TranslateMessages(object value)
        {
            var map = value as Dictionary<string, object>;
            if (map != null)
            {
                foreach (string key in new List<string>(map.Keys))
                {
                    if ((key == "message" || key == "error" || key == "recommendation") && map[key] is string)
                        map[key] = Text((string)map[key]);
                    else TranslateMessages(map[key]);
                }
            }
            else if (!(value is string))
            {
                var list = value as System.Collections.IEnumerable;
                if (list != null) foreach (object item in list) TranslateMessages(item);
            }
            return value;
        }

        private static void Rename(IntPtr hwnd)
        {
            var buffer = new StringBuilder(8192);
            GetWindowText(hwnd, buffer, buffer.Capacity);
            string before = buffer.ToString();
            string after = Text(before);
            if (before != after) SetWindowText(hwnd, after);
        }
        private static IntPtr Observe(int code, IntPtr wParam, IntPtr lParam)
        {
            if (code == 5)
            {
                try
                {
                    var kind = new StringBuilder(256);
                    GetClassName(wParam, kind, kind.Capacity);
                    if (kind.ToString() == "#32770")
                    {
                        var title = new StringBuilder(512);
                        GetWindowText(wParam, title, title.Capacity);
                        bool branded = title.ToString() == "Zephyr" || Text(title.ToString()) != title.ToString();
                        Rename(wParam);
                        EnumChildWindows(wParam, delegate(IntPtr child, IntPtr unused) { Rename(child); return true; }, IntPtr.Zero);
                        if (branded) ReplaceDialogIcon(wParam);
                    }
                }
                catch (Exception error) { Log.Write("Zephyr dialog: " + error); }
            }
            return CallNextHookEx(hook, code, wParam, lParam);
        }
    }

    internal static class Log
    {
        private static readonly object Gate = new object();

        internal static void Write(string message)
        {
            try
            {
                lock (Gate)
                {
                    string folder = Path.Combine(Environment.GetFolderPath(
                        Environment.SpecialFolder.LocalApplicationData), "SkinFusionReverse");
                    Directory.CreateDirectory(folder);
                    File.AppendAllText(Path.Combine(folder, "menu.log"),
                        DateTime.Now.ToString("s") + " " + message + Environment.NewLine);
                }
            }
            catch { }
        }
    }

    internal static class RenameConfig
    {
        private static FileSystemWatcher watcher;
        private static byte[] configuration;

        internal static void Install()
        {
            string directory = AppDomain.CurrentDomain.BaseDirectory;
            string path = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;
            configuration = File.ReadAllBytes(path);
            watcher = new FileSystemWatcher(directory, "*.exe");
            watcher.NotifyFilter = NotifyFilters.FileName;
            watcher.Renamed += delegate(object sender, RenamedEventArgs args)
            {
                try
                {
                    // O R3nz renomeia seu proprio executavel. A configuracao acompanha
                    // somente uma copia que ainda corresponde ao binario preservado.
                    string original = Path.Combine(directory, "..", "resources", "originals", "ZephyrCore.exe");
                    if (!File.Exists(original) || !SameFile(original, args.FullPath)) return;
                    File.WriteAllBytes(args.FullPath + ".config", configuration);
                    Log.Write("Configuracao acompanhou " + args.Name);
                }
                catch (Exception error) { Log.Write(error.ToString()); }
            };
            watcher.EnableRaisingEvents = true;
            Application.ApplicationExit += delegate { watcher.Dispose(); };
        }

        private static bool SameFile(string original, string copy)
        {
            using (var hash = System.Security.Cryptography.SHA256.Create())
            using (var left = File.OpenRead(original))
            using (var right = File.OpenRead(copy))
            {
                string a = Convert.ToBase64String(hash.ComputeHash(left));
                string b = Convert.ToBase64String(hash.ComputeHash(right));
                return a == b;
            }
        }
    }

    internal sealed class Backend : IDisposable
    {
        private readonly object gate = new object();
        private readonly JavaScriptSerializer serializer = new JavaScriptSerializer { MaxJsonLength = Int32.MaxValue };
        private Process process;
        private StreamWriter input;
        private bool disposed;

        internal Dictionary<string, object> Call(object request)
        {
            object data = CallValue(request);
            if (data == null) return new Dictionary<string, object>();
            var map = data as Dictionary<string, object>;
            if (map == null) throw new InvalidDataException("Unexpected custom skins response.");
            return map;
        }

        internal object CallValue(object request)
        {
            lock (gate)
            {
                if (disposed) throw new ObjectDisposedException("Customs");
                EnsureStarted();
                input.WriteLine(serializer.Serialize(request));
                input.Flush();
                string line = process.StandardOutput.ReadLine();
                if (line == null) throw new IOException("The custom skins component closed. See menu.log.");

                var result = serializer.Deserialize<Dictionary<string, object>>(line);
                if (!Convert.ToBoolean(result["ok"])) throw new InvalidOperationException(
                    ZephyrBranding.Text(Convert.ToString(result["error"])));
                object data;
                if (!result.TryGetValue("data", out data) || data == null)
                    return null;

                return ZephyrBranding.TranslateMessages(data);
            }
        }

        private void EnsureStarted()
        {
            if (process != null && !process.HasExited) return;
            if (process != null) process.Dispose();
            string executable = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "skinfusion-customs.exe");
            if (Environment.GetEnvironmentVariable("SKINFUSION_MENU_TEST") == "1")
                executable = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestBackend.exe");
            if (!File.Exists(executable)) throw new FileNotFoundException(
                "Missing skinfusion-customs.exe. Run BUILD.cmd.", executable);

            var info = new ProcessStartInfo(executable);
            info.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;
            info.UseShellExecute = false;
            info.CreateNoWindow = true;
            info.RedirectStandardInput = true;
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = true;
            info.StandardOutputEncoding = Encoding.UTF8;
            info.StandardErrorEncoding = Encoding.UTF8;
            process = new Process();
            process.StartInfo = info;
            process.ErrorDataReceived += delegate(object sender, DataReceivedEventArgs args)
            {
                if (!String.IsNullOrEmpty(args.Data)) Log.Write("LTK: " + args.Data);
            };
            process.Start();
            input = new StreamWriter(process.StandardInput.BaseStream, new UTF8Encoding(false));
            process.BeginErrorReadLine();
            Log.Write("Nucleo de customs iniciado, PID " + process.Id);
        }

        public void Dispose()
        {
            lock (gate)
            {
                disposed = true;
                if (process == null) return;
                try
                {
                    if (!process.HasExited)
                    {
                        input.Close();
                        if (!process.WaitForExit(10000))
                            Log.Write("O nucleo ainda esta finalizando as customs.");
                    }
                }
                catch (Exception error) { Log.Write(error.ToString()); }
                process.Dispose();
                process = null;
            }
        }
    }

    internal sealed class ModRow
    {
        internal string Id;
        internal string Name;
        internal bool Enabled;
        internal object[] Layers;
        public override string ToString() { return Name; }
    }

    internal sealed class CustomsMenu
    {
        private readonly Form form;
        private readonly Backend backend = new Backend();
        private readonly Button originalStart;
        private readonly CheckedListBox mods = new CheckedListBox();
        private readonly Label pathLabel = new Label();
        private readonly Label statusLabel = new Label();
        private readonly Button start = new Button();
        private readonly Button stop = new Button();
        private readonly Button import = new Button();
        private readonly Button remove = new Button();
        private readonly Button layers = new Button();
        private readonly Button folder = new Button();
        private readonly System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
        private bool loading;
        private bool busy;
        private bool polling;
        private bool running;
        private bool pendingOriginalStart;
        private bool startedOriginal;
        private bool closed;

        internal CustomsMenu(Form original)
        {
            form = original;
            Control[] found = form.Controls.Find("startButton", true);
            originalStart = found.Length == 1 ? found[0] as Button : null;
            if (originalStart == null) Log.Write("Botao Start original nao localizado.");

            form.SuspendLayout();
            int left = form.ClientSize.Width;
            int height = Math.Max(452, form.ClientSize.Height);
            var panel = new Panel();
            panel.Name = "SkinFusionCustoms";
            panel.Location = new Point(left + 8, 26);
            panel.Size = new Size(450, height - 34);
            panel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            panel.ForeColor = Color.FromArgb(235, 235, 235);
            panel.BackColor = form.BackColor;

            var title = new Label();
            title.Text = "Custom skins";
            title.Font = new Font(form.Font.FontFamily, 12, FontStyle.Bold);
            title.SetBounds(8, 3, 426, 26);
            pathLabel.Text = "Select the League of Legends folder.";
            pathLabel.AutoEllipsis = true;
            pathLabel.SetBounds(8, 36, 300, 24);
            Setup(folder, "League folder", 318, 31, 120);
            mods.SetBounds(8, 70, 430, height - 237);
            mods.Name = "SkinFusionModList";
            mods.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            mods.CheckOnClick = true;
            mods.BackColor = Color.FromArgb(30, 30, 30);
            mods.ForeColor = panel.ForeColor;

            int row = height - 160;
            Setup(import, "Add", 8, row, 138);
            Setup(remove, "Remove", 154, row, 138);
            Setup(layers, "Layers", 300, row, 138);
            Setup(start, "Start Zephyr + custom skins", 8, row + 38, 284);
            Setup(stop, "Stop custom skins", 300, row + 38, 138);
            statusLabel.SetBounds(8, row + 76, 430, 50);
            statusLabel.Name = "SkinFusionCustomsStatus";
            statusLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            statusLabel.Text = "Loading custom skins library...";

            panel.Controls.AddRange(new Control[] { title, pathLabel, folder, mods,
                import, remove, layers, start, stop, statusLabel });
            folder.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            form.MaximumSize = Size.Empty;
            form.ClientSize = new Size(left + 466, height);
            form.Controls.Add(panel);
            form.ResumeLayout();

            import.Click += ImportMods;
            remove.Click += RemoveMod;
            layers.Click += ChooseLayers;
            folder.Click += ChooseFolder;
            start.Click += Start;
            stop.Click += Stop;
            mods.ItemCheck += ToggleMod;
            form.FormClosed += Closed;
            Application.ApplicationExit += Closed;
            timer.Interval = 1000;
            timer.Tick += Poll;
            timer.Start();
            SetEnabled();
            Reload();
            Log.Write("Customs adicionadas a janela original " + form.GetType().FullName);
        }

        private static void Setup(Button button, string text, int x, int y, int width)
        {
            button.Text = text;
            button.SetBounds(x, y, width, 30);
            button.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            button.BackColor = Color.FromArgb(50, 50, 50);
            button.ForeColor = Color.White;
            button.FlatStyle = FlatStyle.Flat;
        }

        private void SetEnabled()
        {
            bool editable = !busy && !running;
            import.Enabled = remove.Enabled = layers.Enabled = folder.Enabled = mods.Enabled = editable;
            start.Enabled = editable;
            stop.Enabled = running && !busy;
        }

        private void OnUi(Action action)
        {
            if (closed || form.IsDisposed || !form.IsHandleCreated) return;
            try { form.BeginInvoke(action); }
            catch (InvalidOperationException) { }
        }

        private void Request(object request, Action<Dictionary<string, object>> done)
        {
            if (busy || closed) return;
            busy = true;
            SetEnabled();
            Task.Run(delegate
            {
                try
                {
                    var result = backend.Call(request);
                    OnUi(delegate
                    {
                        busy = false;
                        try { if (done != null) done(result); }
                        catch (Exception error) { ShowError(error); }
                        SetEnabled();
                    });
                }
                catch (Exception error)
                {
                    Log.Write(error.ToString());
                    OnUi(delegate
                    {
                        busy = false;
                        pendingOriginalStart = false;
                        ShowError(error);
                        SetEnabled();
                    });
                }
            });
        }

        private void ShowError(Exception error)
        {
            statusLabel.Text = error.Message;
            MessageBox.Show(form, error.Message, "Custom skins", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void Reload()
        {
            Request(new { op = "list" }, delegate(Dictionary<string, object> result)
            {
                loading = true;
                try
                {
                    mods.Items.Clear();
                    foreach (object item in ReadArray(result["mods"]))
                    {
                        var data = (Dictionary<string, object>)item;
                        string display = Convert.ToString(data["displayName"]);
                        var row = new ModRow();
                        row.Id = Convert.ToString(data["id"]);
                        row.Name = String.IsNullOrEmpty(display) ? Convert.ToString(data["name"]) : display;
                        row.Enabled = Convert.ToBoolean(data["enabled"]);
                        row.Layers = ReadArray(data["layers"]);
                        mods.Items.Add(row, row.Enabled);
                    }
                    object league;
                    pathLabel.Text = result.TryGetValue("leaguePath", out league) && league != null
                        ? Convert.ToString(league) : "Select the League of Legends folder.";
                }
                finally { loading = false; }
            });
        }

        private static object[] ReadArray(object value)
        {
            var items = value as System.Collections.IEnumerable;
            if (items == null || value is string) throw new InvalidDataException("Invalid custom skins list.");
            var result = new List<object>();
            foreach (object item in items) result.Add(item);
            return result.ToArray();
        }

        private async void ImportMods(object sender, EventArgs args)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = "Custom skins|*.modpkg;*.fantome;*.zip|Todos os arquivos|*.*";
                dialog.Multiselect = true;
                DialogResult chosen;
                try { chosen = await ShowFileDialog(dialog, form.Handle); }
                catch (Exception error) { ShowError(error); return; }
                if (chosen != DialogResult.OK || closed) return;
                Request(new { op = "import", paths = dialog.FileNames }, delegate(Dictionary<string, object> result)
                {
                    object[] failures = ReadArray(result["failed"]);
                    if (failures.Length > 0)
                    {
                        var text = new StringBuilder();
                        foreach (object failure in failures)
                        {
                            var data = (Dictionary<string, object>)failure;
                            text.AppendLine(Convert.ToString(data["fileName"]) + ": " + data["message"]);
                        }
                        MessageBox.Show(form, text.ToString(), "Files not imported");
                    }
                    Reload();
                });
            }
        }

        private void ToggleMod(object sender, ItemCheckEventArgs args)
        {
            if (loading) return;
            if (busy || running)
            {
                args.NewValue = args.CurrentValue;
                return;
            }
            ModRow row = (ModRow)mods.Items[args.Index];
            bool enabled = args.NewValue == CheckState.Checked;
            args.NewValue = args.CurrentValue;
            Request(new { op = "toggle", id = row.Id, enabled = enabled }, delegate { Reload(); });
        }

        private void RemoveMod(object sender, EventArgs args)
        {
            ModRow row = mods.SelectedItem as ModRow;
            if (row == null) return;
            if (MessageBox.Show(form, "Remove " + row.Name + " from the library?", "Custom skins",
                MessageBoxButtons.YesNo) != DialogResult.Yes) return;
            Request(new { op = "remove", id = row.Id }, delegate { Reload(); });
        }

        private async void ChooseFolder(object sender, EventArgs args)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Selecione League of Legends ou sua pasta Game.";
                if (Directory.Exists(pathLabel.Text)) dialog.SelectedPath = pathLabel.Text;
                DialogResult chosen;
                try { chosen = await ShowFileDialog(dialog, form.Handle); }
                catch (Exception error) { ShowError(error); return; }
                if (chosen != DialogResult.OK || closed) return;
                Request(new { op = "set_league", path = dialog.SelectedPath }, delegate { Reload(); });
            }
        }

        private sealed class WindowOwner : IWin32Window
        {
            private readonly IntPtr handle;
            internal WindowOwner(IntPtr value) { handle = value; }
            public IntPtr Handle { get { return handle; } }
        }

        private static Task<DialogResult> ShowFileDialog(CommonDialog dialog, IntPtr owner)
        {
            if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
                return Task.FromResult(dialog.ShowDialog(new WindowOwner(owner)));

            // O executavel C++/.NET original pode usar MTA. Somente o seletor
            // de arquivos usa STA, sem alterar a thread original do injetor.
            var completion = new TaskCompletionSource<DialogResult>();
            var thread = new Thread(delegate()
            {
                try { completion.SetResult(dialog.ShowDialog(new WindowOwner(owner))); }
                catch (Exception error) { completion.SetException(error); }
            });
            thread.IsBackground = true;
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            return completion.Task;
        }

        private void ChooseLayers(object sender, EventArgs args)
        {
            ModRow row = mods.SelectedItem as ModRow;
            if (row == null) return;
            using (var dialog = new Form())
            using (var list = new CheckedListBox())
            using (var save = new Button())
            {
                dialog.Text = "Layers: " + row.Name;
                dialog.ClientSize = new Size(360, 300);
                dialog.StartPosition = FormStartPosition.CenterParent;
                list.Dock = DockStyle.Fill;
                list.CheckOnClick = true;
                var names = new List<string>();
                foreach (object item in row.Layers)
                {
                    var layer = (Dictionary<string, object>)item;
                    string name = Convert.ToString(layer["name"]);
                    string display = Convert.ToString(layer["displayName"]);
                    names.Add(name);
                    list.Items.Add(String.IsNullOrEmpty(display) ? name : display,
                        Convert.ToBoolean(layer["enabled"]));
                }
                save.Text = "Save";
                save.Dock = DockStyle.Bottom;
                save.Height = 36;
                save.DialogResult = DialogResult.OK;
                dialog.Controls.Add(list);
                dialog.Controls.Add(save);
                if (dialog.ShowDialog(form) != DialogResult.OK) return;
                var states = new Dictionary<string, bool>();
                for (int i = 0; i < names.Count; i++) states[names[i]] = list.GetItemChecked(i);
                Request(new { op = "layers", id = row.Id, states = states }, delegate { Reload(); });
            }
        }

        private void Start(object sender, EventArgs args)
        {
            if (originalStart == null)
            {
                ShowError(new InvalidOperationException("Nao localizei o Start original. Consulte menu.log."));
                return;
            }
            pendingOriginalStart = true;
            Request(new { op = "start" }, delegate(Dictionary<string, object> result)
            {
                ApplyStatus(result);
            });
        }

        private void Stop(object sender, EventArgs args)
        {
            pendingOriginalStart = false;
            Request(new { op = "stop" }, delegate
            {
                // O Start original continua sob o controle do usuario.
                statusLabel.Text = "Parando customs...";
            });
        }

        private void ApplyStatus(Dictionary<string, object> result)
        {
            running = Convert.ToBoolean(result["running"]);
            statusLabel.Text = Convert.ToString(result["message"]);
            bool ready = Convert.ToBoolean(result["overlayReady"]);
            if (pendingOriginalStart && running && ready)
            {
                pendingOriginalStart = false;
                if (String.Equals(originalStart.Text.Trim(), "Start", StringComparison.OrdinalIgnoreCase))
                {
                    originalStart.PerformClick();
                    startedOriginal = true;
                }
                else if (!String.Equals(originalStart.Text.Trim(), "Stop", StringComparison.OrdinalIgnoreCase))
                {
                    Log.Write("Start original com texto desconhecido: " + originalStart.Text);
                    statusLabel.Text += " Use the Zephyr Start button.";
                }
            }
            if (!running) pendingOriginalStart = false;
            SetEnabled();
        }

        private void Poll(object sender, EventArgs args)
        {
            if (busy || polling || closed) return;
            polling = true;
            Task.Run(delegate
            {
                try
                {
                    var result = backend.Call(new { op = "status" });
                    OnUi(delegate
                    {
                        if (!busy) ApplyStatus(result);
                        polling = false;
                    });
                }
                catch (Exception error)
                {
                    Log.Write(error.ToString());
                    OnUi(delegate
                    {
                        statusLabel.Text = error.Message;
                        pendingOriginalStart = false;
                        running = false;
                        polling = false;
                        SetEnabled();
                    });
                }
            });
        }

        private void Closed(object sender, EventArgs args)
        {
            if (closed) return;
            closed = true;
            timer.Stop();
            timer.Dispose();
            Application.ApplicationExit -= Closed;
            Log.Write("Janela encerrada. Start acionado pela extensao: " + startedOriginal);
            Task.Run(delegate { backend.Dispose(); });
        }
    }
}
