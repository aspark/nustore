using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NuStore.Common;

namespace NuStore
{
    internal class RestoreCommand
    {
        private RestoreOptions _options = null;
        private HttpClient _http = null;

        public RestoreCommand(RestoreOptions options)
        {
            _options = options;

            _http = new HttpClient() { Timeout = TimeSpan.FromSeconds(30) };
        }

        private string GetStoreDirctory()
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

        private string GetDepsFileName()
        {
            string fileName = "";
            if (!string.IsNullOrWhiteSpace(_options?.DepsFile))
            {
                fileName = Path.Combine(Directory.GetCurrentDirectory(), _options.DepsFile);
                if (!File.Exists(fileName))
                {
                    throw new FileNotFoundException($"Can't find deps file:{fileName}");
                }

                return fileName;
            }

            fileName = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.deps.json").FirstOrDefault();

            if (string.IsNullOrEmpty(fileName))
            {
                throw new FileNotFoundException("Can't file deps file.");
            }

            return fileName;
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

        private async Task DownloadPackage(string name, string version, string pkgFolder)
        {
            if (Directory.Exists(pkgFolder) && !_options.ForceOverride)
            {
                MessageHelper.Warning($"Skip override:{pkgFolder}");
                return;
            }

            var url = GetPackageContentUrl(name, version);

            try
            {
                var res = await _http.SendAsync(new HttpRequestMessage(HttpMethod.Get, url));
                var pkg = await res.Content.ReadAsStreamAsync();
                //Zip
                using (var zip = new ZipArchive(pkg, ZipArchiveMode.Read, true))
                {
                    var hasFind = false;
                    foreach (var entry in zip.Entries)
                    {
                        if (entry.FullName.StartsWith("lib/"))
                        {
                            hasFind = true;

                            if (entry.Length == 0)
                            {
                                continue;
                            }

                            var fileName = Path.Combine(pkgFolder, entry.FullName);
                            if (File.Exists(fileName) && !_options.ForceOverride)
                            {
                                MessageHelper.Warning($"Skip override:{entry.FullName}");
                                continue;
                            }

                            Directory.CreateDirectory(Path.GetDirectoryName(fileName));

                            entry.ExtractToFile(fileName, true);//_options.ForceOverride
                        }
                    }

                    if (hasFind)
                    {
                        MessageHelper.Info($"Save {name}[{version}] to {pkgFolder}");
                    }
                    else
                    {
                        MessageHelper.Error($"Can't find lib entry in {Path.GetFileName(url)}");
                    }
                }
            }
            catch(Exception ex)
            {
                MessageHelper.Error("Restore failed:"+ ex.Message);
            }
        }

        private (string name, string version) ParsePackageName(string packageName)
        {
            var index = packageName.LastIndexOf('/');

            return (packageName.Substring(0, index), packageName.Substring(index + 1));
        }

        public async Task Execute()
        {
            //x86 x64
            var bit = "x64";// ent.Is64BitOperatingSystem ? "x64" : "x86";
            var fwFolder = Path.Combine(GetStoreDirctory(), $"{bit}/netcoreapp2.0");
            MessageHelper.Warning($"Retore packages to {fwFolder}");

            var file = GetDepsFileName();
            var deps = JsonConvert.DeserializeObject<ProjectDeps>(File.ReadAllText(file));
            foreach (var item in deps.Libraries.AsParallel())
            {
                if (!item.Value.Serviceable 
                    || !string.Equals(item.Value.Type, "package", StringComparison.InvariantCultureIgnoreCase)
                    || (!string.IsNullOrWhiteSpace(_options.Skip) && Regex.IsMatch(item.Key, _options.Skip)))
                {
                    MessageHelper.Info($"Skip restore:{item.Key}");
                    continue;
                }

                (string name, string version) = ParsePackageName(item.Key);

                MessageHelper.Info($"Begin restore package:{name}[{version}]");

                await DownloadPackage(name, version, Path.Combine(fwFolder, item.Value.Path));
            }

            MessageHelper.Info("Complete restore.");
        }
    }
}
