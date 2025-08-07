using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DiscordRpcDemo;

namespace FastDexTool
{
    public partial class FastDex : Form
    {
        private DiscordRpc.EventHandlers handlers;
        private DiscordRpc.RichPresence presence;
        public FastDex()
        {
            InitializeComponent();
            Loadform(new initial(), mainpanel);
            var sesionForm = new session();
            sesionForm.LogPanel = log; 
            Loadform(sesionForm, log);
        }

        private fakeproxy screenFakeProxy = new fakeproxy();
        private scanner screenScanner = new scanner();
        private serverinfo screenServer = new serverinfo();
        private userinfo screenUser = new userinfo();
        private deasher screendeasher = new deasher();

        public void Loadform(object form, Panel targetPanel)
        {
            if (targetPanel.Controls.Count > 0)
                targetPanel.Controls.RemoveAt(0);

            Form f = form as Form;
            f.TopLevel = false;
            f.Dock = DockStyle.Fill;
            targetPanel.Controls.Add(f);
            targetPanel.Tag = f;
            f.Show();
        }


        private void Form1_Load(object sender, EventArgs e)
        {
            this.handlers = default(DiscordRpc.EventHandlers);
            DiscordRpc.Initialize("1401133481383301180", ref this.handlers, true, null);
            this.handlers = default(DiscordRpc.EventHandlers);
            DiscordRpc.Initialize("1401133481383301180", ref this.handlers, true, null);

            this.presence.details = "Minecraft Pentesting Tool";
            this.presence.state = ".gg/aemEcES9CN";
            this.presence.largeImageKey = "fastdex-2";
            this.presence.smallImageKey = "z";
            this.presence.largeImageText = "FastDex UniversalMC";
            this.presence.smallImageText = "V1.0 Beta";
            DiscordRpc.UpdatePresence(ref this.presence);
        }

        private void Close_Click(object sender, EventArgs e)
        {

        }

        private void guna2Button1_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }


        private void guna2Panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void Logo_Click(object sender, EventArgs e)
        {

        }

        private void Logo1_Click(object sender, EventArgs e)
        {

        }

        private void guna2Button1_Click_1(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void Deash_Click(object sender, EventArgs e)
        {
            Loadform(screendeasher, mainpanel);
        }

        private void Finder_Click(object sender, EventArgs e)
        {
            Loadform(new finder(), mainpanel);
        }

        private void Scanner_Click(object sender, EventArgs e)
        {
            Loadform(screenScanner, mainpanel);
        }

        private void Scrapper_Click(object sender, EventArgs e)
        {
            Loadform(screenServer, mainpanel);
        }

        private void iconButton1_Click(object sender, EventArgs e)
        {
            Loadform(screenUser, mainpanel);
        }

        private void Home_Click(object sender, EventArgs e)
        {
            Loadform(new initial(), mainpanel);
        }

        private void FakeProxy_Click(object sender, EventArgs e)
        {
            Loadform(screenFakeProxy, mainpanel);
        }

        private void Discord_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://discord.gg/6ZCdV29w6n");
        }

        private void Logo_Click_1(object sender, EventArgs e)
        {

        }

        private void guna2Button1_Click_2(object sender, EventArgs e)
        {
            this.TopMost = !this.TopMost;
        }

    }
}
