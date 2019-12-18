using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;

namespace IVsTestingExtension.ToolWindows
{
    [Guid(WindowGuidString)]
    public class CommandInvokingWindow : ToolWindowPane
    {
        public const string WindowGuidString = "5D42A8E4-F0AE-44BD-B0A1-193C8901364C";
        public const string Title = "Command Invoking Window";

        public CommandInvokingWindow(ProjectCommandTestingModel state) : base()
        {
            Caption = Title;
            BitmapImageMoniker = KnownMonikers.ImageIcon;
            Content = new ToolWindowControl(state);
        }
    }
}
