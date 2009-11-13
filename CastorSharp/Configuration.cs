/*
 * $Id: Configuration.cs 2427 2007-07-04 13:42:38Z phbaer $
 *
 *
 * Copyright 2005,2006 Carpe Noctem, Distributed Systems Group,
 * University of Kassel. All right reserved.
 *
 * The code is derived from the software contributed to Carpe Noctem by
 * the Carpe Noctem Team.
 *
 * The code is licensed under the Carpe Noctem Userfriendly BSD-Based
 * License (CNUBBL). Redistribution and use in source and binary forms,
 * with or without modification, are permitted provided that the
 * conditions of the CNUBBL are met.
 *
 * You should have received a copy of the CNUBBL along with this
 * software. The license is also available on our website:
 * http://carpenoctem.das-lab.net/license.txt
 *
 *
 * <description>
 */

using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Threading;

namespace Castor {

	/// <summary>Adds a pointer back to the parent NodeList</summary>
	public class NodeList : ArrayList {
		protected NodeList parent = null;

		/// <summary>Gets or sets the reference to the parent node</summary>
		public NodeList Parent {
		    get { return this.parent; }
		    set { this.parent = value; }
		}

		/// <summary>Returns a node of the given type with the given name</summary>
		/// <param name="name">The name of the node</param>
		/// <returns>The content of the node cast to the given type. If no such node was
		/// found, the default value of the given type is returned.</returns>
		public T[] GetAll<T>(string name) {
			List<T> result = new List<T>();

			foreach (object content in this) {
				if (content is T) {
					T t = (T)content;
					
					if (t.Equals(name)) {
						result.Add(t);
					}
				}
			}

			return result.ToArray();
		}

		/// <summary>Returns a node of the given type with the given name</summary>
		/// <param name="name">The name of the node</param>
		/// <returns>The content of the node cast to the given type. If no such node was
		/// found, the default value of the given type is returned.</returns>
		public T Get<T>(string name) {
			foreach (object content in this) {
				if (content is T) {
					T t = (T)content;
					
					if (t.Equals(name)) {
						return t;
					}
				}
			}

			return default(T);
		}

		/// <summary>Returns a node with the given name</summary>
		/// <param name="name">The name of the node</param>
		/// <returns>The content of the node. If no such node was found, the default
		/// value of the given type is returned.</returns>
		public object Get(string name) {
			return Get<object>(name);
		}

		/// <summary>Checks if the tree contains the given object</summary>
		/// <param name="o">The object that has to be checked</param>
		/// <returns>True, if the object is present, false otherwise</returns>
		public override bool Contains(object o) {
			foreach (object content in this) {
				if (content.Equals(o)) {
					return true;
				}
			}

			return false;
		}
	}

	/// <summary>This class defines a mapping between an in-memory representation of
	/// a configuration tree and a permanent, textual representation on disk (in a file)</summary>
	public class Configuration {
		protected string filename;

		// The current level within the config tree
		protected NodeList currentLevel = null;

		// This hastable contains all configuration trees for all configuration
		// files opened by this Configuration class
		protected static NodeList config = null;
		protected NodeList localConfig = null;

		// Formatinfo
		protected NumberFormatInfo format = NumberFormatInfo.InvariantInfo;

		/// <summary>This represents the base class of values added to the configuration tree</summary>
		public class GenericValue<T> {
			public string Name;
			public T Value;
			public int LineInConfig;

			/// <summary>Default constructor. Initializes an instance only with a name</summary>
			/// <param name="name">Name of the node value</param>
			public GenericValue(string name) {
				if (name.IndexOf('.') > -1) {
					throw new Exception("Configuration: A node may not contain a '.' character!");
				}
				this.Name = name;
			}

			/// <summary>Initializes an instance with a name, a value, and the line numer of the value in
			/// the configuration file</summary>
			/// <param name="name">Name of the node value</param>
			/// <param name="value">The actual value of type <t>T</t></param>
			/// <param name="lineInConfig">The line number in the config file where the value is located</param>
			public GenericValue(string name, T value, int lineInConfig) {
				this.Name = name;
				this.Value = value;
				this.LineInConfig = lineInConfig;
			}

			/// <summary>Returns the hashcode of the current node value</summary>
			/// <returns>The hashcode of the current node value</returns>
			public override int GetHashCode() {
				return this.Name.GetHashCode();
			}

			/// <summary>Checks if the current node value is equal to the given object</summary>
			/// <param name="o">The object the current node value has to be compared to</param>
			/// <returns>True, if the two values (node value and the given object) are equal, false otherwise</returns>
			public override bool Equals(object o) {
				if (o is string) {
					return this.Name == (string)o;
				}

				if (this.GetType() == o.GetType()) {
					GenericValue<T> nv = (GenericValue<T>)o;
					return this.Name.Equals(nv.Name);
				}

				return false;							
			}
		}

