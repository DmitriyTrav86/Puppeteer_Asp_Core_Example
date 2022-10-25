using WebConverter.Utils.Interface;

namespace WebConverter.Utils
{
    public class FileStorage : IFileStorage
    {
        private readonly IConfiguration Configuration;
        private readonly string workingDirectory;
        public FileStorage (IConfiguration configuration)
        {
            Configuration = configuration;
            workingDirectory = Path.Combine(Directory.GetCurrentDirectory(), Configuration["Services:Files:RelativeRootPath"]);
        }

        public IEnumerable<string> GetFiles(string relativePath, string pattern = "*")
        {
            if (Directory.Exists(Path.Combine(workingDirectory, relativePath)))
                return Directory.GetFiles(Path.Combine(workingDirectory, relativePath), pattern);
            return new List<string>();
        }

        public async Task<string> SaveFile(IFormFile formFile, string relativePath)
        {
            try
            {
                if (relativePath == null) relativePath = "";
                
                var filePath = Path.Combine(workingDirectory, relativePath, formFile.FileName);
                if (formFile.Length > 0)
                {
                    if (!Directory.Exists(Path.Combine(workingDirectory, relativePath)))
                        Directory.CreateDirectory(Path.Combine(workingDirectory, relativePath));
                    
                    using (Stream fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await formFile.CopyToAsync(fileStream);
                    }
                }
                else
                {
                    return null;
                }
                return filePath;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
