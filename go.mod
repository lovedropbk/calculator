module financial-calculator

go 1.23

require (
	github.com/financial-calculator/engines v0.0.0-00010101000000-000000000000
	github.com/lxn/walk v0.0.0-20210112085537-c389da54e794
	github.com/lxn/win v0.0.0-20210218163916-a377121e959e
	github.com/shopspring/decimal v1.3.1
	gopkg.in/yaml.v3 v3.0.1
)

replace github.com/financial-calculator/engines => ./engines

require (
	github.com/kr/text v0.2.0 // indirect
	github.com/niemeyer/pretty v0.0.0-20200227124842-a10e7caefd8e // indirect
	golang.org/x/sys v0.30.0 // indirect
	gopkg.in/Knetic/govaluate.v3 v3.0.0 // indirect
	gopkg.in/check.v1 v1.0.0-20200227125254-8fa46927fb4f // indirect
)