		/// <summary>Specialized subclass of <t>GenericValue</t> which represents a tree node</summary>
		public class ConfigValue : GenericValue<NodeList> {
			/// <summary>Default consttructor. Initializes the instance only with a name</summary>
			/// <param name="name">The name of the node value</param>
			public ConfigValue(string name) : base(name) {}

			/// <summary>Initializes the instance with a name, a value, and a link back to the parent node</summary>
			/// <param name="name">The name of the node value</param>
			/// <param name="value">The node value</param>
			/// <param name="parent">A reference back to the parent node</param>
			public ConfigValue(string name, NodeList value, NodeList parent) :
				base(name, value, -1)
			{
				value.Parent = parent;
			}
		}


		/// <summary>Specialized subclass of <t>GenericValue</t> which represents a tree node
		/// with a line number</summary>
		public class NodeValue : GenericValue<NodeList> {
			/// <summary>Default consttructor. Initializes the instance only with a name</summary>
			/// <param name="name">The name of the node value</param>
			public NodeValue(string name) : base(name) {}

			/// <summary>Initializes the instance with a name, a value, a line number, and a link
			/// back to the parent node</summary>
			/// <param name="name">The name of the node value</param>
			/// <param name="value">The node value</param>
			/// <param name="lineInConfig">The line number of the current node value in the config file</param>
			/// <param name="parent">A reference back to the parent node</param>
			public NodeValue(string name, NodeList value, int lineInConfig, NodeList parent) :
				base(name, value, lineInConfig)
			{
				value.Parent = parent;
			}
		}

		/// <summary>Specialized subclass of <t>GenericValue</t> which represents a string value node
		/// with a line number</summary>
		public class StringValue : GenericValue<string> {
			/// <summary>Default consttructor. Initializes the instance only with a name</summary>
			/// <param name="name">The name of the node value</param>
			public StringValue(string name)	: base(name) {}

			/// <summary>Initializes the instance with a name, a value, and a line number</summary>
			/// <param name="name">The name of the node value</param>
			/// <param name="value">The node value</param>
			/// <param name="lineInConfig">The line number of the current node value in the config file</param>
			public StringValue(string name, string value, int lineInConfig) :
				base(name, value, lineInConfig)
			{
			}
		}

		/// <summary>Specialized subclass of <t>GenericValue</t> which represents a space</summary>
		public class SpaceValue : GenericValue<string> {
			/// <summary>Default consttructor. Initializes the instance only with a name</summary>
			/// <param name="name">The name of the node value</param>
			public SpaceValue(string name) : base(name) {}

			/// <summary>Initializes the instance with a line number</summary>
			/// <param name="lineInConfig">The line number of the current node value in the config file</param>
			public SpaceValue(int lineInConfig) :
				base("[space" + lineInConfig + "]", "", lineInConfig)
			{
			}
		}

		/// <summary>Specialized subclass of <t>GenericValue</t> which represents a comment</summary>
		public class CommentValue : GenericValue<string> {
			/// <summary>Default consttructor. Initializes the instance only with a name</summary>
			/// <param name="name">The name of the node value</param>
			public CommentValue(string name) : base(name) {}

			/// <summary>Initializes the instance with a line number</summary>
			/// <param name="value">The comment</param>
			/// <param name="lineInConfig">The line number of the current node value in the config file</param>
			public CommentValue(string value, int lineInConfig) :
				base("[comment" + lineInConfig + "]", value, lineInConfig)
			{
			}
		}

		/// <summary>Constructor which implicitly loads the file with the given filename</summary>
		/// <param name="filename">Name of a configuration file</param>
		public Configuration(string filename) {
			Load(filename, new StreamReader(filename), false); // do not catch exception
		}

		/// <summary>Constructor which implicitly loads the file with the given filename. If <t>create</t>
		/// is set to <t>true</t>, the file will be create if it does not exist</summary>
		/// <param name="filename">Name of a configuration file</param>
		/// <param name="create">Set to true if the file should be created if nonexistant, false otherwise</param>
		public Configuration(string filename, bool create) {
			if (create) {
				FileInfo fi = new FileInfo(filename);

				if (!fi.Exists) {
					fi.Create();
				}
			}

			Load(filename, new StreamReader(filename), false); // do not catch exception
		}


		/// <summary>Default constructor. Does nothing</summary>
		public Configuration() {}

		/// <summary>Returns the filename of the underlying configuration storage</summary>
		public string Filename {
			get { return this.filename; }
		}


