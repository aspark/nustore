using System;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;
using NuStore.Common;

namespace NuStore
{
    class Program
    {
        static void Main(string[] args)
        {
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
            
            if (args.Length == 0)
            {
                args = new[] { "--help" };
            }

            var parserResult = Parser.Default.ParseArguments<RestoreOptions>(args);

            parserResult.WithParsed(opt =>
            {
                try
                {
                    new RestoreCommand(opt).Execute().Wait();
                }
                catch (Exception ex)
                {
                    MessageHelper.Error(ex.GetMessage());
                    MessageHelper.Warning("Use \"nustore --help\" for help info...");
                }
            }).WithNotParsed(errs =>
            {
                
            });

#if DEBUG
            Console.ReadKey();
#endif
        }

        private static void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            e.SetObserved();
            MessageHelper.Error((e.Exception.InnerException).GetMessage());
        }
    }
}
