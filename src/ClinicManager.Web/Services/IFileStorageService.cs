using Microsoft.AspNetCore.Http;

namespace ClinicManager.Web.Services;

public interface IFileStorageService
{
    Task<string?> SaveFileAsync(IFormFile file, string folderName);
    void DeleteFile(string filePath);
}