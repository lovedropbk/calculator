module financial-calculator

go 1.23.0

toolchain go1.24.4

require (
	github.com/financial-calculator/engines v0.0.0-00010101000000-000000000000
	github.com/lxn/walk v0.0.0-20210112085537-c389da54e794
	github.com/lxn/win v0.0.0-20210218163916-a377121e959e
	github.com/shopspring/decimal v1.3.1
	github.com/tailscale/walk v0.0.0-20250702155327-6376defdac3f
	github.com/xuri/excelize/v2 v2.9.1
	gopkg.in/yaml.v3 v3.0.1
)

replace github.com/financial-calculator/engines => ./engines

require (
	github.com/dblohm7/wingoes v0.0.0-20231019175336-f6e33aa7cc34 // indirect
	github.com/kr/text v0.2.0 // indirect
	github.com/niemeyer/pretty v0.0.0-20200227124842-a10e7caefd8e // indirect
	github.com/richardlehane/mscfb v1.0.4 // indirect
	github.com/richardlehane/msoleps v1.0.4 // indirect
	github.com/tailscale/win v0.0.0-20250213223159-5992cb43ca35 // indirect
	github.com/tiendc/go-deepcopy v1.6.0 // indirect
	github.com/xuri/efp v0.0.1 // indirect
	github.com/xuri/nfp v0.0.1 // indirect
	golang.org/x/crypto v0.38.0 // indirect
	golang.org/x/exp v0.0.0-20230425010034-47ecfdc1ba53 // indirect
	golang.org/x/net v0.40.0 // indirect
	golang.org/x/sys v0.33.0 // indirect
	golang.org/x/text v0.25.0 // indirect
	gopkg.in/Knetic/govaluate.v3 v3.0.0 // indirect
	gopkg.in/check.v1 v1.0.0-20200227125254-8fa46927fb4f // indirect
)
