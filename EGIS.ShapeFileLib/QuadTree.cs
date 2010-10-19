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
                RectangleF recBounds = helper.GetRecordBounds(recordIndex, shapeFileStream);
                rootNode.Insert(recordIndex, helper, ref recBounds, shapeFileStream);
            }
        }


        public List<int> GetIndices(PointF pt)
        {
            if (rootNode.Bounds.Contains(pt)) return rootNode.GetIndices(pt);
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


        private RectangleF _bounds;
        private QTNode[] children;
        private int _level = 0;
        private List<int> indexList;

        public QTNode(RectangleF bounds, int level)
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

        public RectangleF Bounds
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
            children[TL] = new QTNode(RectangleF.FromLTRB(_bounds.Left, _bounds.Top, 0.5f * (_bounds.Left + _bounds.Right), 0.5f * (_bounds.Top + _bounds.Bottom)), Level + 1);
            children[TR] = new QTNode(RectangleF.FromLTRB(0.5f * (_bounds.Left + _bounds.Right),_bounds.Top,_bounds.Right, 0.5f * (_bounds.Top + _bounds.Bottom)), Level + 1);
            children[BL] = new QTNode(RectangleF.FromLTRB(_bounds.Left, 0.5f * (_bounds.Top + _bounds.Bottom), 0.5f * (_bounds.Left + _bounds.Right), _bounds.Bottom), Level + 1);
            children[BR] = new QTNode(RectangleF.FromLTRB(0.5f * (_bounds.Left + _bounds.Right), 0.5f * (_bounds.Top + _bounds.Bottom), _bounds.Right, _bounds.Bottom), Level + 1);
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
                    PointF pt = helper.GetRecordPoint(recordIndex, shapeFileStream);
                    
                    if(children == null)
                    {
                        CreateChildren();
                    }
                    
                    if (children[TL].Bounds.Contains(pt))
                    {
                        children[TL].Insert(recordIndex, helper, shapeFileStream);
                    }
                    else if (children[TR].Bounds.Contains(pt))
                    {
                        children[TR].Insert(recordIndex, helper, shapeFileStream);
                    }
                    else if (children[BL].Bounds.Contains(pt))
                    {
                        children[BL].Insert(recordIndex, helper, shapeFileStream);
                    }
                    else if (children[BR].Bounds.Contains(pt))
                    {
                        children[BR].Insert(recordIndex, helper, shapeFileStream);
                    }
                    else
                    {
                        throw new InvalidOperationException("point " + pt + " is not contained in children bounds");
                    }                    
                }
                else
                {
                    RectangleF recBounds = helper.GetRecordBounds(recordIndex, shapeFileStream);

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

        internal void Insert(int recordIndex, QTNodeHelper helper, ref RectangleF recBounds, System.IO.Stream shapeFileStream)
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

        public List<int> GetIndices(PointF pt)
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

    }

    interface QTNodeHelper
    {
        bool IsPointData();
        PointF GetRecordPoint(int recordIndex, System.IO.Stream shapeFileStream);
        RectangleF GetRecordBounds(int recordIndex, System.IO.Stream shapeFileStream);
    }
}
