// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { dotnet } from './_framework/dotnet.js'

const { setModuleImports, getAssemblyExports, getConfig, runMainAndExit } = await dotnet
    .withDiagnosticTracing(false)
    .create();

const config = getConfig();
const exports = await getAssemblyExports(config.mainAssemblyName);
console.log(exports);
const text = exports.TsMap.Exporter.JSBaseExporter.LoadMap("C:\\Program Files (x86)\\Steam\\steamapps\\common\\Euro Truck Simulator 2");
console.log(text, "dio");
