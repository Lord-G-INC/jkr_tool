namespace jkr_tool;

public class JKRFolderNode : IRead {
    public struct Node : IRead, IWrite {
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
    public JKRDirectory? Directory;
    public List<JKRDirectory> ChildDirs = [];

    public void Read(BinaryStream stream) {
        stream.ReadItem(ref mNode);
    }

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
        var node = Directory?.ParentNode;
        while (node != null) {
            names.Add(node.Name);
            node = node.Directory?.ParentNode;
        }
        names.Reverse();
        return Path.Join([.. names]);
    }

    public void Unpack(DirectoryInfo dir) {
        if (IsRoot)
           System.IO.Directory.CreateDirectory(Path.Join(dir.FullName, Name));
        dir.Create();
        foreach (var child in ChildDirs) {
            if (child.Name is "." || child.Name is "..")
                continue;
            var fullpath = Path.Join(dir.FullName, child.ToString());
            if (child.IsDir) {
                System.IO.Directory.CreateDirectory(fullpath);
                child.FolderNode?.Unpack(new(fullpath));
            } else if (child.IsFile)
                File.WriteAllBytes(fullpath, child.Data);
        }
    }
}

public class JKRDirectory : IRead {
    public struct Node: IRead {
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

    public bool IsDir => Attr.HasFlag(JKRFileAttr.FOLDER);
    public bool IsFile => Attr.HasFlag(JKRFileAttr.FILE);
    public bool IsShortcut {
        get {
            if (Name == ".." || Name == ".")
                return IsDir;
            return false;
        }
    }
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

    public override string ToString()
    {
        List<string> names = [];
        names.Add(Name);
        var node = ParentNode;
        while (node != null) {
            names.Add(node.Name);
            node = node.Directory?.ParentNode;
        }
        names.Reverse();
        return Path.Join([.. names]);
    }

    public static JKRDirectory CreateDir(string name, JKRFileAttr attr, JKRFolderNode? node, JKRFolderNode? parent) {
        var dir = new JKRDirectory() {
            Name = name,
            Attr = attr,
            FolderNode = node,
            ParentNode = parent
        };
        parent?.ChildDirs.Add(dir);
        return dir;
    } 
}

public class JKRArchive : IRead {
    public JKRArchiveHeader Header;
    public JKRArchiveDataHeader DataHeader = new() {Sync = true};
    public List<JKRFolderNode> FolderNodes = [];
    public List<JKRDirectory> Directories = [];
    public JKRFolderNode Root = new();
    public bool Sync {get => DataHeader.Sync; set => DataHeader.Sync = value;}
    public u16 NextIdx {get => DataHeader.NextIdx; set => DataHeader.NextIdx = value;}
    public JKRArchive() {}

    public JKRArchive(FileInfo info) : this(File.ReadAllBytes(info.FullName)) {}

    public JKRArchive(ReadOnlySpan<byte> span) {
        using BinaryStream stream = new(span);
        Read(stream);
    }

