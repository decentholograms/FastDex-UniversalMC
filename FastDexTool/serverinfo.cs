using Newtonsoft.Json.Linq;
using System;
using System.Drawing;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FastDexTool
{
    public partial class serverinfo : Form
    {
        private string fetchedIp = null;
        private string fetchedPort = null;
        private string fetchedProtocolVersion = null;
        private string fetchedCleanInfo = null; 
        private string fetchedVersionName = null;
        private bool? fetchedBedrock = null;
        private int? fetchedOnline = null;
        private string searchStatus = "SEARCH STATUS";
        private int? fetchedMax = null;
        private bool isServerOnline = false;
        private string lastHostInput = null;

        private readonly Font drawFont = new Font("Comic Sans MS", 8.25f, FontStyle.Bold);
        private readonly Brush drawBrush = Brushes.White;

        public serverinfo()
        {
            InitializeComponent();

            void ApplyStyle(Control.ControlCollection cols)
            {
                foreach (Control c in cols)
                {
                    if (c is Label || c is TextBox || c is Guna.UI2.WinForms.Guna2TextBox)
                    {
                        c.ForeColor = Color.White;
                        c.Font = new Font("Comic Sans MS", 8.25f, FontStyle.Bold);
                    }
                    if (c.HasChildren)
                        ApplyStyle(c.Controls);
                }
            }
            ApplyStyle(this.Controls);

            guna2TextBox1.PlaceholderText = "IP:PORT/DOMAIN";
            guna2TextBox1.PlaceholderForeColor = Color.White;
            guna2TextBox1.Font = new Font("Comic Sans MS", 8.25f, FontStyle.Bold);
            guna2TextBox1.TextAlign = HorizontalAlignment.Center;

        }

        private void UserPhoto_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(UserPhoto.ImageLocation))
            {
                UserPhoto.SizeMode = PictureBoxSizeMode.StretchImage;
                UserPhoto.LoadAsync();
            }
        }


        private void guna2TextBox1_TextChanged(object sender, EventArgs e)
        {
        }

        private string serverProtectionStatus = "";

        private async Task<string> CheckProtectionAsync(string server)
        {
            using (var client = new HttpClient())
            {
                try
                {
                    string url = $"https://networkcalc.com/api/dns/lookup/{server}";
                    var response = await client.GetStringAsync(url);
                    string resLower = response.ToLower();

                    if (resLower.Contains("cloudflare"))
                        return "CLOUDFARE";
                    else if (resLower.Contains("tcpshield"))
                        return "TCPSHIELD";
                    else if (resLower.Contains("neoprotect"))
                        return "NEOPROTECT";
                    else
                        return "NOT-PROTECTED";
                }
                catch
                {
                    return "NOT-PROTECTED";
                }
            }
        }

        private async void Search_Click(object sender, EventArgs e)
        {
            string input = guna2TextBox1.Text.Trim();
            if (string.IsNullOrEmpty(input))
                return;

            try
            {
                lastHostInput = input;
                searchStatus = "SEARCHING...";
                guna2Panel9.Invalidate();

                serverProtectionStatus = await CheckProtectionAsync(input);

                string url = $"https://api.mcsrvstat.us/3/{input}";

                using (var cli = new HttpClient())
                {
                    cli.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) FastDexToolClient/1.0");
                    var resp = await cli.GetStringAsync(url);
                    var json = JObject.Parse(resp);

                    fetchedIp = json["ip"]?.Value<string>();
                    fetchedPort = json["port"]?.Value<string>();

                    var protocol = json["protocol"];
                    fetchedProtocolVersion = protocol?["version"]?.Value<string>();
                    fetchedVersionName = protocol?["name"]?.Value<string>();

                    var bedrockToken = json["bedrock"];
                    fetchedBedrock = bedrockToken != null && bedrockToken.Type == JTokenType.Boolean && bedrockToken.Value<bool>();

                    var players = json["players"];
                    fetchedOnline = players?["online"]?.Value<int>();
                    fetchedMax = players?["max"]?.Value<int>();

                    var onlineToken = json["online"];
                    isServerOnline = onlineToken != null && onlineToken.Type == JTokenType.Boolean && onlineToken.Value<bool>();

                    hasQueried = true;

                    var infoCleanArray = json["info"]?["clean"] as JArray;
                    if (infoCleanArray != null && infoCleanArray.Count > 0)
                    {
                        fetchedCleanInfo = infoCleanArray[0].ToString();
                    }
                    else
                    {
                        fetchedCleanInfo = null;
                    }

                    searchStatus = "SEARCH COMPLETED";
                    RefreshPanels();
                    guna2Panel9.Invalidate();

                    string hostSeg = lastHostInput.Contains(":")
                        ? lastHostInput.Split(':')[0]
                        : lastHostInput;
                    string portSeg = lastHostInput.Contains(":")
                        ? lastHostInput.Split(':')[1]
                        : fetchedPort;

                    string bannerUrl = $"http://status.mclive.eu/FastDex/{hostSeg}/{portSeg}/banner.png";
                    UserPhoto.ImageLocation = bannerUrl;

                    if (!isServerOnline)
                    {
                        UserPhoto.SizeMode = PictureBoxSizeMode.StretchImage;
                        UserPhoto.Size = new Size(313, 41);
                    }
                    else
                    {
                        UserPhoto.SizeMode = PictureBoxSizeMode.Zoom;
                        UserPhoto.Size = new Size(313, 41);
                    }
                }
            }
            catch (Exception ex)
            {
                isServerOnline = false;
                UserPhoto.Image = null;
                fetchedBedrock = null;

                searchStatus = "ERROR OCCURRED";
                guna2Panel9.Invalidate();
                guna2Panel7.Refresh();
            }
        }


        private void guna2Panel10_Paint(object sender, PaintEventArgs e)
        {
            string text = string.IsNullOrEmpty(serverProtectionStatus) ? "PROTECTED STATUS" : serverProtectionStatus;
            SizeF sz = e.Graphics.MeasureString(text, drawFont);

            float x = (guna2Panel10.Width - sz.Width) / 2;
            float y = (guna2Panel10.Height - sz.Height) / 2;

            e.Graphics.DrawString(text, drawFont, drawBrush, x, y);
        }


        private void RefreshPanels()
        {
            Alts.Invalidate();
            guna2Panel2.Invalidate();
            guna2Panel3.Invalidate();
            guna2Panel4.Invalidate();
            guna2Panel5.Invalidate();
            guna2Panel6.Invalidate();
            guna2Panel7.Invalidate();
            guna2Panel8.Invalidate();
            guna2Panel10.Invalidate();
        }

        private void Alts_Paint(object sender, PaintEventArgs e)
        {
            string text = fetchedIp ?? "IP";
            SizeF sz = e.Graphics.MeasureString(text, drawFont);
            e.Graphics.DrawString(text, drawFont, drawBrush,
                (Alts.Width - sz.Width) / 2,
                (Alts.Height - sz.Height) / 2);
        }

        private void guna2Panel2_Paint(object sender, PaintEventArgs e)
        {
            string text = fetchedPort ?? "PORT";
            SizeF sz = e.Graphics.MeasureString(text, drawFont);
            e.Graphics.DrawString(text, drawFont, drawBrush,
                (guna2Panel2.Width - sz.Width) / 2,
                (guna2Panel2.Height - sz.Height) / 2);
        }

        private void guna2Panel3_Paint(object sender, PaintEventArgs e)
        {
            string text = fetchedProtocolVersion ?? "PROTOCOL";
            SizeF sz = e.Graphics.MeasureString(text, drawFont);
            e.Graphics.DrawString(text, drawFont, drawBrush,
                (guna2Panel3.Width - sz.Width) / 2,
                (guna2Panel3.Height - sz.Height) / 2);
        }

        private void guna2Panel4_Paint(object sender, PaintEventArgs e)
        {
            string text = fetchedVersionName ?? "VERSION";
            SizeF sz = e.Graphics.MeasureString(text, drawFont);
            e.Graphics.DrawString(text, drawFont, drawBrush,
                (guna2Panel4.Width - sz.Width) / 2,
                (guna2Panel4.Height - sz.Height) / 2);
        }

        private void guna2Panel5_Paint(object sender, PaintEventArgs e)
        {
            string servtypetext = "SERVER TYPE";
            if (fetchedBedrock.HasValue)
            {
                servtypetext = fetchedBedrock.Value ? "BEDROCK" : "JAVA";
            }

            SizeF sz = e.Graphics.MeasureString(servtypetext, drawFont);
            e.Graphics.DrawString(servtypetext, drawFont, drawBrush,
                (guna2Panel5.Width - sz.Width) / 2,
                (guna2Panel5.Height - sz.Height) / 2);
        }



        private void guna2Panel6_Paint(object sender, PaintEventArgs e)
        {
            string text = (fetchedOnline.HasValue && fetchedMax.HasValue)
                ? $"{fetchedOnline}/{fetchedMax}"
                : "PLAYERS";
            SizeF sz = e.Graphics.MeasureString(text, drawFont);
            e.Graphics.DrawString(text, drawFont, drawBrush,
                (guna2Panel6.Width - sz.Width) / 2,
                (guna2Panel6.Height - sz.Height) / 2);
        }

        private bool hasQueried = false;

        private void guna2Panel7_Paint(object sender, PaintEventArgs e)
        {
            string text = !hasQueried ? "SERV STATUS" : (isServerOnline ? "ONLINE" : "OFFLINE");

            SizeF sz = e.Graphics.MeasureString(text, drawFont);
            e.Graphics.DrawString(text, drawFont, drawBrush,
                (guna2Panel7.Width - sz.Width) / 2,
                (guna2Panel7.Height - sz.Height) / 2);
        }

        private void guna2Panel8_Paint(object sender, PaintEventArgs e)
        {
            string text = !string.IsNullOrEmpty(fetchedCleanInfo) ? fetchedCleanInfo : "DESCRIPTION";

            SizeF sz = e.Graphics.MeasureString(text, drawFont);
            e.Graphics.DrawString(text, drawFont, drawBrush,
                (guna2Panel8.Width - sz.Width) / 2,
                (guna2Panel8.Height - sz.Height) / 2);
        }

        private void guna2Panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void guna2Panel9_Paint(object sender, PaintEventArgs e)
        {
            string text = searchStatus ?? "SEARCH STATUS";
            SizeF sz = e.Graphics.MeasureString(text, drawFont);
            e.Graphics.DrawString(text, drawFont, drawBrush,
                (guna2Panel9.Width - sz.Width) / 2,
                (guna2Panel9.Height - sz.Height) / 2);
        }

        private void ResetAllStatus()
        {
            fetchedIp = null;
            fetchedPort = null;
            fetchedProtocolVersion = null;
            fetchedCleanInfo = null;
            fetchedVersionName = null;
            fetchedBedrock = null;
            fetchedOnline = null;
            fetchedMax = null;
            isServerOnline = false;
            searchStatus = "SEARCH STATUS";
            serverProtectionStatus = null;
            lastHostInput = null;

            guna2TextBox1.PlaceholderText = "IP:PORT/DOMAIN";
            guna2TextBox1.PlaceholderForeColor = Color.White;
            guna2TextBox1.Font = new Font("Comic Sans MS", 8.25f, FontStyle.Bold);
            guna2TextBox1.TextAlign = HorizontalAlignment.Center;
            guna2TextBox1.Text = "";

            string bannerUrl = $"http://status.mclive.eu/FastDex/FastDex.net/banner.png";
            UserPhoto.ImageLocation = bannerUrl;
            UserPhoto.SizeMode = PictureBoxSizeMode.StretchImage;
            UserPhoto.Size = new Size(313, 41);

            guna2Panel2.Invalidate();
            guna2Panel3.Invalidate();
            guna2Panel4.Invalidate();
            guna2Panel5.Invalidate();
            guna2Panel6.Invalidate();
            guna2Panel7.Invalidate();
            guna2Panel8.Invalidate();
            guna2Panel9.Invalidate();
            guna2Panel10.Invalidate();
            Alts.Invalidate();
        }


        private void guna2Button1_Click(object sender, EventArgs e)
        {
            ResetAllStatus();
        }
    }
}