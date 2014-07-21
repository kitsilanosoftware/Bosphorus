using System;
using System.IO;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using YamlDotNet.Core.Events;

namespace Disunity.Loader
{
	public class SceneSettings
	{
		public int m_ObjectHideFlags { get; set; }
	}

	public class Top
	{
		public SceneSettings SceneSettings { get; set; }
	}

	public class Loader
	{
		public Loader ()
		{
		}

		public static void Main (string[] args)
		{
			var stream = File.OpenText (args [0]);
			var reader = new EventReader(new Parser(stream));
			var deserializer = new Deserializer (ignoreUnmatched: true);

			deserializer.RegisterTagMapping ("tag:unity3d.com,2011:29", typeof (Top));

			reader.Allow<StreamStart> ();
			var something = deserializer.Deserialize<Top> (reader);

			Console.WriteLine ("Done, deserialized {0}.", something);
		}
	}
}
