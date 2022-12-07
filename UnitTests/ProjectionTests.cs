using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using EGIS.Projections;
using EGIS.ShapeFileLib;

namespace UnitTests
{
	[TestFixture]
	internal class ProjectionTests
	{
		[Test]
		public void LoadLibraryAndConfirmProjDbFound()
		{
			int geoCount = EGIS.Projections.CoordinateReferenceSystemFactory.Default.GeographicCoordinateSystems.Count;
			int projCount = EGIS.Projections.CoordinateReferenceSystemFactory.Default.ProjectedCoordinateSystems.Count;

			Assert.NotZero(geoCount);
			Assert.NotZero(projCount);

		}

		[Test]
		public void TransformWgs84ToPseudoMercRoundTrip()
		{
			const double Delta = 0.00000001;
			ICRS wgs84 = CoordinateReferenceSystemFactory.Default.GetCRSById(CoordinateReferenceSystemFactory.Wgs84EpsgCode);
			ICRS pseudoMerc = CoordinateReferenceSystemFactory.Default.GetCRSById(CoordinateReferenceSystemFactory.Wgs84PseudoMercatorEpsgCode);

			using (ICoordinateTransformation transformation = CoordinateReferenceSystemFactory.Default.CreateCoordinateTrasformation(wgs84, pseudoMerc))
			{
				PointD wgs84Pt = new PointD(145.0, -37.5);
				PointD pseudoMercPt = transformation.Transform(wgs84Pt);

				PointD roundTripPt = transformation.Transform(pseudoMercPt,TransformDirection.Inverse);

				Assert.AreEqual(wgs84Pt.X, roundTripPt.X, Delta);
				Assert.AreEqual(wgs84Pt.Y, roundTripPt.Y, Delta);
			}

		}

	}
}
