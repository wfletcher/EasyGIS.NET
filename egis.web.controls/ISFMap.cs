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
using System.Text;

namespace EGIS.Web.Controls
{
    /// <summary>
    /// Interface defining methods needed by MapPanControl to interact with SFMap and TiledSFMap classes.
    /// </summary>
    public interface ISFMap
    {
        /// <summary>
        /// the Id of the MapControl
        /// </summary>
        string ControlId
        {
            get;
        }

        /// <summary>
        /// The client Id of the Map's GIS Image
        /// </summary>
        string GisImageClientId
        {
            get;
        }

        /// <summary>
        /// the name of the Javascript resource used by the ISFMap, which is different depending on
        /// whether a TiledSfMap or simple SFMap is used
        /// </summary>
        string ClientJSResouceName
        {
            get;
        }
    }
}
