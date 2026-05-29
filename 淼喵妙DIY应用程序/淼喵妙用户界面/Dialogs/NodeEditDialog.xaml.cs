using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using 淼喵妙神奇工具库.键鼠库.动作;
using 淼喵妙神奇工具库.键鼠库.动作.流程节点;

namespace 淼喵妙用户界面.Dialogs
{
    public class NodeJumpOption
    {
        public string DisplayName { get; set; }
        public 控制节点 Node { get; set; }

        public override string ToString() => DisplayName;
    }

    public partial class NodeEditDialog : Window, INotifyPropertyChanged
    {
        private 控制节点 _node;
        public 控制节点 Node
        {
            get => _node;
            set
            {
                _node = value;
                OnPropertyChanged(nameof(Node));
                OnPropertyChanged(nameof(NodeTypeName));
            }
        }

        public string NodeTypeName => Node?.GetType().Name ?? string.Empty;

        public List<NodeJumpOption> JumpOptions { get; } = new List<NodeJumpOption>();

        private NodeJumpOption _selectedSuccessJump;
        public NodeJumpOption SelectedSuccessJump
        {
            get => _selectedSuccessJump;
            set
            {
                _selectedSuccessJump = value;
                OnPropertyChanged(nameof(SelectedSuccessJump));
            }
        }

        private NodeJumpOption _selectedFailureJump;
        public NodeJumpOption SelectedFailureJump
        {
            get => _selectedFailureJump;
            set
            {
                _selectedFailureJump = value;
                OnPropertyChanged(nameof(SelectedFailureJump));
            }
        }

        private Dictionary<string, Control> _propertyControls = new Dictionary<string, Control>();
        private Dictionary<string, CheckBox> _cloudToggleChecks = new Dictionary<string, CheckBox>();
        private IntPtr _hWnd;

        public NodeEditDialog(控制节点 node, IEnumerable<控制节点> allNodes, IntPtr hWnd = default)
        {
            InitializeComponent();
            Node = node;
            _hWnd = hWnd;
            DataContext = this;

            JumpOptions.Add(new NodeJumpOption { DisplayName = "顺序执行下一节点", Node = null });
            foreach (var n in allNodes)
            {
                JumpOptions.Add(new NodeJumpOption { DisplayName = n.节点名字, Node = n });
            }

            SelectedSuccessJump = JumpOptions.FirstOrDefault(o => o.Node == node.成功后跳转) ?? JumpOptions[0];
            SelectedFailureJump = JumpOptions.FirstOrDefault(o => o.Node == node.失败后跳转) ?? JumpOptions[0];

            LoadCustomProperties();
            RefreshConditionsList();
        }

        private void LoadCustomProperties()
        {
            if (Node == null) return;

            var type = Node.GetType();
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);

