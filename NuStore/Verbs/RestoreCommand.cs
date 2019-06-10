using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NuStore.Common;

namespace NuStore
{
    internal class RestoreCommand: DepsCommandBase
    {
        static RestoreCommand()
        {
            ServicePointManager.DefaultConnectionLimit = 100;
        }

        private RestoreOptions _options = null;
        private HttpClient _http = null;
        //HttpClientFactory
        public RestoreCommand(RestoreOptions options)
        {
            _options = options;

            EnsureDepsFileName(_options.DepsFile);

            _http = new HttpClient() { Timeout = TimeSpan.FromSeconds(30) };
        }

        private string GetStoreDirectory()
        {
            if (!string.IsNullOrWhiteSpace(_options?.Directory))
            {
                return _options.Directory;
            }

            var os = Environment.OSVersion;
            if (os.Platform == PlatformID.Win32NT)
            {
                return Path.Combine(Environment.GetEnvironmentVariable("ProgramW6432") ?? "C:/Program Files", "dotnet/store");
            }
            else if (os.Platform == PlatformID.Unix || os.Platform == PlatformID.MacOSX)
            {
                string dotnet = null;
                //dotnet = Environment.GetEnvironmentVariable("dotnet");
                //if (!string.IsNullOrWhiteSpace(dotnet))
                //{
                //    dotnet = Path.GetDirectoryName(dotnet);//todo: readlink
                //}

                return Path.Combine(dotnet ?? "/usr/local/share/dotnet", "store");
            }
            else
            {
                throw new NotSupportedException("not support os version");
            }
        }

        private string GetPackageDirectory(ProjectDeps deps)
        {
            (string arch, string runtime) = ParseRuntimeInfo(deps);

            return Path.Combine(GetStoreDirectory(), $"{arch}/{runtime}");
        }

        ////https://www.nuget.org/api/v2/package/NuGet.Core/2.14.0
        //private string _url = "https://www.nuget.org/api/v2/package/{0}/{1}";

        private string _packageContentHost = null;
        private string GetPackageContentUrl(string id, string version)
        {
            if (string.IsNullOrWhiteSpace(_packageContentHost))
            {
                lock (this)
                {
                    if (string.IsNullOrWhiteSpace(_packageContentHost))
                    {
                        var content = _http.GetStringAsync(_options.NugetServiceIndex ?? "https://api.nuget.org/v3/index.json").GetAwaiter().GetResult();
                        var index = JsonConvert.DeserializeObject<NuGetServiceIndexApiResult>(content);
                        var host = index.Resources.FirstOrDefault(r => string.Equals(r._Type, "PackageBaseAddress/3.0.0", StringComparison.InvariantCultureIgnoreCase))?._ID;
                        if (string.IsNullOrWhiteSpace(host))
                        {
                            throw new Exception("get nuget package base addr failed");
                        }

                        _packageContentHost = host.TrimEnd('/');
                    }
                }
            }

            //{@id}/{LOWER_ID}/{LOWER_VERSION}/{LOWER_ID}.{LOWER_VERSION}.nupkg
            return string.Format("{0}/{1}/{2}/{1}.{2}.nupkg", _packageContentHost, id.ToLower(), version.ToLower());
        }

        private async Task<bool> DownloadPackage(string name, string version, string libFolder)
        {
            if (Directory.Exists(libFolder) && !_options.Flatten && !_options.ForceOverride)//if flatten, the libFolder is parent dir,so continue download
            {
                MessageHelper.Warning($"Skip override:{libFolder}");
                return false;
            }

            var url = GetPackageContentUrl(name, version);

            try
            {
                var res = await _http.SendAsync(new HttpRequestMessage(HttpMethod.Get, url));
                var pkg = await res.Content.ReadAsStreamAsync();
                //Zip
                using (var zip = new ZipArchive(pkg, ZipArchiveMode.Read, true))
                {
                    var hasLibs = false;
                    var hasSave = false;
                    foreach (var entry in zip.Entries)
                    {
                        hasLibs = true;
                        var tmpName = entry.FullName.ToLower();
                        if (tmpName.StartsWith("lib/") || tmpName.StartsWith("runtimes/"))
                        {
                            if (entry.Length == 0)
                            {
                                continue;
                            }

                            var fileName = Path.Combine(libFolder, entry.Name);
                            if (File.Exists(fileName) && !_options.ForceOverride)
                            {
                                MessageHelper.Warning($"Skip override:{entry.FullName}");
                                continue;
                            }

                            hasSave = true;
                            Directory.CreateDirectory(Path.GetDirectoryName(fileName));
                            entry.ExtractToFile(fileName, true);//_options.ForceOverride
                        }
                    }

                    if(hasSave)
                    {
                        MessageHelper.Successs($"Save {name}[{version}] to {libFolder}");

                        return true;
                    }
                    else if(!hasLibs)
                    {
                        MessageHelper.Error($"Can't find lib entry in {Path.GetFileName(url)}");
                    }
                }
            }
            catch(Exception ex)
            {
                MessageHelper.Error("Restore failed:"+ ex.Message);
            }

            return false;
        }

