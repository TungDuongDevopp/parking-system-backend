
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ParkingSystem.FileStorage;

public interface IFileStorageService 
{
    Task<string> SaveAsync(
       IFormFile file,
       string directory,
       CancellationToken cancellationToken = default);

    Stream Get(string storageKey);

    void Delete(string storageKey);

}
