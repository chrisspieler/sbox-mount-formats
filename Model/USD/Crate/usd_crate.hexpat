#pragma author ducc
#pragma description OpenUSD Crate File


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

Bootstrap Bootstrap @ 0x0;
Toc TableOfContents @ Bootstrap.TocOffset;