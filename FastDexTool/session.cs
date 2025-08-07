using Newtonsoft.Json.Linq;
using System;
using System.Drawing;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FastDexTool
{
    public partial class session : Form
    {
        private const string ServerUrl = "https://fastdexapi.vercel.app/check";
        private const string CurrentVersion = "1.0";
        private string currentLicense = "";
        private Timer licenseCheckTimer;
        private string loadingPanelText = "LOADING...";
        private Timer mensajeTimer;
        private string panelMessage = "PLEASE PUT YOUR LICENSE";

        public static class SessionData
        {
            public static string License { get; set; }
            public static string IP { get; set; }
        }

        public session()
        {
            InitializeComponent();

            mensajeTimer = new Timer();
            mensajeTimer.Interval = 3000;
            mensajeTimer.Tick += (s, e) =>
            {
                mensajeTimer.Stop();
                panelMessage = "PLEASE PUT YOUR LICENSE";
                Alts.Invalidate();
            };
        }

        private async Task CheckStatusFromGitHub()
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string url = "https://fastdexapi.vercel.app/version";
                    string content = await client.GetStringAsync(url);

                    content = content.Trim().ToLower();

                    Warning.Visible = false;
                    Maintenance.Visible = false;
                    Main.Visible = false;

                    if (content.Contains("rate_limited"))
                    {
                        string retryAfter = "0";

                        var match = Regex.Match(content, @"retry_after_seconds\s*=\s*(\d+)");
                        if (match.Success)
                        {
                            retryAfter = match.Groups[1].Value;
                        }

                        panelMessage = $"RATE LIMITED - {retryAfter}s";
                        Warning.Visible = true;
                        Alts.Invalidate();
                        return;
                    }

                    string latestVersion = null;
                    string[] parts = content.Split('|');
                    foreach (var part in parts)
                    {
                        if (part.StartsWith("version="))
                        {
                            latestVersion = part.Replace("version=", "").Trim();
                            break;
                        }
                    }

                    if (!string.IsNullOrEmpty(latestVersion) && IsNewerVersion(latestVersion, CurrentVersion))
                    {
                        panelMessage = $"OUTDATED VERSION - {latestVersion}";
                        Warning.Visible = true;
                        Alts.Invalidate();
                        return;
                    }

                    if (content.Contains("zv"))
                    {
                        Maintenance.Visible = true;
                        panelMessage = "FASTDEX IS IN MAINTENANCE";
                        Alts.Invalidate();
                    }
                    else if (content.Contains("z")) 
                    {
                        Main.Visible = true;
                        Alts.Invalidate();
                    }
                    else if (content.Contains("zx")) 
                    {
                        Warning.Visible = true;
                        panelMessage = "WARNING PANEL";
                        Alts.Invalidate();
                    }
                    else
                    {
                        Main.Visible = true;
                        Alts.Invalidate();
                    }
                }
            }
            catch
            {
                panelMessage = "FAILED TO CONNECT";
                Warning.Visible = true;
                Alts.Invalidate();
            }
        }

        private async Task<bool> ValidateLicenseAsync(string license)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string url = $"{ServerUrl}?license={Uri.EscapeDataString(license)}";

                    for (int attempt = 0; attempt < 2; attempt++)
                    {
                        HttpResponseMessage response = await client.GetAsync(url);
                        string responseBody = await response.Content.ReadAsStringAsync();

                        if (!response.IsSuccessStatusCode)
                        {
                            if (attempt == 0)
                            {
                                await Task.Delay(300);
                                continue;
                            }

                            ShowPanelMessage("API ERROR");
                            return false;
                        }

                        JObject json;
                        try
                        {
                            json = JObject.Parse(responseBody);
                        }
                        catch
                        {
                            ShowPanelMessage("INVALID RESPONSE");
                            return false;
                        }

                        string status = json["status"]?.ToString()?.ToLower();
                        string error = json["error"]?.ToString()?.ToLower();
                        string ip = json["ip"]?.ToString();

                        if (status == "valid" && !string.IsNullOrEmpty(ip))
                        {
                            SessionData.License = license;
                            SessionData.IP = ip;
                            return true;
                        }

                        if (!string.IsNullOrEmpty(error))
                        {
                            if (error.Contains("rate_limited"))
                            {
                                string retrySeconds = json["retry_after_seconds"]?.ToString();
                                if (int.TryParse(retrySeconds, out int seconds))
                                {
                                    ShowPanelMessage($"RATE LIMITED - {seconds}s");
                                }
                                else
                                {
                                    ShowPanelMessage("RATE LIMITED");
                                }
                            }
                            else if (error.Contains("invalid license"))
                            {
                                ShowPanelMessage("INVALID LICENSE");
                            }
                            else if (error.Contains("invalid ip"))
                            {
                                ShowPanelMessage("INVALID IP");
                            }
                            else if (error.Contains("internal_error"))
                            {
                                ShowPanelMessage("FASTDEX API ERROR");
                            }
                            else
                            {
                                ShowPanelMessage("UNKNOWN ERROR");
                            }

                            return false;
                        }

                        if (attempt == 0)
                        {
                            await Task.Delay(300);
                            continue;
                        }

                        ShowPanelMessage("UNKNOWN RESPONSE");
                        return false;
                    }

                    return false;
                }
            }
            catch
            {
                ShowPanelMessage("CONNECTION ERROR");
                return false;
            }
        }

        private bool IsNewerVersion(string latest, string current)
        {
            if (Version.TryParse(latest, out Version latestVer) && Version.TryParse(current, out Version currentVer))
            {
                return latestVer > currentVer;
            }
            return false;
        }

        private void ShowPanelMessage(string msg)
        {
            panelMessage = msg.ToUpper();
            Alts.Invalidate();
            mensajeTimer.Stop();
            mensajeTimer.Start();
        }

        private void Alts_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            using (Font font = new Font("Comic Sans MS", 8.25f, FontStyle.Bold))
            using (Brush brush = new SolidBrush(Color.White))
            {
                SizeF size = g.MeasureString(panelMessage, font);
                float x = ((Control)sender).Width / 2 - size.Width / 2;
                float y = ((Control)sender).Height / 2 - size.Height / 2;
                g.DrawString(panelMessage, font, brush, x, y);
            }
        }



        public Panel LogPanel { get; set; }
        
        private void guna2Button1_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://discord.gg/6ZCdV29w6n");
        }

        private async void Search_Click(object sender, EventArgs e)
        {
            currentLicense = guna2TextBox1.Text.Trim();

            if (string.IsNullOrWhiteSpace(currentLicense) || currentLicense.Length < 5)
            {
                ShowPanelMessage("INVALID INPUT");
                return;
            }

            panelMessage = "LOADING LICENSE...";
            Alts.Invalidate();
            Application.DoEvents(); 

            bool valid = await ValidateLicenseAsync(currentLicense);

            if (valid)
            {
                DesvanecerYOcultar();
                StartLicenseMonitor();
            }
            else
            {
            }
        }

        private void DesvanecerYOcultar()
        {
            Task.Run(async () =>
            {
                for (double op = 1.0; op >= 0; op -= 0.05)
                {
                    this.Invoke((MethodInvoker)(() =>
                    {
                        this.Opacity = op;
                    }));
                    await Task.Delay(50);
                }

                this.Invoke((MethodInvoker)(() =>
                {
                    this.Hide();

                    if (LogPanel != null)
                        LogPanel.Visible = false;  
                }));
            });
        }

        private void MostrarYReiniciar()
        {
            this.Invoke((MethodInvoker)(() =>
            {
                this.Opacity = 1;
                this.Show();

                if (LogPanel != null)
                    LogPanel.Visible = true; 

                ShowPanelMessage("LICENSE REMOVED");
            }));
        }


        private void StartLicenseMonitor()
        {
            licenseCheckTimer = new Timer();
            licenseCheckTimer.Interval = 30000;
            licenseCheckTimer.Tick += async (s, e) =>
            {
                bool valid = await ValidateLicenseAsync(currentLicense);
                if (!valid)
                {
                    licenseCheckTimer.Stop();
                    MostrarYReiniciar();
                }
            };
            licenseCheckTimer.Start();
        }

        private void guna2Panel2_Paint(object sender, PaintEventArgs e)
        {
            string texto = "FASTDEX IS IN MAINTENANCE";
            Font fuente = new Font("Comic Sans MS", 8.25f, FontStyle.Bold);
            Brush pincel = Brushes.White;

            SizeF tamaño = e.Graphics.MeasureString(texto, fuente);
            float x = (guna2Panel2.Width - tamaño.Width) / 2;
            float y = (guna2Panel2.Height - tamaño.Height) / 2;

            e.Graphics.DrawString(texto.ToUpper(), fuente, pincel, x, y);
        }

        private void guna2Panel5_Paint(object sender, PaintEventArgs e)
        {
            string texto = "JOIN DISCORD";
            Font fuente = new Font("Comic Sans MS", 8.25f, FontStyle.Bold);
            Brush pincel = Brushes.White;

            SizeF tamaño = e.Graphics.MeasureString(texto, fuente);
            float x = (guna2Panel5.Width - tamaño.Width) / 2;
            float y = (guna2Panel5.Height - tamaño.Height) / 2;

            e.Graphics.DrawString(texto.ToUpper(), fuente, pincel, x, y);
        }

        private void guna2Button2_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://discord.gg/6ZCdV29w6n");
        }

        private void guna2Panel4_Paint(object sender, PaintEventArgs e)
        {
            string texto = "OUTDATED VERSION";
            Font fuente = new Font("Comic Sans MS", 8.25f, FontStyle.Bold);
            Brush pincel = Brushes.White;

            SizeF tamaño = e.Graphics.MeasureString(texto, fuente);
            float x = (guna2Panel2.Width - tamaño.Width) / 2;
            float y = (guna2Panel2.Height - tamaño.Height) / 2;

            e.Graphics.DrawString(texto.ToUpper(), fuente, pincel, x, y);
        }


        private void guna2Button4_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://discord.gg/6ZCdV29w6n");
        }

        private void guna2Panel1_Paint(object sender, PaintEventArgs e)
        {
            string texto = "JOIN ON DISCORD";
            Font fuente = new Font("Comic Sans MS", 8.25f, FontStyle.Bold);
            Brush pincel = Brushes.White;

            SizeF tamaño = e.Graphics.MeasureString(texto, fuente);
            float x = (guna2Panel5.Width - tamaño.Width) / 2;
            float y = (guna2Panel5.Height - tamaño.Height) / 2;

            e.Graphics.DrawString(texto.ToUpper(), fuente, pincel, x, y);
        }


        private async void session_Load(object sender, EventArgs e)
        {
            guna2Panel3.Invalidate();
            await CheckStatusFromGitHub();
            await Task.Delay(2000);
            guna2Panel3.Visible = false;
        }
 
        private void guna2Panel6_Paint(object sender, PaintEventArgs e)
        {
            if (!guna2Panel3.Visible) return;

            string texto = loadingPanelText;
            Font fuente = new Font("Comic Sans MS", 8.25f, FontStyle.Bold);
            Brush pincel = Brushes.White;

            SizeF tamaño = e.Graphics.MeasureString(texto, fuente);
            float x = (guna2Panel6.Width - tamaño.Width) / 2;
            float y = (guna2Panel6.Height - tamaño.Height) / 2;

            e.Graphics.DrawString(texto.ToUpper(), fuente, pincel, x, y);
        }

    }
}
