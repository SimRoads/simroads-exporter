using Eto;
using System;

namespace TsMap.Canvas
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            string platform = "";

#if LINUX
            platform = Platforms.Gtk;
#elif WINDOWS
            platform = Platforms.WinForms;
#endif

            if (platform.Equals("")) new Eto.Forms.Application().Run(new SetupForm());
            else new Eto.Forms.Application(platform).Run(new SetupForm());
        }
    }
}
