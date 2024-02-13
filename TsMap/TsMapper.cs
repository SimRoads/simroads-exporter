using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using TsMap.Common;
using TsMap.FileSystem;
using TsMap.Helpers;
using TsMap.Helpers.Logger;
using TsMap.Map.Overlays;
using TsMap.TsItem;

namespace TsMap
{
    public class TsMapper
    {
        private readonly string _gameDir;
        private List<Mod> _mods;

        public bool IsEts2 = true;

        private List<Tuple<string, TsMapSettings>> _sectorFiles;

        public MapOverlayManager OverlayManager { get; private set; }
        public LocalizationManager Localization { get; private set; }

        protected readonly Dictionary<ulong, TsPrefab> _prefabLookup = new Dictionary<ulong, TsPrefab>();
        protected readonly Dictionary<ulong, TsCity> _citiesLookup = new Dictionary<ulong, TsCity>();
        protected readonly Dictionary<ulong, TsCountry> _countriesLookup = new Dictionary<ulong, TsCountry>();
        protected readonly Dictionary<int, TsCountry> _countriesLookupById = new Dictionary<int, TsCountry>();
        protected readonly Dictionary<ulong, TsRoadLook> _roadLookup = new Dictionary<ulong, TsRoadLook>();
        protected readonly Dictionary<ulong, TsCompany> _companyLookup = new Dictionary<ulong, TsCompany>();
        protected readonly Dictionary<ulong, TsFerry> _ferryLookup = new Dictionary<ulong, TsFerry>();

        public readonly Dictionary<ulong, TsRoadItem> Roads = new Dictionary<ulong, TsRoadItem>();
        public readonly Dictionary<ulong, TsPrefabItem> Prefabs = new Dictionary<ulong, TsPrefabItem>();
        public readonly Dictionary<ulong, TsMapAreaItem> MapAreas = new Dictionary<ulong, TsMapAreaItem>();
        public readonly Dictionary<ulong, TsCityItem> Cities = new Dictionary<ulong, TsCityItem>();
        public readonly Dictionary<ulong, TsFerryItem> FerryPorts = new Dictionary<ulong, TsFerryItem>();

        public readonly Dictionary<ulong, TsNode> Nodes = new Dictionary<ulong, TsNode>();

        public float minX = float.MaxValue;
        public float maxX = float.MinValue;
        public float minZ = float.MaxValue;
        public float maxZ = float.MinValue;

        public Rectangle BackgroundPos { get; private set; }
        public OverlayImage[] Backgrounds { get; private set; } = new OverlayImage[4];
        public TsMapSettings MapSettings { get; private set;}

        private List<TsSector> Sectors { get; set; }

        public readonly Dictionary<ulong, TsItem.TsItem> MapItems = new Dictionary<ulong, TsItem.TsItem>();

        public TsMapper(string gameDir, List<Mod> mods)
        {
            _gameDir = gameDir;
            _mods = mods;
            Sectors = new List<TsSector>();

            OverlayManager = new MapOverlayManager();
            Localization = new LocalizationManager();
        }

        public virtual List<DlcGuard> GetDlcGuardsForCurrentGame()
        {
            return IsEts2
                ? Consts.DefaultEts2DlcGuards
                : Consts.DefaultAtsDlcGuards;
        }

        private void ParseCityFiles()
        {
            var defDirectory = UberFileSystem.Instance.GetDirectory("def");
            if (defDirectory == null)
            {
                Logger.Instance.Error("Could not read 'def' dir");
                return;
            }

            foreach (var cityFileName in defDirectory.GetFiles("city"))
            {
                var cityFile = UberFileSystem.Instance.GetFile($"def/{cityFileName}");

                var data = cityFile.Entry.Read();
                var lines = Encoding.UTF8.GetString(data).Split('\n');
                foreach (var line in lines)
                {
                    if (line.TrimStart().StartsWith("#")) continue;
                    if (line.Contains("@include"))
                    {
                        var path = PathHelper.GetFilePath(line.Split('"')[1], "def");
                        var city = new TsCity(path);
                        if (city.Token != 0 && !_citiesLookup.ContainsKey(city.Token))
                        {
                            _citiesLookup.Add(city.Token, city);
                        }
                    }
                }
            }
        }

