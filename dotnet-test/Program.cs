using Graffle.FlowSdk;
using Graffle.FlowSdk.Services.Nodes;
using Grpc.Core;
using Grpc.Net.Client;

namespace DotNetTest
{
    internal class Program
    {
        private static List<string> events = new List<string>()
        {
            "A.1654653399040a61.FlowToken.TokensWithdrawn",
            "A.1654653399040a61.FlowToken.TokensDeposited",
            "A.f919ee77447b7497.FlowFees.TokensDeposited",
            "A.f919ee77447b7497.FlowFees.FeesDeducted"
        };
        private static async Task Main(string[] args)
        {
            var uri = new Uri("http://special-few-patron.flow-mainnet.quiknode.pro:9000", UriKind.RelativeOrAbsolute);

            var options = new GrpcChannelOptions()
            {
                Credentials = ChannelCredentials.Insecure,
                MaxReceiveMessageSize = null, //null = no limit
            };

            using var channel = GrpcChannel.ForAddress(uri, options);
            var client = new GraffleClient(channel, Sporks.MainNet());

            var latestBlock = (await client.GetLatestBlockAsync(true)).Height;
            var lastBlock = latestBlock;
            while (true)
            {
                latestBlock = (await client.GetLatestBlockAsync(true)).Height;
                if (lastBlock >= latestBlock)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(500));
                    continue;
                }

                ulong maxBlock;
                if (latestBlock - lastBlock + 1ul > 250ul)
                {
                    maxBlock = lastBlock + 250ul - 1;
                }
                else
                {
                    maxBlock = latestBlock;
                }

                Console.WriteLine($"[{lastBlock}, {maxBlock}] {maxBlock - lastBlock + 1ul} blocks");
                await Parallel.ForEachAsync(events, async (e, _) =>
                {
                    try
                    {
                        var events = await client.GetEventsForHeightRangeAsync(e, lastBlock, maxBlock);

                        Console.WriteLine($"{e} : {events.Count}");
                    }
                    catch (Exception)
                    {
                        Console.WriteLine($"{e} : FAILED");
                        throw;
                    }
                });

                lastBlock = maxBlock + 1ul;
            }
        }
    }
}