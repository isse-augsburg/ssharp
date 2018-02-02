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
  reconfigurations      <- fread(file.path(folder, "reconfigurations.csv"), colClasses=list(integer64=c("Duration (Ticks)")))
  agentReconfigurations <- fread(file.path(folder, "agent-reconfigurations.csv"), colClasses=list(integer64=c("Duration")))

  # Split lists of agents and convert agent IDs to integers.
  reconfigurations[, InvolvedAgents := map(strsplit(InvolvedAgents, " ", fixed=TRUE), strtoi)]
  reconfigurations[, AffectedAgents := map(strsplit(AffectedAgents, " ", fixed=TRUE), strtoi)]

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
  simulations[,Config := config]
  reconfigurations[,':='(Config = config, Model = model)]
  agentReconfigurations[,':='(Config = config, Model = model)]

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

  list(simulations=simulations, reconfigurations=reconfigurations, agentReconfigurations=agentReconfigurations)
}

##################### Main Program ####################

models  <- c("FewAgentsHighRedundancy", "MediumSizePerformanceMeasurementModel", "ManyAgentsLowRedundancy")
configs <- c("Centralized", "Coalition")

# Select root folder.
rootFolder <- if (interactive()) choose.dir(caption = "Select folder with evaluation data") else getwd()
if (is.na(rootFolder))
  quit()

# Read data.
print("Reading performance data...")
performanceData <- Performance.readData(models, configs, rootFolder)

# TODO: Analyze data.
print("Data analysis not yet implemented.")