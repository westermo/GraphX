using System;

namespace Westermo.GraphX.Common.Exceptions
{
    public sealed class GX_ObjectNotFoundException: Exception
    {
        public GX_ObjectNotFoundException(string text)
            : base(text)
        {
        }
    }
}
