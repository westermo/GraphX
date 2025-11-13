using System;

namespace Westermo.GraphX.Common.Exceptions;

public sealed class GX_SerializationException(string text) : Exception(text);