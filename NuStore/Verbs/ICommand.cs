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
    internal interface ICommand
    {
        Task Execute();
    }

    internal abstract class CommandBase : ICommand
    {
        public abstract Task Execute();
    }
    internal abstract class DepsCommandBase : CommandBase
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="depsFile">deps file. default is *.deps.json in current directory</param>
        /// <returns></returns>
        private protected string EnsureDepsFileName(string depsFile = null)
        {
            string fileName = "";
            if (!string.IsNullOrWhiteSpace(depsFile))
            {
                fileName = Path.Combine(Directory.GetCurrentDirectory(), depsFile);
                if (!File.Exists(fileName))
                {
                    throw new FileNotFoundException($"Can't find deps file:{fileName}");
                }

                return fileName;
            }

            fileName = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.deps.json").FirstOrDefault();

            if (string.IsNullOrEmpty(fileName))
            {
                throw new FileNotFoundException("Can't find deps file.");
            }

            return fileName;
        }

        private protected (string FileName, ProjectDeps) GetDeps(string depsFile = null)
        {
            var file = EnsureDepsFileName(depsFile);

            return (file, JsonConvert.DeserializeObject<ProjectDeps>(File.ReadAllText(file)));
        }

        /// <summary>
        /// splite name/version
        /// </summary>
        /// <param name="packageName"></param>
        /// <returns></returns>
        private protected (string name, string version) ParsePackageName(string packageName)
        {
            var index = packageName.LastIndexOf('/');

            return (packageName.Substring(0, index), packageName.Substring(index + 1));
        }
    }
}
