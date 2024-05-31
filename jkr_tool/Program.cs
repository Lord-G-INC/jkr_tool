using jkr_tool;

string path = "C:\\Users\\Matt\\Documents\\LordG-INC\\jkr_tool\\GoldenAmogus.arc";
var data = File.ReadAllBytes(path);
data = Yaz0.Decompress(data);
var arch = new JKRArchive(data);
arch.Unpack(new(Directory.GetCurrentDirectory()));