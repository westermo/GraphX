using System;

namespace Westermo.GraphX.Common.Exceptions
{
    public sealed class GX_ObsoleteException(string text) : Exception(text);
}
