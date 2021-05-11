using System;

namespace TermsrvPatcher
{
    /// <summary>
    /// Dedicated exception for indicating stopping a service failed.
    /// </summary>
    class ServiceStopException : Exception
    {
        public ServiceStopException()
           : base()
        { }
    }
}
