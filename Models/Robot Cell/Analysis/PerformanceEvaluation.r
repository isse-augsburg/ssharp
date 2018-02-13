#######################################################
##    Performance Evaluation Data Analysis Script    ##
#######################################################

##################### Libraries #######################

# Required for data.table (see below).
# Install with
#     install.packages("bit64")
require(bit64)

# Functional programming library, including map, flatmap.
# Install with
#     install.packages("purrr")
library(purrr)

# Advanced data table querying.
# Install with
#     install.packages("data.table")
library(data.table)

##################### Read Data #######################

TestRun.readData <- function(folder) {
  # Read all 3 files.
  simulation            <- fread(file.path(folder, "simulation.csv"))
  reconfigurations      <- fread(file.path(folder, "reconfigurations.csv"), colClasses=list(integer64=c("Duration"))) #, "Duration"
  agentReconfigurations <- fread(file.path(folder, "agent-reconfigurations.csv"), colClasses=list(integer64=c("Duration")))

  # Split lists of agents and convert agent IDs to integers.
  reconfigurations[, InvolvedAgents := map(strsplit(InvolvedAgents, " ", fixed=TRUE), strtoi)]
  reconfigurations[, AffectedAgents := map(strsplit(AffectedAgents, " ", fixed=TRUE), strtoi)]

  #reconfigurations[, ':='(Duration = `Duration (Ticks)`, `Duration (Ticks)` = NULL)]
  simulation[, Model := gsub("2", "", Model)]

  # Add seed to reconfigurations and agentReconfigurations tables.
  seed <- simulation[,Seed]
  reconfigurations[,Seed := seed]
  agentReconfigurations[,Seed := seed]

  list(simulation=simulation, reconfigurations=reconfigurations, agentReconfigurations=agentReconfigurations)
}

TestSet.exists <- function(model, config, rootFolder) {
  testSetFolder <- file.path(rootFolder, paste(model, " (", config, ")", sep=""))
  dir.exists(testSetFolder)
}

TestSet.readData <- function(model, config, rootFolder) {
  # Iterate through all subfolders of testSetFolder and read their data.
  testSetFolder <- file.path(rootFolder, paste(model, " (", config, ")", sep=""))
  testRuns      <- list.dirs(testSetFolder, recursive=FALSE)
  testSetData   <- map(testRuns, TestRun.readData)

  # Combine respective tables.
  simulations           <- rbindlist(map(testSetData, ~ .[["simulation"]]))
  reconfigurations      <- rbindlist(map(testSetData, ~ .[["reconfigurations"]]))
  agentReconfigurations <- rbindlist(map(testSetData, ~ .[["agentReconfigurations"]]))

  # Add test set information as necessary.
  simulations[, Config := config]
  reconfigurations[, Config := config][, Model := model]
  agentReconfigurations[, Config := config][, Model := model]

  list(model, config, simulations=simulations, reconfigurations=reconfigurations, agentReconfigurations=agentReconfigurations)
}

Performance.readData <- function(models, configs, rootFolder) {
  # Iterate through all existing test sets and read their data.
  testSets        <- cross2(models, configs, .filter = ~ !TestSet.exists(.x, .y, rootFolder))
  performanceData <- map(testSets, ~ TestSet.readData(.[1], .[2], rootFolder))

  # Combine respective tables.
  simulations           <- rbindlist(map(performanceData, ~ .[["simulations"]]))
  reconfigurations      <- rbindlist(map(performanceData, ~ .[["reconfigurations"]]))
  agentReconfigurations <- rbindlist(map(performanceData, ~ .[["agentReconfigurations"]]))

  # Set keys on tables, for performance and joins.
  setkey(simulations, Model, Config, Seed)
  setkey(reconfigurations, Model, Config, Seed, Step)
  setkey(agentReconfigurations, Model, Config, Seed, Agent, Step)
  
  # TODO: temporary, remove
  #agentReconfigurations <- unique(agentReconfigurations)

  list(simulations=simulations, reconfigurations=reconfigurations, agentReconfigurations=agentReconfigurations)
}

##################### Analyze Data ####################

geoMean <- function(x) {
  exp(mean(log(x)))
}
harmMean <- function(x) {
  1/mean(1/x)
}

columnMatch <- function(...) {
  setNames(nm = c(...))
}

stats <- function(col) {
  list(mean=mean(col), median=as.double(median(col)), geo=geoMean(col), harm=harmMean(col), min=min(col), max=max(col), sd=sd(col))
}

