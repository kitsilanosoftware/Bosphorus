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
using System.Collections.Generic;
using YamlDotNet.Serialization.Utilities;
using YamlDotNet.Serialization.ObjectFactories;
using System.Runtime.Serialization;

namespace Disunity.Loader
{
	public sealed class BoolConverter : IYamlTypeConverter
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

	public sealed class UnityNodeDeserializer : INodeDeserializer
	{
		private readonly IObjectFactory _objectFactory;
		private readonly ITypeInspector _typeDescriptor;
		private readonly bool _ignoreUnmatched;

		public UnityNodeDeserializer(IObjectFactory objectFactory, ITypeInspector typeDescriptor, bool ignoreUnmatched)
		{
			_objectFactory = objectFactory;
			_typeDescriptor = typeDescriptor;
			_ignoreUnmatched = ignoreUnmatched;
		}

		private void CheckClassName (string className, Type expectedType)
		{
			if (expectedType.Name != className)
			{
				throw new SerializationException(
					string.Format(
						"Key name {0} does not match type {1}", className, expectedType
					)
				);
			}
		}

		bool INodeDeserializer.Deserialize(EventReader reader, Type expectedType,
						   Func<EventReader, Type, object> nestedObjectDeserializer, out object value)
		{
			if (!expectedType.IsSubclassOf(typeof(Disunity.UnityEngine.Object)))
			{
				value = null;
				return false;
			}

			// Position: before the RenderSettings mapping in:
			//
			//     --- !u!104 &2
			//     RenderSettings:
			//       m_Fog: 1
			//       ...
			var mapping = reader.Allow<MappingStart>();
			if (mapping == null)
			{
				value = null;
				return false;
			}

			CheckClassName(reader.Expect<Scalar>().Value, expectedType);
			value = _objectFactory.Create(expectedType);

			// Position: after RenderSettings; now taking care of the real property
			// mapping.
			reader.Expect<MappingStart>();
			while (!reader.Accept<MappingEnd>())
			{
				var propertyName = reader.Expect<Scalar>();
				var property = _typeDescriptor.GetProperty(expectedType, null, propertyName.Value,
									   _ignoreUnmatched);
				if (property == null)
				{
					reader.SkipThisAndNestedEvents();
					continue;
				}

				var propertyValue = nestedObjectDeserializer(reader, property.Type);
				var propertyValuePromise = propertyValue as IValuePromise;
				if (propertyValuePromise == null)
				{
					var convertedValue = TypeConverter.ChangeType(propertyValue, property.Type);
					property.Write(value, convertedValue);
				}
				else
				{
					var valueRef = value;
					propertyValuePromise.ValueAvailable += v =>
					{
						var convertedValue = TypeConverter.ChangeType(v, property.Type);
						property.Write(valueRef, convertedValue);
					};
				}
			}
			// Double mapping.
			reader.Expect<MappingEnd>();
			reader.Expect<MappingEnd>();

			return true;
		}
	}

	public class Loader
	{
		public const string UnityPrefix = "tag:unity3d.com,2011:";

		private Dictionary<string,Type> tagMapping = new Dictionary<string,Type>();

		public Loader ()
		{
			tagMapping.Add(UnityPrefix + "104", typeof(RenderSettings));
			tagMapping.Add(UnityPrefix + "157", typeof(LightmapSettings));
			tagMapping.Add(UnityPrefix + "1", typeof(GameObject));
			tagMapping.Add(UnityPrefix + "4", typeof(Transform));
			tagMapping.Add(UnityPrefix + "23", typeof(Renderer));
			tagMapping.Add(UnityPrefix + "33", typeof(MeshFilter));
			tagMapping.Add(UnityPrefix + "199", typeof(ParticleSystemRenderer));
			tagMapping.Add(UnityPrefix + "198", typeof(ParticleSystem));
			tagMapping.Add(UnityPrefix + "108", typeof(Light));
			tagMapping.Add(UnityPrefix + "111", typeof(Animation));
			tagMapping.Add(UnityPrefix + "114", typeof(MonoBehaviour));
			tagMapping.Add(UnityPrefix + "220", typeof(LightProbeGroup));
			tagMapping.Add(UnityPrefix + "20", typeof(Camera));
			tagMapping.Add(UnityPrefix + "64", typeof(MeshCollider));
			tagMapping.Add(UnityPrefix + "81", typeof(AudioListener));
		}

		private Deserializer CreateDeserializer ()
		{
			var ignoreUnmatched = true;
			var objectFactory = new DefaultObjectFactory();
			var namingConvention = new NullNamingConvention();
			var deserializer = new Deserializer(objectFactory, namingConvention, ignoreUnmatched);

			var typeResolver = new StaticTypeResolver();
			var typeDescriptor =
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

			deserializer.TypeDescriptor = typeDescriptor;

			var nodeDeserializers = deserializer.NodeDeserializers;
			// Add our node deserializer just before the
			// generic mapping -> object one.
			nodeDeserializers.Insert(nodeDeserializers.Count - 1,
				new UnityNodeDeserializer(objectFactory, typeDescriptor, ignoreUnmatched));

			deserializer.RegisterTypeConverter (new BoolConverter ());

			foreach (var pair in tagMapping)
			{
				deserializer.RegisterTagMapping(pair.Key, pair.Value);
			}

			return deserializer;
		}

		public void Load (string filename)
		{
			var knownUnknowns = new Dictionary<string,int>();

			using (var stream = File.OpenText (filename))
			{
				var reader = new EventReader (new Parser (stream));
				var deserializer = CreateDeserializer();

				reader.Expect<StreamStart> ();
				while (!reader.Accept<StreamEnd>())
				{
					reader.Expect<DocumentStart>();

					var nodeEvent = reader.Peek<NodeEvent>();
					var tag = nodeEvent.Tag;

					if (tagMapping.ContainsKey(tag))
					{
						var thing = deserializer.Deserialize<Disunity.UnityEngine.Object> (reader);

						Console.WriteLine("Deserialized: {0}", thing);
					}
					else
					{
						int count;
						knownUnknowns.TryGetValue(tag, out count);
						knownUnknowns[tag] = count + 1;

						reader.SkipThisAndNestedEvents();
					}

					reader.Allow<DocumentEnd>();
				}
			}

			foreach(var pair in knownUnknowns)
			{
				Console.WriteLine("Skipped unknown tag {0} {1} time(s)", pair.Key, pair.Value);
			}
		}

		public static void Main(string[] args)
		{
			var loader = new Loader();

			loader.Load(args[0]);
		}
	}
}
