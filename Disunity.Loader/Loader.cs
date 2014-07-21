using System;
using System.IO;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Disunity.Loader
{
	public class Loader
	{
		public Loader ()
		{
		}

		public static void Main (string[] args)
		{
			var stream = File.OpenText (args [0]);

			var yaml = new YamlStream();
			yaml.Load(stream);

			Console.WriteLine ("Done, loaded {0} documents.", yaml.Documents.Count);
		}
	}
}
