namespace WebConverter.Utils.Interface
{
    public interface IConverter
    {
        Task<string> Convert(string filePath);
        bool IsFileProcessing(string name);

        event EventHandler<int> ProgressUpdate;
    }
}