using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using MuMonitor;

namespace MuMonitor.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const string NewLineChar = "\n";
        string email;
        string emailPwd;
        int checkIntervalInMins;
        bool shutdownOnDisconnected;
        bool takeScreenshot;
        string fileFullName;
        object timerLock = new object();
        Timer workTimer;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void btTest_Click(object sender, RoutedEventArgs e)
        {
            this.tbStatus.Text = string.Empty;
            var btTestColor = this.btTest.Background;
            this.btTest.IsEnabled = false;
            this.btTest.Background = new SolidColorBrush(Colors.LightGray);
            if (!GetUserInput())
            {
                this.btTest.IsEnabled = true;
                this.btTest.Background = btTestColor;
                return;
            }
            CheckOnce();
            this.btTest.IsEnabled = true;
            this.btTest.Background = btTestColor;
        }

        private void btStartCheck_Click(object sender, RoutedEventArgs e)
        {
            if (this.btStartCheck.Content.ToString().Equals("开启监测"))
            {
                if (!GetUserInput())
                {
                    return;
                }

                this.tbStatus.Text = "开始监测。" + NewLineChar;
                this.tbStatus.Text += "第一次分析大概开始于"
                    + DateTime.Now.AddMinutes(checkIntervalInMins).ToString("u")
                    + NewLineChar;
                this.btStartCheck.Content = "停止监测";
                SetTimer();
            }
            else
            {
                this.tbStatus.Text += "停止监测。" + NewLineChar;
                this.btStartCheck.Content = "开启监测";
                lock (timerLock)
                {
                    if (workTimer != null)
                    {
                        workTimer.Stop();
                        workTimer = null;
                    }
                }
            }
        }

        private void SetTimer()
        {
            lock (timerLock)
            {
                if(workTimer != null)
                {
                    workTimer.Stop();
                    workTimer = null;
                }
                // Create a timer with a two second interval.
                workTimer = new Timer(checkIntervalInMins * 60 * 1000);
                // Hook up the Elapsed event for the timer. 
                workTimer.Elapsed += OnTimedEvent;
                workTimer.AutoReset = true;
                workTimer.Enabled = true;
            }
        }
        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            AppendStatus("分析开始于" + DateTime.Now.ToString("u") + NewLineChar);
            CheckOnce();
        }

        private void AppendStatus(string value)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                this.tbStatus.Text += value + NewLineChar;
            }));
        }

        private void CheckOnce()
        {

            try
            {
                bool allDisconnected = false;
                MuNetworkMonitor networkMonitor = new MuNetworkMonitor();
                string status = networkMonitor.Analyze(out allDisconnected);
                string error = string.Empty;
                string hostName = Environment.GetEnvironmentVariable("COMPUTERNAME");
                status = string.Format(CultureInfo.InvariantCulture, "[{0}] {1}", hostName, status);

                if (shutdownOnDisconnected & allDisconnected)
                {
                    status += " 即将关机!";
                }

                bool bScreen = false;

                if (takeScreenshot)
                {
                    bScreen = MuHelper.TakeScreenshot(fileFullName, out error);
                }

                AppendStatus(status + NewLineChar);

                AppendStatus("邮件发送中。。。" + NewLineChar);
                string sendError;
                if(!MuHelper.SendEmail(
                    email,
                    emailPwd,
                    status,
                    bScreen ? status : error,
                    bScreen ? fileFullName : null,
                    out sendError))
                {
                    AppendStatus("错误发生，请联系开发者：" + sendError + NewLineChar);
                }
                else if (shutdownOnDisconnected & allDisconnected)
                {
                    MuHelper.ShutdownComputer();
                }
            }
            catch (Exception ex)
            {
                AppendStatus("错误发生，请联系开发者：" + ex.Message + NewLineChar);
                MuHelper.ReportError(email, emailPwd, ex.Message + "\n" + ex.StackTrace);
            }
        }

        private bool GetUserInput()
        {
            this.lbValidation.Content = string.Empty;
            email = this.tbEmailAddress.Text;
            if(string.IsNullOrEmpty(email))
            {
                this.lbValidation.Content = "邮件地址必需输入！";
                return false;
            }

            emailPwd = this.pbEmailPassword.Password;
            if (string.IsNullOrEmpty(emailPwd))
            {
                this.lbValidation.Content = "邮件密码必需输入！";
                return false;
            }

            checkIntervalInMins = int.Parse((string)((ComboBoxItem)this.cbCheckDuration.SelectedValue).Content);
            shutdownOnDisconnected = this.cbAutoShowdown.IsChecked.HasValue ? this.cbAutoShowdown.IsChecked.Value : false;
            takeScreenshot = this.cbScreenshot.IsChecked.HasValue ? this.cbScreenshot.IsChecked.Value : false;

            var emailParts = email.Split('@');

            if (emailParts.Length != 2)
            {
                this.lbValidation.Content = "邮件地址格式不对!";
                return false;
            }

            if (MuHelper.GetSMTPServerFromType(emailParts[1]) == null)
            {
                this.lbValidation.Content = "邮件服务器不支持，目前只支持163.com和outlook.com";
                return false;
            }


            //That's it! Save the image in the directory and this will work like charm. 
            try
            {
                fileFullName = string.Format(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) +
                      @"\" + MuHelper.ScreenshotFile);
            }
            catch (Exception er)
            {
                this.lbValidation.Content = "发生了错误: " + er.Message;
                return false;
            }

            this.lbValidation.Content = string.Empty;
            return true;
        }


    }
}