            foreach (var field in fields)
            {
                if (IsStandardField(field.Name)) continue;

                var rowPanel = new StackPanel();
                rowPanel.Orientation = Orientation.Horizontal;
                rowPanel.Margin = new Thickness(0, 5, 0, 0);

                var label = new TextBlock();
                label.Text = field.Name + ":";
                label.VerticalAlignment = VerticalAlignment.Center;
                label.Width = 100;
                label.Margin = new Thickness(0, 0, 5, 0);

                if (field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof(节点参数<>))
                {
                    var nodeParamValue = field.GetValue(Node);
                    bool 使用全局变量 = false;
                    string 显示文本 = "";

                    if (nodeParamValue != null)
                    {
                        var genericArg = field.FieldType.GetGenericArguments()[0];
                        var serializeMethod = typeof(节点参数).GetMethod("序列化").MakeGenericMethod(genericArg);
                        显示文本 = (string)serializeMethod.Invoke(null, new[] { nodeParamValue });
                        // 通过反射读取 变量名 字段判断是否为全局变量模式
                        var 变量名字段 = field.FieldType.GetField("变量名");
                        if (变量名字段 != null)
                        {
                            string 变量名值 = 变量名字段.GetValue(nodeParamValue) as string;
                            使用全局变量 = !string.IsNullOrEmpty(变量名值);
                        }
                    }

                    var vertPanel = new StackPanel();
                    vertPanel.Orientation = Orientation.Vertical;
                    vertPanel.Margin = new Thickness(0, 3, 0, 0);

                    // 第一行：Label + CheckBox
                    var headerRow = new StackPanel();
                    headerRow.Orientation = Orientation.Horizontal;

                    var label2 = new TextBlock();
                    label2.Text = field.Name + ":";
                    label2.VerticalAlignment = VerticalAlignment.Center;
                    label2.Width = 100;
                    label2.Margin = new Thickness(0, 0, 5, 0);

                    var chk = new CheckBox();
                    chk.Content = "使用全局变量";
                    chk.IsChecked = 使用全局变量;
                    chk.VerticalAlignment = VerticalAlignment.Center;
                    chk.Margin = new Thickness(0, 0, 0, 3);

                    headerRow.Children.Add(label2);
                    headerRow.Children.Add(chk);
                    vertPanel.Children.Add(headerRow);

                    // 第二行：TextBox
                    var txtBox = new TextBox();
                    txtBox.Width = 300;
                    txtBox.Margin = new Thickness(100, 0, 0, 5);
                    txtBox.Text = 显示文本;
                    var txtBoxRef = txtBox;
                    var chkRef = chk;
                    var paramValRef = nodeParamValue;

                    // 切换 CheckBox 时自动切换文本模式
                    chk.Checked += (s, e2) =>
                    {
                        // 切换到全局变量模式：清空文本，等待用户输入变量名
                        if (paramValRef != null)
                        {
                            var 变量名字段2 = field.FieldType.GetField("变量名");
                            string 当前变量名 = 变量名字段2?.GetValue(paramValRef) as string;
                            txtBoxRef.Text = string.IsNullOrEmpty(当前变量名) ? "" : 当前变量名;
                        }
                        else
                        {
                            txtBoxRef.Text = "";
                        }
                    };
                    chk.Unchecked += (s, e2) =>
                    {
                        // 切换到固定值模式：清空文本，等待用户输入值
                        if (paramValRef != null)
                        {
                            // 把当前文本（可能是变量名）暂存到变量名字段
                            var 变量名字段2 = field.FieldType.GetField("变量名");
                            string 当前文本 = txtBoxRef.Text;
                            if (!string.IsNullOrEmpty(当前文本))
                                变量名字段2?.SetValue(paramValRef, 当前文本);
                        }
                        txtBoxRef.Text = "";
                    };

                    vertPanel.Children.Add(txtBox);
                    _propertyControls[field.Name] = txtBox;
                    _cloudToggleChecks[field.Name] = chk;
                    CustomPropertiesPanel.Children.Add(vertPanel);
                    continue;
                }

                Control inputControl = CreateInputControl(field.FieldType);
                inputControl.Width = 200;
                inputControl.Margin = new Thickness(0, 0, 5, 0);

                if (inputControl is TextBox textBox)
                {
                    if (field.FieldType == typeof(List<string>))
                    {
                        var list = field.GetValue(Node) as List<string>;
                        textBox.Text = list != null ? string.Join(";", list) : "";
                    }
                    else
                    {
                        textBox.Text = field.GetValue(Node)?.ToString() ?? string.Empty;
                    }
                    _propertyControls[field.Name] = textBox;
                }
                else if (inputControl is ComboBox comboBox)
                {
                    var enumValues = Enum.GetValues(field.FieldType);
                    foreach (var value in enumValues)
                    {
                        comboBox.Items.Add(value);
                    }
                    comboBox.SelectedValue = field.GetValue(Node);
                    _propertyControls[field.Name] = comboBox;
                }

                rowPanel.Children.Add(label);
                rowPanel.Children.Add(inputControl);
                CustomPropertiesPanel.Children.Add(rowPanel);
            }

            if (CustomPropertiesPanel.Children.Count == 0)
            {
                var noPropertiesText = new TextBlock();
                noPropertiesText.Text = "该节点类型没有可编辑的特有属性";
                noPropertiesText.Foreground = System.Windows.Media.Brushes.Gray;
                CustomPropertiesPanel.Children.Add(noPropertiesText);
            }
        }

