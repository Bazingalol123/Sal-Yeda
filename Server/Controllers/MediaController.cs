using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using template.Server.Helpers;
using TriangleFileStorage;
using System.IO;

namespace template.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MediaController : ControllerBase
    {
        private readonly FilesManage _filesManage;

        public MediaController(FilesManage filesManage)
        {
            _filesManage = filesManage;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile([FromBody] string imageBase64)
        {
            Console.WriteLine("UploadFile called");
            string fileName = await _filesManage.SaveFile(imageBase64, "png", "uploadedFiles");
            Console.WriteLine($"File uploaded: {fileName}");
            return Ok(fileName);
        }

        [HttpPost("uploadTemp")]
        public async Task<IActionResult> UploadFileTemp([FromBody] string imageBase64)
        {
            Console.WriteLine("UploadFileTemp called");
            string fileName = await _filesManage.SaveFile(imageBase64, "png", "uploadTemp");
            Console.WriteLine($"Temp file uploaded: {fileName}");
            return Ok(fileName);
        }

        [HttpPost("deleteImages")]
        public async Task<IActionResult> DeleteImages([FromBody] List<string> images)
        {
            Console.WriteLine("DeleteImages called");
            var countFalseTry = 0;
            foreach (string img in images)
            {
                Console.WriteLine($"Attempting to delete image: {img}");
                if (!_filesManage.DeleteFile(img, ""))
                {
                    countFalseTry++;
                    Console.WriteLine($"Failed to delete image: {img}");
                }
                else
                {
                    Console.WriteLine($"Successfully deleted image: {img}");
                }
            }

            if (countFalseTry > 0)
            {
                Console.WriteLine($"Problem with {countFalseTry} images");
                return BadRequest("problem with " + countFalseTry.ToString() + " images");
            }
            return Ok("deleted");
        }

        [HttpPost("moveFiles")]
        public async Task<IActionResult> MoveFiles([FromBody] List<string> fileNames)
        {
            Console.WriteLine("MoveFiles called");
            var countFalseTry = 0;
            foreach (string fileName in fileNames)
            {
                string sourcePath = Path.Combine("wwwroot/uploadTemp", fileName);
                string destinationPath = Path.Combine("wwwroot/uploadedFiles", fileName);

                Console.WriteLine($"Attempting to move file: {fileName}");
                Console.WriteLine($"Source path: {sourcePath}");
                Console.WriteLine($"Destination path: {destinationPath}");

                // Check if the file already exists in the destination
                if (System.IO.File.Exists(destinationPath))
                {
                    // Skip the file as it already exists in the destination
                    Console.WriteLine($"File already exists in destination: {destinationPath}");
                    continue;
                }

                // Check if the file exists in the source
                if (!System.IO.File.Exists(sourcePath))
                {
                    countFalseTry++;
                    Console.WriteLine($"File does not exist in source: {sourcePath}");
                    continue;
                }

                try
                {
                    if (!Directory.Exists(Path.GetDirectoryName(destinationPath)))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
                        Console.WriteLine($"Created directory: {Path.GetDirectoryName(destinationPath)}");
                    }

                    System.IO.File.Move(sourcePath, destinationPath);
                    Console.WriteLine($"Successfully moved file to: {destinationPath}");
                }
                catch (Exception ex)
                {
                    countFalseTry++;
                    Console.WriteLine($"Error moving file: {fileName}, Exception: {ex.Message}");
                }
            }

            if (countFalseTry > 0)
            {
                Console.WriteLine($"Problem with moving {countFalseTry} files");
                return BadRequest("Problem with moving " + countFalseTry.ToString() + " files");
            }
            return Ok("Files moved successfully");
        }
    }
}
