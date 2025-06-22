using System;

namespace DevNest.Core.Exceptions
{
    public class DevNestException : Exception
    {
        public DevNestException()
        {
        }

        public DevNestException(string message) : base(message)
        {
        }

        public DevNestException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    public class ServiceException : DevNestException
    {
        public string ServiceName { get; }

        public ServiceException(string serviceName) : base($"Service '{serviceName}' error occurred.")
        {
            ServiceName = serviceName;
        }

        public ServiceException(string serviceName, string message) : base(message)
        {
            ServiceName = serviceName;
        }

        public ServiceException(string serviceName, string message, Exception innerException) : base(message, innerException)
        {
            ServiceName = serviceName;
        }
    }

    public class SiteException : DevNestException
    {
        public string SiteName { get; }

        public SiteException(string siteName) : base($"Site '{siteName}' error occurred.")
        {
            SiteName = siteName;
        }

        public SiteException(string siteName, string message) : base(message)
        {
            SiteName = siteName;
        }

        public SiteException(string siteName, string message, Exception innerException) : base(message, innerException)
        {
            SiteName = siteName;
        }
    }
}
