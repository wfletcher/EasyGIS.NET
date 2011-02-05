#region Copyright and License

/****************************************************************************
**
** Copyright (C) 2008 - 2011 Winston Fletcher.
** All rights reserved.
**
** This file is part of Easy GIS .NET Desktop Edition.
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
using System.Windows.Forms;

namespace egis
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            MainForm f = new MainForm();            
            if (args.Length == 1 && !string.IsNullOrEmpty(args[0]))
            {
                if (args[0].EndsWith(".egp", StringComparison.OrdinalIgnoreCase))
                {
                    f.OpenProject(args[0]);
                }
                else if (args[0].EndsWith(".shp", StringComparison.OrdinalIgnoreCase) ||
                    args[0].EndsWith(".shpx", StringComparison.OrdinalIgnoreCase))
                {
                    f.OpenShapeFile(args[0]);
                }
            }

            Application.Run(f);

        }
        
    }
}