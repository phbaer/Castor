/*
 * $Id: ModuleLoader.cs 2390 2007-06-20 18:21:32Z phbaer $
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
using System.Reflection;
using System.Security;

namespace Castor.Dynamic {
	using Castor;

	public class ModuleLoader<CLASS> {
		protected static MsgSwitch trace = new MsgSwitch(MsgLevel.Off);
		protected static MsgSwitch debug = new MsgSwitch(MsgLevel.Info);
		protected static SystemConfig sc = new SystemConfig();
		protected Type type = null;
		protected Assembly assembly = null;

		public bool Initialized {
			get { return (this.type != null); }
		}

		public string CodeBase {
			get { return (this.assembly != null ? this.assembly.CodeBase : ""); }
		}

		public string FullName {
			get { return (this.assembly != null ? this.assembly.FullName : ""); }
		}

		public string Location {
			get { return (this.assembly != null ? this.assembly.Location : ""); }
		}

		public string ImageRuntimeVersion {
			get { return (this.assembly != null ? this.assembly.ImageRuntimeVersion : ""); }
		}

		public bool GlobalAssemblyCache {
			get { return (this.assembly != null ? this.assembly.GlobalAssemblyCache : false); }
		}

		[Obsolete("This method is obsolete, please switch to Load(string name, string[] paths, bool checkSystem, bool checkLoaded)!", true)] 
		public static ModuleLoader<CLASS> Load(string path, string name) {
			ModuleLoader<CLASS> ml = new ModuleLoader<CLASS>();

			// Input example:
			//   path = /tmp/
			//   name = Test.dll
			//
			// Is tranformed to
			//   name = Test
			//   paths = [ /tmp/Test.dll ]
			//   checkSystem = true
			//   checkLoaded = false
			string nameX = Path.GetFileNameWithoutExtension(name);
			string pathX = Path.Combine(sc.LibPath, Path.Combine((path == null ? "" : path), (name == null ? "" : name)));

			ml.LoadTypeFrom(null, pathX);

			if (!ml.Initialized) {
				try {
					ml.FindType(null, Assembly.Load(nameX));

				} catch (Exception e) {
					Debug.WriteLineIf(debug.Verbose, "Exception while loading {0}: {1}!", name, e.Message);
				}
			}

			return (ml.Initialized ? ml : null);
		}

		/**
		 * Tries to load a type with the given name in the given path. If checkLoded is
		 * true, the loaded types are checked first. The assembly name is assumed to
		 * be equal to the module/type name.
		 * @param path The path to look for the module.
		 * @param name Name of the modules (== assembly name, == type name)
		 * @param checkLoaded If the already loaded types should be checked, true must be passed here
		 * @return A new instance of the module loader with the given type, null if it
		 * was not found.
		 */
		public static ModuleLoader<CLASS> Load(string name, string[] paths, bool checkSystem, bool checkLoaded) {
			return Load(name, paths, checkSystem, checkLoaded, null);
		}

		public static ModuleLoader<CLASS> Load(string name, string[] paths, bool checkSystem, bool checkLoaded, string libraryName) {
			ModuleLoader<CLASS> ml = new ModuleLoader<CLASS>();

			// Check the already loaded types if requested
			if (checkLoaded) {
				Debug.WriteLineIf(debug.Verbose, "Trying to lookup {0} (type {1}) in the current application domain", name, typeof(CLASS));

				foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies()) {
					ml.FindType(name, a);
				}
			}

			// Check the paths if any
			if ((!ml.Initialized) && (paths != null)) {
				Debug.WriteLineIf(debug.Verbose, "Trying to lookup {0} (type {1}) in the given paths ({2})", name, typeof(CLASS), paths[0]);

				foreach (string path in paths) {
					ml.LoadTypeFrom(name, path);
				}
			}

			// Try to lookup the type in the assemblies registered in the system
			if (!ml.Initialized) {
				Debug.WriteLineIf(debug.Verbose, "Trying to lookup {0} (type {1}) in the system (GAC, ...)", name, typeof(CLASS));

				ml.LoadTypeSystem(name, name);
			}

			// Try to lookup the type in the given optional library name
			if ((!ml.Initialized) && (libraryName != null)) {
				Debug.WriteLineIf(debug.Verbose, "Trying to lookup {0} (type {1}) in the system with the optional library name (GAC, ...)", libraryName, typeof(CLASS));

				ml.LoadTypeSystem(name, libraryName);
			}


			// If a modules (type) was found, return the ModuleLoader instance
			return (ml.Initialized ? ml : null);
		}

		/**
		 * Tries to find a class with the given type (CLASS) and the given name (name) in the given assembly (a).
		 * If a type was found, the internal state is set to 'initialized' (Initialized property).
		 *
		 * @param name The name of the class. If this name equals null, the name comparison is skipped.
		 * @param a The assembly in which this class is supposed to be
		 */
		protected void FindType(string name, Assembly a) {

			if (!this.Initialized) {
				foreach (Type t in a.GetTypes()) {

					if ((name == null) || (t.Name.Equals(name))) {
						bool derivedFromInterface = false;

						// Check if the current type (class) is derived from the type (an interface in this context) we look for
						foreach (Type tx in t.GetInterfaces()) {
							if (tx == typeof(CLASS)) {
								derivedFromInterface = true;
							}
						}

						// Match found, exit
						if ((derivedFromInterface) || ((t == typeof(CLASS)) && (t.IsClass)) || (t.IsSubclassOf(typeof(CLASS)))) {
							Debug.WriteLineIf(debug.Verbose, "Found {0}", t);

							this.type = t;
							this.assembly = a;

							break;
						}
					}
				}
			}
		}

		/**
		 * Tries to load a class with the given type (CLASS) and the given name (name) from the assemblies
		 * residing this the given path (path).
		 * If a type was found, the internal state is set to 'initialized' (Initialized property).
		 *
		 * @param name The name of the class
		 * @param path A path in which one or more assemblies may reside
		 */
		protected void LoadTypeFrom(string name, string path) {

			if (!this.Initialized) {
				string[] files = null;

				// Find a single file
				if (File.Exists(path)) {
					files = new string[] { path };

				// Find files in a directory
				} else if (Directory.Exists(path)) {
					files = Directory.GetFiles(path, "*.dll");

				// A path with a search pattern is now assumed
				} else if (Directory.Exists(Path.GetDirectoryName(path))) {
					files = Directory.GetFiles(Path.GetDirectoryName(path), Path.GetFileName(path));
				}

				// Search for the name
				if (files != null) {
					foreach (string file in files) {
						if (File.Exists(file)) {
							try {
								this.assembly = Assembly.LoadFrom(file);
								FindType(name, this.assembly);

							} catch (Exception e) {
								Debug.WriteLineIf(debug.Verbose, "Exception while processing {0}: {1}!", name, e.Message);
							}
						}

						// A type was found, exit
						if (this.Initialized) {
							break;
						}

						this.assembly = null;
					}
				}
			}
		}

		/**
		 * Tries to load a class with the given type (CLASS) and the given name (name) from the GAC
		 * or any other system-specific path.
		 * If a type was found, the internal state is set to 'initialized' (Initialized property).
		 *
		 * @param name The name of the class
		 */
		protected void LoadTypeSystem(string name, string libraryName) {
			if (!this.Initialized) {
				try {
					this.assembly = Assembly.Load(libraryName);

					FindType(name, this.assembly);

					return;

				} catch (Exception e) {
					Debug.WriteLineIf(debug.Verbose, "Exception while loading {0}: {1}!", name, e.Message);
				}

				this.assembly = null;
			}
		}

		/// <summary>Returns the type of the loaded module</summary>
		public Type ModuleType {
			get { return this.type; }
		}

		/// <summary>Creates a new instance of the loaded type</summary>
		/// <param name="types">The typed of the module's constructor arguments</param>
		/// <param name="arguments">The arguments that should be passed to the constructor</param>
		/// <returns>A new instance of the loaded module</returns>
		public CLASS GetInstance(Type[] types, object[] arguments) {
			CLASS o = default(CLASS);

			try {
				ConstructorInfo ci = null;

				if ((types != null) && (arguments != null)) {
					// Get the public instance constructor that takes the given parameters
					ci = this.type.GetConstructor(types);
					if (ci != null) {
						o = (CLASS)ci.Invoke(arguments);
					} else {
						throw new Exception(String.Format("Constructor {0} not found!", GenerateSignature(this.type.ToString(), types)));
					}

				} else {
					// Get the public instance default constructor
					ci = this.type.GetConstructor(Type.EmptyTypes);
					if (ci != null) {
						o = (CLASS)ci.Invoke(null);
					} else {
						throw new Exception(String.Format("Constructor {0} not found!", GenerateSignature(this.type.ToString(), types)));
					}
				}
			} catch (Exception e) {
				throw new Exception(String.Format("Unable to instanciate {0}: {1}", this.type, e.Message));
			}

			return o;
		}

		protected string GenerateSignature(string name, Type[] types) {
			string temp = "";
			
			if ((types != null) && (types.Length > 0)) {
				temp = types[0].ToString();
				for (int i = 1; i < types.Length; i++) {
					temp += (", " + types[i].ToString());
				}
			}

			return String.Format("{0}({1})", name, temp);
		}
	}
}

