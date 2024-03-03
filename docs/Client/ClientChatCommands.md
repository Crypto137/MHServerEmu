# Client Chat Commands

These commands can be entered in the in-game chat window.

## General

| Name               | Description                                                                           | Commands                                                                | Arguments            |
| ------------------ | ------------------------------------------------------------------------------------- | ----------------------------------------------------------------------- | -------------------- |
| All                | Switches to the "All" chat tab.                                                       | /all                                                                    |                      |
| Broadcast          | Sends a message to the broadcast chat channel.                                        | /broadcast<br/>/b                                                       | text                 |
| ChannelChat        | Sends chat directly to the specified channel.                                         | /c                                                                      | channel, text        |
| ChannelHide        | Leaves the specified chat channel.                                                    | /hide                                                                   | channel              |
| ChannelShow        | Joins the specified chat channel.                                                     | /show                                                                   | channel              |
| ChatBanVote        | Casts a vote to ban a player from social chat                                         | /chatbanvote                                                            | player, reason       |
| Console            | Enables console                                                                       | /console                                                                |                      |
| DebugListObjects   | debug list objects                                                                    | /debuglistobjects                                                       |                      |
| Emote              | Emotes the message to whichever chat channel you're in.                               | /emote<br/>/e<br/>/em                                                   | text                 |
| Endgame            | Sends a message to the Endgame chat channel.                                          | /endgame<br/>/end<br/>/eg                                               | text                 |
| Exit               | Exits the game.                                                                       | /exit<br/>/quit                                                         |                      |
| Faction            | Sends a message to your team chat channel.                                            | /team<br/>/faction<br/>/f<br/>/fc                                       | text                 |
| Fps                | Toggles display of FPS stats.                                                         | /fps                                                                    |                      |
| Friend             | Adds a player to your friends list.                                                   | /friend                                                                 | player               |
| FriendsList        | Lists all friends.                                                                    | /friendslist<br/>/friendlist<br/>/friends                               |                      |
| Guild              | Sends a message to your supergroup's chat channel.                                    | /guild<br/>/g<br/>/gc<br/>/gu<br/>/supergroup<br/>/su<br/>/sg           | text                 |
| GuildDemote        | Demote a supergroup officer to a member, or remove a member from your supergroup.     | /guilddemote<br/>/supergroupdemote<br/>/sudemote<br/>/sgdemote          | player               |
| GuildInvite        | Invite another player to the supergroup.                                              | /guildinvite<br/>/supergroupinvite<br/>/suinvite<br/>/sginvite          | player               |
| GuildKick          | Remove a member from your supergroup.                                                 | /guildkick<br/>/supergroupkick<br/>/sukick<br/>/sgkick                  | player               |
| GuildLeader        | Select a new supergroup leader.                                                       | /guildleader<br/>/supergroupleader<br/>/suleader<br/>/sgleader          | player               |
| GuildLeave         | Leave your supergroup.                                                                | /guildleave<br/>/supergroupleave<br/>/suleave<br/>/sgleave              |                      |
| GuildMotd          | Submit a request to change your supergroup's message of the day.                      | /motd                                                                   | message              |
| GuildOfficer       | Sends a message to your supergroup's officer chat channel.                            | /officer<br/>/o<br/>/guildofficer<br/>/supergroupofficer<br/>/sgofficer | text                 |
| GuildPromote       | Promote a supergroup member to an officer.                                            | /guildpromote<br/>/supergrouppromote<br/>/supromote<br/>/sgpromote      | player               |
| GuildRename        | Submit a request to change your supergroup's name.                                    | /guildrename<br/>/supergrouprename<br/>/surename<br/>/sgrename          | name                 |
| Help               | Displays help on a single command or a list of all available commands.                | /help<br/>/?                                                            | command              |
| Ignore             | Adds a player to your ignore list.                                                    | /ignore                                                                 | player               |
| IgnoreList         | Lists all ignored players.                                                            | /ignorelist                                                             |                      |
| Kick               | Removes a player from your party.                                                     | /kick                                                                   | player               |
| LFG                | Sends a message to the LFG (looking for group) chat channel.                          | /lfg                                                                    | text                 |
| Local              | Sends a message to the default (local) chat channel.                                  | /local<br/>/l<br/>/region                                               | text                 |
| Mission            | Switches to the "Mission" chat tab.                                                   | /mission                                                                |                      |
| Mute               | Toggles all audio off or on.                                                          | /mute                                                                   |                      |
| OpenTrade          | Sends trade invite to a player.                                                       | /opentrade                                                              | player               |
| Party              | Sends a message to your party's chat channel.                                         | /party<br/>/p<br/>/pc                                                   | text                 |
| PartyInvite        | Invites another player to your party.                                                 | /partyinvite<br/>/invite                                                | player               |
| PartyLeave         | Removes your character from your party.                                               | /partyleave<br/>/leave<br/>/leaveparty                                  |                      |
| Ping               | Displays network performance stats.                                                   | /ping                                                                   |                      |
| Promote            | Promotes a player to party leader.                                                    | /promote                                                                | player               |
| Reply              | Sends a message to the last player to privately message you.                          | /reply<br/>/r                                                           |                      |
| Report             | Report a player to customer service                                                   | /report                                                                 | player, reason       |
| Roll               | Simulates a die roll.                                                                 | /roll                                                                   | die to roll          |
| Say                | Says chat in a bubble above your head.                                                | /say<br/>/s<br/>/yell<br/>/y<br/>/shout<br/>/sh                         | text                 |
| SetAudioVolume     | Adjusts the volume of all audio [0 to 10].                                            | /audiovolume                                                            | Volume from 0 to 10. |
| SetMocoVolume      | Adjusts the volume of the motion comics [0 to 10].                                    | /comicvolume<br/>/motioncomicvolume                                     | Volume from 0 to 10. |
| SetMusicVolume     | Adjusts the volume of the background music [0 to 10].                                 | /musicvolume                                                            | Volume from 0 to 10. |
| SetSfxVolume       | Adjusts the volume of the sound effects [0 to 10].                                    | /sfxvolume                                                              | Volume from 0 to 10. |
| SetVoiceoverVolume | Adjusts the volume of character voiced dialogue [0 to 10].                            | /dialoguevolume<br/>/voiceovervolume                                    | Volume from 0 to 10. |
| Social             | Sends a message to the social chat channel.                                           | /social                                                                 | text                 |
| SocialChinese      | Sends a message to the Chinese social chat channel.                                   | /socialch<br/>/social-ch<br/>/socialchinese                             | text                 |
| SocialEnglish      | Sends a message to the English social chat channel.                                   | /socialen<br/>/social-en<br/>/socialenglish                             | text                 |
| SocialFrench       | Sends a message to the French social chat channel.                                    | /socialfr<br/>/social-fr<br/>/socialfrench                              | text                 |
| SocialGerman       | Sends a message to the German social chat channel.                                    | /socialde<br/>/social-de<br/>/socialgerman                              | text                 |
| SocialJapanese     | Sends a message to the Japanese social chat channel.                                  | /socialjp<br/>/social-jp<br/>/socialjapanese                            | text                 |
| SocialKorean       | Sends a message to the Korean social chat channel.                                    | /socialko<br/>/social-ko<br/>/socialkorean                              | text                 |
| SocialPortuguese   | Sends a message to the Portuguese social chat channel.                                | /socialpt<br/>/social-pt<br/>/socialport<br/>/socialportuguese          | text                 |
| SocialRussian      | Sends a message to the Russian social chat channel.                                   | /socialru<br/>/social-ru<br/>/socialrussian                             | text                 |
| SocialSpanish      | Sends a message to the Spanish social chat channel.                                   | /sociales<br/>/social-es<br/>/socialspanish                             | text                 |
| Tell               | Sends a private message to another player.                                            | /tell<br/>/t<br/>/whisper<br/>/w<br/>/send<br/>/pm<br/>/private         | player, text         |
| Time               | Shows the time.                                                                       | /time                                                                   |                      |
| TimePlayed         | Shows the time played for your current hero and the total time played for all heroes. | /played                                                                 |                      |
| Trade              | Sends a message to the trade chat channel.                                            | /trade                                                                  | text                 |
| Unfriend           | Removes a player from your friends list.                                              | /unfriend<br/>/remfriend<br/>/removefriend                              | player               |
| Unignore           | Removes a player from your ignore list.                                               | /unignore                                                               | player               |
| Who                | Shows information about a given player.                                               | /who                                                                    | player               |

