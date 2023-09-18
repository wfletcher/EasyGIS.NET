using System;
using System.Collections.Generic;
using NUnit.Framework;
using EGIS.ShapeFileLib;
using System.Drawing;
using System.Linq;

namespace UnitTests
{
    [TestFixture]
	public class GeometryClippingTests
	{

		[Test]
		public void ClipPolyLine()
		{
			EGIS.ShapeFileLib.GeometryAlgorithms.ClipBounds clipBounds = new GeometryAlgorithms.ClipBounds()
			{
				XMin = 100,
				XMax = 200,
				YMin = 100,
				YMax = 200
			};

			PointD[] points = new PointD[10];
			points[0] = new PointD(0, 0);
			points[1] = new PointD(110, 110);
			points[2] = new PointD(150, 110);
			points[3] = new PointD(150, 150);
			points[4] = new PointD(250, 150);
			points[5] = new PointD(250, 190);
			points[6] = new PointD(250, 250);
			points[7] = new PointD(190, 190);
			points[8] = new PointD(150, 190);
			points[9] = new PointD(150, 210);

			List<double> clippedPoints = new List<double>();
			List<int> parts = new List<int>();

			EGIS.ShapeFileLib.GeometryAlgorithms.PolyLineClip(points, 10, clipBounds, clippedPoints, parts);

			points = new PointD[10];
			points[0] = new PointD(20, 120);
			points[1] = new PointD(220, 120);
			points[2] = new PointD(220, 140);
			points[3] = new PointD(20, 140);
			points[4] = new PointD(20, 150);
			points[5] = new PointD(250, 150);
			points[6] = new PointD(250, 180);
			points[7] = new PointD(120, 180);
			points[8] = new PointD(120, 190);
			points[9] = new PointD(190, 190);

			List<double> clippedPoints2 = new List<double>();
			List<int> parts2 = new List<int>();

			EGIS.ShapeFileLib.GeometryAlgorithms.PolyLineClip(points, 10, clipBounds, clippedPoints2, parts2);


			//using (Bitmap bm = new Bitmap(120, 120))
			//{
			//	using (Graphics g = Graphics.FromImage(bm))
			//	{
			//		g.Clear(Color.White);
			//		g.DrawRectangle(Pens.Red, 10, 10, 100, 100);
			//		for (int n = 0; n < parts.Count; ++n)
			//		{
			//			int index1 = parts[n];
			//			int index2 = n < parts.Count - 1 ? parts[n + 1] : clippedPoints.Count;
			//			Console.Out.WriteLine("part " + n);
			//			PointF[] pts = new PointF[(index2 - index1) >> 1];
			//			int index = 0;
			//			for (int i = index1; i < index2; i += 2)
			//			{
			//				Console.Out.WriteLine(string.Format("[{0:0.00000}, {1:0.00000}]", clippedPoints[i], clippedPoints[i + 1]));
			//				pts[index++] = new PointF(-90 + (float)clippedPoints[i], -90 + (float)clippedPoints[i + 1]);
			//			}
			//			using (Pen p = new Pen(Color.Red, 3))
			//			{
			//				g.DrawLines(p, pts);
			//			}
			//		}

			//		for (int n = 0; n < parts2.Count; ++n)
			//		{
			//			int index1 = parts2[n];
			//			int index2 = n < parts2.Count - 1 ? parts2[n + 1] : clippedPoints2.Count;
			//			Console.Out.WriteLine("part " + n);
			//			PointF[] pts = new PointF[(index2 - index1) >> 1];
			//			int index = 0;
			//			for (int i = index1; i < index2; i += 2)
			//			{
			//				Console.Out.WriteLine(string.Format("[{0:0.00000}, {1:0.00000}]", clippedPoints2[i], clippedPoints2[i + 1]));
			//				pts[index++] = new PointF(-90 + (float)clippedPoints2[i], -90 + (float)clippedPoints2[i + 1]);
			//			}
			//			using (Pen p = new Pen(Color.Blue, 2))
			//			{
			//				g.DrawLines(p, pts);
			//			}
			//		}
			//	}
			//	bm.Save(@"c:\temp\clippedBitmap.bmp", System.Drawing.Imaging.ImageFormat.Bmp);
			//}


		}