		/// <summary>Returns a configuration base path. The value of the
		/// environment variable 'envVar' is read, all trailing slashes
		/// are stripped, and an optional subdirectory is finally appended. </summary>
		/// <param name="envVar">Name of an environment variable</param>
		/// <param name="subdir">An optional subdirectory that is appended to the value
		/// in the environment variable</param>
		/// <returns>A directory constructed from the value of an environment variable
		/// plus an optional subdirectory with a treiling slash.</returns>
		public static string GetBasePath(string envVar, string subdir) {
			string path = Environment.GetEnvironmentVariable(envVar);

			subdir = subdir.Trim(new char[] { '/' });

			if (path != null) {
				path = path.TrimEnd(new char[] { '/' }) + "/" + subdir + (subdir.Length > 0 ? "/" : "");
			} else {
				path = subdir + "/";
			}

			return path;
		}

		/// <summary>Loads a new configuration which replaces an already existing one</summary>
		/// <param name="filename">The configuration identifier</param>
		/// <param name="buffer">The configuration tree</param>
		public void LoadString(string filename, string buffer) {
			Load(filename, new StringReader(buffer), true);
		}

		/// <summary>Loads a new configuration and optionally replaces an already existing one</summary>
		/// <param name="filename">The configuration identifier</param>
		/// <param name="reader">The input source</param>
		/// <param name="replaceConfig">An already existing configuration is replaced if this parameter is set to true</param>
		public void Load(string filename, TextReader reader, bool replaceConfig) {
			this.filename = filename;

			// Create new config during first initialization
			if (config == null) {
				config = new NodeList();
			}

			// Replace the whole config
			ConfigValue cv = new ConfigValue(filename, new NodeList(), config);
			if (replaceConfig) {
				if (config.Contains(cv)) {
					config.Remove(cv);
				}
			}

			// Check if the configuration was loaded already.
			// If not, load it
			if (!config.Contains(cv)) {
				// Create a new tree
				this.currentLevel = cv.Value;
				this.localConfig = cv.Value;

				// Create config node
				config.Add(cv);

				string line;
				int lineNr = 0;
				int depth = 0;
				int skip = -1;
				StringCollection history = new StringCollection();

				while ((line = reader.ReadLine()) != null) {
					line = line.Trim();

					lineNr++;

					if (line.Length > 0) {
						switch (line[0]) {

						case '#':
							{
								CommentValue cv1 = new CommentValue(line, lineNr);
								this.currentLevel.Add(cv1);
							}
							break;

						case '[':
							// Check if this is a valid sections

							if (line[line.Length - 1] == ']') {
								string section = "";

								// This is the end of a section

								if (line[1] == '!') {
									// Skipping done.

									if (skip == depth) {
										skip = -1;
									}

									depth--;

									if (skip == -1) {
										section = line.Substring(2, line.Length - 3);

										if (history[history.Count - 1] != section) {
											throw new Exception(
													String.Format("Configuration: Sections nested incorrectly in {0} line {1} ({2}). Exiting.",
														this.filename,
														lineNr,
														line));
										}

										history.RemoveAt(history.Count - 1);

										// Go back
										this.currentLevel = this.currentLevel.Parent;
									}

									// Otherwise a new section starts
								} else {
									depth++;

									if (skip == -1) {
										// Remember the current level
										NodeList temp = this.currentLevel;

										section = line.Substring(1, line.Length - 2);
										history.Add(section);

										// If no such name exists, create a new one

										NodeValue nv = new NodeValue(section, new NodeList(), lineNr, temp);
										if (!this.currentLevel.Contains(nv)) {
											// Add the NodeList to the parent NodeList
											this.currentLevel = nv.Value;

											// Add new node
											temp.Add(nv);

											// Otherwise change to the already existing one
										} else {
											Console.Error.WriteLine(
													String.Format("Configuration: Section {0} already defined in {1}!",
														section, this.filename));
											skip = depth;
										}

									}

								}

							}
							break;

						default:
						   	{
								int index = line.IndexOf('=');

								if (skip == -1) {
									if (index > -1) {
										// A new property (name-value pair) was found
										string name = line.Substring(0, index).Trim();
										string val = line.Substring(index + 1, line.Length - (index + 1)).Trim();

										StringValue sv = new StringValue(name, val, lineNr);
//										if (!this.currentLevel.Contains(sv)) {
											this.currentLevel.Add(sv);

//										} else {
//											throw new Exception(
//													String.Format("Configuration: Name {0} in {1}, line {2} ({3}) already defined!",
//														name, this.filename, lineNr, line));
//										}

									} else {
										throw new Exception(
												String.Format("Configuration: Parse error in {0}, line {1} ({2})",
													this.filename, lineNr, line));
									}
								}
							}
							break;
						}
					} else {
						SpaceValue sv = new SpaceValue(lineNr);
						this.currentLevel.Add(sv);
					}
				}

			} else {
				cv = config.Get<ConfigValue>(this.filename);

				if (cv == null) {
					throw new Exception(
							String.Format("Configuration: Error while loading previously parsed configuration for {0}!",
								this.filename));
				}

				this.localConfig = cv.Value;
			}

			reader.Close();
		}

		
		public void Store() {
			StreamWriter writer = new StreamWriter(File.OpenWrite(this.filename)); // do not catch exception
			writer.Write(Serialize(this.localConfig, 0));
			writer.Close();
		}

