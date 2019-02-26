# Options Uploader Service

A service to automate the upload of Options price to Aspect.

* reads in data from a source (e.g. an ICE .dat file)
* gets a list of option instruments from Aspect
* filters the input data to select that for which we have a target EO or ETO instrument
* maps the data to the required input format
* uploads each price in turn to Aspect

*TODO Add more detail here once service is more fleshed out*