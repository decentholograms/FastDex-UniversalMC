using Guna.UI2.WinForms;
using Newtonsoft.Json.Linq;
using System;
using System.Drawing;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FastDexTool
{
    public partial class userinfo : Form
    {
        private readonly HttpClient client = new HttpClient();
        private CancellationTokenSource cts;

        string nickname = "";
        string premiumStatus = "";
        string playerId = "";
        private string uuidText = "UUID";
        private string offlineUuidText = "OFFLINE UUID";
        private string searchStatusText = "SEARCH STATUS";
        private JArray nameHistory = null;

        private readonly string placeholder = "NICK";
        private bool isPlaceholderActive = true;

        public userinfo()
        {
            InitializeComponent();

            PremiumCheck.Paint += PremiumCheck_Paint;
            Alts.Paint += Alts_Paint;

            guna2TextBox1.Font = new Font("Comic Sans MS", 8.25f, FontStyle.Bold);
            guna2TextBox1.TextAlign = HorizontalAlignment.Center; 
            SetPlaceholder();

            guna2TextBox1.GotFocus += guna2TextBox1_GotFocus;
            guna2TextBox1.LostFocus += guna2TextBox1_LostFocus;
            guna2Panel2.Click += guna2Panel2_Click;
            guna2Panel3.Click += guna2Panel3_Click;
            guna2TextBox1.TextChanged += guna2TextBox1_TextChanged;
        }

        private void SetPlaceholder()
        {
            guna2TextBox1.Text = placeholder;
            guna2TextBox1.ForeColor = Color.White;
            isPlaceholderActive = true;
        }

        private void RemovePlaceholder()
        {
            guna2TextBox1.Text = "";
            isPlaceholderActive = false;
        }

        private void guna2TextBox1_GotFocus(object sender, EventArgs e)
        {
            if (isPlaceholderActive)
            {
                RemovePlaceholder();
            }
        }

        private void guna2TextBox1_LostFocus(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(guna2TextBox1.Text))
            {
                SetPlaceholder();
            }
        }

        private void guna2TextBox1_TextChanged(object sender, EventArgs e)
        {
            if (!isPlaceholderActive)
            {
                nickname = guna2TextBox1.Text.Trim();
            }
        }


        private void Search_Click(object sender, EventArgs e)
        {
            cts?.Cancel();
            cts = new CancellationTokenSource();
            _ = SearchAsync(nickname, cts.Token);
        }

        private async Task SearchAsync(string nick, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(nick)) return;

            searchStatusText = "SEARCHING...";
            guna2Panel4.Invalidate();

            try
            {
                await Task.Delay(300, token);
            }
            catch (TaskCanceledException)
            {
                searchStatusText = "SEARCH STATUS";
                guna2Panel4.Invalidate();
                return;
            }

            try
            {
                string url = $"https://api.mojang.com/users/profiles/minecraft/{nick}";
                HttpResponseMessage response = await client.GetAsync(url, token);

                if (token.IsCancellationRequested)
                {
                    searchStatusText = "SEARCH STATUS";
                    guna2Panel4.Invalidate();
                    return;
                }

                if (!response.IsSuccessStatusCode)
                {
                    SetNoPremiumState(token);
                }
                else
                {
                    string json = await response.Content.ReadAsStringAsync();
                    JObject jsonObj = JObject.Parse(json);

                    if (jsonObj["id"] != null)
                    {
                        premiumStatus = "Premium";
                        playerId = jsonObj["id"].ToString();

                        string formattedUuid =
                            $"{playerId.Substring(0, 8)}-" +
                            $"{playerId.Substring(8, 4)}-" +
                            $"{playerId.Substring(12, 4)}-" +
                            $"{playerId.Substring(16, 4)}-" +
                            $"{playerId.Substring(20)}";

                        uuidText = formattedUuid;
                        await LoadNameHistoryAsync(playerId, token);
                        await LoadPlayerImageAsync($"https://crafatar.com/renders/body/{playerId}?overlay", token);
                    }
                    else
                    {
                        SetNoPremiumState(token);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                searchStatusText = "SEARCH STATUS";
                guna2Panel4.Invalidate();
                return;
            }
            catch
            {
                SetNoPremiumState(token);

                searchStatusText = "NETWORK ERROR";
                guna2Panel4.Invalidate();
                return;
            }

            try
            {
                using (var md5 = System.Security.Cryptography.MD5.Create())
                {
                    byte[] inputBytes = Encoding.UTF8.GetBytes("OfflinePlayer:" + nick);
                    byte[] hashBytes = md5.ComputeHash(inputBytes);

                    byte[] uuidBytes = new byte[16];
                    Array.Copy(hashBytes, uuidBytes, 16);

                    uuidBytes[6] &= 0x0F;
                    uuidBytes[6] |= 0x30;
                    uuidBytes[8] &= 0x3F;
                    uuidBytes[8] |= 0x80;

                    Guid offlineGuid = new Guid(uuidBytes);
                    offlineUuidText = offlineGuid.ToString();
                }
            }
            catch
            {
                offlineUuidText = "OFFLINE UUID";
            }

            if (!token.IsCancellationRequested)
            {
                searchStatusText = "SEARCH COMPLETED";
                PremiumCheck.Invalidate();
                guna2Panel2.Invalidate();
                guna2Panel3.Invalidate();
                guna2Panel4.Invalidate();
            }
        }

        private async void SetNoPremiumState(CancellationToken token)
        {
            premiumStatus = "NoPremium";
            playerId = "";
            nameHistory = null;
            uuidText = "UUID";
            Alts.Invalidate();

            await LoadPlayerImageAsync(
                "https://crafatar.com/renders/body/8667ba71b85a4004af54457a9734eed7?overlay",
                token
            );
        }


        private void Alts_Paint(object sender, PaintEventArgs e)
        {
            using (Font fontName = new Font("Comic Sans MS", 8f, FontStyle.Bold))
            using (Font fontDate = new Font("Comic Sans MS", 7f, FontStyle.Regular))
            using (Brush brushName = new SolidBrush(Color.White))
            using (Brush brushDate = new SolidBrush(Color.LightGray))
            {
                if (premiumStatus != "Premium" || nameHistory == null || nameHistory.Count == 0)
                {
                    string text = "MULTI-ACCOUNTS";
                    SizeF sz = e.Graphics.MeasureString(text, fontName);
                    float x = (Alts.Width - sz.Width) / 2;
                    float y = (Alts.Height - sz.Height) / 2;
                    e.Graphics.DrawString(text, fontName, brushName, x, y);
                    return;
                }

                float yPos = 5f;
                float lineSpacing = 2f;

                foreach (var item in nameHistory)
                {
                    string name = item["name"]?.ToString() ?? "";
                    string changedAtRaw = item["changed_at"]?.ToString();
                    string lastSeenRaw = item["last_seen_at"]?.ToString();

                    string changedAt = FormatDateTime(changedAtRaw);
                    string lastSeenAt = FormatDateTime(lastSeenRaw);

                    e.Graphics.DrawString(name, fontName, brushName, 5, yPos);
                    yPos += fontName.Height + lineSpacing / 2;

                    if (!string.IsNullOrEmpty(changedAt))
                    {
                        e.Graphics.DrawString($"Since: {changedAt}", fontDate, brushDate, 7, yPos);
                        yPos += fontDate.Height + lineSpacing / 2;
                    }

                    if (!string.IsNullOrEmpty(lastSeenAt))
                    {
                        e.Graphics.DrawString($"Last seen: {lastSeenAt}", fontDate, brushDate, 7, yPos);
                        yPos += fontDate.Height + lineSpacing;
                    }
                    else
                    {
                        yPos += lineSpacing;
                    }
                }
            }
        }

        private async Task LoadPlayerImageAsync(string imageUrl, CancellationToken token)
        {
            try
            {
                var response = await client.GetAsync(imageUrl, token);
                response.EnsureSuccessStatusCode();
                var imageBytes = await response.Content.ReadAsByteArrayAsync();

                if (token.IsCancellationRequested) return;

                using (var ms = new System.IO.MemoryStream(imageBytes))
                {
                    var img = Image.FromStream(ms);

                    UserPhoto.Invoke((Action)(() =>
                    {
                        UserPhoto.Image?.Dispose();
                        UserPhoto.Image = img;
                    }));
                }
            }
            catch
            {
            }
        }

        private async Task LoadNameHistoryAsync(string id, CancellationToken token)
        {
            try
            {
                string url = $"https://laby.net/api/v2/user/{id}/get-profile";
                HttpResponseMessage response = await client.GetAsync(url, token);
                if (!response.IsSuccessStatusCode)
                {
                    nameHistory = null;
                    return;
                }
                string json = await response.Content.ReadAsStringAsync();
                JObject obj = JObject.Parse(json);
                nameHistory = (JArray)obj["name_history"];
            }
            catch
            {
                nameHistory = null;
            }
            finally
            {
                Alts.Invoke((Action)(() => Alts.Invalidate()));
            }
        }

        private void PremiumCheck_Paint(object sender, PaintEventArgs e)
        {
            string textToShow = "ACCOUNT TYPE";

            if (premiumStatus == "Premium")
                textToShow = "PREMIUM";
            else if (premiumStatus == "NoPremium")
                textToShow = "NOT-PREMIUM";
            else if (premiumStatus == "Error")
                textToShow = "ERROR";

            if (string.IsNullOrEmpty(textToShow)) return;

            using (Font font = new Font("Comic Sans MS", 8.25f, FontStyle.Bold))
            using (Brush brush = new SolidBrush(Color.White))
            {
                SizeF textSize = e.Graphics.MeasureString(textToShow, font);
                float x = (PremiumCheck.Width - textSize.Width) / 2;
                float y = (PremiumCheck.Height - textSize.Height) / 2;
                e.Graphics.DrawString(textToShow, font, brush, x, y);
            }
        }

        private string FormatDateTime(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "";

            if (DateTime.TryParse(raw, null, DateTimeStyles.AdjustToUniversal, out DateTime dt))
            {
                return dt.ToLocalTime().ToString("MM/dd/yyyy HH:mm");
            }
            return raw;
        }

        private void UserPhoto_Click(object sender, EventArgs e)
        {
        }

        private void guna2Panel2_Paint(object sender, PaintEventArgs e)
        {
            DrawCenteredText(e, guna2Panel2, offlineUuidText);
        }

        private void guna2Panel3_Paint(object sender, PaintEventArgs e)
        {
            DrawCenteredText(e, guna2Panel3, uuidText);
        }

        private void guna2Panel2_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(offlineUuidText) && offlineUuidText != "OFFLINE UUID")
            {
                Clipboard.SetText(offlineUuidText);
            }
        }

        private void guna2Panel3_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(uuidText) && uuidText != "UUID")
            {
                Clipboard.SetText(uuidText);
            }
        }

        private void DrawCenteredText(PaintEventArgs e, Control control, string text)
        {
            if (string.IsNullOrEmpty(text)) return;

            using (Font font = new Font("Comic Sans MS", 8.25f, FontStyle.Bold))
            using (Brush brush = new SolidBrush(Color.White))
            {
                SizeF textSize = e.Graphics.MeasureString(text, font);
                float x = (control.Width - textSize.Width) / 2;
                float y = (control.Height - textSize.Height) / 2;
                e.Graphics.DrawString(text, font, brush, x, y);
            }
        }

        private void userinfo_Load(object sender, EventArgs e)
        {

        }

        private void guna2Panel4_Paint(object sender, PaintEventArgs e)
        {
            DrawCenteredText(e, guna2Panel4, searchStatusText);
        }

        private void guna2Panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void guna2Button1_Click(object sender, EventArgs e)
        {
            SetPlaceholder();
            uuidText = "UUID";
            offlineUuidText = "OFFLINE UUID";
            searchStatusText = "SEARCH STATUS";
            nameHistory = null;

            premiumStatus = "";
            playerId = "";
            PremiumCheck.Invalidate();
            guna2Panel2.Invalidate();
            guna2Panel3.Invalidate();
            guna2Panel4.Invalidate();
            Alts.Invalidate();
            var ctsToken = new CancellationTokenSource();
            _ = LoadPlayerImageAsync("https://crafatar.com/renders/body/207d9eba20b54e778e7bfea47e5c6c13?overlay", ctsToken.Token);
        }
    }
}