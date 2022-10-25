using PuppeteerSharp;
using PuppeteerSharp.Media;
using System.Text;
using WebConverter.Utils.Interface;

namespace WebConverter.Utils
{
    public class PDFConverter : IConverter
    {
        private readonly IConfiguration Configuration;
        private readonly int PageWidth;
        private readonly int PageHeight;
        private readonly bool SinglePageMode;
        private const int FixBlankPageGap = 5;
        private readonly string EvaluateFunction;
        private readonly string WaitForFunction;
        private readonly string ChroniumRelativePath;
        private static List<string> FilesInProcess = new List<string>();
        private string FilePath; 
        private object locker = new();

        public event EventHandler<int> ProgressUpdate;

        public PDFConverter(IConfiguration configuration)
        {
            Configuration = configuration;
            PageWidth = int.Parse(Configuration["Convertation:Pdf:ViewPort:Width"]);
            PageHeight = int.Parse(Configuration["Convertation:Pdf:ViewPort:Height"]);
            SinglePageMode = bool.Parse(Configuration["Convertation:Pdf:ViewPort:SinglePageMode"]);
            EvaluateFunction = Configuration["Convertation:Pdf:Functions:Evaluate"];
            WaitForFunction = Configuration["Convertation:Pdf:Functions:WaitFor"];
            ChroniumRelativePath = Configuration["Convertation:Pdf:ChroniumRelativePath"];
        }

        public bool IsFileProcessing(string name)
        {
            return FilesInProcess.Contains(name);
        }

        private void UpdateProgress(int percents)
        {
            ProgressUpdate?.Invoke(this, percents);
            lock (locker)
            {
                if (!FilesInProcess.Contains(FilePath))
                    FilesInProcess.Add(FilePath);
            }
        }

        private void OnProcessEnd()
        {
            lock (locker)
            {
                if (FilesInProcess.Contains(FilePath))
                    FilesInProcess.Remove(FilePath);
            }
        }


        public async Task<string> GetChroniumPath() {
            var currentDirectory = Directory.GetCurrentDirectory();
            var downloadPath = Path.Combine(currentDirectory, ChroniumRelativePath);
            if (!Directory.Exists(downloadPath))
                Directory.CreateDirectory(downloadPath);
            var files = Directory.GetFiles(downloadPath, "chrome.exe", SearchOption.AllDirectories);
            if (files.Length == 0)
            {
                await DownloadChroniumAsync(downloadPath);
                files = Directory.GetFiles(downloadPath, "chrome.exe", SearchOption.AllDirectories);
            }
            return files[0];
        }

        public async Task<string> Convert(string filePath)
        {
            try
            {
                FilePath = filePath;
                Validate();
                UpdateProgress(0);
                LaunchOptions launchOptions = await GetLaunchOptions();
                UpdateProgress(15);

                using (var browser = await Puppeteer.LaunchAsync(launchOptions))
                {

                    UpdateProgress(30);

                    using (var page = await browser.NewPageAsync())
                    {
                        UpdateProgress(45);

                        await page.GoToAsync("file:" + filePath, new NavigationOptions { Timeout = 0 });
                        await page.SetViewportAsync(new ViewPortOptions { Width = PageWidth, Height = PageHeight });

                        UpdateProgress(60);

                        if (!string.IsNullOrEmpty(EvaluateFunction))
                            await page.EvaluateFunctionAsync(EvaluateFunction);
                        if (!string.IsNullOrEmpty(WaitForFunction))
                            await page.WaitForFunctionAsync(WaitForFunction, new WaitForFunctionOptions { Timeout = 0 });

                        UpdateProgress(75);

                        var height = SinglePageMode
                            ? (await page.EvaluateFunctionAsync<int>("()=>window.scrollY") + PageHeight + FixBlankPageGap)
                            : PageHeight;

                        var resultFile = filePath.Replace(".html", ".pdf");
                        await page.PdfAsync(resultFile, new PdfOptions
                        {
                            Format = null,
                            DisplayHeaderFooter = false,
                            PrintBackground = true,
                            Width = PageWidth,
                            Height = height
                        });

                        UpdateProgress(100);

                        return resultFile;
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                OnProcessEnd();
            }
        }

        private void Validate()
        {
            if (Path.GetExtension(FilePath).ToLower() != ".html") { throw new Exception("Incorrect file extension."); }
        }

        private async Task<LaunchOptions> GetLaunchOptions()
        {
            var launchOptions = new LaunchOptions()
            {
                Headless = true,
                ExecutablePath = await GetChroniumPath(),
                DefaultViewport = new ViewPortOptions { Width = PageWidth, Height = PageHeight },
                Timeout = 0
            };
            return launchOptions;
        }

        private static async Task DownloadChroniumAsync(string downloadPath)
        {
            Console.WriteLine("Downloading Chromium");
            var browserFetcherOptions = new BrowserFetcherOptions { Path = downloadPath };
            var browserFetcher = new BrowserFetcher(browserFetcherOptions);
            await browserFetcher.DownloadAsync(BrowserFetcher.DefaultChromiumRevision);
        }
    }
}
