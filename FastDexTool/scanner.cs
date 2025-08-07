using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FastDexTool
{
    public partial class scanner : Form
    {
        private string rawInput = string.Empty;
        private string targetIP = string.Empty;
        private bool nmapFound = true;
        private bool invalidIP = false;
        private bool hasScanned = false;
        private string scanStatus = string.Empty;
        private string customPortRange = "65501-65535";
        private readonly List<int> openPorts = new List<int>();
        private CancellationTokenSource scanCancellation;
        private ToolTip copyToolTip = new ToolTip();

        public scanner()
        {
            InitializeComponent();
            ip.PlaceholderText = "IP/DOMAIN";
            ip.PlaceholderForeColor = Color.White; 
            ip.ForeColor = Color.White;            
            ip.TextAlign = HorizontalAlignment.Center;
            guna2TextBox1.PlaceholderText = "PORT RANGE";
            guna2TextBox1.PlaceholderForeColor = Color.White; 
            guna2TextBox1.ForeColor = Color.White;              
            guna2TextBox1.TextAlign = HorizontalAlignment.Center;

            typeof(Panel)
                .GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .SetValue(guna2Panel1, true, null);

            guna2Panel1.MouseClick += Guna2Panel1_MouseClick;
        }


        private void Guna2Panel1_MouseClick(object sender, MouseEventArgs e)
        {
            if (scanStatus == "OPEN PORTS" && openPorts.Count > 0)
            {
                List<int> snapshot;
                lock (openPorts)
                {
                    snapshot = openPorts.OrderBy(p => p).ToList();
                }
                string portsText = string.Join(", ", snapshot);
                try
                {
                    Clipboard.SetText(portsText);
                }
                catch
                {
                }
            }
        }

        private async void ShowTemporaryTooltip(Control control, string message, Point location)
        {
            copyToolTip.Show(message, control, location.X, location.Y, 1500);
            await Task.Delay(1500);
            copyToolTip.Hide(control);
        }

        private void ip_TextChanged(object sender, EventArgs e)
        {
            rawInput = ip.Text.Trim();
        }

        private async void Start_Click(object sender, EventArgs e)
        {
            try
            {
                var process = new System.Diagnostics.Process();
                process.StartInfo.FileName = "nmap";
                process.StartInfo.Arguments = "-V";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;

                process.Start();
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                process.WaitForExit();

                string versionOutput = string.IsNullOrWhiteSpace(output) ? error : output;
                if (!versionOutput.ToLower().Contains("nmap"))
                {
                    SetStatus("INSTALL NMAP");
                    return;
                }
            }
            catch
            {
                SetStatus("INSTALL NMAP");
                return;
            }

            if (string.IsNullOrWhiteSpace(rawInput))
            {
                SetStatus("ENTER VALID IP/DOMAIN");
                return;
            }

            invalidIP = false;
            targetIP = rawInput;

            if (!IPAddress.TryParse(rawInput, out _))
            {
                try
                {
                    var entries = Dns.GetHostAddresses(rawInput);
                    var ipv4 = Array.Find(entries, a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                    if (ipv4 != null)
                        targetIP = ipv4.ToString();
                    else
                        throw new Exception("No IPv4 address found");
                }
                catch
                {
                    invalidIP = true;
                    SetStatus("INVALID IP/DOMAIN");
                    return;
                }
            }

            openPorts.Clear();
            hasScanned = false;
            SetStatus("SCANNING...");

            nmapFound = await Task.Run(() => CheckNmap());
            if (!nmapFound)
            {
                SetStatus("NMAP NOT FOUND");
                return;
            }

            scanCancellation?.Cancel();
            scanCancellation = new CancellationTokenSource();

            try
            {
                await RunNmapScan(targetIP, scanCancellation.Token);
            }
            catch (OperationCanceledException)
            {
                SetStatus("SCAN CANCELLED");
                return;
            }
            catch (Exception)
            {
                SetStatus("SCAN ERROR");
                return;
            }

            hasScanned = true;
            if (openPorts.Count == 0)
                SetStatus("NO OPEN PORTS FOUND.");
            else
                SetStatus("OPEN PORTS");
        }

        private void SetStatus(string status)
        {
            scanStatus = status;
            guna2Panel1.Invoke((Action)(() => guna2Panel1.Invalidate()));

        }

        private bool CheckNmap()
        {
            try
            {
                using (Process test = new Process())
                {
                    test.StartInfo.FileName = "nmap.exe";
                    test.StartInfo.Arguments = "--version";
                    test.StartInfo.RedirectStandardOutput = true;
                    test.StartInfo.UseShellExecute = false;
                    test.StartInfo.CreateNoWindow = true;

                    test.Start();

                    if (!test.WaitForExit(2000))
                    {
                        test.Kill();
                        return false;
                    }

                    return test.ExitCode == 0;
                }
            }
            catch
            {
                return false;
            }
        }

        private async Task RunNmapScan(string ip, CancellationToken ct)
        {
            string arguments = $"-p {customPortRange} -T4 -sS -Pn {ip}";

            using (Process proc = new Process())
            {
                proc.StartInfo.FileName = "nmap.exe";
                proc.StartInfo.Arguments = arguments;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.CreateNoWindow = true;

                proc.Start();

                Regex portRegex = new Regex(@"^(\d+)\/tcp\s+open", RegexOptions.IgnoreCase);
                string line;

                while (!proc.HasExited)
                {
                    ct.ThrowIfCancellationRequested();

                    line = await proc.StandardOutput.ReadLineAsync();

                    if (line == null)
                        break;

                    var match = portRegex.Match(line);
                    if (match.Success && int.TryParse(match.Groups[1].Value, out int port))
                    {
                        lock (openPorts)
                        {
                            if (!openPorts.Contains(port))
                            {
                                openPorts.Add(port);
                                SetStatus("OPEN PORTS");
                            }
                        }
                    }
                }

                if (!proc.WaitForExit(5000))
                    proc.Kill();
            }
        }

        private void guna2Panel1_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            int w = guna2Panel1.ClientSize.Width;
            int h = guna2Panel1.ClientSize.Height;

            using (Font font = new Font("Comic Sans MS", 8.25f, FontStyle.Bold))
            using (Brush brush = new SolidBrush(Color.White))
            {
                var centerMessages = new HashSet<string>
                {
                    "ENTER VALID IP/DOMAIN", "INVALID IP/DOMAIN", "SCANNING...",
                    "NMAP NOT FOUND", "INVALID PORT RANGE", "VALID RANGE SET", "NO OPEN PORTS FOUND.", "SCAN CANCELLED", "SCAN ERROR"
                };

                if (centerMessages.Contains(scanStatus))
                {
                    SizeF sz = g.MeasureString(scanStatus, font);
                    g.DrawString(scanStatus.ToUpper(), font, brush, (w - sz.Width) / 2, (h - sz.Height) / 2);
                    return;
                }

                if (scanStatus == "OPEN PORTS")
                {
                    string header = "OPEN PORTS:";
                    float y = 10;
                    SizeF szHeader = g.MeasureString(header, font);
                    g.DrawString(header.ToUpper(), font, brush, (w - szHeader.Width) / 2, y);
                    y += szHeader.Height + 5;

                    List<int> snapshot;
                    lock (openPorts)
                    {
                        snapshot = openPorts.OrderBy(p => p).ToList();
                    }
                    foreach (var port in snapshot)
                    {
                        string portStr = port.ToString();
                        SizeF szPort = g.MeasureString(portStr, font);
                        g.DrawString(portStr, font, brush, (w - szPort.Width) / 2, y);
                        y += szPort.Height + 3;
                    }

                    return;
                }

                if (!hasScanned && string.IsNullOrWhiteSpace(rawInput))
                {
                    string defaultText = "PORTS & LOGS";
                    SizeF sz = g.MeasureString(defaultText, font);
                    g.DrawString(defaultText.ToUpper(), font, brush, (w - sz.Width) / 2, (h - sz.Height) / 2);
                    return;
                }

                string fallback = "PORTS & LOGS";
                SizeF fallbackSize = g.MeasureString(fallback, font);
                g.DrawString(fallback.ToUpper(), font, brush, (w - fallbackSize.Width) / 2, (h - fallbackSize.Height) / 2);
            }
        }

        private void guna2TextBox1_TextChanged(object sender, EventArgs e)
        {
            string portText = guna2TextBox1.Text.Trim();

            if (string.IsNullOrWhiteSpace(portText))
            {
                SetStatus("PORT RANGE");
                customPortRange = "65501-65535";
                return;
            }

            Regex regex = new Regex(@"^(\d{1,5})\s*-\s*(\d{1,5})$");

            if (regex.IsMatch(portText))
            {
                var match = regex.Match(portText);
                int start = int.Parse(match.Groups[1].Value);
                int end = int.Parse(match.Groups[2].Value);

                if (start >= 0 && start <= 65535 && end >= 0 && end <= 65535 && start < end)
                {
                    customPortRange = $"{start}-{end}";
                    if (!string.IsNullOrWhiteSpace(ip.Text.Trim()))
                    {
                        SetStatus("VALID RANGE SET");
                    }
                    return;
                }
            }

            SetStatus("INVALID PORT RANGE");
        }

        private void guna2Button1_Click(object sender, EventArgs e)
        {
            ip.Text = "";
            ip.PlaceholderText = "IP/DOMAIN";
            ip.PlaceholderForeColor = Color.White;
            ip.ForeColor = Color.White;

            guna2TextBox1.Text = "";
            guna2TextBox1.PlaceholderText = "PORT RANGE";
            guna2TextBox1.PlaceholderForeColor = Color.White;
            guna2TextBox1.ForeColor = Color.White;
            rawInput = "";
            customPortRange = "65501-65535"; 
            scanStatus = "PORTS & LOGS";     
            guna2Panel1.Invalidate();
        }


    }
}
