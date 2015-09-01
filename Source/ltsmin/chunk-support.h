// Copyright (c) 2008, 2009, 2010, 2011, 2012, 2013, 2014, 2015 Formal Methods and Tools, University of Twente
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are
// met:
// 
//  * Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 
//  * Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 
//  * Neither the name of the University of Twente nor the names of its
//    contributors may be used to endorse or promote products derived from
//    this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
// LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
// A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
// OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
// SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
// LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
// DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
// THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

/**
\file chunk_support.h
\defgroup chunk_support Chunk Support

A chunk is a pair of a length and a pointer to a piece of memory of at least that size.
A packed chunk is a length followed by the data.
*/
//@{

/**
Define a type for chunk lengths.
*/
typedef uint32_t chunk_len;

/** Chunk as a length,pointer structure.
*/
typedef struct {
	chunk_len len;
	char *data;
} chunk;

/** Chunk as a length,data packed structure.
*/
typedef struct {
	chunk_len len;
	char data[];
} pchunk;

/**
Convert a standard C string to a chunk.
*/
#define chunk_str(s) (chunk{(chunk_len)strlen(s),((char*)s)})

/**
Wrap a length and a pointer as a chunk.
*/
#define chunk_ld(l,d) ((chunk){l,d})

/**
\brief Copy the given binary source chunk and encode it as a string chunk.

Any printable, non-escape character is copied.
The escape caracter is encoded as two escape characters.
Any non-printable character is encoded as the escape character followed by the
character in hex. (E.g. with escape ' (char)0 becomes '00).
*/
extern void chunk_encode_copy(chunk dst, chunk src, char escape);

/**
\brief Copy the given string chunk and decode it.

This function shortens the destination chunk if necessary.
*/
extern void chunk_decode_copy(chunk dst, chunk src, char escape);

/**
\brief Copy the chunk to a string.
If all characters are printable and non-white space, the characters are copied verbatim.
If all characters are printable, but there is white space then a quoted form is used.
Otherwise, the results is \#hex ... hex\#. The empty string is "".
*/
extern void chunk2string(chunk src, size_t  dst_size, char*dst);

/**
\brief Decode a string to a chunk.
*/
extern void string2chunk(char*src, chunk *dst);

//@}
