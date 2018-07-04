namespace BananoRunnerEmulator
{
    using System;

    public class RestartNeededException : Exception
    {
        public RestartNeededException(string message)
            : base(message)
        {
            // Nothing
        }

        public RestartNeededException(string message, Exception innerException)
            : base(message, innerException)
        {
            // Nothing
        }

        public RestartNeededException()
        {
            // Nothing
        }

        protected RestartNeededException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
            // Nothing
        }
    }
}
