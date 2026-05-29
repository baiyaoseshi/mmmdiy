using System.Threading.Tasks;

namespace 淼喵妙用户界面.Services
{
    /// <summary>
    /// 文件服务接口 - 封装文件 I/O 操作，便于单元测试和替换
    /// </summary>
    public interface IFileService
    {
        /// <summary>
        /// 读取文件全部文本
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <returns>文件内容</returns>
        string ReadAllText(string path);

        /// <summary>
        /// 异步读取文件全部文本
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <returns>文件内容</returns>
        Task<string> ReadAllTextAsync(string path);

        /// <summary>
        /// 写入文本到文件
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <param name="content">要写入的内容</param>
        void WriteAllText(string path, string content);

        /// <summary>
        /// 异步写入文本到文件
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <param name="content">要写入的内容</param>
        /// <returns>Task</returns>
        Task WriteAllTextAsync(string path, string content);

        /// <summary>
        /// 检查文件是否存在
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <returns>文件存在返回true</returns>
        bool Exists(string path);
    }
}