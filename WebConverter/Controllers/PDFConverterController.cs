using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using PuppeteerSharp;
using System;
using System.Threading.Tasks;
using System.Xml.Linq;
using WebConverter.Hubs;
using WebConverter.Utils.Interface;

namespace WebConverter.Controllers
{
    public class PDFConverterController : BaseController
    {
        [HttpGet]
        [Authorize]
        public IEnumerable<string> GetRecentFiles()
        {
            return _fileStorage.GetFiles(UserId, "*.pdf").Select(x => Path.GetFileName(x));
        }

        [HttpGet]
        [Authorize]
        public IActionResult Download(string name)
        {
            var files = _fileStorage.GetFiles(UserId, name);
            if (files.Any())
            {
                var fs = new FileStream(files.First(), FileMode.Open);
                return File(fs, "application/pdf", Path.GetFileName(files.First()));
            }
            return BadRequest("No file");
        }

        private readonly IConverter _converter;
        private readonly IFileStorage _fileStorage;
        private IHubContext<SignalRHub> HubContext;
        private string connectionId;

        public PDFConverterController(IConverter converter, IFileStorage fileStorage, IHubContext<SignalRHub> hubcontext)
        {
            _converter = converter;
            _fileStorage = fileStorage;
            HubContext = hubcontext;
        }


        [HttpPost("ConvertToPDF")]
        [Authorize]
        public async Task<IActionResult> ConvertToPDF(object sender, EventArgs e)
        {
            //living progress bar
            connectionId = Request.Headers["connectionId"].FirstOrDefault();
            if (connectionId != null)
            {
                _converter.ProgressUpdate += OnProgressUpdate;
            }

            //store file source on a disk
            var file = Request.Form.Files["file"];
            if (file == null)
            {
                return BadRequest("No file");
            }
            var path = _fileStorage.SaveFile(file, UserId);
            if (string.IsNullOrEmpty(path.Result))
            {
                return Problem("Unable to store file");
            }

            //conversion
            try
            {
                var task = _converter.Convert(path.Result);
                await task;
                var fs = new FileStream(task.Result, FileMode.Open);
                return File(fs, "application/pdf", Path.GetFileName(task.Result));
            }
            catch (Exception error)
            {
                return Problem(error.Message);
            }
            finally
            {
                _converter.ProgressUpdate -= OnProgressUpdate;
            }
        }

        private void OnProgressUpdate(object sender, int percentsComplete)
        {
            HubContext.Clients.Client(connectionId).SendAsync("ReceiveMessage", percentsComplete, null);
        }
    }
}
