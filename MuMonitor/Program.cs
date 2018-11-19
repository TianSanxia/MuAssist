using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.IO;
using System.Net.Mail;
using System.Windows.Forms;
using System.Timers;
using System.Drawing.Imaging;

namespace MuMonitor
{
    class Program
    {
        const string ImageFile = "MuScreen.jpg";
        static string fileFullName = string.Empty;
        static int captureIntervalInMins = 60;
        static System.Timers.Timer workTimer;
        static string email = string.Empty;
        static string emailPwd = string.Empty;
        static Dictionary<string, string> smtpServerDic = new Dictionary<string, string> {
                { "163.COM", "smtp.163.com" },
                { "QQ.COM", "smtp.qq.com" },
                { "OUTLOOK.COM", "smtp-mail.outlook.com"},
                { "LIVE.COM", "smtp.live.com" },
                { "LIVE.CN", "smtp-mail.outlook.com"},
            };

        static void Main(string[] args)
        {
            if(args.Length < 2)
            {
                Console.WriteLine("Input email address and password!");
                PrintUsage();
                return;
            }

            email = args[0];
            emailPwd = args[1];
            var emailParts = email.Split('@');

            if (emailParts.Length != 2)
            {
                Console.WriteLine("Invalid email address!");
                PrintUsage();
                return;
            }

            if(GetSMTPServerFromType(emailParts[1]) == null)
            {
                Console.WriteLine("The email type is not supported!");
                return;
            }
            

            if (args.Length >=3)
            {
                if(!int.TryParse(args[2], out captureIntervalInMins))
                {
                    captureIntervalInMins = 60;
                }

                if(captureIntervalInMins == 0)
                {
                    captureIntervalInMins = 1;
                }
            }

            //That's it! Save the image in the directory and this will work like charm. 
            try
            {
                fileFullName = string.Format(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) +
                      @"\" + ImageFile);
            }
            catch (Exception er)
            {
                Console.WriteLine("Sorry, there was an error: " + er.Message);
            }


            // timer, exception, emails, analisis
            SetTimer();

            while (true)
            {
                Console.Write("Input 'X' to exit:");
                string input = Console.ReadLine();
                if (input.Equals("x", StringComparison.InvariantCultureIgnoreCase))
                {
                    return;
                }
            }
        }

        static void PrintUsage()
        {
            Console.WriteLine("Usage: MuMonitor.exe EmailAddress EmailPassword [Interval In Minutes]");
            Console.WriteLine("Example: MuMonitor.exe mu@163.com muPassword 30");
            Console.WriteLine();
        }

        static bool TakeScreenshot(string fileFullName)
        {
            // Determine the size of the "virtual screen", which includes all monitors.
            int screenLeft = SystemInformation.VirtualScreen.Left;
            int screenTop = SystemInformation.VirtualScreen.Top;
            int screenWidth = SystemInformation.VirtualScreen.Width;
            int screenHeight = SystemInformation.VirtualScreen.Height;

            // Start the process... 
            Console.WriteLine("Taking screenshot...");
            using (Bitmap memoryImage = new Bitmap(screenWidth, screenHeight))
            {
                // Create graphics 
                Console.WriteLine("Creating Graphics...");
                using (Graphics memoryGraphics = Graphics.FromImage(memoryImage))
                {
                    // Copy data from screen 
                    Console.WriteLine("Copying data from screen...");
                    memoryGraphics.CopyFromScreen(screenLeft, screenTop, 0, 0, memoryImage.Size);

                    // Save it! 
                    Console.WriteLine("Saving the image...");
                    memoryImage.Save(fileFullName, ImageFormat.Jpeg);

                    // Write the message, 
                    Console.WriteLine("Picture has been saved...");

                    return true;
                }
            }
        }

        static void SendEmail(string message, string attachment)
        {
            Console.WriteLine("Sending email...");

            var emailParts = email.Split('@');
            string userName = emailParts[0];
            string emailType = emailParts[1];
            string password = emailPwd;
            string smtpServer = GetSMTPServerFromType(emailType);

            string from = email; // "奇迹MU监控小助手";
            string to = email;
            DateTime sendTime = DateTime.Now;

            SmtpClient client = new SmtpClient
            {
                Port = 25,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Host = smtpServer,
                Credentials = new System.Net.NetworkCredential(userName, password)
            };

            MailMessage mail = new MailMessage(from, to)
            {
                Subject = string.Format("{0} 奇迹MU 状态", sendTime.ToString("u")),
                Body = message
            };

            if (!string.IsNullOrEmpty(attachment))
            {
                mail.Attachments.Add(new Attachment(attachment));
            }
            client.Send(mail);
        }

        private static string GetSMTPServerFromType(string emailType)
        {

            string normalizedType = emailType.Trim().ToUpper();

            if (smtpServerDic.ContainsKey(normalizedType))
            {
                return smtpServerDic[normalizedType];
            }

            return null;
        }

        private static void SetTimer()
        {
            // Create a timer with a two second interval.
            workTimer = new System.Timers.Timer(captureIntervalInMins * 60 * 1000);
            // Hook up the Elapsed event for the timer. 
            workTimer.Elapsed += OnTimedEvent;
            workTimer.AutoReset = true;
            workTimer.Enabled = true;
        }

        private static void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            Console.WriteLine("Triggered on {0}", DateTime.Now.ToString("u"));
            try
            {
                if (TakeScreenshot(fileFullName))
                {
                    SendEmail("", fileFullName);
                }
            }
            catch(Exception ex)
            {
                ReportError(ex.StackTrace);
            }
        }

        private static void ReportError(string error)
        {
            SendEmail(error, null);
        }
    }
}
