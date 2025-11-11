using System;

namespace Westermo.GraphX.Common.Exceptions;

public sealed class GX_InvalidDataException(string text) : Exception(text);

public sealed class GX_ConsistencyException(string text) : Exception(text);