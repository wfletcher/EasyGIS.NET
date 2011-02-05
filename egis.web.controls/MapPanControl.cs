#region Copyright and License

/****************************************************************************
**
** Copyright (C) 2008 - 2011 Winston Fletcher.
** All rights reserved.
**
** This file is part of the EGIS.Web.controls class library of Easy GIS .NET.
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
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace EGIS.Web.Controls
{
    /// <summary>
    /// Web Control which provides pan and zoom control for a map
    /// </summary>
    /// <remarks>    
    /// <para>The MapPanControl control is a composite control that provides 4 pan buttons (left, right, up, down), a zoom-in and a zoom-out button.</para>
    /// <para>The control can be customized by specifying a new Image Url for each of the pan or zoom buttons.</para>
    /// <para> Each button is rendered as an "input" HTML element. To specify the CSS style of the buttons specify a class for the control and supply a style for the .class input, as shown below</para>
    /// <code>
    /// &lt;style type="text/css"&gt; .mpc input {padding:2px;} &lt;/style&gt;
    /// ..
    /// &lt;cc1:MapPanControl class="mpc" ID="MapPanControl1" runat="server" /&gt; </code>
    /// </remarks>
    public class MapPanControl : CompositeControl
    {

        private ImageButton btnPanUp, btnPanLeft, btnPanRight, btnPanDown;
        private ImageButton btnZoomIn, btnZoomOut;

        /// <summary>
        /// MapPanControl constructor
        /// </summary>
        public MapPanControl()
        {
        }

        
        private ISFMap mapReference;

        /// <summary>
        /// Sets a reference to the SFMap Web Control that the MapPanControl will interact with
        /// </summary>
        /// <param name="map">the SFMap control</param>
        public void SetMap(ISFMap map)
        {
            mapReference = map;
            if (map != null)
            {
                this.MapReferenceId = map.ControlId;
            }

        }

        
        #region Protected Methods

        /// <summary>
        /// returns the javascript code in order to pan the map left
        /// </summary>
        protected string PanLeftClientJS
        {
            get
            {
                
                return mapReference==null?"":"return panLeft('" + mapReference.GisImageClientId + "')";
            }
        }

        /// <summary>
        /// returns the javascript code in order to pan the map right
        /// </summary>
        protected string PanRightClientJS
        {
            get
            {
                return mapReference == null ? "" : "return panRight('" + mapReference.GisImageClientId + "')";
            }
        }

        /// <summary>
        /// returns the javascript code in order to pan the map up
        /// </summary>
        protected string PanUpClientJS
        {
            get
            {
                return mapReference == null ? "" : "return panUp('" + mapReference.GisImageClientId + "')";
            }
        }

        /// <summary>
        /// returns the javascript code in order to pan the map down
        /// </summary>
        protected string PanDownClientJS
        {
            get
            {
                return mapReference == null ? "" : "return panDown('" + mapReference.GisImageClientId + "')";
            }
        }

        /// <summary>
        /// returns the javascript code in order to zoom in to the map (X2)
        /// </summary>
        protected string ZoomInClientJS
        {
            get
            {
                return mapReference == null ? "" : "return zoomIn('" + mapReference.GisImageClientId + "')";
            }
        }

        /// <summary>
        /// returns the javascript code in order to zoom out of the map (X2)
        /// </summary>
        protected string ZoomOutClientJS
        {
            get
            {
                return mapReference == null ? "" : "return zoomOut('" + mapReference.GisImageClientId + "')";
            }
        }

        #endregion

        private static Control FindControlRecursive(Control Root, string Id)
        {
            if (Root.ID == Id)
                return Root;
            foreach (Control Ctl in Root.Controls)
            {
                Control FoundCtl = FindControlRecursive(Ctl, Id);
                if (FoundCtl != null)
                    return FoundCtl;
            }
            return null;
        }


        private ISFMap FindSFMap()
        {
            if (MapReferenceId == null) return null;
            ISFMap map = this.Page.FindControl(MapReferenceId) as ISFMap;
            if (map == null && this.Page.Master != null)
            {
                //check masterpage
                map = FindControlRecursive(this.Page.Master, MapReferenceId) as ISFMap;
            }
            return map;
        }

        /// <summary>
        /// overrides OnPreRender in CompositeControl
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPreRender(EventArgs e)
        {
            
            this.Style[HtmlTextWriterStyle.TextAlign] = "center";
            
            if (mapReference == null && MapReferenceId!= null)
            {
                mapReference = this.FindSFMap();
            }

            if (mapReference != null)
            {
                string egisScriipt = mapReference.ClientJSResouceName;
                ClientScriptManager csm = this.Page.ClientScript;
                csm.RegisterClientScriptResource(this.GetType(), egisScriipt);

                btnPanLeft.OnClientClick = PanLeftClientJS;
                btnPanRight.OnClientClick = PanRightClientJS;
                btnPanUp.OnClientClick = PanUpClientJS;
                btnPanDown.OnClientClick = PanDownClientJS;
                btnZoomIn.OnClientClick = ZoomInClientJS;
                btnZoomOut.OnClientClick = ZoomOutClientJS;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("could not find " + MapReferenceId);
            }

            base.OnPreRender(e);
        }

        /// <summary>
        /// overrides CreateChildControls in CompositeControl
        /// </summary>
        protected override void CreateChildControls()
        {
            btnPanUp = new ImageButton();
            btnPanUp.ID = "panUp";
            if (!string.IsNullOrEmpty(this.panUpImageUrl))
            {
                btnPanUp.ImageUrl = this.panUpImageUrl;
            }
            else
            {
                btnPanUp.ImageUrl = Page.ClientScript.GetWebResourceUrl(this.GetType(), "EGIS.Web.Controls.panu.gif");
            }
            this.Controls.Add(btnPanUp);

            btnPanLeft = new ImageButton();
            btnPanLeft.ID = "panLeft";
            if (!string.IsNullOrEmpty(this.panLeftImageUrl))
            {
                btnPanLeft.ImageUrl = this.panLeftImageUrl;
            }
            else
            {
                btnPanLeft.ImageUrl = Page.ClientScript.GetWebResourceUrl(this.GetType(), "EGIS.Web.Controls.panl.gif");
            }
            this.Controls.Add(btnPanLeft);

            btnPanRight = new ImageButton();
            btnPanRight.ID = "panRight";
            if (!string.IsNullOrEmpty(this.panRightImageUrl))
            {
                btnPanRight.ImageUrl = this.panRightImageUrl;
            }
            else
            {
                btnPanRight.ImageUrl = Page.ClientScript.GetWebResourceUrl(this.GetType(), "EGIS.Web.Controls.panr.gif");
            }
            this.Controls.Add(btnPanRight);

            btnPanDown = new ImageButton();
            btnPanDown.ID = "panDown";
            if (!string.IsNullOrEmpty(this.panDownImageUrl))
            {
                btnPanDown.ImageUrl = this.panDownImageUrl;
            }
            else
            {
                btnPanDown.ImageUrl = Page.ClientScript.GetWebResourceUrl(this.GetType(), "EGIS.Web.Controls.pand.gif");
            }
            this.Controls.Add(btnPanDown);

            btnZoomIn = new ImageButton();
            btnZoomIn.ID = "zoomIn";
            if (!string.IsNullOrEmpty(this.zoomInImageUrl))
            {
                btnZoomIn.ImageUrl = this.zoomInImageUrl;
            }
            else
            {
                btnZoomIn.ImageUrl = Page.ClientScript.GetWebResourceUrl(this.GetType(), "EGIS.Web.Controls.zoomin.gif");
            }
            this.Controls.Add(btnZoomIn);

            btnZoomOut = new ImageButton();
            btnZoomOut.ID = "zoomOut";
            if (!string.IsNullOrEmpty(this.zoomOutImageUrl))
            {
                btnZoomOut.ImageUrl = this.zoomOutImageUrl;
            }
            else
            {
                btnZoomOut.ImageUrl = Page.ClientScript.GetWebResourceUrl(this.GetType(), "EGIS.Web.Controls.zoomout.gif");
            }
            this.Controls.Add(btnZoomOut);
            
            base.CreateChildControls();
        }

        /// <summary>
        /// overrides RenderChildren in CompositeControl
        /// </summary>
        /// <param name="writer"></param>
        protected override void RenderChildren(HtmlTextWriter writer)
        {
            if (HasControls())
            {
                btnPanUp.RenderControl(writer);
                writer.WriteBreak();
                btnPanLeft.RenderControl(writer);
                btnPanRight.RenderControl(writer);
                writer.WriteBreak();
                btnPanDown.RenderControl(writer);
                writer.WriteBreak();
                btnZoomIn.RenderControl(writer);
                btnZoomOut.RenderControl(writer);

            }            
        }

        /// <summary>
        /// overrides TagKey in WebControl. returns div tag
        /// </summary>
        protected override HtmlTextWriterTag TagKey
        {
            get
            {
                return HtmlTextWriterTag.Div;
            }
        }

        private string panLeftImageUrl;
        private string panRightImageUrl;
        private string panUpImageUrl;
        private string panDownImageUrl;
        private string zoomInImageUrl;
        private string zoomOutImageUrl;

        private string mapReferenceId;

        /// <summary>
        /// gets/sets the ID of the ISFMap that the pan control will interact with
        /// </summary>
        [
        Bindable(true),
        Category("Custom Settings"),
        Description("ID of Map that the Pan Control interacts with")
        ]
        public string MapReferenceId
        {
            get
            {
                return mapReferenceId;
            }
            set
            {
                mapReferenceId = value;
            }
        }


        #region Public Properties to set Button Images

        /// <summary>
        /// Gets or sets the Url of an image to use for the "Pan Left" button
        /// </summary>
        [EditorAttribute(typeof(System.Web.UI.Design.UrlEditor), typeof(System.Drawing.Design.UITypeEditor)), 
        Bindable(true),
        Category("Custom Settings"),
        Description("Url of Image used for the 'Left' pan button.")
        ]
        public string PanLeftImageUrl
        {
            get
            {
                return panLeftImageUrl;
            }
            set
            {
                panLeftImageUrl = value;
            }
        }

        /// <summary>
        /// Gets or sets the Url of an image to use for the "Pan Right" button
        /// </summary>        
        [EditorAttribute(typeof(System.Web.UI.Design.UrlEditor), typeof(System.Drawing.Design.UITypeEditor)),
        Bindable(true),
        Category("Custom Settings"),
        Description("Url of Image used for the 'Right' pan button.")
        ]
        public string PanRightImageUrl
        {
            get
            {
                return panRightImageUrl;
            }
            set
            {
                panRightImageUrl = value;
            }
        }

        /// <summary>
        /// Gets or sets the Url of an image to use for the "Pan Up" button
        /// </summary>        
        [EditorAttribute(typeof(System.Web.UI.Design.UrlEditor), typeof(System.Drawing.Design.UITypeEditor)),
        Bindable(true),
        Category("Custom Settings"),
        Description("Url of Image used for the 'Up' pan button.")
        ]
        public string PanUpImageUrl
        {
            get
            {
                return panUpImageUrl;
            }
            set
            {
                panUpImageUrl = value;
            }
        }

        /// <summary>
        /// Gets or sets the Url of an image to use for the "Pan Down" button
        /// </summary>        
        [EditorAttribute(typeof(System.Web.UI.Design.UrlEditor), typeof(System.Drawing.Design.UITypeEditor)), 
        Bindable(true),
        Category("Custom Settings"),
        Description("Url of Image used for the 'Down' pan button.")
        ]
        public string PanDownImageUrl
        {
            get
            {
                return panDownImageUrl;
            }
            set
            {
                panDownImageUrl = value;
            }
        }

        /// <summary>
        /// Gets or sets the Url of an image to use for the "Zoom In" button
        /// </summary>        
        [EditorAttribute(typeof(System.Web.UI.Design.UrlEditor), typeof(System.Drawing.Design.UITypeEditor)), 
        Bindable(true),
        Category("Custom Settings"),
        Description("Url of Image used for the 'Zoom In' pan button.")
        ]
        public string ZoomInImageUrl
        {
            get
            {
                return zoomInImageUrl;
            }
            set
            {
                zoomInImageUrl = value;
            }
        }

        /// <summary>
        /// Gets or sets the Url of an image to use for the "Zoom Out" button
        /// </summary>        
        [EditorAttribute(typeof(System.Web.UI.Design.UrlEditor), typeof(System.Drawing.Design.UITypeEditor)),
        Bindable(true),
        Category("Custom Settings"),
        Description("Url of Image used for the 'Zoom Out' pan button.")
        ]
        public string ZoomOutImageUrl
        {
            get
            {
                return zoomOutImageUrl;
            }
            set
            {
                zoomOutImageUrl = value;
            }
        }

        #endregion

    }

}
