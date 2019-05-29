using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NuStore
{
    internal interface ICommand
    {
        Task Execute();
    }
}
