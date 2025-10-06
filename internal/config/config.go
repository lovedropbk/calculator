package config

import (
	"fmt"
	"os"
)

type Config struct {
	Port          string
	EnableCORS    bool
	LogJSON       bool
	AllowInsecure bool
}

func FromEnv() Config {
	port := os.Getenv("FC_SVC_PORT")
	if port == "" {
		port = "8123"
	}
	return Config{
		Port:          port,
		EnableCORS:    os.Getenv("FC_SVC_CORS") != "0",
		LogJSON:       os.Getenv("FC_SVC_LOG_JSON") == "1",
		AllowInsecure: os.Getenv("FC_SVC_ALLOW_INSECURE") == "1",
	}
}

func (c Config) ListenAddr() string { return fmt.Sprintf(":%s", c.Port) }
