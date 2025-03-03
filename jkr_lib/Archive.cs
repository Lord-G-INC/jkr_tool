using static Binary_Stream.Util;

namespace jkr_lib;
/// <summary>
/// A Node within a JKRArchive that represents a Folder, containing other Nodes as children.
/// </summary>
public class JKRFolderNode : IRead, IWrite {
    /// <summary>
    /// The inner binary data of a <see cref="JKRFolderNode"/>.
    /// </summary>
    public record struct Node : IRead, IWrite {
        public Node() {}
        public string ShortName = string.Empty;
        public u32 NameOffs;
        public u16 Hash;
        public u16 FileCount;
        public u32 FirstFileOffs;

        public void Read(BinaryStream stream) {
            ShortName = stream.ReadString(4);
            stream.ReadUnmanaged(ref NameOffs);
            stream.ReadUnmanaged(ref Hash);
            stream.ReadUnmanaged(ref FileCount);
            stream.ReadUnmanaged(ref FirstFileOffs);
        }
        public readonly void Write(BinaryStream stream) {
            stream.WriteString(ShortName);
            stream.WriteUnmanaged(NameOffs);
            stream.WriteUnmanaged(Hash);
            stream.WriteUnmanaged(FileCount);
            stream.WriteUnmanaged(FirstFileOffs);
        }
    }
    public Node mNode;
    public bool IsRoot = false;
    public string Name = string.Empty;
    public JKRFileNode? FileNode;
    public List<JKRFileNode> ChildNodes = [];

    public void Read(BinaryStream stream) {
        stream.ReadItem(ref mNode);
    }
    /// <summary>
    /// Gets the shortname of a Node, if this Node is the Root, it will return ROOT.
    /// </summary>
    public string GetShortName() {
        if (IsRoot)
            return "ROOT";
        var ret = Name;
        while (ret.Length < 4)
            ret += ' ';
        return ret[..4].ToUpper();
    }

    public override string ToString()
    {
        List<string> names = [];
        names.Add(Name);
        var node = FileNode?.ParentNode;
        while (node != null) {
            names.Add(node.Name);
            node = node.FileNode?.ParentNode;
        }
        names.Reverse();
        return Path.Join([.. names]);
    }
    /// <summary>
    /// Unpacks this Node and its children to a directory.
    /// </summary>
    /// <param name="dir">The directory to unpack into</param>
    public void Unpack(DirectoryInfo dir) {
        if (IsRoot)
           Directory.CreateDirectory(Path.Join(dir.FullName, Name));
        dir.Create();
        foreach (var child in ChildNodes) {
            if (child.Name is "." || child.Name is "..")
                continue;
            var fullpath = dir.FullName.Contains(Name) switch {
                true => Path.Join(dir.FullName, child.Name),
                false => Path.Join(dir.FullName, Name, child.Name)
            };
            if (child.IsDir) {
                Directory.CreateDirectory(fullpath);
                child.FolderNode?.Unpack(new(fullpath));
            } else if (child.IsFile)
                File.WriteAllBytes(fullpath, child.Data);
        }
    }

    public void Write(BinaryStream stream) {
        stream.WriteString(GetShortName());
        stream.WriteUnmanaged(mNode.NameOffs);
        stream.WriteUnmanaged(CalcHash(Name));
        stream.WriteUnmanaged((u16)ChildNodes.Count);
        stream.WriteUnmanaged(mNode.FirstFileOffs);
    }
}
/// <summary>
/// A Node that represents a "File", despite possibly having a file, 
/// some FileNodes can point to a <see cref="JKRFolderNode"/>.
/// It's best to check the Node's <see cref="Attr"/> to be sure.
/// </summary>
public class JKRFileNode : IRead, IWrite {
    /// <summary>
    /// The inner binary data of a <see cref="JKRFileNode"/>
    /// </summary>
    public record struct Node: IRead {
        public u16 NodeIdx;
        public u16 Hash;
        public u32 AttrAndNameOffs;
        public u32 Data;
        public u32 DataSize;

