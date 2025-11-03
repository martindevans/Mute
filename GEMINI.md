# Mute.Moe

This is a C# project for a Discord bot named Mute.Moe.

## Project Overview

Mute.Moe is a feature-rich Discord bot built with .NET and C#. It uses the Discord.Net library for interacting with the Discord API. The bot has a wide range of features, including:

*   **Large Language Model (LLM) Integration:** The bot uses the `LlmTornado` library to interact with large language models, enabling features like conversational AI.
*   **Speech-to-Text:** It uses `Whisper.Runtime` for speech-to-text functionality.
*   **Image Generation:** The bot can generate images, likely using a service like Automatic1111.
*   **Audio Processing:** It has features for playing audio in voice channels.
*   **Information Retrieval:** The bot can fetch information from various sources, including anime databases, cryptocurrency markets, stock exchanges, and weather APIs.
*   **Database:** It uses SQLite for data storage, managed through a custom `IDatabaseService`.
*   **Modular Architecture:** The bot is built with a modular architecture, with features organized into separate services and modules.

## Building and Running

To build and run the project, you will need the .NET SDK.

### Building

```
dotnet build
```

### Running

The bot requires a `config.json` file for configuration. A `config.json` file is not checked into source control, so you will need to create one. Based on the `.gitignore` file, the `config.json` file should be placed in the `Mute.Moe` directory.

Once the configuration file is in place, you can run the bot with the following command:

```
dotnet run --project Mute.Moe/Mute.Moe.csproj -- "Mute.Moe/config.json"
```

## Development Conventions

*   **Dependency Injection:** The project uses dependency injection extensively, with services configured in the `Startup.cs` file.
*   **Async/Await:** The codebase makes heavy use of `async/await` for asynchronous operations.
*   **C# 12:** The project uses modern C# features, including C# 12.
*   **Testing:** The solution includes a test project (`Mute.Tests`), indicating that unit testing is part of the development process.
