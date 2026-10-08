

using Microsoft.AspNetCore.Http;
using ParkingSystem.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace ParkingSystem.FileStorage;
public class LocalFileStorageAppService:  IFileStorageService

{
    private const long MAX_FILE_SIZE = 50 * 1024 * 1024;
    private const string locationStorage = @"D:\ParkingSystemStorage\Upload";

    private static readonly IReadOnlyList<string> AllowedExtensions = new List<string>
    {
    "pdf", "jpg", "jpeg", "png", "doc", "docx"
    };

    private static readonly IReadOnlyList<string> AllowedMimeTypes = new List<string>
    {
        "application/pdf",
            "image/jpeg",
            "image/png",
            "application/msword",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
    };
    private static string ResolvePath(string storageKey)
    {
        var root = Path.GetFullPath(locationStorage);

        var target = Path.GetFullPath(
            Path.Combine(root, storageKey)
        );

        var rootPrefix = Path.EndsInDirectorySeparator(root)
            ? root
            : root + Path.DirectorySeparatorChar;

        if (!target.StartsWith(
            rootPrefix,
            StringComparison.OrdinalIgnoreCase))
        {
            throw new FileStorageException("Invalid storage key");
        }

        return target;
    }

    private string CreateStorageKey(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            throw new FileStorageException("File is empty");
        }
        if (file.Length > MAX_FILE_SIZE)
        {
            throw new FileTooLargeException($"File size exceeds {MAX_FILE_SIZE}MB");
        }
        string fileName = file.FileName;
        string contentType = file.ContentType;
        string extension = Path.GetExtension(fileName).TrimStart('.');
        if (!AllowedMimeTypes.Contains(contentType))
        {
            throw new FileStorageException("File type is not allowed: " + contentType);
        }
        if (!AllowedExtensions.Contains(extension))
        {
            throw new FileStorageException("File extension is not allowed: " + extension);
        }
        return $"{Guid.NewGuid()}.{extension}";
    }
    public void Delete(string storageKey)
    {
        var target = ResolvePath(storageKey);

        if (!File.Exists(target))
        {
            throw new FileStorageException("File not found");
        }
        File.Delete(target);
    }

    public Stream Get(string storageKey)
    {
        var target = ResolvePath(storageKey);

        if (!File.Exists(target))
        {
            throw new FileStorageException("File not found");
        }
            var file = new FileStream(
            target,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read);
        return file;
    }

    public async Task<string> SaveAsync(
     IFormFile file,
     string directory,
     CancellationToken cancellationToken = default)
    {
        var fileName = CreateStorageKey(file);
        var storageKey = Path.Combine(directory, fileName);
        var target = Path.Combine(locationStorage, storageKey);

        Directory.CreateDirectory(Path.GetDirectoryName(target)!);

        using var stream = new FileStream(target, FileMode.Create);

        await file.CopyToAsync(stream, cancellationToken);

        return storageKey;
    }
}
