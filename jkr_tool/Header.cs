namespace jkr_tool;

public struct JKRArchiveHeader : IRead, IWrite {
    public u32 FileSize;
    public u32 HeaderSize;
    public u32 FileDataOffset;
    public u32 FileDataSize;
    public u32 MRAMSize;
    public u32 ARAMSize;
    public u32 DVDFileSize;

    public void Read(BinaryStream reader) {
        reader.ReadUnmanaged(ref FileSize);
        reader.ReadUnmanaged(ref HeaderSize);
        reader.ReadUnmanaged(ref FileDataOffset);
        reader.ReadUnmanaged(ref FileDataSize);
        reader.ReadUnmanaged(ref MRAMSize);
        reader.ReadUnmanaged(ref ARAMSize);
        reader.ReadUnmanaged(ref DVDFileSize);
    }

    public readonly void Write(BinaryStream stream) {
        stream.WriteUnmanaged(FileSize);
        stream.WriteUnmanaged(HeaderSize);
        stream.WriteUnmanaged(FileDataOffset);
        stream.WriteUnmanaged(FileDataSize);
        stream.WriteUnmanaged(MRAMSize);
        stream.WriteUnmanaged(ARAMSize);
        stream.WriteUnmanaged(DVDFileSize);
    }
}

public struct JKRArchiveDataHeader : IRead, IWrite {
    public u32 DirNodeCount;
    public u32 DirNodeOffset;
    public u32 FileNodeCount;
    public u32 FileNodeOffset;
    public u32 StringTableSize;
    public u32 StringTableOffset;
    public u16 NextIdx;
    public bool Sync;

    public void Read(BinaryStream stream) {
        stream.ReadUnmanaged(ref DirNodeCount);
        stream.ReadUnmanaged(ref DirNodeOffset);
        stream.ReadUnmanaged(ref FileNodeCount);
        stream.ReadUnmanaged(ref FileNodeOffset);
        stream.ReadUnmanaged(ref StringTableSize);
        stream.ReadUnmanaged(ref StringTableOffset);
        stream.ReadUnmanaged(ref NextIdx);
        stream.ReadUnmanaged(ref Sync);
    }

    public readonly void Write(BinaryStream stream) {
        stream.WriteUnmanaged(DirNodeCount);
        stream.WriteUnmanaged(DirNodeOffset);
        stream.WriteUnmanaged(FileNodeCount);
        stream.WriteUnmanaged(FileNodeOffset);
        stream.WriteUnmanaged(StringTableSize);
        stream.WriteUnmanaged(StringTableOffset);
        stream.WriteUnmanaged(NextIdx);
        stream.WriteUnmanaged(Sync);
    }
}