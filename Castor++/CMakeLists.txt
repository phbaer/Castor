cmake_minimum_required(VERSION 2.6)
project(Castor++) 

include(FindPkgConfig)

set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -Wall -Wno-write-strings -Wno-deprecated")

set(Boost_USE_STATIC_LIBS   OFF)
set(Boost_USE_MULTITHREADED ON)
find_package(Boost 1.37.0 COMPONENTS system thread filesystem unit_test_framework)

if(Boost_FOUND)
    include_directories(${CMAKE_CURRENT_SOURCE_DIR} ${Boost_INCLUDE_DIRS} ${BoostTest_INCLUDE_DIRS})
    link_directories(${Boost_LIBRARY_DIRS} ${BoostTest_LIBRARY_DIRS})

    file(GLOB Castor_SRC *.cpp)

    add_library(castor++ SHARED ${Castor_SRC})
    target_link_libraries(castor++ ${Boost_SYSTEM_LIBRARY} ${Boost_THREAD_LIBRARY} ${Boost_FILESYSTEM_LIBRARY})

    if (Boost_UNIT_TEST_FRAMEWORK_FOUND)
        add_executable(test-configuration test/configuration.cpp)
        target_link_libraries(test-configuration castor++ ${Boost_UNIT_TEST_FRAMEWORK_LIBRARY})
    endif()
endif()
