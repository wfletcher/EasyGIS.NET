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

namespace EGIS.ShapeFileLib
{

    /*
     * Most of these functions are derived from c code written by Chuck Gantz- chuck.gantz@globalstar.com
     */

    /// <summary>
    /// Utility class containing static methods to calculate the distance between points and convert projections    
    /// </summary>
	public sealed class ConversionFunctions
	{
        

        private ConversionFunctions() { }

        /// <summary>
        /// Reference ellipsoid (Australia)
        /// </summary>
		public static int RefEllipse = 2; //Australia

        /// <summary>
        /// WGS84 reference ellipsoid
        /// </summary>
        public const int Wgs84RefEllipse = 23;
        

		public static UtmCoordinate LocationFromRangeBearing(UtmCoordinate currentLocation, double range, double bearing)
		{
			//SohCahToa
			//Using the bearing from north, create a triangle and use geometry to work out new coordinates
			double BearingAsRadians = bearing * Math.PI / 180.0;
			double NorthDistance = range * Math.Cos(BearingAsRadians);
			double EastDistance = range * Math.Sin(BearingAsRadians);
		
			UtmCoordinate TargetLocation = new UtmCoordinate();
			TargetLocation.Easting = currentLocation.Easting + EastDistance;
			TargetLocation.Northing = currentLocation.Northing + NorthDistance;
			TargetLocation.Zone = currentLocation.Zone;
			TargetLocation.ZoneIdentifier = currentLocation.ZoneIdentifier;
		
			return TargetLocation;
		}
		
        /// <summary>
        /// Returns the distance between two points...
        /// </summary>
        /// <param name="currentPosition"></param>
        /// <param name="currentDestination"></param>
        /// <returns></returns>
		public static double GetDistance(UtmCoordinate currentPosition, UtmCoordinate currentDestination)
		{
			double dEast, dNorth;
		
			//Check to see if within the same UTM zone
			if (currentPosition.Zone == currentDestination.Zone)
			{
		
				dEast = currentPosition.Easting - currentDestination.Easting;
				dNorth = currentPosition.Northing - currentDestination.Northing;
		
				return Math.Sqrt(dEast * dEast + dNorth * dNorth);
			}
			else
			{
				//Convert to UTM back to latitude and longitude
				LatLongCoordinate CurrentPositionLatLong = UtmToLL(RefEllipse, currentPosition);
				LatLongCoordinate DestinationPositionLatLong = UtmToLL(RefEllipse, currentDestination);

				//Work out distance
				double Distance = DistanceBetweenLatLongPoints(RefEllipse, CurrentPositionLatLong, DestinationPositionLatLong);

				//and return the result
				return Distance;
			}
		}

        /// <summary>
        /// Returns distance in meters between two lat/long coordinates using Haversine formula. More accurate but slower.
        /// </summary>
        /// <param name="referenceEllipsoid"></param>
        /// <param name="origin"></param>
        /// <param name="destination"></param>
        /// <returns></returns>
		public static double DistanceBetweenLatLongPointsHaversine(int referenceEllipsoid, LatLongCoordinate origin, LatLongCoordinate destination)
		{
            return DistanceBetweenLatLongPointsHaversine(referenceEllipsoid, origin.Latitude, origin.Longitude, destination.Latitude, destination.Longitude);
            ////Convert the latitude long decimal degrees to radians and apply the formula
            ////use the proper ellipsoid to get raidus of the earth
            //double EquatorialRadius = EllipseCollection[referenceEllipsoid].EquatorialRadius;

            //double OriginLatAsRadians = origin.Latitude * (Math.PI / 180.0);
            //double OriginLongAsRadians = origin.Longitude * (Math.PI / 180.0);

            //double DestinationLatAsRadians = destination.Latitude * (Math.PI / 180.0);
            //double DestinationLongAsRadians = destination.Longitude * (Math.PI / 180.0);

            //double ChangeLat = DestinationLatAsRadians - OriginLatAsRadians;
            //double ChangeLong = DestinationLongAsRadians - OriginLongAsRadians;
            //double a = Math.Pow(Math.Sin(ChangeLat / 2.0), 2.0) + 
            //    Math.Cos(OriginLatAsRadians) * Math.Cos(DestinationLatAsRadians) * 
            //    Math.Pow(Math.Sin(ChangeLong  / 2.0), 2.0);
            //double c = 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt((1 - a)));
			
            //double Distance = EquatorialRadius * c;

            //return Distance;
		}

        /// <summary>
        /// Returns distance in meters between two lat/long coordinates using Haversine formula. More accurate but slower.
        /// </summary>
        /// <param name="referenceEllipsoid"></param>
        /// <param name="originLatitude"></param>
        /// <param name="originLongitude"></param>
        /// <param name="destinationLatitude"></param>
        /// <param name="destinationLongitude"></param>
        /// <returns></returns>
        public static double DistanceBetweenLatLongPointsHaversine(int referenceEllipsoid, double originLatitude, double originLongitude, double destinationLatitude, double destinationLongitude)
        {
            //Convert the latitude long decimal degrees to radians and apply the formula
            //use the proper ellipsoid to get raidus of the earth
            double EquatorialRadius = EllipseCollection[referenceEllipsoid].EquatorialRadius;

            double OriginLatAsRadians = originLatitude * (Math.PI / 180.0);
            double OriginLongAsRadians = originLongitude * (Math.PI / 180.0);

            double DestinationLatAsRadians = destinationLatitude * (Math.PI / 180.0);
            double DestinationLongAsRadians = destinationLongitude * (Math.PI / 180.0);

            double ChangeLat = DestinationLatAsRadians - OriginLatAsRadians;
            double ChangeLong = DestinationLongAsRadians - OriginLongAsRadians;
            double a = Math.Pow(Math.Sin(ChangeLat / 2.0), 2.0) +
                Math.Cos(OriginLatAsRadians) * Math.Cos(DestinationLatAsRadians) *
                Math.Pow(Math.Sin(ChangeLong / 2.0), 2.0);
            double c = 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt((1 - a)));

