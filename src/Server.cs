using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Text;

int PORT = 6379;
if (args.Length > 0) {
	if (args[0] == "-p" || args[0] == "--port") {
		PORT = int.Parse(args[1]);
		Console.WriteLine("Port: " + PORT);
	}
}
TcpListener server = new TcpListener(IPAddress.Any, PORT);
server.Start();

byte[] pong = Encoding.UTF8.GetBytes("+PONG\r\n");
// byte[] ping = Encoding.UTF8.GetBytes("*1\r\n$4\r\nping\r\n");
byte[] ArgCountError(string command) => Encoding.UTF8.GetBytes($"-ERR wrong number of arguments for '{command}' command\r\n");
Store store = new Store();
Console.WriteLine("Server started");

while (true) {
	Socket socket = await server.AcceptSocketAsync(); // wait for client
	var res = ProcessSocket(socket);
}

async Task ProcessSocket(Socket socket) {
	using var _ = socket;

	while (true) {
		var result = await ReceiveAsync(_);
		var args = ParseRESP(result);
		if (args == null) {
			await socket.SendAsync(Encoding.UTF8.GetBytes("-ERR Protocol error\r\n"), SocketFlags.None);
			continue;
		}

		await HandleCommand(socket, args);
	}
}

async static Task<byte[]> ReceiveAsync(Socket socket) {
	byte[]? buffer = null;
	try {
		buffer = ArrayPool<byte>.Shared.Rent(256);
		var result = await socket.ReceiveAsync(buffer, SocketFlags.None);
		return buffer;
	} finally {
		if (buffer != null) {
			ArrayPool<byte>.Shared.Return(buffer);
		}
	}
}

async Task HandleCommand(Socket socket, List<string> args) {
	string command = args[0].ToUpper();
	Console.Write("Command: " );
	args.ForEach(arg => Console.Write(arg + " "));
	Console.WriteLine();
	try {
	switch(command) {
		case "PING":
			if (args.Count > 2) {
				await socket.SendAsync(ArgCountError(command), SocketFlags.None);
				break;
			}
			if (args.Count == 2) {
				await socket.SendAsync(Encoding.UTF8.GetBytes("+"+args[1]+"\r\n"), SocketFlags.None);
				break;
			}
			await socket.SendAsync(pong, SocketFlags.None);
			break;
		case "ECHO":
			if (args.Count != 2) {
				await socket.SendAsync(ArgCountError(command), SocketFlags.None);
				break;
			}
			await socket.SendAsync(Encoding.UTF8.GetBytes("+"+args[1]+"\r\n"), SocketFlags.None);
			break;
		case "SET":
			var setArgs = SetCommandParser(args);
			store.Set(setArgs);
			await socket.SendAsync(Encoding.UTF8.GetBytes("+OK\r\n"), SocketFlags.None);
			break;
		case "GET":
			var res = store.Get(args[1]);
			if (res == null) {
				await socket.SendAsync(Encoding.UTF8.GetBytes("$-1\r\n"), SocketFlags.None);
				break;
			}
			await socket.SendAsync(Encoding.UTF8.GetBytes("+"+res+"\r\n"), SocketFlags.None);
			break;
		default:
			await socket.SendAsync(pong, SocketFlags.None);
			break;
	}
	} catch (Exception e) {
		if (e is SocketException) {
			Console.WriteLine("SocketException");
		}
	} finally {
		args.Clear();
	}

}

SetCommands SetCommandParser(List<string> input) {
	var commands = new SetCommands();
	commands.key = input[1];
	commands.value = input[2];
	commands.ttl = 0;

	if (input.Count == 5) {
		switch(input[3].ToUpper()) {
			case "EX":
				commands.ttl = int.Parse(input[4]) * 1000;
				break;
			case "PX":
				Console.WriteLine(input[4]);
				commands.ttl = int.Parse(input[4]);
				Console.WriteLine(commands.ttl);
				break;
			default:
				break;
		}
	}

	return commands;
}


(string, int) ArgParser(byte[] input, int pointer) {
	if (pointer + 3 >= input.Length) {
		return ("PING", -1);
	}
	char type = (char)input[pointer++];
	int count = input[pointer++] - '0';

	//Skip \r\n
	pointer += 2;
	byte nextByte = input[pointer + count + 2];
	int next = pointer + count + 2;
	if (nextByte == 0) {
		next = -1;
	}

	switch(type) {
		case '+':
			return (Encoding.UTF8.GetString(input, pointer, count), next);
		case '$':
			return (Encoding.UTF8.GetString(input, pointer, count), next);
		case '-':
			Console.WriteLine("Error");
			break;
		case ':':
			Console.WriteLine("Integer");
			break;
		case '*':
			return ArgParser(input, pointer);
		default:
			break;
	}

	return ("PING", -1);
}


List<string> ParseRESP(byte[] input) {
	var args = new List<string>();
	int i = 0;
	while (true) {
		var (res, count) = ArgParser(input, i);
		args.Add(res);
		if (count == -1) {
			return args;
		}
		i = count;
	}
}
public struct SetCommands {
	public string key;
	public string value;
	public int ttl;
}
