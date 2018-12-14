// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
namespace LibreLancer.Dialogs
{
    public static class CrashWindow
    {
        public static void Run(string title, string message, string details)
        {
            FLLog.Error("Engine", message + "\n" + details);
            RunSwf(title, message, details);
        }

        static void RunSwf(string title, string message, string details)
        {
            dynamic form = SWF.New("System.Windows.Forms.Form");
            form.Text = title;
            form.Width = 600;
            form.Height = 400;

            var label = SWF.New("System.Windows.Forms.Label");
            label.Text = message;
            label.Dock = SWF.Enum("DockStyle", "Top");

            var rtf = SWF.New("System.Windows.Forms.RichTextBox");
            rtf.ReadOnly = true;
            rtf.Text = details;
            rtf.Dock = SWF.Enum("DockStyle", "Fill");
            rtf.DetectUrls = true;

            form.Controls.Add(rtf);
            form.Controls.Add(label);

            SWF.ApplicationRun(form);
        }
    }
}