            double Distance = EquatorialRadius * c;

            return Distance;
        }


        /// <summary>
        /// returns the distance in meters between 2 lat/long double-precision points
        /// </summary>
        /// <param name="referenceEllipsoid"></param>
        /// <param name="origin"></param>
        /// <param name="destination"></param>
        /// <returns></returns>
		public static double DistanceBetweenLatLongPoints(int referenceEllipsoid, LatLongCoordinate origin, LatLongCoordinate destination)
		{
            return DistanceBetweenLatLongPoints(referenceEllipsoid, origin.Latitude, origin.Longitude, destination.Latitude, destination.Longitude);
		}

        /// <summary>
        /// returns the distance in meters between 2 lat/long double-precision points
        /// </summary>
        /// <param name="referenceEllipsoid"></param>
        /// <param name="latOrigin"></param>
        /// <param name="longOrigin"></param>
        /// <param name="latDestination"></param>
        /// <param name="longDestination"></param>
        /// <returns></returns>
        public static double DistanceBetweenLatLongPoints(int referenceEllipsoid, double latOrigin, double longOrigin, double latDestination, double longDestination)
        {
            //Convert the latitude long decimal degrees to radians and apply the formula
            //use the proper ellipsoid to get raidus of the earth
            double EquatorialRadius = EllipseCollection[referenceEllipsoid].EquatorialRadius;

            double OriginLatAsRadians = latOrigin * (Math.PI / 180.0);
            double OriginLongAsRadians = longOrigin * (Math.PI / 180.0);

            double DestinationLatAsRadians = latDestination * (Math.PI / 180.0);
            double DestinationLongAsRadians = longDestination * (Math.PI / 180.0);

            double Distance = Math.Acos(Math.Cos(OriginLatAsRadians) * Math.Cos(OriginLongAsRadians) *
                Math.Cos(DestinationLatAsRadians) * Math.Cos(DestinationLongAsRadians) +
                Math.Cos(OriginLatAsRadians) * Math.Sin(OriginLongAsRadians) *
                Math.Cos(DestinationLatAsRadians) * Math.Sin(DestinationLongAsRadians) +
                Math.Sin(OriginLatAsRadians) * Math.Sin(DestinationLatAsRadians)) * EquatorialRadius;

            return Distance;
        }


		internal static double GetLatitude(string latitudeToken, string direction)
		{
			//Latitude token: DDMM.mmm,N
			//Decimal degrees = DD + (MM.mmm / 60)
			string Degrees = latitudeToken.Substring(0, 2);
			string Minutes = latitudeToken.Substring(2);
            double Latitude = double.Parse(Degrees, System.Globalization.CultureInfo.InvariantCulture) + (double.Parse(Minutes, System.Globalization.CultureInfo.InvariantCulture) / 60.0);
		
			//direction: N or S
			//If direction is South: * -1
			if (string.Compare(direction, "N", StringComparison.OrdinalIgnoreCase) != 0)
				Latitude = -Latitude;
		
			return Latitude;
		}

		public static double GetLongitude(string longitudeToken, string direction)
		{
			//longitude token: DDDMM.mmm,E
			//Decimal degrees = DDD + (MM.mmm / 60)
			string degrees = longitudeToken.Substring(0, 3);
			string minutes = longitudeToken.Substring(3);
            double longitude = double.Parse(degrees, System.Globalization.CultureInfo.InvariantCulture) + (double.Parse(minutes, System.Globalization.CultureInfo.InvariantCulture) / 60.0);
			//direction: E or W
			//If direction is West: * -1
			if (string.Compare(direction, "E", StringComparison.Ordinal) != 0)
				longitude = -longitude;
		
			return longitude;
		}