		public string Serialize() {
			return Serialize(this.localConfig, 0);
		}

		
		protected string Serialize(NodeList ht, int depth) {
			string pad = "";
			string result = "";

			for (int i = 0; i < depth; i++) {
				pad += "\t";
			}
			
			foreach (object o in ht) {
				if (o is StringValue) {
					StringValue sv = (StringValue)o;
					result += (pad + sv.Name + " = " + sv.Value + "\n");

				} else if (o is NodeValue) {
					NodeValue nv = (NodeValue)o;
					result += (pad + "[" + nv.Name + "]" + "\n");
					result += Serialize(nv.Value, depth + 1);
					result += (pad + "[!" + nv.Name + "]" + "\n");

				} else if (o is CommentValue) {
					CommentValue cv = (CommentValue)o;
					result += (cv.Value + "\n");

				} else if (o is SpaceValue) {
					SpaceValue sv = (SpaceValue)o;
					result += (sv.Value + "\n");
					
				} else {
					Debug.WriteLine("Configuration: This should not have been reached! (Serialize)");
				}
			}

			return result;
		}


		/// <summary>Print out the config tree of the node that was passed up to a given level</summary>
		/// <param name="node">The tree is identified by this node</param>
		/// <param name="depth">The maximum depth</param>
		public void Print(NodeList node, int depth) {
			if (node != null) {
				string pad = "";

				for (int i = 0; i < depth; i++) {
					pad += "  ";
				}

				foreach (object o in node) {
					if (o is StringValue) {
						StringValue sv = (StringValue)o;
						Console.WriteLine(pad + sv.Name + " = " + sv.Value);
						
					} else if (o is NodeValue) {
						NodeValue nv = (NodeValue)o;
						Console.WriteLine(pad + nv.Name);
						Print(nv.Value, depth + 1);
					}

				}

			}
		}

		/// <summary>
		/// Returns all nodse referenced by a sections path and a name. Actually, the GenericValue
		/// structure is returned. Both fields are optional. If no such path was found, null is returned.
		/// </summary>
		/// <param name="name">
		/// The path name array. May contain stringified paths (i.e. comp1.comp2.comp3) and
		/// single components (i.e. comp1)
		/// </param>
		/// <returns>
		/// Returns an array of GenericValues which reference T. If no such node was found, null is returned.
		/// </returns>
		public T[] GetAllNodes<T>(string[] name) {
			if ((name != null) && (name.Length > 0)) {
				List<string> newName = new List<string>();

				// Get full name array
				foreach (string n in name) {
					if (n != null) {
						string[] components = n.Split(new char[] { '.' });

						foreach (string c in components) {
							newName.Add(c);
						}
					}
				}

				if (this.localConfig != null) {
					NodeList level = this.localConfig;

					lock(this.localConfig) {
						for (int i = 0; i < newName.Count; i++) {
							// Path element
							if (i < newName.Count - 1) {
								NodeValue nv = level.Get<NodeValue>(newName[i]);
								
								if (nv == null) {
									throw new Exception(String.Format("Path element {0} ({1}) not found in {2}", newName[i], PathToString(newName.ToArray()), this.filename));
								}
								
								level = nv.Value;

								if (level == null) {
									throw new Exception(String.Format("Path element {0} ({1}) not found in {2}", newName[i + 1], PathToString(newName.ToArray()), this.filename));
								}

							// Value element
							} else {
								T[] t = level.GetAll<T>(newName[i]);

								if (t.Length == 0) {
									return null;
								}

								return t;
							}
						}
					}
				}
			} else {
				object nv = new NodeValue("Root", this.localConfig, 0, null);

				if (nv is T) {
					return new T[] { (T)nv };
				}
			}

			return null;
		}


		/// <summary>
		/// Returns the node referenced by a sections path and a name. Actually, the GenericValue
		/// structure is returned. Both fields are optional. If no such path was found, null is returned.
		/// </summary>
		/// <param name="name">
		/// The path name array. May contain stringified paths (i.e. comp1.comp2.comp3) and
		/// single components (i.e. comp1)
		/// </param>
		/// <returns>
		/// Returns the a GenericValue which references T. If no such node was found, null is returned.
		/// </returns>
		public T GetNode<T>(string[] name) {
			T[] t = GetAllNodes<T>(name);

			if (t != null) {
				return t[t.Length - 1];
			}

			return default(T);
		}


