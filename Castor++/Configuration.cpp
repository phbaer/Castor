/*
 * $Id: Configuration.cpp 275 2008-03-12 14:43:31Z phbaer $
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

#include "Configuration.h"

namespace castor {

	Configuration::Configuration() :
		filename(),
		configRoot(new ConfigNode("root"))
	{}

	Configuration::Configuration(std::string filename) :
		filename(filename), configRoot(new ConfigNode("root"))
	{
		load(filename);
	}

	Configuration::Configuration(std::string filename, const std::string content) :
		filename(filename), configRoot(new ConfigNode("root"))
	{
		load(filename, boost::shared_ptr<std::istream>(new std::istringstream(content)), false, false);
	}

	void Configuration::load(std::string filename, boost::shared_ptr<std::istream> content, bool, bool) {

		this->filename = filename;

		int linePos = 0;
		int chrPos = 0;

		std::string line;

		ConfigNode *currentNode = this->configRoot.get();

		while (content->good()) {

			std::getline(*content, line);
			boost::algorithm::trim_left(line);

			int lineLen = line.size();

			chrPos = 1;

			linePos++;

			while (chrPos < lineLen - 1) {

				if (line.size() == 0) break;

				switch (line[0]) {

					case '#':
						{
							std::string comment = line.substr(1, line.size() - 1);

							boost::trim(comment);
							currentNode->create(ConfigNode::Comment, comment);

							chrPos += line.size() - 1;
						}
						continue;

					case '<':
					case '[':
						{
							size_t end = line.find(']');

							if (end == std::string::npos) {
								end = line.find('>');
							}

							if ((line.size() < 2) || (end == std::string::npos)) {
								std::ostringstream ss;
								ss << "Parse error in " << filename << ", line " << linePos << " character " << chrPos << ": malformed tag!";
								throw ConfigException(ss.str());
							}

							if (end - 1 == 0) {
								std::ostringstream ss;
								ss << "Parse error in " << filename << ", line " << linePos << " character " << chrPos << ": malformed tag, tag name empty!";
								throw ConfigException(ss.str());
							}

							std::string name = line.substr(1, end - 1);

							if ((name[0] == '/') || (name[0] == '!')) {

								if (currentNode == NULL) {
									std::ostringstream ss;
									ss << "Parse error in " << filename << ", line " << linePos << " character " << chrPos << ": no opening tag found!";
									throw ConfigException(ss.str());
								}

								if (name.compare(1, name.size() - 1, currentNode->getName()) != 0) {
									std::ostringstream ss;
									ss << "Parse error in " << filename << ", line " << linePos << " character " << chrPos << ": closing tag does not match opening tag!";
									throw ConfigException(ss.str());
								}

								currentNode = currentNode->getParent();
							} else {
								currentNode = currentNode->create(name);
							}

							if (end <= line.size() - 1) {
								line = line.substr(end + 1, line.size() - end - 1);
							}

							chrPos += (end + 1);
						}
						break;

					default:
						chrPos++;

						if ((line[0] != ' ') && (line[0] != '\t')) {

							size_t curPos = 0;
							bool inString = false;

							std::ostringstream ss;

							while (curPos < line.size()) {

								// TODO: This was commented out; why? phb
								if ((!inString) &&
									((line[curPos] == '[') || (line[curPos] == '<')))
								{
									curPos--;
									break;
								}

								if (line[curPos] == '"') {
									inString = !inString;
									curPos++;
								}

								if (curPos < line.size()) {

									ss << line[curPos];
									curPos++;
								}
							}

							line = (curPos >= line.size() - 1 ? "" : line.substr(curPos + 1, line.size() - curPos - 1));

							chrPos += (curPos - 1);

							std::string element = ss.str();
							std::string key;
							std::string value;

							size_t eq = element.find('=');

							if (eq != std::string::npos) {
								key = element.substr(0, eq - 1);
								value = element.substr(eq + 1, element.size() - eq - 1);

								boost::algorithm::trim(key);
								boost::algorithm::trim(value);
							}

							boost::any a(value);

							currentNode->create(key, a);

						} else {
							line = line.substr(1, line.size() - 1);
						}

						break;
				}
			}
		}


		if (this->configRoot.get() != currentNode) {
			std::ostringstream ss;
			ss << "Parse error in " << filename << ", line " << linePos << " character " << line.size() << ": no closing tag found!";
			throw ConfigException(ss.str());
		}
	}

	void Configuration::serialize_internal(std::ostringstream *ss, ConfigNode *node) {

		if (node == NULL) return;

		if (node->getType() == ConfigNode::Node) {

			*ss << std::string(4 * node->getDepth(), ' ') << "[" << node->getName() << "]" << std::endl;
			
			for (std::vector<ConfigNodePtr>::iterator itr = node->getChildren()->begin();
				 itr != node->getChildren()->end(); itr++)
			{
				serialize_internal(ss, (*itr).get());
			}

			*ss << std::string(4 * node->getDepth(), ' ') << "[!" << node->getName() << "]" << std::endl;

		} else if (node -> getType() == ConfigNode::Leaf) {

			*ss << std::string(4 * node->getDepth(), ' ') << node->getName() << " = " << boost::any_cast<std::string>(node->getValue()) << std::endl;

		} else { // Comment

			*ss << std::string(4 * node->getDepth(), ' ') << "# " << node->getName() << std::endl;

		}
	}

	void Configuration::store() {

		if (this->filename.size() > 0) {
			store(this->filename);
		}
	}

	void Configuration::store(std::string filename) {

		std::ostringstream ss;
		std::ofstream os(filename.c_str(), std::ios_base::out);

		serialize_internal(&ss, this->configRoot.get());

		os << ss.str();
	}

	std::string Configuration::serialize() {

		std::ostringstream ss;
		serialize_internal(&ss, this->configRoot.get());

		return ss.str();
	}

	void Configuration::collect(ConfigNode *node, std::vector<std::string> *params, size_t offset, std::vector<ConfigNode *> *result) {

		std::vector<ConfigNodePtr> *children = node->getChildren();

		if (offset == params->size()) {
			result->push_back(node);
			return;
		}

		for (size_t i = offset; i < params->size(); i++) {
			
			bool found = false;

			for (size_t j = 0; j < children->size(); j++) {

				if ((*children)[j]->getName().compare((*params)[i]) == 0) {
					collect((*children)[j].get(), params, offset + 1, result);
					found = true;
				}
			}

			if (!found) return;
		}
	}

	void Configuration::collectSections(ConfigNode *node, std::vector<std::string> *params, size_t offset, std::vector<ConfigNode *> *result) {

		std::vector<ConfigNodePtr> *children = node->getChildren();

//		for(unsigned int i = 0; i < children->size(); i++){
//			std::cout << "Children " << i << " " << (*children)[i]->getName().c_str() << std::endl;
//		}

//		std::cout << "offset " << offset << std::endl;
//		std::cout << "params->size " << params->size() << std::endl;

		if (offset == params->size()) {
//			std::cout << "pushed " << node->getName().c_str() << std::endl;

			//result->push_back(node);
			for(unsigned int i = 0; i < children->size(); i++){
	
				result->push_back((*children)[i].get());
	
			}

			return;
		}

		for (size_t i = offset; i < params->size(); i++) {
			
			bool found = false;

			for (size_t j = 0; j < children->size(); j++) {

				if ((*children)[j]->getName().compare((*params)[i]) == 0) {
//					std::cout << "found true with " << (*children)[j]->getName().c_str() << std::endl;
					collectSections((*children)[j].get(), params, offset + 1, result);
					found = true;
				}
			}

			if (!found) return;
		}
	}

	std::string Configuration::pathNotFound(std::vector<std::string> *params)
	{
		std::ostringstream os;

		if ((params == NULL) || (params->size() == 0))
		{
			os << "Empty path not found in " << this->filename << "!" << std::endl;
		}
		else
		{
			os << "Path '" << (*params)[0];

			for (size_t i = 1; i < params->size(); i++) {
				os << "." << (*params)[i];
			}

			os << "' not found in " << this->filename << "!" << std::endl;
		}

		return os.str();
	}

	std::vector<std::string> Configuration::getSections(const char *path, ...)
	{
		CONSUME_PARAMS(path);

		// Get relevant nodes
		std::vector<ConfigNode *> nodes;
		collectSections(this->configRoot.get(), params.get(), 0, &nodes);

		// If there are no nodes, exit
		if (nodes.size() == 0) {
			throw ConfigException(pathNotFound(params.get()));
		}

		// Copy only the sections
		std::vector<std::string> result;
		for (size_t i = 0; i < nodes.size(); i++) {
			if (nodes[i]->getType() == ConfigNode::Node) {
				result.push_back(nodes[i]->getName());
			}
		}

		return result;
	}

	std::vector<std::string> Configuration::getNames(const char *path, ...)
	{
		CONSUME_PARAMS(path);

		// Get relevant nodes
		std::vector<ConfigNode *> nodes;
		collect(this->configRoot.get(), params.get(), 0, &nodes);

		// If there are no nodes, exit
		if (nodes.size() == 0) {
			throw ConfigException(pathNotFound(params.get()));
		}

		// Copy only the keys
		std::vector<std::string> result;
		for (size_t i = 0; i < nodes.size(); i++) {
			if (nodes[i]->getType() == ConfigNode::Leaf) {
				result.push_back(nodes[i]->getName());
			}
		}

		return result;
	}

	std::vector<std::string> Configuration::tryGetSections(std::string d, const char *path, ...)
	{
		CONSUME_PARAMS(path);

		// Get relevant nodes
		std::vector<ConfigNode *> nodes;
		collect(this->configRoot.get(), params.get(), 0, &nodes);

		// If there are no nodes, return the default one
		if (nodes.size() == 0) {
			std::vector<std::string> result(1);
			result.push_back(d);
			return result;
		}

		// Copy only the sections
		std::vector<std::string> result;
		for (size_t i = 0; i < nodes.size(); i++) {
			if (nodes[i]->getType() == ConfigNode::Node) {
				result.push_back(nodes[i]->getName());
			}
		}

		return result;
	}

	std::vector<std::string> Configuration::tryGetNames(std::string d, const char *path, ...)
	{
		CONSUME_PARAMS(path);

		// Get relevant nodes
		std::vector<ConfigNode *> nodes;
		collect(this->configRoot.get(), params.get(), 0, &nodes);

		// If there are no nodes, return the default one
		if (nodes.size() == 0) {
			std::vector<std::string> result(1);
			result.push_back(d);
			return result;
		}

		// Copy only the sections
		std::vector<std::string> result;
		for (size_t i = 0; i < nodes.size(); i++) {
			if (nodes[i]->getType() == ConfigNode::Leaf) {
				result.push_back(nodes[i]->getName());
			}
		}

		return result;
	}
};