		/*Reference ellipsoids derived from Peter H. Dana's website- 
		http://www.utexas.edu/depts/grg/gcraft/notes/datum/elist.html
		Department of Geography, University of Texas at Austin
		Internet: pdana@mail.utexas.edu
		3/22/95

		Source
		Defense Mapping Agency. 1987b. DMA Technical Report: Supplement to Department of Defense World Geodetic System
		1984 Technical Report. Part I and II. Washington, DC: Defense Mapping Agency
		*/
		public static UtmCoordinate LLToUtm(int referenceEllipsoid, double latitude, double longitude)
		{
			//converts latitude/long to UTM coords.  Equations from USGS Bulletin 1532 
			//East Longitudes are positive, West longitudes are negative. 
			//North latitudes are positive, South latitudes are negative
			//latitude and longitude are in decimal degrees
			//Written by Chuck Gantz- chuck.gantz@globalstar.com

			UtmCoordinate CoordinateSet = new UtmCoordinate();
			double UTMEasting, UTMNorthing;

			double a = EllipseCollection[referenceEllipsoid].EquatorialRadius;
			double eccSquared = EllipseCollection[referenceEllipsoid].EccentricitySquared;
			double k0 = 0.9996;

			double LongOrigin;
			double eccPrimeSquared;
			double N, T, C, A, M;
	
			//Make sure the longitude is between -180.00 .. 179.9
			double LongTemp = (longitude+180)-(int)((longitude+180)/360)*360-180; // -180.00 .. 179.9;

			double LatRad = latitude*(Math.PI / 180.0);
			double LongRad = LongTemp*(Math.PI / 180.0);
			double LongOriginRad;
			int    ZoneNumber;

			ZoneNumber = (int)((LongTemp + 180)/6) + 1;
  
			if( latitude >= 56.0 && latitude < 64.0 && LongTemp >= 3.0 && LongTemp < 12.0 )
				ZoneNumber = 32;

			// Special zones for Svalbard
			if( latitude >= 72.0 && latitude < 84.0 ) 
			{
				if(      LongTemp >= 0.0  && LongTemp <  9.0 ) ZoneNumber = 31;
				else if( LongTemp >= 9.0  && LongTemp < 21.0 ) ZoneNumber = 33;
				else if( LongTemp >= 21.0 && LongTemp < 33.0 ) ZoneNumber = 35;
				else if( LongTemp >= 33.0 && LongTemp < 42.0 ) ZoneNumber = 37;
			}
			LongOrigin = (ZoneNumber - 1)*6 - 180 + 3;  //+3 puts origin in middle of zone
			LongOriginRad = LongOrigin * (Math.PI / 180.0);

			//compute the UTM Zone from the latitude and longitude
			//sprintf(zone, "%d%c", ZoneNumber, UTMLetterDesignator(latitude));
			CoordinateSet.Zone = ZoneNumber;
			CoordinateSet.ZoneIdentifier = UTMLetterDesignator(latitude);

			eccPrimeSquared = (eccSquared)/(1-eccSquared);

			N = a/Math.Sqrt(1-eccSquared*Math.Sin(LatRad)*Math.Sin(LatRad));
			T = Math.Tan(LatRad)*Math.Tan(LatRad);
			C = eccPrimeSquared*Math.Cos(LatRad)*Math.Cos(LatRad);
			A = Math.Cos(LatRad)*(LongRad-LongOriginRad);

			M = a*((1	- eccSquared/4		- 3*eccSquared*eccSquared/64	- 5*eccSquared*eccSquared*eccSquared/256)*LatRad 
				- (3*eccSquared/8	+ 3*eccSquared*eccSquared/32	+ 45*eccSquared*eccSquared*eccSquared/1024)*Math.Sin(2*LatRad)
				+ (15*eccSquared*eccSquared/256 + 45*eccSquared*eccSquared*eccSquared/1024)*Math.Sin(4*LatRad) 
				- (35*eccSquared*eccSquared*eccSquared/3072)*Math.Sin(6*LatRad));
	
			UTMEasting = (double)(k0*N*(A+(1-T+C)*A*A*A/6
				+ (5-18*T+T*T+72*C-58*eccPrimeSquared)*A*A*A*A*A/120)
				+ 500000.0);

			UTMNorthing = (double)(k0*(M+N*Math.Tan(LatRad)*(A*A/2+(5-T+9*C+4*C*C)*A*A*A*A/24
				+ (61-58*T+T*T+600*C-330*eccPrimeSquared)*A*A*A*A*A*A/720)));
			if(latitude < 0)
				UTMNorthing += 10000000.0; //10000000 meter offset for southern hemisphere

			CoordinateSet.Easting = UTMEasting;
			CoordinateSet.Northing = UTMNorthing;

			return CoordinateSet;
		}

        public static System.Drawing.PointF LLToUtm2(double latitude, double longitude, double eqRadius, double inverseFlattening, double scaleFactor)
        {
            double f = 1 / inverseFlattening;
            return LLToUtm(latitude, longitude, eqRadius, 1 - ((1 - f) * (1 - f)), scaleFactor);
        }
        public static System.Drawing.PointF LLToUtm(double latitude, double longitude, double eqRadius, double eccSquared, double scaleFactor)
        {
            //converts latitude/long to UTM coords.  Equations from USGS Bulletin 1532 
            //East Longitudes are positive, West longitudes are negative. 
            //North latitudes are positive, South latitudes are negative
            //latitude and longitude are in decimal degrees
            //Written by Chuck Gantz- chuck.gantz@globalstar.com

            UtmCoordinate CoordinateSet = new UtmCoordinate();
            double UTMEasting, UTMNorthing;

            double a = eqRadius;
            //double eccSquared = EllipseCollection[referenceEllipsoid].EccentricitySquared;
            double k0 = scaleFactor;// 0.9996;

            double LongOrigin;
            double eccPrimeSquared;
            double N, T, C, A, M;

            //Make sure the longitude is between -180.00 .. 179.9
            double LongTemp = (longitude + 180) - (int)((longitude + 180) / 360) * 360 - 180; // -180.00 .. 179.9;

            double LatRad = latitude * (Math.PI / 180.0);
            double LongRad = LongTemp * (Math.PI / 180.0);
            double LongOriginRad;
            int ZoneNumber;

            ZoneNumber = (int)((LongTemp + 180) / 6) + 1;

            if (latitude >= 56.0 && latitude < 64.0 && LongTemp >= 3.0 && LongTemp < 12.0)
                ZoneNumber = 32;

            // Special zones for Svalbard
            if (latitude >= 72.0 && latitude < 84.0)
            {
                if (LongTemp >= 0.0 && LongTemp < 9.0) ZoneNumber = 31;
                else if (LongTemp >= 9.0 && LongTemp < 21.0) ZoneNumber = 33;
                else if (LongTemp >= 21.0 && LongTemp < 33.0) ZoneNumber = 35;
                else if (LongTemp >= 33.0 && LongTemp < 42.0) ZoneNumber = 37;
            }
            LongOrigin = (ZoneNumber - 1) * 6 - 180 + 3;  //+3 puts origin in middle of zone
            LongOriginRad = LongOrigin * (Math.PI / 180.0);

            //compute the UTM Zone from the latitude and longitude
            //sprintf(zone, "%d%c", ZoneNumber, UTMLetterDesignator(latitude));
            CoordinateSet.Zone = ZoneNumber;
            CoordinateSet.ZoneIdentifier = UTMLetterDesignator(latitude);

            eccPrimeSquared = (eccSquared) / (1 - eccSquared);

            N = a / Math.Sqrt(1 - eccSquared * Math.Sin(LatRad) * Math.Sin(LatRad));
            T = Math.Tan(LatRad) * Math.Tan(LatRad);
            C = eccPrimeSquared * Math.Cos(LatRad) * Math.Cos(LatRad);
            A = Math.Cos(LatRad) * (LongRad - LongOriginRad);

            M = a * ((1 - eccSquared / 4 - 3 * eccSquared * eccSquared / 64 - 5 * eccSquared * eccSquared * eccSquared / 256) * LatRad
                - (3 * eccSquared / 8 + 3 * eccSquared * eccSquared / 32 + 45 * eccSquared * eccSquared * eccSquared / 1024) * Math.Sin(2 * LatRad)
                + (15 * eccSquared * eccSquared / 256 + 45 * eccSquared * eccSquared * eccSquared / 1024) * Math.Sin(4 * LatRad)
                - (35 * eccSquared * eccSquared * eccSquared / 3072) * Math.Sin(6 * LatRad));

            UTMEasting = (double)(k0 * N * (A + (1 - T + C) * A * A * A / 6
                + (5 - 18 * T + T * T + 72 * C - 58 * eccPrimeSquared) * A * A * A * A * A / 120)
                + 500000.0);

            UTMNorthing = (double)(k0 * (M + N * Math.Tan(LatRad) * (A * A / 2 + (5 - T + 9 * C + 4 * C * C) * A * A * A * A / 24
                + (61 - 58 * T + T * T + 600 * C - 330 * eccPrimeSquared) * A * A * A * A * A * A / 720)));
            if (latitude < 0)
                UTMNorthing += 10000000.0; //10000000 meter offset for southern hemisphere

            //CoordinateSet.Easting = UTMEasting;
            //CoordinateSet.Northing = UTMNorthing;

            //return CoordinateSet;
            return new System.Drawing.PointF((float)UTMEasting, (float)UTMNorthing);
        }