		/// <summary>
		/// Yields the linenumber in the configfile, where the pattern ist found.
		/// <para>This may be used for error reporting, if the value is wrong
		/// this.formatted, in unexpected ranges or something else.</para>
		/// </summary>
		/// <param name="name">Path into the config tree</param>
		/// <returns>The line in the config file for the given path</returns>
		public int GetLineInConfig(params string[] name) {
			object o = GetNode<object>(name);

			if (o is StringValue) {
				return ((StringValue)o).LineInConfig;
			} else if (o is NodeValue) {
				return ((NodeValue)o).LineInConfig;
			}

			return -1;
		}

		/// <summary>Returns the values (strings) of the specified key name and the section path.</summary>
		/// <param name="name">Path into the config tree</param>
		/// <returns>The values (strings) to the key name if it was found, an empty string otherwise</returns>
		public string[] GetAllStrings(params string[] name) {
			StringValue[] temp = GetAllNodes<StringValue>(name);

			if (temp == null) {
				throw new Castor.CException("Key {0} not found in {1}!", PathToString(name), this.filename);
			}

			string[] result = new string[temp.Length];
			for (int i = 0; i < temp.Length; i++) {
				result[i] = temp[i].Value;
			}

			return result;
		}

		public string[] TryGetAllStrings(string[] dflt, params string[] name) {
			try {
				return GetAllStrings(name);
			} catch {
				// Silently drop exception
			}

			return dflt;
		}

	
		/// <summary>Returns the value (string) of the specified key name and the section path.</summary>
		/// <param name="name">Path into the config tree</param>
		/// <returns>The value (string) to the key name if it was found, an empty string otherwise</returns>
		public string GetString(params string[] name) {
			StringValue sv = GetNode<StringValue>(name);

			if (sv == null) {
				throw new Exception(String.Format(
						"Key {0} not found in {1}!", PathToString(name), this.filename));
			}

			return sv.Value;
		}

		/// Same as GetSections but does not trow an exception. It returns an empty string instead
		public string TryGetString(string dflt, params string[] name) {
			try {
				return GetString(name);
			} catch {
				// Silently drop exception
			}

			return dflt;
		}

		/// <summary>Updates a value in the configuration tree.</summary>
		/// <param name="val">The new value of 'name'</param>
		/// <param name="name">Path into the config tree</param>
		public void SetString(string val, params string[] name) {
			StringValue sv = GetNode<StringValue>(name);

			if (sv == null) {
				throw new Exception(String.Format(
						"Unable to set {0}={1} in {2}!", PathToString(name), val, this.filename));
			}
			
			sv.Value = val;
		}

		/// <summary>Returns the value (bool) of the specified key name and the section path.</summary>
		/// <param name="name">Path into the config tree</param>
		/// <returns>The value (bool) to the key name if it was found, 0 otherwise</returns>
		public bool GetBool(params string[] name) {
			string s = RemoveComment(GetString(name)).ToLower();
			return ((s.StartsWith("true")) || (s.StartsWith("1")) || (s.StartsWith("yes")) || (s.StartsWith("on")));
		}

		/// <summary>
		/// Returns the value (bool) of the specified key name and the section path.
		/// Does not throw an exception.
		/// </summary>
		/// <param name="dflt">Default value</param>
		/// <param name="name">Path into the config tree</param>
		/// <returns>The value (bool) to the key name if it was found, 0 otherwise</returns>
		public bool TryGetBool(bool dflt, params string[] name) {
			try {
				return GetBool(name);
			} catch {
				// Silently drop exception
			}
			return dflt;
		}

		/// <summary>Updates a value in the configuration tree.</summary>
		/// <param name="val">The new value of 'name'</param>
		/// <param name="name">Path into the config tree</param>
		public void SetBool(bool val, params string[] name) {
			try {
				SetString(Convert.ToString(val, this.format), name);
			} catch {
				// Silently drop exception
			}
		}

		/// <summary>Returns the value (byte) of the specified key name and the section path.</summary>
		/// <param name="name">Path into the config tree</param>
		/// <returns>The value (byte) to the key name if it was found, 0 otherwise</returns>
		public byte GetByte(params string[] name) {
			return Convert.ToByte(RemoveComment(GetString(name)), this.format);
		}

		/// <summary>
		/// Returns the value (byte) of the specified key name and the section path.
		/// Does not throw an exception.
		/// </summary>
		/// <param name="dflt">Default value</param>
		/// <param name="name">Path into the config tree</param>
		/// <returns>The value (byte) to the key name if it was found, 0 otherwise</returns>
		public byte TryGetByte(byte dflt, params string[] name) {
			try {
				return Convert.ToByte(RemoveComment(GetString(name)), this.format);
			} catch {
				// Silently drop exception
			}
			return dflt;
		}

		/// <summary>Updates a value in the configuration tree.</summary>
		/// <param name="val">The new value of 'name'</param>
		/// <param name="name">Path into the config tree</param>
		public void SetByte(byte val, params string[] name) {
			try {
				SetString(Convert.ToString(val, this.format), name);
			} catch {
				// Silently drop exception
			}
		}

