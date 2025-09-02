using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.LinkLabel;

namespace BloqueadorUrlHost
{
    public partial class FrmPrincipal : Form
    {
        public FrmPrincipal()
        {
            InitializeComponent();
            this.Text = "Bloqueador de URLHost - v" + Application.ProductVersion;
            strStatusAdmin.Text = IsAdministrator() ? "Executando como Administrador" : "Não está executando como Administrador";
            lstLinks.Items.Clear();
        }

        private bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private void Timer1_Tick(object sender, EventArgs e)
        {
            DateTime hora = DateTime.Now;
            strHora.Text = hora.ToString("HH:mm:ss");
            strData.Text = hora.ToString("dd' de 'MMMM' de 'yyyy");
        }

        private void BtnAdicionar_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtLink.Text))
            {
                MessageBox.Show("Por favor, insira um Link/URL para bloquear.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            string link = txtLink.Text.Trim();
            if (!lstLinks.Items.Contains(link))
            {
                lstLinks.Items.Add(link);
                txtLink.Text = "";
            }
            else
            {
                MessageBox.Show("Este Link/URL já está na lista.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnImportar_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Arquivo de Texto (*.txt)|*.txt";
                ofd.Title = "Importar lista de links";

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var linhas = File.ReadAllLines(ofd.FileName, Encoding.UTF8);
                        int adicionados = 0;
                        foreach (var linha in linhas)
                        {
                            string link = linha.Trim();
                            if (!string.IsNullOrEmpty(link) && !lstLinks.Items.Contains(link))
                            {
                                lstLinks.Items.Add(link);
                                adicionados++;
                            }
                        }
                        MessageBox.Show($"{adicionados} link(s) importado(s) com sucesso!", "Importação", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Erro ao importar: " + ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void BtnExportar_Click(object sender, EventArgs e)
        {
            if (lstLinks.Items.Count == 0)
            {
                MessageBox.Show("Não há links para exportar.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "Arquivo de Texto (*.txt)|*.txt";
                sfd.Title = "Exportar lista de links";
                sfd.FileName = "links_bloquear.txt";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        List<string> links = new List<string>();
                        foreach (var item in lstLinks.Items)
                        {
                            links.Add(item.ToString());
                        }
                        File.WriteAllLines(sfd.FileName, links, Encoding.UTF8);
                        MessageBox.Show("Lista exportada com sucesso!", "Exportação", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Erro ao exportar: " + ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void BtnExecutar_Click(object sender, EventArgs e)
        {
            BloquearLiberarAcoes(false);
            strStatusExecucao.Text = "Iniciando bloqueio...";
            Application.DoEvents();

            if (!IsAdministrator())
            {
                MessageBox.Show("O programa precisa ser executado como Administrador para bloquear URLs.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);

                BloquearLiberarAcoes(true);
                strStatusExecucao.Text = "Precisa da permissão de Administrador para executar...";
                Application.DoEvents();

                return;
            }

            List<string> links = new List<string>();
            foreach (string link in lstLinks.Items)
            {
                string linkhttp;
                string linkhttps;
                if (link.StartsWith("http://"))
                {
                    linkhttp = link;
                    linkhttps = link.Replace("http://", "https://");
                }
                else if (link.StartsWith("https://"))
                {
                    linkhttps = link;
                    linkhttp = link.Replace("https://", "http://");
                }
                else
                {
                    linkhttp = "http://" + link;
                    linkhttps = "https://" + link;
                }

                if (!links.Contains(linkhttp))
                {
                    if (UrlValida(linkhttp) && UrlExiste(linkhttp))
                    {
                        links.Add(linkhttp);
                    }
                }

                if (!links.Contains(linkhttps))
                {
                    if (UrlValida(linkhttps) && UrlExiste(linkhttps))
                    {
                        links.Add(linkhttps);
                    }
                }
                List<string> ips = ObterIpDaUrl(link);
                foreach (string ip in ips)
                {
                    string urlHttp = "http://" + ip;
                    string urlHttps = "https://" + ip;

                    if (!links.Contains(urlHttp))
                    {
                        if (UrlValida(urlHttp) && UrlExiste(urlHttp))
                        {
                            links.Add(urlHttp);
                        }
                    }
                    if (!links.Contains(urlHttps))
                    {
                        if (UrlValida(urlHttps) && UrlExiste(urlHttps))
                        {
                            links.Add(urlHttps);
                        }
                    }
                }
            }

            if (links.Count == 0)
            {
                MessageBox.Show("Nenhum Link/URL válido para bloquear.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                BloquearLiberarAcoes(true);
                strStatusExecucao.Text = "Sem links/URLs válidos para bloquear...";
                Application.DoEvents();

                return;
            }

            if (BloquearUrlsNoHosts(links))
            {
                List<string> urlsNaoBloqueadas = new List<string>();
                foreach (var url in links)
                {
                    if (!TestarBloqueioUrl(url))
                    {
                        urlsNaoBloqueadas.Add(url);
                    }
                }

                if (urlsNaoBloqueadas.Count == 0)
                {
                    MessageBox.Show("Todas as URLs foram bloqueadas com sucesso!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    BloquearLiberarAcoes(true);
                    strStatusExecucao.Text = "Links/URLs bloqueadas com sucesso!";
                    Application.DoEvents();
                }
                else
                {
                    string msg = "As seguintes URLs não puderam ser bloqueadas:" + Environment.NewLine + Environment.NewLine + string.Join(Environment.NewLine, urlsNaoBloqueadas);
                    MessageBox.Show(msg, "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);


                    BloquearLiberarAcoes(true);
                    strStatusExecucao.Text = "Alguns links/URLs não foram bloqueados...";
                    Application.DoEvents();
                }
            }
            else
            {
                MessageBox.Show("Falha ao tentar bloquear as URLs. Verifique se o programa está sendo executado como Administrador.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);

                BloquearLiberarAcoes(true);
                strStatusExecucao.Text = "Falha ao bloquear...";
                Application.DoEvents();
                return;
            }
        }

        private void BloquearLiberarAcoes(bool liberar)
        {
            txtLink.Enabled = liberar;
            btnAdicionar.Enabled = liberar;
            btnImportar.Enabled = liberar;
            btnExportar.Enabled = liberar;
            btnExecutar.Enabled = liberar;
            lstLinks.Enabled = liberar;
            btnRemover.Enabled = liberar;
            Application.DoEvents();
        }

        private bool UrlValida(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out Uri uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }

        private bool UrlExiste(string url)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "HEAD";
                request.Timeout = 3000; // 3 segundos
                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    return response.StatusCode == HttpStatusCode.OK;
                }
            }
            catch
            {
                return false;
            }
        }
        private List<string> ObterIpDaUrl(string url)
        {
            try
            {
                if (!UrlValida(url))
                    return new List<string>();

                Uri uri = new Uri(url);
                string host = uri.Host;
                IPAddress[] ips = Dns.GetHostAddresses(host);
                return ips.Select(ip => ip.ToString()).ToList();
            }
            catch
            {
                return new List<string>();
            }
        }


        private bool BloquearUrlsNoHosts(List<string> lstLinks)
        {
            try
            {
                string hostsPath = @"C:\Windows\System32\drivers\etc\hosts";
                var linhas = new List<string>();

                // Lê o conteúdo atual do arquivo Hosts
                if (File.Exists(hostsPath))
                    linhas = File.ReadAllLines(hostsPath).ToList();

                foreach (var url in lstLinks)
                {
                    if (!UrlValida(url))
                        continue;

                    Uri uri = new Uri(url);
                    string host = uri.Host;

                    // Verifica se já existe uma entrada para o host
                    bool jaExiste = linhas.Any(l => l.Contains(host));
                    if (!jaExiste)
                    {
                        linhas.Add($"127.0.0.1 {host}");
                    }
                }

                // Escreve as linhas de volta ao arquivo Hosts
                File.WriteAllLines(hostsPath, linhas);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool TestarBloqueioUrl(string url)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "HEAD";
                request.Timeout = 3000; // 3 segundos
                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    // Se conseguir resposta, não está bloqueado
                    return false;
                }
            }
            catch (WebException ex)
            {
                // Se der erro de conexão, provavelmente está bloqueado
                if (ex.Status == WebExceptionStatus.ConnectFailure ||
                    ex.Status == WebExceptionStatus.NameResolutionFailure ||
                    ex.Status == WebExceptionStatus.Timeout)
                {
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        private void BtnRemover_Click(object sender, EventArgs e)
        {
            var selectedItems = lstLinks.SelectedItems;
            while (selectedItems.Count > 0)
            {
                lstLinks.Items.Remove(selectedItems[0]);
            }
        }
    }
}