        public void Read(BinaryStream stream) {
            stream.ReadUnmanaged(ref NodeIdx);
            stream.ReadUnmanaged(ref Hash);
            stream.ReadUnmanaged(ref AttrAndNameOffs);
            stream.ReadUnmanaged(ref Data);
            stream.ReadUnmanaged(ref DataSize);
        }
    }
    public Node mNode;
    public JKRFileAttr Attr;
    public JKRFolderNode? FolderNode;
    public JKRFolderNode? ParentNode;
    public string Name = string.Empty;
    public u16 NameOffs;
    public u8[] Data = [];
    /// <summary>
    /// Gets info from the <see cref="Attr"/> to see if this Node is a "Directory".
    /// If this is true, this Node points to a <see cref="JKRFolderNode"/>.
    /// </summary>
    public bool IsDir => Attr.HasFlag(JKRFileAttr.FOLDER);
    /// <summary>
    /// Gets info from the <see cref="Attr"/> to see if this Node is a File.
    /// If this is true, <see cref="Data"/>'s length will not be 0.
    /// </summary>
    public bool IsFile => Attr.HasFlag(JKRFileAttr.FILE);
    /// <summary>
    /// This is a special property to see if this Node is a "Shortcut".
    /// A shortcut node is a "Directory" that will either point to the owning <see cref="JKRFolderNode"/>,
    /// or the parent <see cref="JKRFolderNode"/>.
    /// </summary>
    public bool IsShortcut {
        get {
            if (Name == ".." || Name == ".")
                return IsDir;
            return false;
        }
    }
    /// <summary>
    /// Determines how this Node is to be loaded on a Wii/GCN's real hardware.
    /// </summary>
    public JKRPreloadType PreloadType { get
    {
        if (IsFile)
        {
            if (Attr.HasFlag(JKRFileAttr.LOAD_TO_MRAM))
                return JKRPreloadType.MRAM;
            else if (Attr.HasFlag(JKRFileAttr.LOAD_TO_ARAM))
                return JKRPreloadType.ARAM;
            else if (Attr.HasFlag(JKRFileAttr.LOAD_FROM_DVD))
                return JKRPreloadType.DVD;
        }
        return JKRPreloadType.NONE;
    } }

    public void Read(BinaryStream stream) {
        stream.ReadItem(ref mNode);
        NameOffs = (u16)(mNode.AttrAndNameOffs & 0x00FFFFFF);
        Attr = (JKRFileAttr)(mNode.AttrAndNameOffs >> 24);
    }

    public void Write(BinaryStream stream) {
        stream.WriteUnmanaged(mNode.NodeIdx);
        stream.WriteUnmanaged(CalcHash(Name));
        u32 attr = (u32)Attr;
        stream.WriteUnmanaged((attr << 24) | NameOffs);
        stream.WriteUnmanaged(mNode.Data);
        stream.WriteUnmanaged(mNode.DataSize);
    }

    public override string ToString()
    {
        List<string> names = [];
        names.Add(Name);
        var node = ParentNode;
        while (node != null) {
            names.Add(node.Name);
            node = node.FileNode?.ParentNode;
        }
        names.Reverse();
        return Path.Join([.. names]);
    }
    /// <summary>
    /// Creates a brand new Node using the arguements as information.
    /// If <paramref name="parent"/> is not null,
    /// the new Node will be added to it's <see cref="JKRFolderNode.ChildNodes"/>.
    /// </summary>
    /// <param name="name">The Node's name</param>
    /// <param name="attr">The Node's attributes</param>
    /// <param name="node">The Node's owning folder, if any.</param>
    /// <param name="parent">The Node's parent folder, if any.</param>
    /// <returns></returns>
    public static JKRFileNode CreateNode(string name, JKRFileAttr attr, JKRFolderNode? node, JKRFolderNode? parent) {
        var dir = new JKRFileNode() {
            Name = name,
            Attr = attr,
            FolderNode = node,
            ParentNode = parent
        };
        parent?.ChildNodes.Add(dir);
        return dir;
    }
    /// <summary>
    /// Loads this Node's <see cref="Data"/> into a <typeparamref name="T"/> that implements <see cref="IRead"/>.
    /// <typeparamref name="T"/> must also have a default ctor.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="endian">The endian to use</param>
    /// <param name="enc">The encoding for the <see cref="BinaryStream"/>, if needed.</param>
    /// <returns></returns>
    public T LoadInto<T>(Endian endian, Encoding? enc = null) where T : IRead, new()
    {
        return FromBytes<T>(Data, endian, enc);
    }
}
/// <summary>
/// A RARC/CRAR file, the main way the format should be dealt with using this library.
/// </summary>
public class JKRArchive : IRead, IWrite {
    public JKRArchiveHeader Header;
    public JKRArchiveDataHeader DataHeader = new() {Sync = true};
    public List<JKRFolderNode> FolderNodes = [];
    public List<JKRFileNode> FileNodes = [];
    public JKRFolderNode Root = new();
    /// <summary>
    /// Gets or Sets to sync up all nodes when Writing/Reading
    /// </summary>
    public bool Sync {get => DataHeader.Sync; set => DataHeader.Sync = value;}
    /// <summary>
    /// The next avalable <see cref="JKRFileNode.Node.NodeIdx"/>, capped at 65535.
    /// </summary>
    public u16 NextIdx {get => DataHeader.NextIdx; set => DataHeader.NextIdx = value;}
    public JKRArchive() {}
    /// <summary>
    /// Utility ctor to open a file and read it in, DOES NOT HANDLE YAZ0.
    /// </summary>
    /// <param name="info">The file to read.</param>
    public JKRArchive(FileInfo info) : this(File.ReadAllBytes(info.FullName)) {}
    /// <summary>
    /// Reads a read-only span of bytes for data. Yaz0 is NOT handled by this ctor, make sure to decompress it!
    /// </summary>
    public JKRArchive(ReadOnlySpan<byte> span) {
        using BinaryStream stream = new(span);
        Read(stream);
    }
    /// <summary>
    /// Reads a stream for data. Yaz0 is NOT handled by this ctor, make sure to decompress it!
    /// </summary>
    public JKRArchive(Stream stream) {
        using BinaryStream reader = new(stream);
        Read(reader);
    }
    /// <summary>
    /// Reads a BinaryStream for data. Yaz0 is NOT handled by this ctor, make sure to decompress it!
    /// </summary>
    public JKRArchive(BinaryStream stream) {
        Read(stream);
    }

