// class translated from Java
// Credit goes to Charles Hayden http://www.chayden.net/eliza/Eliza.html

namespace Mute.Services.Responses.Eliza.Eliza
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
