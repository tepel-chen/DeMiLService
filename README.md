# DeMiL

You can access to [DeMiLMissionViewer](https://ktane.timwi.de/More/DeMiLMissionViewer/index.html) to see the missions you have installed, see their details, and start the mission.
Once you press "Save and disable all mods", the mission mods will be disabled in game, but you can still view them through the web page.

Feel free to make any application using DeMiL API.
DeMiL API Reference https://github.com/tepel-chen/DeMiLService/wiki/API-Reference


## Build

You need to compile DeMiLAssembly before you build the mod in Unity.
To do this, first, if you have different game path, change `<GameFolder Condition="'$(GameFolder)' == ''" />` accordingly. 
Then, open DeMiLAssembly.csproj with Visual Studio and run `Build DeMiLAssembly`.