namespace Mute.Moe.Discord.Services.Responses.Eliza.Engine
{
	public sealed class Transform
	{
	    public string Source { get; }
        public string Destination { get; }

		internal Transform(string source, string destination)
		{
			Source = source;
			Destination = destination;
		}
	}
}