		[Test]
        public void ClipPolygon()
        {
            PointD[] polygon = new PointD[6];
            polygon[0] = new PointD(5, 5);
            polygon[1] = new PointD(4, 4);
            polygon[2] = new PointD(-2, 3);
            polygon[3] = new PointD(-4, 10);
            polygon[4] = new PointD(6, 10);
            polygon[5] = polygon[0];
            GeometryAlgorithms.ClipBounds clipBounds = new GeometryAlgorithms.ClipBounds()
            {
                XMin = 0,
                XMax = 12,
                YMin = 0,
                YMax = 8
            };

            List<PointD> clippedPolygon = new List<PointD>();

            GeometryAlgorithms.PolygonClip(polygon, 6, clipBounds, clippedPolygon);

            Console.WriteLine("Input polygon");
            for (int n = 0; n < polygon.Length; ++n)
            {
                if (n > 0) Console.Write(", ");
                Console.Write(polygon[n]);
            }
            Console.WriteLine();
            Console.WriteLine("Clipped polygon");
            for (int n = 0; n < clippedPolygon.Count; ++n)
            {
                if (n > 0) Console.Write(", ");
                Console.Write(clippedPolygon[n]);
            }
            Console.WriteLine();

            double minX = clippedPolygon.Select(p => p.X).Min();
            Assert.GreaterOrEqual(minX, clipBounds.XMin, "clipped polygon min x coordinate is < clipping bounds");
            double maxX = clippedPolygon.Select(p => p.X).Max();
            Assert.LessOrEqual(maxX, clipBounds.XMax, "clipped polygon max x coordinate is > clipping bounds");

            double minY = clippedPolygon.Select(p => p.Y).Min();
            Assert.GreaterOrEqual(minY, clipBounds.YMin, "clipped polygon min y coordinate is < clipping bounds");
            double maxY = clippedPolygon.Select(p => p.Y).Max();
            Assert.LessOrEqual(maxY, clipBounds.YMax, "clipped polygon max y coordinate is > clipping bounds");


            //const int Scale = 20;

            //using (Bitmap bm = new Bitmap(505, 505))
            //{
            //    using (Graphics g = Graphics.FromImage(bm))
            //    {
            //        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            //        g.Clear(Color.White);
            //        g.TranslateTransform(250, 250);

            //        g.DrawRectangle(Pens.Black, Rectangle.FromLTRB((int)clipBounds.XMin * Scale, (int)clipBounds.YMin * Scale, (int)clipBounds.XMax * Scale, (int)clipBounds.YMax * Scale));

            //        PointF[] pts = ConvertPoints(polygon, Scale);

            //        g.DrawLines(Pens.Blue, pts);

            //        pts = ConvertPoints(clippedPolygon, Scale);

            //        g.DrawLines(Pens.Red, pts);


            //    }
            //    bm.Save(@"c:\temp\clipedPolygon.bmp",System.Drawing.Imaging.ImageFormat.Bmp);   

            //}

            //GeometryAlgorithms.PolygonClip(polygon, 5, clipBounds, clippedPolygon);

            //for (int n = 0; n < clippedPolygon.Count; ++n)
            //{
            //    if (n > 0) Console.Write(", ");
            //    Console.Write(clippedPolygon[n]);
            //}
            //Console.WriteLine();


		}
        
