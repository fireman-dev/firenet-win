using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace FireNet.UI
{
    public partial class App : Application
    {
        public App()
        {
            try
            {
                Directory.CreateDirectory("logs");
            }
            catch { }

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                try
                {
                    File.AppendAllText("logs/crash.log",
                        $"[UnhandledException]\n{DateTime.Now}\n{e.ExceptionObject}\n\n");
                }
                catch { }
            };

            DispatcherUnhandledException += (s, e) =>
            {
                try
                {
                    File.AppendAllText("logs/ui-crash.log",
                        $"[UI Exception]\n{DateTime.Now}\n{e.Exception}\n\n");
                }
                catch { }

                e.Handled = true;
            };

            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                try
                {
                    File.AppendAllText("logs/task-crash.log",
                        $"[Task Exception]\n{DateTime.Now}\n{e.Exception}\n\n");
                }
                catch { }
            };
        }
    }
}
