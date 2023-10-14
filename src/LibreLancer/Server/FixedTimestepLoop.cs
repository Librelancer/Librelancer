using System;
using System.Diagnostics;
using System.Threading;

namespace LibreLancer.Server
{
    public class FixedTimestepLoop
    {
        private const int SLEEP_TIME_COUNT = 64;

        private CircularBuffer<TimeSpan> sleepTimes = new CircularBuffer<TimeSpan>(SLEEP_TIME_COUNT);

        //FromSeconds creates an inaccurate timespan
        public TimeSpan TimeStep { get; private set; } = TimeSpan.FromTicks(166667);

        public TimeSpan TotalTime { get; private set; }

        private OnStep onStep;

        public delegate void OnStep(TimeSpan delta, TimeSpan totalTime, uint currentTick);

        public FixedTimestepLoop(OnStep onStep)
        {
            for (int i = 0; i < SLEEP_TIME_COUNT; i++)
                sleepTimes.Enqueue(TimeSpan.FromMilliseconds(1));
            this.onStep = onStep;
        }

        TimeSpan sleepPrecision = TimeSpan.FromMilliseconds(1);

        void UpdateSleepPrecision(TimeSpan sleepTime)
        {
            if (sleepTime > TimeSpan.FromMilliseconds(5))
                sleepTime = TimeSpan.FromMilliseconds(5);
            sleepTimes.Enqueue(sleepTime);
            var precision = TimeSpan.MinValue;
            for (int i = 0; i < sleepTimes.Count; i++)
            {
                if (sleepTimes[i] > precision)
                    precision = sleepTimes[i];
            }
            sleepPrecision = precision;
        }

        private Stopwatch timer;
        private bool running = false;

        private TimeSpan accumulatedTime;
        private TimeSpan lastTime;

        TimeSpan Accumulate()
        {
            var current = timer.Elapsed;
            var diff = (current - lastTime);
            accumulatedTime += diff;
            lastTime = current;
            return diff;
        }


        public void Start()
        {
            running = true;
            timer = Stopwatch.StartNew();
            uint currentTick = 1;
            while (running)
            {
                Accumulate();
                //FNA Sleep Algorithm: Sleep based on worst case thread sleep time,
                //then use SpinWait
                while (accumulatedTime + sleepPrecision < TimeStep)
                {
                    Thread.Sleep(1);
                    UpdateSleepPrecision(Accumulate());
                }
                while (accumulatedTime < TimeStep)
                {
                    Thread.SpinWait(1);
                    Accumulate();
                }

                int stepCount = 0;
                while (accumulatedTime >= TimeStep && stepCount < 2)
                {
                    TotalTime += TimeStep;
                    accumulatedTime -= TimeStep;
                    stepCount++;
                    onStep(TimeStep,TotalTime, currentTick++);
                }
                if (stepCount == 2 && accumulatedTime >= TimeStep)
                {
                    TotalTime += accumulatedTime;
                    onStep(accumulatedTime,TotalTime, currentTick++);
                    accumulatedTime = TimeSpan.Zero;
                }
            }
        }




        public void Stop()
        {
            running = false;
        }
    }
}
