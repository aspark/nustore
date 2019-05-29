using System;
using System.Collections.Generic;
using System.Text;
using CommandLine;
using CommandLine.Text;

namespace NuStore.Common
{
    [Verb("restore", HelpText="restore the packages declared in deps file")]
    internal class RestoreOptions
    {
        [Option('p', "deps", HelpText ="deps file. default is *.deps.json in current directory")]
        public string DepsFile { get; set; }

        [Option('d', "dir", HelpText = "directory packages stored (typically at /usr/local/share/dotnet/store on macOS/Linux and C:/Program Files/dotnet/store on Windows)")]
        public string Directory { get; set; }

        [Option('f', "force", HelpText = "override existing packages, default is false")]
        public bool ForceOverride { get; set; }

        [Option("nuget", HelpText = "set nuget resource api url. default: https://api.nuget.org/v3/index.json")]
        public string NugetServiceIndex { get; set; }

        [Option('e', "exclude", HelpText = "skip packages, support regex. separated by semicolon for mutiple")]
        public string Exclude { get; set; }

        [Option('s', "special", HelpText = "restore special packages, support regex. separated by semicolon for mutiple")]
        public string Special { get; set; }

        [Option("runtime", HelpText = ".net core runtime version, the defaut value set by deps file, for example netcoreapp2.0/netcoreapp2.1")]
        public string Runtime { get; set; }

        [Option("arch", HelpText="x64/x86, by default this value is resolved from platform attribute which declared in deps file")]
        public string Architecture { get; set; }

        [Option("flatten", HelpText = "restore files directly onto path/package.dll instead of restoring in nested folders (eg. path/x64/netcoreapp2.0/package/1.2.0/lib/netcoreapp2.0/package.dll)")]
        public bool Flatten { get; set; }

        [Option('v', "verbosity", HelpText = "show detailed log")]
        public bool Verbosity { get; set; }

        [Usage]
        public static IEnumerable<Example> Examples
        {
            get
            {
                yield return new Example("Use test.json to resolve deps, and save packages to d:/packages", new RestoreOptions { DepsFile = "test.json", Directory = "d:/packages" });
                yield return new Example("Skip special package", new RestoreOptions { Exclude = "^microsoft.*;^System.*" });
                yield return new Example("Only restore the special package", new RestoreOptions { Special = "Microsoft\\.Extensions.Logging", ForceOverride = true });
            }
        }
    }

    //[Verb("fallback", HelpText = "fullfill fallback folder with special version packages")]
    //public class FallbackOptions
    //{

    //}

    [Verb("minify", HelpText = "Minify the publish package(delete the packages which not hosted in nuget)")]
    public class MinifyOptions
    {
        [Option('d', "dir", HelpText="output directory")]
        public string Directory { get; set; }

        [Option('v', "verbosity", HelpText = "show detailed log")]//verbose
        public bool Verbosity { get; set; }

        [Option('k', "kind", HelpText = "specify target assembly kind (library, exe, winexe supported, default is same as first assembly)")]
        public ILRepacking.ILRepack.Kind? Kind { get; set; }

        [Option("search", HelpText = "adds the path to the search directories for referenced assemblies (can be specified multiple times)")]
        public string[] SearchDirectories { get; set; }

        [Option("delaysign", HelpText = "sets the key, but don't sign the assembly")]
        public bool DelaySign { get; set; }

        [Usage]
        public static IEnumerable<Example> Examples
        {
            get
            {
                yield return new Example("Minify all dlls in currenty dir to one dll, and save it to d:/packages", new MinifyOptions { Directory = "d:/publish" });
            }
        }
    }
}
