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

        private const int MaxRecentCRSListSize = 12;

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
            this.crsSelectionControl1.LoadCoordinateSystems(this.crsFactory != null ? this.crsFactory : CoordinateReferenceSystemFactory.Default, GetRecentCRSList());
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            if (this.DialogResult == System.Windows.Forms.DialogResult.OK)
            {
                if (crsSelectionControl1.SelectedCRS != null) AddToRecentCRSList(crsSelectionControl1.SelectedCRS.Id);
            }
        }

        private static List<int> GetRecentCRSList()
        {
            if (Properties.Settings.Default.RecentCRSList == null) Properties.Settings.Default.RecentCRSList = new System.Collections.Specialized.StringCollection();

            List<int> resentList = new List<int>();
            foreach (String crs in Properties.Settings.Default.RecentCRSList)
            {
                int id;
                if (int.TryParse(crs, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out id))
                {
                    resentList.Add(id);
                }
            }
            return resentList;
        }

        private static void AddToRecentCRSList(string crs)
        {
            if (Properties.Settings.Default.RecentCRSList == null) Properties.Settings.Default.RecentCRSList = new System.Collections.Specialized.StringCollection();

            var recentList = Properties.Settings.Default.RecentCRSList;

            //check if the crsId already exists in the recentList
            int index = recentList.IndexOf(crs);
            if (index == 0) return; //already first element in the list. just return
            if (index > 0) recentList.RemoveAt(index);
            recentList.Insert(0, crs);

            //limit the number of entries we store in the recent list
            while(recentList.Count > MaxRecentCRSListSize)
            {
                recentList.RemoveAt(recentList.Count - 1);
            }
            Properties.Settings.Default.Save();
        }



    }
}
