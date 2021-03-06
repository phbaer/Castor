/*
 * $Id: Configuration.h 275 2008-03-12 14:43:31Z phbaer $
 *
 * Copyright 2008 Carpe Noctem, Distributed Systems Group,
 * University of Kassel. All right reserved.
 *
 * The code is licensed under the Carpe Noctem Userfriendly BSD-Based
 * License (CNUBBL). Redistribution and use in source and binary forms,
 * with or without modification, are permitted provided that the
 * conditions of the CNUBBL are met.
 *
 * You should have received a copy of the CNUBBL along with this
 * software. The license is also available on our website:
 * http://carpenoctem.das-lab.net/license.txt
 */

#ifndef CASTOR_CONFIGURATION_H
#define CASTOR_CONFIGURATION_H 1

#include <vector>
#include <string>
#include <iostream>
#include <fstream>
#include <cstdarg>
#include <sstream>
#include <string>
#include <cstdarg>

#include <boost/algorithm/string.hpp>
#include <boost/lexical_cast.hpp>
#include <boost/shared_ptr.hpp>
#include <boost/any.hpp>

#include "ConfigException.h"

#define CONSUME_PARAMS(path) \
boost::shared_ptr<std::vector<std::string> > params(new std::vector<std::string>());\
if (path != NULL) {\
	va_list ap;\
	va_start(ap, path);\
	const char *temp = path;\
	do { \
		std::vector<std::string> result; \
		boost::split(result, temp, boost::is_any_of(".")); \
		for (size_t i = 0; i < result.size(); i++) { \
			params->push_back(result[i]); \
		} \
	} while ((temp = va_arg(ap, const char *)) != NULL); \
	va_end(ap); \
}

namespace castor {

	class ConfigNode;

	typedef boost::shared_ptr<ConfigNode> ConfigNodePtr;

	class ConfigNode {

		public:

			typedef enum {
				Node = 0,
				Leaf = 1,
				Comment = 2,
			} Type;

		protected:

			std::string name;
			boost::any value;
			ConfigNode *parent;
			std::vector<ConfigNodePtr> children;
			int depth;
			Type type;

		public:

			ConfigNode(std::string name) :
				name(name), value(), parent(NULL), children(), depth(0), type(Node)
			{
			}

			ConfigNode(Type type, std::string name) :
				name(name), value(), parent(NULL), children(), depth(0), type(type)
			{
			}

			ConfigNode(std::string name, boost::any &value) :
				name(name), value(value), parent(NULL), children(), depth(0), type(Leaf)
			{
			}

			ConfigNode(const ConfigNode &other) :
				name(other.name), value(other.value), parent(other.parent),
				children(other.children), depth(other.depth), type(other.type)
			{
			}

			~ConfigNode() {
//				std::cout << "deleting " << this->name << std::endl;
			}

			ConfigNode *create(std::string name) {
				this->children.push_back(ConfigNodePtr(new ConfigNode(name)));
				this->children.back()->setParent(this);
				return this->children.back().get();
			}

			ConfigNode *create(Type type, std::string name) {
				this->children.push_back(ConfigNodePtr(new ConfigNode(type, name)));
				this->children.back()->setParent(this);
				return this->children.back().get();
			}

			ConfigNode *create(std::string name, boost::any &value) {
				this->children.push_back(ConfigNodePtr(new ConfigNode(name, value)));
				this->children.back()->setParent(this);
				return this->children.back().get();
			}

			std::vector<ConfigNodePtr> *getChildren() {
				return &this->children;
			}

			ConfigNode *getParent() {
				return this->parent;
			}

			void setParent(ConfigNode *parent) {
				this->parent = parent;
				this->depth = parent->depth + 1;
			}

			const boost::any &getValue() const {
				return this->value;
			}

			void setValue(boost::any &value) {
				this->value = value;
			}

			const std::string &getName() const {
				return this->name;
			}

			int getDepth() const {
				return this->depth;
			}

			Type getType() const {
				return this->type;
			}

			ConfigNode &operator=(const ConfigNode &other) {

				this->name = other.name;
				this->value = other.value;
				this->parent = other.parent;
				this->children = other.children;
				this->depth = other.depth;
				this->type = other.type;

				return *this;
			}
	};

