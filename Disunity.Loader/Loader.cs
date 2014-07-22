using System;
using System.IO;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using YamlDotNet.Core.Events;

namespace Disunity.Loader
{
	public class BoolConverter : IYamlTypeConverter
	{
		public bool Accepts (Type type)
		{
			return type == typeof (bool);
		}

		public object ReadYaml (IParser parser, Type type)
		{
			var evt = parser.Current;
			var scalar = evt as Scalar;
			if (scalar == null)
			{
				throw new InvalidOperationException (
					string.Format ("Reading a bool out of {0}", evt));
			}

			parser.MoveNext ();

			int i;
			if (Int32.TryParse (scalar.Value, out i))
			{
				return i != 0;
			}

			return bool.Parse (scalar.Value);
		}

		public void WriteYaml (IEmitter emitter, object value, Type type)
		{
			throw new NotImplementedException (
				"Custom YAML writing.");
		}
	}

	public class RenderSettings
	{
		// public static Color ambientLight { get; set; }

		[YamlAlias("m_FlareFadeSpeed")]
		public /* static */ float flareFadeSpeed { get; set; }

		[YamlAlias("m_FlareStrength")]
		public /* static */ float flareStrength { get; set; }

		[YamlAlias("m_Fog")]
		public /* static */ bool fog { get; set; }

		// public static Color fogColor { get; set; }

		[YamlAlias("m_FogDensity")]
		public /* static */ float fogDensity { get; set; }

		[YamlAlias("m_LinearFogEnd")]
		public /* static */ float fogEndDistance { get; set; }

		// public static FogMode fogMode { get; set; }

		[YamlAlias("m_LinearFogStart")]
		public /* static */ float fogStartDistance { get; set; }

		[YamlAlias("m_HaloStrength")]
		public /* static */ float haloStrength { get; set; }

		// public static Material skybox { get; set; }
	}

	public class Top
	{
		public RenderSettings RenderSettings { get; set; }
	}

	public class Loader
	{
		public Loader ()
		{
		}

		public static void Main (string[] args)
		{
			var stream = File.OpenText (args [0]);
			var reader = new EventReader (new Parser (stream));
			var deserializer = new Deserializer (ignoreUnmatched: true);

			deserializer.RegisterTagMapping ("tag:unity3d.com,2011:104", typeof (Top));
			deserializer.RegisterTypeConverter (new BoolConverter ());

			reader.Allow<StreamStart> ();
			if (reader.Accept<DocumentStart> ())
			{
				// Skip SceneSettings as we don't know
				// which class to map this to.
				reader.SkipThisAndNestedEvents ();
			}

			var something = deserializer.Deserialize<Top> (reader);

			Console.WriteLine ("Deserialized {0}.", something);
			Console.WriteLine ("\tflareFadeSpeed: {0}", something.RenderSettings.flareFadeSpeed);
			Console.WriteLine ("\tflareStrength: {0}", something.RenderSettings.flareStrength);
			Console.WriteLine ("\tfog: {0}", something.RenderSettings.fog);
			Console.WriteLine ("\tfogDensity: {0}", something.RenderSettings.fogDensity);
			Console.WriteLine ("\tfogEndDistance: {0}", something.RenderSettings.fogEndDistance);
			Console.WriteLine ("\tfogStartDistance: {0}", something.RenderSettings.fogStartDistance);
			Console.WriteLine ("\thaloStrength: {0}", something.RenderSettings.haloStrength);
		}
	}
}
