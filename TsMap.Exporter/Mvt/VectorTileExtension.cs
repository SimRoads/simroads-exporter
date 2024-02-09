using System;
using System.Collections.Generic;

namespace TsMap.Exporter.Mvt
{
    public partial class Tile
    {
        public partial class Types
        {
            public partial class Layer
            {
                private Dictionary<string, Tuple<uint, Dictionary<object, uint>>> _keys = new();

                public uint[] GetOrCreateTag<T>(string key, T value)
                {
                    uint keyIndex;
                    if (_keys.ContainsKey(key))
                    {
                        keyIndex = _keys[key].Item1;
                    }
                    else
                    {
                        keyIndex = (uint)Keys.Count;
                        _keys[key] = new Tuple<uint, Dictionary<object, uint>>(keyIndex, new Dictionary<object, uint>());
                        Keys.Add(key);
                    }

                    uint valueIndex;
                    if (_keys[key].Item2.ContainsKey(value))
                    {
                        valueIndex = _keys[key].Item2[value];
                    }
                    else
                    {
                        valueIndex = (uint)Values.Count;
                        _keys[key].Item2[value] = valueIndex;
                        var layerValue = new Value();
                        switch (value)
                        {
                            case string s:
                                layerValue.StringValue = s;
                                break;
                            case bool b:
                                layerValue.BoolValue = b;
                                break;
                            case int i:
                                layerValue.IntValue = i;
                                break;
                            case uint u:
                                layerValue.UintValue = u;
                                break;
                            case ulong ul:
                                layerValue.UintValue = ul;
                                break;
                            case long l:
                                layerValue.SintValue = l;
                                break;
                            case float f:
                                layerValue.FloatValue = f;
                                break;
                            case double d:
                                layerValue.DoubleValue = d;
                                break;
                            default:
                                throw new Exception("Invalid value type");
                        }

                        Values.Add(layerValue);
                    }

                    return new uint[] { keyIndex, valueIndex };
                }

            }
        }
    }
}
