namespace InstallWizard
{
    partial class Form1
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.lblText = new System.Windows.Forms.Label();
            this.lblTitle = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.btnBack = new System.Windows.Forms.Button();
            this.btnNext = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.pnDB = new System.Windows.Forms.Panel();
            this.btnTestConn = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.txtPassword = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.txtDatabase = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.txtUsername = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.txtDbServer = new System.Windows.Forms.TextBox();
            this.pnEnterprise = new System.Windows.Forms.Panel();
            this.label6 = new System.Windows.Forms.Label();
            this.txtUri = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.txtName = new System.Windows.Forms.TextBox();
            this.pnCheck = new System.Windows.Forms.Panel();
            this.txtCheckConfig = new System.Windows.Forms.TextBox();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.groupBox2.SuspendLayout();
            this.pnDB.SuspendLayout();
            this.pnEnterprise.SuspendLayout();
            this.pnCheck.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.BackColor = System.Drawing.SystemColors.Window;
            this.groupBox1.Controls.Add(this.lblText);
            this.groupBox1.Controls.Add(this.lblTitle);
            this.groupBox1.Controls.Add(this.pictureBox1);
            this.groupBox1.ForeColor = System.Drawing.SystemColors.ControlText;
            this.groupBox1.Location = new System.Drawing.Point(-5, -7);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(507, 56);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            // 
            // lblText
            // 
            this.lblText.AutoSize = true;
            this.lblText.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblText.Location = new System.Drawing.Point(26, 32);
            this.lblText.Name = "lblText";
            this.lblText.Size = new System.Drawing.Size(35, 13);
            this.lblText.TabIndex = 2;
            this.lblText.Text = "label1";
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTitle.Location = new System.Drawing.Point(13, 12);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(41, 13);
            this.lblTitle.TabIndex = 1;
            this.lblTitle.Text = "label1";
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = global::InstallWizard.Resource1.safeid;
            this.pictureBox1.Location = new System.Drawing.Point(364, 19);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(133, 23);
            this.pictureBox1.TabIndex = 1;
            this.pictureBox1.TabStop = false;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.btnBack);
            this.groupBox2.Controls.Add(this.btnNext);
            this.groupBox2.Controls.Add(this.btnCancel);
            this.groupBox2.Location = new System.Drawing.Point(-5, 317);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(507, 50);
            this.groupBox2.TabIndex = 1;
            this.groupBox2.TabStop = false;
            // 
            // btnBack
            // 
            this.btnBack.Location = new System.Drawing.Point(247, 16);
            this.btnBack.Name = "btnBack";
            this.btnBack.Size = new System.Drawing.Size(75, 23);
            this.btnBack.TabIndex = 15;
            this.btnBack.Text = "< &Voltar";
            this.btnBack.UseVisualStyleBackColor = true;
            this.btnBack.Click += new System.EventHandler(this.btnBack_Click);
            // 
            // btnNext
            // 
            this.btnNext.Location = new System.Drawing.Point(324, 16);
            this.btnNext.Name = "btnNext";
            this.btnNext.Size = new System.Drawing.Size(75, 23);
            this.btnNext.TabIndex = 16;
            this.btnNext.Text = "&Avançar >";
            this.btnNext.UseVisualStyleBackColor = true;
            this.btnNext.Click += new System.EventHandler(this.btnNext_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(416, 16);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 17;
            this.btnCancel.Text = "Cancelar";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // pnDB
            // 
            this.pnDB.Controls.Add(this.btnTestConn);
            this.pnDB.Controls.Add(this.label5);
            this.pnDB.Controls.Add(this.txtPassword);
            this.pnDB.Controls.Add(this.label4);
            this.pnDB.Controls.Add(this.txtDatabase);
            this.pnDB.Controls.Add(this.label3);
            this.pnDB.Controls.Add(this.txtUsername);
            this.pnDB.Controls.Add(this.label2);
            this.pnDB.Controls.Add(this.label1);
            this.pnDB.Controls.Add(this.txtDbServer);
            this.pnDB.Location = new System.Drawing.Point(0, 50);
            this.pnDB.Name = "pnDB";
            this.pnDB.Size = new System.Drawing.Size(497, 270);
            this.pnDB.TabIndex = 2;
            // 
            // btnTestConn
            // 
            this.btnTestConn.Location = new System.Drawing.Point(23, 199);
            this.btnTestConn.Name = "btnTestConn";
            this.btnTestConn.Size = new System.Drawing.Size(132, 23);
            this.btnTestConn.TabIndex = 7;
            this.btnTestConn.Text = "Testar conexão";
            this.btnTestConn.UseVisualStyleBackColor = true;
            this.btnTestConn.Click += new System.EventHandler(this.btnTestConn_Click);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(264, 148);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(38, 13);
            this.label5.TabIndex = 11;
            this.label5.Text = "Senha";
            // 
            // txtPassword
            // 
            this.txtPassword.Location = new System.Drawing.Point(280, 164);
            this.txtPassword.Name = "txtPassword";
            this.txtPassword.PasswordChar = '*';
            this.txtPassword.Size = new System.Drawing.Size(183, 20);
            this.txtPassword.TabIndex = 6;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(8, 69);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(78, 13);
            this.label4.TabIndex = 9;
            this.label4.Text = "Base de dados";
            // 
            // txtDatabase
            // 
            this.txtDatabase.Location = new System.Drawing.Point(24, 85);
            this.txtDatabase.Name = "txtDatabase";
            this.txtDatabase.Size = new System.Drawing.Size(462, 20);
            this.txtDatabase.TabIndex = 2;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(21, 148);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(43, 13);
            this.label3.TabIndex = 7;
            this.label3.Text = "Usuário";
            // 
            // txtUsername
            // 
            this.txtUsername.Location = new System.Drawing.Point(37, 164);
            this.txtUsername.Name = "txtUsername";
            this.txtUsername.Size = new System.Drawing.Size(205, 20);
            this.txtUsername.TabIndex = 5;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 123);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(124, 13);
            this.label2.TabIndex = 5;
            this.label2.Text = "Informações de conexão";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(8, 18);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(148, 13);
            this.label1.TabIndex = 4;
            this.label1.Text = "Endereço do banco de dados";
            // 
            // txtDbServer
            // 
            this.txtDbServer.Location = new System.Drawing.Point(24, 34);
            this.txtDbServer.Name = "txtDbServer";
            this.txtDbServer.Size = new System.Drawing.Size(462, 20);
            this.txtDbServer.TabIndex = 1;
            // 
            // pnEnterprise
            // 
            this.pnEnterprise.Controls.Add(this.label6);
            this.pnEnterprise.Controls.Add(this.txtUri);
            this.pnEnterprise.Controls.Add(this.label7);
            this.pnEnterprise.Controls.Add(this.txtName);
            this.pnEnterprise.Location = new System.Drawing.Point(530, 25);
            this.pnEnterprise.Name = "pnEnterprise";
            this.pnEnterprise.Size = new System.Drawing.Size(497, 130);
            this.pnEnterprise.TabIndex = 3;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(14, 62);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(89, 13);
            this.label6.TabIndex = 13;
            this.label6.Text = "Domínio principal";
            // 
            // txtUri
            // 
            this.txtUri.Location = new System.Drawing.Point(30, 78);
            this.txtUri.Name = "txtUri";
            this.txtUri.Size = new System.Drawing.Size(462, 20);
            this.txtUri.TabIndex = 11;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(14, 11);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(93, 13);
            this.label7.TabIndex = 12;
            this.label7.Text = "Nome da empresa";
            // 
            // txtName
            // 
            this.txtName.Location = new System.Drawing.Point(30, 27);
            this.txtName.Name = "txtName";
            this.txtName.Size = new System.Drawing.Size(462, 20);
            this.txtName.TabIndex = 10;
            // 
            // pnCheck
            // 
            this.pnCheck.Controls.Add(this.txtCheckConfig);
            this.pnCheck.Location = new System.Drawing.Point(547, 173);
            this.pnCheck.Name = "pnCheck";
            this.pnCheck.Size = new System.Drawing.Size(497, 270);
            this.pnCheck.TabIndex = 14;
            // 
            // txtCheckConfig
            // 
            this.txtCheckConfig.Location = new System.Drawing.Point(17, 11);
            this.txtCheckConfig.Multiline = true;
            this.txtCheckConfig.Name = "txtCheckConfig";
            this.txtCheckConfig.ReadOnly = true;
            this.txtCheckConfig.Size = new System.Drawing.Size(462, 250);
            this.txtCheckConfig.TabIndex = 10;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(735, 396);
            this.Controls.Add(this.pnCheck);
            this.Controls.Add(this.pnEnterprise);
            this.Controls.Add(this.pnDB);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Form1";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.groupBox2.ResumeLayout(false);
            this.pnDB.ResumeLayout(false);
            this.pnDB.PerformLayout();
            this.pnEnterprise.ResumeLayout(false);
            this.pnEnterprise.PerformLayout();
            this.pnCheck.ResumeLayout(false);
            this.pnCheck.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Label lblText;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button btnNext;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Panel pnDB;
        private System.Windows.Forms.Button btnBack;
        private System.Windows.Forms.Panel pnEnterprise;
        private System.Windows.Forms.TextBox txtDbServer;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox txtPassword;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox txtDatabase;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtUsername;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button btnTestConn;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox txtUri;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox txtName;
        private System.Windows.Forms.Panel pnCheck;
        private System.Windows.Forms.TextBox txtCheckConfig;
    }
}

