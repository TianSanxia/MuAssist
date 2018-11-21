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
using System.Configuration;
using System.Management;

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
                //{ "QQ.COM", "smtp.qq.com" },
                { "OUTLOOK.COM", "smtp-mail.outlook.com" }
            };
        static bool ShutdownOnDisconnected = false;

        static void Main(string[] args)
        {

            email = ConfigurationManager.AppSettings["EmailAddress"];
            emailPwd = ConfigurationManager.AppSettings["EmailPassword"];
            var emailParts = email.Split('@');

            if (emailParts.Length != 2)
            {
                Console.WriteLine("Invalid email address!");
                return;
            }

            if(GetSMTPServerFromType(emailParts[1]) == null)
            {
                Console.WriteLine("The email type is not supported!");
                return;
            }

            captureIntervalInMins = int.Parse(ConfigurationManager.AppSettings["CheckIntervalInMins"]);

            if(captureIntervalInMins < 1 )
            {
                captureIntervalInMins = 1;
            }

            ShutdownOnDisconnected = bool.Parse(ConfigurationManager.AppSettings["ShutdownOnDisconnected"]);

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
            try
            {
                Console.WriteLine("Sending email...");

                var emailParts = email.Split('@');
                string userName = emailParts[0];
                string emailType = emailParts[1];
                string password = emailPwd;
                string smtpServer = GetSMTPServerFromType(emailType);

                string from = email; 
                string to = email;
                DateTime sendTime = DateTime.Now;

                string normalizedType = emailType.Trim().ToUpper();

                using (SmtpClient client = new SmtpClient
                {
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Host = smtpServer,
                    
                })
                {
                    if(normalizedType.Equals("OUTLOOK.COM"))
                    {
                        client.Port = 587;
                        client.EnableSsl = true;
                        client.Credentials = new System.Net.NetworkCredential(email, password);
                    }
                    else
                    {
                        client.Credentials = new System.Net.NetworkCredential(userName, password);
                    }

                    using (MailMessage mail = new MailMessage(from, to)
                    {
                        Subject = message.Substring(0, message.Length > 50? 50: message.Length),
                        Body = message
                    })
                    {
                        if (!string.IsNullOrEmpty(attachment))
                        {
                            mail.Attachments.Add(new Attachment(attachment));
                        }
                        client.Send(mail);

                    }
                }

                if (attachment != null)
                {
                    File.Delete(attachment);
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("Failed to send email: {0}, {1}", ex.Message, ex.StackTrace);
            }
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
                bool allDisconnected = false;
                MuNetworkMonitor networkMonitor = new MuNetworkMonitor();
                string status = networkMonitor.Analyze(out allDisconnected);

                if (ShutdownOnDisconnected & allDisconnected)
                {
                    status += " 即将关机!";
                }
                Console.WriteLine(status);


                bool bScreen = TakeScreenshot(fileFullName);

                SendEmail(status, bScreen ? fileFullName : null);

                if(ShutdownOnDisconnected & allDisconnected)
                {                
                    Console.WriteLine("Shutdown ...");
                    Shutdown();
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

        static void Shutdown()
        {
            ManagementBaseObject mboShutdown = null;
            ManagementClass mcWin32 = new ManagementClass("Win32_OperatingSystem");
            mcWin32.Get();

            // You can't shutdown without security privileges
            mcWin32.Scope.Options.EnablePrivileges = true;
            ManagementBaseObject mboShutdownParams =
            mcWin32.GetMethodParameters("Win32Shutdown");

            // Flag 1 means we want to shut down the system. Use "2" to reboot.
            mboShutdownParams["Flags"] = "1";
            mboShutdownParams["Reserved"] = "0";
            foreach (ManagementObject manObj in mcWin32.GetInstances())
            {
                mboShutdown = manObj.InvokeMethod("Win32Shutdown",
                mboShutdownParams, null);
            }
        }
    }
}
