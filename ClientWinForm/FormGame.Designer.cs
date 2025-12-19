namespace ClientWinForm
{
    partial class FormGame
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
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormGame));
            listBoxPlayers = new ListBox();
            labelMyNick = new Label();
            labelTurnWho = new Label();
            pictureBoxDeck = new PictureBox();
            imageListCards = new ImageList(components);
            listViewHand = new ListView();
            listView1 = new ListView();
            listView2 = new ListView();
            listView3 = new ListView();
            nickname1 = new Label();
            nickname2 = new Label();
            nickname3 = new Label();
            ((System.ComponentModel.ISupportInitialize)pictureBoxDeck).BeginInit();
            SuspendLayout();
            // 
            // listBoxPlayers
            // 
            listBoxPlayers.Font = new Font("Segoe UI", 12F);
            listBoxPlayers.FormattingEnabled = true;
            listBoxPlayers.ItemHeight = 28;
            listBoxPlayers.Location = new Point(1262, 585);
            listBoxPlayers.Margin = new Padding(4);
            listBoxPlayers.Name = "listBoxPlayers";
            listBoxPlayers.Size = new Size(234, 256);
            listBoxPlayers.TabIndex = 0;
            // 
            // labelMyNick
            // 
            labelMyNick.AutoSize = true;
            labelMyNick.Location = new Point(13, 731);
            labelMyNick.Margin = new Padding(4, 0, 4, 0);
            labelMyNick.Name = "labelMyNick";
            labelMyNick.Size = new Size(148, 28);
            labelMyNick.TabIndex = 1;
            labelMyNick.Text = "Твой никнейм:";
            // 
            // labelTurnWho
            // 
            labelTurnWho.AutoSize = true;
            labelTurnWho.Location = new Point(12, 782);
            labelTurnWho.Name = "labelTurnWho";
            labelTurnWho.Size = new Size(133, 28);
            labelTurnWho.TabIndex = 2;
            labelTurnWho.Text = "Текущий ход:";
            // 
            // pictureBoxDeck
            // 
            pictureBoxDeck.Image = (Image)resources.GetObject("pictureBoxDeck.Image");
            pictureBoxDeck.Location = new Point(263, 289);
            pictureBoxDeck.Name = "pictureBoxDeck";
            pictureBoxDeck.Size = new Size(159, 222);
            pictureBoxDeck.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBoxDeck.TabIndex = 3;
            pictureBoxDeck.TabStop = false;
            pictureBoxDeck.Click += pictureBoxDeck_Click;
            // 
            // imageListCards
            // 
            imageListCards.ColorDepth = ColorDepth.Depth32Bit;
            imageListCards.ImageSize = new Size(159, 222);
            imageListCards.TransparentColor = Color.Transparent;
            // 
            // listViewHand
            // 
            listViewHand.LargeImageList = imageListCards;
            listViewHand.Location = new Point(263, 653);
            listViewHand.MultiSelect = false;
            listViewHand.Name = "listViewHand";
            listViewHand.Size = new Size(759, 217);
            listViewHand.TabIndex = 4;
            listViewHand.UseCompatibleStateImageBehavior = false;
            // 
            // listView1
            // 
            listView1.Location = new Point(35, 12);
            listView1.Name = "listView1";
            listView1.Size = new Size(387, 170);
            listView1.TabIndex = 5;
            listView1.UseCompatibleStateImageBehavior = false;
            // 
            // listView2
            // 
            listView2.Location = new Point(550, 12);
            listView2.Name = "listView2";
            listView2.Size = new Size(387, 170);
            listView2.TabIndex = 6;
            listView2.UseCompatibleStateImageBehavior = false;
            // 
            // listView3
            // 
            listView3.Location = new Point(1077, 12);
            listView3.Name = "listView3";
            listView3.Size = new Size(387, 170);
            listView3.TabIndex = 7;
            listView3.UseCompatibleStateImageBehavior = false;
            // 
            // nickname1
            // 
            nickname1.AutoSize = true;
            nickname1.Location = new Point(179, 185);
            nickname1.Name = "nickname1";
            nickname1.Size = new Size(120, 28);
            nickname1.TabIndex = 8;
            nickname1.Text = "Имя игрока";
            // 
            // nickname2
            // 
            nickname2.AutoSize = true;
            nickname2.Location = new Point(700, 185);
            nickname2.Name = "nickname2";
            nickname2.Size = new Size(120, 28);
            nickname2.TabIndex = 9;
            nickname2.Text = "Имя игрока";
            // 
            // nickname3
            // 
            nickname3.AutoSize = true;
            nickname3.Location = new Point(1229, 185);
            nickname3.Name = "nickname3";
            nickname3.Size = new Size(120, 28);
            nickname3.TabIndex = 10;
            nickname3.Text = "Имя игрока";
            // 
            // FormGame
            // 
            AutoScaleDimensions = new SizeF(11F, 28F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1514, 882);
            Controls.Add(nickname3);
            Controls.Add(nickname2);
            Controls.Add(nickname1);
            Controls.Add(listView3);
            Controls.Add(listView2);
            Controls.Add(listView1);
            Controls.Add(listViewHand);
            Controls.Add(pictureBoxDeck);
            Controls.Add(labelTurnWho);
            Controls.Add(labelMyNick);
            Controls.Add(listBoxPlayers);
            Font = new Font("Segoe UI", 12F);
            Margin = new Padding(4);
            Name = "FormGame";
            Text = "Form1";
            ((System.ComponentModel.ISupportInitialize)pictureBoxDeck).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ListBox listBoxPlayers;
        private Label labelMyNick;
        private Label labelTurnWho;
        private PictureBox pictureBoxDeck;
        private ImageList imageListCards;
        private ListView listViewHand;
        private ListView listView1;
        private ListView listView2;
        private ListView listView3;
        private Label nickname1;
        private Label nickname2;
        private Label nickname3;
    }
}
