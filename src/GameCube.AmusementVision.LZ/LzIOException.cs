using System;

namespace GameCube.AmusementVision.LZ;

/// <summary>
///     Thrown when an LZ file fails to be written.
/// </summary>
public sealed class LzIOException : System.IO.IOException
{
    public LzIOException()
    {
    }

    public LzIOException(string message)
        : base(message)
    {
    }

    public LzIOException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}