using System.Runtime.InteropServices;

namespace jkr_lib;

public static class Util {
    public static Span<T> MakeSpan<T>(this ref T item) where T : unmanaged {
        return MemoryMarshal.CreateSpan(ref item, 1);
    }
    public static Span<TTo> Cast<TFrom, TTo>(this Span<TFrom> span) where TFrom : struct where TTo : struct {
        return MemoryMarshal.Cast<TFrom, TTo>(span);
    }
    public static void DumpFile(string file) {
        var data = File.ReadAllBytes(file);
        data = Yaz0.Decompress(data);
        var arch = new JKRArchive(data);
        arch.Unpack(new DirectoryInfo(file).Parent!);
    }
    public static void AddFilesToArchive(string archive, string folderPath, Endian endian = Endian.Big) {
        var data = File.ReadAllBytes(archive);
        data = Yaz0.Decompress(data);
        var arch = new JKRArchive(data);
        arch.ImportFromFolder(folderPath, JKRFileAttr.FILE | JKRFileAttr.LOAD_TO_MRAM);
        data = arch.ToBytes(endian);
        data = Yaz0.Compress(data);
        FileInfo finfo = new(new DirectoryInfo(archive).FullName);
        File.WriteAllBytes(finfo.FullName, data);
    }
}