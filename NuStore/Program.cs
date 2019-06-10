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

            var parserResult = Parser.Default.ParseArguments<RestoreOptions, MinifyOptions>(args);

            parserResult.MapResult<RestoreOptions, MinifyOptions, object>(
                opts => Restore(opts),
                opts => Minify(opts),
                errs => "");

            //parserResult.WithParsed(opts =>
            //{
            //    Restore(opts);
            //}).WithNotParsed(errs =>
            //{

            //});

#if DEBUG
            Console.ReadKey();
#endif
        }

        private static object Restore(RestoreOptions opts)
        {
            try
            {
                new RestoreCommand(opts).Execute().Wait();
            }
            catch (Exception ex)
            {
                MessageHelper.Error(ex.ToString());
                MessageHelper.Warning("Use \"nustore restore --help\" for help info...");
            }

            return null;
        }

        private static object Minify(MinifyOptions opts)
        {
            try
            {
                new MinifyCommand(opts).Execute().Wait();
            }
            catch (Exception ex)
            {
                MessageHelper.Error(ex.ToString());
                MessageHelper.Warning("Use \"nustore minify --help\" for help info...");
            }

            return null;
        }

        private static void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            e.SetObserved();
            MessageHelper.Error((e.Exception.InnerException).GetMessage());
        }
    }
}
