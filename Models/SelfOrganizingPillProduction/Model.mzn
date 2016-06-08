include "globals.mzn";

%%%%%%%%%%%%%%%%%%%%%% input data %%%%%%%%%%%%%%%%%%%%%%
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
set of int: AGENT_0 = AGENT union {0};

% Describes how much an agent has available of an ingredient (use 0 or 1 for produce, consume).
array[AGENT, CAP] of int: capabilities;

% connectivity
array[AGENT,AGENT] of bool: isConnected;

%%%%%%%%%%%%%%%%%%%%%% model %%%%%%%%%%%%%%%%%%%%%%

% worst case: between each step, almost all agents have to be visited
int: maxSteps = max(TASK_IND) * noAgents;
set of int: STEPS = 1..maxSteps;

% the step in which this part of the task is actually executed
array[TASK_IND] of var STEPS: relevantStep;

% start at the beginning: no steps before production
constraint relevantStep[1] = 1;

% capabilities are applied in order
constraint forall(t in 1..max(TASK_IND)-1) (relevantStep[t] < relevantStep[t+1]);

% last capability applied
var int: lastStep = relevantStep[max(TASK_IND)];

% sequence of agents the resource traverses
array[STEPS] of var AGENT_0: route;

% each step on the route is valid
constraint forall(s in 1..lastStep-1) (route[s] = route[s+1] \/ isConnected[route[s], route[s+1]]);

% capabilities are available
array[AGENT, TASK_IND] of bool: canDo = array2d(AGENT, TASK_IND, [task_amount[t] <= capabilities[a, task[t]] | a in AGENT, t  in TASK_IND]);
constraint forall(t in TASK_IND) (canDo[route[relevantStep[t]], t]);

% route ends when last capability applied, not before
constraint forall(s in STEPS) (s > lastStep <-> route[s] = 0);

% for output only
array[STEPS] of var TASK_IND_0: worked_on;
constraint forall(t in TASK_IND) (worked_on[relevantStep[t]] = t);

solve minimize lastStep;
%solve satisfy;

output [
"agents = \([route[i] | i in 1..lastStep ])\n"++
"workedOn = \([ worked_on[i] | i in 1..lastStep ])\n"
]