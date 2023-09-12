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
            var platform = Platforms.WinForms;

#if LINUX
            platform = Platforms.Gtk;
#endif

            new Eto.Forms.Application(platform).Run(new SetupForm());
        }
    }
}
