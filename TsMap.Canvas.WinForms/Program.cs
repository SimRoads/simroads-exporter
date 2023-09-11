using Eto.Forms;
using System;

namespace TsMap.Canvas.Winforms
{
    class MainClass
    {
        [STAThread]
        public static void Main(string[] args)
        {
            new Application(Eto.Platforms.WinForms).Run(new SetupForm());
        }
    }
}