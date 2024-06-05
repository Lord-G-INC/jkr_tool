using BinaryStream stream = new();

stream.WriteUnmanaged(1, 2);

var data = stream.ToArray();

Console.WriteLine(data.Length);