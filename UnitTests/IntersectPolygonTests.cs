using EGIS.ShapeFileLib;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests
{
    [TestFixture]
    public class IntersectPolygonTests
    {
        PointD[] polygon;
        PointD[] polyline;

        [SetUp]
        public void Prepare()
        {
            polygon = new PointD[]
            {
                new PointD(-1027.57124731682, 260.152920724245),
                new PointD(-984.545440236484, 710.791636986736),
                new PointD(-548.626079027792, 332.617437912183),
                new PointD(-501.071239623208, 593.036796556336),
                new PointD(-1027.57124731682, 260.152920724245)
            };

            polyline = new PointD[]
            {
                new PointD(-927.865, 348.584),
                new PointD(-927.865, 348.584),
                new PointD(-908.355, 426.124),
                new PointD(-904.605, 429.884),
                new PointD(-900.355, 433.084),
                new PointD(-895.705, 435.644),
                new PointD(-891.195, 438.044),
                new PointD(-886.955, 440.914),
                new PointD(-883.065, 444.214),
                new PointD(-879.855, 447.574),
                new PointD(-876.985, 451.224),
                new PointD(-875.235, 453.334),
                new PointD(-873.175, 455.144),
                new PointD(-870.865, 456.614),
                new PointD(-868.345, 457.714),
                new PointD(-865.695, 458.404),
                new PointD(-862.975, 458.684),
                new PointD(-860.235, 458.544),
                new PointD(-857.435, 457.954),
                new PointD(-855.335, 457.434),
            };
        }

        [Test]
        public void BowTiePolygonFailure()
        {
            var intersects =  NativeMethods.PolyLinePolygonIntersect(polyline, polyline.Length, polygon, polygon.Length);

            Assert.That(intersects, Is.True);
        }

        [Test]
        public void BowTiePolygonPoint00()
        {
            var intersects = GeometryAlgorithms.PointInPolygon(polygon, polyline[0].X, polyline[0].Y);

            Assert.That(intersects, Is.True);
        }

        [Test]
        public void BowTiePolygonPoint04()
        {
            var intersects = GeometryAlgorithms.PointInPolygon(polygon, polyline[4].X, polyline[4].Y);

            Assert.That(intersects, Is.True);
        }

        [Test]
        public void BowTiePolygonPoint09()
        {
            var intersects = GeometryAlgorithms.PointInPolygon(polygon, polyline[9].X, polyline[9].Y);

            Assert.That(intersects, Is.True);
        }

        [Test]
        public void BowTiePolygonPoint14()
        {
            var intersects = GeometryAlgorithms.PointInPolygon(polygon, polyline[14].X, polyline[14].Y);

            Assert.That(intersects, Is.True);
        }

        [Test]
        public void BowTiePolygonPoint19()
        {
            var intersects = GeometryAlgorithms.PointInPolygon(polygon, polyline[19].X, polyline[19].Y);

            Assert.That(intersects, Is.True);
        }
    }
}
