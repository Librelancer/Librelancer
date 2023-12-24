using CoreAudio;
using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace LibreLancer.Media
{
    internal class NotificationWindows : IDisposable
    {
        MMDeviceEnumerator devEnumerator;
        MMNotificationClient notificationClient;

        public NotificationWindows()
        {
            devEnumerator = new MMDeviceEnumerator(Guid.NewGuid());
            notificationClient = new MMNotificationClient(devEnumerator);
            notificationClient.DefaultDeviceChanged += NotificationClient_DefaultDeviceChanged;
        }

        private void NotificationClient_DefaultDeviceChanged(object sender, DefaultDeviceChangedEventArgs e)
        {
            Interlocked.Increment(ref DeviceEvents.DefaultDeviceChange);
        }

        public void Dispose()
        {
            notificationClient.DefaultDeviceChanged -= NotificationClient_DefaultDeviceChanged;
        }
    }
    static class DeviceEvents
    {
        public static int DefaultDeviceChange = 0;
        static NotificationWindows notifications;

        public static void Init()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                notifications = new NotificationWindows();
            }
        }

        public static void Deinit()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                notifications?.Dispose();
                notifications = null;
            }
        }
    }
}
