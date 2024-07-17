using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.IO.Compression;

public class FileBackedTcpListener
{
    private readonly X509Certificate2 _certificate;
    private readonly string _packageDirectory;
    private readonly TcpListener _tcpListener;
    private readonly string _uri;

    public FileBackedTcpListener(string packageDirectory, X509Certificate2 certificate)
    {
        _packageDirectory = packageDirectory;
        _certificate = certificate;
        _tcpListener = new TcpListener(IPAddress.Loopback, 44444); // 0 for any available port
        _tcpListener.Start();
        _uri = $"https://{_tcpListener.LocalEndpoint}/";
    }

    // Starts the server and waits for client requests
    public async Task StartServer()
    {
        Console.WriteLine($"Server started. Listening on {_tcpListener.LocalEndpoint}");

        while (true)
        {
            Console.WriteLine("Waiting for client connection...");
            var client = await _tcpListener.AcceptTcpClientAsync();
            Console.WriteLine("Client connected.");

            // Handling client in a new task to allow multiple client connections
            _ = Task.Run(() => HandleClient(client));
        }
    }

    private async Task HandleClient(TcpClient client)
    {
        using (client)
        using (var sslStream = new SslStream(client.GetStream(), false))
        {
            try
            {
                await sslStream.AuthenticateAsServerAsync(_certificate, clientCertificateRequired: false, checkCertificateRevocation: true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}");
            }
            using (var reader = new StreamReader(sslStream, Encoding.ASCII, false, 128))
            using (var writer = new StreamWriter(sslStream, Encoding.ASCII, 128, false))
            {
                try
                {
                    var requestLine = await reader.ReadLineAsync();
                    var requestParts = requestLine?.Split(' ');
                    if (requestParts == null || requestParts.Length < 2)
                    {
                        throw new InvalidOperationException("Invalid HTTP request line.");
                    }
                    Console.WriteLine($"bout to check {requestParts[0]} and {requestParts[1]}");
                    string method = requestParts[0];
                    string rawUrl = requestParts[1];

                    string path = requestParts[1];
                    var parts = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                    if (path == "/v3/index.json")
                    {
                        SendIndexJsonResponse(writer);
                    }
                    else if (parts.Length > 1 && parts[0] == "v3")
                    {
                        if (parts[1] == "package")
                        {
                            if (parts.Length == 4)
                            {
                                ProcessPackageRequest(parts[2], writer);
                            }
                            else
                            {
                                SendPackageFile(parts[2], parts[3], parts[4], writer, sslStream);
                            }
                        }
                        else
                        {
                            await writer.WriteLineAsync("HTTP/1.1 404 Not Found");
                        }
                    }
                    else
                    {
                        await writer.WriteLineAsync("HTTP/1.1 404 Not Found");
                    }
                }
                catch (Exception ex)
                {
                    // Handle exception
                    Console.WriteLine("Error processing request: " + ex.Message);
                }
            }
        }
    }

    private void ProcessPackageRequest(string id, StreamWriter writer)
    {
        try
        {
            var versions = GetVersionsFromDirectory(id);

            var json = JsonConvert.SerializeObject(new { versions });
            writer.WriteLine("HTTP/1.1 200 OK");
            writer.WriteLine("Content-Type: application/json");
            writer.WriteLine();
            writer.WriteLine(json);
            writer.Flush();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing request: {ex.Message}");
        }
    }

    private void SendPackageFile(string id, string version, string nupkg, StreamWriter writer, SslStream sslStream)
    {
        var filePath = Path.Combine(_packageDirectory, id, version, nupkg);

        if (!File.Exists(filePath))
        {
            writer.WriteLine("HTTP/1.1 404 Not Found");
            return;
        }

        using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        {
            writer.WriteLine("HTTP/1.1 200 OK");
            writer.WriteLine("Content-Type: application/octet-stream");
            writer.WriteLine($"Content-Disposition: attachment; filename=\"{id}.{version}.nupkg\"");
            writer.WriteLine($"Content-Length: {new FileInfo(filePath).Length}");
            writer.WriteLine();
            writer.Flush();

            fileStream.CopyTo(sslStream);
        }
    }

    private string[] GetVersionsFromDirectory(string id)
    {
        var directoryPath = Path.Combine(_packageDirectory, id);
        if (!Directory.Exists(directoryPath))
        {
            throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");
        }

        var dirInfo = new DirectoryInfo(directoryPath);
        return dirInfo.GetDirectories().Select(d => d.Name).ToArray();
    }

    private void SendIndexJsonResponse(StreamWriter writer)
    {
        var indexResponse = new
        {
            version = "3.0.0",
            resources = new object[]
            {
                new Resource { Type = "SearchQueryService", Id = $"{_uri}v3/query" },
                new Resource { Type = "RegistrationsBaseUrl", Id = $"{_uri}v3/registration" },
                new Resource { Type = "PackageBaseAddress/3.0.0", Id = $"{_uri}v3/package" },
                new Resource { Type = "PackagePublish/2.0.0", Id = $"{_uri}v3/packagepublish" }
            }
        };

        string jsonResponse = JsonConvert.SerializeObject(indexResponse);

        writer.WriteLine("HTTP/1.1 200 OK");
        writer.WriteLine("Content-Type: application/json");
        writer.WriteLine();
        writer.WriteLine(jsonResponse);
        writer.Flush();
    }

    public static void Main(string[] args)
    {
        X509Certificate2 certificate = GenerateSelfSignedCertificate();
        string packageDirectory = "packages/abcdefghijkl/1.0.0";

        if (!Directory.Exists(packageDirectory))
        {
            Directory.CreateDirectory(packageDirectory);
        }

        // Create test.nuspec file
        string nuspecPath = Path.Combine(packageDirectory, "abcdefghijkl.nuspec");
        File.WriteAllText(nuspecPath, @"
<package>
  <metadata>
    <id>abcdefghijkl</id>
    <version>1.0.0</version>
    <description>Testing</description>
    <authors>NuGetTest</authors>
    <title />
  </metadata>
</package>");

        // Create the .nupkg file
        string nupkgPath = Path.Combine(packageDirectory, "abcdefghijkl.1.0.0.nupkg");
        if (File.Exists(nupkgPath))
        {
            File.Delete(nupkgPath);
        }
        using (var archive = ZipFile.Open(nupkgPath, ZipArchiveMode.Create))
        {
            archive.CreateEntryFromFile(nuspecPath, "abcdefghijkl.nuspec");
        }

        // Delete the nuspec file after creating the .nupkg file
        File.Delete(nuspecPath);

        var listener = new FileBackedTcpListener("./packages/", certificate);
        listener.StartServer().Wait();
    }

    private static X509Certificate2 GenerateSelfSignedCertificate()
    {
        using (var rsa = RSA.Create(2048))
        {
            var request = new CertificateRequest("cn=test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            var start = DateTime.UtcNow;
            var end = DateTime.UtcNow.AddYears(1);
            var cert = request.CreateSelfSigned(start, end);
            var certBytes = cert.Export(X509ContentType.Pfx, "password");

            return new X509Certificate2(certBytes, "password", X509KeyStorageFlags.Exportable);
        }
    }
}

public class Resource
{
    [JsonProperty("@type")]
    public string Type { get; set; }

    [JsonProperty("@id")]
    public string Id { get; set; }
}