		public static LatLongCoordinate UtmToLL(int referenceEllipsoid, UtmCoordinate position)
		{
			return UtmToLL(referenceEllipsoid, position.Northing, position.Easting, position.Zone, position.ZoneIdentifier);
		}

		public static LatLongCoordinate UtmToLL(int referenceEllipsoid, double utmNorthing,
			double utmEasting, int zone, char zoneIdentifier)
		{
			//converts Utm coords to latitude/long.  Equations from USGS Bulletin 1532 
			//East Longitudes are positive, West longitudes are negative. 
			//North latitudes are positive, South latitudes are negative
			//latitude and longitude are in decimal degrees. 
			//Written by Chuck Gantz- chuck.gantz@globalstar.com

			LatLongCoordinate CoordinateSet = new LatLongCoordinate();
			double Lat, Long;

			double k0 = 0.9996;
			double a = EllipseCollection[referenceEllipsoid].EquatorialRadius;
			double eccSquared = EllipseCollection[referenceEllipsoid].EccentricitySquared;
			double eccPrimeSquared;
			double e1 = (1-Math.Sqrt(1-eccSquared))/(1+Math.Sqrt(1-eccSquared));
			double N1, T1, C1, R1, D, M;
			double LongOrigin;
			double mu, /*phi1,*/ phi1Rad;
			double x, y;
			int ZoneNumber;
			//int NorthernHemisphere; //1 for northern hemispher, 0 for southern

			x = utmEasting - 500000.0; //remove 500,000 meter offset for longitude
			y = utmNorthing;

			ZoneNumber = zone;
            if ((zoneIdentifier - 'N') >= 0)
            {
                //NorthernHemisphere = 1;//point is in northern hemisphere
            }
            else
            {
              //  NorthernHemisphere = 0;//point is in southern hemisphere
                y -= 10000000.0;//remove 10,000,000 meter offset used for southern hemisphere
            }

			LongOrigin = (ZoneNumber - 1)*6 - 180 + 3;  //+3 puts origin in middle of zone

			eccPrimeSquared = (eccSquared)/(1-eccSquared);

			M = y / k0;
			mu = M/(a*(1-eccSquared/4-3*eccSquared*eccSquared/64-5*eccSquared*eccSquared*eccSquared/256));

			phi1Rad = mu + (3*e1/2-27*e1*e1*e1/32)*Math.Sin(2*mu) 
				+ (21*e1*e1/16-55*e1*e1*e1*e1/32)*Math.Sin(4*mu)
				+(151*e1*e1*e1/96)*Math.Sin(6*mu);
			//phi1 = phi1Rad*(180.0 / Math.PI);

			N1 = a/Math.Sqrt(1-eccSquared*Math.Sin(phi1Rad)*Math.Sin(phi1Rad));
			T1 = Math.Tan(phi1Rad)*Math.Tan(phi1Rad);
			C1 = eccPrimeSquared*Math.Cos(phi1Rad)*Math.Cos(phi1Rad);
			R1 = a*(1-eccSquared)/Math.Pow(1-eccSquared*Math.Sin(phi1Rad)*Math.Sin(phi1Rad), 1.5);
			D = x/(N1*k0);

			Lat = phi1Rad - (N1*Math.Tan(phi1Rad)/R1)*(D*D/2-(5+3*T1+10*C1-4*C1*C1-9*eccPrimeSquared)*D*D*D*D/24
				+(61+90*T1+298*C1+45*T1*T1-252*eccPrimeSquared-3*C1*C1)*D*D*D*D*D*D/720);
			Lat = Lat * (180.0 / Math.PI);

			Long = (D-(1+2*T1+C1)*D*D*D/6+(5-2*C1+28*T1-3*C1*C1+8*eccPrimeSquared+24*T1*T1)
				*D*D*D*D*D/120)/Math.Cos(phi1Rad);
			Long = LongOrigin + Long * (180.0 / Math.PI);

			CoordinateSet.Latitude = Lat;
			CoordinateSet.Longitude = Long;

			return CoordinateSet;
		}

