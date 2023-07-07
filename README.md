# SocuciusErgallaBotv3

SocuciusErgallaBotv3 is a Discord music bot based on the character Socucius Ergalla from Morrowind. It provides functions for playing music in a channel with your friends.

## Features

- Slash commands for music control, organized under the Music category.
- Play music from various sources using a URL or search query.
- Play music with start and/or end time.
- Play a random song from previously played music.
- Pause the current playback.
- Stop playing and leave the voice channel.
- Skip to the next song in the queue (If shuffle mode is set to "endless," it will queue a random song from history).
- Seek to a specific position in the current song.
- Adjust the volume for all users.
- Set repeat mode to none, single, or all.
- Set shuffle mode to none, playlist, or endless.
- Remove a song from the queue by index.
- Display the current queue.
- Show the status of the player, queue, shuffle and repeat modes, current song position, top 5 songs, and top 5 users.

## Dependencies

- This bot utilizes the [DSharpPlus](https://github.com/DSharpPlus/DSharpPlus) library for Discord API integration.
- Audio streaming is handled by [Lavalink](https://github.com/Frederikam/Lavalink). Refer to the Lavalink GitHub repository for help setting up Lavalink.

## Setup

On first run, the bot will create a folder named "Resources" in its working directory. This folder will contain config.json with all of the required Lavalink and Discord configuration options. This includes Lavalink server details and Discord API token. The bot will also create a database within the "Resources" folder to store previously played music.  Once the configuration is set up, run the bot again to start the music playback functionality.

## Slash Commands

Use the following slash commands under the Music category to control the music playback:

- `/music play <url or query>`: Play a song from various sources using a URL or search query.
- `/music play <url or query with start and/or end time>`: Play a song with specified start and/or end time.
- `/music play`: Play a random song from previously played music.
- `/music pause`: Pause the current playback.
- `/music stop`: Stop playing and leave the voice channel.
- `/music skip`: Skip to the next song in the queue (If shuffle mode is set to "endless," it will queue a random song from history).
- `/music seek`: Seek to a specific position in the current song.
- `/music volume`: Adjust the volume for all users.
- `/music repeat`: Set repeat mode to none, single, or all.
- `/music shuffle`: Set shuffle mode to none, playlist, or endless.
- `/music remove`: Remove a song from the queue by index.
- `/music queue`: Display the current queue.
- `/music status`: Show the status of the player, queue, shuffle and repeat modes, current song position, top 5 songs, and top 5 users.

## License

This project is licensed under the MIT License.