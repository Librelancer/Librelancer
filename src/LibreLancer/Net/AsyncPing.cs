// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
    
using System;
using System.Globalization;
using System.Net;
using System.Net.NetworkInformation;
using System.Diagnostics;

namespace LibreLancer
{
    //Mono's Ping class is stupidly broken on Linux
    public class AsyncPing
    {
        public static void Run(IPAddress addr, Action<int> RTT)
        {
            if (Platform.RunningOS == OS.Linux) RunLinux(addr, RTT);
            else
            {
                var ping = new Ping();
                ping.PingCompleted += (sender,e) => {
                    if (e.Reply.Status == IPStatus.Success) RTT((int)e.Reply.RoundtripTime);
                };
                string data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
                byte[] buffer = System.Text.Encoding.ASCII.GetBytes(data);
                int timeout = 2000;
                var options = new PingOptions(64, true);
                ping.SendAsync(addr, timeout, buffer, options, null);
            }
        }
        static void RunLinux(IPAddress addr, Action<int> RTT)
        {
            AsyncManager.RunTask(() =>
                {
                    try
                    {
                        var p = new ProcessStartInfo("bash", "-c 'LANG=C ping -c 2 -i 0.8 -w 2 \"" + addr + "\"'");
                        p.UseShellExecute = false;
                        p.RedirectStandardOutput = true;
                        var proc = Process.Start(p);
                        string s = "";
                        proc.OutputDataReceived += (sender, e) =>
                        {
                            s += "\n" + e.Data;
                        };
                        proc.BeginOutputReadLine();
                        proc.WaitForExit();
                        if (proc.ExitCode != 0) return;
                        //Parse result (e.g. rtt min/avg/max/mdev = 27.766/28.624/29.482/0.858 ms)
                        var split = s.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        var f = split[split.Length - 1];
                        var avg = (int)double.Parse(f.Split('/')[4], CultureInfo.InvariantCulture);
                        RTT(avg);
                    }
                    catch { }
                });
        }
    }
}