        public static System.Drawing.PointF PseudoAmgToLL(System.Drawing.PointF amgPoint)
        {
            //converts UTM coords to latitude/long.  Equations from USGS Bulletin 1532 
            //East Longitudes are positive, West longitudes are negative. 
            //North latitudes are positive, South latitudes are negative
            //latitude and longitude are in decimal degrees. 
            //Written by Chuck Gantz- chuck.gantz@globalstar.com

            int ReferenceEllipsoid = Wgs66RefEllipsoid;
            double Lat, Long;

            double k0 = 1;// 0.9996;
            double a = EllipseCollection[ReferenceEllipsoid].EquatorialRadius;
            double eccSquared = EllipseCollection[ReferenceEllipsoid].EccentricitySquared;
            double eccPrimeSquared;
            double e1 = (1 - Math.Sqrt(1 - eccSquared)) / (1 + Math.Sqrt(1 - eccSquared));
            double N1, T1, C1, R1, D, M;
            double LongOrigin;
            double mu, phi1Rad;
            double x, y;
            //int ZoneNumber;
            //int NorthernHemisphere=0; //1 for northern hemispher, 0 for southern

            x = amgPoint.X - 500000.0; //remove 500,000 meter offset for longitude
            y = amgPoint.Y;

            //ZoneNumber = 54;
            y -= 10000000.0;//remove 10,000,000 meter offset used for southern hemisphere
            
            //low level coonversion from agd66 to gda94
            x += 112;
            y += 178;

            LongOrigin = 145;  //middle of zone

            eccPrimeSquared = (eccSquared) / (1 - eccSquared);

            M = y / k0;
            mu = M / (a * (1 - eccSquared / 4 - 3 * eccSquared * eccSquared / 64 - 5 * eccSquared * eccSquared * eccSquared / 256));

            phi1Rad = mu + (3 * e1 / 2 - 27 * e1 * e1 * e1 / 32) * Math.Sin(2 * mu)
                + (21 * e1 * e1 / 16 - 55 * e1 * e1 * e1 * e1 / 32) * Math.Sin(4 * mu)
                + (151 * e1 * e1 * e1 / 96) * Math.Sin(6 * mu);
            //phi1 = phi1Rad * (180.0 / Math.PI);

            N1 = a / Math.Sqrt(1 - eccSquared * Math.Sin(phi1Rad) * Math.Sin(phi1Rad));
            T1 = Math.Tan(phi1Rad) * Math.Tan(phi1Rad);
            C1 = eccPrimeSquared * Math.Cos(phi1Rad) * Math.Cos(phi1Rad);
            R1 = a * (1 - eccSquared) / Math.Pow(1 - eccSquared * Math.Sin(phi1Rad) * Math.Sin(phi1Rad), 1.5);
            D = x / (N1 * k0);

            Lat = phi1Rad - (N1 * Math.Tan(phi1Rad) / R1) * (D * D / 2 - (5 + 3 * T1 + 10 * C1 - 4 * C1 * C1 - 9 * eccPrimeSquared) * D * D * D * D / 24
                + (61 + 90 * T1 + 298 * C1 + 45 * T1 * T1 - 252 * eccPrimeSquared - 3 * C1 * C1) * D * D * D * D * D * D / 720);
            Lat = Lat * (180.0 / Math.PI);

            Long = (D - (1 + 2 * T1 + C1) * D * D * D / 6 + (5 - 2 * C1 + 28 * T1 - 3 * C1 * C1 + 8 * eccPrimeSquared + 24 * T1 * T1)
                * D * D * D * D * D / 120) / Math.Cos(phi1Rad);
            Long = LongOrigin + Long * (180.0 / Math.PI);

            return new System.Drawing.PointF((float)Long, (float)Lat);
            
        }

        /// <summary>
        /// Converts a projected point (in UTM coords) to latitude/long.  
        /// </summary>
        /// <param name="inPoint"></param>
        /// <param name="centralMeridian"></param>
        /// <param name="latOrigin"></param>
        /// <param name="falseEasting"></param>
        /// <param name="falseNorthing"></param>
        /// <param name="scaleFactor"></param>
        /// <param name="equatorialRadius"></param>
        /// <param name="inverseFlattening"></param>
        /// <returns></returns>
        /// <remarks>
        /// <para>Equations from USGS Bulletin 1532
        /// </para>
        /// <para>
        /// returned latitude and longitude points are expressed in decimal degrees.                     
        /// </para>
        /// </remarks>
        public static System.Drawing.PointF ToLL2(System.Drawing.PointF inPoint, double centralMeridian, double latOrigin, double falseEasting, double falseNorthing, double scaleFactor, double equatorialRadius, double inverseFlattening)
        {
            double f = 1/inverseFlattening;
            return ToLL(inPoint, centralMeridian, latOrigin, falseEasting, falseNorthing, scaleFactor, equatorialRadius, 1 - ((1 - f) * (1 - f)));
        }

