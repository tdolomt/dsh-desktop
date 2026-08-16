using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace DSHUninstaller
{
    static class Program
    {
        public static string RootDir = null;
        public static string ExtraDir = null;

        [STAThread]
        static int Main(string[] args)
        {
            // Cleanup mode: this copy was placed in %TEMP% by the uninstaller;
            // args[0] is the install directory to remove entirely.
            if (args.Length > 0 && args[0].Length > 3 && Directory.Exists(args[0]))
            {
                Thread.Sleep(1200);
                try { Directory.Delete(args[0], true); } catch { }
                try
                {
                    Process.Start(new ProcessStartInfo("cmd.exe",
                        "/c ping -n 2 127.0.0.1 >nul & del \"" + Application.ExecutablePath + "\"")
                    { UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden });
                }
                catch { }
                return 0;
            }

            bool elevated = false;
            foreach (string a in args)
                if (a == "--elevated") elevated = true;

            RootDir = Path.GetDirectoryName(Application.ExecutablePath);
            ExtraDir = ReadDataDirOverride(RootDir);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (!elevated && !CanWrite(RootDir))
            {
                try
                {
                    Process.Start(new ProcessStartInfo(Application.ExecutablePath, "--elevated") { Verb = "runas", UseShellExecute = true });
                    return 0;
                }
                catch
                {
                    MessageBox.Show("卸载需要管理员权限(安装位置受系统保护)。", "需要管理员权限");
                    return 1;
                }
            }

            using (MainForm f = new MainForm())
            {
                Application.Run(f);
                return f.Result;
            }
        }

        static bool CanWrite(string dir)
        {
            try
            {
                string probe = Path.Combine(dir, ".uwtest");
                File.WriteAllText(probe, "x");
                File.Delete(probe);
                return true;
            }
            catch { return false; }
        }

        static string ReadDataDirOverride(string root)
        {
            try
            {
                string ini = Path.Combine(root, "datadir.ini");
                if (!File.Exists(ini)) return null;
                foreach (string line in File.ReadAllLines(ini))
                {
                    string s = line.Trim();
                    if (s.Length == 0 || s.StartsWith("#")) continue;
                    return s;
                }
            }
            catch { }
            return null;
        }
    }

    class MainForm : Form
    {
        public int Result = 1;

        static readonly Color Accent = Color.FromArgb(0x2B, 0x7C, 0xD9);
        static readonly Color AccentDark = Color.FromArgb(0x1E, 0x63, 0xB8);
        static readonly Color HeaderTop = Color.FromArgb(0x4A, 0x1E, 0x1E);
        static readonly Color HeaderBottom = Color.FromArgb(0xC0, 0x39, 0x2B);
        static readonly Color BorderGray = Color.FromArgb(0xD8, 0xDE, 0xE4);
        static readonly Color TextMain = Color.FromArgb(0x24, 0x2A, 0x30);
        static readonly Color TextDim = Color.FromArgb(0x6B, 0x74, 0x80);

        Panel page1, page2;
        Button btnUninstall, btnCancel, btnFinish;
        ProgressBar progress;
        Label lblStatus, lblStep, lblTitle;
        CheckBox chkExtra;

        public MainForm()
        {
            Text = "DeepSeek Harness 卸载程序";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(620, 400);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.White;
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
            wireBg();
            BuildLayout();
            ShowPage(page1);
        }

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

        static Button MakePrimaryButton(string text, Point loc, Color color)
        {
            var b = new Button
            {
                Text = text,
                Location = loc,
                Size = new Size(112, 34),
                Font = new Font("Microsoft YaHei UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = color,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = ControlPaint.Dark(color, 0.1f);
            return b;
        }

        static Button MakeSecondaryButton(string text, Point loc)
        {
            var b = new Button
            {
                Text = text,
                Location = loc,
                Size = new Size(112, 34),
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
                Text = "卸载程序",
                Font = new Font("Microsoft YaHei UI", 9f),
                ForeColor = Color.FromArgb(0xE8, 0xC9, 0xC5),
                BackColor = Color.Transparent,
                Location = new Point(65, 40),
                AutoSize = true
            };
            h.Controls.Add(t);
            h.Controls.Add(sub);
            return h;
        }

        void BuildLayout()
        {
            lblStep = MakeLabel("", 9f, FontStyle.Regular, TextDim, new Point(500, 6));

            // --- page 1: confirm ---
            page1 = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            page1.Controls.Add(MakeLabel("卸载 DeepSeek Harness", 17f, FontStyle.Bold, TextMain, new Point(36, 26)));
            page1.Controls.Add(MakeLabel("将删除以下内容:", 10f, FontStyle.Bold, TextMain, new Point(36, 68)));
            page1.Controls.Add(MakeLabel("•  桌面与开始菜单快捷方式", 10f, FontStyle.Regular, TextMain, new Point(52, 96)));
            page1.Controls.Add(MakeLabel("•  安装目录:" + Program.RootDir, 10f, FontStyle.Regular, TextMain, new Point(52, 124)));
            page1.Controls.Add(MakeLabel("•  安装目录内的全部数据(会话/配置/插件)", 10f, FontStyle.Regular, TextMain, new Point(52, 152)));
            chkExtra = new CheckBox
            {
                Text = "同时删除自定义数据目录:" + (Program.ExtraDir ?? "(未配置)"),
                Font = new Font("Microsoft YaHei UI", 9.5f),
                ForeColor = TextMain,
                Location = new Point(52, 186),
                AutoSize = true,
                Checked = false,
                Enabled = Program.ExtraDir != null
            };
            page1.Controls.Add(chkExtra);
            page1.Controls.Add(MakeLabel("提示:卸载前建议先运行「导出数据.cmd」备份凭证与会话。", 9.5f, FontStyle.Regular, Color.FromArgb(0xB4, 0x77, 0x1E), new Point(36, 232)));
            page1.Controls.Add(lblStep);

            // --- page 2: progress / done (in-place transition) ---
            page2 = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            lblTitle = MakeLabel("正在卸载", 17f, FontStyle.Bold, TextMain, new Point(36, 26));
            progress = new ProgressBar
            {
                Location = new Point(36, 100), Size = new Size(548, 18),
                Style = ProgressBarStyle.Continuous
            };
            lblStatus = new Label
            {
                Text = "准备中...",
                Location = new Point(36, 134), Size = new Size(548, 60),
                Font = new Font("Microsoft YaHei UI", 9.5f),
                ForeColor = TextDim
            };
            page2.Controls.Add(lblTitle); page2.Controls.Add(progress); page2.Controls.Add(lblStatus);
            page2.Controls.Add(lblStep);

            btnUninstall = MakePrimaryButton("卸载", new Point(392, 356), Color.FromArgb(0xC0, 0x39, 0x2B));
            btnCancel = MakeSecondaryButton("取消", new Point(272, 356));
            btnFinish = MakePrimaryButton("完成", new Point(392, 356), Accent);
            btnCancel.Visible = btnFinish.Visible = false;

            btnUninstall.Click += (s, e) =>
            {
                btnUninstall.Enabled = false;
                ShowPage(page2);
                bgw.RunWorkerAsync();
            };
            btnCancel.Click += (s, e) => { Application.Exit(); };
            btnFinish.Click += (s, e) => { Result = 0; Application.Exit(); };

            Controls.Add(btnUninstall); Controls.Add(btnCancel); Controls.Add(btnFinish);
            Controls.Add(page1); Controls.Add(page2);
            Controls.Add(MakeHeader());
        }

        void ShowPage(Panel p)
        {
            page1.Visible = page2.Visible = false;
            p.Visible = true;
            btnUninstall.Visible = (p == page1);
            btnCancel.Visible = (p == page1);
            btnFinish.Visible = (p == page2 && done);
            lblStep.Text = p == page1 ? "确认" : done ? "完成" : "卸载中";
        }

        bool done = false;

        BackgroundWorker bgw = new BackgroundWorker();
        void wireBg()
        {
            bgw.WorkerReportsProgress = true;
            bgw.DoWork += (s, e) =>
            {
                try
                {
                    // 1. shortcuts
                    bgw.ReportProgress(5, "正在删除快捷方式...");
                    string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    string publicDesktop = Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory);
                    TryDelete(Path.Combine(desktop, "DSH Web.lnk"));
                    TryDelete(Path.Combine(publicDesktop, "DSH Web.lnk"));
                    string sm = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "DeepSeek Harness");
                    TryDeleteDir(sm);
                    bgw.ReportProgress(12, "快捷方式已删除");

                    // 2. custom data dir (opt-in)
                    if (chkExtra.Checked && Program.ExtraDir != null)
                    {
                        bgw.ReportProgress(18, "正在删除自定义数据目录...");
                        TryDeleteDir(Program.ExtraDir);
                        bgw.ReportProgress(24, "数据目录已删除");
                    }

                    // 3. delete the install directory contents in-process
                    bgw.ReportProgress(28, "正在清理安装目录...");
                    long total = CountFiles(Program.RootDir);
                    long done = 0;
                    DeleteContents(Program.RootDir, Application.ExecutablePath,
                        (d, t) =>
                        {
                            done = d;
                            int pct = 28 + (int)(62 * Math.Min(1.0, (double)d / Math.Max(1, t)));
                            bgw.ReportProgress(Math.Min(pct, 90), "正在删除文件...");
                        });
                    bgw.ReportProgress(92, "正在完成清理...");

                    // 4. schedule removal of the leftover folder (contains this exe)
                    // A copy of this exe in %TEMP% deletes the whole install dir,
                    // then removes itself — no console windows involved.
                    string copy = Path.Combine(Path.GetTempPath(), "dsh_cleanup_" + Guid.NewGuid().ToString("N").Substring(0, 8) + ".exe");
                    File.Copy(Application.ExecutablePath, copy, true);
                    Process.Start(new ProcessStartInfo(copy, "\"" + Program.RootDir + "\"")
                    { UseShellExecute = true, WindowStyle = ProcessWindowStyle.Hidden });
                    bgw.ReportProgress(100, "卸载完成");
                }
                catch (Exception ex)
                {
                    bgw.ReportProgress(100, "卸载过程中出现问题:" + ex.Message);
                }
            };
            bgw.ProgressChanged += (s, e) =>
            {
                if (progress.Value != e.ProgressPercentage) progress.Value = e.ProgressPercentage;
                string txt = e.UserState as string;
                if (!string.IsNullOrEmpty(txt)) lblStatus.Text = txt;
            };
            bgw.RunWorkerCompleted += (s, e) =>
            {
                done = true;
                lblTitle.Text = "✓ 卸载完成";
                lblTitle.ForeColor = Color.FromArgb(0x2E, 0x9E, 0x5B);
                progress.Visible = false;
                lblStatus.Text = "DeepSeek Harness 已成功卸载,安装目录已清理。\r\n残留文件将在窗口关闭后自动清除。";
                ShowPage(page2);
            };
        }

        static long CountFiles(string dir)
        {
            try { return Directory.GetFiles(dir, "*", SearchOption.AllDirectories).Length; }
            catch { return 1; }
        }

        static void DeleteContents(string dir, string keep, Action<long, long> progress, ref long done, long total)
        {
            foreach (string f in Directory.GetFiles(dir))
            {
                if (keep != null && string.Equals(f, keep, StringComparison.OrdinalIgnoreCase)) { done++; continue; }
                try { File.Delete(f); } catch { }
                done++;
                progress(done, total);
            }
            foreach (string d in Directory.GetDirectories(dir))
            {
                try
                {
                    DeleteContents(d, keep, progress, ref done, total);
                    Directory.Delete(d);
                }
                catch { }
            }
        }

        static void DeleteContents(string dir, string keep, Action<long, long> progress)
        {
            long done = 0;
            long total = CountFiles(dir);
            DeleteContents(dir, keep, progress, ref done, total);
        }

        static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { }
        }

        static void TryDeleteDir(string path)
        {
            try { if (Directory.Exists(path)) Directory.Delete(path, true); } catch { }
        }
    }
}
