string path = "C:\\Users\\Matt\\Documents\\LordG-INC\\jkr_tool\\jkr_tool\\bin\\Debug\\net8.0\\GoldenAmogus";

var arch = JKRArchive.CreateArchive("GoldenAmogus");

arch.ImportFromFolder(path, JKRFileAttr.FILE | JKRFileAttr.LOAD_TO_MRAM);

Console.WriteLine(arch);