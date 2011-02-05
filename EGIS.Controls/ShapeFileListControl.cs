#region Copyright and License

/****************************************************************************
**
** Copyright (C) 2008 - 2011 Winston Fletcher.
** All rights reserved.
**
** This file is part of the EGIS.Controls class library of Easy GIS .NET.
** 
** Easy GIS .NET is free software: you can redistribute it and/or modify
** it under the terms of the GNU Lesser General Public License version 3 as
** published by the Free Software Foundation and appearing in the file
** lgpl-license.txt included in the packaging of this file.
**
** Easy GIS .NET is distributed in the hope that it will be useful,
** but WITHOUT ANY WARRANTY; without even the implied warranty of
** MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
** GNU General Public License for more details.
**
** You should have received a copy of the GNU General Public License and
** GNU Lesser General Public License along with Easy GIS .NET.
** If not, see <http://www.gnu.org/licenses/>.
**
****************************************************************************/

#endregion


using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace EGIS.Controls
{
    /// <summary>
    /// User control containing a list of ShapeFiles
    /// </summary>
    public partial class ShapeFileListControl : UserControl
    {
        public ShapeFileListControl()
        {
            InitializeComponent();

            ToolTip toolTip = new ToolTip();            
            // Set up the ToolTip text for the Button and Checkbox.
            toolTip.SetToolTip(this.btnMoveUp, "Move selected layer up in drawing order");
            toolTip.SetToolTip(this.btnMoveDown, "Move selected layer down in drawing order");
            toolTip.SetToolTip(this.btnRemove, "Remove selected layer from map");
            toolTip.SetToolTip(this.button1, "Add new layer to map");

        }

        #region events

        /// <summary>
        /// Selected ShapeFile Changed event
        /// </summary>
        public event EventHandler<EventArgs> SelectedShapeFileChanged;

        protected void OnSelectedShapeFileChanged()
        {
            if (SelectedShapeFileChanged != null)
            {
                //to do: check if invoke required
                SelectedShapeFileChanged(this, new EventArgs());
            }
        }

        /// <summary>
        /// Event fired when the add layer button is clicked 
        /// </summary>
        public event EventHandler AddLayerClicked;

        /// <summary>
        /// Called when AddLayer button is clicked. Fires the AddLayerClicked event
        /// </summary>
        protected void OnAddLayerClicked()
        {
            if (AddLayerClicked != null)
            {
                AddLayerClicked(this, new EventArgs());
            }
        }

        #endregion


        private void btnMoveUp_Click(object sender, EventArgs e)
        {
            if (_map != null && lstShapefiles.SelectedItem != null)
            {
                EGIS.ShapeFileLib.ShapeFile sf = lstShapefiles.SelectedItem as EGIS.ShapeFileLib.ShapeFile;
                _map.MoveShapeFileUp(lstShapefiles.SelectedItem as EGIS.ShapeFileLib.ShapeFile);
                lstShapefiles.SelectedItem = sf;
            }
        }

        private void btnMoveDown_Click(object sender, EventArgs e)
        {
            if (_map != null && lstShapefiles.SelectedItem != null)
            {
                EGIS.ShapeFileLib.ShapeFile sf = lstShapefiles.SelectedItem as EGIS.ShapeFileLib.ShapeFile;
                _map.MoveShapeFileDown(lstShapefiles.SelectedItem as EGIS.ShapeFileLib.ShapeFile);
                lstShapefiles.SelectedItem = sf;
            }

        }

        private void btnRemove_Click(object sender, EventArgs e)
        {
            if (_map != null && lstShapefiles.SelectedItem != null)
            {
                if (MessageBox.Show(this, "Remove Layer " + lstShapefiles.SelectedItem.ToString() + "?", "Confirm Layer Removal", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    EGIS.ShapeFileLib.ShapeFile sf = lstShapefiles.SelectedItem as EGIS.ShapeFileLib.ShapeFile;
                    
                    _map.RemoveShapeFile(sf);
                    sf.Close();
                }
            }

        }

        private SFMap _map;

        /// <summary>
        /// Reference to the SFMap that the ShapeFileListControl layers is associated with
        /// </summary>
        public SFMap Map
        {
            get
            {
                return _map;
            }
            set
            {
                SetMap(value);
            }
        }

        /// <summary>
        /// Returns the currently selected ShapeFile in the controls list of 
        /// shapefiles
        /// </summary>
        public EGIS.ShapeFileLib.ShapeFile SelectedShapeFile
        {
            get
            {
                return this.lstShapefiles.SelectedItem as EGIS.ShapeFileLib.ShapeFile;
            }
        }

        /// <summary>
        /// called when the Map property is set
        /// </summary>
        /// <param name="map"></param>
        protected virtual void SetMap(SFMap map)
        {
            this._map = map;
            if (map != null)
            {
                map.ShapeFilesChanged += new EventHandler<EventArgs>(map_ShapeFilesChanged);//new ShapeFilesChangedEventHandler(map_ShapeFilesChanged);
            }

        }

        private void map_ShapeFilesChanged(object sender, EventArgs args)
        {
            this.lstShapefiles.Items.Clear();
            if(Map == null) return;
            EGIS.ShapeFileLib.ShapeFile[] shapefiles = new EGIS.ShapeFileLib.ShapeFile[_map.ShapeFileCount];
            for (int n = 0; n < shapefiles.Length; n++)
            {
                shapefiles[n] = _map[n];
            }
            this.lstShapefiles.Items.AddRange(shapefiles);
            if (lstShapefiles.Items.Count > 0)
            {
                this.lstShapefiles.SelectedIndex = 0;
            }
        }

        private void lstShapefiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.OnSelectedShapeFileChanged();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OnAddLayerClicked();
        }
    }
}
