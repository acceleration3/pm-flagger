﻿/*
 * Created: July 26, 2015
 * Purpose: Extend MemoryStream
 */

using System.IO;

public static class MemoryStreamExtensions
{
    public static void Append(this MemoryStream stream, byte value)
    {
        stream.Append(new[] { value });
    }

    public static void Append(this MemoryStream stream, byte[] values)
    {
        stream.Write(values, 0, values.Length);
    }
}