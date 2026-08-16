using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace DSHInstaller
{
    static class Program
    {
        static string SilentDir = null;
        static string InstalledDir = null;

        [STAThread]
        static int Main(string[] args)
        {
            foreach (string a in args)
            {
                if (a.StartsWith("/sdir=", StringComparison.OrdinalIgnoreCase))
                    SilentDir = a.Substring(6).Trim('"');
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (SilentDir != null)
            {
                return SilentInstall(SilentDir);
            }
            using (MainForm f = new MainForm())
            {
                Application.Run(f);
                return f.Result;
            }
        }

        public static string FindInstallDir()
        {
            if (Directory.Exists("D:\\")) return "D:\\Program Files\\DSH";
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "DSH");
        }

        // ---- payload loading (encrypted payload.dat beside the exe) ----
        static string DecryptPayloadToTemp()
        {
            string dat = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "payload.dat");
            if (!File.Exists(dat)) return null;
            byte[] footer = Encoding.ASCII.GetBytes("DSHPAYLOAD01");
            using (FileStream fs = File.OpenRead(dat))
            {
                long len = fs.Length;
                if (len < footer.Length + 8) return null;
                byte[] magic = new byte[footer.Length];
                fs.Seek(len - footer.Length - 8, SeekOrigin.Begin);
                fs.Read(magic, 0, footer.Length);
                for (int i = 0; i < footer.Length; i++) if (magic[i] != footer[i]) return null;
                byte[] lenBytes = new byte[8];
                fs.Seek(len - 8, SeekOrigin.Begin);
                fs.Read(lenBytes, 0, 8);
                long dataLen = BitConverter.ToInt64(lenBytes, 0);
                if (dataLen <= 0 || dataLen > len) return null;

                byte[] key = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes("DSH-Portable-2026"));
                byte[] iv = new byte[16];
                Array.Copy(SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes("DeepSeekHarness-iv")), iv, 16);

                string tmp = Path.Combine(Path.GetTempPath(), "dsh_payload_" + Guid.NewGuid().ToString("N") + ".zip");
                fs.Seek(0, SeekOrigin.Begin);
                using (FileStream dst = File.Create(tmp))
                using (RijndaelManaged aes = new RijndaelManaged { Key = key, IV = iv, Mode = CipherMode.CBC, Padding = PaddingMode.PKCS7 })
                using (CryptoStream cs = new CryptoStream(dst, aes.CreateDecryptor(), CryptoStreamMode.Write))
                {
                    byte[] buf = new byte[1048576];
                    long remaining = dataLen;
                    while (remaining > 0)
                    {
                        int chunk = (int)Math.Min(buf.Length, remaining);
                        int read = fs.Read(buf, 0, chunk);
                        if (read <= 0) break;
                        cs.Write(buf, 0, read);
                        remaining -= read;
                    }
                }
                return tmp;
            }
        }

        public static int ExtractPayload(string destDir, Action<long, long, string> progress)
        {
            string tmp = DecryptPayloadToTemp();
            if (tmp == null) return -1;
            try
            {
                using (ZipArchive za = ZipFile.OpenRead(tmp))
                {
                    long total = za.Entries.Count;
                    long done = 0;
                    foreach (ZipArchiveEntry e in za.Entries)
                    {
                        string target = Path.Combine(destDir, e.FullName.Replace('/', Path.DirectorySeparatorChar));
                        if (e.Name.Length == 0) { Directory.CreateDirectory(target); done++; continue; }
                        Directory.CreateDirectory(Path.GetDirectoryName(target));
                        if (progress != null) progress(done, total, e.FullName);
                        e.ExtractToFile(target, true);
                        done++;
                    }
                }
                return 0;
            }
            finally { try { File.Delete(tmp); } catch { } }
        }

        static int SilentInstall(string dir)
        {
            try
            {
                Directory.CreateDirectory(dir);
                string probe = Path.Combine(dir, ".wtest");
                File.WriteAllText(probe, "x");
                File.Delete(probe);
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine("NEED_ADMIN");
                return 3;
            }
            int rc = ExtractPayload(dir, null);
            if (rc == -1) { Console.WriteLine("PAYLOAD_MISSING"); return 4; }
            if (rc != 0) { Console.WriteLine("PAYLOAD_FAIL"); return 4; }
            FixProfileStore(dir);
            CreateShortcuts(dir);
            Console.WriteLine("INSTALL_OK " + dir);
            return 0;
        }

        // Re-run `pnpm install` inside the web profile so pnpm's virtual-store
        // metadata points at THIS machine's install path (the shipped copy
        // records the packager's path). Non-fatal: the app still runs without
        // it; a later plugin update would repair the profile anyway.
        public static void FixProfileStore(string dir)
        {
            try
            {
                string profile = Path.Combine(dir, "data", "profiles", "web");
                string nodeExe = Path.Combine(dir, "node", "node.exe");
                string pnpmCjs = Path.Combine(dir, "global", "node_modules", "pnpm", "bin", "pnpm.cjs");
                if (!File.Exists(nodeExe) || !File.Exists(pnpmCjs) || !File.Exists(Path.Combine(profile, "package.json"))) return;
                var psi = new ProcessStartInfo(nodeExe, "\"" + pnpmCjs + "\" install")
                {
                    WorkingDirectory = profile,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                psi.EnvironmentVariables["CI"] = "true";
                psi.EnvironmentVariables["PATH"] =
                    Path.Combine(dir, "node") + ";" + Path.Combine(dir, "global") + ";" +
                    (psi.EnvironmentVariables["PATH"] ?? "");
                using (Process p = Process.Start(psi))
                {
                    p.WaitForExit(240000);
                }
            }
            catch { /* non-fatal */ }
        }

        public static void CreateShortcuts(string dir)
        {
            try
            {
                Type t = Type.GetTypeFromProgID("WScript.Shell");
                dynamic shell = Activator.CreateInstance(t);
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string appExe = Path.Combine(dir, "electron", "electron.exe");
                string appDir = Path.Combine(dir, "app");
                string icon = Path.Combine(dir, "app", "DSH.ico");
                // desktop
                dynamic lnk = shell.CreateShortcut(Path.Combine(desktop, "DSH Web.lnk"));
                lnk.TargetPath = appExe;
                lnk.Arguments = "\"" + appDir + "\"";
                lnk.WorkingDirectory = dir;
                lnk.IconLocation = icon;
                lnk.Save();
                // start menu
                string sm = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "DeepSeek Harness");
                Directory.CreateDirectory(sm);
                lnk = shell.CreateShortcut(Path.Combine(sm, "DSH Web.lnk"));
                lnk.TargetPath = appExe;
                lnk.Arguments = "\"" + appDir + "\"";
                lnk.WorkingDirectory = dir;
                lnk.IconLocation = icon;
                lnk.Save();
                lnk = shell.CreateShortcut(Path.Combine(sm, "卸载 DSH.lnk"));
                lnk.TargetPath = Path.Combine(dir, "uninstall.exe");
                lnk.WorkingDirectory = dir;
                lnk.IconLocation = icon;
                lnk.Save();
            }
            catch { }
        }

        public static void LaunchApp(string dir)
        {
            try
            {
                string appExe = Path.Combine(dir, "electron", "electron.exe");
                string appDir = Path.Combine(dir, "app");
                if (File.Exists(appExe))
                    Process.Start(new ProcessStartInfo(appExe, "\"" + appDir + "\"") { WorkingDirectory = dir });
            }
            catch { }
        }
    }

    class MainForm : Form
    {
        public int Result = 1;

        // palette
        static readonly Color Accent = Color.FromArgb(0x2B, 0x7C, 0xD9);
        static readonly Color AccentDark = Color.FromArgb(0x1E, 0x63, 0xB8);
        static readonly Color HeaderTop = Color.FromArgb(0x12, 0x30, 0x5E);
        static readonly Color HeaderBottom = Color.FromArgb(0x2B, 0x7C, 0xD9);
        static readonly Color BorderGray = Color.FromArgb(0xD8, 0xDE, 0xE4);
        static readonly Color TextMain = Color.FromArgb(0x24, 0x2A, 0x30);
        static readonly Color TextDim = Color.FromArgb(0x6B, 0x74, 0x80);
        static readonly Color OkGreen = Color.FromArgb(0x2E, 0x9E, 0x5B);

        Panel page1, page2, page3, page4;
        Button btnNext, btnBack, btnInstall, btnBrowse, btnFinish, btnCancel;
        TextBox txtDir;
        ProgressBar progress;
        Label lblStatus, lblProgress, lblStep;
        CheckBox chkLaunch;
        Label lblFinishDir;

        public MainForm()
        {
            Text = "DeepSeek Harness 安装程序";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(660, 440);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.White;
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
            wireBg();

            BuildLayout();
            ShowPage(page1);
        }

        // ---------- visual helpers ----------
        static Label MakeLabel(string text, float size, FontStyle style, Color color, Point loc)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Microsoft YaHei UI", size, style),
                ForeColor = color,
                Location = loc,
                AutoSize = true,
                BackColor = Color.Transparent
            };
        }

        static Button MakePrimaryButton(string text, Point loc)
        {
            var b = new Button
            {
                Text = text,
                Location = loc,
                Size = new Size(104, 34),
                Font = new Font("Microsoft YaHei UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Accent,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = AccentDark;
            return b;
        }

        static Button MakeSecondaryButton(string text, Point loc)
        {
            var b = new Button
            {
                Text = text,
                Location = loc,
                Size = new Size(104, 34),
                Font = new Font("Microsoft YaHei UI", 9.5f),
                ForeColor = TextMain,
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 1;
            b.FlatAppearance.BorderColor = BorderGray;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(0xF2, 0xF6, 0xFA);
            return b;
        }

        Panel MakeHeader()
        {
            var h = new Panel { Dock = DockStyle.Top, Height = 68 };
            h.Paint += (s, e) =>
            {
                using (var br = new LinearGradientBrush(h.ClientRectangle, HeaderTop, HeaderBottom, 0f))
                    e.Graphics.FillRectangle(br, h.ClientRectangle);
                try
                {
                    var icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                    e.Graphics.DrawIcon(icon, new Rectangle(16, 16, 36, 36));
                }
                catch { }
            };
            var t = new Label
            {
                Text = "DeepSeek Harness",
                Font = new Font("Microsoft YaHei UI", 15, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Location = new Point(64, 12),
                AutoSize = true
            };
            var sub = new Label
            {
                Text = "安装程序  ·  v1.0.0",
                Font = new Font("Microsoft YaHei UI", 9f),
                ForeColor = Color.FromArgb(0xC9, 0xDD, 0xF4),
                BackColor = Color.Transparent,
                Location = new Point(65, 40),
                AutoSize = true
            };
            h.Controls.Add(t);
            h.Controls.Add(sub);
            return h;
        }

        // ---------- layout ----------
        void BuildLayout()
        {
            // step indicator (top-right of the content area)
            lblStep = MakeLabel("步骤 1 / 4", 9f, FontStyle.Regular, TextDim, new Point(560, 6));

            // --- page 1: welcome ---
            page1 = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            page1.Controls.Add(MakeLabel("欢迎安装 DeepSeek Harness", 17f, FontStyle.Bold, TextMain, new Point(36, 28)));
            page1.Controls.Add(MakeLabel("DeepSeek Harness 桌面版 — 独立应用窗口,无需浏览器。", 10f, FontStyle.Regular, TextDim, new Point(36, 64)));
            page1.Controls.Add(MakeLabel("本安装包已内置全部组件:", 10f, FontStyle.Bold, TextMain, new Point(36, 100)));
            page1.Controls.Add(MakeLabel("•  Electron 桌面运行时", 10f, FontStyle.Regular, TextMain, new Point(52, 128)));
            page1.Controls.Add(MakeLabel("•  Node.js 运行时", 10f, FontStyle.Regular, TextMain, new Point(52, 154)));
            page1.Controls.Add(MakeLabel("•  dsh 引擎与全部依赖", 10f, FontStyle.Regular, TextMain, new Point(52, 180)));
            page1.Controls.Add(MakeLabel("•  插件(任务看板 / 宠物 / 实时统计 / 皮肤中心)", 10f, FontStyle.Regular, TextMain, new Point(52, 206)));
            page1.Controls.Add(MakeLabel("安装特点:", 10f, FontStyle.Bold, TextMain, new Point(36, 244)));
            page1.Controls.Add(MakeLabel("•  所有文件只安装在所选目录内,不写注册表", 10f, FontStyle.Regular, TextMain, new Point(52, 272)));
            page1.Controls.Add(MakeLabel("•  数据(配置/会话)默认保存在安装目录 data\\ 下", 10f, FontStyle.Regular, TextMain, new Point(52, 298)));
            page1.Controls.Add(MakeLabel("•  卸载:运行安装目录 uninstall.cmd 一键清理", 10f, FontStyle.Regular, TextMain, new Point(52, 324)));
            page1.Controls.Add(lblStep);

            // --- page 2: install dir ---
            page2 = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            page2.Controls.Add(MakeLabel("选择安装位置", 17f, FontStyle.Bold, TextMain, new Point(36, 28)));
            page2.Controls.Add(MakeLabel("建议安装在非系统盘(默认 D:\\Program Files\\DSH)。", 10f, FontStyle.Regular, TextDim, new Point(36, 68)));
            page2.Controls.Add(MakeLabel("可点击「浏览」选择文件夹,或直接在输入框键入路径。", 10f, FontStyle.Regular, TextDim, new Point(36, 92)));
            txtDir = new TextBox
            {
                Text = Program.FindInstallDir(),
                Location = new Point(36, 130), Size = new Size(470, 28),
                Font = new Font("Microsoft YaHei UI", 10.5f),
                BorderStyle = BorderStyle.FixedSingle
            };
            btnBrowse = MakeSecondaryButton("浏览...", new Point(520, 128));
            btnBrowse.Click += (s, e) =>
            {
                using (FolderBrowserDialog d = new FolderBrowserDialog())
                {
                    d.Description = "选择安装目录";
                    d.SelectedPath = Directory.Exists(txtDir.Text) ? txtDir.Text : Program.FindInstallDir();
                    if (d.ShowDialog(this) == DialogResult.OK) txtDir.Text = d.SelectedPath;
                }
            };
            var space = new Panel
            {
                Location = new Point(36, 176), Size = new Size(560, 60),
                BackColor = Color.FromArgb(0xF6, 0xF9, 0xFC), BorderStyle = BorderStyle.FixedSingle
            };
            space.Controls.Add(MakeLabel("磁盘空间:安装后约需 1.5 GB", 9.5f, FontStyle.Regular, TextDim, new Point(12, 12)));
            space.Controls.Add(MakeLabel("系统要求:Windows 10 / 11 64 位", 9.5f, FontStyle.Regular, TextDim, new Point(12, 34)));
            page2.Controls.Add(txtDir); page2.Controls.Add(btnBrowse); page2.Controls.Add(space);
            page2.Controls.Add(lblStep);

            // --- page 3: progress ---
            page3 = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            page3.Controls.Add(MakeLabel("正在安装", 17f, FontStyle.Bold, TextMain, new Point(36, 28)));
            page3.Controls.Add(MakeLabel("正在解压组件并完成配置,请稍候...", 10f, FontStyle.Regular, TextDim, new Point(36, 68)));
            progress = new ProgressBar
            {
                Location = new Point(36, 120), Size = new Size(588, 18),
                Style = ProgressBarStyle.Continuous
            };
            lblProgress = MakeLabel("0%", 10f, FontStyle.Bold, Accent, new Point(590, 146));
            lblStatus = new Label
            {
                Text = "准备中...",
                Location = new Point(36, 168), Size = new Size(588, 60),
                Font = new Font("Microsoft YaHei UI", 9f),
                ForeColor = TextDim
            };
            page3.Controls.Add(progress); page3.Controls.Add(lblProgress); page3.Controls.Add(lblStatus);
            page3.Controls.Add(lblStep);

            // --- page 4: done ---
            page4 = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            page4.Controls.Add(MakeLabel("✓", 40f, FontStyle.Bold, OkGreen, new Point(36, 24)));
            page4.Controls.Add(MakeLabel("安装完成", 17f, FontStyle.Bold, TextMain, new Point(112, 38)));
            page4.Controls.Add(MakeLabel("DeepSeek Harness 已成功安装到您的电脑。", 10f, FontStyle.Regular, TextMain, new Point(112, 76)));
            page4.Controls.Add(MakeLabel("安装目录:", 10f, FontStyle.Regular, TextDim, new Point(36, 132)));
            lblFinishDir = MakeLabel("", 10f, FontStyle.Regular, TextMain, new Point(110, 132));
            page4.Controls.Add(MakeLabel("首次启动后请在 设置 → 模型 中配置 API Key。", 10f, FontStyle.Regular, TextDim, new Point(36, 166)));
            page4.Controls.Add(MakeLabel("数据保存在安装目录 data\\ 下;卸载请运行「卸载 DSH」。", 10f, FontStyle.Regular, TextDim, new Point(36, 192)));
            chkLaunch = new CheckBox
            {
                Text = "立即启动 DeepSeek Harness",
                Font = new Font("Microsoft YaHei UI", 10f),
                ForeColor = TextMain,
                Location = new Point(36, 238),
                AutoSize = true,
                Checked = true
            };
            page4.Controls.Add(chkLaunch);
            page4.Controls.Add(lblStep);

            // --- nav buttons ---
            btnBack = MakeSecondaryButton("< 上一步", new Point(380, 396));
            btnNext = MakePrimaryButton("下一步 >", new Point(492, 396));
            btnInstall = MakePrimaryButton("安装", new Point(492, 396));
            btnCancel = MakeSecondaryButton("取消", new Point(380, 396));
            btnFinish = MakePrimaryButton("完成", new Point(492, 396));
            btnBack.Visible = btnInstall.Visible = btnCancel.Visible = btnFinish.Visible = false;

            btnNext.Click += (s, e) => { ShowPage(page2); };
            btnBack.Click += (s, e) => { ShowPage(page1); };
            btnInstall.Click += (s, e) => { StartInstall(); };
            btnCancel.Click += (s, e) => { Application.Exit(); };
            btnFinish.Click += (s, e) =>
            {
                if (chkLaunch.Checked) Program.LaunchApp(txtDir.Text.Trim());
                Result = 0;
                Application.Exit();
            };

            Controls.Add(btnBack); Controls.Add(btnNext); Controls.Add(btnInstall);
            Controls.Add(btnCancel); Controls.Add(btnFinish);
            Controls.Add(page1); Controls.Add(page2); Controls.Add(page3); Controls.Add(page4);
            // Header must be added last: docked controls are laid out in reverse
            // order, so the last-added control docks first (top band) and the
            // pages fill the space below it instead of being covered.
            Controls.Add(MakeHeader());
        }

        void ShowPage(Panel p)
        {
            page1.Visible = page2.Visible = page3.Visible = page4.Visible = false;
            p.Visible = true;
            btnNext.Visible = (p == page1);
            btnBack.Visible = (p == page2);
            btnInstall.Visible = (p == page2);
            btnCancel.Visible = (p == page3);
            btnFinish.Visible = (p == page4);
            lblStep.Text = p == page1 ? "步骤 1 / 4 · 欢迎"
                        : p == page2 ? "步骤 2 / 4 · 安装位置"
                        : p == page3 ? "步骤 3 / 4 · 安装中"
                        : "步骤 4 / 4 · 完成";
            if (p == page4) lblFinishDir.Text = txtDir.Text.Trim();
        }

        void StartInstall()
        {
            string dir = txtDir.Text.Trim();
            if (dir.Length == 0) { MessageBox.Show(this, "请选择安装目录。", "提示"); return; }
            try { Directory.CreateDirectory(dir); }
            catch (Exception ex) { MessageBox.Show(this, "无法创建目录:" + ex.Message, "错误"); return; }
            string probe = Path.Combine(dir, ".wtest");
            try { File.WriteAllText(probe, "x"); File.Delete(probe); }
            catch (UnauthorizedAccessException)
            {
                // needs elevation
                try
                {
                    Process.Start(new ProcessStartInfo(Application.ExecutablePath, "/sdir=\"" + dir + "\"") { Verb = "runas", UseShellExecute = true });
                    Application.Exit();
                }
                catch (Exception)
                {
                    MessageBox.Show(this, "安装需要管理员权限(安装位置受系统保护)。", "需要管理员权限");
                }
                return;
            }
            ShowPage(page3);
            btnCancel.Enabled = false;
            bgw.RunWorkerAsync(new object[] { dir });
        }

        BackgroundWorker bgw = new BackgroundWorker();
        void wireBg()
        {
            bgw.WorkerReportsProgress = true;
            bgw.DoWork += (s, e) =>
            {
                object[] p = (object[])e.Argument;
                string dir = (string)p[0];
                try
                {
                    int rc = Program.ExtractPayload(dir,
                        (done, total, name) =>
                        {
                            int pct = (int)(done * 100 / Math.Max(1, total));
                            bgw.ReportProgress(pct, name);
                        });
                    e.Result = rc == 0 ? "OK" : "PAYLOAD_FAIL";
                }
                catch (Exception ex) { e.Result = ex.Message; }
            };
            bgw.ProgressChanged += (s, e) =>
            {
                progress.Value = e.ProgressPercentage;
                lblProgress.Text = e.ProgressPercentage + "%";
                string name = (string)e.UserState;
                if (name != null && name.Length > 60) name = "..." + name.Substring(name.Length - 60);
                lblStatus.Text = "正在解压:" + name;
            };
            bgw.RunWorkerCompleted += (s, e) =>
            {
                if (e.Result == null || (string)e.Result != "OK")
                {
                    MessageBox.Show(this, "安装失败:" + e.Result, "错误");
                    Application.Exit();
                    return;
                }
                progress.Value = 100;
                lblProgress.Text = "100%";
                lblStatus.Text = "正在关联插件依赖...";
                Program.FixProfileStore(txtDir.Text.Trim());
                Program.CreateShortcuts(txtDir.Text.Trim());
                ShowPage(page4);
            };
        }
    }
}
