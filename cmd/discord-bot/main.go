package main

import (
	"syscall"
	"os"
	"os/signal"
	discord "github.com/chronojam/discord-bot/pkg/discord-bot"

	// Addons
	_ "github.com/chronojam/discord-bot/pkg/command/ping"
	_ "github.com/chronojam/discord-bot/pkg/command/ivy"
)

func main() {
	ds := discord.New()

	sc := make(chan os.Signal, 1)
	signal.Notify(sc, syscall.SIGINT, syscall.SIGTERM, os.Interrupt, os.Kill)
	ds.Run(sc)
}