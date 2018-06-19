﻿using System;
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
    internal class RestoreCommand
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

        private async Task<bool> DownloadPackage(string name, string version, string pkgFolder)
        {
            if (Directory.Exists(pkgFolder) && !_options.ForceOverride)
            {
                MessageHelper.Warning($"Skip override:{pkgFolder}");
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
                        if (entry.FullName.StartsWith("lib/"))
                        {
                        
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

                            hasSave = true;
                            Directory.CreateDirectory(Path.GetDirectoryName(fileName));
                            entry.ExtractToFile(fileName, true);//_options.ForceOverride
                        }
                    }

                    if(hasSave)
                    {
                        MessageHelper.Successs($"Save {name}[{version}] to {pkgFolder}");

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

        private (string name, string version) ParsePackageName(string packageName)
        {
            var index = packageName.LastIndexOf('/');

            return (packageName.Substring(0, index), packageName.Substring(index + 1));
        }

        private ConcurrentDictionary<string, Regex[]> _dicReg = new ConcurrentDictionary<string, Regex[]>();
        private bool IsMatch(string regString, string value)
        {
            if(!string.IsNullOrWhiteSpace(regString))
            {
                var regList = _dicReg.GetOrAdd(regString, str => {
                    return regString
                    .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => new Regex(s, RegexOptions.IgnoreCase)).ToArray();
                });

                return regList.Any(r => r.IsMatch(value));
            }

            return false;
        }

        private bool NeedDownload(string package)
        {
            if (!string.IsNullOrWhiteSpace(_options.Special) && IsMatch(_options.Special, package))
                return true;

            return IsMatch(_options.Exclude, package) == false;
        }

        public async Task Execute()
        {
            //x86 x64
            var bit = "x64";// ent.Is64BitOperatingSystem ? "x64" : "x86";
            var fwFolder = Path.Combine(GetStoreDirctory(), $"{bit}/netcoreapp2.0");
            MessageHelper.Warning($"Retore packages to {fwFolder}");

            var count = 0;
            var file = GetDepsFileName();
            var deps = JsonConvert.DeserializeObject<ProjectDeps>(File.ReadAllText(file));
            foreach (var item in deps.Libraries.AsParallel())
            {
                if (!item.Value.Serviceable
                    || !string.Equals(item.Value.Type, "package", StringComparison.InvariantCultureIgnoreCase)
                    ||!NeedDownload(item.Key))
                {
                    MessageHelper.Warning($"Not Package Skip restore:{item.Key}");
                    continue;
                }


                (string name, string version) = ParsePackageName(item.Key);

                MessageHelper.Info($"Begin restore package:{name}[{version}]");

                if(await DownloadPackage(name, version, Path.Combine(fwFolder, item.Value.Path)))
                {
                    count++;
                }
            }

            MessageHelper.Successs($"Complete Restore {count} packages.");
        }
    }
}