    public override string ToString() => Root.ToString();

    public void Read(BinaryStream stream) {
        var magic = stream.ReadString(4);
        if (magic != "RARC" && magic != "CRAR")
            return;
        var endian = magic switch {
            "RARC" => Endian.Big,
            "CRAR" => Endian.Little,
            _ => BinaryStream.Native
        };
        stream.Endian = endian;
        Header.Read(stream);
        DataHeader.Read(stream);
        var table = StringTable.FromArchive(stream, this);
        stream.Seek(DataHeader.DirNodeOffset + Header.HeaderSize, SeekOrigin.Begin);
        FolderNodes.Capacity = (int)DataHeader.DirNodeCount;
        FileNodes.Capacity = (int)DataHeader.FileNodeCount;
        for (int i = 0; i < DataHeader.DirNodeCount; i++) {
            var node = stream.ReadItem<JKRFolderNode>();
            node.Name = table[node.mNode.NameOffs];
            if (!Root.IsRoot) {
                node.IsRoot = true;
                Root = node;
            }
            FolderNodes.Add(node);
        }
        stream.Seek(DataHeader.FileNodeOffset + Header.HeaderSize, SeekOrigin.Begin);
        for (int i = 0; i < DataHeader.FileNodeCount; i++) {
            var dir = stream.ReadItem<JKRFileNode>();
            stream.Position += 4;
            dir.Name = table[dir.NameOffs];
            if (dir.IsDir && dir.mNode.Data != u32.MaxValue) {
                dir.FolderNode = FolderNodes[(int)dir.mNode.Data];
                if (dir.FolderNode.mNode.Hash == dir.mNode.Hash)
                    dir.FolderNode.FileNode = dir;
            } else if (dir.IsFile) {
                var pos = Header.FileDataOffset + Header.HeaderSize + dir.mNode.Data;
                Array.Resize(ref dir.Data, (int)dir.mNode.DataSize);
                var seek = new Seek<BinaryStream>(stream, pos);
                stream.Read(dir.Data);
                seek.Dispose();
            }
            FileNodes.Add(dir);
        }
        for (int i = 0; i < FolderNodes.Count; i++) {
            var node = FolderNodes[i];
            int off = (int)node.mNode.FirstFileOffs;
            int count = off + node.mNode.FileCount;
            for (int y = off; y < count; y++) {
                FileNodes[y].ParentNode = FolderNodes[i];
                FolderNodes[i].ChildNodes.Add(FileNodes[y]);
            }
        }
    }
    /// <summary>
    /// Unpacks the Root Node into a directory. For more info see <seealso cref="JKRFolderNode.Unpack(DirectoryInfo)"/>
    /// </summary>
    public void Unpack(DirectoryInfo info) {
        Root.Unpack(info);
    }

