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
  reconfigurations      <- fread(file.path(folder, "reconfigurations.csv"))
  agentReconfigurations <- fread(file.path(folder, "agent-reconfigurations.csv"))

  # Split lists of agents and convert agent IDs to integers.
  reconfigurations[, InvolvedAgents := map(strsplit(InvolvedAgents, " ", fixed=TRUE), strtoi)]
  reconfigurations[, AffectedAgents := map(strsplit(AffectedAgents, " ", fixed=TRUE), strtoi)]

  list(simulation, reconfigurations, agentReconfigurations)
}

TestSet.readData <- function(model, config, rootFolder) {
  # Iterate through all subfolders of testSetFolder and read their data.
  testSetFolder <- file.path(rootFolder, paste(model, " (", config, ")", sep=""))
  testRuns      <- list.dirs(testSetFolder, recursive=FALSE)
  testSetData   <- map(testRuns, TestRun.readData)

  list(model, config, testSetData)
}

Performance.readData <- function(models, configs, rootFolder) {
  map(models, ~ map(configs, ~ TestSet.readData(.y, .x, rootFolder), .))
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