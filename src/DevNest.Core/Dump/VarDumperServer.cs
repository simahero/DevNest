using PhpSerializerNET;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace DevNest.Core.Dump;

public class VarDumperServer
{
    public ObservableCollection<object> Dumps { get; set; } = new();

    public async Task StartAsync(int port = 9912)
    {
        var listener = new TcpListener(IPAddress.Loopback, port);
        listener.Start();
        System.Diagnostics.Debug.WriteLine($"VarDumper server listening on tcp://127.0.0.1:{port}");

        while (true)
        {
            var client = await listener.AcceptTcpClientAsync();
            _ = HandleClientAsync(client);
        }
    }

    private async Task HandleClientAsync(TcpClient client)
    {
        System.Diagnostics.Debug.WriteLine("VarDumper client connected.");

        try
        {
            using var stream = client.GetStream();
            var buffer = new byte[8192];

            while (client.Connected)
            {
                int bytesRead = await stream.ReadAsync(buffer);
                if (bytesRead == 0) break;

                var b64rawData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                System.Diagnostics.Debug.WriteLine($"VarDumper received data: {b64rawData}");

                byte[] base64EncodedBytes = Convert.FromBase64String(b64rawData);
                string decodedString = System.Text.Encoding.UTF8.GetString(base64EncodedBytes);

                try
                {
                    var deserializedObject = PhpSerialization.Deserialize(decodedString);
                    if (deserializedObject != null)
                    {
                        Dumps.Add(deserializedObject);
                    }

                    System.Diagnostics.Debug.WriteLine("Deserialized object:");
                    System.Diagnostics.Debug.WriteLine(JsonSerializer.Serialize(deserializedObject));

                    //DumpReceived?.Invoke(deserializedObject);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error deserializing data: {ex.Message}");
                }

                // Symfony VarDumper sends formatted dump data
                // For now, just display the raw dump content

            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"VarDumper client error: {ex.Message}");
        }
        finally
        {
            System.Diagnostics.Debug.WriteLine("VarDumper client disconnected.");
        }

        System.Diagnostics.Debug.WriteLine("Client disconnected.");
    }
}
