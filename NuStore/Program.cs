using System;
using CommandLine;
using NuStore.Common;

namespace NuStore
{
    class Program
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<RestoreOptions>(args).WithParsed(opt => {
                new RestoreCommand(opt).Execute().Wait();
            });
        }
    }
}
