using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MuMonitor
{
    public class MuHelper
    {
        public const string ScreenshotFile = "MuScreen.jpg";

        static Dictionary<string, string> smtpServerDic = new Dictionary<string, string> {
                { "163.COM", "smtp.163.com" },
                //{ "QQ.COM", "smtp.qq.com" },
                { "OUTLOOK.COM", "smtp-mail.outlook.com" }
            };

        public static bool SendEmail(
            string email, 
            string emailPwd, 
            string subject, 
            string message, 
            string attachment,
            out string error)
        {
            error = string.Empty;
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
                    if (normalizedType.Equals("OUTLOOK.COM"))
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
                        Subject = subject.Substring(0, subject.Length > 50 ? 50 : subject.Length),
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

                return true;
            }
            catch (Exception ex)
            {
                error = string.Format("邮件发送失败: {0}, {1}", ex.Message, ex.StackTrace);
                return false;
            }
        }

        public static string GetSMTPServerFromType(string emailType)
        {
            string normalizedType = emailType.Trim().ToUpper();

            if (smtpServerDic.ContainsKey(normalizedType))
            {
                return smtpServerDic[normalizedType];
            }

            return null;
        }

        public static bool TakeScreenshot(string fileFullName, out string message)
        {
            message = string.Empty;
            try
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
            catch (Exception ex)
            {
                message = ex.Message + "\n" + ex.StackTrace;
                return false;
            }
        }

        public static void ReportError(string email, string emailPwd, string error)
        {
            string sendError;
            SendEmail(
                   email,
                   emailPwd,
                   "程序出问题了，请联系开发者",
                   error,
                   null,
                   out sendError);
        }

        public static void ShutdownComputer()
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
