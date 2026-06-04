using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace ClinicManager.Web.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _env;

    public LocalFileStorageService(IWebHostEnvironment env)
    {
        _env = env;
    }

    public async Task<string?> SaveFileAsync(IFormFile file, string folderName)
    {
        if (file == null || file.Length == 0)
            return null;

        var uploadsFolder = Path.Combine(_env.WebRootPath, folderName);

        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        var uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(fileStream);
        }

        return $"/{folderName}/{uniqueFileName}";
    }

    public void DeleteFile(string fileUrl)
    {
        if (string.IsNullOrEmpty(fileUrl)) return;

        var relativePath = fileUrl.TrimStart('/');
        var physicalPath = Path.Combine(_env.WebRootPath, relativePath);

        if (File.Exists(physicalPath))
        {
            File.Delete(physicalPath);
        }
    }
}