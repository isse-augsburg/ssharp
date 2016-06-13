% ==========================================
% rolealloc-compact.mzn
% ---------------------
% Encodes the role allocation problem faced
% in the self-organizing pill production system
% as a rather compact constraint model
% -> requires some simple postprocessing
%    to get an object graph in the ODP
%
% Core idea is to encode roles as "role segments"
% where each role segment can be either
%  *) an applied capability
%  *) a transportation action (of a cart)

% ISSE Augsburg, 2016
% ==========================================

include "stdlib.mzn";
include "increasing.mzn";

% ----------------------------------------
% input data
% ----------------------------------------

int: noCapabilities;
set of int: CAP = 1..noCapabilities;

% task specification
array[int] of CAP: task; % the sequence of capabilities to apply
set of int: TASK_IND = index_set(task);
array[TASK_IND] of int: task_amount; % how much of each ingredient is required (use 1 for produce, consume)

set of int: TASK_IND_0 = TASK_IND union {0}; % includes the zero for the first pointer "incoming port from bottom"

% agent specification
int: noAgents;
set of int: AGENT = 1..noAgents;

% Describes how much an agent has available of an ingredient (use 0 or 1 for produce, consume).
array[AGENT, CAP] of int: capabilities;

% connectivity
array[AGENT,AGENT] of bool: isConnected;

% ----------------------------------------
% decision variables
% ----------------------------------------

% allocated role segments
% should be as few as possible, should in principle be bounded by the number of capabilities to apply (including movement tasks) 

int: maxRoleSegments = (noAgents-1)*max(TASK_IND); % worst case: between each step, all other stations have to be visited
set of int: ROLE_SEGMENTS = 1..maxRoleSegments;
set of int: ROLE_SEGMENTS_0 = ROLE_SEGMENTS union {0};
var ROLE_SEGMENTS: lastRoleSegment; % to be decided by the solver, after that we do not care about the rest

% postcondition
array[ROLE_SEGMENTS_0] of var TASK_IND_0: poststate; % encoded as "processed_until"
constraint increasing(poststate); % monotonically increasing series
constraint poststate[0] = 0 /\ poststate[lastRoleSegment] = max(TASK_IND); % fixed values

array[ROLE_SEGMENTS] of var AGENT: agent; % which agent is responsible for a given segment
array[ROLE_SEGMENTS] of var 0..1: capApplied; % encodes if a role segment is empty (i.e. transportation) or not (basically a boolean)

% better viewpoint for capabilities constraint
array[TASK_IND] of var AGENT: taskAssigned; % which agent is responsible for a given step in the task

% ----------------------------------------
% constraints ensuring valid transitions
% for role segments
% ----------------------------------------

% If a role segment is not transport (i.e. it represents the application of one capability),
% the state has advanced by one after it was executed.
constraint forall(r in ROLE_SEGMENTS) (poststate[r] = poststate[r-1] + capApplied[r]);

% Channeling role segments viewpoint and taskAssigned viewpoint:
% If a role segment is not transport, then the agent responsible for applying the capability
% poststate[r] of the segment is the agent responsible for the role segment itself, agent[r].
constraint forall(r in ROLE_SEGMENTS) (
  capApplied[r] = 1 -> (taskAssigned[poststate[r]] = agent[r])
);

% For each step s, the responsible agent taskAssigned[s] has enough of capability task[s] available.
%constraint forall(s in TASK_IND) (task_amount[s] <= capabilities[taskAssigned[s], task[s]]);
constraint forall(c in CAP, a in AGENT) (sum([ task_amount[s] | s in TASK_IND where task[s] = c /\ taskAssigned[s] = a ]) <= capabilities[a, c]);

% Connect adjacent role segments of different agents.
constraint forall(r in 1..lastRoleSegment-1) (agent[r] != agent[r+1] -> isConnected[agent[r], agent[r+1]]);

% ----------------------------------------
% objective function (make plans short)
% ----------------------------------------
% TODO: maybe minimize sum of allocatedRoles.length over all agents? (use less busy stations)
solve
% prioritize shorter plans (domain-specific search heuristic)
%:: int_search([lastRoleSegment], input_order,indomain_min,complete)

:: int_search(capApplied, first_fail ,indomain_max,complete)
minimize lastRoleSegment;

output [
"agents = \([agent[i] | i in 1..lastRoleSegment] )\n"++
"workedOn = \([if capApplied[i] = 1 then poststate[i] else 0 endif | i in 1..lastRoleSegment] )\n"
];