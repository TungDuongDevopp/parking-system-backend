
using Abp.UI;
using System;
using System.Net;

namespace ParkingSystem.Exceptions;

public class FileStorageException: UserFriendlyException, IHasHttpStatusCode
{
    public HttpStatusCode StatusCode => HttpStatusCode.BadRequest;

    public FileStorageException(string message)
        : base(message)
    {
    }

    public FileStorageException(string message, string details)
        : base(message, details)
    {
    }

    public FileStorageException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
