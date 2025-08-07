using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FastDexTool
{
    public partial class finder : Form
    {
        public finder()
        {
            InitializeComponent();
            guna2Panel1.Paint += guna2Panel1_Paint;

        }
        private void guna2Panel1_Paint(object sender, PaintEventArgs e)
        {
            string texto = "SOON...";
            using (Font fuente = new Font("Comic Sans MS", 8.25F, FontStyle.Bold))
            using (Brush pincel = new SolidBrush(Color.White))
            {
                SizeF size = e.Graphics.MeasureString(texto, fuente);
                float x = (guna2Panel1.Width - size.Width) / 2;
                float y = (guna2Panel1.Height - size.Height) / 2;

                e.Graphics.DrawString(texto, fuente, pincel, x, y);
            }
        }

    }
}
