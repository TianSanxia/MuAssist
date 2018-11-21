using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace MuMonitor
{
    internal class MuNetworkMonitor
    {
        private string ProcessName = "main";
        private int TraceDurationInMins = 1;

        public MuNetworkMonitor()
        {
            ProcessName = ConfigurationManager.AppSettings["ProcessName"];
            TraceDurationInMins = 1;
        }

        public string Analyze(out bool allDisconnected)
        {
            string message = string.Empty;

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
                        message += "MU_" + index + ":";
                        message += GetStatus(perf.Value, out online);
                        if(online)
                        {
                            allDisconnected = false;
                        }
                        Console.WriteLine("{2}, Send:{0} B/sec, Recv:{1} B/sec", perf.Value.BytesSent, perf.Value.BytesReceived, perf.Key);
                        index++;
                    }
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
            else if (perfData.BytesSent <= 10)
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
