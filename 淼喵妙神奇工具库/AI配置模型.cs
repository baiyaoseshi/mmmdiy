using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace 淼喵妙神奇工具库
{
    public class AIConfigData
    {
        public string 提供者类型 { get; set; } = "";
        public string Ollama地址 { get; set; } = "http://localhost:11434";
        public string Ollama模型 { get; set; } = "qwen2:0.5b";
        public string 远程API地址 { get; set; } = "";
        public string 加密API密钥 { get; set; } = "";
        public string 远程模型 { get; set; } = "";
        public string 加密GoogleAPI密钥 { get; set; } = "";
        public string Google搜索引擎ID { get; set; } = "";
        public int 最大输出Token { get; set; } = 8192;
        public float? 温度 { get; set; } = null;
    }

    public class AINamedConfig
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string 名称 { get; set; } = "默认配置";
        public AIConfigData 配置 { get; set; } = new AIConfigData();
    }

    public class AIChatMessage : INotifyPropertyChanged
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string 角色 { get; set; } = "用户";
        public DateTime 时间戳 { get; set; } = DateTime.Now;

        private string _内容 = "";
        public string 内容
        {
            get => _内容;
            set { if (_内容 != value) { _内容 = value; OnPropertyChanged(); } }
        }

        private string _思考内容 = "";
        public string 思考内容
        {
            get => _思考内容;
            set { if (_思考内容 != value) { _思考内容 = value; OnPropertyChanged(); } }
        }

        private bool _思考已折叠 = false;
        public bool 思考已折叠
        {
            get => _思考已折叠;
            set { if (_思考已折叠 != value) { _思考已折叠 = value; OnPropertyChanged(); } }
        }

        private bool _是否私有 = false;
        public bool 是否私有
        {
            get => _是否私有;
            set { if (_是否私有 != value) { _是否私有 = value; OnPropertyChanged(); } }
        }

        public List<string> 图片列表 { get; set; } = new List<string>();

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class AIQuickCommand
    {
        public string 名称 { get; set; } = "";
        public string 触发短语 { get; set; } = "";
        public string 完整Prompt { get; set; } = "";
    }

    public class AIConversation
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string 名称 { get; set; } = "新对话";
        public string 文本AI配置Id { get; set; }
        public string 多模态AI配置Id { get; set; }
        public AIConfigData AI配置 { get; set; } = new AIConfigData();
        public List<string> 工具库分类列表 { get; set; } = new List<string>();
        public string 自定义规则 { get; set; } = "";
        public List<AIQuickCommand> 快捷指令列表 { get; set; } = new List<AIQuickCommand>();
        public List<AIChatMessage> 消息列表 { get; set; } = new List<AIChatMessage>();
        public int? 最大输出Token { get; set; } = null;
        public float? 温度 { get; set; } = null;
        public bool 启用网页搜索 { get; set; } = true;
    }

    public class AIPersistenceData
    {
        public AIConfigData 全局AI配置 { get; set; } = new AIConfigData();
        public List<AINamedConfig> AI配置列表 { get; set; } = new List<AINamedConfig>();
        public string 上次文本配置Id { get; set; }
        public string 上次多模态配置Id { get; set; }
        public string 全局自定义规则 { get; set; } = "始终用中文回复";
        public List<AIQuickCommand> 全局快捷指令列表 { get; set; } = new List<AIQuickCommand>();
        public List<AIConversation> 对话列表 { get; set; } = new List<AIConversation>();
        public string 视觉AI配置Id { get; set; }
        public string 评审AI配置Id { get; set; }
        public bool 启用自主学习 { get; set; } = false;
        public bool 启用增量记录 { get; set; } = false;
        public bool 过滤私有消息 { get; set; } = true;
    }
}