        /// <summary>
        /// Converts a projected point (in UTM coords) to latitude/long.  
        /// </summary>
        /// <param name="inPoint"></param>
        /// <param name="centralMeridian"></param>
        /// <param name="latOrigin"></param>
        /// <param name="falseEasting"></param>
        /// <param name="falseNorthing"></param>
        /// <param name="scaleFactor"></param>
        /// <param name="equatorialRadius"></param>
        /// <param name="eccSquared"></param>
        /// <returns></returns>
        /// <remarks>
        /// <para>Equations from USGS Bulletin 1532 </para>
        /// </para>
        /// <para>
        /// returned latitude and longitude points are expressed in decimal degrees.                     
        /// </para>
        /// </remarks>
        public static System.Drawing.PointF ToLL(System.Drawing.PointF inPoint, double centralMeridian, double latOrigin, double falseEasting, double falseNorthing, double scaleFactor, double equatorialRadius, double eccSquared)
        {
            //converts UTM coords to latitude/long.  Equations from USGS Bulletin 1532 
            //East Longitudes are positive, West longitudes are negative. 
            //North latitudes are positive, South latitudes are negative
            //latitude and longitude are in decimal degrees. 
            //Written by Chuck Gantz- chuck.gantz@globalstar.com

            double Lat, Long;

            double k0 = scaleFactor;// 0.9996;
            double a = equatorialRadius;
            double eccPrimeSquared;
            double e1 = (1 - Math.Sqrt(1 - eccSquared)) / (1 + Math.Sqrt(1 - eccSquared));
            double N1, T1, C1, R1, D, M;
            double mu, phi1Rad;
            double x, y;
            
            x = inPoint.X - falseEasting;
            y = inPoint.Y;

            y -= falseNorthing;//remove 10,000,000 meter offset used for southern hemisphere
            
            double LongOrigin = centralMeridian;

            eccPrimeSquared = (eccSquared) / (1 - eccSquared);

            M = y / k0;
            mu = M / (a * (1 - eccSquared / 4 - 3 * eccSquared * eccSquared / 64 - 5 * eccSquared * eccSquared * eccSquared / 256));

            phi1Rad = mu + (3 * e1 / 2 - 27 * e1 * e1 * e1 / 32) * Math.Sin(2 * mu)
                + (21 * e1 * e1 / 16 - 55 * e1 * e1 * e1 * e1 / 32) * Math.Sin(4 * mu)
                + (151 * e1 * e1 * e1 / 96) * Math.Sin(6 * mu);
            
            N1 = a / Math.Sqrt(1 - eccSquared * Math.Sin(phi1Rad) * Math.Sin(phi1Rad));
            T1 = Math.Tan(phi1Rad) * Math.Tan(phi1Rad);
            C1 = eccPrimeSquared * Math.Cos(phi1Rad) * Math.Cos(phi1Rad);
            R1 = a * (1 - eccSquared) / Math.Pow(1 - eccSquared * Math.Sin(phi1Rad) * Math.Sin(phi1Rad), 1.5);
            D = x / (N1 * k0);

            Lat = phi1Rad - (N1 * Math.Tan(phi1Rad) / R1) * (D * D / 2 - (5 + 3 * T1 + 10 * C1 - 4 * C1 * C1 - 9 * eccPrimeSquared) * D * D * D * D / 24
                + (61 + 90 * T1 + 298 * C1 + 45 * T1 * T1 - 252 * eccPrimeSquared - 3 * C1 * C1) * D * D * D * D * D * D / 720);
            Lat = latOrigin + Lat * (180.0 / Math.PI);

            Long = (D - (1 + 2 * T1 + C1) * D * D * D / 6 + (5 - 2 * C1 + 28 * T1 - 3 * C1 * C1 + 8 * eccPrimeSquared + 24 * T1 * T1)
                * D * D * D * D * D / 120) / Math.Cos(phi1Rad);
            Long = LongOrigin + Long * (180.0 / Math.PI);

            return new System.Drawing.PointF((float)Long, (float)Lat);

        }