		/// <summary>Returns the value (char) of the specified key name and the section path.</summary>
		/// <param name="name">Path into the config tree</param>
		/// <returns>The value (char) to the key name if it was found, 0 otherwise</returns>
		public long GetChar(params string[] name) {
			return Convert.ToChar(RemoveComment(GetString(name)), this.format);
		}

		/// <summary>
		/// Returns the value (char) of the specified key name and the section path.
		/// Does not throw an exception.
		/// </summary>
		/// <param name="dflt">Default value</param>
		/// <param name="name">Path into the config tree</param>
		/// <returns>The value (char) to the key name if it was found, 0 otherwise</returns>
		public char TryGetChar(char dflt, params string[] name) {
			try {
				return Convert.ToChar(RemoveComment(GetString(name)), this.format);
			} catch {
				// Silently drop exception
			}
			return dflt;
		}

		/// <summary>Updates a value in the configuration tree.</summary>
		/// <summary>Updates a value in the configuration tree.</summary>
		/// <param name="val">The new value of 'name'</param>
		/// <param name="name">Path into the config tree</param>
		public void SetChar(char val, params string[] name) {
			try {
				SetString(Convert.ToString(val, this.format), name);
			} catch {
				// Silently drop exception
			}
		}

		/// <summary>Returns the value (ushort) of the specified key name and the section path.</summary>
		/// <param name="name">Path into the config tree</param>
		/// <returns>The value (ushort) to the key name if it was found, 0 otherwise</returns>
		public long GetUShort(params string[] name) {
			return Convert.ToUInt16(RemoveComment(GetString(name)), this.format);
		}

		/// <summary>
		/// Returns the value (ushort) of the specified key name and the section path.
		/// Does not throw an exception.
		/// </summary>
		/// <param name="dflt">Default value</param>
		/// <param name="name">Path into the config tree</param>
		/// <returns>The value (ushort) to the key name if it was found, 0 otherwise</returns>
		public ushort TryGetUShort(ushort dflt, params string[] name) {
			try {
				return Convert.ToUInt16(RemoveComment(GetString(name)), this.format);
			} catch {
				// Silently drop exception
			}
			return dflt;
		}

		/// <summary>Updates a value in the configuration tree.</summary>
		/// <summary>Updates a value in the configuration tree.</summary>
		/// <param name="val">The new value of 'name'</param>
		/// <param name="name">Path into the config tree</param>
		public void SetUShort(ushort val, params string[] name) {
			try {
				SetString(Convert.ToString(val, this.format), name);
			} catch {
				// Silently drop exception
			}
		}

		/// <summary>Returns the value (short) of the specified key name and the section path.</summary>
		/// <param name="name">Path into the config tree</param>
		/// <returns>The value (short) to the key name if it was found, 0 otherwise</returns>
		public short GetShort(params string[] name) {
			return Convert.ToInt16(RemoveComment(GetString(name)), this.format);
		}

		/// <summary>
		/// Returns the value (short) of the specified key name and the section path.
		/// Does not throw an exception.
		/// </summary>
		/// <param name="dflt">Default value</param>
		/// <param name="name">Path into the config tree</param>
		/// <returns>The value (short) to the key name if it was found, 0 otherwise</returns>
		public short TryGetShort(short dflt, params string[] name) {
			try {
				return Convert.ToInt16(RemoveComment(GetString(name)), this.format);
			} catch {
				// Silently drop exception
			}
			return dflt;
		}

		/// <summary>Updates a value in the configuration tree.</summary>
		/// <summary>Updates a value in the configuration tree.</summary>
		/// <param name="val">The new value of 'name'</param>
		/// <param name="name">Path into the config tree</param>
		public void SetShort(short val, params string[] name) {
			try {
				SetString(Convert.ToString(val, this.format), name);
			} catch {
				// Silently drop exception
			}
		}

		/// <summary>Returns the value (uint) of the specified key name and the section path.</summary>
		/// <param name="name">Path into the config tree</param>
		/// <returns>The value (uint) to the key name if it was found, 0 otherwise</returns>
		public long GetUInt(params string[] name) {
			return Convert.ToUInt32(RemoveComment(GetString(name)), this.format);
		}

		/// <summary>
		/// Returns the value (uint) of the specified key name and the section path.
		/// Does not throw an exception.
		/// </summary>
		/// <param name="dflt">Default value</param>
		/// <param name="name">Path into the config tree</param>
		/// <returns>The value (uint) to the key name if it was found, 0 otherwise</returns>
		public uint TryGetUInt(uint dflt, params string[] name) {
			try {
				return Convert.ToUInt32(RemoveComment(GetString(name)), this.format);
			} catch {
				// Silently drop exception
			}
			return dflt;
		}

