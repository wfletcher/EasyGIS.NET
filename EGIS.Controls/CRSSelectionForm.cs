using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using EGIS.Projections;

namespace EGIS.Controls
{
    public partial class CRSSelectionForm : Form
    {
        private ICRSFactory crsFactory;

        public CRSSelectionForm()
        {
            InitializeComponent();
        }

        public CRSSelectionForm(ICRSFactory crsFactory)
            : this()
        {
            this.crsFactory = crsFactory;
            
        }



        public ICRS SelectedCRS
        {
            get
            {
                return this.crsSelectionControl1.SelectedCRS;
            }
            set
            {
                this.crsSelectionControl1.SelectedCRS = value;
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            this.crsSelectionControl1.LoadCoordinateSystems(this.crsFactory != null ? this.crsFactory : CoordinateReferenceSystemFactory.Default);
        }
    }
}
