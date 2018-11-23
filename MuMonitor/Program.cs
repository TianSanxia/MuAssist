using System;
using System.Timers;
using System.Configuration;

namespace MuMonitor
{
    class Program
    {      
        static string fileFullName = string.Empty;
        static int captureIntervalInMins = 60;
        static Timer workTimer;
        static string email = string.Empty;
        static string emailPwd = string.Empty;
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

            if(MuHelper.GetSMTPServerFromType(emailParts[1]) == null)
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
                      @"\" + MuHelper.ScreenshotFile);
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

        private static void SetTimer()
        {
            // Create a timer with a two second interval.
            workTimer = new Timer(captureIntervalInMins * 60 * 1000);
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
                string error = string.Empty;

                if (ShutdownOnDisconnected & allDisconnected)
                {
                    status += " 即将关机!";
                }
                Console.WriteLine(status);


                bool bScreen = MuHelper.TakeScreenshot(fileFullName, out error);

                string sendError;
                if (!MuHelper.SendEmail(
                    email,
                    emailPwd,
                    status,
                    bScreen ? status : error,
                    bScreen ? fileFullName : null,
                    out sendError))
                {
                    Console.WriteLine(sendError);
                }
                else
                if (ShutdownOnDisconnected & allDisconnected)
                {
                    Console.WriteLine("Shutdown this computer...");
                    MuHelper.ShutdownComputer();
                }
            }
            catch(Exception ex)
            {
                MuHelper.ReportError(email, emailPwd, ex.Message + "\n" + ex.StackTrace);
            }
        }

    }
}
