/* Copyright (C) <2009-2011> <Thorben Linneweber, Jitter Physics>
* 
*  This software is provided 'as-is', without any express or implied
*  warranty.  In no event will the authors be held liable for any damages
*  arising from the use of this software.
*
*  Permission is granted to anyone to use this software for any purpose,
*  including commercial applications, and to alter it and redistribute it
*  freely, subject to the following restrictions:
*
*  1. The origin of this software must not be misrepresented; you must not
*      claim that you wrote the original software. If you use this software
*      in a product, an acknowledgment in the product documentation would be
*      appreciated but is not required.
*  2. Altered source versions must be plainly marked as such, and must not be
*      misrepresented as being the original software.
*  3. This notice may not be removed or altered from any source distribution. 
*/

#region Using Statements
using System;
using System.Collections.Generic;

using LibreLancer.Jitter.Dynamics;
using LibreLancer.Jitter.LinearMath;
using LibreLancer.Jitter.Collision.Shapes;
using System.Threading;
#if PORTABLE
using System.Threading.Tasks;
using Thread = System.Threading.Tasks.Task;
#endif
#endregion

namespace LibreLancer.Jitter
{

    /// <summary>
    /// Jitters ThreadManager class handles the internal multithreading of the
    /// engine.
    /// </summary>
    public class ThreadManager
    {

        public const int ThreadsPerProcessor = 1;

        private readonly int[] xBoxMap = new int[] { 1, 3, 4, 5 };

        private ManualResetEvent waitHandleA, waitHandleB;
        private ManualResetEvent currentWaitHandle;

        volatile List<Action<object>> tasks = new List<Action<object>>();
        volatile List<object> parameters = new List<object>();

        private Thread[] threads;
        private int currentTaskIndex, waitingThreadCount;

        internal int threadCount;

        /// <summary>
        /// Get the number of threads used by the ThreadManager to execute
        /// tasks.
        /// </summary>
        public int ThreadCount { private set { this.threadCount = value; } get { return threadCount; } }

        static ThreadManager instance = null;

        public static ThreadManager Instance 
        { 
            get 
            {
                if (instance == null)
                {
                    instance = new ThreadManager();
                    instance.Initialize();
                }

                return instance;
            }
        }

        private ThreadManager() { }

        private void Initialize()
        {


            threadCount = System.Environment.ProcessorCount * ThreadsPerProcessor;

            threads = new Thread[threadCount];
            waitHandleA = new ManualResetEvent(false);
            waitHandleB = new ManualResetEvent(false);

            currentWaitHandle = waitHandleA;

            AutoResetEvent initWaitHandle = new AutoResetEvent(false);

            for (int i = 1; i < threads.Length; i++)
            {
                threads[i] = NewThread(() =>
                {
                    initWaitHandle.Set();
                    ThreadProc();
                });

                threads[i].Start();
                initWaitHandle.WaitOne();
            }



        }

        /// <summary>
        /// Executes all tasks previously added to the ThreadManager.
        /// The method finishes when all tasks are complete.
        /// </summary>
        public void Execute()
        {
            currentTaskIndex = 0;
            waitingThreadCount = 0;

            currentWaitHandle.Set();
            PumpTasks();

            while (waitingThreadCount < threads.Length - 1) ThreadSleep(0);

            currentWaitHandle.Reset();
            currentWaitHandle = (currentWaitHandle == waitHandleA) ? waitHandleB : waitHandleA;

            tasks.Clear();
            parameters.Clear();
        }

        /// <summary>
        /// Adds a task to the ThreadManager. The task and the parameter
        /// is added to an internal list. Call <see cref="Execute"/>
        /// to execute and remove the tasks from the internal list.
        /// </summary>
        /// <param name="task"></param>
        /// <param name="param"></param>
        public void AddTask(Action<object> task, object param)
        {
            tasks.Add(task);
            parameters.Add(param);
        }

        private void ThreadProc()
        {
            while (true)
            {
                Interlocked.Increment(ref waitingThreadCount);
                waitHandleA.WaitOne();
                PumpTasks();

                Interlocked.Increment(ref waitingThreadCount);
                waitHandleB.WaitOne();
                PumpTasks();
            }
        }

        private void PumpTasks()
        {
            int count = tasks.Count;

            while (currentTaskIndex < count)
            {
                int taskIndex = currentTaskIndex;

                if (taskIndex == Interlocked.CompareExchange(ref currentTaskIndex, taskIndex + 1, taskIndex)
                    && taskIndex < count)
                {
                    tasks[taskIndex](parameters[taskIndex]);
                }
            }
        }

        private static void ThreadSleep(int dueTime)
        {
#if PORTABLE
            Task.Delay(dueTime).Wait();
#else
            Thread.Sleep(dueTime);
#endif
        }

#if PORTABLE
        private delegate void ThreadStart();
#endif

        private static Thread NewThread(ThreadStart action)
        {
#if PORTABLE
            return new Thread(action.Invoke, TaskCreationOptions.LongRunning);
#else
            return new Thread(action) { IsBackground = true };
#endif
        }

    }

}
