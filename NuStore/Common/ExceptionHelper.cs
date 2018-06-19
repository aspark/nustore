using System;
using System.Collections.Generic;
using System.Text;

namespace NuStore.Common
{
    public static class ExceptionHelper
    {
        public static string GetMessage(this Exception exception)
        {
            return exception?.GetBaseException()?.Message;
        }
    }
}
