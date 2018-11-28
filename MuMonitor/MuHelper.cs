using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.Mail;
using System.Runtime.InteropServices;
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

        public static bool TestEmail(
            string email,
            string emailPwd,
            out string error)
        {
            return SendEmail(email, emailPwd, "测试", "", null, out error);
        }

        public static bool SendEmail(
            string email,
            string emailPwd,
            string subject,
            string message,
            string[] attachments,
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
                        if (attachments != null)
                        {
                            foreach (var attachment in attachments)
                            {
                                mail.Attachments.Add(new Attachment(attachment));
                            }
                        }
                        client.Send(mail);

                    }
                }

                if (attachments != null)
                {
                    foreach (var attachment in attachments)
                    {
                        File.Delete(attachment);
                    }
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

        [DllImport("user32.dll")]
        static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, int nFlags);

        static Bitmap PrintWindow(IntPtr hwnd)
        {
            RECT rc;
            GetWindowRect(hwnd, out rc);

            Bitmap bmp = new Bitmap(rc.Width, rc.Height, PixelFormat.Format32bppArgb);
            Graphics gfxBmp = Graphics.FromImage(bmp);
            IntPtr hdcBitmap = gfxBmp.GetHdc();

            PrintWindow(hwnd, hdcBitmap, 0);

            gfxBmp.ReleaseHdc(hdcBitmap);
            gfxBmp.Dispose();

            return bmp;
        }

        public static bool CaptureProcess(Process process, string fileName)
        {
            RECT lpRect;
            GetWindowRect(process.MainWindowHandle, out lpRect);
            using (Bitmap bitmap = PrintWindow(process.MainWindowHandle))
            {
                bitmap.Save(fileName, ImageFormat.Jpeg);
            }
            return true;
        }

    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        private int _Left;
        private int _Top;
        private int _Right;
        private int _Bottom;

        public RECT(RECT Rectangle) : this(Rectangle.Left, Rectangle.Top, Rectangle.Right, Rectangle.Bottom)
        {
        }
        public RECT(int Left, int Top, int Right, int Bottom)
        {
            _Left = Left;
            _Top = Top;
            _Right = Right;
            _Bottom = Bottom;
        }

        public int X
        {
            get { return _Left; }
            set { _Left = value; }
        }
        public int Y
        {
            get { return _Top; }
            set { _Top = value; }
        }
        public int Left
        {
            get { return _Left; }
            set { _Left = value; }
        }
        public int Top
        {
            get { return _Top; }
            set { _Top = value; }
        }
        public int Right
        {
            get { return _Right; }
            set { _Right = value; }
        }
        public int Bottom
        {
            get { return _Bottom; }
            set { _Bottom = value; }
        }
        public int Height
        {
            get { return _Bottom - _Top; }
            set { _Bottom = value + _Top; }
        }
        public int Width
        {
            get { return _Right - _Left; }
            set { _Right = value + _Left; }
        }
        public Point Location
        {
            get { return new Point(Left, Top); }
            set
            {
                _Left = value.X;
                _Top = value.Y;
            }
        }
        public Size Size
        {
            get { return new Size(Width, Height); }
            set
            {
                _Right = value.Width + _Left;
                _Bottom = value.Height + _Top;
            }
        }

        public static implicit operator Rectangle(RECT Rectangle)
        {
            return new Rectangle(Rectangle.Left, Rectangle.Top, Rectangle.Width, Rectangle.Height);
        }
        public static implicit operator RECT(Rectangle Rectangle)
        {
            return new RECT(Rectangle.Left, Rectangle.Top, Rectangle.Right, Rectangle.Bottom);
        }
        public static bool operator ==(RECT Rectangle1, RECT Rectangle2)
        {
            return Rectangle1.Equals(Rectangle2);
        }
        public static bool operator !=(RECT Rectangle1, RECT Rectangle2)
        {
            return !Rectangle1.Equals(Rectangle2);
        }

        public override string ToString()
        {
            return "{Left: " + _Left + "; " + "Top: " + _Top + "; Right: " + _Right + "; Bottom: " + _Bottom + "}";
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public bool Equals(RECT Rectangle)
        {
            return Rectangle.Left == _Left && Rectangle.Top == _Top && Rectangle.Right == _Right && Rectangle.Bottom == _Bottom;
        }

        public override bool Equals(object Object)
        {
            if (Object is RECT)
            {
                return Equals((RECT)Object);
            }
            else if (Object is Rectangle)
            {
                return Equals(new RECT((Rectangle)Object));
            }

            return false;
        }
    }
}