		/// <summary>Updates a value in the configuration tree.</summary>
		/// <summary>Updates a value in the configuration tree.</summary>
		/// <param name="val">The new value of 'name'</param>
		/// <param name="name">Path into the config tree</param>
		public void SetUInt(uint val, params string[] name) {
			try {
				SetString(Convert.ToString(val, this.format), name);
			} catch {
				// Silently drop exception
			}
		}

		/// <summary>Returns the value (int) of the specified key name and the section path.</summary>
		/// <param name="name">Path into the config tree</param>
		/// <returns>The value (int) to the key name if it was found, 0 otherwise</returns>
		public int GetInt(params string[] name) {
			return Convert.ToInt32(RemoveComment(GetString(name)), this.format);
		}

		/// <summary>
		/// Returns the value (int) of the specified key name and the section path.
		/// Does not throw an exception.
		/// </summary>
		/// <param name="dflt">Default value</param>
		/// <param name="name">Path into the config tree</param>
		/// <returns>The value (int) to the key name if it was found, 0 otherwise</returns>
		public int TryGetInt(int dflt, params string[] name) {
			try {
				return Convert.ToInt32(RemoveComment(GetString(name)), this.format);
			} catch {
				// Silently drop exception
			}
			return dflt;
		}

		/// <summary>Updates a value in the configuration tree.</summary>
		/// <summary>Updates a value in the configuration tree.</summary>
		/// <param name="val">The new value of 'name'</param>
		/// <param name="name">Path into the config tree</param>
		public void SetInt(int val, params string[] name) {
			try {
				SetString(Convert.ToString(val, this.format), name);
			} catch {
				// Silently drop exception
			}
		}

		/// <summary>Returns the value (ulong) of the specified key name and the section path.</summary>
		/// <param name="name">Path into the config tree</param>
		/// <returns>The value (ulong) to the key name if it was found, 0 otherwise</returns>
		public ulong GetULong(params string[] name) {
			return Convert.ToUInt64(RemoveComment(GetString(name)), this.format);
		}

		/// <summary>
		/// Returns the value (ulong) of the specified key name and the section path.
		/// Does not throw an exception.
		/// </summary>
		/// <param name="dflt">Default value</param>
		/// <param name="name">Path into the config tree</param>
		/// <returns>The value (ulong) to the key name if it was found, 0 otherwise</returns>
		public ulong TryGetULong(ulong dflt, params string[] name) {
			try {
				return Convert.ToUInt64(RemoveComment(GetString(name)), this.format);
			} catch {
				// Silently drop exception
			}
			return dflt;
		}

		/// <summary>Updates a value in the configuration tree.</summary>
		/// <summary>Updates a value in the configuration tree.</summary>
		/// <param name="val">The new value of 'name'</param>
		/// <param name="name">Path into the config tree</param>
		public void SetULong(ulong val, params string[] name) {
			try {
				SetString(Convert.ToString(val, this.format), name);
			} catch {
				// Silently drop exception
			}
		}

		/// <summary>Returns the value (long) of the specified key name and the section path.</summary>
		/// <param name="name">Path into the config tree</param>
		/// <returns>The value (long) to the key name if it was found, 0 otherwise</returns>
		public long GetLong(params string[] name) {
			return Convert.ToInt64(RemoveComment(GetString(name)), this.format);
		}

		/// <summary>
		/// Returns the value (long) of the specified key name and the section path.
		/// Does not throw an exception.
		/// </summary>
		/// <param name="dflt">Default value</param>
		/// <param name="name">Path into the config tree</param>
		/// <returns>The value (long) to the key name if it was found, 0 otherwise</returns>
		public long TryGetLong(long dflt, params string[] name) {
			try {
				return Convert.ToInt64(RemoveComment(GetString(name)), this.format);
			} catch {
				// Silently drop exception
			}
			return dflt;
		}

		/// <summary>Updates a value in the configuration tree.</summary>
		/// <summary>Updates a value in the configuration tree.</summary>
		/// <param name="val">The new value of 'name'</param>
		/// <param name="name">Path into the config tree</param>
		public void SetLong(long val, params string[] name) {
			try {
				SetString(Convert.ToString(val, this.format), name);
			} catch {
				// Silently drop exception
			}
		}

		/// <summary>Returns the value (float) of the specified key name and the section path.</summary>
		/// <param name="name">Path into the config tree</param>
		/// <returns>The value (float) to the key name if it was found, 0.0 otherwise</returns>
		public double GetFloat(params string[] name) {
			return Convert.ToSingle(GetString(name), this.format);
		}

