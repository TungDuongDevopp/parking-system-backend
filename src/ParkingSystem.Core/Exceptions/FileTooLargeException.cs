

using Abp.UI;
using System;
using System.Net;

namespace ParkingSystem.Exceptions;

public class FileTooLargeException : UserFriendlyException, IHasHttpStatusCode
{
    public HttpStatusCode StatusCode => HttpStatusCode.RequestEntityTooLarge;

    public FileTooLargeException(string message)
        : base(message)
    {
    }

    public FileTooLargeException(string message, string details)
        : base(message, details)
    {
    }

    public FileTooLargeException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