	class Configuration {

		protected:

			std::string filename;

			ConfigNodePtr configRoot;

			void serialize_internal(std::ostringstream *ss, ConfigNode *node);

			template<typename Target>
				Target convert(std::string value) {

					if (typeid(Target) == typeid(bool)) {

						boost::algorithm::to_lower(value);

						if (("false" == value) || ("no" == value) || ("0" == value)) {
							return boost::lexical_cast<Target>(false);
						}

						return boost::lexical_cast<Target>(true);
					}

					printf("Value %s\n", value.c_str());

					return boost::lexical_cast<Target>(value);
				}

			void collect(ConfigNode *node, std::vector<std::string> *params, size_t offset, std::vector<ConfigNode *> *result);
			void collectSections(ConfigNode *node, std::vector<std::string> *params, size_t offset, std::vector<ConfigNode *> *result);
			std::string pathNotFound(std::vector<std::string> *params);

		public:
			Configuration();
			Configuration(std::string filename);
			Configuration(std::string filename, const std::string content);

			inline void load(std::string filename) { load(filename, boost::shared_ptr<std::ifstream>(new std::ifstream(filename.c_str(), std::ifstream::in)), false, false); }

			void load(std::string filename, boost::shared_ptr<std::istream> content, bool create, bool replace);

			void store();
			void store(std::string filename);

			std::string serialize();

			template<typename T>
				T get(const char *path, ...) {

					CONSUME_PARAMS(path);

					std::vector<ConfigNode *> nodes;
					collect(this->configRoot.get(), params.get(), 0, &nodes);

					if (nodes.size() == 0) {
						throw ConfigException(pathNotFound(params.get()));
					}

					return convert<T>(boost::any_cast<std::string>(nodes[0]->getValue()));
				}

			template<typename T>
				std::vector<T> getAll(const char *path, ...)
				{
					CONSUME_PARAMS(path);
		
					// Get relevant nodes
					std::vector<ConfigNode *> nodes;
					collect(this->configRoot.get(), params.get(), 0, &nodes);
		
					// If there are no nodes, exit
					if (nodes.size() == 0) {
						throw ConfigException(pathNotFound(params.get()));
					}
		
					// Copy only all values over
					std::vector<T> result;
					for (size_t i = 0; i < nodes.size(); i++) {
						result.push_back(convert<T>(boost::any_cast<std::string>(nodes[i]->getValue())));
					}

					return result;

				}

			template<typename T>
				T tryGet(T d, const char *path, ...) {

					CONSUME_PARAMS(path);

					std::vector<ConfigNode *> nodes;

					collect(this->configRoot.get(), params.get(), 0, &nodes);

					if (nodes.size() == 0) {
						return d;
					}

					return convert<T>(boost::any_cast<std::string>(nodes[0]->getValue()));
				}

			template<typename T>
				boost::shared_ptr<std::vector<T> > tryGetAll(T d, const char *path, ...) {

					CONSUME_PARAMS(path);

					std::vector<ConfigNode *> nodes;

					collect(this->configRoot.get(), params.get(), 0, &nodes);

					boost::shared_ptr<std::vector<T> > result(new std::vector<T>());

					if (nodes.size() == 0) {

						result->push_back(d);

						return result;
					}

					for (int i = 0; i < nodes.size(); i++) {
						result->push_back(convert<T>(boost::any_cast<std::string>(nodes[i]->getValue())));
					}

					return result;
				}

			template<typename T>
				void set(T value, const char *path, ...) {

					CONSUME_PARAMS(path);

					std::vector<ConfigNode *> nodes;

					collect(this->configRoot.get(), params.get(), 0, &nodes);

					for (int i = 0; i < nodes.size(); i++) {
						if (nodes[i]->getType() == ConfigNode::Leaf) {
							nodes[i]->setValue(value);
						}
					}
				}

			std::vector<std::string> getSections(const char *path, ...);
			std::vector<std::string> getNames(const char *path, ...);

			std::vector<std::string> tryGetSections(std::string d, const char *path, ...);
			std::vector<std::string> tryGetNames(std::string d, const char *path, ...);
	};

};

#endif /* CASTOR_CONFIGURATION_H */

