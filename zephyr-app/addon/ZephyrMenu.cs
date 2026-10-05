using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using Microsoft.Win32;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace SkinFusion
{
    internal sealed class OriginalControls
    {
        private readonly Form form;
        private readonly Button start;

        internal OriginalControls(Form original)
        {
            form = original;
            Control[] found = original.Controls.Find("startButton", true);
            start = found.Length == 1 ? found[0] as Button : null;
            if (start == null) throw new InvalidOperationException("Zephyr Start button not found.");
        }

        internal Task<T> OnUi<T>(Func<T> action)
        {
            var completion = new TaskCompletionSource<T>();
            Action run = delegate
            {
                try { completion.SetResult(action()); }
                catch (Exception error) { completion.SetException(error); }
            };
            try
            {
                if (form.IsDisposed || !form.IsHandleCreated)
                    throw new ObjectDisposedException("Zephyr");
                if (form.InvokeRequired) form.BeginInvoke(run);
                else run();
            }
            catch (Exception error) { completion.TrySetException(error); }
            return completion.Task;
        }

        internal Task<object> SetRunning(bool running)
        {
            return OnUi<object>(delegate
            {
                string expected = running ? "Start" : "Stop";
                string already = running ? "Stop" : "Start";
                if (String.Equals(start.Text.Trim(), expected, StringComparison.OrdinalIgnoreCase))
                {
                    // O controle original fica vivo e visivel sob a interface.
                    // PerformClick usa o mesmo handler que funcionou na versao base.
                    if (!start.Enabled)
                        throw new InvalidOperationException("The Zephyr Start button is unavailable.");
                    if (start.CanSelect) start.PerformClick();
                    else if (!form.Visible)
                    {
                        // PerformClick exige visibilidade. Na bandeja, o mesmo evento
                        // Click e levantado sem mostrar a janela ou trocar a injecao.
                        typeof(Button).GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic)
                            .Invoke(start, new object[] { EventArgs.Empty });
                    }
                    else throw new InvalidOperationException("The Zephyr Start button is unavailable.");
                }
                if (!String.Equals(start.Text.Trim(), already, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Unexpected Zephyr Start state: " + start.Text);
                return Status();
            });
        }

        internal Task<object> ReadStatus()
        {
            return OnUi<object>(delegate { return Status(); });
        }

        private string Label(string name)
        {
            Control[] found = form.Controls.Find(name, true);
            return found.Length == 1 ? found[0].Text.Trim() : "";
        }

        private object Status()
        {
            bool running = String.Equals(start.Text.Trim(), "Stop", StringComparison.OrdinalIgnoreCase);
            bool loaded = String.Equals(Label("dllStatusLabel"), "Injected", StringComparison.OrdinalIgnoreCase);
            bool found = String.Equals(Label("gameStatusLabel"), "Found", StringComparison.OrdinalIgnoreCase);
            return new
            {
                running = running,
                phase = loaded ? "attached" : running ? (found ? "attaching" : "waiting") : "idle",
                message = loaded ? "LOADED|Zephyr loaded."
                    : running ? (found ? "LOADING|Zephyr found the game. Waiting to load."
                                      : "WAITING|Zephyr is waiting for a match.")
                              : "STOPPED|Zephyr stopped."
            };
        }
    }

    internal sealed class UiPreferences
    {
        public bool autoRun { get; set; }
        public bool minimizeToTray { get; set; }
        internal UiPreferences() { minimizeToTray = true; }
    }

    internal sealed class EmbeddedPanel : Panel
    {
        private readonly IntPtr parent;
        internal EmbeddedPanel(IntPtr owner) { parent = owner; }
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams parameters = base.CreateParams;
                parameters.Parent = parent;
                parameters.Style = (parameters.Style & ~unchecked((int)0x80000000)) | 0x40000000;
                return parameters;
            }
        }
    }

    internal sealed class ZephyrMenu
    {
        private readonly Form form;
        private readonly OriginalControls original;
        private readonly Backend backend = new Backend();
        private readonly UpdateChecker updates = new UpdateChecker();
        private readonly JavaScriptSerializer json = new JavaScriptSerializer { MaxJsonLength = Int32.MaxValue };
        private readonly string directory = AppDomain.CurrentDomain.BaseDirectory;
        private readonly string preferencePath;
        private readonly Size initialSize;
        private readonly Size initialMaximum;
        private readonly IntPtr owner;
        private EmbeddedPanel panel;
        private WebView2 browser;
        private Thread thread;
        private IntPtr panelHandle;
        private NotifyIcon tray;
        private bool ownTray;
        private volatile bool closed;
        private volatile bool exiting;
        private volatile UiPreferences preferences;
        private string lastCustomError;
        private bool startupRevealed;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr window, IntPtr after,
            int x, int y, int width, int height, uint flags);

        internal ZephyrMenu(Form existing)
        {
            form = existing;
            StartupCover.Install(form);
            original = new OriginalControls(form);
            owner = form.Handle;
            initialSize = form.ClientSize;
            initialMaximum = form.MaximumSize;
            preferencePath = Path.Combine(Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData), "SkinFusionReverse", "zephyr-settings.json");
            preferences = File.Exists(preferencePath)
                ? json.Deserialize<UiPreferences>(File.ReadAllText(preferencePath)) : new UiPreferences();
            if (preferences == null) preferences = new UiPreferences();
            form.MaximumSize = Size.Empty;
            form.MinimumSize = new Size(440, 640);
            form.FormBorderStyle = FormBorderStyle.Sizable;
            Rectangle available = Screen.FromControl(form).WorkingArea;
            form.ClientSize = new Size(480, Math.Min(710, Math.Max(560, available.Height - 90)));
            form.Location = new Point(
                Math.Max(available.Left, Math.Min(form.Left, available.Right - form.Width)),
                Math.Max(available.Top, Math.Min(form.Top, available.Bottom - form.Height)));
            form.ClientSizeChanged += Resize;
            form.FormClosed += Closed;
            form.FormClosing += Closing;
            InstallTray();

            // A UI web tem sua propria STA e message pump no mesmo processo.
            // A thread e a inicializacao do R3nz original permanecem como estavam.
            thread = new Thread(RunUi);
            thread.IsBackground = true;
            thread.Name = "Zephyr UI";
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }

        private void RunUi()
        {
            ZephyrBranding.Install();
            try
            {
                panel = new EmbeddedPanel(owner);
                panel.Name = "ZephyrHost";
                panel.BackColor = Color.FromArgb(9, 9, 9);
                panel.Visible = false;
                panel.Size = new Size(480, 710);
                panelHandle = panel.Handle;
                browser = new WebView2();
                browser.Dock = DockStyle.Fill;
                browser.DefaultBackgroundColor = Color.FromArgb(9, 9, 9);
                panel.Controls.Add(browser);
                panel.BeginInvoke(new Action(Initialize));
                Application.Run();
            }
            catch (Exception error) { Fallback(error); }
            finally
            {
                ZephyrBranding.Remove();
                panelHandle = IntPtr.Zero;
                if (browser != null) browser.Dispose();
                if (panel != null) panel.Dispose();
            }
        }

        private async void Initialize()
        {
            try
            {
                string ui = Path.Combine(directory, "ui");
                if (!File.Exists(Path.Combine(ui, "index.html")))
                    throw new FileNotFoundException("Zephyr interface files are missing.");
                string data = Path.Combine(Path.GetDirectoryName(preferencePath), "ZephyrWebView");
                CoreWebView2Environment environment = await CoreWebView2Environment.CreateAsync(null, data, null);
                await browser.EnsureCoreWebView2Async(environment);
                if (closed) return;
                browser.CoreWebView2.SetVirtualHostNameToFolderMapping("zephyr.local", ui,
                    CoreWebView2HostResourceAccessKind.DenyCors);
                browser.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                browser.CoreWebView2.Settings.AreDevToolsEnabled = false;
                browser.CoreWebView2.Settings.IsStatusBarEnabled = false;
                browser.CoreWebView2.NavigationStarting += delegate(object sender, CoreWebView2NavigationStartingEventArgs args)
                {
                    if (!LocalPage(args.Uri)) args.Cancel = true;
                };
                browser.CoreWebView2.NewWindowRequested += delegate(object sender, CoreWebView2NewWindowRequestedEventArgs args)
                {
                    args.Handled = true;
                };
                browser.CoreWebView2.ProcessFailed += delegate(object sender, CoreWebView2ProcessFailedEventArgs args)
                {
                    Fallback(new InvalidOperationException("WebView2: " + args.ProcessFailedKind));
                };
                browser.CoreWebView2.WebMessageReceived += Message;
                browser.CoreWebView2.NavigationCompleted += delegate(object sender, CoreWebView2NavigationCompletedEventArgs args)
                {
                    if (!args.IsSuccess)
                        Fallback(new InvalidOperationException("Failed to open the interface: " + args.WebErrorStatus));
                    else original.OnUi<object>(delegate { Resize(null, EventArgs.Empty); return null; });
                };
                browser.CoreWebView2.Navigate("https://zephyr.local/index.html");
                await original.OnUi<object>(delegate { Resize(null, EventArgs.Empty); return null; });
                Log.Write("Zephyr embutido no R3nz, PID " + System.Diagnostics.Process.GetCurrentProcess().Id);
            }
            catch (Exception error) { Fallback(error); }
        }

        private static bool LocalPage(string address)
        {
            Uri uri;
            return Uri.TryCreate(address, UriKind.Absolute, out uri)
                && uri.Scheme == "https" && uri.Host == "zephyr.local" && uri.IsDefaultPort;
        }

        private void Message(object sender, CoreWebView2WebMessageReceivedEventArgs args)
        {
            if (!LocalPage(args.Source) || closed) return;
            try
            {
                var request = json.Deserialize<Dictionary<string, object>>(args.WebMessageAsJson);
                // Dialogos modais so abrem depois que o callback WebView2 terminou.
                panel.BeginInvoke(new Action(delegate { Respond(request); }));
            }
            catch (Exception error) { Log.Write("Mensagem Zephyr: " + error); }
        }

        private async void Respond(Dictionary<string, object> request)
        {
            object id;
            if (!request.TryGetValue("id", out id)) return;
            try
            {
                object args;
                request.TryGetValue("args", out args);
                object value = await Invoke(Convert.ToString(request["command"]),
                    args as Dictionary<string, object> ?? new Dictionary<string, object>());
                Send(new { id = id, result = new { ok = true, value = value } });
            }
            catch (Exception error)
            {
                Log.Write("Operacao Zephyr: " + error);
                Send(new { id = id, result = new { ok = false, error = ZephyrBranding.Text(error.Message) } });
            }
        }

        private void Send(object message)
        {
            if (closed || browser == null || browser.CoreWebView2 == null) return;
            browser.CoreWebView2.PostWebMessageAsJson(json.Serialize(message));
        }

        private Task<object> Customs(object request)
        {
            return Task.Run<object>(delegate { return backend.CallValue(request); });
        }

        private static string StringArg(Dictionary<string, object> args, string name)
        {
            object value;
            if (!args.TryGetValue(name, out value)) throw new ArgumentException("Missing " + name);
            return Convert.ToString(value);
        }

        private static bool BoolArg(Dictionary<string, object> args, string name)
        {
            object value;
            return args.TryGetValue(name, out value) && Convert.ToBoolean(value);
        }

        private async Task<object> Invoke(string command, Dictionary<string, object> args)
        {
            switch (command)
            {
                case "show_main_window": return null;
                case "startup_ready": return await RevealStartup();
                case "check_updates": return await updates.Check();
                case "open_update": return updates.OpenRelease();
                case "dialog_open": return OpenDialog(args);
                case "plugin:injector|official_status": return await original.ReadStatus();
                case "plugin:injector|start_official":
                    await WaitForOverlay();
                    return await original.SetRunning(true);
                case "plugin:injector|stop_official": return await original.SetRunning(false);
                case "plugin:library|get_installed_mods":
                    return ((Dictionary<string, object>)await Customs(new { op = "list" }))["mods"];
                case "plugin:library|install_mod":
                    string path = StringArg(args, "filePath");
                    var installed = (Dictionary<string, object>)await Customs(new { op = "import", paths = new[] { path } });
                    object[] failures = ArrayValue(installed["failed"]);
                    if (failures.Length > 0)
                        throw new InvalidOperationException(Convert.ToString(((Dictionary<string, object>)failures[0])["message"]));
                    // O importador retorna IDs para novos, atualizados e ja existentes.
                    foreach (string key in new[] { "installed", "updated", "alreadyInstalled" })
                    {
                        foreach (object entry in ArrayValue(installed[key]))
                        {
                            var item = entry as Dictionary<string, object>;
                            if (item != null && item.ContainsKey("id")) return item;
                        }
                    }
                    throw new InvalidOperationException("The importer did not return the custom skin ID.");
                case "plugin:library|toggle_mod":
                    return await Customs(new { op = "toggle", id = StringArg(args, "modId"), enabled = BoolArg(args, "enabled") });
                case "plugin:library|uninstall_mod": return await Customs(new { op = "remove", id = StringArg(args, "modId") });
                case "plugin:library|get_health_check_readiness": return await Customs(new { op = "readiness" });
                case "plugin:library|inspect_custom_mod": return await Customs(new { op = "inspect", id = StringArg(args, "modId") });
                case "plugin:library|analyze_mod_wads": return await Customs(new { op = "analyze", id = StringArg(args, "modId") });
                case "plugin:library|check_mod_health": return await Customs(new { op = "health", id = StringArg(args, "modId") });
                case "plugin:library|check_custom_selection": return await Customs(new { op = "preflight", ids = args["modIds"] });
                case "start_patcher": return await Customs(new { op = "start" });
                case "stop_patcher": return await Customs(new { op = "stop" });
                case "get_patcher_status":
                    var status = (Dictionary<string, object>)await Customs(new { op = "status" });
                    if (Convert.ToString(status["phase"]) == "failed")
                    {
                        string message = Convert.ToString(status["message"]);
                        if (message != lastCustomError) Send(new { @event = "patcher-error", payload = message });
                        lastCustomError = message;
                    }
                    else lastCustomError = null;
                    return status;
                case "get_settings":
                    var state = (Dictionary<string, object>)await Customs(new { op = "list" });
                    return new { leaguePath = state["leaguePath"], autoRun = preferences.autoRun, minimizeToTray = preferences.minimizeToTray };
                case "save_settings":
                    var settings = (Dictionary<string, object>)args["settings"];
                    string leaguePath = StringArg(settings, "leaguePath");
                    await Customs(new { op = "set_league", path = String.IsNullOrWhiteSpace(leaguePath) ? null : leaguePath });
                    var updated = new UiPreferences { autoRun = BoolArg(settings, "autoRun"), minimizeToTray = BoolArg(settings, "minimizeToTray") };
                    await original.OnUi<object>(delegate { SavePreferences(updated); return null; });
                    return null;
                case "auto_detect_league_path":
                    object detected = await Customs(new { op = "detect" });
                    return detected ?? await Task.Run<object>(delegate { return DetectFromDisk(); });
                default: throw new NotSupportedException("Unknown operation: " + command);
            }
        }

        private async Task<object> RevealStartup()
        {
            if (startupRevealed) return new { elapsedMilliseconds = 0 };
            // Keep the native cover above WebView until its logo is decoded.
            // Show the rendered page underneath before removing that cover.
            panel.Visible = true;
            await original.OnUi<object>(delegate { Resize(null, EventArgs.Empty); StartupCover.Front(form); return null; });
            await Task.Delay(80);
            int elapsed = await original.OnUi<int>(delegate
            {
                int value = StartupCover.Elapsed(form);
                StartupCover.Remove(form);
                return value;
            });
            startupRevealed = true;
            return new { elapsedMilliseconds = elapsed };
        }

        private static object[] ArrayValue(object value)
        {
            var result = new List<object>();
            var items = value as System.Collections.IEnumerable;
            if (items != null) foreach (object item in items) result.Add(item);
            return result.ToArray();
        }

        private object DetectFromDisk()
        {
            string installs = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "Riot Games", "RiotClientInstalls.json");
            if (File.Exists(installs))
            {
                try
                {
                    var data = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(File.ReadAllText(installs));
                    object associated;
                    if (data.TryGetValue("associated_client", out associated))
                    {
                        var paths = associated as Dictionary<string, object>;
                        if (paths != null) foreach (string path in paths.Keys)
                            if (File.Exists(Path.Combine(path, "Game", "League of Legends.exe"))) return path;
                    }
                }
                catch (Exception error) { Log.Write("Deteccao Riot: " + error.Message); }
            }
            foreach (string drive in Directory.GetLogicalDrives())
            {
                foreach (string folder in new[] { "Riot Games", "Games\\Riot Games" })
                {
                    string path = Path.Combine(drive, folder, "League of Legends");
                    if (File.Exists(Path.Combine(path, "Game", "League of Legends.exe"))) return path;
                }
            }
            return null;
        }

        private async Task WaitForOverlay()
        {
            var deadline = DateTime.UtcNow.AddMinutes(3);
            while (true)
            {
                if (closed) throw new ObjectDisposedException("Zephyr");
                var status = (Dictionary<string, object>)await Customs(new { op = "status" });
                if (Convert.ToString(status["phase"]) == "failed")
                    throw new InvalidOperationException(Convert.ToString(status["message"]));
                if (!Convert.ToBoolean(status["running"]) || Convert.ToBoolean(status["overlayReady"])) return;
                if (DateTime.UtcNow >= deadline) throw new TimeoutException("Custom skin preparation has not finished. Try again.");
                await Task.Delay(200);
            }
        }

        private sealed class DialogOwner : IWin32Window
        {
            private readonly IntPtr window;
            internal DialogOwner(IntPtr handle) { window = handle; }
            public IntPtr Handle { get { return window; } }
        }

        private object OpenDialog(Dictionary<string, object> args)
        {
            if (BoolArg(args, "directory"))
            {
                using (var dialog = new FolderBrowserDialog())
                {
                    dialog.Description = "Select the League of Legends or Game folder.";
                    return dialog.ShowDialog(new DialogOwner(owner)) == DialogResult.OK ? dialog.SelectedPath : null;
                }
            }
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = "Custom skins|*.modpkg;*.fantome";
                dialog.Multiselect = BoolArg(args, "multiple");
                if (dialog.ShowDialog(new DialogOwner(owner)) != DialogResult.OK) return null;
                return dialog.Multiselect ? (object)dialog.FileNames : dialog.FileName;
            }
        }

        private T Field<T>(string name) where T : class
        {
            FieldInfo field = form.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            return field == null ? null : field.GetValue(form) as T;
        }

        private void InstallTray()
        {
            tray = Field<NotifyIcon>("notifyIcon");
            if (tray == null)
            {
                ownTray = true;
                tray = new NotifyIcon();
                tray.Icon = form.Icon ?? SystemIcons.Application;
                tray.ContextMenu = new ContextMenu(new[] {
                    new MenuItem("Open Zephyr", delegate { Restore(); }),
                    new MenuItem("Quit", delegate { Quit(); }) });
            }
            else
            {
                var exit = Field<MenuItem>("menuItem");
                if (exit != null) exit.Click += TrayExit;
            }
            tray.Icon = form.Icon ?? SystemIcons.Application;
            tray.Text = "Zephyr";
            tray.MouseDoubleClick += TrayOpen;
            var hide = Field<ToolStripMenuItem>("toolstripmenuItem2");
            if (hide != null) hide.Checked = preferences.minimizeToTray;
        }

        private void TrayExit(object sender, EventArgs args) { Quit(); }
        private void TrayOpen(object sender, MouseEventArgs args) { Restore(); }
        private void Restore()
        {
            if (closed) return;
            form.Show();
            form.WindowState = FormWindowState.Normal;
            form.Activate();
            tray.Visible = false;
        }

        private void Quit()
        {
            exiting = true;
            if (!form.IsDisposed) form.Close();
        }

        private void SavePreferences(UiPreferences updated)
        {
            string root = Path.GetFullPath(Path.Combine(directory, ".."));
            string startup = Path.Combine(root, "Zephyr.exe");
            if (updated.autoRun && !File.Exists(startup)) throw new FileNotFoundException("Missing Zephyr.exe.");
            using (RegistryKey run = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"))
            {
                if (updated.autoRun) run.SetValue("ZephyrSkinFusion", "\"" + startup + "\"");
                else run.DeleteValue("ZephyrSkinFusion", false);
            }
            Directory.CreateDirectory(Path.GetDirectoryName(preferencePath));
            string staged = preferencePath + ".tmp";
            File.WriteAllText(staged, new JavaScriptSerializer().Serialize(updated));
            if (File.Exists(preferencePath)) File.Replace(staged, preferencePath, null);
            else File.Move(staged, preferencePath);
            preferences = updated;
            var hide = Field<ToolStripMenuItem>("toolstripmenuItem2");
            if (hide != null) hide.Checked = updated.minimizeToTray;
        }

        private void Resize(object sender, EventArgs args)
        {
            if (panelHandle == IntPtr.Zero || closed || form.ClientSize.Width < 1 || form.ClientSize.Height < 1) return;
            SetWindowPos(panelHandle, IntPtr.Zero, 0, 0, form.ClientSize.Width, form.ClientSize.Height, 0x4010);
            StartupCover.Front(form);
        }

        private void Closing(object sender, FormClosingEventArgs args)
        {
            if (exiting || !preferences.minimizeToTray || args.CloseReason != CloseReason.UserClosing) return;
            args.Cancel = true;
            form.Hide();
            tray.Visible = true;
        }

        private void Detach()
        {
            form.ClientSizeChanged -= Resize;
            form.FormClosing -= Closing;
            form.FormClosed -= Closed;
            if (tray != null)
            {
                tray.MouseDoubleClick -= TrayOpen;
                var exit = Field<MenuItem>("menuItem");
                if (exit != null) exit.Click -= TrayExit;
                if (ownTray) tray.Dispose();
            }
        }

        private void Fallback(Exception error)
        {
            if (closed) return;
            closed = true;
            Log.Write("Falha na UI Zephyr, voltando ao menu classico: " + error);
            Task.Run(delegate { backend.Dispose(); });
            original.OnUi<object>(delegate
            {
                Detach();
                StartupCover.Remove(form);
                form.MinimumSize = Size.Empty;
                form.MaximumSize = initialMaximum;
                form.ClientSize = initialSize;
                new CustomsMenu(form);
                MessageBox.Show(form, "The Zephyr interface could not load. The classic menu is still available.\n\n"
                    + error.Message + "\n\nInstall or repair the Microsoft Edge WebView2 Runtime.", "Zephyr");
                return null;
            });
            if (panel != null && panel.IsHandleCreated)
            {
                try { panel.BeginInvoke(new Action(delegate { panel.Visible = false; Application.ExitThread(); })); }
                catch (InvalidOperationException) { }
            }
        }

        private void Closed(object sender, FormClosedEventArgs args)
        {
            if (closed) return;
            closed = true;
            Detach();
            if (panel != null && panel.IsHandleCreated)
            {
                try { panel.BeginInvoke(new Action(Application.ExitThread)); }
                catch (InvalidOperationException) { }
            }
            Task.Run(delegate { backend.Dispose(); });
        }
    }
}