        private void ParseCountryFiles()
        {
            var defDirectory = UberFileSystem.Instance.GetDirectory("def");
            if (defDirectory == null)
            {
                Logger.Instance.Error("Could not read 'def' dir");
                return;
            }

            foreach (var countryFilePath in defDirectory.GetFiles("country"))
            {
                var countryFile = UberFileSystem.Instance.GetFile($"def/{countryFilePath}");

                var data = countryFile.Entry.Read();
                var lines = Encoding.UTF8.GetString(data).Split('\n');
                foreach (var line in lines)
                {
                    if (line.TrimStart().StartsWith("#")) continue;
                    if (line.Contains("@include"))
                    {
                        var path = PathHelper.GetFilePath(line.Split('"')[1], "def");
                        var country = new TsCountry(path);
                        if (country.Token != 0 && !_countriesLookup.ContainsKey(country.Token))
                        {
                            _countriesLookup.Add(country.Token, country);
                            _countriesLookupById.Add(country.CountryId, country);

                            if (country.CountryCode != string.Empty)
                                OverlayManager.AddOverlay(country.CountryCode, OverlayType.Flag, country.X, country.Y, "Flag", 0, country);
                        }
                    }
                }
            }
        }

        private void ParseBackground()
        {
            var defDirectory = UberFileSystem.Instance.GetDirectory("def");
            if (defDirectory == null)
            {
                Logger.Instance.Error("Could not read 'def' dir");
                return;
            }


            var mapDataFiles = new Stack<UberFile>();
            mapDataFiles.Push(UberFileSystem.Instance.GetFile("def/map_data.sii"));
            PointF mapCenter = PointF.Empty, mapSize = PointF.Empty;
            while (mapDataFiles.Count > 0 && (mapCenter == PointF.Empty || mapSize == PointF.Empty))
            {
                var data = mapDataFiles.Pop().Entry.Read();
                var lines = Encoding.UTF8.GetString(data).Split('\n');
                foreach (var line in lines)
                {
                    if (line.TrimStart().StartsWith("#")) continue;
                    if (line.Contains("@include"))
                    {
                        mapDataFiles.Push(UberFileSystem.Instance.GetFile(line.Split('"')[1].Substring(1)));
                    }
                    if (line.Contains("ui_map_center") || line.Contains("ui_map_size"))
                    {
                        var x = float.Parse(line.Split('(')[1].Split(',')[0].Trim(), CultureInfo.InvariantCulture.NumberFormat);
                        var z = float.Parse(line.Split(')')[0].Split(',')[1].Trim(), CultureInfo.InvariantCulture.NumberFormat);
                        if (line.Contains("ui_map_center"))
                        {
                            mapCenter = new PointF(x, z);
                        }
                        else
                        {
                            mapSize = new PointF(x, z);
                        }
                    }
                }
            }
            var settings = _sectorFiles.First().Item2;
            mapSize.X = mapSize.X * 1000f / settings.Scale;
            mapSize.Y = mapSize.Y * 1000f / settings.Scale;
            BackgroundPos = new Rectangle((int)(mapSize.X / -2.0f + mapCenter.X), (int)(mapSize.Y / -2.0f + mapCenter.Y), (int)mapSize.X, (int)mapSize.Y);

            for (int i = 0; i < 4; i++)
            {
                Backgrounds[i] = new OverlayImage($"material/ui/map/map{i}.dds");
                Backgrounds[i].Parse();
            }
        }

        private void ParsePrefabFiles()
        {
            var worldDirectory = UberFileSystem.Instance.GetDirectory("def/world");
            if (worldDirectory == null)
            {
                Logger.Instance.Error("Could not read 'def/world' dir");
                return;
            }

            foreach (var prefabFileName in worldDirectory.GetFiles("prefab"))
            {
                if (!prefabFileName.StartsWith("prefab")) continue;
                var prefabFile = UberFileSystem.Instance.GetFile($"def/world/{prefabFileName}");

                var data = prefabFile.Entry.Read();
                var lines = Encoding.UTF8.GetString(data).Split('\n');

                var token = 0UL;
                var path = "";
                var category = "";
                foreach (var line in lines)
                {
                    var (validLine, key, value) = SiiHelper.ParseLine(line);
                    if (validLine)
                    {
                        if (key == "prefab_model")
                        {
                            token = ScsToken.StringToToken(SiiHelper.Trim(value.Split('.')[1]));
                        }
                        else if (key == "prefab_desc")
                        {
                            path = PathHelper.EnsureLocalPath(value.Split('"')[1]);
                        }
                        else if (key == "category")
                        {
                            category = value.Contains('"') ? value.Split('"')[1] : value.Trim();
                        }
                    }

                    if (line.Contains("}") && token != 0 && path != "")
                    {
                        var prefab = new TsPrefab(path, token, category);
                        if (prefab.Token != 0 && !_prefabLookup.ContainsKey(prefab.Token))
                        {
                            _prefabLookup.Add(prefab.Token, prefab);
                        }

                        token = 0;
                        path = "";
                        category = "";
                    }
                }
            }
        }

