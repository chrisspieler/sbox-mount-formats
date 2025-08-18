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

struct StringsSection {
    u64 NumEntries;
    u32 Indices[NumEntries];
};

struct CompressedData {
    u64 DataSize;
    u8 Data[DataSize];
};

struct FieldsSection {
    u64 NumFields;
    CompressedData Indices;
    CompressedData Values;
};

struct FieldSetsSection {
    u64 NumFieldSets;
    CompressedData Data;
};

struct PathsSection {
    u64 NumPaths;
    u64 NumEncodedPaths;
    CompressedData PathIndices;
    CompressedData ElementTokenIndices;
    CompressedData Jumps;
};

struct SpecsSection {
    u64 NumSpecs;
    CompressedData PathIndices;
    CompressedData FieldSetIndices;
    CompressedData SpecTypes;
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

TokensSection Tokens @ FindSection( TableOfContents, "TOKENS" );
StringsSection Strings @ FindSection( TableOfContents, "STRINGS" );
FieldsSection Fields @ FindSection( TableOfContents, "FIELDS" );
FieldSetsSection FieldSets @ FindSection( TableOfContents, "FIELDSETS" );
PathsSection Paths @ FindSection( TableOfContents, "PATHS" );
SpecsSection Specs @ FindSection( TableOfContents, "SPECS" );