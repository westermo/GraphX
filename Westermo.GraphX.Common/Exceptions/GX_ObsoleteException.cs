﻿using System;

namespace Westermo.GraphX.Common.Exceptions
{
    public sealed class GX_ObsoleteException: Exception
    {
        public GX_ObsoleteException(string text)
            : base(text)
        {
        }
    }
}
