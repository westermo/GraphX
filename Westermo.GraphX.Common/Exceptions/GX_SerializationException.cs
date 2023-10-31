﻿using System;

namespace Westermo.GraphX.Common.Exceptions
{
    public sealed class GX_SerializationException: Exception
    {
        public GX_SerializationException(string text)
            : base(text)
        {
        }
    }
}
