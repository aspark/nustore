using ILRepacking;
using NuStore.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NuStore
{
    internal class MinifyCommand : ICommand
    {
        private MinifyOptions _options = null;

        public MinifyCommand(MinifyOptions options)
        {
            _options = options;
        }

        public Task Execute()
        {
            var pack = new ILRepack(new RepackOptions() {
                DelaySign = _options.DelaySign,
                TargetKind = _options.Kind,
                SearchDirectories = _options.SearchDirectories,
                LogVerbose = _options.Verbosity,
                OutputFile = _options.Directory,
                InputAssemblies = new string[0]//todo
            }, new PackLogger() { ShouldLogVerbose = _options.Verbosity });

            pack.Repack();

            return Task.CompletedTask;
        }

        //simple loger for ilrepack
        private class PackLogger : ILogger
        {
            public bool ShouldLogVerbose { get; set; }

            public void DuplicateIgnored(string ignoredType, object ignoredObject)
            {
                MessageHelper.Warning($"ignore type:{ignoredType}");
            }

            public void Error(string msg)
            {
                MessageHelper.Error(msg);
            }

            public void Info(string msg)
            {
                MessageHelper.Info(msg);
            }

            public void Log(object str)
            {
                MessageHelper.Info(str?.ToString());
            }

            public void Verbose(string msg)
            {
                if(ShouldLogVerbose)
                    MessageHelper.Info(msg);
            }

            public void Warn(string msg)
            {
                MessageHelper.Warning(msg);
            }
        }
    }
}
