# Squirrel
A tool to download the minimap of a Everybody Edits world quickly.

There are two ways to load the world data: (see `Program.cs`)
- the initialization (`init`) data
- use BigDB data (latest saved data)
 
Minimaps are downloaded, compressed and saved as `[worldId].png`.

### Dependencies
- nQuant
- PlayerIOClient

### Included classes
FastPixel: http://www.codeproject.com/Articles/15192/FastPixel-A-much-faster-alternative-to-Bitmap-SetP