using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using EGIS.Projections;

namespace EGIS.Controls
{
    /// <summary>
    /// UserControl to Select a Coordinate Reference System
    /// </summary>
    public partial class CRSSelectionControl : UserControl
    {
        private ICRSFactory crsFactory;
        private ICRS selectedCRS;

        public CRSSelectionControl()
        {
            InitializeComponent();                              
        }


        #region public properties and methods

        public void LoadCoordinateSystems(ICRSFactory crsFactory, List<int> recentCRSList = null)
        {
            ICRS crs = SelectedCRS;
            this.crsFactory = crsFactory;

            LoadCoordinateSystems();

            if (crs != null)
            {
                SelectedCRS = crs;
            }

            LoadRecentCRSList(recentCRSList);
        }

        public void LoadRecentCRSList(List<int> recentList)
        {
            this.lstRecentCRS.Items.Clear();
            if (recentList == null) return;
            foreach (int id in recentList)
            {
                ICRS crs = FindCRSByEpgsCode(id);
                if (crs != null) lstRecentCRS.Items.Add(crs);
            }
        }

        public ICRS SelectedCRS
        {
            get
            {
                return this.selectedCRS;
            }
            set
            {
                this.selectedCRS = value;
                UpdateSelectedCRS(true);
            }
        }

       
        #endregion

        private void LoadCoordinateSystems()
        {
            //this.cbSelectedCRS.Items.Clear();
            this.cbSelectedCRS.DataSource = null;//.Clear();
            if (this.crsFactory == null) return;

            
            if (this.rbGeographic.Checked)
            {
                var dataSource = crsFactory.GeographicCoordinateSystems.OrderBy(o => o.Name).ToList();
                string filter = this.txtFilter.Text.Trim();
                if (!string.IsNullOrEmpty(filter))
                {
                    filter = filter.ToLower();
                    dataSource = dataSource.FindAll(x => x.Name.ToLower().Contains(filter));

                }
                this.cbSelectedCRS.DataSource = dataSource;
                //this.cbSelectedCRS.Items.AddRange(crsFactory.GeographicCoordinateSystems.OrderBy(o => o.Name).ToArray());
            }
            else if (this.rbProjected.Checked)
            {
                var dataSource = crsFactory.ProjectedCoordinateSystems.OrderBy(o => o.Name).ToList();
                string filter = this.txtFilter.Text.Trim();
                if (!string.IsNullOrEmpty(filter))
                {
                    filter = filter.ToLower();
                    dataSource = dataSource.FindAll(x => x.Name.ToLower().Contains(filter));

                }
                this.cbSelectedCRS.DataSource = dataSource;
                //this.cbSelectedCRS.Items.AddRange(crsFactory.ProjectedCoordinateSystems.OrderBy(o => o.Name).ToArray());               
            }
            if (this.cbSelectedCRS.Items.Count > 0)
            {
                this.cbSelectedCRS.SelectedIndex = 0;
            }
        }

        private void UpdateSelectedCRS( bool findCrs = false)
        {
            if (this.selectedCRS == null)
            {
                this.txtWKT.Text = "";
            }
            else
            {
                this.txtWKT.Text = this.selectedCRS.WKT;
                int code = 0;
                int.TryParse(this.selectedCRS.Id, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out code);
                this.nudEPGS.Value = code;

                if (findCrs && crsFactory!= null)
                {
                    int index = crsFactory.GeographicCoordinateSystems.OrderBy(o => o.Name).ToList().FindIndex(crs => crs.Id == this.selectedCRS.Id);
                    if (index >= 0)
                    {
                        this.rbGeographic.Checked = true;
                        LoadCoordinateSystems();
                        this.cbSelectedCRS.SelectedIndex = index;
                    }
                    else
                    {
                        index = crsFactory.ProjectedCoordinateSystems.OrderBy(o => o.Name).ToList().FindIndex(crs => crs.Id == this.selectedCRS.Id);
                        if (index >= 0)
                        {
                            this.rbProjected.Checked = true;
                            LoadCoordinateSystems();
                            this.cbSelectedCRS.SelectedIndex = index;
                        }
                    }
                }                
            }
        }

        private void cbSelectedCRS_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbSelectedCRS.SelectedIndex >= 0)
            {
                this.selectedCRS = cbSelectedCRS.SelectedItem as ICRS;
                UpdateSelectedCRS();
            }
        }

        private void btnFindEPGS_Click(object sender, EventArgs e)
        {
            this.selectedCRS = FindCRSByEpgsCode((int)nudEPGS.Value);
            UpdateSelectedCRS();
        }

        private ICRS FindCRSByEpgsCode(int code)
        {
            if(this.crsFactory == null) return null;

            return this.crsFactory.GetCRSById(code);

        }

        

        private void rbGeographic_CheckedChanged(object sender, EventArgs e)
        {
            LoadCoordinateSystems();
        }

        private void rbProjected_CheckedChanged(object sender, EventArgs e)
        {
            LoadCoordinateSystems();
        }

        private void lstRecentCRS_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstRecentCRS.SelectedIndex >= 0)
            {
                this.selectedCRS = lstRecentCRS.SelectedItem as ICRS;
                UpdateSelectedCRS(true);
            }
        }

		private void txtFilter_TextChanged(object sender, EventArgs e)
		{
            LoadCoordinateSystems();
		}
	}
}
