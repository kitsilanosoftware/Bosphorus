using System;
using System.IO;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization.TypeResolvers;
using YamlDotNet.Serialization.TypeInspectors;
using YamlDotNet.Serialization.NamingConventions;
using Disunity.UnityEngine;

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

			var typeResolver = new StaticTypeResolver();

			deserializer.TypeDescriptor =
				new YamlAttributesTypeInspector(
					new NamingConventionTypeInspector(
						new ReadableAndWritablePropertiesTypeInspector(
							new SelectManyTypeInspector(
								new ReadablePropertiesTypeInspector(typeResolver),
								new FieldsTypeInspector(typeResolver)
							)
						),
						new NullNamingConvention()
					)
				);

			reader.Allow<StreamStart> ();
			if (reader.Accept<DocumentStart> ())
			{
				// Skip SceneSettings as we don't know
				// which class to map this to.
				reader.SkipThisAndNestedEvents ();
			}

			var something = deserializer.Deserialize<Top> (reader);

			Console.WriteLine ("Deserialized {0}.", something);
			Console.WriteLine ("\tambientLight: {0}", something.RenderSettings.ambientLight);
			Console.WriteLine ("\tflareFadeSpeed: {0}", something.RenderSettings.flareFadeSpeed);
			Console.WriteLine ("\tflareStrength: {0}", something.RenderSettings.flareStrength);
			Console.WriteLine ("\tfog: {0}", something.RenderSettings.fog);
			Console.WriteLine ("\tfogColor: {0}", something.RenderSettings.fogColor);
			Console.WriteLine ("\tfogDensity: {0}", something.RenderSettings.fogDensity);
			Console.WriteLine ("\tfogEndDistance: {0}", something.RenderSettings.fogEndDistance);
			Console.WriteLine ("\tfogStartDistance: {0}", something.RenderSettings.fogStartDistance);
			Console.WriteLine ("\thaloStrength: {0}", something.RenderSettings.haloStrength);
		}
	}
}
