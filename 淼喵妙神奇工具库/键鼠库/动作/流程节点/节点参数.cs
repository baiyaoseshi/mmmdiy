using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;

namespace 淼喵妙神奇工具库.键鼠库.动作.流程节点
{
    /// <summary>
    /// 节点参数泛型类 - 统一管理直接值和全局端变量引用
    /// 运行时通过 解析值(全局) 按优先级获取最终值：变量名非空则从全局字典获取，否则返回直接值
    /// </summary>
    /// <typeparam name="T">参数值类型</typeparam>
    public class 节点参数<T>
    {
        /// <summary>
        /// 节点自身存储的直接值（变量名为空时使用）
        /// </summary>
        public T 直接值;

        /// <summary>
        /// 全局端变量名，非空时优先从全局字典获取值
        /// </summary>
        public string 变量名;

        /// <summary>
        /// 无参构造（供反序列化使用）
        /// </summary>
        public 节点参数()
        {
            直接值 = default;
            变量名 = "";
        }

        /// <summary>
        /// 仅指定直接值
        /// </summary>
        public 节点参数(T 直接值)
        {
            this.直接值 = 直接值;
            this.变量名 = "";
        }

        /// <summary>
        /// 指定直接值和变量名
        /// </summary>
        public 节点参数(T 直接值, string 变量名)
        {
            this.直接值 = 直接值;
            this.变量名 = 变量名 ?? "";
        }

        /// <summary>
        /// 解析参数的最终值
        /// 优先级：变量名非空 → 从全局字典获取（缺失或类型错误则抛异常），变量名为空 → 返回直接值
        /// </summary>
        /// <param name="全局">共享数据字典</param>
        /// <returns>解析后的值</returns>
        /// <exception cref="InvalidOperationException">全局变量名已设置但字典中不存在该键</exception>
        /// <exception cref="InvalidCastException">全局变量存在但类型转换失败</exception>
        public T 解析值(Dictionary<string, object> 全局)
        {
            var 变量名 = this.变量名.Replace("$","");
            if (!string.IsNullOrEmpty(变量名))
            {
                if (!全局.ContainsKey(变量名))
                    throw new InvalidOperationException($"全局变量 \"{变量名}\" 不存在于全局字典中，请检查上游节点是否正确设置了该变量");
                object value = 全局[变量名];
                try
                {
                    return (T)value;
                }
                catch (InvalidCastException)
                {
                    throw new InvalidCastException($"全局变量 \"{变量名}\" 的值类型为 {value?.GetType().Name ?? "null"}，无法转换为 {typeof(T).Name}");
                }
            }
            return 直接值;
        }

        /// <summary>
        /// 判断是否使用全局变量模式
        /// </summary>
        public bool 使用全局变量 => !string.IsNullOrEmpty(变量名);
    }

    /// <summary>
    /// 节点参数静态辅助类 - 提供序列化/反序列化统一入口
    /// 序列化格式：直接值模式输出 [值]，全局变量模式输出 [$变量名]
    /// </summary>
    public static class 节点参数
    {
        /// <summary>
        /// 序列化节点参数为字符串
        /// </summary>
        public static string 序列化<T>(节点参数<T> 参数)
        {
            if (参数.使用全局变量)
                return "$" + 参数.变量名;
            return 转换直接值为字符串(参数.直接值);
        }

        /// <summary>
        /// 反序列化字符串为节点参数
        /// </summary>
        public static 节点参数<T> 反序列化<T>(string 字符串)
        {
            if (string.IsNullOrEmpty(字符串))
                return new 节点参数<T>();

            if (字符串.StartsWith("$"))
            {
                string 变量名 = 字符串.Substring(1);
                return new 节点参数<T>(default, 变量名);
            }

            T 值 = 转换字符串为值<T>(字符串);
            return new 节点参数<T>(值);
        }

        /// <summary>
        /// 将直接值转换为字符串表示
        /// </summary>
        private static string 转换直接值为字符串<T>(T 值)
        {
            if (值 == null) return "";
            if (typeof(T) == typeof(Point))
            {
                Point p = (Point)(object)值;
                return $"{p.X},{p.Y}";
            }
            if (typeof(T) == typeof(Size))
            {
                Size s = (Size)(object)值;
                return $"{s.Width},{s.Height}";
            }
            if (typeof(T) == typeof(OpenCvSharp.Size))
            {
                OpenCvSharp.Size s = (OpenCvSharp.Size)(object)值;
                return $"{s.Width},{s.Height}";
            }
            if (typeof(T) == typeof(Rectangle))
            {
                Rectangle r = (Rectangle)(object)值;
                return $"{r.X},{r.Y},{r.Width},{r.Height}";
            }
            if (typeof(T) == typeof(Color))
            {
                Color c = (Color)(object)值;
                return c.ToArgb().ToString();
            }
            if (typeof(T).IsEnum)
                return 值.ToString();
            return 值.ToString();
        }

        /// <summary>
        /// 将字符串表示转换为对应类型值
        /// </summary>
        private static T 转换字符串为值<T>(string 字符串)
        {
            if (typeof(T) == typeof(string))
                return (T)(object)字符串;
            if (typeof(T) == typeof(int))
                return (T)(object)int.Parse(字符串);
            if (typeof(T) == typeof(double))
                return (T)(object)double.Parse(字符串, CultureInfo.InvariantCulture);
            if (typeof(T) == typeof(float))
                return (T)(object)float.Parse(字符串, CultureInfo.InvariantCulture);
            if (typeof(T) == typeof(long))
                return (T)(object)long.Parse(字符串);
            if (typeof(T) == typeof(bool))
                return (T)(object)bool.Parse(字符串);
            if (typeof(T) == typeof(Point))
            {
                string[] parts = 字符串.Split(',');
                if (parts.Length == 2)
                    return (T)(object)new Point(int.Parse(parts[0]), int.Parse(parts[1]));
                return default;
            }
            if (typeof(T) == typeof(Size))
            {
                string[] parts = 字符串.Split(',');
                if (parts.Length == 2)
                    return (T)(object)new Size(int.Parse(parts[0]), int.Parse(parts[1]));
                return default;
            }
            if (typeof(T) == typeof(OpenCvSharp.Size))
            {
                string[] parts = 字符串.Split(',');
                if (parts.Length == 2)
                    return (T)(object)new OpenCvSharp.Size(int.Parse(parts[0]), int.Parse(parts[1]));
                return default;
            }
            if (typeof(T) == typeof(Rectangle))
            {
                string[] parts = 字符串.Split(',');
                if (parts.Length == 4)
                    return (T)(object)new Rectangle(int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2]), int.Parse(parts[3]));
                return default;
            }
            if (typeof(T) == typeof(Color))
                return (T)(object)Color.FromArgb(int.Parse(字符串));
            if (typeof(T).IsEnum)
                return (T)Enum.Parse(typeof(T), 字符串);
            // 兜底使用 Convert
            return (T)Convert.ChangeType(字符串, typeof(T));
        }
    }
}