        private bool IsStandardField(string fieldName)
        {
            var standardFields = new[] { "成功后跳转", "失败后跳转", "成功后等待", "失败后等待", "节点备注", "节点名字", "条件列表" };
            return Array.IndexOf(standardFields, fieldName) >= 0;
        }

        private Control CreateInputControl(Type fieldType)
        {
            if (fieldType.IsEnum)
            {
                return new ComboBox();
            }
            else if (fieldType == typeof(int) || fieldType == typeof(double) || 
                     fieldType == typeof(float) || fieldType == typeof(long))
            {
                var textBox = new TextBox();
                textBox.InputScope = new InputScope();
                textBox.InputScope.Names.Add(new InputScopeName(InputScopeNameValue.Number));
                return textBox;
            }
            else
            {
                return new TextBox();
            }
        }

        private void RefreshConditionsList()
        {
            ConditionsListBox.Items.Clear();
            if (Node == null) return;
            for (int i = 0; i < Node.条件列表.Count; i++)
            {
                var cond = Node.条件列表[i];
                string label = cond.Item1 != null
                    ? $"条件{i + 1}: {cond.Item1.GetType().Name} {(cond.Item2 ? "(反转)" : "")}"
                    : $"条件{i + 1}: (空)";
                ConditionsListBox.Items.Add(label);
            }
            if (ConditionsListBox.Items.Count == 0)
            {
                ConditionsListBox.Items.Add("(无运行条件)");
            }
        }

        private void AddCondition_Click(object sender, RoutedEventArgs e)
        {
            if (Node == null) return;
            Node.添加条件(_hWnd);
            RefreshConditionsList();
        }

        private void RemoveCondition_Click(object sender, RoutedEventArgs e)
        {
            if (Node == null) return;
            int idx = ConditionsListBox.SelectedIndex;
            if (idx < 0 || idx >= Node.条件列表.Count) return;
            Node.条件列表.RemoveAt(idx);
            RefreshConditionsList();
        }

        private void SaveCustomProperties()
        {
            if (Node == null) return;

            Node.成功后跳转 = SelectedSuccessJump?.Node;
            Node.失败后跳转 = SelectedFailureJump?.Node;

            var type = Node.GetType();

            foreach (var kvp in _propertyControls)
            {
                var fieldName = kvp.Key;
                var control = kvp.Value;
                var field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);

                if (field == null) continue;

                try
                {
                    if (field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof(节点参数<>))
                    {
                        if (control is TextBox textBox)
                        {
                            bool 使用全局变量 = _cloudToggleChecks.TryGetValue(fieldName, out var chk) && chk.IsChecked == true;
                            var genericArg = field.FieldType.GetGenericArguments()[0];

                            if (使用全局变量)
                            {
                                var nodeParamType = typeof(节点参数<>).MakeGenericType(genericArg);
                                var instance = Activator.CreateInstance(nodeParamType);
                                var 变量名字段 = nodeParamType.GetField("变量名");
                                var 变量名文本 = textBox.Text.TrimStart('$');
                                变量名字段?.SetValue(instance, 变量名文本);
                                field.SetValue(Node, instance);
                            }
                            else
                            {
                                // 固定值模式：用反序列化还原
                                var deserializeMethod = typeof(节点参数).GetMethod("反序列化").MakeGenericMethod(genericArg);
                                var nodeParamValue = deserializeMethod.Invoke(null, new[] { textBox.Text });
                                field.SetValue(Node, nodeParamValue);
                            }
                        }
                    }
                    else if (control is TextBox textBox)
                    {
                        if (field.FieldType == typeof(List<string>))
                        {
                            var list = textBox.Text.Split(';')
                                .Select(s => s.Trim())
                                .Where(s => !string.IsNullOrEmpty(s))
                                .ToList();
                            field.SetValue(Node, list);
                        }
                        else
                        {
                            object value = Convert.ChangeType(textBox.Text, field.FieldType);
                            field.SetValue(Node, value);
                        }
                    }
                    else if (control is ComboBox comboBox)
                    {
                        field.SetValue(Node, comboBox.SelectedValue);
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            SaveCustomProperties();
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}