## Emotes

| Name           | Description                           | Commands                                                     |
| -------------- | ------------------------------------- | ------------------------------------------------------------ |
| EmoteAgree     | Performs the "Agree" emote            | /agree<br/>/nod<br/>/yes                                     |
| EmoteAttack    | Performs the "Attack" emote           | /attack                                                      |
| EmoteBeckon    | Performs the "Beckon" emote           | /beckon                                                      |
| EmoteBored     | Performs the "Bored" emote            | /bored<br/>/fidget<br/>/impatient                            |
| EmoteBow       | Performs the "Bow" emote              | /bow<br/>/curtsey                                            |
| EmoteCheer     | Performs the "Cheer" emote            | /cheer<br/>/yay<br/>/win                                     |
| EmoteCongrats  | Performs the "Congrats" emote         | /congrats<br/>/congratulations<br/>/grats                    |
| EmoteCry       | Performs the "Cry" emote              | /cry<br/>/sob<br/>/weep<br/>/depressed<br/>/sad              |
| EmoteDance     | Performs the "Dance" emote            | /dance<br/>/boogie<br/>/getdown                              |
| EmoteLaugh     | Performs the "Laugh" emote            | /laugh<br/>/lol<br/>/haha<br/>/rofl<br/>/lmao                |
| EmoteListen    | Performs the "Listen" emote           | /listen                                                      |
| EmoteNo        | Performs the "No" emote               | /no                                                          |
| EmotePoint     | Performs the "Point" emote            | /point                                                       |
| EmotePose      | Performs the "Pose" emote             | /pose                                                        |
| EmoteRetreat   | Performs the "Retreat" emote          | /retreat                                                     |
| EmoteSalute    | Performs the "Salute" emote           | /salute                                                      |
| EmoteShowOff   | Performs the "Show Off" emote         | /showoff                                                     |
| EmoteVolunteer | Performs the "Volunteer" emote        | /volunteer<br/>/here<br/>/me                                 |
| EmoteWait      | Performs the "Wait" emote             | /wait                                                        |
| EmoteWave      | Performs the "Friendly Gesture" emote | /wave<br/>/hello<br/>/hi<br/>/bye<br/>/farewell<br/>/goodbye |
| EmoteYawn      | Performs the "Yawn" emote             | /yawn<br/>/sleepy<br/>/zzz                                   |
