using System.Collections.Generic;
using System.Drawing;
using System.Text;
using TsMap.Common;
using TsMap.FileSystem;
using TsMap.Helpers;

namespace TsMap
{
    public class TsFerry
    {
        public readonly ulong Token;
        public readonly string Name;
        public readonly string LocalizationToken;
        public readonly string TransportType = "ferry";

        private List<TsFerryConnection> connections = new();

        public TsFerry(string path)
        {
            var file = UberFileSystem.Instance.GetFile(path);

            if (file == null) return;
            var fileContent = file.Entry.Read();
            var lines = Encoding.UTF8.GetString(fileContent).Split('\n');

            foreach (var line in lines)
            {
                var (validLine, key, value) = SiiHelper.ParseLine(line);
                if (!validLine) continue;

                if (key == "ferry_name")
                {
                    Name = value.Split('"')[1];
                }
                else if (key == "ferry_name_localized")
                {
                    LocalizationToken = value.Split('"')[1].Trim('@');
                }
                else if (key == "transport_type")
                {
                    TransportType = value.Split('"')[1];
                }
                else if (key == "ferry_data")
                {
                    Token = ScsToken.StringToToken( value.Split('.')[1].Trim());
                }
            }
        }

        public void AddConnection(TsFerryConnection connection)
        {
            connections.Add(connection);
        }

        public List<TsFerryConnection> GetConnections()
        {
            return connections;
        }

    }


    public class TsFerryPoint
    {
        public float X;
        public float Z;
        public double Rotation;

        public TsFerryPoint(float x, float z)
        {
            X = x;
            Z = z;
        }
        public void SetRotation(double rot)
        {
            Rotation = rot;
        }
    }

    public class TsFerryConnection
    {
        public readonly TsFerry StartPort;
        public PointF StartPortLocation { get; private set; }
        public readonly TsFerry EndPort;
        public PointF EndPortLocation { get; private set; }
        public List<TsFerryPoint> Connections = new List<TsFerryPoint>();

        public TsFerryConnection(TsFerry start, TsFerry end)
        {
            StartPort = start;
            EndPort = end;
            start.AddConnection(this);
        }

        public void AddConnectionPosition(int index, float x, float z)
        {
            if (Connections.Count > index) return;
            Connections.Add(new TsFerryPoint(x / 256, z / 256));
        }
        public void AddRotation(int index, double rot)
        {
            if (Connections.Count <= index) return;
            Connections[index].SetRotation(rot);
        }

        public void SetPortLocation(ulong ferryPortId, float x, float z)
        {
            if (ferryPortId == StartPort.Token)
            {
                StartPortLocation = new PointF(x, z);
            }
            else if (ferryPortId == EndPort.Token)
            {
                EndPortLocation = new PointF(x, z);
            }
        }
    }

}
