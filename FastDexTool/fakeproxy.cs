using Guna.UI2.WinForms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Markup;
using static FastDexTool.session;

namespace FastDexTool
{
    public partial class fakeproxy : Form
    {
        private string mensajeSuperior = "";
        private string logsTextoVacio = "JOIN/LEAVE/CMD/MSG LOG";
        private Process proxyProcess;
        private bool proxyActivo = false;
        private string panel4Mensaje = "PROXY INFO";
        private string proxyIP = "";
        private List<string> logsUnificados = new List<string>();
        private const int maxLinesUnificadas = 9;
        private string adminKey = "";
        private string mensajeInferior = "";
        private Timer mensajeTimer;
        private string ipValueGlobal = "";
        private string pinggyTcpUrl = "";
        private int modoSeleccionado = 0;

        private bool placeholderActiveIp = true;
        private bool placeholderActiveToken = true;

        public fakeproxy()
        {
            InitializeComponent();

            mensajeTimer = new Timer();
            mensajeTimer.Interval = 3000;

            ip.Enter += Ip_Enter;
            ip.Leave += Ip_Leave;

            guna2TextBox2.KeyDown += guna2TextBox2_KeyDown;
            guna2TextBox2.Enter += Token_Enter;
            guna2TextBox2.Leave += Token_Leave;
            guna2TextBox2.TextChanged += guna2TextBox2_TextChanged;

            mensajeTimer.Tick += (s, e) =>
            {
                mensajeTimer.Stop();
                mensajeSuperior = "";
                mensajeInferior = "";
                guna2Panel1.Invalidate();

                if (string.IsNullOrWhiteSpace(ip.Text))
                    SetIpPlaceholder();

                if (string.IsNullOrWhiteSpace(guna2TextBox2.Text))
                    SetTokenPlaceholder();
            };

            SetIpPlaceholder();
            SetTokenPlaceholder();
        }


        private void guna2TextBox1_TextChanged(object sender, EventArgs e)
        {
            if (!placeholderActiveIp)
            {
                ipValueGlobal = ip.Text.Trim();
            }
            else
            {
                ipValueGlobal = "";
            }
        }

        private async void guna2Button2_Click(object sender, EventArgs e)
        {
            proxyActivo = true;
            try
            {
                var process = new System.Diagnostics.Process();
                process.StartInfo.FileName = "python";
                process.StartInfo.Arguments = "--version";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;

                process.Start();
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                process.WaitForExit();

                string versionOutput = string.IsNullOrWhiteSpace(output) ? error : output;
                if (!versionOutput.StartsWith("Python"))
                {
                    mensajeSuperior = "PYTHON REQUIRED";
                    mensajeInferior = "INSTALL PYTHON 3.11";
                    guna2Panel1.Invalidate();
                    mensajeTimer.Start();
                    return;
                }

                string version = versionOutput.Replace("Python", "").Trim();
                if (!Version.TryParse(version, out Version parsed) || parsed.Major < 3 || (parsed.Major == 3 && parsed.Minor < 11))
                {
                    mensajeSuperior = "PYTHON REQUIRED";
                    mensajeInferior = "INSTALL PYTHON 3.11";
                    guna2Panel1.Invalidate();
                    mensajeTimer.Start();
                    return;
                }
            }
            catch
            {
                mensajeSuperior = "PYTHON REQUIRED";
                mensajeInferior = "INSTALL PYTHON 3.11";
                guna2Panel1.Invalidate();
                mensajeTimer.Start();
                return;
            }

            if (placeholderActiveIp || string.IsNullOrWhiteSpace(ip.Text))
            {
                mensajeSuperior = "PLEASE ENTER";
                mensajeInferior = "VALID IP ADDRESS";
                guna2Panel1.Invalidate();
                mensajeTimer.Start();
                return;
            }

            if (modoSeleccionado == 0)
            {
                mensajeSuperior = "SELECT MODE";
                mensajeInferior = "OPTION FOR START";
                guna2Panel1.Invalidate();
                mensajeTimer.Start();
                return;
            }

            if (string.IsNullOrWhiteSpace(pinggyTcpUrl) || !pinggyTcpUrl.StartsWith("tcp://", StringComparison.OrdinalIgnoreCase))
            {
                mensajeSuperior = "PLEASE SETUP";
                mensajeInferior = "PINGGY TCP URL";
                guna2Panel1.Invalidate();
                mensajeTimer.Start();
                return;
            }

            mensajeTimer.Start();
            Application.DoEvents();
            guna2Button3.Visible = false;
            guna2Button4.Visible = false;
            guna2Button5.Visible = false;
            guna2Button6.Visible = false;

            guna2Button9.Visible = true;
            guna2Button10.Visible = true;
            guna2Button11.Visible = true;
            guna2Button12.Visible = true;

            proxyIP = "";
            adminKey = "";

            string ipValue = ip.Text.Trim();
            string modo = modoSeleccionado.ToString();
            string rutaTunnel = GetTunnel();

            string roamingDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));

