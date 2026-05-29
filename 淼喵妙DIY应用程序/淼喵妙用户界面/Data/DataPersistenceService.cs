using System;
using System.IO;
using System.Text.Json;

namespace 淼喵妙用户界面.Data
{
    /// <summary>
    /// 数据持久化服务 - 负责用户数据的保存和加载
    /// </summary>
    public static class DataPersistenceService
    {
        /// <summary>
        /// 获取应用程序数据存储目录路径
        /// </summary>
        private static string AppDataPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "淼喵妙脚本DIY");
        
        /// <summary>
        /// 获取用户数据文件完整路径
        /// </summary>
        private static string UserDataFilePath => Path.Combine(AppDataPath, "userdata.json");

        /// <summary>
        /// 加载用户数据
        /// </summary>
        /// <returns>用户数据对象，如果加载失败则返回新的空数据</returns>
        public static UserData LoadUserData()
        {
            try
            {
                if (File.Exists(UserDataFilePath))
                {
                    string json = File.ReadAllText(UserDataFilePath);
                    return JsonSerializer.Deserialize<UserData>(json, new JsonSerializerOptions { WriteIndented = true });
                }
            }
            catch { }
            
            // 加载失败或文件不存在时返回新的空数据
            return new UserData();
        }

        /// <summary>
        /// 保存用户数据
        /// </summary>
        /// <param name="data">要保存的用户数据</param>
        public static void SaveUserData(UserData data)
        {
            try
            {
                // 确保数据目录存在
                if (!Directory.Exists(AppDataPath))
                {
                    Directory.CreateDirectory(AppDataPath);
                }
                
                // 序列化并保存数据
                string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(UserDataFilePath, json);
            }
            catch { }
        }

        /// <summary>
        /// 检查用户数据文件是否存在
        /// </summary>
        /// <returns>如果数据文件存在返回 true，否则返回 false</returns>
        public static bool HasUserData()
        {
            return File.Exists(UserDataFilePath);
        }

        /// <summary>
        /// 清空用户数据文件（数据损坏时调用）
        /// </summary>
        public static void ClearUserData()
        {
            try
            {
                if (File.Exists(UserDataFilePath))
                    File.Delete(UserDataFilePath);
            }
            catch { }
        }
    }
}