using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace egis
{
    public partial class LicenceAgreementForm : Form
    {
        public LicenceAgreementForm()
        {
            InitializeComponent();
            this.rtbLicence.Text = Properties.Settings.Default.LicenceText;

        }
    }
}