﻿using System.Runtime.Serialization;

namespace GameCube.AmusementVision.LZ
{
    /// <summary>
    /// Thrown when an invalid Arc file is read/written.
    /// </summary>
    public class InvalidLzFileException : Exception
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

        public InvalidLzFileException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
