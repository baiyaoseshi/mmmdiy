using System.IO;
using System.Threading.Tasks;

namespace 淼喵妙用户界面.Services
{
    /// <summary>
    /// 文件服务实现 - 基于 System.IO.File
    /// </summary>
    public class FileService : IFileService
    {
        /// <inheritdoc/>
        public string ReadAllText(string path)
        {
            return File.ReadAllText(path);
        }

        /// <inheritdoc/>
        public Task<string> ReadAllTextAsync(string path)
        {
            return Task.Run(() => File.ReadAllText(path));
        }

        /// <inheritdoc/>
        public void WriteAllText(string path, string content)
        {
            File.WriteAllText(path, content);
        }

        /// <inheritdoc/>
        public Task WriteAllTextAsync(string path, string content)
        {
            return Task.Run(() => File.WriteAllText(path, content));
        }

        /// <inheritdoc/>
        public bool Exists(string path)
        {
            return File.Exists(path);
        }
    }
}