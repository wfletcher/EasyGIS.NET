using System.Collections.Generic;
using System.Linq;

namespace EGIS.Mapbox.Vector.Tile
{
    public static class AttributesParser
    {
        //public static List<AttributeKeyValue> Parse(List<string> keys, List<Tile.Value> values, List<uint> tags)
        //{
        //    var result = new List<AttributeKeyValue>();
        //    var odds = tags.GetOdds().ToList();
        //    var evens = tags.GetEvens().ToList();

        //    for (var i = 0; i < evens.Count; i++)
        //    {
        //        var key = keys[(int)evens[i]];
        //        var val = values[(int)odds[i]];
        //        result.Add(GetAttr(key,val));
        //    }
        //    return result;
        //}

        public static List<AttributeKeyValue> Parse(List<string> keys, List<Tile.Value> values, List<uint> tags)
        {
            var result = new List<AttributeKeyValue>();

            for (var i = 0; i < tags.Count;)
            {
                var key = keys[(int)tags[i++]];
                var val = values[(int)tags[i++]];
                result.Add(GetAttr(key, val));
            }
            return result;
        }

        private static AttributeKeyValue GetAttr(string key,Tile.Value value)
        {
            AttributeKeyValue res = null;

            if (value.HasBoolValue)
            {
                res = new AttributeKeyValue(key,value.BoolValue);
            }
            else if (value.HasDoubleValue)
            {
                res = new AttributeKeyValue(key,value.DoubleValue);
            }
            else if (value.HasFloatValue)
            {
                res = new AttributeKeyValue(key,value.FloatValue);
            }
            else if (value.HasIntValue)
            {
                res = new AttributeKeyValue(key,value.IntValue);
            }
            else if (value.HasStringValue)
            {
                res = new AttributeKeyValue(key,value.StringValue);
            }
            else if (value.HasSIntValue)
            {
                res = new AttributeKeyValue(key,value.SintValue, AttributeType.SIntValue);
            }
            else if (value.HasUIntValue)
            {
                res = new AttributeKeyValue(key,value.UintValue);
            }
            return res;
        }


        //public static Tile.Value ToTileValue(AttributeValue value)
        //{
        //    Tile.Value tileValue = new Tile.Value();
        //    if (value.AttributeType == AttributeType.StringValue)
        //    {
        //        tileValue.StringValue = value.Value;
        //    }
        //    else if (value.AttributeType == AttributeType.BoolValue)
        //    {
        //        tileValue.BoolValue = value.Value;
        //    }
        //    else if (value.AttributeType == AttributeType.DoubleValue)
        //    {
        //        tileValue.DoubleValue = value.Value;
        //    }
        //    else if (value.AttributeType == AttributeType.FloatValue)
        //    {
        //        tileValue.FloatValue = value.Value;
        //    }
        //    else if (value.AttributeType == AttributeType.IntValue)
        //    {
        //        tileValue.IntValue = value.Value;
        //    }
        //    else if (value.AttributeType == AttributeType.UIntValue)
        //    {
        //        tileValue.UintValue = value.Value;
        //    }
        //    else if (value.AttributeType == AttributeType.SIntValue)
        //    {
        //        tileValue.SintValue = value.Value;
        //    }
        //    else
        //    {
        //        throw new System.Exception(string.Format("Could not determine tileValue. valye type is {0}", value.GetType()));
        //    }
        //    return tileValue;
        //}

    }
}