        private void ParseRoadLookFiles()
        {
            var worldDirectory = UberFileSystem.Instance.GetDirectory("def/world");
            if (worldDirectory == null)
            {
                Logger.Instance.Error("Could not read 'def/world' dir");
                return;
            }

            foreach (var roadLookFileName in worldDirectory.GetFiles("road_look"))
            {
                if (!roadLookFileName.StartsWith("road")) continue;
                var roadLookFile = UberFileSystem.Instance.GetFile($"def/world/{roadLookFileName}");

                var data = roadLookFile.Entry.Read();
                var lines = Encoding.UTF8.GetString(data).Split('\n');
                TsRoadLook roadLook = null;

                foreach (var line in lines)
                {
                    var (validLine, key, value) = SiiHelper.ParseLine(line);
                    if (validLine)
                    {
                        if (key == "road_look")
                        {
                            roadLook = new TsRoadLook(ScsToken.StringToToken(SiiHelper.Trim(value.Split('.')[1].Trim('{'))));
                        }
                        if (roadLook == null) continue;
                        if (key == "lanes_left[]")
                        {
                            roadLook.LanesLeft.Add(value);
                        }
                        else if (key == "lanes_right[]")
                        {
                            roadLook.LanesRight.Add(value);
                        }
                        else if (key == "road_offset")
                        {
                            roadLook.Offset = float.Parse(value, CultureInfo.InvariantCulture);
                        }
                    }

                    if (line.Contains("}") && roadLook != null)
                    {
                        if (roadLook.Token != 0 && !_roadLookup.ContainsKey(roadLook.Token))
                        {
                            _roadLookup.Add(roadLook.Token, roadLook);
                            roadLook = null;
                        }
                    }
                }
            }
        }

        private void ParseFerryConnections()
        {
            var ferryDirectory = UberFileSystem.Instance.GetDirectory("def/ferry");
            if (ferryDirectory == null)
            {
                Logger.Instance.Error("Could not read 'def/ferry' dir");
                return;
            }

            foreach (var ferryFilePath in ferryDirectory.GetFilesByExtension("def/ferry", ".sui", ".sii"))
            {
                var ferry = new TsFerry(ferryFilePath);
                _ferryLookup[ferry.Token] = ferry;
            }

            var connectionDirectory = UberFileSystem.Instance.GetDirectory("def/ferry/connection");
            if (connectionDirectory == null)
            {
                Logger.Instance.Error("Could not read 'def/ferry/connection' dir");
                return;
            }

            foreach (var ferryConnectionFilePath in connectionDirectory.GetFilesByExtension("def/ferry/connection", ".sui", ".sii"))
            {
                var ferryConnectionFile = UberFileSystem.Instance.GetFile(ferryConnectionFilePath);

                var data = ferryConnectionFile.Entry.Read();
                var lines = Encoding.UTF8.GetString(data).Split('\n');

                TsFerryConnection conn = null;

                foreach (var line in lines)
                {
                    var (validLine, key, value) = SiiHelper.ParseLine(line);
                    if (validLine)
                    {
                        if (conn != null)
                        {
                            if (key.Contains("connection_positions"))
                            {
                                var index = int.Parse(key.Split('[')[1].Split(']')[0]);
                                var vector = value.Split('(')[1].Split(')')[0];
                                var values = vector.Split(',');
                                var x = float.Parse(values[0], CultureInfo.InvariantCulture);
                                var z = float.Parse(values[2], CultureInfo.InvariantCulture);
                                conn.AddConnectionPosition(index, x, z);
                            }
                            else if (key.Contains("connection_directions"))
                            {
                                var index = int.Parse(key.Split('[')[1].Split(']')[0]);
                                var vector = value.Split('(')[1].Split(')')[0];
                                var values = vector.Split(',');
                                var x = float.Parse(values[0], CultureInfo.InvariantCulture);
                                var z = float.Parse(values[2], CultureInfo.InvariantCulture);
                                conn.AddRotation(index, Math.Atan2(z, x));
                            }
                        }

                        if (key == "ferry_connection")
                        {
                            var portIds = value.Split('.');
                            conn = new TsFerryConnection(_ferryLookup[ScsToken.StringToToken(portIds[1])],
                                _ferryLookup[ScsToken.StringToToken(portIds[2].TrimEnd('{').Trim())]);
                        }
                    }

                    if (!line.Contains("}") || conn == null) continue;
                }
            }

        }

