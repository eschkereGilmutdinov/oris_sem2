namespace ClientWinForm
{
    partial class FormRegister
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
            label1 = new Label();
            textBoxNick = new TextBox();
            label2 = new Label();
            textBoxEmail = new TextBox();
            buttonConnect = new Button();
            labelLobbyStatus = new Label();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 12F);
            label1.Location = new Point(278, 102);
            label1.Name = "label1";
            label1.Size = new Size(176, 28);
            label1.TabIndex = 0;
            label1.Text = "Введите никнейм:";
            // 
            // textBoxNick
            // 
            textBoxNick.Font = new Font("Segoe UI", 12F);
            textBoxNick.Location = new Point(278, 143);
            textBoxNick.Name = "textBoxNick";
            textBoxNick.Size = new Size(176, 34);
            textBoxNick.TabIndex = 1;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI", 12F);
            label2.Location = new Point(278, 180);
            label2.Name = "label2";
            label2.Size = new Size(147, 28);
            label2.TabIndex = 2;
            label2.Text = "Введите почту:";
            // 
            // textBoxEmail
            // 
            textBoxEmail.Font = new Font("Segoe UI", 12F);
            textBoxEmail.Location = new Point(278, 216);
            textBoxEmail.Name = "textBoxEmail";
            textBoxEmail.Size = new Size(176, 34);
            textBoxEmail.TabIndex = 3;
            // 
            // buttonConnect
            // 
            buttonConnect.Font = new Font("Segoe UI", 12F);
            buttonConnect.Location = new Point(278, 256);
            buttonConnect.Name = "buttonConnect";
            buttonConnect.Size = new Size(176, 38);
            buttonConnect.TabIndex = 4;
            buttonConnect.Text = "Войти";
            buttonConnect.UseVisualStyleBackColor = true;
            buttonConnect.Click += buttonConnect_Click;
            // 
            // labelLobbyStatus
            // 
            labelLobbyStatus.AutoSize = true;
            labelLobbyStatus.Font = new Font("Segoe UI", 12F);
            labelLobbyStatus.Location = new Point(244, 310);
            labelLobbyStatus.Name = "labelLobbyStatus";
            labelLobbyStatus.Size = new Size(247, 28);
            labelLobbyStatus.TabIndex = 5;
            labelLobbyStatus.Text = "Подключение к серверу...";
            // 
            // FormRegister
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(labelLobbyStatus);
            Controls.Add(buttonConnect);
            Controls.Add(textBoxEmail);
            Controls.Add(label2);
            Controls.Add(textBoxNick);
            Controls.Add(label1);
            Name = "FormRegister";
            Text = "Form1";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private TextBox textBoxNick;
        private Label label2;
        private TextBox textBoxEmail;
        private Button buttonConnect;
        private Label labelLobbyStatus;
    }
}