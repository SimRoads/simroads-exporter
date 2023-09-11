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
            new Eto.Forms.Application().Run(new SetupForm());
        }
    }
}