Performance.timeBetweenReconfs <- function(reconfigurations) {
  plusOneStep <- reconfigurations[, .(Step = Step - 1, Duration, Seed, Config, Model, realStep = Step)]
  sameStep <- reconfigurations[, .(Step = Step, Duration, Seed, Config, Model, realStep = Step)]
  join <- sameStep[plusOneStep, on = columnMatch("Model", "Config", "Seed", "Step"), roll=TRUE, nomatch=0L]
  join[, .(Model, Config, Seed, firstReconf.Step = realStep, firstReconf.Duration = Duration, secondReconf.Step = i.realStep, secondReconf.Duration = i.Duration, timeBetween = i.realStep - realStep)]
}

Performance.timeBetweenAgentReconfs <- function(agentReconfigurations) {
  plusOneStep <- agentReconfigurations[, .(Step = Step - 1, Agent, Duration, Seed, Config, Model, realStep = Step)]
  sameStep <- agentReconfigurations[, .(Step = Step, Agent, Duration, Seed, Config, Model, realStep = Step)]
  join <- sameStep[plusOneStep, on = columnMatch("Model", "Config", "Seed", "Agent", "Step"), roll=TRUE, nomatch=0L]
  join[, .(Model, Config, Seed, Agent, firstReconf.Step = realStep, firstReconf.Duration = Duration, secondReconf.Step = i.realStep, secondReconf.Duration = i.Duration, timeBetween = i.realStep - realStep)]
}

Performance.timePerformance <- function(simulations, agentReconfigurations) {
  # for each run and agent, sum the reconfiguration time
  reconfTime <- agentReconfigurations[, .(reconfTime = sum(Duration)), .(Model, Config, Seed, Agent)]

  # join with the simulation and compute ratio between agent reconfiguration time and total simulation time
  join <- reconfTime[simulations, on = columnMatch("Model", "Config", "Seed"), nomatch=0L]
  join[, timePerformance := reconfTime / (End - Start)]

  join
}

##################### Main Program ####################

models  <- c("FewAgentsHighRedundancy", "ManyAgentsHighRedundancy", "ManyAgentsLowRedundancy")
configs <- c("Centralized", "Coalition")

# Select root folder.
rootFolder <- if (interactive()) choose.dir(caption = "Select folder with evaluation data") else getwd()
if (is.na(rootFolder))
  quit()

# Read data.
print("Reading performance data...")
performanceData       <- Performance.readData(models, configs, rootFolder)
simulations           <- performanceData[["simulations"]]
reconfigurations      <- performanceData[["reconfigurations"]]
agentReconfigurations <- performanceData[["agentReconfigurations"]]

# Analyze data.
print("Analyzing data...")
result.Throughput               <- simulations[, .(Capability = mean(`Capability Throughput`), Resource = mean(`Resource Throughput`)), .(Model, Config)]
result.NumReconfigurations      <- reconfigurations[, .(count = .N), .(Config, Model, Seed)][, .(mean = mean(count), median = as.double(median(count)), range = max(count)-min(count)), .(Config, Model)]
result.ChangesPerReconf         <- reconfigurations[Failed == FALSE, .(changes = AddedRoles + RemovedRoles, Model, Config)][, .(mean = mean(changes), median = as.double(median(changes)), range = max(changes) - min(changes), min = min(changes), max = max(changes)), .(Config, Model)]
result.InvolvementEfficiency    <- reconfigurations[Failed == FALSE, .(numAffected = as.double(lapply(AffectedAgents, "length")), numInvolved = as.double(lapply(InvolvedAgents, "length")), Model, Config)][numInvolved != 0, mean(numAffected / numInvolved), .(Model, Config)]

result.TimeBetweenReconfs       <-           Performance.timeBetweenReconfs(reconfigurations)[, stats(timeBetween), .(Model, Config)]
result.TimeBetweenAgentReconfs  <- Performance.timeBetweenAgentReconfs(agentReconfigurations)[, stats(timeBetween), .(Model, Config)]

result.ReconfigurationTime      <-      reconfigurations[, stats(Duration), .(Model, Config)]
result.AgentReconfigurationTime <- agentReconfigurations[, stats(Duration), .(Model, Config)]

result.ReconfigurationSuccess   <- reconfigurations[, .(failed=sum(Failed), total=.N, percentage=(sum(Failed)/.N)*100, numSeeds=length(unique(Seed))), .(Model, Config)]

result.AgentTimePerformance     <- Performance.timePerformance(simulations, agentReconfigurations)
result.SystemTimePerformance    <- result.AgentTimePerformance[, stats(timePerformance), .(Model, Config)]