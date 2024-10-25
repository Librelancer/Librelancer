using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace LibreLancer
{
    /// <summary>
    /// Slimmed down version of the BepuPhysics ThreadDispatcher. Not re-entrant
    /// </summary>
    public class ParallelActionRunner : IDisposable
    {
        int threadCount;
        /// <summary>
        /// Gets the number of threads to dispatch work on.
        /// </summary>
        public int ThreadCount => threadCount;
        struct Worker
        {
            public Thread Thread;
            public AutoResetEvent Signal;
        }

        Worker[] workers;
        AutoResetEvent finished;

        /// <summary>
        /// Creates a new thread dispatcher with the given number of threads.
        /// </summary>
        /// <param name="threadCount">Number of threads to dispatch on each invocation.</param>
        public ParallelActionRunner(int threadCount)
        {
            if (threadCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(threadCount), "Thread count must be positive.");
            this.threadCount = threadCount;
            workers = new Worker[threadCount - 1];
            for (int i = 0; i < workers.Length; ++i)
            {
                workers[i] = new Worker { Thread = new Thread(WorkerLoop), Signal = new AutoResetEvent(false) };
                workers[i].Thread.IsBackground = true;
                workers[i].Thread.Start((workers[i].Signal, i + 1));
            }
            finished = new AutoResetEvent(false);
        }

        void DispatchThread(int workerIndex)
        {
            managedWorker(workerIndex);
            if (Interlocked.Decrement(ref remainingWorkerCounter.Value) == -1)
            {
                finished.Set();
            }
        }

        volatile Action<int> managedWorker;

        //We'd like to avoid the thread readonly values above being adjacent to the thread readwrite counter.
        //If they were in the same cache line, it would cause a bit of extra contention for no reason.
        //(It's not *that* big of a deal since the counter is only touched once per worker, but padding this also costs nothing.)
        //In a class, we don't control layout, so wrap the counter in a beefy struct.
        //128B padding is used for the sake of architectures that might try prefetching cache line pairs and running into sync problems.
        [StructLayout(LayoutKind.Explicit, Size = 256)]
        struct Counter
        {
            [FieldOffset(128)]
            public int Value;
        }

        Counter remainingWorkerCounter;

        void WorkerLoop(object untypedSignal)
        {
            var (signal, workerIndex) = ((AutoResetEvent, int))untypedSignal;
            while (true)
            {
                signal.WaitOne();
                if (disposed)
                    return;
                DispatchThread(workerIndex);
            }
        }

        void SignalThreads(int maximumWorkerCount)
        {
            //Worker 0 is not signalled; it's the executing thread.
            //So if we want 4 total executing threads, we should signal 3 workers.
            int maximumWorkersToSignal = maximumWorkerCount - 1;
            var workersToSignal = maximumWorkersToSignal < workers.Length ? maximumWorkersToSignal : workers.Length;
            remainingWorkerCounter.Value = workersToSignal;
            for (int i = 0; i < workersToSignal; ++i)
            {
                workers[i].Signal.Set();
            }
        }

        int currentTask = 0;
        private int taskCount;

        private Action<int> taskBody;

        void IndexWorker(int workerIndex)
        {
            int jobIndex;
            while ((jobIndex = Interlocked.Increment(ref currentTask)) < taskCount)
            {
                taskBody(jobIndex);
            }
        }

        private IList<Action> tasks;

        void ActionWorker(int workerIndex)
        {
            int jobIndex;
            while ((jobIndex = Interlocked.Increment(ref currentTask)) < taskCount)
            {
                tasks[jobIndex]();
            }
        }

        /// <summary>
        /// Runs a list of actions across multiple workers
        /// </summary>
        /// <param name="actions">The actions to run</param>
        /// <param name="maximumWorkerCount">Max amount of workers from this instance</param>
        /// <exception cref="InvalidOperationException">An operation is already running</exception>
        public void RunActions(IList<Action> actions, int maximumWorkerCount = int.MaxValue)
        {
            if (this.tasks != null || this.taskBody != null)
                throw new InvalidOperationException();
            if (taskCount <= 0)
                return;
            this.currentTask = -1;
            this.tasks = actions;
            if (maximumWorkerCount <= 1) {
                for (int i = 0; i < taskCount; i++)
                {
                    actions[i]();
                }
            }
            else
            {
                this.managedWorker = ActionWorker;
                SignalThreads(maximumWorkerCount);
                //Calling thread does work. No reason to spin up another worker and block this one!
                DispatchThread(0);
                finished.WaitOne();
                this.managedWorker = null;
            }
            this.tasks = null;
        }

        public void RunActions(Action<int> taskBody, int taskCount, int maximumWorkerCount = int.MaxValue)
        {
            if (this.tasks != null || this.taskBody != null)
                throw new InvalidOperationException();
            if (taskCount <= 0)
                return;
            this.currentTask = -1;
            this.taskCount = taskCount;
            this.taskBody = taskBody;
            if (maximumWorkerCount <= 1) {
                for (int i = 0; i < taskCount; i++)
                {
                    taskBody(i);
                }
            }
            else
            {
                this.managedWorker = IndexWorker;
                SignalThreads(maximumWorkerCount);
                //Calling thread does work. No reason to spin up another worker and block this one!
                DispatchThread(0);
                finished.WaitOne();
                this.managedWorker = null;
            }
            this.taskBody = null;
        }

        volatile bool disposed;

        /// <summary>
        /// Waits for all pending work to complete and then disposes all workers.
        /// </summary>
        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
                SignalThreads(threadCount);
                foreach (var worker in workers)
                {
                    worker.Thread.Join();
                    worker.Signal.Dispose();
                }
            }
        }

    }
}