            string scriptPath = Path.Combine(roamingDir, "FastDexData", "FProxy", "proxy.py");
            string workingDir = Path.Combine(roamingDir, "FastDexData", "FProxy");

            try
            {
                string licencia = SessionData.License;

                proxyProcess = new Process();
                proxyProcess.StartInfo.FileName = "python";
                proxyProcess.StartInfo.Arguments = $"\"{scriptPath}\" {ipValue} {modo} \"{rutaTunnel}\" \"{licencia}\"";
                proxyProcess.StartInfo.WorkingDirectory = workingDir;
                proxyProcess.StartInfo.CreateNoWindow = true;
                proxyProcess.StartInfo.UseShellExecute = false;
                proxyProcess.StartInfo.RedirectStandardOutput = true;
                proxyProcess.StartInfo.RedirectStandardError = true;

                proxyProcess.OutputDataReceived += ProxyOutputHandler;
                proxyProcess.ErrorDataReceived += ProxyOutputHandler;

                proxyProcess.Start();
                proxyProcess.BeginOutputReadLine();
                proxyProcess.BeginErrorReadLine();

                panel4Mensaje = "STARTING PROXY...";
                guna2Panel4.Invalidate();
            }
            catch (Exception)
            {
                mensajeSuperior = "FAILED TO START";
                mensajeInferior = "CHECK PYTHON/FILES";
                guna2Panel1.Invalidate();
                mensajeTimer.Start();
            }
        }

        private void guna2Panel4_Paint(object sender, PaintEventArgs e)
        {
            try
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (Font fuente = new Font("Comic Sans MS", 8.25f, FontStyle.Bold))
                using (Brush pincel = new SolidBrush(Color.White))
                {
                    string mensajeArriba = string.IsNullOrWhiteSpace(proxyIP) ? panel4Mensaje : $"PROXY IP: {proxyIP}";
                    string mensajeAbajo = string.IsNullOrWhiteSpace(adminKey) ? "" : $"ADMIN KEY: {adminKey}";

                    SizeF sizeArriba = g.MeasureString(mensajeArriba, fuente);
                    SizeF sizeAbajo = g.MeasureString(mensajeAbajo, fuente);

                    int centroX1 = ((Control)sender).Width / 2 - (int)(sizeArriba.Width / 2);
                    int centroX2 = ((Control)sender).Width / 2 - (int)(sizeAbajo.Width / 2);
                    int centroY = ((Control)sender).Height / 2 - (int)((sizeArriba.Height + sizeAbajo.Height) / 2);

                    g.DrawString(mensajeArriba, fuente, pincel, centroX1, centroY);
                    if (!string.IsNullOrEmpty(mensajeAbajo))
                        g.DrawString(mensajeAbajo, fuente, pincel, centroX2, centroY + sizeArriba.Height);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Paint error: " + ex.Message);
            }
        }

        private void ProxyOutputHandler(object sender, DataReceivedEventArgs e)
        {
            if (!proxyActivo || string.IsNullOrWhiteSpace(e.Data))
                return;

            string line = e.Data.Trim();

            Console.WriteLine("[proxy.py] " + line);

            this.BeginInvoke((MethodInvoker)(() =>
            {
                if (line.StartsWith("Admin Key:", StringComparison.OrdinalIgnoreCase))
                {
                    adminKey = line.Replace("Admin Key:", "").Trim();
                    if (!string.IsNullOrWhiteSpace(proxyIP))
                    {
                        panel4Mensaje = $"PROXY IP: {proxyIP} | ADMIN KEY: {adminKey}";
                        guna2Panel4.Invalidate();
                    }
                    return;
                }

                if (line.StartsWith("Proxy IP:", StringComparison.OrdinalIgnoreCase))
                {
                    proxyIP = line.Replace("Proxy IP:", "").Trim();
                    if (!string.IsNullOrWhiteSpace(adminKey))
                    {
                        panel4Mensaje = $"PROXY IP: {proxyIP} | ADMIN KEY: {adminKey}";
                        guna2Panel4.Invalidate();
                    }
                    return;
                }

                if (line.IndexOf("Servidor no encontrado", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    panel4Mensaje = "SERVER NOT FOUND";
                    guna2Panel4.Invalidate();

                    Task.Delay(2000).ContinueWith(_ =>
                    {
                        guna2Panel4.Invoke(new Action(() =>
                        {
                            panel4Mensaje = "PROXY INFO";
                            guna2Panel4.Invalidate();
                        }));
                    });

                }

                else if (line.Contains("[Connect]") || line.Contains("[Disconnect]") || line.Contains("[Command]") || line.Contains("[Message]"))
                {
                    try
                    {
                        int fechaStart = line.IndexOf('[') + 1;
                        int fechaEnd = line.IndexOf(']');
                        string fecha = line.Substring(fechaStart, fechaEnd - fechaStart);

                        string tipo = "";
                        string usuario = "";
                        string contenido = "";

                        if (line.Contains("[Connect]"))
                            tipo = "CONNECT";
                        else if (line.Contains("[Disconnect]"))
                            tipo = "DISCONNECT";
                        else if (line.Contains("[Command]"))
                            tipo = "CMD";
                        else if (line.Contains("[Message]"))
                            tipo = "MSG";

                        int userStart = line.IndexOf('[', fechaEnd + 1 + tipo.Length) + 1;
                        int userEnd = line.IndexOf(']', userStart);
                        usuario = line.Substring(userStart, userEnd - userStart);

                        int igual = line.IndexOf('=', userEnd);
                        if (igual > 0)
                        {
                            int contStart = line.IndexOf('[', igual) + 1;
                            int contEnd = line.IndexOf(']', contStart);
                            if (contStart > 0 && contEnd > contStart)
                                contenido = line.Substring(contStart, contEnd - contStart);
                        }

                        string final = $"{fecha} | {tipo} | {usuario}";
                        if (!string.IsNullOrWhiteSpace(contenido))
                            final += $" | {contenido}";

                        if (logsUnificados.Count >= maxLinesUnificadas)
                            logsUnificados.RemoveAt(0);

                        logsUnificados.Add(final);
                        Logs.Invalidate();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error parsing log: " + ex.Message);
                    }
                }
                else if (panel4Mensaje == "STARTING PROXY...")
                {
                    panel4Mensaje = line;
                    guna2Panel4.Invalidate();
                }
            }));
        }

        private void Logs_Paint(object sender, PaintEventArgs e)
        {
            try
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                using (Font fuente = new Font("Comic Sans MS", 8f, FontStyle.Bold))
                using (Brush pincel = new SolidBrush(Color.White))
                {
                    var panel = (Control)sender;
                    int margenHorizontal = 10;
                    int margenVertical = 4;
                    float y = margenVertical;

                    if (logsUnificados.Count == 0)
                    {
                        string texto = logsTextoVacio;
                        SizeF size = g.MeasureString(texto, fuente);
                        float centroX = (panel.Width - size.Width) / 2;
                        float centroY = (panel.Height - size.Height) / 2;
                        g.DrawString(texto, fuente, pincel, centroX, centroY);
                        return;
                    }

                    foreach (string linea in logsUnificados)
                    {
                        SizeF size = g.MeasureString(linea, fuente);
                        float x = margenHorizontal;

                        if (y + size.Height > panel.Height)
                            break;

                        g.DrawString(linea, fuente, pincel, x, y);
                        y += size.Height + margenVertical;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Paint error: " + ex.Message);
            }
        }

        private void guna2Button3_Click(object sender, EventArgs e)
        {
            modoSeleccionado = 2;
            mensajeSuperior = "SELECTED";
            mensajeInferior = "MODE: 2";
            guna2Panel1.Invalidate();
            mensajeTimer.Start();
        }

        private void guna2Button4_Click(object sender, EventArgs e)
        {
            modoSeleccionado = 1;
            mensajeSuperior = "SELECTED";
            mensajeInferior = "MODE: 1";
            guna2Panel1.Invalidate();
            mensajeTimer.Start();
        }

        private void guna2Button5_Click(object sender, EventArgs e)
        {
            modoSeleccionado = 3;
            mensajeSuperior = "SELECTED";
            mensajeInferior = "MODE: 3";
            guna2Panel1.Invalidate();
            mensajeTimer.Start();
        }

        private void guna2Button6_Click(object sender, EventArgs e)
        {
            modoSeleccionado = 4;
            mensajeSuperior = "SELECTED";
            mensajeInferior = "MODE: 4";
            guna2Panel1.Invalidate();
            mensajeTimer.Start();
        }

        private void SetIpPlaceholder()
        {
            placeholderActiveIp = true;
            ip.Text = "IP:PORT/DOMAIN";
            ip.ForeColor = Color.White;
            ip.Font = new Font("Comic Sans MS", 8.25f, FontStyle.Bold);
            ip.TextAlign = HorizontalAlignment.Center;
            ip.Enabled = true;
        }

        private void Ip_Enter(object sender, EventArgs e)
        {
            if (placeholderActiveIp)
            {
                ip.Text = "";
                ip.ForeColor = Color.White;
                ip.Font = new Font("Comic Sans MS", 8.25f, FontStyle.Bold);
                ip.TextAlign = HorizontalAlignment.Left;
                placeholderActiveIp = false;
            }
        }

        private void Ip_Leave(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ip.Text))
                SetIpPlaceholder();
        }

        private void Token_Enter(object sender, EventArgs e)
        {
            if (placeholderActiveToken)
            {
                guna2TextBox2.Text = "";
                guna2TextBox2.ForeColor = Color.White;
                guna2TextBox2.Font = new Font("Comic Sans MS", 8.25f, FontStyle.Bold);
                guna2TextBox2.TextAlign = HorizontalAlignment.Left;
                placeholderActiveToken = false;
                guna2TextBox2.Enabled = true;
            }
        }

        private void Token_Leave(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(guna2TextBox2.Text))
                SetTokenPlaceholder();
        }

        private void guna2Panel1_Paint(object sender, PaintEventArgs e)
        {
            try
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                using (Font fuente = new Font("Comic Sans MS", 8.25f, FontStyle.Bold))
                using (Brush pincel = new SolidBrush(Color.White))
                using (StringFormat formatoCentrado = new StringFormat()
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Near
                })
                {
                    Rectangle rect = guna2Panel1.ClientRectangle;

                    if (!string.IsNullOrWhiteSpace(mensajeSuperior) || !string.IsNullOrWhiteSpace(mensajeInferior))
                    {
                        float totalAltura = fuente.Height * 2;
                        float startY = (rect.Height - totalAltura) / 2;

                        Rectangle rectSuperior = new Rectangle(0, (int)startY, rect.Width, fuente.Height);
                        Rectangle rectInferior = new Rectangle(0, (int)(startY + fuente.Height), rect.Width, fuente.Height);

                        g.DrawString(mensajeSuperior, fuente, pincel, rectSuperior, formatoCentrado);
                        g.DrawString(mensajeInferior, fuente, pincel, rectInferior, formatoCentrado);
                    }
                    else
                    {
                        string texto = "FASTDEX LOGS";
                        Rectangle rectTexto = new Rectangle(0, 0, rect.Width, rect.Height);
                        formatoCentrado.LineAlignment = StringAlignment.Center;
                        g.DrawString(texto, fuente, pincel, rectTexto, formatoCentrado);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Paint error: " + ex.Message);
            }
        }


        private void guna2Button1_Click(object sender, EventArgs e)
        {
            proxyActivo = false;
            modoSeleccionado = 0;

            guna2Button3.Visible = true;
            guna2Button4.Visible = true;
            guna2Button5.Visible = true;
            guna2Button6.Visible = true;

            guna2Button9.Visible = false;
            guna2Button10.Visible = false;
            guna2Button11.Visible = false;
            guna2Button12.Visible = false;

            placeholderActiveIp = true;     
            ip.Text = "IP:PORT/DOMAIN";
            ip.TextAlign = HorizontalAlignment.Center;
            ip.ForeColor = Color.White;      

            ipValueGlobal = "";             

            StopAllJavaProcesses();
        }

        private void StopAllJavaProcesses()
        {
            try
            {
                Process[] allProcesses = Process.GetProcesses();

                foreach (Process proc in allProcesses)
                {
                    try
                    {
                        if (proc.ProcessName.Equals("java", StringComparison.OrdinalIgnoreCase))
                        {
                            proc.Kill();
                            proc.WaitForExit();
                            proc.Dispose();
                        }
                    }
                    catch (Exception exProc)
                    {
                        Console.WriteLine($"NO SE PUDO MATAR PROCESO (PID {proc.Id}): {exProc.Message}");
                    }
                }

                panel4Mensaje = "PROXY INFO";
                proxyIP = "";
                adminKey = "";
                guna2Panel4.Invalidate();

                logsTextoVacio = "JOIN/LEAVE/CMD/MSG LOG";
                logsUnificados.Clear();
                Logs.Invalidate();

                mensajeSuperior = "FAKEPROXY WAS";
                mensajeInferior = "STOPPED SUCCESSFULLY";
                guna2Panel1.Invalidate();
                mensajeTimer.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR AL DETENER PROCESOS JAVA: " + ex.Message);
            }
        }

        private void fakeproxy_Load(object sender, EventArgs e) { }

        public string GetTunnel()
        {
            return pinggyTcpUrl;
        }

        private void SetTokenPlaceholder()
        {
            placeholderActiveToken = true;
            guna2TextBox2.Text = "PINGGY TCP";
            guna2TextBox2.ForeColor = Color.White;
            guna2TextBox2.Font = new Font("Comic Sans MS", 8.25f, FontStyle.Bold);
            guna2TextBox2.TextAlign = HorizontalAlignment.Center;
            guna2TextBox2.Enabled = true;
        }


        private void guna2TextBox2_TextChanged(object sender, EventArgs e)
        {
            var tb = sender as Guna2TextBox;
            if (tb == null) return;
            if (placeholderActiveToken) return;

            string value = (tb.Text ?? "").Trim();

            if (value.StartsWith("tcp://", StringComparison.OrdinalIgnoreCase))
            {
                pinggyTcpUrl = value;
                mensajeSuperior = "SUCCESS";
                mensajeInferior = "PINGGY TCP URL SET";
                guna2Panel1.Invalidate();
                mensajeTimer.Start();
            }
            else
            {
                pinggyTcpUrl = "";
            }
        }
        private void guna2TextBox2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                if (string.IsNullOrWhiteSpace(pinggyTcpUrl))
                {
                    mensajeSuperior = "ERROR";
                    mensajeInferior = "INVALID OR EMPTY TCP URL";
                    guna2Panel1.Invalidate();
                    mensajeTimer.Start();
                }
            }
        }


        private void guna2Button9_Click(object sender, EventArgs e)
        {

        }

        private void guna2Button10_Click(object sender, EventArgs e)
        {

        }

        private void guna2Button11_Click(object sender, EventArgs e)
        {

        }

        private void guna2Button12_Click(object sender, EventArgs e)
        {

        }
        private void Save_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(proxyIP))
            {
                Clipboard.SetText(proxyIP);
            }
        }

        private async void guna2Button7_Click(object sender, EventArgs e)
        {
            Clipboard.SetText("ssh -p 443 -R0:127.0.0.1:33330 tcp@a.pinggy.io");

            mensajeSuperior = "OPEN CMD WINDOW";
            mensajeInferior = "RIGHT CLICK AND ENTER";
            guna2Panel1.Invalidate();

            await Task.Delay(3000);

            mensajeSuperior = "IN FINGERPRINT";
            mensajeInferior = "TYPE 'YES' AND ENTER";
            guna2Panel1.Invalidate();

            await Task.Delay(3000);

            mensajeSuperior = "IN PASSWORD";
            mensajeInferior = "PRESS ENTER AGAIN";
            guna2Panel1.Invalidate();

            await Task.Delay(3000);

            mensajeSuperior = "COPY THE TCP URL";
            mensajeInferior = "PASTE IT IN FASTDEX";
            guna2Panel1.Invalidate();
            mensajeTimer.Start();
        }
    }
}