using System.Collections.Generic;
using System.IO;
using ProtoBuf;

namespace EGIS.Mapbox.Vector.Tile
{
    public static class VectorTileParser
    {
        public static List<VectorTileLayer> Parse(Stream stream)
        {
            var tile = Serializer.Deserialize<Tile>(stream);
            var list = new List<VectorTileLayer>();
            foreach (var layer in tile.Layers)
            {
                var extent = layer.Extent;
                var vectorTileLayer = new VectorTileLayer();
                vectorTileLayer.Name = layer.Name;
                vectorTileLayer.Version = layer.Version;
                vectorTileLayer.Extent = layer.Extent;

                foreach (var feature in layer.Features)
                {
                    var vectorTileFeature = FeatureParser.Parse(feature, layer.Keys, layer.Values, extent);
                    vectorTileLayer.VectorTileFeatures.Add(vectorTileFeature);
                }
                list.Add(vectorTileLayer);
            }
            return list;
        }

        public static void Encode(List<VectorTileLayer> layers, Stream stream)
        {
            Tile tile = new Tile();

            foreach (var vectorTileLayer in layers)
            {
                Tile.Layer tileLayer = new Tile.Layer();
                tile.Layers.Add(tileLayer);
                
                tileLayer.Name = vectorTileLayer.Name;
                tileLayer.Version = vectorTileLayer.Version;
                tileLayer.Extent = vectorTileLayer.Extent;

                //index the key value attributes
                List<string> keys = new List<string> ();
                List<AttributeKeyValue> values = new List<AttributeKeyValue>();

                Dictionary<string, int> keysIndex = new Dictionary<string, int>();
                Dictionary<dynamic, int> valuesIndex = new Dictionary<dynamic, int>();

                foreach (var feature in vectorTileLayer.VectorTileFeatures)
                {
                    foreach (var keyValue in feature.Attributes)
                    {
                        if (!keysIndex.ContainsKey(keyValue.Key))
                        {
                            keysIndex.Add(keyValue.Key, keys.Count);
                            keys.Add(keyValue.Key);
                        }
                        if (!valuesIndex.ContainsKey(keyValue.Value))
                        {
                            valuesIndex.Add(keyValue.Value, values.Count);
                            values.Add(keyValue);
                        }
                    }
                }

                for(int n=0;n< vectorTileLayer.VectorTileFeatures.Count; ++n)
                {
                    var feature = vectorTileLayer.VectorTileFeatures[n];

                    Tile.Feature tileFeature = new Tile.Feature();
                    tileLayer.Features.Add(tileFeature);

                    ulong id;
                    if (!ulong.TryParse(feature.Id, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out id))
                    {
                        id = (ulong)(n + 1);
                    }
                    tileFeature.Id = id;
                    tileFeature.Type = feature.GeometryType;
                    GeometryParser.EncodeGeometry(feature.Geometry, feature.GeometryType, tileFeature.Geometry);
                    foreach (var keyValue in feature.Attributes)
                    {
                        tileFeature.Tags.Add((uint)keysIndex[keyValue.Key]);
                        tileFeature.Tags.Add((uint)valuesIndex[keyValue.Value]);
                    }                    
                }
                
                tileLayer.Keys.AddRange(keys);
                foreach (var value in values)
                {
                    tileLayer.Values.Add(AttributeKeyValue.ToTileValue(value));
                }
                
            }

            Serializer.Serialize<Tile>(stream, tile);

        }


    }
}
