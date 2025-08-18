# s&box File Importers
This library provides utilities for extracting data from games mounted by s&box.

### Installation
Place the contents of this repo anywhere in the Editor folder of a s&box project. 

### Usage
*Coming soon!*

### Supported Formats

#### Models
*Coming soon!*

#### Images
##### DDS (DirectDraw Surface)
The following texture formats may be read from DDS files:
- DXT1
- DXT3
- DXT5

#### Sounds
*Coming soon!*

#### Compression
The following formats may be decompressed:
- LZ4

### Attributions
Because DLL references are not supported in s&box editor projects, some code has been copied directly from third party repositories. Modifications have been made to copied code to ensure compatibility with s&box.

All copied code is attributed below:

##### K4os.Compression.LZ4
Repo: https://github.com/MiloszKrajewski/K4os.Compression.LZ4
Author: [Milosz Krajewski](https://github.com/MiloszKrajewski)
License: [MIT](https://github.com/MiloszKrajewski/K4os.Compression.LZ4/blob/master/LICENSE)
Path: `/ThirdParty/K4os.Compression.LZ4`