﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Resources;
using System.Threading;
using System.Windows.Forms;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Server;

namespace ProjectCreation
{
    public partial class FrmCreateTeamProject : Form
    {
        private static ResourceManager _resourceManager;

        public FrmCreateTeamProject()
        {
#if DEBUG
            Thread.CurrentThread.CurrentUICulture = MessageBox.Show("Choose Yes for pt-BR or No for en-US", "Choose Language", MessageBoxButtons.YesNo) ==
                                                    DialogResult.Yes ? new CultureInfo("pt-BR") : new CultureInfo("en-US");
#endif

            InitializeComponent();

            txtCaminho.Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "teamprojects.txt");
            txtLogPath.Text = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            _resourceManager = new ResourceManager("ProjectCreation.Resource", this.GetType().Assembly);
        }

        private void btnCriar_click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtCollectionUrl.Text))
            {
                MessageBox.Show(_resourceManager.GetString("FrmCriarTeamProject_txtCollectionUrl_Precisa_Ser_Informada"), _resourceManager.GetString("MessageBox_TituloGenericoErro"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtCollectionUrl.Focus();
                return;
            }

            if (!File.Exists(txtCaminho.Text))
            {
                MessageBox.Show(string.Format(_resourceManager.GetString("FrmCriarTeamProject_InvalidListPath"), txtCaminho.Text), _resourceManager.GetString("MessageBox_TituloGenericoErro"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtLogPath.Text))
            {
                MessageBox.Show(_resourceManager.GetString("FrmCriarTeamProject_txtLogPath_Required"), _resourceManager.GetString("MessageBox_TituloGenericoErro"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!Directory.Exists(txtLogPath.Text))
            {
                var result = MessageBox.Show(_resourceManager.GetString("FrmCriarTeamProject_btnCriar_txtLogPath_DirectoryNotFoud_Create"), _resourceManager.GetString("MessageBox_TituloGenericoErro"), MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    Directory.CreateDirectory(txtLogPath.Text);
                }
                else
                {
                    return;
                }
            }

            if (string.IsNullOrWhiteSpace(cmbProcessTemplates.Text))
            {
                MessageBox.Show(_resourceManager.GetString("FrmCriarTeamProject_ProcessTemplateNotSelected"),
                    _resourceManager.GetString("MessageBox_TituloGenericoErro"), MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return;
            }

            string validacao;

            var lines = ReadFile(txtCaminho.Text, out validacao);

            if (!string.IsNullOrWhiteSpace(validacao))
            {
                MessageBox.Show(string.Format(_resourceManager.GetString("FrmCriarTeamProject_InvalidListFile"), "\r\n", validacao),
                    _resourceManager.GetString("MessageBox_TituloGenericoErro"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            const string executavel = "tfpt.exe";

            var comando = string.Format(@"createteamproject /collection:{0} /teamproject:""[teamproject]"" /processtemplate:""{2}"" /sourcecontrol:New /log:""{1}"" /noportal", txtCollectionUrl.Text, txtLogPath.Text, cmbProcessTemplates.Text);

            foreach (var line in lines)
            {
                var teamProject = line.Split('|')[0];
                var portal = line.Split('|')[1];

                var comandoFinal = comando.Replace("[teamproject]", teamProject);

                if (portal.Equals(_resourceManager.GetString("FrmCriarTeamProject_TextFile_Yes")))
                {
                    comandoFinal = comandoFinal.Replace("/noportal", string.Empty);
                }

                try
                {
                    RunTool(executavel, comandoFinal);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(string.Format(_resourceManager.GetString("FrmCriarTeamProject_CouldNotCreateTeamProject"), teamProject));
                }
            }
        }

        private static IEnumerable<string> ReadFile(string filePath, out string message)
        {
            var lines = File.ReadAllLines(filePath);
            message = string.Empty;
            var messages = new List<string>();
            var columns = false;
            var yesNo = false;
            var emptyLines = false;

            if (lines.Length == 0)
            {
                messages.Add(_resourceManager.GetString("FrmCriarTeamProject_txtCaminho_Arquivo_Vazio"));
            }

            foreach (var line in lines)
            {
                var split = line.Split('|');

                if (string.IsNullOrWhiteSpace(line) && !emptyLines)
                {
                    messages.Add(_resourceManager.GetString("FrmCriarTeamProject_EmptyLines"));
                    emptyLines = true;
                    continue;
                }
                
                if (split.Length < 2 && !columns)
                {
                    messages.Add(_resourceManager.GetString("FrmCriarTeamProject_YourFileContainsLessThan2Lines"));
                    columns = true;

                    continue;
                }

                if (split.Length == 2)
                {
                    if (split[1].ToLower().Equals(_resourceManager.GetString("FrmCriarTeamProject_TextFile_Yes").ToLower())
                        || split[1].ToLower().Equals(_resourceManager.GetString("FrmCriarTeamProject_TextFile_No").ToLower())
                        || yesNo)
                    {
                        continue;
                    }

                    messages.Add(_resourceManager.GetString("FrmCriarTeamProject_InvalidSharepointSiteFlag"));
                    yesNo = true;
                }
            }

            if (messages.Count > 0)
            {
                message = messages.Aggregate(message, (current, individualMessage) => current + string.Format("\r\n{0}", individualMessage));
            }

            return lines;
        }

        private static void RunTool(string fileName, string arguments)
        {
            var info = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                WorkingDirectory = @"%programfiles(x86)%\Microsoft Team Foundation Server 2013 Power Tools"
            };

            var process = Process.Start(info);
            process.WaitForExit();
        }

        private void txtCaminho_Click(object sender, EventArgs e)
        {
            var openFile = new OpenFileDialog
            {
                InitialDirectory = Path.GetDirectoryName(txtCaminho.Text),
                Filter = _resourceManager.GetString("FrmCriarTeamProject_txtCaminho_FilterFiles")
            };

            if (openFile.ShowDialog() == DialogResult.OK)
            {
                txtCaminho.Text = openFile.FileName;
            }
        }

        private void txtCaminho_TextChanged(object sender, EventArgs e)
        {

        }

        private void txtLogPath_Click(object sender, EventArgs e)
        {
            folderBrowsing.RootFolder = Environment.SpecialFolder.Desktop;

            if (folderBrowsing.ShowDialog() == DialogResult.OK)
            {
                txtLogPath.Text = folderBrowsing.SelectedPath;
            }
        }

        private void btnSelecionarColecao_Click(object sender, EventArgs e)
        {
            var tpcPicker = new TeamProjectPicker(TeamProjectPickerMode.NoProject, false);

            var dialogResult = tpcPicker.ShowDialog();

            if (dialogResult == DialogResult.OK)
            {
                txtCollectionUrl.Text = tpcPicker.SelectedTeamProjectCollection.Uri.ToString();

                this.AtualizarProcessTemplates();
            }
        }

        private void AtualizarProcessTemplates()
        {
            var tfs = new TfsTeamProjectCollection(new Uri(txtCollectionUrl.Text));

            var templates = tfs.GetService<IProcessTemplates>();

            var headers = templates.TemplateHeaders();

            cmbProcessTemplates.Items.Clear();

            if (headers.Any())
            {
                cmbProcessTemplates.Enabled = true;
                
                foreach (var header in headers)
                {
                    cmbProcessTemplates.Items.Add(header.Name);
                }
            }
            else
            {
                cmbProcessTemplates.Enabled = false;
            }
        }
    }
}
