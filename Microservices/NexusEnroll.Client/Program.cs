using System;

namespace NexusEnroll
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            var app = new System.Windows.Application();
            app.Run(new MainWindow());
        }
    }
}
