namespace TransWriter
{
    partial class TransForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TransForm));
            OriginalText = new TextBox();
            labPlaceHolder = new Label();
            SuspendLayout();
            // 
            // OriginalText
            // 
            OriginalText.Dock = DockStyle.Fill;
            OriginalText.ForeColor = SystemColors.ControlText;
            OriginalText.Location = new Point(0, 0);
            OriginalText.Multiline = true;
            OriginalText.Name = "OriginalText";
            OriginalText.ScrollBars = ScrollBars.Vertical;
            OriginalText.Size = new Size(721, 264);
            OriginalText.TabIndex = 5;
            OriginalText.TextChanged += TextBox_TextChanged;
            OriginalText.KeyDown += OriginalText_KeyDown;
            // 
            // labPlaceHolder
            // 
            labPlaceHolder.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            labPlaceHolder.AutoSize = true;
            labPlaceHolder.BackColor = SystemColors.Window;
            labPlaceHolder.ForeColor = SystemColors.GrayText;
            labPlaceHolder.Location = new Point(213, 71);
            labPlaceHolder.Name = "labPlaceHolder";
            labPlaceHolder.Size = new Size(292, 96);
            labPlaceHolder.TabIndex = 6;
            labPlaceHolder.Text = "按 Ctrl  +回车 快速翻译\r\n按 Shift +退格 查看原文\r\n按 Shift +空格  随时呼出程序\r\n快速 按 Ctrl  +回车 两次  退出程序 ";
            // 
            // TransForm
            // 
            AutoScaleDimensions = new SizeF(11F, 24F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(721, 264);
            Controls.Add(labPlaceHolder);
            Controls.Add(OriginalText);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            Name = "TransForm";
            Opacity = 0.9D;
            Text = "迷你翻译";
            TopMost = true;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private TextBox OriginalText;
        private Label labPlaceHolder;
    }
}
