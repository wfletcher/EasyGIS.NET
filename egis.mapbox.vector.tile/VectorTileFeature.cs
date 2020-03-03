using System.Collections.Generic;

namespace EGIS.Mapbox.Vector.Tile
{

    public enum AttributeType
    {
        Unknown,
        StringValue,
        FloatValue,
        DoubleValue,
        IntValue,
        UIntValue,
        SIntValue,
        BoolValue
    }

    
    public class AttributeKeyValue
    {
        public AttributeKeyValue(string key, double val)
        {
            Key = key;
            Value = val;
            AttributeType = AttributeType.DoubleValue;
        }

        public AttributeKeyValue(string key, float val)
        {
            Key = key;
            Value = val;
            AttributeType = AttributeType.FloatValue;
        }

        public AttributeKeyValue(string key, string val)
        {
            Key = key;
            Value = val;
            AttributeType = AttributeType.StringValue;
        }

        public AttributeKeyValue(string key, bool val)
        {
            Key = key;
            Value = val;
            AttributeType = AttributeType.BoolValue;
        }

       
        public AttributeKeyValue(string key, System.Int64 val)
        {
            Key = key;
            Value = val;
            AttributeType = AttributeType.IntValue;
        }

        public AttributeKeyValue(string key, System.UInt64 val)
        {
            Key = key;
            Value = val;
            AttributeType = AttributeType.UIntValue;
        }

        public AttributeKeyValue(string key, dynamic val, AttributeType attributeType)
        {
            Key = key;
            Value = val;
            AttributeType = attributeType;
        }

        public string Key;

        public dynamic Value;
        //public object Value;

        public AttributeType AttributeType;


        public static Tile.Value ToTileValue(AttributeKeyValue value)
        {
            Tile.Value tileValue = new Tile.Value();
            if (value.AttributeType == AttributeType.StringValue)
            {
                tileValue.StringValue = value.Value;
            }
            else if (value.AttributeType == AttributeType.BoolValue)
            {
                tileValue.BoolValue = value.Value;
            }
            else if (value.AttributeType == AttributeType.DoubleValue)
            {
                tileValue.DoubleValue = value.Value;
            }
            else if (value.AttributeType == AttributeType.FloatValue)
            {
                tileValue.FloatValue = value.Value;
            }
            else if (value.AttributeType == AttributeType.IntValue)
            {
                tileValue.IntValue = value.Value;
            }
            else if (value.AttributeType == AttributeType.UIntValue)
            {
                tileValue.UintValue = value.Value;
            }
            else if (value.AttributeType == AttributeType.SIntValue)
            {
                tileValue.SintValue = value.Value;
            }
            else
            {
                throw new System.Exception(string.Format("Could not determine tileValue. valye type is {0}", value.GetType()));
            }
            return tileValue;
        }


        public override string ToString()
        {
            return Value.ToString();
        }

    }




    public class VectorTileFeature
	{
        public string Id { get; set; }
		public List<List<Coordinate>> Geometry {get;set;}
		///public List<KeyValuePair<string, object>> Attributes { get; set; }
        
        public List<AttributeKeyValue> Attributes { get; set; }

		public Tile.GeomType GeometryType { get; set; }
        public uint Extent { get; set; }
    }
}

