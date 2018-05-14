package ivy

import (
	bot "github.com/chronojam/discord-bot/pkg/discord-bot"
	"github.com/bwmarrin/discordgo"
	ivy "robpike.io/ivy/mobile"

	"strings"
	"fmt"
)


func init() {
	bot.AddHandler(messageCreate)
}

func messageCreate(s *discordgo.Session, m *discordgo.MessageCreate) {	
	ok, args := bot.ParseMessage(m)
	if !ok {
		return
	}

	if args[1] == "ivy" {
		fmt.Println(args[2:])
		out, err := ivy.Eval(strings.Join(args[2:], " ") + "\n")
		if err != nil {
			s.ChannelMessageSend(m.ChannelID, fmt.Sprintf("%v", err))
		}

		s.ChannelMessageSend(m.ChannelID, out)
	}


}