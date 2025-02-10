using System;
using System.Drawing;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;
using Microsoft.Data.Sqlite; // ���Ȱ�װ Microsoft.Data.Sqlite ��

namespace TransWriter
{
    public partial class TransForm : Form
    {
        // ȫ���ȼ���س�����ID
        private const int HOTKEY_ID = 9000; // ��������
        private const int WM_HOTKEY = 0x0312;
        private const int MOD_SHIFT = 0x0004;

        // ����ͼ��
        private NotifyIcon notifyIcon;
        private string apiKey;

        private int ctrlEnterCount = 0;
        private DateTime lastCtrlEnterTime;

        public TransForm()
        {
            InitializeComponent();

            // ��ʼ������ͼ��
            SetupTrayIcon();

            // ע��ȫ���ȼ���Shift + �ո�
            RegisterHotKey(this.Handle, HOTKEY_ID, MOD_SHIFT, (int)Keys.Space);

            // �����¼�
            this.Load += TransForm_Load;
            this.Resize += TransForm_Resize;
            this.FormClosing += TransForm_FormClosing;

            // �ı������ָı�ʱ����Ӧ�߶�
            this.OriginalText.Multiline = true;
  
            this.OriginalText.TextChanged += TextBox_TextChanged;
        


            // ���� API Key
            LoadApiKey();

            // ���ó�ʼռλ���ı�
            SetPlaceholderText();
        }
        private void SetPlaceholderText()
        {
            labPlaceHolder.Visible = true;
        }

        #region API���ú����ݿ��¼

        private async void btnTranslate_Click(object sender, EventArgs e)
        {
            string originalText = OriginalText.Text.Trim();
            if (string.IsNullOrEmpty(originalText))
            {
                MessageBox.Show("��������Ҫ����������ı���");
                return;
            }

            // �������ڷ������ʾ�ʣ����磺�뽫�������ķ����Ӣ�ģ�����
            string prompt = $@"����һλ�������������Ļ��ᣬ�ܹ�׼ȷ�ؽ����ĺ�Ӣ�Ļ��෭�롣
���������¹���
0 ��Ҫ�ش����⣬ֻ�����ı���
2 ���ԭ�������ģ�������Ӣ�ģ���֮��Ȼ��
1 ר�����ʱ���ԭ�Ρ�
2 ʹ����ͨ���׶�������ĸ���ߵı��ϰ�ߣ�Ҫ������ò��ֻ������ġ�
3 ����������������Ȼ�����ģ���Ҫ����ѡ��
```OriginalText
{originalText}
```";
            prompt = prompt.Replace(Environment.NewLine, "\n\n");

            // ���� API ����� JSON ����
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

                    // ���� JSON ��Ӧ
                    using (JsonDocument doc = JsonDocument.Parse(responseContent))
                    {
                        var root = doc.RootElement;
                        var choices = root.GetProperty("choices");
                        if (choices.GetArrayLength() > 0)
                        {
                            var message = choices[0].GetProperty("message");
                            string translatedText = message.GetProperty("content").GetString();

                            translatedText = translatedText.Replace("\n", Environment.NewLine);
                            // ����������ʾ�� EnglishText �ı�����
                            OriginalText.Text = translatedText;
							Clipboard.SetText(translatedText);

							// ����ԭ�ĺ����ĵ��������ݿ�
							SaveTranslationToDatabase(originalText, translatedText);
                        }
                        else
                        {
                            MessageBox.Show("API ���ؽ����Ч��");
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("���÷��� API ����" + ex.Message);
                }
            }
        }

