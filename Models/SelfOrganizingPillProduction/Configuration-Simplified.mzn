% ==========================================
% Configuration-Simplified.mzn
% ---------------------
% Encodes the role allocation problem faced
% in the self-organizing pill production system
% as a rather compact constraint model
% -> requires some simple postprocessing
%    to get an object graph in the ODP
%
% Instead of using role segments,
% we only decide a function 
%      taskAssigned : TASK_IND -> AGENT
%
% A major abstraction assumption is to only
% use a reachability graph in "connected"
% this graph needs to calculate the trans.
% closure as preprocessing and fill in paths
% for the solution.

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
int: lastTask = max(TASK_IND);

array[TASK_IND] of int: task_amount; % how much of each ingredient is required (use 1 for produce, consume)

% agent specification
int: noAgents;
set of int: AGENT = 1..noAgents;

% Describes how much an agent has available of an ingredient (use 0 or 1 for produce, consume).
array[AGENT, CAP] of int: capabilities;

% connectivity -- now this should be reachability (i.e., transitively closed)
array[AGENT,AGENT] of bool: isConnected;

% For each step s, the responsible agent taskAssigned[s] has enough of capability task[s] available.
% constraint forall(s in TASK_IND) (task_amount[s] <= capabilities[taskAssigned[s], task[s]]);

% this can be calculated statically to relieve the solver a bit 
array[TASK_IND,AGENT] of bool: canDo = array2d(TASK_IND, AGENT, [task_amount[t] <= capabilities[a, task[t]] | t  in TASK_IND, a in AGENT]);
array[TASK_IND] of bool: satisfiable = [ exists(a in AGENT) (canDo[t,a]) | t in TASK_IND];

% not really a constraint but an input data validation
% constraint forall(t in TASK_IND) (assert(satisfiable[t],"Task \(t) cannot be done by *any* agent!"));
constraint forall(t in TASK_IND) (satisfiable[t]);

% ----------------------------------------
% decision variables
% ----------------------------------------

array[TASK_IND] of var AGENT: taskAssigned; % which agent is responsible for a given step in the task

% ----------------------------------------
% decision variables
% ----------------------------------------
% Agents can actually do their assigned jobs
constraint forall(s in TASK_IND) ( canDo[s,taskAssigned[s]] );

% If we switch from one agent to another, those guys need be connected; that way we make progress in every TASK_IND step
constraint forall(s in 1..lastTask-1) (taskAssigned[s] != taskAssigned[s+1] -> isConnected[taskAssigned[s], taskAssigned[s+1]]);

% ----------------------------------------
% objective function 
% ----------------------------------------
% TODO: maybe minimize sum of allocatedRoles.length over all agents? (use less busy stations)
solve
satisfy;

output [
"agents = \(taskAssigned)\n"
];

