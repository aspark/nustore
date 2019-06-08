using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace NuStore.Common
{
    internal class ProjectDeps
    {
        [JsonProperty("runtimeTarget")]
        public RuntimeTarget RuntimeTarget { get; set; }

        [JsonProperty("targets")]
        public Dictionary<string, Dictionary<string, JToken>> Targets { get; set; }

        [JsonProperty("compilationOptions")]
        public CompilationOptions CompilationOptions { get; set; }

        [JsonProperty("libraries")]
        public Dictionary<string, ProjectLibrary> Libraries { get; set; }
    }

    internal class RuntimeTarget
    {
        [JsonProperty("name")]
        public string Name { get; set; }
    }

    internal class CompilationOptions
    {
        [JsonProperty("platform")]
        public string Platform { get; set; }
    }

    internal class ProjectLibrary
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("serviceable")]
        public bool Serviceable { get; set; }

        [JsonProperty("path")]
        public string Path { get; set; }

        [JsonProperty("sha512")]
        public string SHA512 { get; set; }

        [JsonProperty("hashPath")]
        public string HashPath { get; set; }
    }
}
