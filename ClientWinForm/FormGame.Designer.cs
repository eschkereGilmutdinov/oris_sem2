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
            listViewStall = new ListView();
            listView1 = new ListView();
            listView2 = new ListView();
            listView3 = new ListView();
            nickname1 = new Label();
            nickname2 = new Label();
            nickname3 = new Label();
            listViewHand = new ListView();
            buttonPlayCard = new Button();
            pictureBoxBabyDeck = new PictureBox();
            pictureBoxDiscard = new PictureBox();
            buttonDiscard = new Button();
            ((System.ComponentModel.ISupportInitialize)pictureBoxDeck).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBoxBabyDeck).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBoxDiscard).BeginInit();
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
            labelMyNick.Location = new Point(13, 542);
            labelMyNick.Margin = new Padding(4, 0, 4, 0);
            labelMyNick.Name = "labelMyNick";
            labelMyNick.Size = new Size(148, 28);
            labelMyNick.TabIndex = 1;
            labelMyNick.Text = "Твой никнейм:";
            // 
            // labelTurnWho
            // 
            labelTurnWho.AutoSize = true;
            labelTurnWho.Location = new Point(13, 585);
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
            imageListCards.ImageSize = new Size(120, 180);
            imageListCards.TransparentColor = Color.Transparent;
            // 
            // listViewStall
            // 
            listViewStall.LargeImageList = imageListCards;
            listViewStall.Location = new Point(263, 542);
            listViewStall.MultiSelect = false;
            listViewStall.Name = "listViewStall";
            listViewStall.Size = new Size(935, 156);
            listViewStall.TabIndex = 4;
            listViewStall.UseCompatibleStateImageBehavior = false;
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
            // listViewHand
            // 
            listViewHand.LargeImageList = imageListCards;
            listViewHand.Location = new Point(263, 722);
            listViewHand.MultiSelect = false;
            listViewHand.Name = "listViewHand";
            listViewHand.Size = new Size(935, 187);
            listViewHand.TabIndex = 11;
            listViewHand.UseCompatibleStateImageBehavior = false;
            // 
            // buttonPlayCard
            // 
            buttonPlayCard.Location = new Point(28, 722);
            buttonPlayCard.Name = "buttonPlayCard";
            buttonPlayCard.Size = new Size(191, 41);
            buttonPlayCard.TabIndex = 12;
            buttonPlayCard.Text = "Разыграть карту";
            buttonPlayCard.UseVisualStyleBackColor = true;
            buttonPlayCard.Click += buttonPlayCard_Click;
            // 
            // pictureBoxBabyDeck
            // 
            pictureBoxBabyDeck.Image = (Image)resources.GetObject("pictureBoxBabyDeck.Image");
            pictureBoxBabyDeck.Location = new Point(640, 289);
            pictureBoxBabyDeck.Name = "pictureBoxBabyDeck";
            pictureBoxBabyDeck.Size = new Size(159, 222);
            pictureBoxBabyDeck.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBoxBabyDeck.TabIndex = 13;
            pictureBoxBabyDeck.TabStop = false;
            pictureBoxBabyDeck.Click += pictureBoxBabyDeck_Click;
            // 
            // pictureBoxDiscard
            // 
            pictureBoxDiscard.Location = new Point(1039, 289);
            pictureBoxDiscard.Name = "pictureBoxDiscard";
            pictureBoxDiscard.Size = new Size(159, 222);
            pictureBoxDiscard.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBoxDiscard.TabIndex = 14;
            pictureBoxDiscard.TabStop = false;
            // 
            // buttonDiscard
            // 
            buttonDiscard.Location = new Point(28, 784);
            buttonDiscard.Name = "buttonDiscard";
            buttonDiscard.Size = new Size(191, 41);
            buttonDiscard.TabIndex = 15;
            buttonDiscard.Text = "Сбросить карту";
            buttonDiscard.UseVisualStyleBackColor = true;
            buttonDiscard.Click += buttonDiscard_Click;
            // 
            // FormGame
            // 
            AutoScaleDimensions = new SizeF(11F, 28F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1641, 1054);
            Controls.Add(buttonDiscard);
            Controls.Add(pictureBoxDiscard);
            Controls.Add(pictureBoxBabyDeck);
            Controls.Add(buttonPlayCard);
            Controls.Add(listViewHand);
            Controls.Add(nickname3);
            Controls.Add(nickname2);
            Controls.Add(nickname1);
            Controls.Add(listView3);
            Controls.Add(listView2);
            Controls.Add(listView1);
            Controls.Add(listViewStall);
            Controls.Add(pictureBoxDeck);
            Controls.Add(labelTurnWho);
            Controls.Add(labelMyNick);
            Controls.Add(listBoxPlayers);
            Font = new Font("Segoe UI", 12F);
            Margin = new Padding(4);
            Name = "FormGame";
            Text = "Form1";
            ((System.ComponentModel.ISupportInitialize)pictureBoxDeck).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBoxBabyDeck).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBoxDiscard).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ListBox listBoxPlayers;
        private Label labelMyNick;
        private Label labelTurnWho;
        private PictureBox pictureBoxDeck;
        private ImageList imageListCards;
        private ListView listViewStall;
        private ListView listView1;
        private ListView listView2;
        private ListView listView3;
        private Label nickname1;
        private Label nickname2;
        private Label nickname3;
        private ListView listViewHand;
        private Button buttonPlayCard;
        private PictureBox pictureBoxBabyDeck;
        private PictureBox pictureBoxDiscard;
        private Button buttonDiscard;
    }
}
