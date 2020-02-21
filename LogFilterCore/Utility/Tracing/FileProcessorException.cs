using System;

namespace LogFilterCore.Utility.Tracing
{
    public class FileProcessorException : Exception
    {
        public FileProcessorException(string message) : base(message)
        {
        }

        public FileProcessorException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}