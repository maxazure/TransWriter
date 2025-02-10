using System;
using System.Windows.Forms;
using Microsoft.Data.Sqlite;

namespace TransWriter
{
    public partial class SettingsForm : Form
    {
        public SettingsForm()
        {
            InitializeComponent();
            LoadApiKey();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            string apiKey = txtApiKey.Text.Trim();
            if (string.IsNullOrEmpty(apiKey))
            {
                MessageBox.Show("API Key 不能为空。");
                return;
            }

            SaveApiKeyToDatabase(apiKey);
            MessageBox.Show("API Key 已保存。");
            this.Close();
        }

        private void LoadApiKey()
        {
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
                        txtApiKey.Text = result.ToString();
                    }
                }
            }
        }

        private void SaveApiKeyToDatabase(string apiKey)
        {
            string connectionString = "Data Source=translations.db";
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                string createTableCmd = @"CREATE TABLE IF NOT EXISTS Config (
                                            Key TEXT PRIMARY KEY,
                                            Value TEXT
                                          );";
                using (var command = new SqliteCommand(createTableCmd, connection))
                {
                    command.ExecuteNonQuery();
                }

                string insertCmd = "INSERT OR REPLACE INTO Config (Key, Value) VALUES ('ApiKey', @value)";
                using (var command = new SqliteCommand(insertCmd, connection))
                {
                    command.Parameters.AddWithValue("@value", apiKey);
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}