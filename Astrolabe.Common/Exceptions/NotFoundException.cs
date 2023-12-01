using System;
using System.Diagnostics.CodeAnalysis;

namespace Astrolabe.Common.Exceptions;

public class NotFoundException : Exception
{

    public static void ThrowIfNull([NotNull] object? obj)
    {
        if (obj == null)
        {
            throw new NotFoundException();
        }
    }
}