using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
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
                lnk.TargetPath = Path.Combine(dir, "uninstall.cmd");
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
        Panel page1, page2, page3, page4;
        Button btnNext, btnBack, btnInstall, btnBrowse, btnFinish, btnCancel;
        TextBox txtDir;
        ProgressBar progress;
        Label lblStatus, lblProgress;
        CheckBox chkLaunch;

        public MainForm()
        {
            Text = "DeepSeek Harness 安装程序";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            ClientSize = new Size(560, 380);
            StartPosition = FormStartPosition.CenterScreen;
            wireBg();

            BuildPages();
            ShowPage(page1);
        }

        void BuildPages()
        {
            // --- page 1: welcome ---
            page1 = new Panel { Dock = DockStyle.Fill };
            var title = new Label
            {
                Text = "欢迎安装 DeepSeek Harness",
                Font = new Font("Microsoft YaHei", 16, FontStyle.Bold),
                Location = new Point(30, 24), AutoSize = true
            };
            var info = new TextBox
            {
                Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical,
                Location = new Point(30, 70), Size = new Size(500, 210),
                Font = new Font("Microsoft YaHei", 10),
                Text =
                    "DeepSeek Harness 桌面版:一个独立的应用窗口,无需浏览器。\r\n\r\n" +
                    "本安装包已内置全部组件:\r\n" +
                    "  · Electron 桌面运行时\r\n  · Node.js 运行时\r\n  · dsh 引擎与全部依赖\r\n  · 插件(任务看板 / 宠物 / 实时统计 / 皮肤中心)\r\n\r\n" +
                    "安装特点:\r\n" +
                    "  · 所有文件只安装在所选目录内,不写注册表\r\n  · 数据(配置/会话)默认保存在安装目录 data\\ 下\r\n  · 卸载:运行安装目录 uninstall.cmd 一键清理\r\n\r\n" +
                    "点击「下一步」选择安装位置。"
            };
            page1.Controls.Add(title); page1.Controls.Add(info);

            // --- page 2: install dir ---
            page2 = new Panel { Dock = DockStyle.Fill };
            var l2 = new Label
            {
                Text = "选择安装位置",
                Font = new Font("Microsoft YaHei", 14, FontStyle.Bold),
                Location = new Point(30, 24), AutoSize = true
            };
            var l3 = new Label
            {
                Text = "建议安装在非系统盘(默认 D:\\Program Files\\DSH)。\r\n可点击「浏览」选择文件夹,或直接在输入框键入路径。",
                Font = new Font("Microsoft YaHei", 9),
                Location = new Point(30, 66), AutoSize = true
            };
            txtDir = new TextBox
            {
                Text = Program.FindInstallDir(),
                Location = new Point(30, 110), Size = new Size(400, 24),
                Font = new Font("Microsoft YaHei", 10)
            };
            btnBrowse = new Button
            {
                Text = "浏览...", Location = new Point(438, 108), Size = new Size(90, 28)
            };
            btnBrowse.Click += (s, e) =>
            {
                using (FolderBrowserDialog d = new FolderBrowserDialog())
                {
                    d.Description = "选择安装目录";
                    d.SelectedPath = Directory.Exists(txtDir.Text) ? txtDir.Text : Program.FindInstallDir();
                    if (d.ShowDialog(this) == DialogResult.OK) txtDir.Text = d.SelectedPath;
                }
            };
            page2.Controls.Add(l2); page2.Controls.Add(l3); page2.Controls.Add(txtDir); page2.Controls.Add(btnBrowse);

            // --- page 3: progress ---
            page3 = new Panel { Dock = DockStyle.Fill };
            var l4 = new Label
            {
                Text = "正在安装...",
                Font = new Font("Microsoft YaHei", 14, FontStyle.Bold),
                Location = new Point(30, 24), AutoSize = true
            };
            progress = new ProgressBar { Location = new Point(30, 80), Size = new Size(500, 22) };
            lblProgress = new Label { Text = "0%", Location = new Point(500, 112), AutoSize = true, TextAlign = ContentAlignment.TopRight };
            lblStatus = new Label
            {
                Text = "正在解压组件...", Location = new Point(30, 120), Size = new Size(500, 160),
                Font = new Font("Microsoft YaHei", 9)
            };
            page3.Controls.Add(l4); page3.Controls.Add(progress); page3.Controls.Add(lblProgress); page3.Controls.Add(lblStatus);

            // --- page 4: done ---
            page4 = new Panel { Dock = DockStyle.Fill };
            var l5 = new Label
            {
                Text = "安装完成",
                Font = new Font("Microsoft YaHei", 16, FontStyle.Bold),
                Location = new Point(30, 24), AutoSize = true
            };
            var l6 = new Label
            {
                Text = "DeepSeek Harness 已成功安装。\r\n\r\n首次启动后请在 设置 → 模型 中配置 API Key。\r\n\r\n提示:全部数据保存在安装目录 data\\ 下,\r\n卸载请运行开始菜单中的「卸载 DSH」。",
                Font = new Font("Microsoft YaHei", 10),
                Location = new Point(30, 70), Size = new Size(500, 100)
            };
            chkLaunch = new CheckBox
            {
                Text = "立即启动 DeepSeek Harness", Font = new Font("Microsoft YaHei", 10),
                Location = new Point(30, 180), AutoSize = true, Checked = true
            };
            page4.Controls.Add(l5); page4.Controls.Add(l6); page4.Controls.Add(chkLaunch);

            // --- nav buttons ---
            btnBack = new Button { Text = "< 上一步", Location = new Point(300, 320), Size = new Size(90, 30), Visible = false };
            btnNext = new Button { Text = "下一步 >", Location = new Point(396, 320), Size = new Size(90, 30) };
            btnInstall = new Button { Text = "安装", Location = new Point(396, 320), Size = new Size(90, 30), Visible = false };
            btnCancel = new Button { Text = "取消", Location = new Point(200, 320), Size = new Size(90, 30), Visible = false };
            btnFinish = new Button { Text = "完成", Location = new Point(396, 320), Size = new Size(90, 30), Visible = false };
            btnBrowse.Enabled = true;

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