        private bool NeedDownload(string package)
        {
            if (!string.IsNullOrWhiteSpace(_options.Special))
            {
                if(IsMatch(_options.Special, package))
                      return true;

                return false;
            }

            return IsMatch(_options.Exclude, package) == false;
        }

        Regex _regRuntime = new Regex(@"\.?(?<name>\w+)\W*Version=v(?<ver>[\d\.]*)", RegexOptions.IgnoreCase);
        private (string arch, string runtime) ParseRuntimeInfo(ProjectDeps deps)
        {
            var arch = _options.Architecture;
            if(string.IsNullOrWhiteSpace(arch))
            {
                switch((deps?.CompilationOptions?.Platform??"").ToLower())
                {
                    //case "anycpu":
                    //    arch = Environment.Is64BitOperatingSystem ? "x64" : "x86";
                    //    break;
                    case "anycpu32bitpreferred":
                        arch = "x86";
                        break;
                    case "x86":
                        arch = "x86";
                        break;
                    case "x64":
                        arch = "x64";
                        break;
                    //case "arm":
                    //    arch = "x64";
                    //    break;
                    //case "itanium":
                    //    arch = "x64";
                    //    break;
                    //default:
                    //    throw new NotSupportedException("");
                }
            }

            if (string.IsNullOrWhiteSpace(arch))
                arch = Environment.Is64BitOperatingSystem ? "x64" : "x86";


            var runtime = _options.Runtime;
            if(string.IsNullOrWhiteSpace(runtime))
            {
                var name = deps?.RuntimeTarget?.Name;
                if (!string.IsNullOrWhiteSpace(name))
                {
                    var m = _regRuntime.Match(name);
                    if (m.Success)
                    {
                        runtime = (m.Groups["name"].Value + m.Groups["ver"].Value).ToLower();
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(runtime))
                runtime = "netcoreapp2.0";


            return (arch, runtime);
        }

        public override async Task Execute()
        {
            var (_, deps) = GetDeps(_options.DepsFile);
            
            (string arch, string runtime) = ParseRuntimeInfo(deps);

            var storeDirectory = GetStoreDirectory();
            MessageHelper.Info($"begin restore packages/dlls to {storeDirectory}");

            if (!_options.Yes && !_options.ForceOverride)
            {
                MessageHelper.WarningInline($"continue restore?(y/n):");//{Environment.NewLine}
                if (Console.ReadKey().KeyChar != 'y' && Console.ReadKey().KeyChar != 'Y')
                {
                    Console.Write(Environment.NewLine);
                    MessageHelper.Error($"exit restore...");
                    return;
                }
            }

            var pkgFolder = GetPackageDirectory(deps);//append arch/runtime

            var count = 0;
            foreach (var item in deps.Libraries.AsParallel())
            {
                if (!item.Value.Serviceable
                    || !string.Equals(item.Value.Type, "package", StringComparison.InvariantCultureIgnoreCase)
                    ||!NeedDownload(item.Key))
                {
                    if (_options.Verbosity)
                        MessageHelper.Warning($"Not serviceable, Skip restore:{item.Key}");
                    continue;
                }

                (string name, string version) = ParsePackageName(item.Key);

                if(_options.Verbosity)
                    MessageHelper.Info($"Begin restore package:{name}[{version}]");

                if (await DownloadPackage(name, version, _options.Flatten ? storeDirectory : Path.Combine(pkgFolder, item.Value.Path)))
                {
                    count++;
                }
            }

            MessageHelper.Successs($"Complete Restore {count} packages.");
        }
    }
}
