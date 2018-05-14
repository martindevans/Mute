package ping

import (
	bot "github.com/chronojam/discord-bot/pkg/discord-bot"
	"github.com/bwmarrin/discordgo"
)


func init() {
	bot.AddHandler(messageCreate)
}

// This function will be called (due to AddHandler above) every time a new
// message is created on any channel that the autenticated bot has access to.
func messageCreate(s *discordgo.Session, m *discordgo.MessageCreate) {	
	ok, args := bot.ParseMessage(m)
	if !ok {
		return
	}
	// Ignore all messages created by the bot itself
	// This isn't required in this specific example but it's a good practice.
	if m.Author.ID == s.State.User.ID {
		return
	}
	// If the message is "ping" reply with "Pong!"
	if args[1] == "ping" {
		s.ChannelMessageSend(m.ChannelID, "Pong!")
	}
	
	// If the message is "pong" reply with "Ping!"
	if args[1] == "pong" {
		s.ChannelMessageSend(m.ChannelID, "Ping!")
	}
}