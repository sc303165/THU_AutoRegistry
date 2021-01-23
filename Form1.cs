using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        string ID, pwd;
        string Version = "2.0.0";
        string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log.ini");
        public static Form2 f2;
        DateTime taptime;
        bool isprogressing = false;
        string nextstr = "";
        bool Hasjustcompleted = false;
        bool IsMinimized = false;
        int Netcount = 0;
        public Form1()
        {
            SetWebBrowserFeatures(11);
            InitializeComponent();
        }
        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto, ExactSpelling = true)]
        public static extern IntPtr GetForegroundWindow(); //获得本窗体的句柄
        [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "SetForegroundWindow")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);//设置此窗体为活动窗体
        public IntPtr Handle1;
        private void button1_Click(object sender, EventArgs e)
        {
            timer1.Stop();
            timer2.Stop();
            try
            {
                //打卡间隔时间不得短于10s
                if (Convert.ToDouble(textBox5.Text) * 3600 < 10)
                {
                    textBox5.Text = "0.2";
                }

                pwd_error.Text = "";
                button1.Enabled = false;
                button1.Text = "自动打卡中…";
                isprogressing = true;//指示是否正在访问网页自动打卡的bool值
                //点击开始按钮：将文本框的账密存入log。替换掉原有的ID行和pwd行
                //如相同，则开始自动打卡
                ID = textBox1.Text;
                pwd = textBox2.Text;
                /*log.ini文件结构
                 * ID=
                 * pwd=
                 * lastregistertime=
                 */
                webBrowser1.Navigate("https://thos.tsinghua.edu.cn/");
            }
            catch (FormatException fe)
            {
                pwd_error.Text = "输入格式不正确";
                button3.PerformClick();
            }
        }

        private void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            if (webBrowser1.Document.Title == "导航已取消" || webBrowser1.Document.Title == "无法访问此页")
            {
                //断网情形
                if (pwd_error.Text != "输入格式不正确")
                {
                    pwd_error.Text = "请检查网络连接";
                    button3.PerformClick();
                    timer2.Start();
                }
            }
            //  Console.WriteLine(e.Url.ToString());
            if (e.Url.ToString().ToLower().IndexOf("id.tsinghua") > 0)
            {
                HtmlDocument doc = webBrowser1.Document;
                for (int i = 0; i < doc.All.Count; i++)
                {
                    switch (doc.All[i].Name)
                    {
                        case "i_user":
                            // 用户名
                            doc.All[i].SetAttribute("value", ID);
                            break;
                        case "i_pass":
                            // 密码
                            doc.All[i].SetAttribute("value", pwd);
                            break;
                    }
                }
                webBrowser1.Document.InvokeScript("doLogin");
            }

            if (e.Url.ToString() == "https://thos.tsinghua.edu.cn/fp/view?m=fp#act=fp/formHome")
            {
                if (webBrowser1.ReadyState < WebBrowserReadyState.Complete || webBrowser1.Url.ToString() == LastUrl) return;
                LastUrl = webBrowser1.Url.ToString();
            }

            if (e.Url.ToString().IndexOf("https://thos.tsinghua.edu.cn/fp/bsdesign.do?") > -1)
            {

            }

            if (e.Url.ToString().IndexOf("https://thos.tsinghua.edu.cn/fp/formParser?status=select") > -1)
            {
                if (WindowState == FormWindowState.Minimized)
                {
                    IsMinimized = true;
                    this.Opacity = 0;
                    this.WindowState = FormWindowState.Normal;
                }

                HtmlElement element = webBrowser1.Document.GetElementById("mag_take_cancel");
                if (element != null)
                {
                    element.InvokeMember("click");
                }
                //检测提交按钮
                HtmlElement commit = webBrowser1.Document.GetElementById("commit");
                if (commit != null)
                {
                    commit.InvokeMember("click");
                }
            }

            if (e.Url.ToString().IndexOf("https://thos.tsinghua.edu.cn/fp/view?m=fp#act=fp/myserviceapply/indexNew") > -1)
            {      //打卡完成
                Hasjustcompleted = true;
                taptime = DateTime.Now;
                string taptimes = taptime.ToString();
                current_status.Text = taptimes + " 已打卡";
                if (IsMinimized)
                {
                    IsMinimized = false;
                    this.WindowState = FormWindowState.Minimized;
                    this.Opacity = 1;
                }

                this.Text = "清华大学疫情日报自动打卡机V" + Version + "——" + current_status.Text;
                string logtext = "ID=" + ID + "\npwd=" + pwd + "\nlastregistertime=" + taptimes;
                File.WriteAllText(path, logtext);
                timer1.Start();
                isprogressing = false;
                nextstr = taptime.AddHours(Convert.ToDouble(textBox5.Text)).ToString();
                notifyIcon1.Text = "清华自动打卡机V" + Version + "\n下次打卡时间：" + nextstr;
                next_time.Text = nextstr;
                Hasjustcompleted = false;
            }


            if (e.Url.ToString() == "https://id.tsinghua.edu.cn/do/off/ui/auth/login/check")
            {
                if (webBrowser1.ReadyState < WebBrowserReadyState.Complete || webBrowser1.Url.ToString() == LastUrl) return;
                LastUrl = webBrowser1.Url.ToString();
                //密码错误的情况 <span id="msg_note" class="red">您的用户名或密码不正确，请重试！
                HtmlDocument doc = webBrowser1.Document;
                HtmlElement he0 = doc.GetElementById("msg_note");
                if (he0 != null)
                {
                    webBrowser1.Stop();
                    pwd_error.Text = "密码错误，请重新输入";
                    button1.Enabled = true;
                    button1.Text = "启动自动打卡";
                }
            }


        }
        private void webBrowser1_Navigated(object sender, WebBrowserNavigatedEventArgs e)
        {
            address.Text = e.Url.ToString();
            if (e.Url.ToString() == "https://thos.tsinghua.edu.cn/fp/view?m=fp#act=fp/formHome")
            {
                if (webBrowser1.ReadyState < WebBrowserReadyState.Complete) return;
                //  if (webBrowser1.ReadyState < WebBrowserReadyState.Complete || webBrowser1.Url.ToString() == LastUrl) return;
                // LastUrl = webBrowser1.Url.ToString();
                webBrowser1.Navigate("https://thos.tsinghua.edu.cn/fp/view?m=fp#from=hall&serveID=b44e2daf-0ef6-4d11-a115-0eb0d397934f&act=fp/serveapply");
            }

        }

        static void SetWebBrowserFeatures(int ieVersion)
        {
            // don't change the registry if running in-proc inside Visual Studio  
            if (LicenseManager.UsageMode != LicenseUsageMode.Runtime)
                return;
            //获取程序及名称  
            var appName = System.IO.Path.GetFileName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            //得到浏览器的模式的值  
            UInt32 ieMode = GeoEmulationModee(ieVersion);
            var featureControlRegKey = @"HKEY_CURRENT_USER\Software\Microsoft\Internet Explorer\Main\FeatureControl\";
            //设置浏览器对应用程序（appName）以什么模式（ieMode）运行  
            Registry.SetValue(featureControlRegKey + "FEATURE_BROWSER_EMULATION",
                appName, ieMode, RegistryValueKind.DWord);
            // enable the features which are "On" for the full Internet Explorer browser  
            //不晓得设置有什么用  
            Registry.SetValue(featureControlRegKey + "FEATURE_ENABLE_CLIPCHILDREN_OPTIMIZATION",
                appName, 1, RegistryValueKind.DWord);
        }
        /// <summary>  
        /// 获取浏览器版本  
        /// </summary>  
        /// <returns></returns>  
        static int GetBrowserVersion()
        {
            int browserVersion = 0;
            using (var ieKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Internet Explorer",
                RegistryKeyPermissionCheck.ReadSubTree,
                System.Security.AccessControl.RegistryRights.QueryValues))
            {
                var version = ieKey.GetValue("svcVersion");
                if (null == version)
                {
                    version = ieKey.GetValue("Version");
                    if (null == version)
                        throw new ApplicationException("Microsoft Internet Explorer is required!");
                }
                int.TryParse(version.ToString().Split('.')[0], out browserVersion);
            }
            if (browserVersion < 7)
            {
                throw new ApplicationException("不支持的浏览器版本!");
            }
            return browserVersion;
        }
        /// <summary>  
        /// 通过版本得到浏览器模式的值  
        /// </summary>  
        /// <param name="browserVersion"></param>  
        /// <returns></returns>  
        static UInt32 GeoEmulationModee(int browserVersion)
        {
            UInt32 mode = 11000; // Internet Explorer 11. Webpages containing standards-based !DOCTYPE directives are displayed in IE11 Standards mode.   
            switch (browserVersion)
            {
                case 7:
                    mode = 7000; // Webpages containing standards-based !DOCTYPE directives are displayed in IE7 Standards mode.   
                    break;
                case 8:
                    mode = 8000; // Webpages containing standards-based !DOCTYPE directives are displayed in IE8 mode.   
                    break;
                case 9:
                    mode = 9000; // Internet Explorer 9. Webpages containing standards-based !DOCTYPE directives are displayed in IE9 mode.                      
                    break;
                case 10:
                    mode = 10000; // Internet Explorer 10.  
                    break;
                case 11:
                    mode = 11000; // Internet Explorer 11  
                    break;
            }
            return mode;
        }
        public string LastUrl
        {
            get { return _LastUrl; }
            set { _LastUrl = value; }
        }
        private string _LastUrl;

        private void button2_Click(object sender, EventArgs e)
        {
            f2 = new Form2();
            f2.Show();
        }

        private void textBox5_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
            {
                button1.PerformClick();
            }
            if ((e.KeyChar < 48 || e.KeyChar > 57) && e.KeyChar != 8 && e.KeyChar != 13 && e.KeyChar != 46)
            {
                e.Handled = true;
            }
            if (e.KeyChar == 46)
            {
                //已经有一个小数点
                if (((TextBox)sender).Text.IndexOf(".") >= 0)
                {
                    e.Handled = true;
                }
                //如果小数点是第一位的话就取消
                if (((TextBox)sender).SelectionStart == 0)
                {
                    e.Handled = true;
                }
            }

        }

        private void timer1_Tick_1(object sender, EventArgs e)
        {
            //1秒钟检测一次系统时间
            DateTime currenttime = DateTime.Now;
            if ((currenttime - taptime).TotalHours >= Convert.ToDouble(textBox5.Text))
            {
                button1.Enabled = true;
                button1.PerformClick();
            }
        }

        private void textBox2_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
            {
                button1.PerformClick();
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            timer1.Stop();
            timer2.Stop();
            Netcount = 0;
            webBrowser1.Stop();
            button1.Enabled = true;
            button1.Text = "启动自动打卡";
        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                this.ShowInTaskbar = false;
                try
                {
                    DateTime next = taptime.AddHours(Convert.ToDouble(textBox5.Text));
                    string tiptext = " ";
                    if (button1.Enabled)
                    {
                        tiptext = "自动打卡未开启";
                        notifyIcon1.ShowBalloonTip(3000, "打卡机已隐藏", tiptext, ToolTipIcon.Info);
                    }
                    else if (Hasjustcompleted)
                    {
                        tiptext = current_status.Text;
                        notifyIcon1.ShowBalloonTip(3000, "打卡完成", tiptext, ToolTipIcon.Info);
                    }
                    else if (!isprogressing)//没有正在访问网页
                    {
                        tiptext = "下次打卡预计时间：" + next.ToString();
                        nextstr = tiptext;
                        notifyIcon1.Text = "清华自动打卡机V" + Version + nextstr;
                        notifyIcon1.ShowBalloonTip(3000, "打卡机已隐藏", tiptext, ToolTipIcon.Info);
                    }
                    else if (isprogressing)//正在访问网页
                    {
                        tiptext = "打卡进行中";
                        notifyIcon1.ShowBalloonTip(3000, "打卡机已隐藏", tiptext, ToolTipIcon.Info);
                    }

                }
                catch (FormatException fe)
                {

                }
            }
        }



        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {

        }

        private void exit_Click(object sender, EventArgs e)
        {
            this.Dispose();
            Application.Exit();
        }

        private void show_Click(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Normal)
            {
                this.WindowState = FormWindowState.Minimized;
                //show.Text = "显示窗口";
            }
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.WindowState = FormWindowState.Normal;
                // show.Text = "隐藏窗口";
            }

        }

        private void button4_Click(object sender, EventArgs e)
        {
            try
            {
                File.Delete(path);
            }
            catch (Exception eee) { }
            textBox1.Text = "";
            textBox2.Text = "";
            MessageBox.Show("系统缓存的账密信息已删除。", "提示");
        }
        private void notifyIcon1_Click(object sender, EventArgs e)
        {
            MouseEventArgs Mouse_e = (MouseEventArgs)e;
            if (Mouse_e.Button == MouseButtons.Left)
            {
                ShowInTaskbar = true;
                this.Show();
                this.WindowState = FormWindowState.Normal;
                this.TopMost = true;
                this.TopMost = false;
                SetForegroundWindow(Handle1);
                this.Focus();
                this.Activate();
            }
        }
        private void notifyIcon1_DoubleClick(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Normal;
            SetForegroundWindow(Handle1);
            this.Focus();
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.WindowState = FormWindowState.Normal;
            SetForegroundWindow(Handle1);
            this.Focus();
        }

        private void webBrowser1_Navigating(object sender, WebBrowserNavigatingEventArgs e)
        {
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            //timer2启动条件：断网时尝试打卡
            Netcount++;
            button1.Text = "等待联网\n(" + (15 - Netcount).ToString() + "s)";
            if (Netcount >= 15)//联网检测周期设定为15s
            {
                Netcount = 0;
                button1.Enabled = true;
                button1.PerformClick();
                timer2.Stop();
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            WindowState = FormWindowState.Minimized;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.Text = "清华大学疫情日报自动打卡机V" + Version;
            Handle1 = this.Handle;
            //timer1.Start();
            //从log文件读取账密和上次打卡时间
            /*log.ini文件结构
             * ID=
             * pwd=
             * lastregistertime=
             */
            if (File.Exists(path))
            {
                StreamReader sr = new StreamReader(path);
                string ID_log = sr.ReadLine();
                string pwd_log = sr.ReadLine();
                string lastregistertime = sr.ReadLine();
                if (ID_log != null && pwd_log != null && lastregistertime != null)
                {
                    ID_log = ID_log.Substring(ID_log.IndexOf("=") + 1);
                    pwd_log = pwd_log.Substring(pwd_log.IndexOf("=") + 1);
                    lastregistertime = lastregistertime.Substring(lastregistertime.IndexOf("=") + 1);
                }
                textBox1.Text = ID_log;
                textBox2.Text = pwd_log;// else textBox2.Text = "";
                if (lastregistertime != "")
                {
                    current_status.Text = lastregistertime + " 已打卡";
                    taptime = Convert.ToDateTime(lastregistertime);
                }
                else
                {
                    current_status.Text = "未打卡";
                }
                sr.Close();

                if (button1.Enabled == false && isprogressing == false)
                {
                    nextstr = "\n下次打卡时间：" + taptime.AddHours(Convert.ToDouble(textBox5.Text)).ToString();
                }
                else
                {
                    nextstr = "\n下次打卡时间：";
                }
                notifyIcon1.Text = "清华自动打卡机V" + Version + nextstr;
            }

        }
    }
}
