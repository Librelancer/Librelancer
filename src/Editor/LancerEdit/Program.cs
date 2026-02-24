// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using LibreLancer;
using LibreLancer.ContentEdit;
using LibreLancer.Dialogs;
using WattleScript.Interpreter;

namespace LancerEdit
{
	class Program
    {
        private const string PipeGuid = "62e369d8adb04159908c125e22e12b94";

        [STAThread]
        [SuppressMessage("ReSharper", "MethodSupportsCancellation")]
        static void Main(string[] args)
        {
            if (!OpenOnOther(args))
            {
                MainWindow mw = null;
                Task pipeServer = null;
                CancellationTokenSource cts = new CancellationTokenSource();
                AppHandler.Run(() =>
                {
                    var editorConfig = EditorConfiguration.Load(true);
                    mw = new MainWindow(editorConfig) { InitOpenFile = args };
                    pipeServer = Task.Run(async () => await PipeServer(cts.Token, x =>
                    {
                        mw.QueueUIThread(() =>
                        {
                            mw.OpenFile(x);
                            mw.BringToFront();
                        });
                    }));
                    mw.Run();
                    mw.Config.Save();
                    cts.Cancel();
                    pipeServer.Wait();
                }, () =>
                {
                    cts.Cancel();
                    if (pipeServer != null)
                        pipeServer.Wait();
                    mw.Crashed();
                });
            }
        }

        static async Task PipeServer(CancellationToken token, Action<string> openFile)
        {
            var myPid = Process.GetCurrentProcess().Id;
            // Create pipe and start the async connection wait
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await using var pipeServer = new NamedPipeServerStream(
                        $"{PipeGuid}-{myPid}", PipeDirection.In, 1, PipeTransmissionMode.Byte,
                        PipeOptions.CurrentUserOnly | PipeOptions.WriteThrough | PipeOptions.Asynchronous);
                    await pipeServer.WaitForConnectionAsync(token);
                    using (var reader = new BinaryReader(pipeServer))
                    {
                        var count = reader.ReadInt32();
                        for (int i = 0; i < count; i++) {
                            openFile(reader.ReadString());
                        }
                    }
                }
                catch
                {
                    // ignored
                }
            }
        }

        static bool OpenOnOther(string[] args)
        {
            if (args.Length < 1)
            {
                return false;
            }
            var myProcess = Process.GetCurrentProcess();
            foreach (var p in Process.GetProcessesByName(myProcess.ProcessName))
            {
                if (p.Id == myProcess.Id)
                    continue;
                try
                {
                    using var client = new NamedPipeClientStream(".", $"{PipeGuid}-{p.Id}", PipeDirection.Out);
                    client.Connect(TimeSpan.FromSeconds(1));
                    using var writer = new BinaryWriter(client);
                    writer.Write(args.Length);
                    foreach (var a in args)
                        writer.Write(a);
                    Console.WriteLine($"Opened in process pid={p.Id}");
                    return true;
                }
                catch
                {
                    // ignored
                }
            }
            return false;
        }
	}
}
