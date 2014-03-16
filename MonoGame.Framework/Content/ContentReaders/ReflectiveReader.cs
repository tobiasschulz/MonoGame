#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2014 Ethan Lee and the MonoGame Team
 *
 * Released under the Microsoft Public License.
 * See LICENSE for details.
 */

/* Original source from SilverSprite project at http://silversprite.codeplex.com */
#endregion


using System;
using System.Reflection;

namespace Microsoft.Xna.Framework.Content
{
	internal class ReflectiveReader<T> : ContentTypeReader
	{
		ConstructorInfo constructor;
		PropertyInfo[] properties;
		FieldInfo[] fields;
		ContentTypeReaderManager manager;

		Type targetType;
		Type baseType;
		ContentTypeReader baseTypeReader;

		internal ReflectiveReader() : base(typeof(T))
		{
			targetType = typeof(T);
		}

		protected internal override void Initialize(ContentTypeReaderManager manager)
		{
			base.Initialize(manager);
			this.manager = manager;
			Type type = targetType.BaseType;
			if (type != null && type != typeof(object))
			{
				baseType = type;
				baseTypeReader = manager.GetTypeReader(baseType);
			}
			constructor = targetType.GetDefaultConstructor();
			properties = targetType.GetAllProperties();
			fields = targetType.GetAllFields();
		}

		static object CreateChildObject(PropertyInfo property, FieldInfo field)
		{
			object obj = null;
			Type t;
			if (property != null)
			{
				t = property.PropertyType;
			}
			else
			{
				t = field.FieldType;
			}
			if (t.IsClass && !t.IsAbstract)
			{
				ConstructorInfo constructor = t.GetDefaultConstructor();
				if (constructor != null)
				{
					obj = constructor.Invoke(null);
				}
			}
			return obj;
		}

		private void Read(
			object parent,
			ContentReader input,
			MemberInfo member
		) {
			PropertyInfo property = member as PropertyInfo;
			FieldInfo field = member as FieldInfo;
			// properties must have public get and set
			if (property != null &&
				(property.CanWrite == false ||
				 property.CanRead == false) )
			{
				return;
			}

			if (property != null && property.Name == "Item")
			{
				MethodInfo getMethod = property.GetGetMethod();
				MethodInfo setMethod = property.GetSetMethod();

				if ( (getMethod != null &&
				      getMethod.GetParameters().Length > 0) ||
				     (setMethod != null &&
				      setMethod.GetParameters().Length > 0) )
				{
					/*
					 * This is presumably a property like this[indexer] and this
					 * should not get involved in the object deserialization
					 * */
					return;
				}
			}
			Attribute attr = Attribute.GetCustomAttribute(member, typeof(ContentSerializerIgnoreAttribute));
			if (attr != null)
			{
				return;
			}
			Attribute attr2 = Attribute.GetCustomAttribute(member, typeof(ContentSerializerAttribute));
			bool isSharedResource = false;
			if (attr2 != null)
			{
				ContentSerializerAttribute cs = attr2 as ContentSerializerAttribute;
				isSharedResource = cs.SharedResource;
			}
			else
			{
				if (property != null)
				{
					foreach (MethodInfo info in property.GetAccessors(true))
					{
						if (info.IsPublic == false)
						{
							return;
						}
					}
				}
				else
				{
					if (!field.IsPublic)
					{
						return;
					}

					// evolutional: Added check to skip initialise only fields
					if (field.IsInitOnly)
					{
						return;
					}
				}
			}
			ContentTypeReader reader = null;
			Type elementType = null;
			if (property != null)
			{
				reader = manager.GetTypeReader(elementType = property.PropertyType);
			}
			else
			{
				reader = manager.GetTypeReader(elementType = field.FieldType);
			}
			if (!isSharedResource)
			{
				object existingChildObject = CreateChildObject(property, field);
				object obj2;
				if (reader == null && elementType == typeof(object))
				{
					/* Reading elements serialized as "object" */
					obj2 = input.ReadObject<object>();
				}
				else
				{
					/* Default */

					// evolutional: Fix. We can get here and still be NULL,
					// exit gracefully
					if (reader == null)
					{
						return;
					}
					obj2 = input.ReadObject(reader, existingChildObject);
				}
				if (property != null)
				{
					property.SetValue(parent, obj2, null);
				}
				else
				{
					// Private fields can be serialized if they have
					// ContentSerializerAttribute added to them
					if (field.IsPrivate == false || attr2 != null)
					{
						field.SetValue(parent, obj2);
					}
				}
			}
			else
			{
				Action<object> action = delegate(object value)
				{
					if (property != null)
					{
						property.SetValue(parent, value, null);
					}
					else
					{
						field.SetValue(parent, value);
					}
				};
				input.ReadSharedResource(action);
			}
		}

		protected internal override object Read(
			ContentReader input,
			object existingInstance
		) {
			T obj;
			if (existingInstance != null)
			{
				obj = (T) existingInstance;
			}
			else
			{
				if (constructor == null)
				{
					obj = (T) Activator.CreateInstance(typeof(T));
				}
				else
				{
					obj = (T) constructor.Invoke(null);
				}
			}
			if (baseTypeReader != null)
			{
				baseTypeReader.Read(input, obj);
			}
			// Box the type.
			object boxed = (object) obj;
			foreach (PropertyInfo property in properties)
			{
				Read(boxed, input, property);
			}
			foreach (FieldInfo field in fields)
			{
				Read(boxed, input, field);
			}
			// Unbox it... required for value types.
			obj = (T) boxed;
			return obj;
		}
	}
}