    protected void SortNodesandDirs(JKRFolderNode node) {
        List<JKRFileNode> folders = [];
        List<JKRFileNode> shortcuts = [];
        foreach (var dir in node.ChildNodes) {
            if (dir.IsShortcut)
                shortcuts.Add(dir);
            else if (dir.IsDir)
                folders.Add(dir);
        }
        foreach (var shortcut in shortcuts) {
            var index = shortcut.FolderNode switch {
                null => u32.MaxValue,
                _ => (u32)FolderNodes.IndexOf(shortcut.FolderNode)
            };
            shortcut.mNode.Data = index;
            node.ChildNodes.Remove(shortcut);
            node.ChildNodes.Add(shortcut);
        }
        node.mNode.FirstFileOffs = (u32)FileNodes.Count;
        node.mNode.FileCount = (u16)node.ChildNodes.Count;
        FileNodes.AddRange(node.ChildNodes);
        foreach (var dir in folders) {
            var index = (u32)FolderNodes.IndexOf(dir.FolderNode!);
            dir.mNode.Data = index;
            SortNodesandDirs(dir.FolderNode!);
        }
    }
    /// <summary>
    /// Sorts the nodes by how Nintendo sorts them
    /// </summary>
    public void SortNodesandDirs() {
        FileNodes.Clear();
        SortNodesandDirs(Root);
        Recalc_File_Indeces();
    }

    protected void Recalc_File_Indeces() {
        if (Sync) {
            NextIdx = (u16)FileNodes.Count;
            foreach (var (i, dir) in FileNodes.Select((d, id) => (id, d)))
                if (dir.IsFile)
                    dir.mNode.NodeIdx = (u16)i;
        } else {
            u16 file_id = 0;
            foreach (var dir in FileNodes)
                if (dir.IsFile)
                    dir.mNode.NodeIdx = file_id++;
            NextIdx = file_id;
        }
    }
    /// <summary>
    /// Creates the Root node if it does not exist, only call this if you are making a new Archive!
    /// </summary>
    /// <param name="name"></param>
    public void CreateRoot(string name) {
        if (Root.IsRoot)
            return;
        Root = new JKRFolderNode() {
            Name = name,
            IsRoot = true,
            mNode = new() {
                ShortName = "ROOT"
            }
        };
        FolderNodes.Add(Root);
        JKRFileNode.CreateNode(".", JKRFileAttr.FOLDER, Root, Root);
        JKRFileNode.CreateNode("..", JKRFileAttr.FOLDER, null, Root);
        SortNodesandDirs();
    }
    /// <summary>
    /// Creates a new archive based off name and wanting to sync.
    /// By default sync is turned ON.
    /// </summary>
    public static JKRArchive CreateArchive(string name, bool sync = true) {
        JKRArchive arch = new() { Sync = sync };
        arch.CreateRoot(name);
        return arch;
    }
    /// <summary>
    /// Creates a new FolderNode based off the name and possible parent.
    /// </summary>
    public JKRFolderNode CreateFolder(string name, JKRFolderNode? parent) {
        JKRFolderNode node = new() {
            Name = name
        };
        node.mNode.ShortName = node.GetShortName();
        var dir = JKRFileNode.CreateNode(name, JKRFileAttr.FOLDER, node, parent);
        JKRFileNode.CreateNode(".", JKRFileAttr.FOLDER, node, node);
        JKRFileNode.CreateNode("..", JKRFileAttr.FOLDER, parent, node);
        node.FileNode = dir;
        FolderNodes.Add(node);
        SortNodesandDirs();
        return node;
    }
    /// <summary>
    /// Creates a new FileNode bassed off the name, possible parent, and attributes.
    /// </summary>
    public JKRFileNode CreateFile(string name, JKRFolderNode? parent, JKRFileAttr attr) {
        var dir = JKRFileNode.CreateNode(name, attr, null, parent);
        if (!Sync)
            dir.mNode.NodeIdx = NextIdx++;
        return dir;
    }
    /// <summary>
    /// Imports files and directories from a path on your machine.
    /// </summary>
    public void ImportFromFolder(string filepath, JKRFileAttr attr) {
        if (!Root.IsRoot) {
            var lastidx = filepath.LastIndexOf('\\');
            var name = filepath[(lastidx + 1)..];
            CreateRoot(name);
        }
        ImportNode(filepath, attr, Root);
        SortNodesandDirs();
    }

    protected void ImportNode(string filepath, JKRFileAttr attr, JKRFolderNode? parent) {
        foreach (var entry in Directory.GetFileSystemEntries(filepath)) {
            if (Directory.Exists(entry)) {
                DirectoryInfo info = new(entry);
                if (info.Name is "." || info.Name is "..") continue;
                var node = CreateFolder(info.Name, parent);
                ImportNode(info.FullName, attr, node);
            } else if (File.Exists(entry)) {
                FileInfo info = new(entry);
                if (info.Name is "." || info.Name is "..") continue;
                var node = CreateFile(info.Name, parent, attr);
                node.Data = File.ReadAllBytes(info.FullName);
                node.mNode.DataSize = (u32)node.Data.Length;
            }
        }
    }

