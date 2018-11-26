using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;

namespace MuMonitor
{
    public class MuNetworkMonitor
    {
        private const string ProcessName = "main";
        private int TraceDurationInMins = 1;
        private bool TakeScreenshot = false;

        public MuNetworkMonitor(bool screenshot)
        {
            TraceDurationInMins = 1;
            TakeScreenshot = screenshot;
        }

        public string Analyze(out bool allDisconnected, out string[] screenFiles)
        {
            string message = string.Empty;
            screenFiles = null;

            // Get Progress "main.exe"
            Process[] proArr = Process.GetProcessesByName(ProcessName);
            if (proArr == null || proArr.Length == 0)
            {
                message = "奇迹进程找不到了.";
                allDisconnected = true;
            }
            else
            {
                allDisconnected = true;
                var pids = from process in proArr select process.Id;

                using (NetworkPerformanceReporter perfReporter = new NetworkPerformanceReporter(pids.ToArray()))
                {
                    Console.WriteLine("Capturing network and analyze.");
                    perfReporter.Initialize();
                    Thread.Sleep(TraceDurationInMins * 1000 * 60); // Wait for tracing
                    perfReporter.Stop();
                    var perfDatas = perfReporter.GetNetworkPerformanceData();

                    int index = 1;
                    
                    foreach (var perf in perfDatas)
                    {
                        bool online = false;
                        message += "MU_" + perf.Key + ":";
                        message += GetStatus(perf.Value, out online);
                        if(online)
                        {
                            allDisconnected = false;
                        }
                        Console.WriteLine("{2}, Send:{0} B/sec, Recv:{1} B/sec", perf.Value.BytesSent, perf.Value.BytesReceived, perf.Key);
                        index++;
                    }
                }

                if(TakeScreenshot)
                {
                    List<string> files = new List<string>();
                    foreach(var muProcess in proArr)
                    {
                        string muScreenFile = string.Format(
                            CultureInfo.InvariantCulture, 
                            "{0}\\MU_{1}.jpg", 
                            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                            muProcess.Id);
                        MuHelper.CaptureProcess(muProcess, muScreenFile);
                        files.Add(muScreenFile);
                    }
                    screenFiles = files.ToArray();
                }
            }
            return message;
        }

        private string GetStatus(NetworkPerformanceData perfData, out bool online)
        {
            string status = string.Empty;
            if (perfData.BytesSent == 0 && perfData.BytesReceived == 0)
            {
                status = "掉线了";
                online = false;
            }
            else if (perfData.BytesSent < 10 && perfData.BytesReceived < 30)
            {
                status = "安全区";
                online = true;
            }
            else
            {
                status = "战斗中";
                online = true;
            }
            status += " ";
            return status;
        }
    }
}
