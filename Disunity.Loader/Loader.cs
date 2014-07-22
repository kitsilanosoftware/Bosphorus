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

	public struct Color {

		public Color (float r, float g, float b, float a)
		{
			this.r = r;
			this.g = g;
			this.b = b;
			this.a = a;
		}

		public Color (float r, float g, float b)
		{
			this.r = r;
			this.g = g;
			this.b = b;
			this.a = 1.0f;
		}

		// public static Color Lerp (Color a, Color b, float t);
		// public override bool Equals (object other);
		// public override int GetHashCode ();

		public override string ToString ()
		{
			return String.Format ("RGBA({0}, {1}, {2}, {3})", r, g, b, a);
		}

		// public string ToString (string format);

		// public static Color operator + (Color a, Color b);
		// public static Color operator - (Color a, Color b);
		// public static Color operator * (Color a, Color b);
		// public static Color operator * (Color a, float b);
		// public static Color operator * (float b, Color a);
		// public static Color operator / (Color a, float b);
		// public static bool operator == (Color lhs, Color rhs);
		// public static bool operator != (Color lhs, Color rhs);
		// public static implicit operator Vector4 (Color c);
		// public static implicit operator Color (Vector4 v);

		// public static Color black { get; }
		// public static Color blue { get; }
		// public static Color clear { get; }
		// public static Color cyan { get; }
		// public static Color gray { get; }
		// public static Color green { get; }
		// public static Color grey { get; }
		// public static Color magenta { get; }
		// public static Color red { get; }
		// public static Color white { get; }
		// public static Color yellow { get; }
		// public Color gamma { get; }
		// public float grayscale { get; }
		// public float this [int index] { get; set; }
		// public Color linear { get; }

		public float r;
		public float g;
		public float b;
		public float a;
	}

	public class RenderSettings
	{
		[YamlAlias("m_AmbientLight")]
		public /* static */ Color ambientLight { get; set; }

		[YamlAlias("m_FlareFadeSpeed")]
		public /* static */ float flareFadeSpeed { get; set; }

		[YamlAlias("m_FlareStrength")]
		public /* static */ float flareStrength { get; set; }

		[YamlAlias("m_Fog")]
		public /* static */ bool fog { get; set; }

		[YamlAlias("m_FogColor")]
		public /* static */ Color fogColor { get; set; }

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
