#include "Configuration.h"

#include <stdint.h>
#include <iostream>
#include <iomanip>
#include <string>

#ifdef NODEBUG
#undef NODEBUG
#endif /* NODEBUG */
#include <cassert>

#define CASTOR_CHECK_INIT														\
	static uint32_t count = 0;													

#define CASTOR_CHECK(n)															\
{																				\
	std::cout << std::setw(4) << std::setfill('0')								\
		<< count++ << " Checking '" << #n << "'" << std::endl;					\
	assert(n);																	\
}

#define CASTOR_CHECK_THROW(n)													\
try																				\
{																				\
	std::cout << std::setw(4) << std::setfill('0')								\
		<< count++ << " Checking '" << #n << "'" << std::endl;					\
	n;																			\
}																				\
catch (const std::exception &e)													\
{																				\
	std::cout << std::endl;														\
	std::cout << __func__ << ": " << __FILE__ << ":" << __LINE__ << ": "		\
		<< "Caught exception " << e.what() << std::endl;						\
	exit(1);																	\
}																				\
catch (...)																		\
{																				\
	std::cout << std::endl;														\
	std::cout << __func__ << ": " << __FILE__ << ":" << __LINE__ << ": "		\
		<< "Caught unknown exception " << std::endl;							\
	exit(1);																	\
}


CASTOR_CHECK_INIT


void read_config(const std::string config)
{
	castor::Configuration c;
	
	CASTOR_CHECK_THROW(c.load(config));

	bool value;
	CASTOR_CHECK_THROW(value = c.get<bool>("ahoi", "bhoi.choi", "bla", NULL));
	CASTOR_CHECK(value);

	std::vector<bool> args;
	CASTOR_CHECK_THROW(args = c.getAll<bool>("ahoi", "bhoi.choi", "bla", NULL));
	CASTOR_CHECK(args.size() == 4);
	CASTOR_CHECK(args[0] == 1);
	CASTOR_CHECK(args[1] == 0);
	CASTOR_CHECK(args[2] == 1);
	CASTOR_CHECK(args[3] == 1);

	std::vector<std::string> sections;
	CASTOR_CHECK_THROW(sections = c.getSections("ahoi", "bhoi", NULL));
	CASTOR_CHECK(sections.size() == 2);
	CASTOR_CHECK(sections[0] == "choi");
	CASTOR_CHECK(sections[1] == "choi");

	std::vector<std::string> names;
	CASTOR_CHECK_THROW(names = c.getNames("ahoi", "bhoi", "choi", NULL));
	CASTOR_CHECK(names.size() == 0);

	bool exception = false;
	try {
		std::string arg2 = c.get<std::string>("bla", "blubb", "x", "y.z.h.j", NULL);
	} catch (...) {
		exception = true;
	}
	CASTOR_CHECK(exception);

	std::cout << c.serialize() << std::endl;
}

int main(int argc, char *argv[])
{
	if (argc < 2)
	{
		std::cerr << argv[0] << " [path to test-configuration.conf]" << std::endl;
		exit(0);
	}

	read_config(std::string(argv[1]) + "/test-configuration.conf");
}