        /// <summary>
        /// Converts a projected point (in UTM coords) to latitude/long.  
        /// </summary>
        /// <param name="inPoint"></param>
        /// <param name="centralMeridian"></param>
        /// <param name="latOrigin"></param>
        /// <param name="falseEasting"></param>
        /// <param name="falseNorthing"></param>
        /// <param name="scaleFactor"></param>
        /// <param name="ellipsoid"></param>
        /// <returns></returns>
        /// <remarks>
        /// <para>Equations from USGS Bulletin 1532 </para>
        /// </para>
        /// <para>
        /// returned latitude and longitude points are expressed in decimal degrees.                     
        /// </para>
        /// </remarks>
        public static System.Drawing.PointF ToLL(System.Drawing.PointF inPoint, double centralMeridian, double latOrigin, double falseEasting, double falseNorthing, double scaleFactor, Ellipsoid ellipsoid)
        {
            
            double Lat, Long;

            double k0 = scaleFactor;// 0.9996;
            double a = ellipsoid.EquatorialRadius;
            double eccSquared = ellipsoid.EccentricitySquared;
            double eccPrimeSquared;
            double e1 = (1 - Math.Sqrt(1 - eccSquared)) / (1 + Math.Sqrt(1 - eccSquared));
            double N1, T1, C1, R1, D, M;
            double mu, phi1Rad;
            double x, y;
           
            x = inPoint.X - falseEasting; 
            y = inPoint.Y;

            y -= falseNorthing;//remove 10,000,000 meter offset used for southern hemisphere
           
            double LongOrigin = centralMeridian;
            
            eccPrimeSquared = (eccSquared) / (1 - eccSquared);

            M = y / k0;
            mu = M / (a * (1 - eccSquared / 4 - 3 * eccSquared * eccSquared / 64 - 5 * eccSquared * eccSquared * eccSquared / 256));

            phi1Rad = mu + (3 * e1 / 2 - 27 * e1 * e1 * e1 / 32) * Math.Sin(2 * mu)
                + (21 * e1 * e1 / 16 - 55 * e1 * e1 * e1 * e1 / 32) * Math.Sin(4 * mu)
                + (151 * e1 * e1 * e1 / 96) * Math.Sin(6 * mu);
            //phi1 = phi1Rad * (180.0 / Math.PI);

            N1 = a / Math.Sqrt(1 - eccSquared * Math.Sin(phi1Rad) * Math.Sin(phi1Rad));
            T1 = Math.Tan(phi1Rad) * Math.Tan(phi1Rad);
            C1 = eccPrimeSquared * Math.Cos(phi1Rad) * Math.Cos(phi1Rad);
            R1 = a * (1 - eccSquared) / Math.Pow(1 - eccSquared * Math.Sin(phi1Rad) * Math.Sin(phi1Rad), 1.5);
            D = x / (N1 * k0);

            Lat = phi1Rad - (N1 * Math.Tan(phi1Rad) / R1) * (D * D / 2 - (5 + 3 * T1 + 10 * C1 - 4 * C1 * C1 - 9 * eccPrimeSquared) * D * D * D * D / 24
                + (61 + 90 * T1 + 298 * C1 + 45 * T1 * T1 - 252 * eccPrimeSquared - 3 * C1 * C1) * D * D * D * D * D * D / 720);
            Lat = latOrigin + Lat * (180.0 / Math.PI);

            Long = (D - (1 + 2 * T1 + C1) * D * D * D / 6 + (5 - 2 * C1 + 28 * T1 - 3 * C1 * C1 + 8 * eccPrimeSquared + 24 * T1 * T1)
                * D * D * D * D * D / 120) / Math.Cos(phi1Rad);
            Long = LongOrigin + Long * (180.0 / Math.PI);

            return new System.Drawing.PointF((float)Long, (float)Lat);

        }


		private static char UTMLetterDesignator(double lat)
		{
			//This routine determines the correct UTM letter designator for the given latitude
			//returns 'Z' if latitude is outside the UTM limits of 84N to 80S
			//Written by Chuck Gantz- chuck.gantz@globalstar.com
			char LetterDesignator;

			if((84 >= lat) && (lat >= 72)) LetterDesignator = 'X';
			else if((72 > lat) && (lat >= 64)) LetterDesignator = 'W';
			else if((64 > lat) && (lat >= 56)) LetterDesignator = 'V';
			else if((56 > lat) && (lat >= 48)) LetterDesignator = 'U';
			else if((48 > lat) && (lat >= 40)) LetterDesignator = 'T';
			else if((40 > lat) && (lat >= 32)) LetterDesignator = 'S';
			else if((32 > lat) && (lat >= 24)) LetterDesignator = 'R';
			else if((24 > lat) && (lat >= 16)) LetterDesignator = 'Q';
			else if((16 > lat) && (lat >= 8)) LetterDesignator = 'P';
			else if(( 8 > lat) && (lat >= 0)) LetterDesignator = 'N';
			else if(( 0 > lat) && (lat >= -8)) LetterDesignator = 'M';
			else if((-8> lat) && (lat >= -16)) LetterDesignator = 'L';
			else if((-16 > lat) && (lat >= -24)) LetterDesignator = 'K';
			else if((-24 > lat) && (lat >= -32)) LetterDesignator = 'J';
			else if((-32 > lat) && (lat >= -40)) LetterDesignator = 'H';
			else if((-40 > lat) && (lat >= -48)) LetterDesignator = 'G';
			else if((-48 > lat) && (lat >= -56)) LetterDesignator = 'F';
			else if((-56 > lat) && (lat >= -64)) LetterDesignator = 'E';
			else if((-64 > lat) && (lat >= -72)) LetterDesignator = 'D';
			else if((-72 > lat) && (lat >= -80)) LetterDesignator = 'C';
			else LetterDesignator = 'Z'; //This is here as an error flag to show that the Latitude is outside the UTM limits

			return LetterDesignator;
		}

        public const int Wgs66RefEllipsoid = 2;

