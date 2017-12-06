#region Copyright and License

/****************************************************************************
**
** Copyright (C) 2008 - 2011 Winston Fletcher.
** All rights reserved.
**
** This file is part of the EGIS.ShapeFileLib class library of Easy GIS .NET.
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
using System.Drawing;

namespace EGIS.ShapeFileLib
{
    internal class QuadTree
    {
        private QTNode rootNode;

        public QuadTree(RectangleF bounds)
        {
            rootNode = new QTNode(bounds, 0);
        }

        public void Insert(int recordIndex, QTNodeHelper helper, System.IO.Stream shapeFileStream)
        {
            if (helper.IsPointData())
            {
                rootNode.Insert(recordIndex, helper, shapeFileStream);
            }
            else
            {
                RectangleD recBounds = helper.GetRecordBoundsD(recordIndex, shapeFileStream);
                //check for zero width or height to avoid issue when checking rectangle intersection
                //if width/height is zero
                if (recBounds.Width < 0.0000001)
                {
                    recBounds.Width = 0.0000001;
                }
                if (recBounds.Height < 0.0000001)
                {
                    recBounds.Height = 0.0000001;
                }
                rootNode.Insert(recordIndex, helper, ref recBounds, shapeFileStream);
            }
        }


        public List<int> GetIndices(PointD pt)
        {
            if (rootNode.Bounds.Contains(pt)) return rootNode.GetIndices(pt);
            return null;
        }


        public List<int> GetIndices(ref RectangleD rect)
        {
            if (rootNode.Bounds.IntersectsWith(rect))
            {
                List<int> indices = new List<int>();
                Dictionary<int, int> duplicates = new Dictionary<int, int>();
                rootNode.GetIndices(ref rect, indices, duplicates);
                return indices;
            } 
            return null;
        }

        public List<int> GetIndices(PointD centre, double radius)
        {
            RectangleD r = rootNode.Bounds;
            if (GeometryAlgorithms.RectangleCircleIntersects(ref r, ref centre, radius))
            {
                List<int> indices = new List<int>();
                Dictionary<int, int> duplicates = new Dictionary<int, int>();
                rootNode.GetIndices(centre, radius, indices, duplicates);
                return indices;
            }
            return null;
        }


    }

    internal class QTNode
    {
        private const int TL = 0;
        private const int TR = 1;
        private const int BL = 2;
        private const int BR = 3;

        public static int MaxLevels = 7;
        //public static int MinLevels = 2;
        //public static int MaxIndicesPerNode = 16;


        private RectangleD _bounds;
        private QTNode[] children;
        private int _level = 0;
        private List<int> indexList;

        public QTNode(RectangleD bounds, int level)
        {
            this._bounds = bounds;
            this._level = level;
            //if (level < MinLevels)
            //{
            //    //create children
            //}
            if (Level == MaxLevels)
            {                
                indexList = new List<int>();                
            }
        }

        public RectangleD Bounds
        {
            get
            {
                return _bounds;
            }            
        }

        public int Level
        {
            get
            {
                return _level;
            }
            //set
            //{
            //    _level = value;
            //}
        }


        private void CreateChildren()
        {
            children = new QTNode[4];
            children[TL] = new QTNode(RectangleD.FromLTRB(_bounds.Left, _bounds.Top, 0.5 * (_bounds.Left + _bounds.Right), 0.5 * (_bounds.Top + _bounds.Bottom)), Level + 1);
            children[TR] = new QTNode(RectangleD.FromLTRB(0.5 * (_bounds.Left + _bounds.Right),_bounds.Top,_bounds.Right, 0.5 * (_bounds.Top + _bounds.Bottom)), Level + 1);
            children[BL] = new QTNode(RectangleD.FromLTRB(_bounds.Left, 0.5 * (_bounds.Top + _bounds.Bottom), 0.5 * (_bounds.Left + _bounds.Right), _bounds.Bottom), Level + 1);
            children[BR] = new QTNode(RectangleD.FromLTRB(0.5 * (_bounds.Left + _bounds.Right), 0.5 * (_bounds.Top + _bounds.Bottom), _bounds.Right, _bounds.Bottom), Level + 1);
        }

        public void Insert(int recordIndex, QTNodeHelper helper, System.IO.Stream shapeFileStream)
        {
            if (Level == MaxLevels)
            {                
                indexList.Add(recordIndex);
            }
            else
            {                
                if(helper.IsPointData())
                {
                    PointD pt = helper.GetRecordPoint(recordIndex, shapeFileStream);
                    
                    if(children == null)
                    {
                        CreateChildren();
                    }
                    
                    if (children[TL].Bounds.Contains(pt))
                    {
                        children[TL].Insert(recordIndex, helper, shapeFileStream);
                    }
                     if (children[TR].Bounds.Contains(pt))
                    {
                        children[TR].Insert(recordIndex, helper, shapeFileStream);
                    }
                     if (children[BL].Bounds.Contains(pt))
                    {
                        children[BL].Insert(recordIndex, helper, shapeFileStream);
                    }
                     if (children[BR].Bounds.Contains(pt))
                    {
                        children[BR].Insert(recordIndex, helper, shapeFileStream);
                    }
                    //else
                    //{
                    //    throw new InvalidOperationException("point " + pt + " is not contained in children bounds");
                    //}                    
                }
                else
                {
                    RectangleD recBounds = helper.GetRecordBoundsD(recordIndex, shapeFileStream);

                    if (children == null)
                    {
                        CreateChildren();
                    }
                    int c = 0;
                    if (children[TL].Bounds.IntersectsWith(recBounds))
                    {
                        c++;
                        children[TL].Insert(recordIndex, helper, shapeFileStream);
                    }
                    if (children[TR].Bounds.IntersectsWith(recBounds))
                    {
                        c++;
                        children[TR].Insert(recordIndex, helper, shapeFileStream);
                    }
                    if (children[BL].Bounds.IntersectsWith(recBounds))
                    {
                        c++;
                        children[BL].Insert(recordIndex, helper, shapeFileStream);
                    }
                    if (children[BR].Bounds.IntersectsWith(recBounds))
                    {
                        c++;
                        children[BR].Insert(recordIndex, helper, shapeFileStream);
                    }                    
                }
            }
            

        }

        internal void Insert(int recordIndex, QTNodeHelper helper, ref RectangleD recBounds, System.IO.Stream shapeFileStream)
        {
            if (Level == MaxLevels)
            {
                indexList.Add(recordIndex);
            }
            else
            {
                if (helper.IsPointData())
                {
                }
                else
                {
                    //RectangleF recBounds = helper.GetRecordBounds(recordIndex, shapeFileStream);

                    if (children == null)
                    {
                        CreateChildren();
                    }
                    int c = 0;
                    if (children[TL].Bounds.IntersectsWith(recBounds))
                    {
                        c++;
                        children[TL].Insert(recordIndex, helper, ref recBounds, shapeFileStream);
                    }
                    if (children[TR].Bounds.IntersectsWith(recBounds))
                    {
                        c++;
                        children[TR].Insert(recordIndex, helper,ref recBounds, shapeFileStream);
                    }
                    if (children[BL].Bounds.IntersectsWith(recBounds))
                    {
                        c++;
                        children[BL].Insert(recordIndex, helper,ref recBounds, shapeFileStream);
                    }
                    if (children[BR].Bounds.IntersectsWith(recBounds))
                    {
                        c++;
                        children[BR].Insert(recordIndex, helper,ref recBounds, shapeFileStream);
                    }
                }
            }


        }

        public List<int> GetIndices(PointD pt)
        {
            if (children != null)
            {
                //check each child bounds
                if (children[TL].Bounds.Contains(pt))
                {
                    return children[TL].GetIndices(pt);
                }
                else if (children[TR].Bounds.Contains(pt))
                {
                    return children[TR].GetIndices(pt);
                }
                else if (children[BL].Bounds.Contains(pt))
                {
                    return children[BL].GetIndices(pt);
                }
                else if (children[BR].Bounds.Contains(pt))
                {
                    return children[BR].GetIndices(pt);
                }
                else
                {
                    return null;
                }
            }
            else
            {              
                //assumes already checked node's Bounds contains pt
                return this.indexList;                
            }

        }

        internal void GetIndices(ref RectangleD rect, List<int> indices, System.Collections.Generic.Dictionary<int, int> foundIndicies)
        {
            if (children != null)
            {
                //check each child bounds
                if (children[TL].Bounds.IntersectsWith(rect))
                {
                    children[TL].GetIndices(ref rect, indices, foundIndicies);
                }
                if (children[TR].Bounds.IntersectsWith(rect))
                {
                    children[TR].GetIndices(ref rect, indices, foundIndicies);
                }
                if (children[BL].Bounds.IntersectsWith(rect))
                {
                    children[BL].GetIndices(ref rect, indices, foundIndicies);
                }
                if (children[BR].Bounds.IntersectsWith(rect))
                {
                    children[BR].GetIndices(ref rect, indices, foundIndicies);
                }                
            }
            else
            {
                if (indexList != null)
                {
                    //assumes already checked node's Bounds intersect rect
                    //add the node'x indices, checking if it has already been added
                    //We need to check for duplicates as a shape may intersect more than 1 node
                    for (int n = indexList.Count - 1; n >= 0; --n)
                    {
                        if (!foundIndicies.ContainsKey(indexList[n]))
                        {
                            indices.Add(indexList[n]);
                            foundIndicies.Add(indexList[n], 0);
                        }
                    }
                }
            }

        }


        internal void GetIndices(PointD centre, double radius, List<int> indices, System.Collections.Generic.Dictionary<int, int> foundIndicies)
        {
            if (children != null)
            {
                //check each child bounds
                if (GeometryAlgorithms.RectangleCircleIntersects(ref children[TL]._bounds, ref centre, radius))
                {
                    children[TL].GetIndices(centre, radius, indices, foundIndicies);
                }
                if (GeometryAlgorithms.RectangleCircleIntersects(ref children[TR]._bounds, ref centre, radius))
                {
                    children[TR].GetIndices(centre, radius, indices, foundIndicies);
                }
                if (GeometryAlgorithms.RectangleCircleIntersects(ref children[BL]._bounds, ref centre, radius))                
                {
                    children[BL].GetIndices(centre, radius, indices, foundIndicies);
                }
                if (GeometryAlgorithms.RectangleCircleIntersects(ref children[BR]._bounds, ref centre, radius))                
                {
                    children[BR].GetIndices(centre, radius, indices, foundIndicies);
                }
            }
            else
            {
                if (indexList != null)
                {
                    //assumes already checked node's Bounds intersect rect
                    //add the node'x indices, checking if it has already been added
                    //We need to check for duplicates as a shape may intersect more than 1 node
                    for (int n = indexList.Count - 1; n >= 0; --n)
                    {
                        if (!foundIndicies.ContainsKey(indexList[n]))
                        {
                            indices.Add(indexList[n]);
                            foundIndicies.Add(indexList[n], 0);
                        }
                    }
                }
            }

        }

    }

    interface QTNodeHelper
    {
        bool IsPointData();
        PointD GetRecordPoint(int recordIndex, System.IO.Stream shapeFileStream);
        RectangleD GetRecordBoundsD(int recordIndex, System.IO.Stream shapeFileStream);
    }
}
