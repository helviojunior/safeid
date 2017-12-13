using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using SafeTrend.Data;
using IAM.GlobalDefs;
using System.Net;
using IAM.Config;
using IAM.EnterpriseCreator;
using System.Data;
using System.Data.SqlClient;
using System.Xml;

namespace InstallWizard
{
    public enum WizardStep
    {
        DB = 1,
        Enterprise,
        Check,
        Install,
        Installed
    }

    public partial class Form1 : Form
    {
        private string[] args;
        private WizardStep step = WizardStep.DB;

        public Form1(string[] args)
        {
            this.args = args;
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.Text = "Configuração do SafeId";
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Width = 505;
            this.Height = 390;


#if DEBUG
            txtDbServer.Text = ".";
            txtDatabase.Text = "install_test";
            txtUsername.Text = "sa";
            txtPassword.Text = "123456";
            txtName.Text = "Empresa teste";
            txtUri.Text    = "url_teste";
#endif

            rebuild();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            confirmExit();
        }

        private void btnNext_Click(object sender, EventArgs e)
        {

            switch (step)
            {
                case WizardStep.DB:
                    //Check DB config
                    if (TestConn())
                    {
                        step = WizardStep.Enterprise;
                        rebuild();
                    }
                    break;

                case WizardStep.Enterprise:

                    //Verifica informações da empresa

                    if (txtName.Text.Trim() == "")
                    {
                        MessageBox.Show("Favor preencher o nome da empresa.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        txtName.Focus();
                        return;
                    }

                    if (txtUri.Text.Trim() == "")
                    {
                        MessageBox.Show("Favor preencher o domínio principal.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        txtUri.Focus();
                        return;
                    }


                    try
                    {
                        IPAddress tmp = IPAddress.Parse(txtUri.Text.Trim());

                        MessageBox.Show("Domínio principal não pode ser um endereço IP", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        txtUri.Focus();
                        return;
                    }
                    catch { }


                    try
                    {
                        Uri tmp = new Uri("http://" + txtUri.Text.Trim() + "/");

                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Domínio principal inválido.\r\n\r\n" + ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        txtUri.Focus();
                        return;
                    }

                    step = WizardStep.Check;
                    rebuild();
                    break;

                case WizardStep.Check:
                    step = WizardStep.Install;
                    rebuild();
                    startInstall();
                    break;
            }

        }

        private void btnBack_Click(object sender, EventArgs e)
        {

            switch (step)
            {
                case WizardStep.Enterprise:
                    //Check DB config
                    step = WizardStep.DB;
                    rebuild();
                    break;

                case WizardStep.Check:
                case WizardStep.Install:
                    //Check DB config
                    step = WizardStep.Enterprise;
                    rebuild();
                    break;

            }

        }

        private void rebuild()
        {

            pnDB.Visible = pnEnterprise.Visible = pnCheck.Visible = false;
            btnBack.Visible = false;

            lblTitle.Text = "";
            lblText.Text = "";

            btnNext.Text = "&Avançar >";
            btnNext.Enabled = true;

            pnDB.Top = pnEnterprise.Top = pnCheck.Top = 50;
            pnDB.Left = pnEnterprise.Left = pnCheck.Left = 0;
            pnDB.Width = pnEnterprise.Width = pnCheck.Width = 497;
            pnDB.Height = pnEnterprise.Height = pnCheck.Height = 270;


            switch (step)
            {
                case WizardStep.DB:

                    pnDB.Visible = true;
                    lblTitle.Text = "Banco de dados";
                    lblText.Text = "Preencha as configurações do banco de dados";

                    txtUsername.Enabled = txtPassword.Enabled = true;
                    break;

                case WizardStep.Enterprise:

                    pnEnterprise.Visible = true;
                    btnBack.Visible = true;
                    lblTitle.Text = "Adição de empresa";
                    lblText.Text = "Preencha as informações da empresa";

                    break;


                case WizardStep.Check:
                    pnCheck.Visible = true;
                    btnBack.Visible = true;
                    lblTitle.Text = "Checagem das configurações";
                    lblText.Text = "Verifique as configurações e clique em Instalar";

                    btnNext.Text = "&Instalar";

                    txtCheckConfig.Text = "";

                    txtCheckConfig.Text += "Dados da Empresa" + Environment.NewLine;
                    txtCheckConfig.Text += "\tNome: " + txtName.Text + Environment.NewLine;
                    txtCheckConfig.Text += "\tDomínio principal: " + txtUri.Text + Environment.NewLine;


                    txtCheckConfig.Text += Environment.NewLine;

                    txtCheckConfig.Text += "Banco de dados" + Environment.NewLine;
                    txtCheckConfig.Text += "\tServidor: " + txtDbServer.Text + Environment.NewLine;
                    txtCheckConfig.Text += "\tBase de dados: " + txtDatabase.Text + Environment.NewLine;
                    txtCheckConfig.Text += "\tTipo de autenticação: Usuário/senha" + Environment.NewLine;
                    txtCheckConfig.Text += "\tUsuário: " + txtUsername.Text + Environment.NewLine;
                    txtCheckConfig.Text += "\tSenha: ***" + Environment.NewLine;

                    break;


                case WizardStep.Install:
                    pnCheck.Visible = true;
                    btnBack.Visible = false;
                    btnNext.Enabled = false;
                    btnCancel.Enabled = false;

                    lblTitle.Text = "Instalação";
                    lblText.Text = "Aguarde o processo de instalação";
                    
                    btnNext.Text = "Instalando...";

                    txtCheckConfig.Text = "";

                    break;
            }
        }


        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;

            if (step != WizardStep.Install)
                confirmExit();
        }

        private void confirmExit()
        {

            if (step == WizardStep.Installed)
                Process.GetCurrentProcess().Kill();

            if (MessageBox.Show("A Instalação não foi concluída. Se você sair agora, o programa não será instalado.\r\n\r\nSair do Programa de Instalação?", "Sair do Programa de Instalação?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.Yes)
            {
                /*
                if ((args != null) && (args.Length > 0))
                {
                    if (File.Exists(args[1]) && (Path.GetExtension(args[1]).ToLower() == ".exe"))
                    {
                        Process.Start(args[1], "/SILENT /NORESTART /SUPPRESSMSGBOXES");
                    }
                }*/

                Process.GetCurrentProcess().Kill();
            }
        }

        private void rbTrusted_CheckedChanged(object sender, EventArgs e)
        {
            rebuild();
        }

        private void rbUserDefined_CheckedChanged(object sender, EventArgs e)
        {
            rebuild();
        }

        private void btnTestConn_Click(object sender, EventArgs e)
        {
            if (TestConn())
                MessageBox.Show("Conexão realizada com sucesso!", "Teste de conexão", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private Boolean TestConn()
        {
            //return true;

            this.Cursor = Cursors.WaitCursor;

            try
            {

                if (txtDbServer.Text.Trim() == "")
                {
                    MessageBox.Show("Favor preencher o endereço do banco de dados.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    txtDbServer.Focus();
                    return false;
                }

                if (txtDatabase.Text.Trim() == "")
                {
                    MessageBox.Show("Favor preencher a base de dados.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    txtDatabase.Focus();
                    return false;
                }

                if (txtUsername.Text.Trim() == "")
                {
                    MessageBox.Show("Favor preencher o nome do usuário.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    txtUsername.Focus();
                    return false;
                }


                if (txtPassword.Text.Trim() == "")
                {
                    MessageBox.Show("Favor preencher a senha.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    txtPassword.Focus();
                    return false;
                }

                IAMDatabase db = null;
                try
                {

                    if (txtDatabase.Text.Trim().ToLower() == "master")
                        throw new Exception("Não pode ser utilizado a base de dados Master");


                    db = new IAMDatabase(txtDbServer.Text, txtDatabase.Text, txtUsername.Text, txtPassword.Text);

                    db.openDB();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Erro ao conectar na base de dados.\r\n\r\n" + ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
                finally
                {
                    if (db != null)
                        db.Dispose();
                }

                return true;
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }



        private void startInstall()
        {
            Application.DoEvents();

            Boolean success = false;

            txtCheckConfig.Text = "Iniciando instalação" + Environment.NewLine;

            
            IAMDatabase db = null;
            try
            {

                txtCheckConfig.Text += "Definindo variáveis de ambiente: ";
                Application.DoEvents();
                DirectoryInfo appDir = new DirectoryInfo(Environment.CurrentDirectory);

                try
                {
                    appDir = new DirectoryInfo(args[0]);
                }
                catch { }
                txtCheckConfig.Text += "OK" + Environment.NewLine;
                txtCheckConfig.Text += "\tDiretório de execução: " + appDir.FullName + Environment.NewLine;

                Application.DoEvents();

                txtCheckConfig.Text += "Conectando no banco de dados: ";
                Application.DoEvents();

                if (txtDatabase.Text.Trim().ToLower() == "master")
                    throw new Exception("Não pode ser utilizado a base de dados Master");

                db = new IAMDatabase(txtDbServer.Text, txtDatabase.Text, txtUsername.Text, txtPassword.Text);

                db.openDB();

                txtCheckConfig.Text += "OK" + Environment.NewLine;
                Application.DoEvents();

                //##############################
                //Estrutura de dados
                txtCheckConfig.Text += "Criando estrutura de dados: ";
                Application.DoEvents();

                //Verifica se a base de dados está sendo utilizada
                Int64 tableCount = db.ExecuteScalar<Int64>("SELECT cast(COUNT(*) as bigint) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo'");

                if (tableCount > 0)
                {
                    if (MessageBox.Show("A base de dados " + txtDatabase.Text + " contém " + tableCount + " tabelas e aparentemente está sendo utilizado por outra aplicação.\r\n\r\nDeseja continuar a instalação nesta base?", "Deseja continuar a instalação?", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == System.Windows.Forms.DialogResult.No)
                    {
                        throw new Exception("Cancelado pelo usuário");
                    }
                }

                Object trans = db.BeginTransaction();
                try
                {
                    using (IAMDbInstall dbCreate = new IAMDbInstall(db))
                        dbCreate.Create(trans);

                    db.Commit();
                }
                catch (Exception ex)
                {
                    db.Rollback();
                    throw ex;
                }
                txtCheckConfig.Text += "OK" + Environment.NewLine;
                Application.DoEvents();

                //##############################
                //Verificvando existência de outras empresas
                txtCheckConfig.Text += "Verificando configuração existente: ";

                Int64 enterpriseCount = db.ExecuteScalar<Int64>("SELECT cast(COUNT(*) as bigint) FROM enterprise");
                if (enterpriseCount > 0)
                    throw new Exception("Base de dados com informações de outras empresas.");

                txtCheckConfig.Text += "OK" + Environment.NewLine;
                Application.DoEvents();

                //##############################
                //Atualizando Base de dados
                txtCheckConfig.Text += "Atualizando base de dados: ";
                try
                {
                    using (IAM.GlobalDefs.Update.IAMDbUpdate updt = new IAM.GlobalDefs.Update.IAMDbUpdate(txtDbServer.Text, txtDatabase.Text, txtUsername.Text, txtPassword.Text))
                        updt.Update();

                    txtCheckConfig.Text += "OK" + Environment.NewLine;
                    Application.DoEvents();
                }
                catch (Exception ex)
                {
                    throw new Exception("Falha ao atualizar o banco de dados: " + ex.Message);
                }

                //##############################
                //Finalizando instalação
                txtCheckConfig.Text += "Configurando diretórios: ";
                Application.DoEvents();

                db.ExecuteNonQuery("delete from server_config where data_name = 'pluginFolder'; insert into server_config (data_name, data_value) values ('pluginFolder','" + Path.Combine(appDir.FullName, "IAMServer\\Plugins") + "')");
                db.ExecuteNonQuery("delete from server_config where data_name = 'inboundFiles'; insert into server_config (data_name, data_value) values ('inboundFiles','" + Path.Combine(appDir.FullName, "IAMServer\\In") + "')");
                db.ExecuteNonQuery("delete from server_config where data_name = 'outboundFiles'; insert into server_config (data_name, data_value) values ('outboundFiles','" + Path.Combine(appDir.FullName, "IAMServer\\Out") + "')");

                txtCheckConfig.Text += "OK" + Environment.NewLine;
                Application.DoEvents();

                //##############################
                //Certificados e chaves de instalação
                txtCheckConfig.Text += "Gerando chave de instalação: ";
                Application.DoEvents();

                using (ServerKey2 sk = new ServerKey2(db.Connection))
                    sk.RenewCert(db.Connection);
                txtCheckConfig.Text += "OK" + Environment.NewLine;
                Application.DoEvents();


                //##############################
                //Criando a empresa
                txtCheckConfig.Text += "Criando empresa: ";
                Application.DoEvents();

                Creator creator = new Creator(db, txtName.Text.Trim(), txtUri.Text.Trim(), "pt-BR");
                creator.BuildCertificates();
                creator.Commit();

                txtCheckConfig.Text += "OK" + Environment.NewLine;
                Application.DoEvents();

                //##############################
                //Criando a empresa
                txtCheckConfig.Text += "Criando arquivos de configuração: ";
                Application.DoEvents();

                FileInfo serverFile = new FileInfo( Path.Combine(appDir.FullName, "IAMServer\\server.conf"));
                
                if (serverFile.Exists)
                    serverFile.Delete();

                WriteToFile(serverFile, "sqlserver=" + txtDbServer.Text.Trim() + Environment.NewLine);
                WriteToFile(serverFile, "sqldb=" + txtDatabase.Text.Trim() + Environment.NewLine);
                WriteToFile(serverFile, "sqlusername=" + txtUsername.Text.Trim() + Environment.NewLine);
                WriteToFile(serverFile, "sqlpassword=" + txtPassword.Text.Trim() + Environment.NewLine);
                WriteToFile(serverFile, "enginemaxthreads=30" + Environment.NewLine);
                
                //Web.config
                FileInfo webConfigFile = new FileInfo(Path.Combine(appDir.FullName, "IAMServer\\web\\web.config"));

                if (webConfigFile.Exists)
                {

                    XmlDocument doc = new XmlDocument();
                    doc.Load(webConfigFile.FullName);

                    //get root element
                    System.Xml.XmlElement Root = doc.DocumentElement;

                    XmlNode connectionStringsNode = Root["connectionStrings"];
                    foreach (XmlNode cs in connectionStringsNode.ChildNodes)
                    {
                        Boolean update = false;
                        foreach (XmlAttribute att in cs.Attributes)
                            if (att.Name.ToLower() == "name" && att.Value.ToLower() == "iamdatabase")
                                update = true;

                        if (update)
                            foreach (XmlAttribute att in cs.Attributes)
                                if (att.Name.ToLower() == "connectionstring")
                                    att.Value = db.ConnectionString;

                    }

                    doc.Save(webConfigFile.FullName);
                    doc = null;
                }

                txtCheckConfig.Text += "OK" + Environment.NewLine;
                Application.DoEvents();

                success = true;

            }
            catch (Exception ex)
            {
                success = false;

                txtCheckConfig.Text += "ERRO" + Environment.NewLine;
                txtCheckConfig.Text += "\t" + ex.Message + Environment.NewLine;
                Application.DoEvents();

                return;
            }
            finally
            {
                if (db != null)
                    db.Dispose();

                if (!success)
                {
                    txtCheckConfig.Text += Environment.NewLine + "PROCESSO ABORTADO!!!" + Environment.NewLine;
                    btnBack.Enabled = true;
                    btnBack.Visible = true;
                    btnNext.Text = "&Avançar >";
                    btnCancel.Enabled = true;
                }
                else
                {
                    txtCheckConfig.Text += Environment.NewLine + "Instalação realizada com sucesso." + Environment.NewLine;
                    btnCancel.Text = "Finalizar";
                    btnCancel.Enabled = true;
                    btnNext.Visible = false;
                    step = WizardStep.Installed;

                }


                //Localiza e remove todos os arquivos .cer e .pfx deste diretório
                try
                {
                    

                    List<FileInfo> files = new List<FileInfo>();
                    try
                    {
                        files.AddRange(new DirectoryInfo(Environment.CurrentDirectory).GetFiles("*.cer"));
                        files.AddRange(new DirectoryInfo(Environment.CurrentDirectory).GetFiles("*.pfx"));
                    }
                    catch { }

                    try
                    {
                        System.Reflection.Assembly asm = System.Reflection.Assembly.GetAssembly(this.GetType());

                        files.AddRange(new DirectoryInfo(Path.GetDirectoryName(asm.Location)).GetFiles("*.cer"));
                        files.AddRange(new DirectoryInfo(Path.GetDirectoryName(asm.Location)).GetFiles("*.pfx"));
                    }
                    catch { }

                    foreach(FileInfo f in files)
                        try
                        {
                            f.Delete();
                        }
                        catch { }
                }
                catch { }

            }

        }

        private void WriteToFile(FileInfo file, String text)
        {
            BinaryWriter writer = new BinaryWriter(File.Open(file.FullName, FileMode.Append));
            writer.Write(Encoding.UTF8.GetBytes(text));
            writer.Flush();
            writer.Close();
        }

    }
}
