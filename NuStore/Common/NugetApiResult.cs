using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace NuStore.Common
{
    //https://api.nuget.org/v3/index.json
    /// <summary>
    /// nuget service index result
    /// </summary>
    class NuGetServiceIndexApiResult
    {
        public string Version { get; set; }

        public NuGetResourceItem[] Resources { get; set; }

        public class NuGetResourceItem
        {
            [JsonProperty(PropertyName = "@id")]
            public string _ID { get; set; }

            //SearchQueryService 
            //  GET {@id}/query?q=NuGet.Versioning&prerelease=false&semVerLevel=1.0.7
            //PackageBaseAddress/3.0.0 
            //  GET {@id}/{LOWER_ID}/index.json //version list
            //  GET {@id}/{LOWER_ID}/{LOWER_VERSION}/{LOWER_ID}.{LOWER_VERSION}.nupkg
            //  GET {@id}/{LOWER_ID}/{LOWER_VERSION}/{LOWER_ID}.nuspec
            [JsonProperty(PropertyName = "@type")]
            public string _Type { get; set; }
            
            public string Comment { get; set; }
        }
    }

    ////SearchQueryService
    //class NugetQueryApiResult
    //{
    //    public NugetQueryData[] data { get; set; }

    //    public class NugetQueryData
    //    {
    //        [JsonProperty(PropertyName ="@id")]
    //        public string _ID { get; set; }

    //        [JsonProperty(PropertyName = "@type")]
    //        public string _Type { get; set; }

    //        public string ID { get; set; }

    //        public string Version { get; set; }
    //    }

    //    public class NugetQueryVersion
    //    {
    //        public string Version { get; set; }

    //        //public int Download { get; set; }

    //        [JsonProperty(PropertyName = "@id")]
    //        public string _ID { get; set; }
    //    }
    //}

    ////Package Content
    //public class NugetPackageContentApiResult
    //{
    //    public string PackageContent { get; set; }
    //}
}
