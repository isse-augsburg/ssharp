#!/usr/local/bin/python3

# The MIT License (MIT)
# 
# Copyright (c) 2014-2017, Institute for Software & Systems Engineering
# 
# Permission is hereby granted, free of charge, to any person obtaining a copy
# of this software and associated documentation files (the "Software"), to deal
# in the Software without restriction, including without limitation the rights
# to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
# copies of the Software, and to permit persons to whom the Software is
# furnished to do so, subject to the following conditions:
# 
# The above copyright notice and this permission notice shall be included in
# all copies or substantial portions of the Software.
# 
# THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
# IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
# FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
# AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
# LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
# OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
# THE SOFTWARE.

# Run under Windows using Python 3.6
# c:\Python36\Scripts\pip.exe install numpy
# c:\Python36\Scripts\pip.exe install matplotlib
# c:\Python36\python.exe createGraphFigure.py

import csv
import numpy
import matplotlib.pyplot as pyplot
from matplotlib.ticker import FuncFormatter

fileToRead = 'graph.csv'
csvDelimiter = ','
floatDelimiter = '.'
csvRawDataFile = open(fileToRead,'r')
csvRawData = csv.reader(csvRawDataFile, delimiter=csvDelimiter)
csvRows = list(csvRawData)

# read fromValues
fromValues = []
for entry in (csvRows[1])[1:]:
  newValue = float(entry.replace(floatDelimiter, '.'))
  fromValues.append(newValue)

# see http://matplotlib.org/api/matplotlib_configuration_api.html#matplotlib.rc
titleFont = {'fontname':'Garamond','fontsize':16}
labelFont = {'fontname':'Garamond','fontsize':14}
#standardFont = {'family':'serif','serif':['Garamond'],'sans-serif':['Garamond']}
standardFont = {'family':'serif','serif':['Garamond']}
standardFigure = {'figsize': (10,5)}
#pyplot.rcParams['figure.figsize'] = 10,5
pyplot.rc('font',**standardFont)
pyplot.rc('figure',**standardFigure)


def createCustomFormatter(scaleY):
  # https://matplotlib.org/2.0.0/examples/pylab_examples/custom_ticker1.html
  # http://matplotlib.org/api/ticker_api.html#tick-formatting
  # http://matplotlib.org/api/ticker_api.html#matplotlib.ticker.FormatStrFormatter
  #
  factor = 10.0 ** scaleY
  print(factor)
  def formatMoreBeautiful(value, tickPos):
    return '$ %.2f \\times\ 10^{-%s}$' % (value*factor, str(scaleY))
  return FuncFormatter(formatMoreBeautiful)

  
def printRow(rowToRead,yLabelName,fileName,scaleY):
  # read resultValues
  resultValues = []
  row = csvRows[rowToRead]
  rowName = row[0]
  rowData = row[1:]
  print(len(rowData))
  for entry in rowData:
    newValue = float(entry.replace(floatDelimiter, '.'))
    resultValues.append(newValue)
  
  pyplot.cla()
  pyplot.clf()
  fig, ax = pyplot.subplots()
  if scaleY != 1:
    ax.yaxis.set_major_formatter(createCustomFormatter(scaleY))
  pyplot.plot(fromValues,resultValues, 'o-')
  pyplot.title('',**titleFont)
  pyplot.xlabel('Pr(F1)',**labelFont)
  pyplot.xticks([0.0, 1.0])
  pyplot.ylabel(yLabelName,**labelFont)
  pyplot.savefig(fileName, format="svg")
  #pyplot.show()

printRow(2,"Pr(Hazard)","graph.svg",1)
