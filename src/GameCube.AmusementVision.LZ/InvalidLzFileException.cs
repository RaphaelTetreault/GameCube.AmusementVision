using System;

namespace GameCube.AmusementVision.LZ;

/// <summary>
///     Thrown when an invalid LZ file is read/written.
/// </summary>
public sealed class InvalidLzFileException : Exception
{
    public InvalidLzFileException()
    {
    }

    public InvalidLzFileException(string message)
        : base(message)
    {
    }

    public InvalidLzFileException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
