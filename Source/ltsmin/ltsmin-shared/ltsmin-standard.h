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

/*
* ltsmin-standard.h
*
*  Defines constants that are used internally by LTSmin to represent
*  transition systems and tool behavior.
*
*  Created on: Aug 9, 2012
*      Author: laarman
*/

/**
* Exit codes
*/

#define LTSMIN_EXIT_COUNTER_EXAMPLE     1
#define LTSMIN_EXIT_SUCCESS             0
#define LTSMIN_EXIT_FAILURE             255
#define LTSMIN_EXIT_UNSOUND             2

/**
* Matrices
*/

#define LTSMIN_MATRIX_ACTIONS_READS     "dm_actions_reads"
#define LTSMIN_MUST_DISABLE_MATRIX      "dm_must_disable"
#define LTSMIN_MUST_ENABLE_MATRIX       "dm_must_enable"

/**
* Types
*/

#define LTSMIN_TYPE_BOOL                "bool"


/**
* Values
*/

#define LTSMIN_VALUE_BOOL_FALSE         "false" // GBchunkPutAt(.., 0)
#define LTSMIN_VALUE_BOOL_TRUE          "true"  // GBchunkPutAt(.., 1)
#define LTSMIN_VALUE_ACTION_PROGRESS    "progress"  // progress actions
/* A value that contains "<progress>" is counted as a progress transition */
#define LTSMIN_VALUE_STATEMENT_PROGRESS "<progress>"


/**
* Edges
*/

/** The invisible action.
*/
#define LTSMIN_EDGE_VALUE_TAU           "tau"

/** actions (has to be a prefix: defined as "action_label" in mcrl) */
#define LTSMIN_EDGE_TYPE_ACTION_PREFIX  "action"

/** actions class, for use in e.g. Mapa models. */
#define LTSMIN_EDGE_TYPE_ACTION_CLASS  "action_class"

/**
* Statements, used for:
* - pretty printing model transitions
* - extracting structural features of transitions, see LTSMIN_VALUE_STATEMENT_PROGRESS
* The assumption is here that this type contains at least as many values as
* transition groups.
**/
#define LTSMIN_EDGE_TYPE_STATEMENT      "statement"

/**
@brief The name and type of the hyper edge group.

Hyper edges are represented using an extra edge label.
If the value of this label is 0 then the edge is not an hyper edge.
Otherwise edges, which start is the same state and are marked with the same hyper edge group
are part of a single hyper edge.
*/
#define LTSMIN_EDGE_TYPE_HYPEREDGE_GROUP "group" 

/**
* States labels
*/

/* Guard prefixes */
#define LTSMIN_LABEL_TYPE_GUARD_PREFIX  "guard"
