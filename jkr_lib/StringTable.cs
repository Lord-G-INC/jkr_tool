using System.Collections.Frozen;

namespace jkr_lib;

public class StringTable {
    public Dictionary<u32, string> Table {get; init;} = [];

    public FrozenDictionary<string, u32> Lookup => Table.Select(k => (k.Value, k.Key))
    .ToDictionary((k) => k.Value, (v) => v.Key).ToFrozenDictionary(); 

    protected List<byte> Data {get; init;} = [];

    public static StringTable FromArchive(BinaryStream stream, JKRArchive arch) {
        StringTable table = new() {
        Data = new(stream.SeekTask(arch.DataHeader.StringTableOffset + arch.Header.HeaderSize, (x) => {
            byte[] bytes = new byte[arch.DataHeader.StringTableSize];
            x.Read(bytes);
            return bytes;
        })) };
        int i = 0;
        u32 off = 0;
        while (i < table.Data.Count) {
            List<byte> bytes = [];
            for (byte b = table.Data[i++]; b != 0; b = table.Data[i++])
                bytes.Add(b);
            string str = Encoding.UTF8.GetString([.. bytes]);
            table += (off, str);
            off += (u32)bytes.Count + 1;
            if (table.Data[i..].All(x => x is 0)) {
                int len = table.Data[i..].Count;
                table.Data.RemoveRange(i, len);
                break;
            }
        }
        return table;
    }

    public string this[u32 key] {
        get => Table[key];
        set => Table[key] = value;
    }

    public u32 this[string key] => Lookup[key];

    public u32 Add(string item) {
        if (Lookup.TryGetValue(item, out var off))
            return off;
        else {
            var o = (u32)Data.Count;
            var bytes = Encoding.UTF8.GetBytes(item);
            Data.AddRange(bytes);
            Data.Add(0);
            Table.Add(o, item);
            return o;
        }
    }

    public u32[] Add(params string[] items) {
        List<u32> nums = [];
        foreach (var item in items)
            nums.Add(Add(item));
        return [.. nums];
    }

    public static StringTable operator+(StringTable table, string item) {
        table.Add(item);
        return table;
    }

    public static StringTable operator+(StringTable table, (u32 off, string str) tup) {
        table.Table.Add(tup.off, tup.str);
        return table;
    }

    public byte[] ToArray() {
        while (Data.Count % 32 != 0)
            Data.Add(0);
        return [.. Data];
    }
}