using System;

namespace Westermo.GraphX.Common.Exceptions;

public sealed class GX_ObjectNotFoundException(string text) : Exception(text);