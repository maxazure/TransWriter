namespace TransWriter
{
    partial class SettingsForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            txtApiKey = new TextBox();
            btnSave = new Button();
            label1 = new Label();
            SuspendLayout();
            // 
            // txtApiKey
            // 
            txtApiKey.Location = new Point(113, 55);
            txtApiKey.Name = "txtApiKey";
            txtApiKey.Size = new Size(614, 30);
            txtApiKey.TabIndex = 0;
            // 
            // btnSave
            // 
            btnSave.Location = new Point(615, 289);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(112, 34);
            btnSave.TabIndex = 1;
            btnSave.Text = "保存设置";
            btnSave.UseVisualStyleBackColor = true;
            btnSave.Click += btnSave_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(28, 59);
            label1.Name = "label1";
            label1.Size = new Size(71, 24);
            label1.TabIndex = 2;
            label1.Text = "APIKEY";
            // 
            // SettingsForm
            // 
            AutoScaleDimensions = new SizeF(11F, 24F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(757, 349);
            Controls.Add(label1);
            Controls.Add(btnSave);
            Controls.Add(txtApiKey);
            Name = "SettingsForm";
            Text = "设置";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TextBox txtApiKey;
        private Button btnSave;
        private Label label1;
    }
}