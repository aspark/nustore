using System;
using System.Collections.Generic;
using System.Text;

namespace NuStore.Common
{
    internal class ProjectDeps
    {
        public Dictionary<string, ProjectLibrary> Libraries { get; set; }
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