		public static readonly Ellipsoid[] EllipseCollection = 
		{
            //  id, Ellipsoid name, Equatorial Radius, square of eccentricity	
			new Ellipsoid( -1, "Placeholder", 0, 0),//placeholder only, To allow array indices to match id numbers
			new Ellipsoid( 1, "Airy", 6377563, 0.00667054),
			new Ellipsoid( 2, "Australian National", 6378160, 0.006694542),
			new Ellipsoid( 3, "Bessel 1841", 6377397, 0.006674372),
			new Ellipsoid( 4, "Bessel 1841 (Nambia) ", 6377484, 0.006674372),
			new Ellipsoid( 5, "Clarke 1866", 6378206, 0.006768658),
			new Ellipsoid( 6, "Clarke 1880", 6378249, 0.006803511),
			new Ellipsoid( 7, "Everest", 6377276, 0.006637847),
			new Ellipsoid( 8, "Fischer 1960 (Mercury) ", 6378166, 0.006693422),
			new Ellipsoid( 9, "Fischer 1968", 6378150, 0.006693422),
			new Ellipsoid( 10, "GRS 1967", 6378160, 0.006694605),
			new Ellipsoid( 11, "GRS 1980", 6378137, 0.00669438),
			new Ellipsoid( 12, "Helmert 1906", 6378200, 0.006693422),
			new Ellipsoid( 13, "Hough", 6378270, 0.00672267),
			new Ellipsoid( 14, "International", 6378388, 0.00672267),
			new Ellipsoid( 15, "Krassovsky", 6378245, 0.006693422),
			new Ellipsoid( 16, "Modified Airy", 6377340, 0.00667054),
			new Ellipsoid( 17, "Modified Everest", 6377304, 0.006637847),
			new Ellipsoid( 18, "Modified Fischer 1960", 6378155, 0.006693422),
			new Ellipsoid( 19, "South American 1969", 6378160, 0.006694542),
			new Ellipsoid( 20, "WGS 60", 6378165, 0.006693422),
			new Ellipsoid( 21, "WGS 66", 6378145, 0.006694542),
			new Ellipsoid( 22, "WGS-72", 6378135, 0.006694318),
			new Ellipsoid( 23, "WGS-84", 6378137, 0.00669438)
		};
	}


    /// <summary>
    /// struct used to store a UTM Coordinate. Used in ConversionFunctions
    /// </summary>
    /// <seealso cref="ConversionFunctions"/>
	[Serializable]
	public struct UtmCoordinate
	{
		private double _easting;
		private double _northing;
		private int zone;
		private char zoneIdentifier;

		public override string ToString()
		{
            return ZoneIdentifier.ToString(System.Globalization.CultureInfo.InvariantCulture) + Easting.ToString(System.Globalization.CultureInfo.InvariantCulture) + "E, " + Northing.ToString(System.Globalization.CultureInfo.InvariantCulture) + "N";
		}

        public double Easting
        {
            get
            {
                return _easting;
            }
            set
            {
                _easting = value;
            }
        }
        public double Northing
        {
            get
            {
                return _northing;
            }
            set
            {
                _northing = value;
            }
        }
        public int Zone
        {
            get
            {
                return zone;
            }
            set
            {
                zone = value;
            }
        }
        public char ZoneIdentifier
        {
            get
            {
                return zoneIdentifier;
            }
            set
            {
                zoneIdentifier = value;
            }
        }

	}

    /// <summary>
    /// struct used to store a Lat Long Coordinate. Used in ConversionFunctions
    /// </summary>
    /// <seealso cref="ConversionFunctions"/>
	public struct LatLongCoordinate
	{
		private double latitude;
		private double longitude;

        public override string ToString()
        {
            return string.Format(System.Globalization.CultureInfo.InvariantCulture,"{0},{1}", new object[] { Latitude, Longitude });
        }

        public double Latitude
        {
            get
            {
                return latitude;
            }
            set
            {
                latitude = value;
            }
        }

        public double Longitude
        {
            get
            {
                return longitude;
            }
            set
            {
                longitude = value;
            }
        }

	}

    /// <summary>
    /// Simple class that encapsulates the parameters defining an Ellipsoid 
    /// </summary>
    /// <remarks>
    /// <para>
    /// </para>
    /// <para>
    /// Note that in most SPHEROID parameters of the WKT format contained in a ".prj" projection file, 
    /// the flattening is expressed as 1/f. For example, in 
    /// SPHEROID["GRS_1980",6378137.0,298.257222101] 1/f = 298.257222101. 6378137.0 is the EquatorialRadius
    /// </para>
    /// <para>
    /// See remarks on EccentricitySquared for explanation on how to convert "f" to "E^2"
    /// </para>
    /// </remarks>
    /// <seealso cref="Ellipsoid.EccentricitySquared"/>
    /// <seealso cref="Ellipsoid.EquatorialRadius"/>        
	public class Ellipsoid
	{
		private int _id;
        private string _ellipsoidName;
        private double _equatorialRadius;
        private double _eccentricitySquared;

		public Ellipsoid(int id, string name, double radius, double ecc)
		{
			this.Id = id;
			this.EllipsoidName = name; 
			this.EquatorialRadius = radius; 
			this.EccentricitySquared = ecc;
		}

        public override string ToString()
        {
            return EllipsoidName;
        }

        /// <summary>
        /// unique Id of the Ellipsoid 
        /// </summary>
        public int Id
        {
            get
            {
                return _id;
            }
            set
            {
                _id = value;
            }
        }

        /// <summary>
        /// The well known name of the Ellipsoid
        /// </summary>
        public string EllipsoidName
        {
            get
            {
                return _ellipsoidName;
            }
            set
            {
                _ellipsoidName = value;
            }
        }

        /// <summary>
        /// The equatorial radius (semi-major axis) of the ellipsoid
        /// </summary>
        public double EquatorialRadius
        {
            get
            {
                return _equatorialRadius;
            }
            set
            {
                _equatorialRadius = value;
            }
        }

        /// <summary>
        /// Eccentricity of the Ellipsoid squared        
        /// </summary>
        /// <remarks>
        /// E^2 is related to the flattening of the Ellipsoid as follows:
        /// <para>
        /// E^2 = 1 - (1-f)^2
        /// </para>
        /// <para>
        /// Note that in most SPHEROID parameters of the WKT format the flattening is expressed as 1/f. For example, in 
        /// SPHEROID["GRS_1980",6378137.0,298.257222101] 1/f = 298.257222101
        /// </para>
        /// </remarks>
        public double EccentricitySquared
        {
            get
            {
                return _eccentricitySquared;
            }
            set
            {
                this._eccentricitySquared = value;
            }
        }

        public double InverseFlattening
        {
            get
            {
                return 1/(1 - Math.Pow(1 - EccentricitySquared, 0.5));
            }

        }

	}
}

