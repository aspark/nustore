using ILRepacking;
using Newtonsoft.Json;
using NuStore.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuStore
{
    internal class MinifyCommand : DepsCommandBase
    {
        private MinifyOptions _options = null;

        public MinifyCommand(MinifyOptions options)
        {
            _options = options;
        }

        public override Task Execute()
        {
            var (file, deps) = GetDeps();

            var pkgs = deps.Libraries.Where(l => string.Equals(l.Value.Type, "project", StringComparison.InvariantCultureIgnoreCase)).Select(l => l.Key).ToArray();
            var dlls = pkgs.Select(p => $"{ParsePackageName(p).name}.dll").ToArray();
            if (dlls.Length > 0)
            {
                var currentDir = Directory.GetCurrentDirectory();
                foreach(var dll in dlls)
                {
                    if (!File.Exists(Path.Combine(currentDir, dll)))
                        throw new Exception($"Can not find dll in current dir:{dll}");
                }

                MessageHelper.Info($"minify dlls:{string.Join(";", dlls)}");

                var outDir = Path.Combine(currentDir, string.IsNullOrWhiteSpace(_options.Directory) ? "nustored" : _options.Directory);
                var outFile = Path.Combine(outDir, dlls.First());

                var options = new RepackOptions()
                {
                    DelaySign = _options.DelaySign,
                    SearchDirectories = _options.SearchDirectories?? new string[0],
                    LogVerbose = _options.Verbosity,
                    OutputFile = outFile,
                    InputAssemblies = dlls
                };
                if (!string.IsNullOrWhiteSpace(_options.Kind))
                {
                    options.TargetKind = Enum.Parse<ILRepack.Kind>(_options.Kind);
                }

                var pack = new ILRepack(options, new PackLogger() { ShouldLogVerbose = _options.Verbosity });

                pack.Repack();

                //generate deps.json
                var hashPkgs = new HashSet<string>(pkgs.Skip(1));// hold the main dll
                RemoveMergedDlls(deps.Targets, hashPkgs);
                RemoveMergedDlls(deps.Libraries, hashPkgs);
                File.WriteAllText(Path.Combine(outDir, Path.GetFileName(file)), JsonConvert.SerializeObject(deps,new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore }), Encoding.UTF8);

                foreach (var filter in (_options.CopyFilters == null || _options.CopyFilters.Count() == 0 ? new[] { "appsettings.json" } : _options.CopyFilters))
                {
                    CopyFile(currentDir, filter, outDir);
                }

                //copy runtimeconfig
                CopyFile(currentDir, "*.runtimeconfig.*", outDir);

                MessageHelper.Successs($"minify completed. output:{outDir}");
            }

            return Task.CompletedTask;
        }

        private void RemoveMergedDlls<TV>(IDictionary<string, TV> dic, HashSet<string> removeKeys, HashSet<string> skipKeys= null)
        {
            foreach (var key in dic.Keys.ToArray())
            {
                if (removeKeys.Contains(key) && (skipKeys == null || !skipKeys.Contains(key)))
                    dic.Remove(key);
            }
        }

        private void CopyFile(string currentDir, string filter, string outDir)
        {
            foreach (var runtimeFile in Directory.GetFiles(currentDir, filter))
            {
                File.Copy(runtimeFile, Path.Combine(outDir, Path.GetFileName(runtimeFile)), true);
            }
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
