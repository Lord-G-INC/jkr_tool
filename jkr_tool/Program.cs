string path = "C:\\Users\\Matt\\Documents\\LordG-INC\\jkr_tool\\jkr_tool\\bin\\Debug\\net8.0\\WarpAreaErrorLayout";

JKRArchive arch = JKRArchive.CreateArchive("WarpAreaErrorLayout");

arch.ImportFromFolder(path, JKRFileAttr.FILE | JKRFileAttr.LOAD_TO_MRAM);

var data = arch.ToBytes(Endian.Big);

JKRArchive other = new(data);

Console.WriteLine(other);