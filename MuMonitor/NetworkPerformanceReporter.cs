using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;

namespace MuMonitor
{
    public sealed class NetworkPerformanceReporter : IDisposable
    {
        private const string SessionName = "MU_89FF9737-A15A-4E0A-8C8C-577CD1CC6E33";
        private DateTime m_EtwStartTime;
        private TraceEventSession m_EtwSession;
        private int[] processIds;

        private object counterLock = new object();
        private  Dictionary< int, Counters> m_CountersDic = new Dictionary<int, Counters>();

        private class Counters
        {
            public long Received;
            public long Sent;
        }

        public NetworkPerformanceReporter(int[] procIds)
        {
            processIds = procIds;
            foreach(var pid in processIds)
            {
                m_CountersDic.Add(pid, new Counters());
            }
        }

        public static NetworkPerformanceReporter Create(int[] procIds)
        {
            var networkPerformancePresenter = new NetworkPerformanceReporter(procIds);
            networkPerformancePresenter.Initialize();
            return networkPerformancePresenter;
        }

        public void Initialize()
        {
            // Note that the ETW class blocks processing messages, so should be run on a different thread if you want the application to remain responsive.
            Task.Run(() => StartEtwSession());
        }

        private void StartEtwSession()
        {
            try
            {
                ResetCounters();

                using (m_EtwSession = new TraceEventSession(SessionName))
                {
                    if (m_EtwSession.EnableKernelProvider(KernelTraceEventParser.Keywords.NetworkTCPIP))
                    {

                        m_EtwSession.Source.Kernel.TcpIpRecv += data =>
                        {
                            if (m_CountersDic.ContainsKey(data.ProcessID))
                            {
                                lock (counterLock)
                                {
                                    m_CountersDic[data.ProcessID].Received += data.size;
                                }
                            }
                        };

                        m_EtwSession.Source.Kernel.TcpIpSend += data =>
                        {
                            if (m_CountersDic.ContainsKey(data.ProcessID))
                            {
                                lock (counterLock)
                                {
                                    m_CountersDic[data.ProcessID].Sent += data.size;
                                }
                            }
                        };

                        m_EtwSession.Source.Process();
                    }
                    else
                    {
                        throw new Exception("EnableKernelProvider failed.");
                    }
                }
            }
            catch(Exception ex)
            {         
                ResetCounters(); // Stop reporting figures
                // Probably should log the exception
                throw;
            }
        }

        public IDictionary<int, NetworkPerformanceData> GetNetworkPerformanceData()
        {
            Dictionary<int, NetworkPerformanceData> perfDataDic = new Dictionary<int, NetworkPerformanceData>();

            var timeDifferenceInSeconds = (DateTime.Now - m_EtwStartTime).TotalSeconds;

            lock (counterLock)
            {
                foreach (var counters in m_CountersDic)
                {
                    NetworkPerformanceData networkData = new NetworkPerformanceData
                    {
                        BytesReceived = Convert.ToInt64(counters.Value.Received / timeDifferenceInSeconds),
                        BytesSent = Convert.ToInt64(counters.Value.Sent / timeDifferenceInSeconds)
                    };
                    perfDataDic.Add(counters.Key, networkData);
                }

            }

            // Reset the counters to get a fresh reading for next time this is called.
            ResetCounters();

            return perfDataDic;
        }

        private void ResetCounters()
        {
            lock (counterLock)
            {
                foreach (var counters in m_CountersDic)
                {
                    counters.Value.Sent = 0;
                    counters.Value.Received = 0;
                }
            }
            m_EtwStartTime = DateTime.Now;
        }

        public void Stop()
        {
            m_EtwSession?.Stop();
        }

        public void Dispose()
        {
            m_EtwSession?.Dispose();
        }
    }

    public sealed class NetworkPerformanceData
    {
        public long BytesReceived { get; set; }
        public long BytesSent { get; set; }
    }
}