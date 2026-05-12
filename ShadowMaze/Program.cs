using System;
using System.Windows.Forms;
using ShadowMaze.View;
using ShadowMaze.Model;

namespace ShadowMaze
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}