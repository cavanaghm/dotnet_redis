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
var wg = new List<Task>();
Store store = new Store();
Console.WriteLine("Server started");

while (true) {
	HandleRequest();
}

async Task HandleRequest() {
	var client = await server.AcceptTcpClientAsync();
	await ProcessClient(client);
}


async Task ProcessClient(TcpClient client) {
	using var _ = client;
	while (true) {
		var result = await ReceiveMessage(client);
		var args = ParseRESP(result);
		if (args == null) {
			client.GetStream().Write(Encoding.UTF8.GetBytes("-ERR Protocol error\r\n"));
			continue;
		}

		await HandleCommand(client, args);
	}
}

async Task<byte[]> ReceiveMessage(TcpClient socket) {
	var buffer = new byte[1024];
	var result = await socket.GetStream().ReadAsync(buffer, 0, buffer.Length);

	return buffer;
}

async Task HandleCommand(TcpClient client, List<string> args) {
	string command = args[0].ToUpper();
	var stream = client.GetStream();
	// Console.Write("Command: " );
	// args.ForEach(arg => Console.Write(arg + " "));
	// Console.WriteLine();
	try {
	switch(command) {
		case "PING":
			if (args.Count > 2) {
				await stream.WriteAsync(ArgCountError(command));
				break;
			}
			if (args.Count == 2) {
				await stream.WriteAsync(Encoding.UTF8.GetBytes("+"+args[1]+"\r\n"));
				break;
			}
			await stream.WriteAsync(pong);
			break;
		case "ECHO":
			if (args.Count != 2) {
				await stream.WriteAsync(ArgCountError(command));
				break;
			}
			await stream.WriteAsync(Encoding.UTF8.GetBytes("+"+args[1]+"\r\n"));
			break;
		case "SET":
			var setArgs = SetCommandParser(args);
			store.Set(setArgs);
			await stream.WriteAsync(Encoding.UTF8.GetBytes("+OK\r\n"));
			break;
		case "GET":
			var res = store.Get(args[1]);
			if (res == null) {
				await stream.WriteAsync(Encoding.UTF8.GetBytes("$-1\r\n"));
				break;
			}
			await stream.WriteAsync(Encoding.UTF8.GetBytes("+"+res+"\r\n"));
			break;
		default:
			await stream.WriteAsync(pong);
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
				commands.ttl = int.Parse(input[4]);
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
