# Load packages
for (package in c('bnlearn', 'jsonlite')) {
  if (!require(package, character.only=T, quietly=T)) {
    install.packages(package, repos="http://cran.rstudio.com/")
    library(package, character.only=T)
  }
}

# Read necessary input files from command line arguments
args <- commandArgs(trailingOnly = TRUE)
white <- data.frame(read.csv(args[1]))
black <- data.frame(read.csv(args[2]))

load_data <- function(files) {
  frames <- lapply(files, read.csv, colClasses = c("factor"))
  do.call(rbind, frames)
}
data <- load_data(args[3:length(args)])

if(!is.data.frame(white) || nrow(white) == 0) {
  white <- NULL
}
if(!is.data.frame(black) || nrow(black) == 0) {
  black <- NULL
}

# Learn dag structure and parameters
dagResult <- tabu(data, whitelist = white, blacklist = black)
dagParams <-  bn.fit(dagResult, data)

# Convert probability tables to correctly named data frames
probToDataFrame <- function(elem, name) {
  probFrame <- list(rvar = name, conditions = elem$parents, probs = as.vector(elem$prob))
  probFrame
}

cat("\n\nRESULTING DAG\n")
# Construct object with relevant results
probTables <- mapply(probToDataFrame, elem = dagParams, name = names(dagParams), SIMPLIFY = FALSE)
probTables <- unname(probTables)
all <- list(nodes = names(dagResult$nodes), arcs = as.data.frame(dagResult$arcs), probTables = probTables)
toJSON(all)


