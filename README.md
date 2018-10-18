## Euro Truck Simulator 2 / American Truck Simulator Map Renderer

This Application reads ATS/ETS2 files to draw roads, prefabs, map overlays, ferry lines and city names.

### Map mod support (WIP)
It can now load map mods.

Making all/specific map mods supported won't be a priority for me.

#### Supported* map mods:

ETS2:
- Promods V2.31
- Rusmap V1.8.1
- The Great Steppe V1.2
- Paris Rebuild V2.3
- ScandinaviaMod V0.4
- Emden V1.02c (Doesn't show city name)
- Sardinia map V0.9 (Can't load some dds files)

ATS:
- Haven't tested any yet

\* The supported maps load and it gets drawn but I haven't looked at anything specific so it's always possible there will be some items missing or things will get drawn that shouldn't.

![Preview of the map](/docs/preview.jpg "Preview of the map")

### Supported maps / DLC
- ATS
    - Base
    - Nevada
    - Arizona
    - New Mexico
    - Oregon
- ETS2
    - Base
    - Going East!
    - Scandinavia
    - Vive la France !
    - Italia

#### Dependencies (NuGet)
[DotNetZip](https://www.nuget.org/packages/DotNetZip/)

#### Based on
[Original project](https://github.com/nlhans/ets2-map)
