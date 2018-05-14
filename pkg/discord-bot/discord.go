package discord

import (
	"os"
	"strings"

	log "github.com/sirupsen/logrus"
	"github.com/bwmarrin/discordgo"
)

const ResponsePrefix = "!hugot"
var Handlers = []func(*discordgo.Session, *discordgo.MessageCreate){}

type DiscordBot struct {
	dg *discordgo.Session 
}

func New() *DiscordBot {
	dg, err := discordgo.New("Bot " + os.Getenv("DISCORD_TOKEN"))
	if err != nil {
		log.Fatalf("%v", err)
	}

	for i, handler := range Handlers {
		log.Infof("Registering Handler: %d", i)
		dg.AddHandler(handler)
	}

	return &DiscordBot{
		dg: dg,
	}
}

// Silly helper function for modules to determine if this is meant for our bot.
func ParseMessage(m *discordgo.MessageCreate) (bool, []string) {
	args := strings.Split(m.Content, " ")
	if args[0] != ResponsePrefix {
		return false, args
	}

	return true, args
}

func AddHandler(handler func(*discordgo.Session, *discordgo.MessageCreate)) {
	Handlers = append(Handlers, handler)
}

func (d *DiscordBot) Run(c chan os.Signal) {
	err := d.dg.Open()
	if err != nil {
		log.Fatalf("%v", err)
	}

	// Wait here until CTRL-C or other term signal is received.
	log.Info("Bot is now running.  Press CTRL-C to exit.")

	<-c
	
	// Cleanly close down the Discord session.
	d.dg.Close()
}