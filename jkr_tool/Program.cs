using jkr_lib;
using Binary_Stream;

int files = 0;
Console.WriteLine("JKR Tool 1.0\nBy Lord-Giganticus\n-------------");
foreach (var arg in args.Where(x => Directory.Exists(x) || File.Exists(x))) {
    if (Directory.Exists(arg)) {
        DirectoryInfo info = new(arg);
        // Console.WriteLine("{info.FullName} ==> {info.FullName}.arc")
        JKRArchive arch = JKRArchive.CreateArchive(info.Name);
        arch.ImportFromFolder(info.FullName, JKRFileAttr.FILE | JKRFileAttr.LOAD_TO_MRAM);
        var data = arch.ToBytes(Endian.Big);
        data = Yaz0.Compress(data);
        FileInfo finfo = new(info.FullName + ".arc");
        File.WriteAllBytes(finfo.FullName, data);
        files++;
    } else if (File.Exists(arg)) {
        jkr_lib.Util.DumpFile(arg);
        files++;
    }
}
if (files == 0) 
    Console.WriteLine("Usage:\n\njkr_tool <FileOrDir1> [FileOrDir2] [FileOrDir3] [...]\n\nWill convert all given files/folders.\nWhen given a folder, it will be packed into a RARC archive.\nWhen given a file, it will be unpacked into a folder.\nThe endian is determined by the RARC/CRAR archive.");
else 
    Console.WriteLine($"{files} files/folders converted.");