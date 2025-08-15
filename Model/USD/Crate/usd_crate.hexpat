#pragma author ducc
#pragma description OpenUSD Crate File

import std.string;

struct Section {
  char Name[16];
  u64 Start;
  u64 End;
};

// Table of Contents
struct Toc {
    u64 Count;
    Section Sections[Count];
};

struct Version {
    u8 Major;
    u8 Minor;
    u8 Patch;
    padding[5];
};

struct Bootstrap {
    u64 Magic;
    Version Version;
    u64 TocOffset;
    u64 Reserved;
};

struct TokensSection {
    u64 NumTokens;
    u64 UncompressedSize;
    u64 CompressedSize;
    u8 NumChunks;
    u8 CompressedData[CompressedSize - 1];
};

struct IndexList {
    u64 NumEntries;
    u32 Indices[NumEntries];
};

fn FindSection( ref Toc toc, str findName )
{
    for( u8 i = 0, i < toc.Count, i += 1 )
    {
        Section section = toc.Sections[i];
        str name = section.Name;
        if ( std::string::starts_with( name, findName ) ) {
            return section.Start;
        }
    }
    return 0x0;
};

Bootstrap Bootstrap @ 0x0;
Toc TableOfContents @ Bootstrap.TocOffset;

TokensSection TokensSection @ FindSection( TableOfContents, "TOKENS" );
IndexList StringsSection @ FindSection( TableOfContents, "STRINGS" );