		/// <summary>
		/// Returns the value (float) of the specified key name and the section path.
		/// Does not throw an exception.
		/// </summary>
		/// <param name="dflt">Default value</param>
		/// <param name="name">Path into the config tree</param>
		/// <returns>The value (float) to the key name if it was found, 0 otherwise</returns>
		public float TryGetFloat(float dflt, params string[] name) {
			try {
				return Convert.ToSingle(RemoveComment(GetString(name)), this.format);
			} catch {
				// Silently drop exception
			}
			return dflt;
		}

		/// <summary>Updates a value in the configuration tree.</summary>
		/// <summary>Updates a value in the configuration tree.</summary>
		/// <param name="val">The new value of 'name'</param>
		/// <param name="name">Path into the config tree</param>
		public void SetFloat(float val, params string[] name) {
			SetString(Convert.ToString(val, this.format), name);
		}

		/// <summary>Returns the value (double) of the specified key name and the section path.</summary>
		/// <param name="name">Path into the config tree</param>
		/// <returns>The value (double) to the key name if it was found, 0.0 otherwise</returns>
		public double GetDouble(params string[] name) {
			return Convert.ToDouble(GetString(name), this.format);
		}

		/// <summary>
		/// Returns the value (double) of the specified key name and the section path.
		/// Does not throw an exception.
		/// </summary>
		/// <param name="dflt">Default value</param>
		/// <param name="name">Path into the config tree</param>
		/// <returns>The value (double) to the key name if it was found, 0 otherwise</returns>
		public double TryGetDouble(double dflt, params string[] name) {
			try {
				return Convert.ToDouble(RemoveComment(GetString(name)), this.format);
			} catch {
				// Silently drop exception
			}
			return dflt;
		}

		/// <summary>Updates a value in the configuration tree.</summary>
		/// <summary>Updates a value in the configuration tree.</summary>
		/// <param name="val">The new value of 'name'</param>
		/// <param name="name">Path into the config tree</param>
		public void SetDouble(double val, params string[] name) {
			SetString(Convert.ToString(val, this.format), name);
		}


		/// <summary>Returns all names defined below the specified path</summary>
		/// <param name="name">Path into the config tree</param>
		/// <returns>All names defined below the specified path</returns>
		public string[] GetNames(params string[] name) {
			NodeValue nv = GetNode<NodeValue>(name);

			if (nv == null) {
				throw new Exception(String.Format(
						"Key {0} not found in {1}!", PathToString(name), this.filename));
			}

			List<string> result = new List<string>();
			
			if (nv.Value != null) {
				foreach (object o in nv.Value) {
					if (o is StringValue) {
						result.Add(((StringValue)o).Name);
					}
				}
			}

			return result.ToArray();
		}

		/// <summary>Returns all sections defined below the specified path</summary>
		/// <param name="name">Path into the config tree</param>
		/// <returns>All sections defined below the specified path</returns>
		public string[] GetSections(params string[] name) {
			NodeValue nv = null;

			try {
				nv = GetNode<NodeValue>(name);
			} catch (Exception e) {
				Debug.WriteLine("Configuration: Error while trying to retrieve node: {0}", e.Message);
			}

			if (nv == null) {
				throw new Exception(String.Format(
						"Key {0} not found in {1}!", PathToString(name), this.filename));
			}

			List<string> result = new List<string>();

			if (nv.Value != null) {
				foreach (object o in nv.Value) {
					if (o is NodeValue) {
						result.Add(((NodeValue)o).Name);
					}
				}
			}

			return result.ToArray();
		}

		/// <summary>
		/// Returns a new instance of Configuration with localConfig
		/// set to the specified sections path
		/// </summary>
		/// <param name="name">The path under which the key name should be looked up</param>
		/// <returns>A new instance of Configuration</returns>
		public Configuration GetInstance(params string[] name) {
			Configuration config = null;
			NodeValue nv = GetNode<NodeValue>(name);
				
			if (nv != null) {
				NodeList level = nv.Value;
						
				config = new Configuration();
				config.filename = this.filename + "-" + PathToString(GetSections(name));
				config.currentLevel = level;
				config.localConfig = level;
			}

			return config;
		}

		protected string RemoveComment(string input) {
			int pos = input.IndexOf("#");
			if (pos > -1) {
				return input.Substring(0, pos).Trim();
			}
			return input.Trim();
		}

		protected string PathToString(string[] name) {
			string result = "";

			if (name != null) {
				List<string> newName = new List<string>();
				// Get full name array
				foreach (string n in name) {
					if (n != null) {
						string[] components = n.Split(new char[] { '.' });

						foreach (string c in components) {
							newName.Add(c);
						}
					}
				}

				if (newName.Count > 0) {
					StringBuilder sb = new StringBuilder();
					sb.Append(newName[0]);
					for (int i = 1; i < newName.Count; i++) {
						sb.Append('.');
						sb.Append(newName[i]);
					}
					result = sb.ToString();
				}
			}

			return result;
		}
	}
}