    public JKRArchive(Stream stream) {
        using BinaryStream reader = new(stream);
        Read(reader);
    }

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
        stream.Seek(DataHeader.DirNodeOffset + Header.HeaderSize, SeekOrigin.Begin);
        FolderNodes.Capacity = (int)DataHeader.DirNodeCount;
        Directories.Capacity = (int)DataHeader.FileNodeCount;
        for (int i = 0; i < DataHeader.DirNodeCount; i++) {
            var node = stream.ReadItem<JKRFolderNode>();
            long pos = DataHeader.StringTableOffset + Header.HeaderSize + node.mNode.NameOffs;
            node.Name = stream.ReadNTStringAt(pos);
            if (!Root.IsRoot) {
                node.IsRoot = true;
                Root = node;
            }
            FolderNodes.Add(node);
        }
        stream.Seek(DataHeader.FileNodeOffset + Header.HeaderSize, SeekOrigin.Begin);
        for (int i = 0; i < DataHeader.FileNodeCount; i++) {
            var dir = stream.ReadItem<JKRDirectory>();
            stream.Position += 4;
            long pos = DataHeader.StringTableOffset + Header.HeaderSize + dir.NameOffs;
            dir.Name = stream.ReadNTStringAt(pos);
            if (dir.IsDir && dir.mNode.Data != u32.MaxValue) {
                dir.FolderNode = FolderNodes[(int)dir.mNode.Data];
                if (dir.FolderNode.mNode.Hash == dir.mNode.Hash)
                    dir.FolderNode.Directory = dir;
            } else if (dir.IsFile) {
                pos = Header.FileDataOffset + Header.HeaderSize + dir.mNode.Data;
                Array.Resize(ref dir.Data, (int)dir.mNode.DataSize);
                var seek = new Seek<BinaryStream>(stream, pos);
                stream.Read(dir.Data);
                seek.Dispose();
            }
            Directories.Add(dir);
        }
        for (int i = 0; i < FolderNodes.Count; i++) {
            var node = FolderNodes[i];
            int off = (int)node.mNode.FirstFileOffs;
            int count = off + node.mNode.FileCount;
            for (int y = off; y < count; y++) {
                Directories[y].ParentNode = FolderNodes[i];
                FolderNodes[i].ChildDirs.Add(Directories[y]);
            }
        }
    }

    public void Unpack(DirectoryInfo info) {
        Root.Unpack(info);
    }

    protected void SortNodesandDirs(JKRFolderNode node) {
        List<JKRDirectory> folders = [];
        List<JKRDirectory> shortcuts = [];
        foreach (var dir in node.ChildDirs) {
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
            node.ChildDirs.Remove(shortcut);
            node.ChildDirs.Add(shortcut);
        }
        node.mNode.FirstFileOffs = (u32)Directories.Count;
        Directories.AddRange(node.ChildDirs);
        foreach (var dir in folders) {
            var index = (u32)FolderNodes.IndexOf(dir.FolderNode!);
            dir.mNode.Data = index;
            SortNodesandDirs(dir.FolderNode!);
        }
    }

    public void SortNodesandDirs() {
        Directories.Clear();
        SortNodesandDirs(Root);
        Recalc_File_Indeces();
    }

    protected void Recalc_File_Indeces() {
        if (Sync) {
            NextIdx = (u16)Directories.Count;
            foreach (var (i, dir) in Directories.Select((d, id) => (id, d)))
                if (dir.IsFile)
                    dir.mNode.NodeIdx = (u16)i;
        } else {
            u16 file_id = 0;
            foreach (var dir in Directories)
                if (dir.IsFile)
                    dir.mNode.NodeIdx = file_id++;
            NextIdx = file_id;
        }
    }

    public void CreateRoot(string name) {
        if (Root.IsRoot)
            return;
        Root = new JKRFolderNode() {
            Name = name,
            IsRoot = true
        };
        FolderNodes.Add(Root);
        JKRDirectory.CreateDir(".", JKRFileAttr.FOLDER, Root, Root);
        JKRDirectory.CreateDir("..", JKRFileAttr.FOLDER, null, Root);
        SortNodesandDirs();
    }

    public static JKRArchive CreateArchive(string name, bool sync = true) {
        JKRArchive arch = new() { Sync = sync };
        arch.CreateRoot(name);
        return arch;
    }

    public JKRFolderNode CreateFolder(string name, JKRFolderNode? parent) {
        JKRFolderNode node = new() {
            Name = name
        };
        FolderNodes.Add(node);
        JKRDirectory.CreateDir(name, JKRFileAttr.FOLDER, node, parent);
        JKRDirectory.CreateDir(".", JKRFileAttr.FOLDER, node, node);
        JKRDirectory.CreateDir("..", JKRFileAttr.FOLDER, parent, node);
        SortNodesandDirs();
        return node;
    }

    public JKRDirectory CreateFile(string name, JKRFolderNode? parent, JKRFileAttr attr) {
        var dir = JKRDirectory.CreateDir(name, attr, null, parent);
        if (!Sync)
            dir.mNode.NodeIdx = NextIdx++;
        return dir;
    }

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
            }
        }
    }
}