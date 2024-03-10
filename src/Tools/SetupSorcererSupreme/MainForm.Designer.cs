namespace SetupSorcererSupreme
{
    partial class MainForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            nextButton = new Button();
            headerLabel = new Label();
            bodyLabel = new Label();
            panel1 = new Panel();
            folderBrowseTextBox = new TextBox();
            folderBrowseButton = new Button();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // nextButton
            // 
            nextButton.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point);
            nextButton.Location = new Point(379, 280);
            nextButton.Name = "nextButton";
            nextButton.Size = new Size(90, 29);
            nextButton.TabIndex = 0;
            nextButton.Text = "Next";
            nextButton.UseVisualStyleBackColor = true;
            nextButton.Click += nextButton_Click;
            // 
            // headerLabel
            // 
            headerLabel.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point);
            headerLabel.Location = new Point(12, 6);
            headerLabel.MaximumSize = new Size(460, 50);
            headerLabel.Name = "headerLabel";
            headerLabel.Size = new Size(460, 50);
            headerLabel.TabIndex = 1;
            headerLabel.Text = "Header Label";
            headerLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // bodyLabel
            // 
            bodyLabel.AutoSize = true;
            bodyLabel.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point);
            bodyLabel.Location = new Point(12, 76);
            bodyLabel.MaximumSize = new Size(460, 0);
            bodyLabel.Name = "bodyLabel";
            bodyLabel.Size = new Size(86, 21);
            bodyLabel.TabIndex = 2;
            bodyLabel.Text = "Body Label";
            // 
            // panel1
            // 
            panel1.BackColor = SystemColors.Control;
            panel1.Controls.Add(headerLabel);
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(480, 64);
            panel1.TabIndex = 3;
            // 
            // folderBrowseTextBox
            // 
            folderBrowseTextBox.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point);
            folderBrowseTextBox.Location = new Point(12, 141);
            folderBrowseTextBox.Name = "folderBrowseTextBox";
            folderBrowseTextBox.Size = new Size(361, 29);
            folderBrowseTextBox.TabIndex = 4;
            folderBrowseTextBox.Text = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Marvel Heroes";
            folderBrowseTextBox.Visible = false;
            // 
            // folderBrowseButton
            // 
            folderBrowseButton.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point);
            folderBrowseButton.Location = new Point(379, 141);
            folderBrowseButton.Name = "folderBrowseButton";
            folderBrowseButton.Size = new Size(90, 29);
            folderBrowseButton.TabIndex = 5;
            folderBrowseButton.Text = "Browse...";
            folderBrowseButton.UseVisualStyleBackColor = true;
            folderBrowseButton.Visible = false;
            folderBrowseButton.Click += folderBrowseButton_Click;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.ControlLight;
            ClientSize = new Size(481, 321);
            Controls.Add(folderBrowseButton);
            Controls.Add(folderBrowseTextBox);
            Controls.Add(panel1);
            Controls.Add(bodyLabel);
            Controls.Add(nextButton);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "MHServerEmu Setup Sorcerer Supreme";
            panel1.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button nextButton;
        private Label headerLabel;
        private Label bodyLabel;
        private Panel panel1;
        private TextBox folderBrowseTextBox;
        private Button folderBrowseButton;
    }
}