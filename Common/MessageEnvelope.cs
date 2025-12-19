namespace Common
{
    public class MessageEnvelope
    {
        public string Type { get; set; } = "";
        public object? Payload { get; set; }
    }
}
