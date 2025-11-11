using System;
using System.Reflection;

namespace Westermo.GraphX.Controls;

internal static class ExceptionExtensions
{
    extension(Exception exception)
    {
        internal void PreserveStackTrace()
        {
            // In .NET 4.5 and later this isn't needed... (yes, this is a brutal hack!)
            var preserveStackTrace = typeof(Exception).GetMethod(
                "InternalPreserveStackTrace",
                BindingFlags.Instance | BindingFlags.NonPublic);

            preserveStackTrace?.Invoke(exception, null);
        }
    }
}