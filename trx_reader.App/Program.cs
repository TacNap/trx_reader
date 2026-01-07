using Anthropic;
using Anthropic.Models.Messages;

class trx_reader
{
    static async Task Main()
    {
        var client = new AnthropicClient();

        MessageCreateParams parameters = new()
        {
            MaxTokens = 1024,
            Messages = [new() { Role = Role.User, Content = "Hello, Claude" }],
            Model = "claude-sonnet-4-5",
        };
        var response = await client.Messages.Create(parameters);

        var message = string.Join(
            "",
            response
                .Content.Where(message => message.Value is TextBlock)
                .Select(message => message.Value as TextBlock)
                .Select((textBlock) => textBlock.Text)
        );

        Console.WriteLine(message);
    }
}
