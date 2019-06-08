using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace NuStore.Common
{
    internal class ProjectDeps
    {
        public RuntimeTarget RuntimeTarget { get; set; }

        public Dictionary<string, Dictionary<string, JToken>> Targets { get; set; }

        public CompilationOptions CompilationOptions { get; set; }

        public Dictionary<string, ProjectLibrary> Libraries { get; set; }
    }

    internal class RuntimeTarget
    {
        public string Name { get; set; }
    }

    internal class CompilationOptions
    {
        public string Platform { get; set; }
    }

    internal class ProjectLibrary
    {
        public string Type { get; set; }

        public bool Serviceable { get; set; }

        public string Path { get; set; }

        public string SHA512 { get; set; }

        public string HashPath { get; set; }
    }
}
