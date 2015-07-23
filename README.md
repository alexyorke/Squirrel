# Squirrel
A tool to download the minimap of an Everybody Edits world quickly.

There are two ways to load the world data: (see `Program.cs`)
- the initialization (`init`) data (slower; latest data)
- use BigDB data (faster; latest saved data)
 
Minimaps are downloaded, optionally compressed and saved as `[worldId].png`.

### Dependencies
- nQuant
- PlayerIOClient

### Included classes
FastPixel: http://bit.ly/1OAMTNJ
