namespace WebConverter.Utils.Interface
{
    public interface IFileStorage
    {
        Task<string> SaveFile(IFormFile formFile, string relativePath);
        IEnumerable<string> GetFiles(string relativePath, string pattern);
    }
}