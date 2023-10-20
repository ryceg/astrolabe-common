using System;

namespace Astrolabe.Common.Exceptions;

public class NotFoundException : Exception
{

    public static void ThrowIfNull(object obj)
    {
        if (obj == null)
        {
            throw new NotFoundException();
        }
    }
}