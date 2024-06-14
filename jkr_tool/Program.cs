foreach (var arg in args.Where(x => Directory.Exists(x) || File.Exists(x))) {
    if (Directory.Exists(arg)) {
        DirectoryInfo info = new(arg);
        JKRArchive arch = JKRArchive.CreateArchive(info.Name);
        arch.ImportFromFolder(info.FullName, JKRFileAttr.FILE | JKRFileAttr.LOAD_TO_MRAM);
        var data = arch.ToBytes(Endian.Big);
        data = Yaz0.Compress(data);
        FileInfo finfo = new(info.FullName + ".arc");
        File.WriteAllBytes(finfo.FullName, data);
    } else if (File.Exists(arg)) {
        FileInfo info = new(arg);
        var parent = info.Directory!;
        var data = File.ReadAllBytes(info.FullName);
        data = Yaz0.Decompress(data);
        JKRArchive arch = new(data);
        arch.Unpack(parent);
    }
}