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
            var currentDir = Directory.GetCurrentDirectory();

            string[] dlls = null;
            if (_options.MergeAll)
                dlls = GetDllsFromDir(file, currentDir);
            else
                dlls = GetDllsFromDeps(deps);

            if(dlls != null)
            {
                if (!string.IsNullOrWhiteSpace(_options.Exclude))
                {
                    //exclude special dlls
                    dlls = dlls.Where(d => !IsMatch(_options.Exclude, d)).ToArray();//todo:will keep the order?
                }

                if (dlls.Length > 0)
                {
                    foreach (var dll in dlls)
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
                        DebugInfo = _options.DebugInfo,
                        SearchDirectories = _options.SearchDirectories ?? new string[0],
                        LogVerbose = _options.Verbosity,
                        OutputFile = outFile,
                        InputAssemblies = dlls
                    };
                    if (!string.IsNullOrWhiteSpace(_options.Kind))
                    {
                        options.TargetKind = Enum.Parse<ILRepack.Kind>(_options.Kind, true);
                    }

                    var pack = new ILRepack(options, new PackLogger() { ShouldLogVerbose = _options.Verbosity });

                    pack.Repack();

                    //generate deps.json
                    var hashDlls = new HashSet<string>(dlls.Skip(1).Select(d => Path.GetFileNameWithoutExtension(d)));// hold the main dll
                    if(hashDlls.Any())
                    {
                        if(deps.Targets != null)
                        {
                            foreach (var target in deps.Targets)
                            {
                                RemoveMergedDlls(target.Value, hashDlls);
                            }
                        }

                        RemoveMergedDlls(deps.Libraries, hashDlls);
                    }

                    File.WriteAllText(Path.Combine(outDir, Path.GetFileName(file)), JsonConvert.SerializeObject(deps, new JsonSerializerSettings()
                    {
                        NullValueHandling = NullValueHandling.Ignore,
                        Formatting = Formatting.Indented
                    }), Encoding.UTF8);

                    foreach (var filter in (_options.CopyFilters == null || _options.CopyFilters.Count() == 0 ? new[] { "appsettings.json" } : _options.CopyFilters))
                    {
                        CopyFile(currentDir, filter, outDir);
                    }

                    //copy runtimeconfig
                    CopyFile(currentDir, "*.runtimeconfig.*", outDir);

                    MessageHelper.Successs($"minify completed. output:{outDir}");
                }
                else
                {
                    MessageHelper.Error("can not find/match dlls");
                }
            }

            return Task.CompletedTask;
        }

        public string[] GetDllsFromDeps(ProjectDeps deps)
        {
            var pkgs = deps.Libraries.Where(l => string.Equals(l.Value.Type, "project", StringComparison.InvariantCultureIgnoreCase)).Select(l => l.Key).ToArray();

            if (pkgs.Length == 0)
            {
                MessageHelper.Error("Can not find any project dll");
                return null;
            }

            var dlls = pkgs.Select(p => $"{ParsePackageName(p).name}.dll").ToArray();

            return dlls;
        }

        public string[] GetDllsFromDir(string depFileName, string currentDir)
        {
            var mainDll = Path.GetFileName(depFileName).Replace(".deps.json", ".dll", StringComparison.InvariantCultureIgnoreCase);

            //MessageHelper.Info($"use maindll:{mainDll}");//debug

            var dlls = Directory.GetFiles(currentDir, "*.dll").Select(f => Path.GetFileName(f));

            return dlls.OrderBy(d => string.Equals(d, mainDll, StringComparison.InvariantCultureIgnoreCase) ? 0 : 1).ToArray();
        }

        private void RemoveMergedDlls<TV>(IDictionary<string, TV> dic, HashSet<string> removeKeys, HashSet<string> skipKeys= null)
        {
            if (dic == null || removeKeys == null || removeKeys.Count == 0)
                return;

            foreach (var key in dic.Keys.ToArray())
            {
                var (name, _) = ParsePackageName(key);

                //MessageHelper.Info($"ParsePackageName:{name}, removeKeys:{string.Join(";", removeKeys)}, Contains:{removeKeys.Contains(name)}");//debug

                if (removeKeys.Contains(name) && (skipKeys == null || !skipKeys.Contains(name)))
                {
                    dic.Remove(key);
                    //MessageHelper.Info($"remove deps:{key}");//debug
                }
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
