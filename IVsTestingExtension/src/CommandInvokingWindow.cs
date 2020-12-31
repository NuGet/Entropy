using System;
using System.Runtime.InteropServices;
using IVsTestingExtension.Xaml.ToolWindow;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;

namespace IVsTestingExtension
{
    [Guid(WindowGuidString)]
    public class CommandInvokingWindow : ToolWindowPane
    {
        public const string WindowGuidString = "5D42A8E4-F0AE-44BD-B0A1-193C8901364C";
        public const string Title = "IVs Testing Extension";

        public CommandInvokingWindow(ToolWindowControlViewModel state) : base()
        {
            Caption = Title;
            BitmapImageMoniker = KnownMonikers.ImageIcon;
            Content = new ToolWindowControl(state);
        }
    }
}
