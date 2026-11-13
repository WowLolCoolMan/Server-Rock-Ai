using ServerRock.AgentHost.Protocol;

if (!args.Contains("--stdio", StringComparer.Ordinal))
{
    Console.Error.WriteLine("Server Rock Agent Host requires --stdio for the local desktop bridge.");
    return 2;
}

await AgentHostLoop.RunAsync(Console.OpenStandardInput(), Console.OpenStandardOutput());
return 0;
