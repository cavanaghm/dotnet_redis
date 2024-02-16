using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Text;

TcpListener server = new TcpListener(IPAddress.Any, 6379);
server.Start();
byte[] pong = Encoding.UTF8.GetBytes("+PONG\r\n");
byte[] ping = Encoding.UTF8.GetBytes("*1\r\n$4\r\nping\r\n");
byte[] ArgCountError(string command) => Encoding.UTF8.GetBytes($"-ERR wrong number of arguments for '{command}' command\r\n");
// ArgParser(ping, 0);

while (true) {
	Socket socket = await server.AcceptSocketAsync(); // wait for client
	var res = ProcessSocket(socket);
}

async Task ProcessSocket(Socket socket) {
	using var _ = socket;

	while (true) {
		var result = await ReceiveAsync(socket);
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
	} catch (Exception e) {
		//return empty array
		return Array.Empty<byte>();
	} finally {
		if (buffer != null) {
			ArrayPool<byte>.Shared.Return(buffer);
		}
	}
}

async Task HandleCommand(Socket socket, List<string> args) {
	string command = args[0].ToUpper();
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
			await socket.SendAsync(pong, SocketFlags.None);
			break;
		case "GET":
			await socket.SendAsync(pong, SocketFlags.None);
			break;
		default:
			await socket.SendAsync(pong, SocketFlags.None);
			break;
	}
	} catch (Exception e) {
		Console.WriteLine(e);
	} finally {
		args.Clear();
	}

}


(string, int) ArgParser(byte[] input, int pointer) {
	if (pointer + 3 >= input.Length) {
		return ("PING", -1);
	}
	char type = (char)input[pointer++];
	int count = input[pointer++] - '0';

	//Skip \r\n
	pointer += 2;

	switch(type) {
		case '+':
			return (Encoding.UTF8.GetString(input, pointer, count), pointer + count + 2);
		case '$':
			return (Encoding.UTF8.GetString(input, pointer, count), pointer + count + 2);
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


List<string>? ParseRESP(byte[] input) {
	var args = new List<string>();
	int i = 0;
	while (i < input.Length) {
		var (res, count) = ArgParser(input, i);
		if (count == -1) {
			return args;
		}
		i = count;
		args.Add(res);
	}
	if (args != null) {
		return args;
	}
	return null;
}
