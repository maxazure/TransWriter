using System;
using System.Drawing;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;
using Microsoft.Data.Sqlite; // 请先安装 Microsoft.Data.Sqlite 包

namespace TransWriter
{
    public partial class TransForm : Form
    {
        // 全局热键相关常量与ID
        private const int HOTKEY_ID = 9000; // 任意数字
        private const int WM_HOTKEY = 0x0312;
        private const int MOD_SHIFT = 0x0004;

        // 托盘图标
        private NotifyIcon notifyIcon;
        private string apiKey;

        private int ctrlEnterCount = 0;
        private DateTime lastCtrlEnterTime;
        private string OldOriginalText = "";

        public TransForm()
        {
            InitializeComponent();

            // 初始化托盘图标
            SetupTrayIcon();

            // 注册全局热键：Shift + 空格
            RegisterHotKey(this.Handle, HOTKEY_ID, MOD_SHIFT, (int)Keys.Space);

            // 窗体事件
            this.Load += TransForm_Load;
            this.Resize += TransForm_Resize;
            this.FormClosing += TransForm_FormClosing;

            // 文本框文字改变时自适应高度
            this.OriginalText.Multiline = true;
  
            this.OriginalText.TextChanged += TextBox_TextChanged;
        


            // 加载 API Key
            LoadApiKey();

            // 设置初始占位符文本
            SetPlaceholderText();
        }
        private void SetPlaceholderText()
        {
            labPlaceHolder.Visible = true;
        }

        #region API调用和数据库记录

        private async void btnTranslate_Click(object sender, EventArgs e)
        {
            string originalText = OriginalText.Text.Trim();
            if (string.IsNullOrEmpty(originalText))
            {
                MessageBox.Show("请输入需要翻译的中文文本。");
                return;
            }

            // 构造用于翻译的提示词，例如：请将以下中文翻译成英文：……
            string prompt = $@"你是一位生活在新西兰的华裔，能够准确地将中文和英文互相翻译。
请遵守以下规则：
0 不要回答问题，只翻译文本。
2 如果原文是中文，译文是英文，反之亦然。
1 专有名词保留原形。
2 使译文通俗易懂，符合母语者的表达习惯，要保持礼貌，只输出译文。
3 仅仅输出你觉得最自然的译文，不要让我选择。
```OriginalText
{originalText}
```";
            prompt = prompt.Replace(Environment.NewLine, "\n\n");

            // 构造 API 请求的 JSON 数据
            var requestBody = new
            {
                model = "llama-3.3-70b-versatile",
                temperature = 1,
                messages = new[]
                {
                    new { role = "user", content = prompt }
                }
            };

            string jsonPayload = JsonSerializer.Serialize(requestBody);

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                try
                {
                    var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                    var response = await client.PostAsync("https://api.groq.com/openai/v1/chat/completions?translation", content);
                    response.EnsureSuccessStatusCode();

                    string responseContent = await response.Content.ReadAsStringAsync();

                    // 解析 JSON 响应
                    using (JsonDocument doc = JsonDocument.Parse(responseContent))
                    {
                        var root = doc.RootElement;
                        var choices = root.GetProperty("choices");
                        if (choices.GetArrayLength() > 0)
                        {
                            var message = choices[0].GetProperty("message");
                            string translatedText = message.GetProperty("content").GetString();

                            translatedText = translatedText.Replace("\n", Environment.NewLine);
                            // 将翻译结果显示到 EnglishText 文本框中
                            OriginalText.Text = translatedText;
							Clipboard.SetText(translatedText);

							// 保存原文和译文到本地数据库
							SaveTranslationToDatabase(originalText, translatedText);
                            OldOriginalText = translatedText;
                           
                        }
                        else
                        {
                            MessageBox.Show("API 返回结果无效。");
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("调用翻译 API 出错：" + ex.Message);
                }
            }
        }

        /// <summary>
        /// 使用 SQLite 将翻译记录保存到本地数据库
        /// </summary>
        private void SaveTranslationToDatabase(string originalText, string translatedText)
        {
            // 数据库文件路径（程序根目录下 translations.db）
            string connectionString = "Data Source=translations.db";

            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                // 创建数据表（如果不存在）
                string createTableCmd = @"CREATE TABLE IF NOT EXISTS Translations (
                                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                            OriginalText TEXT,
                                            TranslatedText TEXT,
                                            CreatedAt TEXT
                                          );";
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = createTableCmd;
                    command.ExecuteNonQuery();
                }