    protected byte[] CollectStrings() {
        StringTable table = new();
        table.Add(".", "..", Root.Name);
        Root.mNode.NameOffs = 5;
        CollectStrings(table, Root);
        return table.ToArray();
    }

    protected static void CollectStrings(StringTable table, JKRFolderNode node) {
        for (int i = 0; i < node.ChildNodes.Count; i++) {
            var dir = node.ChildNodes[i];
            if (dir.Name is ".")
                dir.NameOffs = 0;
            else if (dir.Name is "..")
                dir.NameOffs = 2;
            else {
                dir.NameOffs = (u16)table.Add(dir.Name);
            }
            if (dir.IsDir && !dir.IsShortcut) {
                dir.FolderNode!.mNode.NameOffs = dir.NameOffs;
                CollectStrings(table, dir.FolderNode!);
            }
        }
    }

    protected static long WriteFileData(BinaryStream stream, List<JKRFileNode> files) {
        var start = stream.Position;
        for (int i = 0; i < files.Count; i++) {
            files[i].mNode.Data = (uint)(stream.Position - start);
            stream.Write(files[i].Data);
            while (stream.Position % 32 != 0)
                stream.WriteByte(0);
        }
        files.Clear();
        return stream.Position - start;
    }

    public void Write(BinaryStream stream) {
        List<JKRFileNode> mram = [], aram = [], dvd = [];
        foreach (var dir in FileNodes)
            if (dir.PreloadType is JKRPreloadType.MRAM)
                mram.Add(dir);
            else if (dir.PreloadType is JKRPreloadType.ARAM)
                aram.Add(dir);
            else if (dir.PreloadType is JKRPreloadType.DVD)
                dvd.Add(dir);
        var fnodecount = FolderNodes.Count;
        var dnodecount = FileNodes.Count;
        var dnodeoff = 0x40 + Align32(fnodecount * 0x10);
        var stroff = dnodeoff + Align32(dnodecount * 0x14);
        var strdata = CollectStrings();
        stream.Seek(stroff, SeekOrigin.Begin);
        stream.Write(strdata);
        stream.Seek(0x40, SeekOrigin.Begin);
        foreach (var node in FolderNodes)
            node.Write(stream);
        stream.Seek(0, SeekOrigin.End);
        while (stream.Position % 32 != 0)
            stream.WriteByte(0);
        var fdataoff = stream.Position - 0x20;
        var mram_size = WriteFileData(stream, mram);
        var aram_size = WriteFileData(stream, aram);
        var dvd_size = WriteFileData(stream, dvd);
        var total_size = (u32)(mram_size + aram_size + dvd_size);
        stream.Seek(dnodeoff, SeekOrigin.Begin);
        foreach (var node in FileNodes) {
            node.Write(stream);
            stream.WriteUnmanaged(0);
        }
        stream.Seek(0, SeekOrigin.Begin);
        stream.WriteString(stream.Endian is Endian.Big ? "RARC" : "CRAR");

        JKRArchiveHeader header = new() {
            FileSize = (u32)stream.Length,
            HeaderSize = 0x20,
            FileDataOffset = (u32)fdataoff,
            FileDataSize = total_size,
            MRAMSize = (u32)mram_size,
            ARAMSize = (u32)aram_size,
            DVDFileSize = (u32)dvd_size
        };
        header.Write(stream);
        
        JKRArchiveDataHeader dataheader = new() {
            DirNodeCount = (u32)fnodecount,
            DirNodeOffset = 0x20,
            FileNodeCount = (u32)dnodecount,
            FileNodeOffset = (u32)dnodeoff - 0x20,
            StringTableSize = (u32)strdata.Length,
            StringTableOffset = (u32)stroff - 0x20,
            NextIdx = NextIdx,
            Sync = Sync
        };
        dataheader.Write(stream);
    }
    /// <summary>
    /// Writes all the data of this Archive to bytes, depending on the Endian.
    /// </summary>
    public byte[] ToBytes(Endian endian, Encoding? enc = null) => Binary_Stream.Util.ToBytes(this, endian, enc);
    /// <summary>
    /// Attempts to find a FolderNode either based on it's full path or name.
    /// </summary>
    public IEnumerable<JKRFolderNode> FindFolder(string name) {
        return FolderNodes.Where(x => x.Name == name || x.ToString() == name);
    }
    /// <summary>
    /// Attempts to find a FileNode either based on it's full path or name.
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public IEnumerable<JKRFileNode> FindFile(string name) {
        return FileNodes.Where(x => x.Name == name || x.ToString() == name);
    }
}