        private void ParseCompanyFiles()
        {
            var defDirectory = UberFileSystem.Instance.GetDirectory("def");
            if (defDirectory == null)
            {
                Logger.Instance.Error("Could not read 'def' dir");
                return;
            }

            foreach (var companyFileName in defDirectory.GetFiles("company"))
            {
                var companyFile = UberFileSystem.Instance.GetFile($"def/{companyFileName}");

                var data = companyFile.Entry.Read();
                var lines = Encoding.UTF8.GetString(data).Split('\n');
                foreach (var line in lines)
                {
                    if (line.TrimStart().StartsWith("#")) continue;
                    if (line.Contains("@include"))
                    {
                        var path = PathHelper.GetFilePath(line.Split('"')[1], "def");
                        var company = new TsCompany(path);
                        if (company.Token != 0 && !_companyLookup.ContainsKey(company.Token))
                        {
                            _companyLookup.Add(company.Token, company);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Parse all definition files
        /// </summary>
        private void ParseDefFiles()
        {
            var startTime = DateTime.Now.Ticks;
            ParseCityFiles();
            Logger.Instance.Info($"Loaded {_citiesLookup.Count} cities in {(DateTime.Now.Ticks - startTime) / TimeSpan.TicksPerMillisecond}ms");

            startTime = DateTime.Now.Ticks;
            ParseCountryFiles();
            Logger.Instance.Info($"Loaded {_countriesLookup.Count} countries in {(DateTime.Now.Ticks - startTime) / TimeSpan.TicksPerMillisecond}ms");

            startTime = DateTime.Now.Ticks;
            ParsePrefabFiles();
            Logger.Instance.Info($"Loaded {_prefabLookup.Count} prefabs in {(DateTime.Now.Ticks - startTime) / TimeSpan.TicksPerMillisecond}ms");

            startTime = DateTime.Now.Ticks;
            ParseCompanyFiles();
            Logger.Instance.Info($"Loaded {_companyLookup.Count} companies in {(DateTime.Now.Ticks - startTime) / TimeSpan.TicksPerMillisecond}ms");

            startTime = DateTime.Now.Ticks;
            ParseRoadLookFiles();
            Logger.Instance.Info($"Loaded {_roadLookup.Count} roads in {(DateTime.Now.Ticks - startTime) / TimeSpan.TicksPerMillisecond}ms");

            startTime = DateTime.Now.Ticks;
            ParseFerryConnections();
            Logger.Instance.Info($"Loaded {_ferryLookup.Count} ferry ports in {(DateTime.Now.Ticks - startTime) / TimeSpan.TicksPerMillisecond}ms");

            startTime = DateTime.Now.Ticks;
            ParseBackground();
            Logger.Instance.Info($"Loaded background in {(DateTime.Now.Ticks - startTime) / TimeSpan.TicksPerMillisecond}ms");
        }

        /// <summary>
        /// Parse all .base files
        /// </summary>
        private void LoadSectorFiles()
        {
            var baseMapEntry = UberFileSystem.Instance.GetDirectory("map");
            if (baseMapEntry == null)
            {
                Logger.Instance.Error("Could not read 'map' dir");
                return;
            }

            var mbdFilePaths = baseMapEntry.GetFilesByExtension("map", ".mbd"); // Get the map names from the mbd files
            if (mbdFilePaths.Count == 0)
            {
                Logger.Instance.Error("Could not find mbd file");
                return;
            }

            _sectorFiles = new List<Tuple<string, TsMapSettings>>();

            foreach (var filePath in mbdFilePaths)
            {
                var mapName = PathHelper.GetFileNameWithoutExtensionFromPath(filePath);
                IsEts2 = !(mapName == "usa");

                var mapFileDir = UberFileSystem.Instance.GetDirectory($"map/{mapName}");
                if (mapFileDir == null)
                {
                    Logger.Instance.Error($"Could not read 'map/{mapName}' directory");
                    return;
                }

                MapSettings = new TsMapSettings(this, filePath);

                _sectorFiles.AddRange(mapFileDir.GetFilesByExtension($"map/{mapName}", ".base").Select(x => new Tuple<string, TsMapSettings>(x, MapSettings)));
            }
        }

        /// <summary>
        /// Parse through all .scs files and retrieve all necessary files
        /// </summary>
        public virtual void Parse()
        {
            var startTime = DateTime.Now.Ticks;

            if (!Directory.Exists(_gameDir))
            {
                Logger.Instance.Error("Could not find Game directory.");
                return;
            }

            UberFileSystem.Instance.AddSourceDirectory(_gameDir);

            _mods.Reverse(); // Highest priority mods (top) need to be loaded last

            foreach (var mod in _mods)
            {
                if (mod.Load) UberFileSystem.Instance.AddSourceFile(mod.ModPath);
            }

            UberFileSystem.Instance.AddSourceFile(Path.Combine(Environment.CurrentDirectory,
                "custom_resources.zip"));

            Logger.Instance.Info($"Loaded all .scs files in {(DateTime.Now.Ticks - startTime) / TimeSpan.TicksPerMillisecond}ms");

            LoadSectorFiles();
            ParseDefFiles();

            var preLocaleTime = DateTime.Now.Ticks;
            Localization.LoadLocaleValues();
            Logger.Instance.Info($"It took {(DateTime.Now.Ticks - preLocaleTime) / TimeSpan.TicksPerMillisecond} ms to read all locale files");

            if (_sectorFiles == null) return;
            var preMapParseTime = DateTime.Now.Ticks;
            Sectors = _sectorFiles.Select(file => new TsSector(this, file.Item1, file.Item2)).ToList();
            Sectors.ForEach(sec => sec.Parse());
            Sectors.ForEach(sec => sec.ClearFileData());
            Logger.Instance.Info($"It took {(DateTime.Now.Ticks - preMapParseTime) / TimeSpan.TicksPerMillisecond} ms to parse all (*.base) files");

            foreach (var mapItem in MapItems.Values)
            {
                mapItem.Update();
            }

            Logger.Instance.Info($"Loaded {OverlayManager.GetOverlayImagesCount()} overlay images, with {OverlayManager.GetOverlays().Count} overlays on the map");
            Logger.Instance.Info($"It took {(DateTime.Now.Ticks - startTime) / TimeSpan.TicksPerMillisecond} ms to fully load.");
        }

        public void UpdateEdgeCoords(TsNode node)
        {
            if (minX > node.X) minX = node.X;
            if (maxX < node.X) maxX = node.X;
            if (minZ > node.Z) minZ = node.Z;
            if (maxZ < node.Z) maxZ = node.Z;
        }

        public TsNode GetNodeByUid(ulong uid)
        {
            return Nodes.ContainsKey(uid) ? Nodes[uid] : null;
        }

        public TsItem.TsItem GetItemByUid(ulong uid)
        {
            return MapItems.ContainsKey(uid) ? MapItems[uid] : null;
        }

        public TsCountry GetCountryByTokenName(string name)
        {
            var token = ScsToken.StringToToken(name);
            return _countriesLookup.ContainsKey(token) ? _countriesLookup[token] : null;
        }

        public TsCountry GetCountryById(int id)
        {
            return _countriesLookupById.ContainsKey(id) ? _countriesLookupById[id] : null;
        }

        public TsRoadLook LookupRoadLook(ulong lookId)
        {
            return _roadLookup.ContainsKey(lookId) ? _roadLookup[lookId] : null;
        }

        public TsPrefab LookupPrefab(ulong prefabId)
        {
            return _prefabLookup.ContainsKey(prefabId) ? _prefabLookup[prefabId] : null;
        }

        public TsCity LookupCity(ulong cityId)
        {
            return _citiesLookup.ContainsKey(cityId) ? _citiesLookup[cityId] : null;
        }

        public TsCompany LookupCompany(ulong companyId)
        {
            return _companyLookup.ContainsKey(companyId) ? _companyLookup[companyId] : null;
        }

        public TsFerry LookupFerry(ulong ferryPortId)
        {
            return _ferryLookup.ContainsKey(ferryPortId) ? _ferryLookup[ferryPortId] : null;
        }

    }
}