        /// <summary>
        /// ʹ�� SQLite �������¼���浽�������ݿ�
        /// </summary>
        private void SaveTranslationToDatabase(string originalText, string translatedText)
        {
            // ���ݿ��ļ�·���������Ŀ¼�� translations.db��
            string connectionString = "Data Source=translations.db";

            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                // �������ݱ���������ڣ�
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

                // �����¼
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

        #region ���弰�ؼ��ߴ��λ�õ���

        // �������ʱ�������Ͻ�λ������Ϊ������ڵ�λ�ã�����ֹ���峬����Ļ��Ե
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

        // ����ߴ�仯ʱ�������ı���Ŀ��
        private void TransForm_Resize(object sender, EventArgs e)
        {
            AdjustTextBoxWidths();
        }

        /// <summary>
        /// ���������ı���Ŀ��Ϊ�������ڿ�� - ��ť��� - 2 ����
        /// ͬʱ������ť��λ���� EnglishText �ı�����Ҳ�
        /// </summary>
        private void AdjustTextBoxWidths()
        {
        }

        /// <summary>
        /// ���ı������ָı�ʱ���Զ������߶�����Ӧ����
        /// </summary>
        private void TextBox_TextChanged(object sender, EventArgs e)
        {
            if (sender is TextBox tb)
            {
                AdjustTextBoxHeight(tb);
            }
            // �������ı���߶Ⱥ󣬼���Ƿ���Ҫ��������߶�
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
        /// �����ı����ݼ�������߶ȣ��������ı���߶ȣ�������� TextRenderer.MeasureText ���м򵥲�����
        /// </summary>
        private void AdjustTextBoxHeight(TextBox tb)
        {
            // �������߶ȣ������������ߣ��ɸ�����Ҫ������
            int maxHeight = 300;
            Size proposedSize = new Size(tb.Width, int.MaxValue);
            Size textSize = TextRenderer.MeasureText(tb.Text, tb.Font, proposedSize, TextFormatFlags.WordBreak);
            int newHeight = textSize.Height + 10; // �����ʵ����ڱ߾�

			// ����� OriginalText����ȷ���߶�����Ϊ 120
			if (tb == OriginalText)
			{
				newHeight = Math.Max(newHeight, 120);
			}

			tb.Height = Math.Min(newHeight, maxHeight);
        }


        /// <summary>
        /// ���������ı���ĸ߶Ⱥ������ڴ����е�λ�õ�������߶ȣ�ʹ����ȫ���ɼ�
        /// </summary>
        private void AdjustFormHeightToFitTextBoxes()
        {
        }


        #endregion

        #region ����ͼ���ȫ���ȼ�

        /// <summary>
        /// ��ʼ������ͼ�꣬����ӵ���¼�
        /// </summary>
        private void SetupTrayIcon()
        {
            notifyIcon = new NotifyIcon();
            notifyIcon.Icon = this.Icon;
            notifyIcon.Text = "TransWriter";
            notifyIcon.Visible = true;

            // ���������Ĳ˵�
            ContextMenuStrip contextMenu = new ContextMenuStrip();
            ToolStripMenuItem settingsMenuItem = new ToolStripMenuItem("����");
            settingsMenuItem.Click += SettingsMenuItem_Click;
            contextMenu.Items.Add(settingsMenuItem);

            // ����˳��˵���
            ToolStripMenuItem exitMenuItem = new ToolStripMenuItem("�˳�");
            exitMenuItem.Click += ExitMenuItem_Click;
            contextMenu.Items.Add(exitMenuItem);

            notifyIcon.ContextMenuStrip = contextMenu;
            notifyIcon.MouseClick += NotifyIcon_MouseClick;
        }
        private void ExitMenuItem_Click(object sender, EventArgs e)
        {
            // �˳�����
            Application.Exit();
        }
        private void SettingsMenuItem_Click(object sender, EventArgs e)
        {
            SettingsForm settingsForm = new SettingsForm();
            settingsForm.ShowDialog();
            LoadApiKey(); // ���¼��� API Key
        }

        /// <summary>
        /// ������ͼ�걻���ʱ����ʾ����
        /// </summary>
        private void NotifyIcon_MouseClick(object sender, MouseEventArgs e)
        {
            // ���������ʾ����
            if (e.Button == MouseButtons.Left)
            {
                ShowForm();
            }
 
        }

        /// <summary>
        /// ��ʾ���壨�����̻��ȼ����
        /// </summary>
        private void ShowForm()
        {
			SetWindowsPos();
			this.Show();
            // ���֮ǰ�����ˣ��ָ������ߴ�
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.WindowState = FormWindowState.Normal;
            }
            this.Activate();
            OriginalText.Clear();
            SetPlaceholderText();
        }

		// ����ȫ���ȼ���Ϣ
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

        // ���û�����رհ�ťʱ�����ش���������˳�������ͼ����Ȼ����
        private void TransForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {

				OriginalText.Text = "";

				e.Cancel = true;
                this.Hide();
            }
        }

        // P/Invoke ע��/ע��ȫ���ȼ�
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

                // ���� Config ����������ڣ�
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
            // �ж��Ƿ�ͬʱ���� Ctrl �� Enter
            if (e.Control && e.KeyCode == Keys.Enter)
            {
                // ��ֹ���ı����в��뻻��
                e.SuppressKeyPress = true;

                // ����Ƿ��ڶ�ʱ���������������� Ctrl + Enter
                if ((DateTime.Now - lastCtrlEnterTime).TotalMilliseconds < 500)
                {
                    ctrlEnterCount++;
                }
                else
                {
                    ctrlEnterCount = 1;
                }

                lastCtrlEnterTime = DateTime.Now;

                if (ctrlEnterCount == 2)
                {
                    // ���ü�����
                    ctrlEnterCount = 0;

                    OriginalText.Clear();

                    // �رմ���
                    this.Close();
                    SendKeys.Send("^v");
                }
                else
                {
                    // ִ�а�ť����¼�
                    btnTranslate_Click(sender, e);
                }
            }
            // �ж��Ƿ�ͬʱ���� Ctrl �� Backspace
            else if (e.Shift && e.KeyCode == Keys.Back)
            {
                // ��ֹ���ı�����ɾ���ַ�
                e.SuppressKeyPress = true;

                // �����ݿ��ж�ȡ���һ�� OriginalText �����ص� OriginalText �ı�����
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
                        MessageBox.Show("���ݿ���û���ҵ��κμ�¼��");
                    }
                }
            }
        }
    }
}
