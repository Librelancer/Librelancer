using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace XwtPlus.TextEditor
{
    public class CaretAnimation : IDisposable
    {
        const int AnimationInterval = 500;
        Timer timer;

        public bool CaretState { get; set; }
        public Action Callback { get; set; }

        public CaretAnimation()
        {
            timer = new Timer(AnimationInterval);
            timer.Elapsed += (sender, args) =>
            {
                CaretState = !CaretState;
                var action = Callback;
                if (action != null)
                {
                    Xwt.Application.Invoke(action);
                }
            };
        }

        public void Start()
        {
            lock (timer)
            {
                timer.Start();
            }
        }

        public void Restart()
        {
            lock (timer)
            {
                timer.Stop();
                CaretState = true;
                timer.Start();
            }
        }

        public void Dispose()
        {
            lock (timer)
            {
                timer.Stop();
            }
        }
    }
}
