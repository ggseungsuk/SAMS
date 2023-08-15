using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SAMS
{
    public partial class FormSetting : Form
    {
        public FormSetting()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            FormMain.zoneRadius = Convert.ToInt32(guna2TextBox1.Text);
        }

        private void guna2TextBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            //숫자(백스페이스)만 입력되도록 필터링    
            if(!(char.IsDigit(e.KeyChar) || e.KeyChar == Convert.ToChar(Keys.Back)))
            {        
                e.Handled = true;    
            }
        }

        private void FormSetting_Load(object sender, EventArgs e)
        {
            guna2TextBox1.Text = FormMain.zoneRadius.ToString();
            chkShowArrowCtrl.Checked = FormMain.showArrowCtrl;
            chkShowSidemap.Checked = FormMain.showSidemap;
        }

        private void chkShowArrowCtrl_CheckedChanged(object sender, EventArgs e)
        {
            FormMain.showArrowCtrl = chkShowArrowCtrl.Checked;
        }

        private void chkShowSidemap_CheckedChanged(object sender, EventArgs e)
        {
            FormMain.showSidemap = chkShowSidemap.Checked;
        }
    }
}
