package main

import (
	"os"
	"fmt"
	"os/signal"
	"syscall"
	"github.com/bwmarrin/discordgo"
)

func main() {
	dg, err := discordgo.New("Bot " + "NDQ1Njg1NTYxMTY5MDE4ODkx.DduJlA.Z1JKt0A_mo_qZKevw-2bMbm3wmU")
	if err != nil {
		panic(err)
	}

	dg.AddHandler(messageCreate)

	err = dg.Open()
	if err != nil {
		panic(err)
	}

		// Wait here until CTRL-C or other term signal is received.
		fmt.Println("Bot is now running.  Press CTRL-C to exit.")
		sc := make(chan os.Signal, 1)
		signal.Notify(sc, syscall.SIGINT, syscall.SIGTERM, os.Interrupt, os.Kill)
		<-sc
	
		// Cleanly close down the Discord session.
		dg.Close()
}

// This function will be called (due to AddHandler above) every time a new
// message is created on any channel that the autenticated bot has access to.
func messageCreate(s *discordgo.Session, m *discordgo.MessageCreate) {
	
		// Ignore all messages created by the bot itself
		// This isn't required in this specific example but it's a good practice.
		if m.Author.ID == s.State.User.ID {
			return
		}
		// If the message is "ping" reply with "Pong!"
		if m.Content == "ping" {
			s.ChannelMessageSend(m.ChannelID, "Pong!")
		}
	
		// If the message is "pong" reply with "Ping!"
		if m.Content == "pong" {
			s.ChannelMessageSend(m.ChannelID, "Ping!")
		}
	}