﻿// For more information see https://aka.ms/fsharp-console-apps
open System
open System.IO
open System.Net
open System.Net.Sockets
let acceptClient (client : TcpClient) handler = async {
    use stream = client.GetStream()
    use reader = new StreamReader(stream)
    let header = reader.ReadLine()
    if not (String.IsNullOrEmpty(header)) then
        use writer = new StreamWriter(stream)
        handler (header, writer)
        writer.Flush()
}
let startServer(address, port) handler =
    let ip = IPAddress.Parse(address: string)
    let listener = TcpListener(ip, port)
    listener.Start()
    async {
        while true do
            let! client = listener.AcceptTcpClientAsync() |> Async.AwaitTask
            acceptClient client handler |> Async.Start
    }
    |> Async.Start
type StreamWriter with
    member writer.BinaryWrite(bytes: byte[]) =
        let writer = new BinaryWriter(writer.BaseStream)
        writer.Write(bytes)
let staticContentHandler root (header: string, response: StreamWriter) =
    let parts = header.Split(' ')
    let resource = parts.[1]
    let path = Path.Combine(root, resource.TrimStart('/').TrimStart('\\'))
    if File.Exists(path) then
        response.Write("HTTP/1.1 200 OK\r\n\r\n")
        if resource.EndsWith(".png") then
            let bytes = File.ReadAllBytes(path)
            response.BinaryWrite(bytes)
        else
            let text = File.ReadAllText(path)
            response.Write(text)
    else
        response.Write("HTTP/1.1 404 Not Found \r\n\r\n" + resource + " not found.")
startServer("127.0.0.1", 8080) (staticContentHandler @".")
