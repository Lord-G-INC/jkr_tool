namespace jkr_lib;

[Flags]
public enum JKRFileAttr {
    FILE = 0x1,
    FOLDER = 0x2,
    COMPRESSED = 0x4,
    LOAD_TO_MRAM = 0x10,
    LOAD_TO_ARAM = 0x20,
    LOAD_FROM_DVD = 0x40,
    USE_SZS = 0x80,
    FILE_AND_COMPRESSION = 0x85,
    FILE_AND_PRELOAD = 0x71
}

[Flags]
public enum JKRPreloadType {
    NONE = -1,
    MRAM = 0,
    ARAM = 1,
    DVD = 2
}