        [Test]
        public void ClipPolygonHole()
        {
            

            PointD[] polygon = new PointD[7];
            polygon[0] = new PointD(10, 0);
            polygon[1] = new PointD(0, 0);
            polygon[2] = new PointD(0, 10);
            polygon[3] = new PointD(9, 10);
            polygon[4] = new PointD(4, 6);
            polygon[5] = new PointD(10, 8);
            polygon[6] = polygon[0];

            PointD[] hole = new PointD[5];
            hole[0] = new PointD(6, 1);
            hole[1] = new PointD(8, 1);
            hole[2] = new PointD(8, 4.5);
            hole[3] = new PointD(6, 4.5);
            hole[4] = hole[0];

            List<PointD> clippedHole = new List<PointD>();

            GeometryAlgorithms.ClipBounds clipBounds = new GeometryAlgorithms.ClipBounds()
            {
                XMin = 5,
                XMax = 9,
                YMin = 4,
                YMax = 9
            };

            List<PointD> clippedPolygon = new List<PointD>();

            //check the direction of the polygons
            bool polygonIsHole = GeometryAlgorithms.IsPolygonHole(polygon, polygon.Length);
            bool holeIsHole = GeometryAlgorithms.IsPolygonHole(hole, hole.Length);

            Assert.IsFalse(polygonIsHole, "polygon should not be a hole - points order should be reversed");
            Assert.IsTrue(holeIsHole, "hole pts wrong order and not detected as a hole");

            GeometryAlgorithms.PolygonClip(polygon, 7, clipBounds, clippedPolygon);

            GeometryAlgorithms.PolygonClip(hole, 5, clipBounds, clippedHole);

            bool clippedPolygonIsHole = GeometryAlgorithms.IsPolygonHole(clippedPolygon, clippedPolygon.Count);
            bool clippedHoleIsHole = GeometryAlgorithms.IsPolygonHole(clippedHole, clippedHole.Count);

            Assert.AreEqual(polygonIsHole, clippedPolygonIsHole, "Clipped polygon order changed after clipping");
            Assert.AreEqual(holeIsHole, clippedHoleIsHole, "Clipped hole order changed after clipping");


            //         for (int n = 0; n < polygon.Length; ++n)
            //{
            //	if (n > 0) Console.Write(", ");
            //	Console.Write(polygon[n]);
            //}
            //Console.WriteLine();
            //for (int n = 0; n < clippedPolygon.Count; ++n)
            //{
            //	if (n > 0) Console.Write(", ");
            //	Console.Write(clippedPolygon[n]);
            //}
            //Console.WriteLine();
            //for (int n = 0; n < clippedPolygon.Count - 1; ++n)
            //{
            //	if (n > 0) Console.Write(", ");
            //	Console.Write(clippedPolygon[n]);
            //}
            //Console.WriteLine();


            //const int Scale = 50;
            //using (Bitmap bm = new Bitmap(505, 505))
            //{
            //	using (Graphics g = Graphics.FromImage(bm))
            //	{
            //		g.Clear(Color.White);

            //		System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
            //		path.FillMode = System.Drawing.Drawing2D.FillMode.Winding;

            //	    PointF[] pts = ConvertPoints(polygon, Scale);
            //                 PointF[] holePts = ConvertPoints(hole, Scale);

            //		//because we're drawing upside down we need to reverse the points to correct the winding order
            //		Array.Reverse(pts);
            //		Array.Reverse(holePts);


            //		path.AddPolygon(pts);
            //		path.AddPolygon(holePts);

            //		g.FillPath(Brushes.Blue, path);


            //                 pts = ConvertPoints(clippedPolygon, Scale);
            //                 holePts = ConvertPoints(clippedHole, Scale);


            //		path = new System.Drawing.Drawing2D.GraphicsPath();
            //		path.FillMode = System.Drawing.Drawing2D.FillMode.Winding;

            //		//because we're drawing upside down we need to reverse the points to correct the winding order
            //		Array.Reverse(pts);
            //		Array.Reverse(holePts);

            //		path.AddPolygon(pts);
            //		path.AddPolygon(holePts);

            //		g.FillPath(Brushes.Yellow, path);

            //		//using (Pen p = new Pen(Color.Green, 2))
            //		//{
            //		//    g.DrawPath(p, path);
            //		//}

            //		//draw the clipping rectangle
            //		using (Pen p = new Pen(Color.Red, 2))
            //		{
            //			g.DrawRectangle(p, new Rectangle((int)clipBounds.XMin * Scale, (int)clipBounds.YMin * Scale,
            //				(int)(clipBounds.XMax - clipBounds.XMin) * Scale,
            //				(int)(clipBounds.YMax - clipBounds.YMin) * Scale));
            //		}

            //		for (int n = 0; n < pts.Length; ++n)
            //		{
            //			g.DrawString(n.ToString(), new Font("Courier", 10), Brushes.Black, pts[n]);
            //		}

            //	}
            //	bm.Save(@"c:\temp\clippedpolygon2.bmp", System.Drawing.Imaging.ImageFormat.Bmp);
            //}



        }



        protected static PointF[] ConvertPoints(IList<PointD> inputPoints, double scale=1)
        {
            PointF[] pts = new PointF[inputPoints.Count];
            for (int i = 0; i < pts.Length; ++i)
            {
                pts[i] = new PointF((float)(inputPoints[i].X * scale), (float)(inputPoints[i].Y * scale));
            }
            return pts;
        }
    }
}
