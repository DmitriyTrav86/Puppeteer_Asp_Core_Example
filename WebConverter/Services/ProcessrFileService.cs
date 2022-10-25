using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using WebConverter.Utils.Interface;

namespace ServiceWorkerCronJobDemo.Services
{
    public interface IScopedService
    {
        Task DoWork(CancellationToken cancellationToken);
    }

    public class ProcessFileService : IScopedService
    {
        private readonly ILogger<ProcessFileService> _logger;
        private readonly IConfiguration Configuration;
        private readonly int lifetimeInMinutes;
        private readonly IConverter _converter;


        public ProcessFileService(ILogger<ProcessFileService> logger, IConfiguration configuration, IConverter converter)
        {
            _logger = logger;
            Configuration = configuration;
            lifetimeInMinutes = int.Parse(Configuration["Services:Files:Livetime"]);
            _converter = converter;
        }

        public async Task DoWork(CancellationToken cancellationToken)
        {
            var workingDirectory = Path.Combine(Directory.GetCurrentDirectory(), Configuration["Services:Files:RelativeRootPath"]);
            if (Directory.Exists(workingDirectory)) { 
                DirectoryInfo workingDirectoryInfo = new DirectoryInfo(workingDirectory);
                foreach (DirectoryInfo directoryInfo in workingDirectoryInfo.GetDirectories().Concat(new[] { workingDirectoryInfo }).ToList())
                {
                    processDirectory(directoryInfo);
                }
            }

            _logger.LogInformation("{now} ProcessFileService is working.", DateTime.Now.ToString("T"));
            await Task.Delay(1000 * 20, cancellationToken);
        }

        private async void processDirectory(DirectoryInfo directoryInfo)
        {
            try
            {
                foreach (FileInfo file in directoryInfo.GetFiles())
                {
                    if (file.Extension.ToLower() == ".html" && !_converter.IsFileProcessing(file.FullName))
                    {
                        var pdfFile = Path.ChangeExtension(file.FullName, ".pdf");
                        if (!File.Exists(pdfFile) || (new FileInfo(pdfFile)).Length == 0)
                        {
                            await _converter.Convert(file.FullName);
                        }
                    }
                    if (file.LastWriteTime < DateTime.Now.AddMinutes(-lifetimeInMinutes))
                        file.Delete();
                }
                if (!directoryInfo.GetFiles().Any() && !directoryInfo.GetDirectories().Any())
                    directoryInfo.Delete();
            }   
            catch(Exception e)
            {

            }
        }
    }
}