                // 插入记录
                string insertCmd = "INSERT INTO Translations (OriginalText, TranslatedText, CreatedAt) VALUES (@orig, @trans, @createdAt)";
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = insertCmd;
                    command.Parameters.AddWithValue("@orig", originalText);
                    command.Parameters.AddWithValue("@trans", translatedText);
                    command.Parameters.AddWithValue("@createdAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    command.ExecuteNonQuery();
                }
            }
        }

        #endregion

        #region 窗体及控件尺寸和位置调整

        // 窗体加载时：将左上角位置设置为鼠标所在的位置，并防止窗体超出屏幕边缘
        private void TransForm_Load(object sender, EventArgs e)
        {
            SetWindowsPos();

		}

        private void SetWindowsPos()
        {
			Point mousePos = Cursor.Position;
			Rectangle screenBounds = Screen.FromPoint(mousePos).WorkingArea;
			int newX = mousePos.X;
			int newY = mousePos.Y;
			if (newX + this.Width > screenBounds.Right)
			{
				newX = screenBounds.Right - this.Width;
			}
			if (newY + this.Height > screenBounds.Bottom)
			{
				newY = screenBounds.Bottom - this.Height;
			}
			this.Location = new Point(newX, newY);

		}

        // 窗体尺寸变化时，调整文本框的宽度
        private void TransForm_Resize(object sender, EventArgs e)
        {
            AdjustTextBoxWidths();
        }

        /// <summary>
        /// 设置两个文本框的宽度为：窗体内宽度 - 按钮宽度 - 2 像素
        /// 同时调整按钮的位置在 EnglishText 文本框的右侧
        /// </summary>
        private void AdjustTextBoxWidths()
        {
        }

        /// <summary>
        /// 当文本框文字改变时，自动调整高度以适应内容
        /// </summary>
        private void TextBox_TextChanged(object sender, EventArgs e)
        {
            if (sender is TextBox tb)
            {
                AdjustTextBoxHeight(tb);
            }
            // 调整完文本框高度后，检查是否需要调整窗体高度
            AdjustFormHeightToFitTextBoxes();

            if (OriginalText.Text == "")
            {
                SetPlaceholderText();
            }
            else
            {
                labPlaceHolder.Visible = false;
            }
        }

        /// <summary>
        /// 根据文本内容计算所需高度，并调整文本框高度（这里采用 TextRenderer.MeasureText 进行简单测量）
        /// </summary>
        private void AdjustTextBoxHeight(TextBox tb)
        {
            // 限制最大高度，避免无限增高（可根据需要调整）
            int maxHeight = 300;
            Size proposedSize = new Size(tb.Width, int.MaxValue);
            Size textSize = TextRenderer.MeasureText(tb.Text, tb.Font, proposedSize, TextFormatFlags.WordBreak);
            int newHeight = textSize.Height + 10; // 加上适当的内边距

			// 如果是 OriginalText，则确保高度至少为 120
			if (tb == OriginalText)
			{
				newHeight = Math.Max(newHeight, 120);
			}

			tb.Height = Math.Min(newHeight, maxHeight);
        }


        /// <summary>
        /// 根据两个文本框的高度和它们在窗体中的位置调整窗体高度，使内容全部可见
        /// </summary>
        private void AdjustFormHeightToFitTextBoxes()
        {
        }


        #endregion

        #region 托盘图标和全局热键

        /// <summary>
        /// 初始化托盘图标，并添加点击事件
        /// </summary>
        private void SetupTrayIcon()
        {
            notifyIcon = new NotifyIcon();
            notifyIcon.Icon = this.Icon;
            notifyIcon.Text = "TransWriter";
            notifyIcon.Visible = true;

            // 创建上下文菜单
            ContextMenuStrip contextMenu = new ContextMenuStrip();
            ToolStripMenuItem settingsMenuItem = new ToolStripMenuItem("设置");
            settingsMenuItem.Click += SettingsMenuItem_Click;
            contextMenu.Items.Add(settingsMenuItem);

            // 添加退出菜单项
            ToolStripMenuItem exitMenuItem = new ToolStripMenuItem("退出");
            exitMenuItem.Click += ExitMenuItem_Click;
            contextMenu.Items.Add(exitMenuItem);

            notifyIcon.ContextMenuStrip = contextMenu;
            notifyIcon.MouseClick += NotifyIcon_MouseClick;
        }
        private void ExitMenuItem_Click(object sender, EventArgs e)
        {
            // 退出程序
            Application.Exit();
        }
        private void SettingsMenuItem_Click(object sender, EventArgs e)
        {
            SettingsForm settingsForm = new SettingsForm();
            settingsForm.ShowDialog();
            LoadApiKey(); // 重新加载 API Key
        }

        /// <summary>
        /// 当托盘图标被点击时，显示窗体
        /// </summary>
        private void NotifyIcon_MouseClick(object sender, MouseEventArgs e)
        {
            // 左键单击显示窗体
            if (e.Button == MouseButtons.Left)
            {
                ShowForm();
            }
 
        }

        /// <summary>
        /// 显示窗体（从托盘或热键激活）
        /// </summary>
        private void ShowForm()
        {
			SetWindowsPos();
			this.Show();
            // 如果之前隐藏了，恢复正常尺寸
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.WindowState = FormWindowState.Normal;
            }
            this.Activate();
            OriginalText.Clear();
            SetPlaceholderText();
        }

		// 捕获全局热键消息
		protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY)
            {
                int id = m.WParam.ToInt32();
                if (id == HOTKEY_ID)
                {
                    ShowForm();
                }
            }
            base.WndProc(ref m);
        }

        // 当用户点击关闭按钮时，隐藏窗体而不是退出，托盘图标仍然存在
        private void TransForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {

				OriginalText.Text = "";

				e.Cancel = true;
                this.Hide();
            }
        }

        // P/Invoke 注册/注销全局热键
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);


        private void CheckAndCreateDatabase()
        {
            string connectionString = "Data Source=translations.db";
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                // 创建 Config 表（如果不存在）
                string createTableCmd = @"CREATE TABLE IF NOT EXISTS Config (
                                    Key TEXT PRIMARY KEY,
                                    Value TEXT
                                  );";
                using (var command = new SqliteCommand(createTableCmd, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        private void LoadApiKey()
        {
            CheckAndCreateDatabase();
            string connectionString = "Data Source=translations.db";
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                string selectCmd = "SELECT Value FROM Config WHERE Key = 'ApiKey'";
                using (var command = new SqliteCommand(selectCmd, connection))
                {
                    var result = command.ExecuteScalar();
                    if (result != null)
                    {
                        apiKey = result.ToString();
                    }
                }
            }
        }

        #endregion

        private void OriginalText_KeyDown(object sender, KeyEventArgs e)
        {
            // 判断是否同时按下 Ctrl 和 Enter
            if (e.Control && e.KeyCode == Keys.Enter)
            {
                // 防止在文本框中插入换行
                e.SuppressKeyPress = true;
                ctrlEnterCount++;
                // 检查是否按下两次 Ctrl + Enter

                lastCtrlEnterTime = DateTime.Now;

                if (ctrlEnterCount > 1 && OriginalText.Text == OldOriginalText)
                {
                    // 重置计数器
                    ctrlEnterCount = 0;

                    // 关闭窗口
                    this.Close();
                    SendKeys.Send("^v");
                }
                else
                {
                    // 执行按钮点击事件
                    btnTranslate_Click(sender, e);
                }
             
            }
            // 判断是否同时按下 Ctrl 和 Backspace
            else if (e.Shift && e.KeyCode == Keys.Back)
            {
                // 防止在文本框中删除字符
                e.SuppressKeyPress = true;

                // 从数据库中读取最后一条 OriginalText 并加载到 OriginalText 文本框中
                LoadLastOriginalText();
            }
        }

        private void LoadLastOriginalText()
        {
            string connectionString = "Data Source=translations.db";
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                string selectCmd = "SELECT OriginalText FROM Translations ORDER BY Id DESC LIMIT 1";
                using (var command = new SqliteCommand(selectCmd, connection))
                {
                    var result = command.ExecuteScalar();
                    if (result != null)
                    {
                        OriginalText.Text = result.ToString();
                    }
                    else
                    {
                        MessageBox.Show("数据库中没有找到任何记录。");
                    }
                }
            }
        }
    }
}
