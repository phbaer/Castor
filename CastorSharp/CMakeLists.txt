cmake_minimum_required(VERSION 2.6)
project(CastorSharp)

set(CastorSharp_CSFLAGS
    "-optimize+"
    "-nowarn:1570,1572,1591"
    "-debug"
    "-keyfile:${CMAKE_CURRENT_SOURCE_DIR}/Castor.snk")
set(CastorSharp_OUT
    "${CMAKE_CURRENT_BINARY_DIR}/Spica.Castor.dll")
#set(CastorSharp_LIBS
#    "-r:Mono.C5.dll"
#    "-r:Mono.Posix.dll"
#    "-r:ICSharpCode.SharpZipLib.dll")

file(GLOB CastorSharp_SRC *.cs)

add_custom_command(
    OUTPUT ${CastorSharp_OUT}
    DEPENDS ${CastorSharp_SRC}
    COMMAND /usr/bin/gmcs
        ${CastorSharp_CSFLAGS}
        ${CastorSharp_SRC}
        ${CastorSharp_LIBS}
        -target:library
        -out:${CastorSharp_OUT})

add_custom_target(
    ${CMAKE_PROJECT_NAME}
    ALL DEPENDS ${CastorSharp_OUT})

#install(FILES ${CastorSharp_OUT}
#    DESTINATION ${CMAKE_CURRENT_SOURCE_DIR}/../../lib/)
