namespace binary_stream;

public class BinaryStream : MemoryStream {
    public readonly static Endian Native = BitConverter.IsLittleEndian ? Endian.Little : Endian.Big;

    public Endian Endian {get; set;} = Native;

    public bool Reverse => Endian != Native;

    public BinaryStream() : base() {}

    public BinaryStream(int capacity) : base(capacity) {}

    public BinaryStream(ReadOnlySpan<byte> bytes) : this(bytes.Length) {
        Write(bytes);
        Position = 0;
    }

    public BinaryStream(Stream other) : this((int)other.Length) {
        long oldpos = other.Position;
        other.Position = 0;
        other.CopyTo(this);
        other.Position = oldpos;
        Position = 0;
    }

    public T ReadUnmanaged<T>() where T : unmanaged {
        Span<byte> bytes = new byte[Unsafe.SizeOf<T>()];
        Read(bytes);
        if (Reverse && bytes.Length > 1)
            bytes.Reverse();
        return Unsafe.ReadUnaligned<T>(ref bytes[0]);
    }

    public void ReadUnmanaged<T>(ref T value) where T : unmanaged {
        value = ReadUnmanaged<T>();
    }

    public void WriteUnmanaged<T>(T value) where T : unmanaged {
        Span<byte> bytes = new byte[Unsafe.SizeOf<T>()];
        Unsafe.WriteUnaligned(ref bytes[0], value);
        if (Reverse && bytes.Length > 1)
            bytes.Reverse();
        Write(bytes);
    }

    public string ReadString(int len, Encoding? enc = null) {
        byte[] data = new byte[len];
        Read(data);
        enc ??= Encoding.UTF8;
        return enc.GetString(data);
    }

    public string ReadNTString(Encoding? enc = null) {
        List<byte> bytes = new(sbyte.MaxValue);
        for (byte b = (byte)ReadByte(); b != 0; b = (byte)ReadByte())
            bytes.Add(b);
        enc ??= Encoding.UTF8;
        return enc.GetString(bytes.ToArray());
    }

    public string ReadNTStringAt(long pos, Encoding? enc = null) {
        return this.SeekTask(pos, (x) => x.ReadNTString(enc));
    }

    public void WriteString(string value, Encoding? enc = null) {
        enc ??= Encoding.UTF8;
        Write(enc.GetBytes(value));
    }

    public void WriteNTString(string value, Encoding? enc = null) {
        WriteString(value, enc);
        WriteByte(0);
    }

    public void WriteNTStrings(Encoding? enc, params string[] values) {
        foreach (var s in values) WriteNTString(s, enc);
    }

    public T ReadItem<T>() where T : IRead, new() {
        T res = new();
        res.Read(this);
        return res;
    }

    public void ReadItem<T>(ref T item) where T : IRead {
        item.Read(this);
    }

    public void WriteItem<T>(T item) where T : IWrite {
        item.Write(this);
    }

    public void WriteUnmanaged(params ValueType[] values) {
        foreach (var value in values) {
            if (!value.GetType().IsUnmanaged())
                continue;
            Globals.WriteValueType(this, value);
        }
    }
}