using Guna.UI2.WinForms;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FastDexTool
{
    public partial class deasher : Form
    {
        private string placeholderHash = "ENCRYPTED PASSWORD";
        private Font defaultFont = new Font("Comic Sans MS", 8.25f, FontStyle.Bold);
        private Brush defaultBrush = Brushes.White;
        private bool typingHash = false;
        private bool typingSalt = false;
        private string panelResultText = "DEASH RESULT";
        private string panelStatusText = "DEASH STATUS";

        private string wordlistPath = null;

        public deasher()
        {
            InitializeComponent();

            ip.ForeColor = Color.White;
            ip.PlaceholderText = placeholderHash;
            ip.TextAlign = HorizontalAlignment.Center;

            ip.TextChanged += ip_TextChanged;

            ip.Paint += ip_Paint;

            guna2Panel2.Click += guna2Panel2_Click;

            ip.Invalidate();
            ip.ForeColor = Color.White;
            ip.PlaceholderForeColor = Color.White;
        }

        private void ip_TextChanged(object sender, EventArgs e)
        {
            typingHash = !string.IsNullOrWhiteSpace(ip.Text);
            ip.Invalidate();
        }

        private void ip_Paint(object sender, PaintEventArgs e)
        {
            if (!typingHash && string.IsNullOrWhiteSpace(ip.Text) && !ip.Focused)
            {
                string text = placeholderHash.ToUpper();
                SizeF sz = e.Graphics.MeasureString(text, defaultFont);
                e.Graphics.DrawString(text, defaultFont, defaultBrush,
                    (ip.Width - sz.Width) / 2, (ip.Height - sz.Height) / 2);
            }
        }

        private void guna2Panel1_Paint(object sender, PaintEventArgs e)
        {
            string text = panelStatusText.ToUpper();
            SizeF sz = e.Graphics.MeasureString(text, defaultFont);
            e.Graphics.DrawString(text, defaultFont, defaultBrush,
                (guna2Panel1.Width - sz.Width) / 2, (guna2Panel1.Height - sz.Height) / 2);
        }

        private void guna2Panel2_Paint(object sender, PaintEventArgs e)
        {
            string text = panelResultText.ToUpper();
            SizeF sz = e.Graphics.MeasureString(text, defaultFont);
            e.Graphics.DrawString(text, defaultFont, defaultBrush,
                (guna2Panel2.Width - sz.Width) / 2, (guna2Panel2.Height - sz.Height) / 2);
        }

        private void guna2Panel2_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(panelResultText))
            {
                Clipboard.SetText(panelResultText);
            }
        }

        private void guna2Button8_Click(object sender, EventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog
            {
                Filter = "Text files (*.txt)|*.txt",
                Title = "Select a Wordlist"
            };

            if (fileDialog.ShowDialog() == DialogResult.OK)
            {
                wordlistPath = fileDialog.FileName;
            }
        }
        private (string algorithm, string hash, string salt) ParseInputs(string primary)
        {
            string algorithm = null;
            string hash = null;
            string salt = null;

            if (string.IsNullOrEmpty(primary))
                return (null, null, null);

            string[] rawParts = primary.Split('$');
            if (rawParts.Length >= 4)
            {
                string maybeAlgo = rawParts[1].ToUpperInvariant();
                if (maybeAlgo == "SHA512" || maybeAlgo == "SHA256" || maybeAlgo == "SHA1" || maybeAlgo == "MD5")
                {
                    algorithm = maybeAlgo;
                    string a = rawParts[2];
                    string b = rawParts[3];
                    if (a.Length >= b.Length)
                    {
                        hash = a;
                        salt = b;
                    }
                    else
                    {
                        hash = b;
                        salt = a;
                    }
                    return (algorithm, hash, salt);
                }
            }

            string[] parts = primary.Split(new[] { '$' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2)
            {
                if (parts[0].Length >= parts[1].Length)
                {
                    hash = parts[0];
                    salt = parts[1];
                }
                else
                {
                    hash = parts[1];
                    salt = parts[0];
                }
            }
            else if (parts.Length == 1)
            {
                if (IsLikelyHash(parts[0]))
                    hash = parts[0];
                else
                    salt = parts[0];
            }

            return (algorithm, hash, salt);
        }

        private bool IsLikelyHash(string candidate)
        {
            return !string.IsNullOrEmpty(candidate) && candidate.Length >= 32;
        }

        private async void Start_Click(object sender, EventArgs e)
        {
            string rawPrimary = ip.Text.Trim();
            var (algorithm, hash, salt) = ParseInputs(rawPrimary);

            if (string.IsNullOrWhiteSpace(hash))
            {
                panelResultText = "ENTER A HASH";
                guna2Panel2.Invalidate();
                await Task.Delay(5000);
                panelResultText = "DEASH RESULT";
                guna2Panel2.Invalidate();
                return;
            }

            if (string.IsNullOrWhiteSpace(wordlistPath) || !File.Exists(wordlistPath))
            {
                panelResultText = "ENTER A WORDLIST";
                guna2Panel2.Invalidate();
                await Task.Delay(5000);
                panelResultText = "DEASH RESULT";
                guna2Panel2.Invalidate();
                return;
            }

            string roamingDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
            string pythonScript = Path.Combine(roamingDir, "FastDexData", "Utils", "Deash.py");
            string combined = !string.IsNullOrWhiteSpace(salt) ? $"{hash}${salt}" : hash;

            string arguments = $"\"{pythonScript}\" {combined} \"{wordlistPath}\"";

            panelStatusText = "DEASHING...";
            guna2Panel1.Invalidate();

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/C python {arguments}",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            try
            {
                using (Process proc = Process.Start(psi))
                {
                    string output = await proc.StandardOutput.ReadToEndAsync();
                    proc.WaitForExit();

                    if (output.Contains("HASH_RESULT:NO_WORDLIST"))
                        panelResultText = "NO WORDLIST";
                    else if (output.Contains("HASH_RESULT:NO_DEASHED"))
                        panelResultText = "NO DEASHED";
                    else if (output.Contains("HASH_RESULT:"))
                        panelResultText = output.Split(new[] { "HASH_RESULT:" }, StringSplitOptions.None)[1].Trim();
                    else
                        panelResultText = "UNKNOWN RESULT";

                    panelStatusText = "DEASH COMPLETED";
                }
            }
            catch (Exception)
            {
                panelResultText = "ERROR";
                panelStatusText = "FAILED";
            }

            guna2Panel1.Invalidate();
            guna2Panel2.Invalidate();
        }

        private void deasher_Load(object sender, EventArgs e)
        {

        }

        private void guna2Button1_Click(object sender, EventArgs e)
        {
            ip.Text = "";
            typingHash = false;
            typingSalt = false;
            wordlistPath = null;
            panelResultText = "DEASH RESULT";
            panelStatusText = "DEASH STATUS";
            ip.Invalidate();
            guna2Panel1.Invalidate();
            guna2Panel2.Invalidate();
        }
    }
}