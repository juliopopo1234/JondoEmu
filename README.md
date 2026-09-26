High-performance server emulator for **Dofus 3 Unity (Client 3.6.10.11)** written in C# (**.NET 10**), with decoupled modular projects, a SQLite data layer, a combat engine driven entirely by client data — PvM, duels and Koliseo — a cross-platform launcher and a world editor.

> ⚠️ **Runs against Dofus 3 clients 3.6.10.11 and 3.6.10.10.** The live game is on 3.6.11.12, so the
> official launcher does not provide a client that works here — [Step 2](#step-2--get-the-361011-client)
> has a download. Ankama renames every protobuf message on some patches; the toolchain for that is in
> [Surviving the next patch](#-surviving-the-next-patch).

---

## 📑 Contents

| 🖥️ [Launcher](#-launcher) | 🧩 [Server](#-server) | 🛠️ [Jondo Studio](#-jondo-studio) |
|:---|:---|:---|
| The player's window, in Avalonia. A team of up to eight accounts, each with its character drawn from the client's own bones. | The emulator itself. Four listeners in one process, one session per socket, and guards that refuse to boot on bad data. | The world editor. Nine sections over the client's data, writing a reviewable diff instead of a 240 MB binary. |

&nbsp;

> **New here?** [Quick Start](#-quick-start) puts you in the game in four steps ·
> [What you get](#-what-you-get) is what lands on disk

&nbsp;

- 🌍 &nbsp;**World** &nbsp;— &nbsp;[Connection and authentication](#-connection-and-authentication) · [World and movement](#-world-and-movement) · [Travel](#-travel) · [Houses, bins and haven bags](#-houses-bins-and-haven-bags) · [Social](#-social) · [Guilds and raids](#-guilds-and-raids)

- 🎒 &nbsp;**Character** &nbsp;— &nbsp;[Character and inventory](#-character-and-inventory) · [Appearances](#-appearances) · [Professions](#-professions)

- 📚 &nbsp;**Content** &nbsp;— &nbsp;[NPCs and monsters](#-npcs-and-monsters) · [Quests](#-quests) · [Dungeons](#-dungeons) · [Infinite Dreams](#-infinite-dreams) · [Jondo Coin](#-jondo-coin)

- ⚔️ &nbsp;**Combat** &nbsp;— &nbsp;[One engine, four rulebooks](#-one-engine-four-rulebooks) · [PvM](#-pvm-combat) · [Duels](#-duels) · [Koliseo](#-koliseo) · [Spell effect engine](#-spell-effect-engine) · [Spell check-list](#-spell-check-list) · [Combat challenges](#-combat-challenges) · [Not implemented](#-not-implemented-at-all)

- 🔎 &nbsp;**Tools** &nbsp;— &nbsp;[Jondo Studio](#-jondo-studio) · [Surviving the next patch](#-surviving-the-next-patch)

- 🧱 &nbsp;**Under the hood** &nbsp;— &nbsp;[Tests](#-tests) · [Source layout](#-source-layout) · [Database and persistence](#-database-and-persistence)

---

## 🚀 Quick Start

**Nothing has to be compiled.** The launcher ships as a single ready-to-run executable with every dependency inside it, and the world database ships compressed and extracts itself on first run.

### Step 1 — Install the .NET 10 runtime

Download it from [dotnet.microsoft.com](https://dotnet.microsoft.com/download/dotnet/10.0). The *Desktop Runtime* is the one you want.

### Step 2 — Get the 3.6.10.11 client

The live game is on 3.6.11.12 and Ankama's launcher only provides the current version, which this
emulator does not support.

**⬇️ [Dofus 3.6.10.11 — download](https://www.swisstransfer.com/dl/01a082ed-3e6a-70b9-987b-7f2551484389)**

It is the stock Ankama client. Unpack it as a **`Cliente 3.6.10.11`** folder beside the emulator
folder, which is where the launcher looks first; anywhere else works if you point **Settings** at
your `Dofus.exe`.

A 3.6.10.11 or 3.6.10.10 install from before the patch also works — keep the official launcher
from updating it.

### Step 3 — Point the Dofus client at the emulator

The official client talks to Ankama's servers and checks their SSL certificates. **JondoFix**, a MelonLoader mod, redirects it to your machine instead. It comes already built in this repository.

1. Get **MelonLoader 0.7.x** from [its releases page](https://github.com/LavaGang/MelonLoader/releases). 0.7.x is published as *Open-Beta*, so it shows up as a **pre-release** and the page's "Latest" tag points at 0.6.x. **0.6.x does not work with this client** — tick *show pre-releases* and take 0.7.x. This repository is tested against **0.7.3**.
2. Run the installer and point it at your **`Dofus.exe`**. MelonLoader detects the rest (`Game Type: Il2cpp`, `Game Arch: x64`, `Runtime Type: net6`, Unity `6000.3.16f1`).
3. Copy **`JondoFix/JondoFix.dll`** from this repository into the **`Mods/`** folder of your Dofus installation, next to `Dofus.exe`. MelonLoader creates that folder the first time the game starts; if it is not there yet, create it yourself.

> The mod ships **already compiled**; `JondoFix/` also carries its source.

Afterwards:
* The installer drops a **`version.dll`** next to `Dofus.exe`, which loads MelonLoader. Renaming it to `version.dll.disabled` turns the whole thing off so you can play the official game; renaming it back turns it on again.
* MelonLoader writes a log per run under **`MelonLoader/Logs/`**. If the client starts but never reaches the emulator, look there first.

What JondoFix does: intercepts sockets, Named Pipes and DNS queries and sends them to `localhost` (ports `8888`, `5555`, `15881`, `6337`); stops HTTPS requests from failing against the local self-signed certificate; and injects the environment variables the client expects (`ZAAP_PORT`, `ZAAP_HASH`, and so on).

### Step 4 — Run it

Double-click **`Jondo Emulator Launcher.exe`**. It launches **`Jondo Server.exe`** itself, in its own window with the log and the counters.

On the first run it unpacks `datos/world.zip` into `bases/world.db` (about 240 MB) and creates `bases/auth.db` with a test account. Sign in to add an account to the launcher's team, tick one or several saved profiles, then press **Launch selected**. Up to eight independent Dofus clients can be active at once.

```
Account: keka
Password: test
```

By default the emulator looks for the client next to itself, in a `Cliente 3.6.10.11` folder beside the emulator folder — or `Cliente 3.6.10.10`, whichever it finds first. If yours lives somewhere else, set it in **Settings**. The choice is remembered.

The **ES / EN / FR** switch sets the language of the launcher *and* of the game: the client is started with that `--langCode`.

**`Jondo Studio.exe`** is the third executable and needs nothing else running. See [Jondo Studio](#-jondo-studio).

---

## 📂 What you get

```
Jondo Emulator Launcher.exe   ← this is what you run
Jondo Server.exe              the server; the launcher starts it
Jondo Studio.exe              the world editor
content/                      the only files edited by hand, versioned in git
datos/                        json and bin the emulator reads (maps, items, appearances, zaaps…)
bases/                        writable databases and backups
docs/                         technical documentation
launcher_assets/              launcher artwork and music
JondoFix/                     the MelonLoader mod, source and compiled dll
Jondo.Unity.*/                source code
```

Player and administrator actions are written as one JSON object per line in `logs/activity.jsonl`: commands, equipment moves, lottery prizes, granted items, fights, live administration and new unhandled packet shapes. Credentials, launcher tokens and game tickets are never included.

---

## ✅ Emulation status

✅ done · 🟡 partial · 🚧 in progress · ❌ missing

### 🖥️ Launcher
<img width="2560" height="1512" alt="image" src="https://github.com/user-attachments/assets/68e0e721-b36c-4524-b5d6-660fd5beb3c0" />
<img width="2560" height="1504" alt="image" src="https://github.com/user-attachments/assets/86f835e0-f161-4f51-af42-a810a192f150" />

Built with **Avalonia**, the same toolkit as the Studio.

- ✅ Three screens — *Play*, *Accounts*, *Settings* — with the server-status pill in the header
- ✅ Account cards with the character drawn in them — portrait, name and level. The portrait is assembled from the client's own bones, the same way Jondo Studio draws NPCs; no character image ships inside the executable
- ✅ The portrait shows the character as they look in the world: chosen head, real equipment and the cosmetics over it
- ✅ Persistent team of up to 8 accounts, one independent Dofus process each; the highest-level character of each account is the one shown
- ✅ Account creation and login, written to `auth.db`; credentials sealed with DPAPI
- ✅ Per-client identity chain — instance id, launch hash, Zaap session, game token, single-use ticket, socket-owned session
- ✅ Independent lifecycle indicators for profiles, processes and sockets
- ✅ Embedded server log; single-file deployment; ES/EN/FR
- ✅ HD and 4K scenery packs from Ankama's own CDN, for the client's own version only: resumable download, every chunk and file checked against its SHA-1, verify and remove, and `--hdReady` / `--4kReady` passed only for a verified pack (`docs/client-graphics.md`)
- ✅ Animated neon sign and falling stars
- ✅ Launcher and server are separate programs — the launcher carries no database, maps, handlers or effect catalogue
- 🚧 OAuth — loopback redirect and PKCE on the launcher side; the server half waits for a website

### 🧩 Server
<img width="2558" height="1508" alt="image" src="https://github.com/user-attachments/assets/df3cce87-166d-4f5a-8aff-a4fcd2575c87" />

`Jondo Server.exe`. The launcher starts it, but it can be run on its own — or on another machine.

- ✅ Four listeners in one process — Zaap (`8888`), game (`5555`), chat (`6337`) and HAAPI
  (`15881`), plus a self-signed certificate for the client's HTTPS
- ✅ One session per socket: every handler reads the session it is serving, so eight clients on one
  machine never see each other's state
- ✅ Its own window with the live log, the counters and the connected clients
- ✅ Regression guards that run at boot and refuse to start when the shipped data does not match
  what the code expects — see [Tests](#-tests)
- ✅ A loopback control API the launcher talks to: log tail, account login, and the characters of an
  account with the look already composed for drawing
- ✅ Runs on another machine: every listener honours `JONDO_PUBLIC_BIND`, and the launcher runs a
  loopback relay so the client reaches it (HAAPI and the chat server hand the client `127.0.0.1`)
- ✅ Unanswerable packets are recorded in their own database, deduplicated by protobuf shape

### 🔐 Connection and authentication

- ✅ Zaap, HAAPI and connection server emulation, VIP check bypassed
- ✅ Account creation and login against `auth.db`, with the password hashed and the attempt rate
  limited by the socket's IP
- ✅ Per-client identity chain — instance id, launch hash, Zaap session, game token, single-use
  ticket, socket-owned session
- ✅ Server and character selection, showing the mount being ridden and each character's equipment
<img width="2560" height="1500" alt="image" src="https://github.com/user-attachments/assets/c4c194ad-dcd1-407f-a3f1-b44c8f4baed2" />
<img width="2558" height="1504" alt="image" src="https://github.com/user-attachments/assets/70d02ad2-8fc0-4ec8-b836-1dda959ed271" />

- ✅ Character creation with a starter kit — Astrub zaap, adventurer set, 1,000,000 kamas, 100
  scrolled points per characteristic
<img width="2560" height="1504" alt="image" src="https://github.com/user-attachments/assets/881c9530-6631-46ed-b85e-c7fd92602455" />
<img width="2560" height="1502" alt="image" src="https://github.com/user-attachments/assets/09a94e1f-165a-407d-91f1-1dd7b18063de" />
<img width="2558" height="1510" alt="image" src="https://github.com/user-attachments/assets/a65407d0-7e65-4481-bdfe-ea65554bb29e" />

- ✅ Account roles, and an administrator-only channel over loopback
- ✅ Reconnecting into a fight. Close the client mid-fight and the fight goes on without you; log
  back in and the character still fighting is picked without a selection screen and put back on the
  board as it stands — the in-progress `kaa`, every fighter, the live buffs, and the current turn
  with the time it has left. Picking a character the ordinary way while it has a fight pending
  counts as a surrender, and the character enters the world on the roleplay map it left

### 🗺️ World and movement
- ✅ World loading, spawn, name hover, last cell and map persisted. Multiclient.
<img width="2560" height="1506" alt="image" src="https://github.com/user-attachments/assets/8882de29-36b0-4af9-be22-2d5f3bd4c6d4" />

- ✅ **15,360 maps**, **17,211** with walkable-cell data, **17,222** with combat cells
- ✅ Movement, map change and adjacent maps; auto-pilot from the minimap and *travel to*
<img width="538" height="452" alt="image" src="https://github.com/user-attachments/assets/a6438938-00c2-4a76-b4e1-48abf3d56934" />

- ✅ Seeing others arrive and leave, in all four directions: whoever walks off the map disappears from the others' screens (`kmu`), with the `jsd` before it for their party, as the captures send it
- ✅ Up to 8 clients at once, each on its own socket-owned session
- ✅ Everybody is drawn wearing their gear — the other players on the map, the opponent in a fight and every character on the selection screen. Equipment is read per character from `CharacterItems`

### 🌀 Travel

- ✅ **62 waypoints** with map, cell and sub-area, plus 3 departure-only zaaps the waypoint table omits
- ✅ Travel between zaaps with the real cost and destination list
<img width="2560" height="1502" alt="image" src="https://github.com/user-attachments/assets/1524d485-a845-4b62-a71c-b88de3bb7b54" />

- ✅ Discovered zaaps announced on world entry (`hjk`)
- ✅ Zaapis of Bonta (24) and Brakmar (21) at a flat 20 kamas
<img width="2560" height="1484" alt="image" src="https://github.com/user-attachments/assets/472e09f2-a49d-431e-8935-f60355457cdd" />

- ✅ The right window per list: `hjj` root field 0 zaap, 1 zaapi, 3 boat
- ✅ **16 temporal anomalies** with their 120-minute countdown, surfacing at vestiges (type 359)
<img width="2560" height="1514" alt="image" src="https://github.com/user-attachments/assets/942d4d71-9711-45f1-9156-5381f7ad14b8" />

- ✅ **3,815 interactive teleports** imported, 3,719 active across 2,655 maps
- ✅ Passages that fire when you step on the cell, hooked to the end of a walk
- ✅ Each route carries its own interactive type
- 🟡 Every extracted passage still declares skill 114 (*Utilizar* on a zaap) where the game uses
  184; new passages written in Jondo Studio declare 184
- ✅ New passages can be created, both ways, from Jondo Studio
<img width="2560" height="1506" alt="image" src="https://github.com/user-attachments/assets/e3908060-2ad1-415c-a11a-cb6f323b9378" />

### 🏘️ Houses, bins and haven bags

- ✅ **1,437 doors on 553 maps**, all enterable and ownerless; **261 house models** with name, price and room count
<img width="1112" height="920" alt="image" src="https://github.com/user-attachments/assets/1506283c-f6cd-45b5-b9c4-f345273f67bb" />

- ✅ Entering and leaving (`jqw` in, `jru` out), coming out through the door you went in by
- ❌ The house plaque, chest, access code, buying and selling
- ✅ **67 public bins on 63 maps** — they open, show empty and close
- ❌ Putting items into a bin and taking them out
- ✅ Haven bags: entering and leaving, their own zaap, **48 themes**, **4,083 furniture pieces** placed and persisted, chest with the full item flow, lottery machine, and no monsters inside
<img width="2560" height="1492" alt="image" src="https://github.com/user-attachments/assets/a81a3b24-8559-4ad5-8a27-e6913eef95a8" />

> Which house sits behind which door is not in the client data. The 1,437 doors share **114 genuine interiors**, assigned deterministically within their own neighbourhood; the mapping lives in `datos/casas_mundo_3.6.10.10.json` and can be corrected by hand.

### 💬 Social

- ✅ Information messages as `lqn { type, message, parameters }` against the client's 2,555-entry table
- ✅ Level-up window with music and animation, on a real gain and on `.level` in either direction
<img width="2560" height="1514" alt="image" src="https://github.com/user-attachments/assets/490997fc-1300-4a29-9963-32077efdf0dd" />

- ✅ Private messages (`kth`)
- ✅ Last connection time and IP, stored per character
- ✅ Parties — invite, accept, refuse, leave, hand over the lead, kick, and a full member sheet
- ✅ Lead passes on when the leader leaves; a disconnect removes the member and tells the rest
- ✅ Friends list
- ✅ Trading with another player on the map: ask, refuse or accept, lay stacks down and take them back, kamas, ready on both sides, and the goods changing hands — a new stack under a new uid, or onto one of the same — measured on both sides in the two trade captures
- ✅ Every command answers in the session's own language, from a catalogue in Spanish, English and French. The language comes from the `--langCode` the launcher started the client with
- ✅ Following the leader: the member's client walks after him map by map on each `ikv` the server sends it, as the follow capture measures; a zaap cuts the follow, as on the real server
- ❌ The invitation popup's *Details* button (`imd` → `ilb`), the dedicated member-gone message (`inc`) and party search

### ⚜️ Guilds and raids

**Guilds**

- ✅ Founding: the altar of the Guild Temple (element 480310 on map 106169344, [0,−8] north of the
  Amakna village) opens the client's own editor; the `jjg` it sends back spends the guildalogem
  (item 1575), and the founder is redrawn with the guild under his name. `.gremio crear <nombre>`
  founds through the same path with a fixed emblem
- ✅ Gremia, outside the temple, sells the guildalogem for one kama, the book *Acerca de los gremios*
  for 500 and the guild shield for 100,000 (`content/npcs/shops.json`)
- ✅ The guild block travels in every map actor (`f5 { f4 { emblem, id, name, level } }`), so a
  guilded character shows the guild under their name to everyone
- ✅ The guild window: header (`jhh`), ranks (`jco`), member list (`jgu`) with class, level,
  achievement points, gremichas, online state and the leader's note; the guild comes with you into
  the world on login, rebuilt from the database
- ✅ Leave from the window (`jho`) or with `.gremio salir`; kick with `.gremio expulsar`
- ✅ Ranks — open, rename, set rights, create (`jcs`, `jct`, `jck`, `jcv`), each answered with the
  whole `jco`. Rights are stored as they arrive. `.gremio rango <personaje> <n>` assigns one
- ✅ Member notes (`jjj` → `jgz`), the guild log (`jim` → `jil`: founding and every join), the
  directory profile the leader writes (`jcc` → `jci`: description, level range, tags and title) and
  the directory search (`jjm` → `jme` + `jiv`: every guild with its leader, size and emblem)
- ✅ Applications and invitations both ways — apply, list, read one, accept
- ✅ Contributions — 10,000 kamas buy 10 guild kamas, five a week, the week turning on Tuesday
- ✅ The oracle shop, five oracles, priced by how many accounts the guild has
- ❌ The client's own requests for applying, inviting, kicking, assigning a rank and buying a raid
  are not handled; `.gremio` and `.raid` stand in
- ❌ The guild chest, the *Encargos* and *Casas* tabs

**Raids** — the Gigalodón Abyss and the Eternal Gardens Sanctuary — are bought with guild kamas
(360 and 480), launched by a captain and run against a clock: an hour the first, two the second.

- ✅ The instance carries the raid's named variables, `Raid_Score` and `n1..n5_worldlight`, which
  the content the client ships reads through its own criteria
- ✅ A criterion evaluator over the client's criterion language — `&`, `|`, parentheses — with a
  tri-state answer, so an unknown term is not read as false
- ✅ Monster aggression follows the monsters' own criteria in `world.db`: the Abyss monsters are
  immune while their floor has light
- ✅ The clock returns everyone to the map and cell they came from, and the captain can close the
  raid early
- ✅ Raid loot from the monsters' global loot table: depths salt at 30% (100% from the three floor
  guardians) and the seven gems, each monster with its own rates. A global loot row whose criterion
  cannot be evaluated does not drop
- ✅ The luminomachine, NPC 8007, one on each of the five lit floors: it offers the light bands the
  player can pay for, takes the salt and raises that floor's `nX_worldlight`. One more band costs 1,
  3, 6 and 10 salt; a jump pays the sum
- ✅ The chest at the far end, NPC 7861, with its two screens: drop every treasure in, or take it
  and end the raid for the whole team. Anyone may take it
- ✅ A treasure is any item carrying effect 4063, *Valor de un objeto*: the gems from Quartz at 2 to
  Ónix at 30, the three guardians' trophies at 1000, 5000 and 10000, and the salt at 1
- ✅ The chest fills up as the score rises, through the five looks of its template (5000, 13000,
  27000 and 45000 points). NPC templates with several looks and a criterion each pick the right one
  per player
- ✅ The weekly ladder, per raid, keeping each guild's best run of the week, ties broken by who got
  there first; `.raid clasificacion` prints it with the podium ornament of each place
- ❌ The podium ornaments are named, not granted: the wardrobe offers all 167 to everybody
- ❌ The raid panel — timer, score and light on screen; `.raid` prints them instead
- ❌ The Gigalodón fight when the clock beats you to the chest; the clock closes the raid
- ❌ The entry map and the positions of machines and chests are not taken from captures: the lowest
  map of each floor and the walkable cell nearest the middle are used

### 🎒 Character and inventory

- ✅ **21,748 item templates** and **66,294 item effects** — spawning, equipping, bags, destruction, persistence
- ✅ **929 item sets** with their bonuses
- ✅ **520 mounts** with their look, swapped and unequipped correctly
<img width="2560" height="1500" alt="image" src="https://github.com/user-attachments/assets/375da573-ab61-4bf0-83fd-6f2a8f872cde" />
<img width="2560" height="1506" alt="image" src="https://github.com/user-attachments/assets/581b105c-9569-4f54-ab77-01e122b8ce06" />

- ✅ Characteristic assignment, dynamic capital, points in sync across every client panel
- ✅ Scrolled characteristics kept apart from spent points: every character starts with 100 in each
  of the six, sent in the field the client draws as *Adicional*, and the capital counts only the
  points the player spent
<img width="708" height="1048" alt="image" src="https://github.com/user-attachments/assets/b07f0ac2-f701-4f3a-82e2-c04f884d696d" />

- ✅ **17,113 spells** across **34,823 spell levels**; **638 character heads**
<img width="2560" height="1508" alt="image" src="https://github.com/user-attachments/assets/02b5e575-20e3-47ed-b095-55443fb792ab" />

- ✅ **539 titles** and **167 ornaments**, applied, persisted and carried in the map actor block
<img width="2560" height="1498" alt="image" src="https://github.com/user-attachments/assets/0e579800-2776-4aba-8629-58bb2e6c7acf" />
<img width="2560" height="1502" alt="image" src="https://github.com/user-attachments/assets/25225e6b-2b6e-4aa6-8f56-12937b0754a0" />

- ✅ Life regeneration, run by the client and switched by the server: started on every return to a
  roleplay map (`ktz`) and stopped on the way into a fight (`kuq`), so a fight starts on the life
  the ticks earned
- ✅ Commands — `.teleport [x,y]` or `.teleport <map id>`, `.kamas`, `.shop`, `.size`, `.level`, `.item`, `.itemset`, `.receta`, `.sueno`, `.gremio`, `.raid`; they answer with an information line only their author sees
- ✅ Live administration over HTTP — `POST /api/personaje` sets characteristics, kamas and level, grants items or a mount, and teleports a connected character without a reconnect. `POST /api/rol` changes account roles. Administrator only, loopback only
- 🟡 `.level` repaints the in-fight spell bar, but the fighter's own level is not updated until the next fight

### 👕 Appearances
<img width="2560" height="1508" alt="image" src="https://github.com/user-attachments/assets/30ee645b-191b-4146-9966-d2c3fb72a9cf" />
<img width="2560" height="1500" alt="image" src="https://github.com/user-attachments/assets/2655dfd1-d565-484c-9735-54dd00f4f8b0" />

Dofus does not ship the item-to-look table: the server sends it. **2,371 of the 2,420 cosmetics** in the catalogue are covered.

| Type | Working / catalogue | | Type | Working / catalogue |
|---|---:|---|---|---:|
| Shields | 524 / 524 | | Petmounts | 151 / 151 |
| Hats | 464 / 464 | | Mounts | 121 / 121 |
| Capes | 357 / 357 | | Shoulders | 121 / 121 |
| Pets | 242 / 242 | | Costumes | 92 / 92 |
| Weapons | 194 / 194 | | Living objects | 61 / 61 |
| Wings | 44 / 44 | | Miscellaneous | 0 / 49 |

- ✅ Appearance weapons carry no look — the client draws them; the server remembers which of the 10 weapon slots each occupies
- ✅ Living objects imitate a different garment per variant, stored as **543 object/variant pairs** across 10 slots
- ✅ Mount and pet appearances are mutually exclusive
- ✅ The real equipment renders too, and a cosmetic replaces it rather than stacking on top: **741 real items** carry their own skin into the look
- ✅ The same skin list feeds the launcher's portraits
- 🟡 82 skins were inferred by image matching and are held back at load until verified
- 🟡 A second look path survives in `InventoryHandler` for four items
- ❌ Per-character colours: every look is composed from the breed's default palette

### ⛏️ Professions

- ✅ **25,090 resources on 4,507 maps** across the six gathering jobs
- ✅ The three states — full, depleted, busy
- ✅ Job levels and experience persisted, with the curve `10 × level × (level − 1)`

- ✅ What you gather lands in the inventory, and the amount grows with job level
- ✅ Too low a job level blocks gathering
- ✅ **577 workshop stations** on the world's maps, recognised by their graphic: 21 declared in the captures' `jss`, 2 seen used, 15 from PR #44's Incarnam captures and 16 found inside the workshops of a one-skill job (tailor, shoemaker, sculptor, smith, jeweller, handyman, hunter, fisherman...)
- ✅ The craft window of every job with any of the **4,858 recipes**: pick a recipe or lay the ingredients by hand, craft one or many, the job's level asked for
- ✅ Crafted equipment rolls each characteristic in its own range; what rolls nothing joins a stack
<img width="2488" height="1396" alt="image" src="https://github.com/user-attachments/assets/9651bea6-a649-4154-9389-f30c1da7ea58" />

- ✅ Craft experience `⌊20 · recipe level / (1 + 0.1 · gap^1.1)⌋` — the tutorial's +20 — and the level-up window (`isz`), now for gathering too
- ✅ Smithmagic on the six magus tables: clean success, partial success (it enters and costs weight elsewhere) and failure, with the client's own rune weights, the pool, over and exo up to a weight of 101, exo AP/MP/range at 1%
<img width="2560" height="1508" alt="image" src="https://github.com/user-attachments/assets/b4b5f143-fe17-4e42-b656-407480a0414c" />
- ✅ Signature runes: "Fabricado por" on a craft, "Modificado por" on a magus table, stored in the item itself
- 🟡 The odds of a rune are the community's model (66/34/0 on a weak item, 43/50/7 at the perfect jet, a 15% floor, a rune's reach of 30·√weight); Ankama never published theirs
- ✅ Maging for someone else: invite a customer or a magus from a magus table, the customer lays their item, runes and signature, pays when a rune went on their item; the magus' side measured whole, the customer's mirrored
- ✅ Breaking items at the grinder into their base runes, `(3 · value · weight · level / 200 + 1) · coefficient`
- ✅ The breaking focus: the focused characteristic takes half the weight of every other
<img width="1378" height="1218" alt="image" src="https://github.com/user-attachments/assets/acb6b847-1ab0-4966-83db-657fd0fa38c3" />
- ✅ The artisans' directory: each job's settings (free, minimum level) kept per character, the public list, and the book of every workshop opening its jobs
- ✅ Transcendence runes: 100% of success within the density rule of their own data, never over an over or an exo, and the item closed to smithmagic afterwards
- ➖ Corruption runes: not in the 3.6.10 game data (Ankama withdrew them in 2.51); only their help text remains
- ✅ `.oficios [level]` puts every job at a level (200 by default), `.oficio <job> <level>` one of them
- ✅ `.receta <item> [times]` puts the ingredients of an item's recipe in the bag, each onto the stack already there
- ✅ Forgegod mode for administrators (`.forjadios on|off`, `.forgegod`, `.forgedieu`): no rune fails, no weight cap, two AP of exo, transcendence on anything, no job level on recipes
<img width="1370" height="1182" alt="image" src="https://github.com/user-attachments/assets/751cb412-d2de-4b21-b91e-498342754dce" />

### 👹 NPCs and monsters
<img width="954" height="836" alt="image" src="https://github.com/user-attachments/assets/78779a18-0cd2-4f5c-b403-0c39cd291bcb" />
<img width="2560" height="1510" alt="image" src="https://github.com/user-attachments/assets/43ebcfc4-fdbb-4924-b876-0c06743f8294" />
<img width="2558" height="1510" alt="image" src="https://github.com/user-attachments/assets/0b4adf75-9b36-4298-b428-d0444297adb3" />

- ✅ **6,468 NPC templates** with 3D looks and dialogue trees
- ✅ **422 NPCs** standing where Ankama puts them across **202 maps**, with dialogue attached where known
- ✅ **5,134 monsters** with native Protobuf bone models, custom scales and textures, quest monsters and archmonsters included
<img width="1700" height="930" alt="image" src="https://github.com/user-attachments/assets/02254e58-ec87-4839-82ac-f142ec5ef9cd" />

- ✅ **38,744 mapped mob groups**, respawned and kept populated, 1 to 8 monsters each
- ✅ Sub-area aware spawning across **562 sub-areas**, with radius-2 cell validation so nothing spawns on decorations or zaap pillars
- ✅ No monsters indoors, and none standing on a zaap — not in houses, banks or shops; the 763 dungeon rooms are exempt. 7,214 groups of 38,744 kept out
- ✅ NPC colours read as `index=value` pairs, decimal or hexadecimal: the **2,045 NPCs that carry colours** render with theirs
- ✅ A dialogue always offers at least one real reply, so it can always be closed
- 🟡 **401 monsters have no spells at all** in the database
- ✅ Dialogue trees — which reply leads to which line — are authored in `content/npcs/dialogues.json`; the client's data does not hold that mapping
<img width="1138" height="694" alt="image" src="https://github.com/user-attachments/assets/fc1182c3-a261-4bcd-9532-84a2ceda8dc8" />
<img width="1082" height="692" alt="image" src="https://github.com/user-attachments/assets/39cdf857-b506-4968-b2f9-c0c5f80b64c3" />

- ✅ Monster groups placed by hand, and Ankama's own removable, in `content/monsters/groups.json`
- ✅ The kanojedo of the Amakna village (map 99090957): its door, six Puch Ingball inside — one per
  grade from level 1 to level 200, never moving and never replaced — and the master, NPC 7416,
  whose two screens set a session up: six levels, then one to four puchs, and the fight opens on the
  spot. Themed puchs — Vil Smis, Sombra, Hiperescampo, Sylargh, Cráneo Rosa — take turns with the
  Ingball at random among those with a grade at that level. Training fights offer no challenges,
  give no rewards and leave the group in place
- ✅ Measured arenas in `content/fights/arenas.json` pair a roleplay map with the arena its fights
  are held on, ahead of the general rule

### 📜 Quests
<img width="2560" height="1498" alt="image" src="https://github.com/user-attachments/assets/6dbe2000-4f3c-4b41-9409-5be932f84d6e" />
<img width="1452" height="1226" alt="image" src="https://github.com/user-attachments/assets/77256193-a9dd-48df-9d0a-5408613fef34" />

**1,976 quests**, with their 2,225 steps and 15,547 objectives.

- ✅ A quest is handed over by an NPC saying a particular line — 1,260 steps declare one
- ✅ Objectives complete two ways: the client reports the **5,670** that ask you to click something
  the server never sees, and the server counts the ones that ask you to beat a monster
- ✅ Progress is written the moment it changes
- 🟡 The start condition language has **29 operators**; six are understood, covering every term of
  **935 of the 1,976** conditions. The rest are let through and named

Full workings in **`docs/quests.md`**.

### 🏰 Dungeons
<img width="2550" height="1498" alt="image" src="https://github.com/user-attachments/assets/f79f7881-c68e-45b5-ae29-b4aaba928a1d" />
<img width="2560" height="1510" alt="image" src="https://github.com/user-attachments/assets/3c73a696-1de3-46ae-bea6-149bc06009fb" />
<img width="2560" height="1508" alt="image" src="https://github.com/user-attachments/assets/7b481a47-2fea-43da-a8a0-5b2692030473" />
<img width="2560" height="1510" alt="image" src="https://github.com/user-attachments/assets/a6347069-3d31-45bc-b40f-f4d942674e48" />

**187 dungeons**, with their **763 rooms**, their key and their boss.

- ✅ Talk to the guardian, hand over the key, and you are in the first room; win a fight and you
  move on; beat the boss in the last one and you come out
- ✅ The boss is placed at startup in **126** dungeons, in the room the data says, at its highest grade
- ✅ Each room has one group of eight built from the dungeon's own monsters, and a fight takes the first `clamp(players, 4, 8)` — four for a player alone — at the room's grade, as the jalatós capture shows; the map carries the group's variants by team size byte for byte; a beaten room comes back as itself, boss included
- ✅ The keyring and the required item come from the client's own data
- ✅ Dungeon challenges are imposed at 0% and carry achievements

> Ankama's dungeons are chains of rooms walked through doors, and none of the 187 has its internal
> passages in the extracted data or in Ankama's own world graph, so winning a fight moves you to the
> next room instead.

Full workings in **`docs/dungeons.md`**.

### 🌙 Infinite Dreams

Entered from the Plano Astral's well: a dream of 26 rooms in depth, walked band by band.

- ✅ Ten difficulties in three families (Sueño, Paradoja, Pesadilla), each with its measured starting bonus, dream points, astral storms and Draconiros arena
- ✅ Five bands, as the invitation capture measures them: fountains at rows 4, 10, 16 and 25, band IV closed by one fight room alone, and the **Fin du rêve** at row 26
- ✅ Every fight room pays its bonus and its dream points on entry — 5, 15 for the marked ones, 10 in band V — and the HUD shows the score, the points and the bonuses summed
- ✅ The bestiary, the loot table and the placement map of the room one stands in
- ✅ The fountains' shop (Rey Gob one fountain in four): bonuses, spells and dream points bought with dream points; the Rey Gob's favour once per fountain
- ✅ Astral storms reroll the room's group and map; a dream is saved to the base and resumed after a disconnection or a restart
- ✅ The Fin du rêve in waves of bosses, wanted monsters and high-level monsters: level 250 +5 a wave (1 to win, 5 at most) in a Sueño, 275 +10 (3 to win, 15 at most) in a Paradoja, 300 +15 (3 to win, no end) in a Pesadilla. Winning it, or falling after the waves it takes, ends the dream won
- ✅ A lost fight spends the Draconiros arena and the room can be tried again; with no arena left the dream is lost
- ✅ `.sueno [row]` (`.sueño`, `.dream`, `.reve`), administrators only: carries the dream in progress down its own graph to a room of that row, or to the Fin du rêve with no row — every room on the way entered and won as if fought, its bonus and dream points paid
- 🟡 Monsters are brought to the Fin du rêve's level by scaling their life and characteristics; the game's own scaling is not known, and the other rooms fight at the world groups' own grades
- ❌ Favour rooms, and the effects of the spells the shop sells

### 🪙 Jondo Coin

A currency of this server's own — a real item with its own template.
<img width="1676" height="1102" alt="image" src="https://github.com/user-attachments/assets/aee2eb3f-b2a3-4c35-a35c-fafe69669355" />

- ✅ Drops from every monster at 100%, one coin per 25 monster levels: 1 for 1-25, 2 for 26-50, up to 9 at 201+
- ✅ Its own description in the five client languages, picked at runtime from the language the client is running in
- ✅ Vendors that charge in coins instead of kamas, one per category, appearance shops among them, priced by item type and rarity

See `docs/jondo-coin.md`.

---

## ⚔️ One engine, four rulebooks

One fight engine serves four kinds of fight. What changes between them comes from a rules object:

| | Against monsters | Duel | Koliseo | Training |
|---|:---:|:---:|:---:|:---:|
| Challenges offered | yes | no | no | no |
| Placement clock | 45.0 s | — | 59.2 s | 45.0 s |
| `kam` type | 4 | 0 | 7 | 4 |
| `kaa` countdown | yes | no | yes | yes |
| Monster loot and experience | yes | no | no | no |
| Koliseo payout | no | no | yes | no |
| Clears the group on a win | yes | no | no | no |
| Moves to the next room | yes | no | no | no |

Two rules hold the rest together:

* The teams are `Azul` and `Rojo`, not players and monsters: in a duel both sides are people.
* Everything sent to a client is composed inside that client's own session, from each fighter's own record.

Three architecture tests enforce it: no lookups that assume one team is the players, no rules decided by fight type outside the rules object, and nothing writing to a single socket unless it is painting one person's own view.

### 🐉 PvM combat
<img width="2560" height="1510" alt="image" src="https://github.com/user-attachments/assets/d5fdf2d1-0244-4529-b2b9-06cf3dcdc1e5" />

- ✅ Tactical arenas resolved from each roleplay map, with clean context transitions
- ✅ Placement phase with red and blue tiles and cell swapping before *Ready*
- ✅ Isometric geometry (`MapGeometry`) over a pre-computed O(1) BFS distance matrix, with no diagonal steps
- ✅ Line of sight traced between cell centres against the arena's own blocker set
- ✅ Turn protocol, 30-second timers with automatic pass, AP/MP replenishment
- ✅ Movement with per-tile MP cost and collision against occupied cells
- ✅ Loot, victory and defeat screens, experience over **1,889 levels**, level-ups and group respawn
- ✅ End-of-fight statistics — damage dealt by source (own casts, glyphs and walls, summons, turn triggers, pushes), taken, heals given and received, shields, enemies defeated, and the per-turn and per-AP averages, each player getting their own numbers
- ✅ Monsters and bosses run their own spells' mechanics: the behaviour spell cast at the start, triggered rows armed on every fighter they name, 30+ triggers (damage by element, heals, states on and off, pushes and collisions, thresholds, deaths), state disabling (952), telefrags, delayed sub-casts, life thresholds, revives, glyphs shown in their own colours — Conde Kontatrás's clock works end to end. See **`docs/bosses.md`**
- ✅ Monster AI that plans its turn: every spell it can pay for, against every target, from every cell its MP reach — the blow against the target's resistance, kills first and the weakest enemy focused, heals for the badly wounded, AP/MP removal, buffs and summons once a turn; cooldowns, casts per turn and per target honoured; then it places itself (ranged at its reach, melee against the weakest to lock him, fleeing when nearly dead)
- 🟡 Weapon strikes apply damage and AP cost; the slash animation does not
- ✅ Push and collision damage, `blockedCells × (level/2 + push − resistance + 32) / 4`, floored. The fighter acting as the wall takes half, and the **Unmovable** state cancels it
- ✅ Joining someone else's fight in its placement: the swords on the map, a click on them (`kay`), or a party member pulled in behind the leader with *automatic entry*, and *automatic ready*; a dungeon's monster side grows with each player to the first `clamp(players, 4, 8)` of the room's eight
- ✅ A party opens its fights kept to the party, as the real server does, and the side's leader switches the options from the fight window — no spectators, party only, closed, asking for help (`jzx` → `kau`); an outsider knocking on a party-only side is turned down (`jxs` 16)
- ✅ A won fight is shared: the experience with the game's group bonus, each player's part by level up to two and a half times the strongest monster's; the kamas by prospecting; the items rolled for each player; and every end screen lists everybody's gains, as the follow capture shows. A player alone gets what he always got
- 🟡 Wisdom, the experience given to a mount or a guild and account bonuses are not modelled, alone or in a group; refusals other than a party-only side are not answered
- ✅ A dropped client does not stop the fight, and the player can come back into it — see
  [Connection and authentication](#-connection-and-authentication)
- ❌ Lock and tackle in melee

### 🤺 Duels
<img width="2560" height="1506" alt="image" src="https://github.com/user-attachments/assets/286367e0-6342-4aef-b07b-52d3bdbdf9d4" />
<img width="2560" height="1502" alt="image" src="https://github.com/user-attachments/assets/c1f3f058-81ea-4189-8f4b-312e3209f63a" />

Player against player, on the map, by challenging somebody standing there.

- ✅ Offer, accept and refuse, with the challenge id echoed through every frame of the fight
- ✅ Both fighters composed from their own character record — look, level, characteristics, equipment
- ✅ Placement with no clock, and no challenges offered
- ✅ Victory and defeat screens, each player's own, and both sides returned to the map
- ✅ Nothing is won and nothing is lost — no experience, no kamas, no loot
- ✅ The end-of-fight card shows the other player's portrait

### 🏟️ Koliseo
<img width="2560" height="1504" alt="image" src="https://github.com/user-attachments/assets/2b19a035-4124-41d6-9c43-0c6881f69e40" />
<img width="2560" height="1498" alt="image" src="https://github.com/user-attachments/assets/417c9bf9-c895-4a7a-8de3-ef394ce5392c" />
<img width="2560" height="1498" alt="image" src="https://github.com/user-attachments/assets/0a3bc3d6-ce61-4729-aa23-958a024c07fb" />
<img width="2560" height="1496" alt="image" src="https://github.com/user-attachments/assets/dfb0aeb1-b507-48b0-8e05-286c3fa1945e" />

Ranked PvP through a queue. Open the window, pick a format, get matched, fight, get paid.

- ✅ The format table (`lux` → `ltd`) — 1v1, 2v2, 3v3 open and a fourth closed
- ✅ Enrolling (`lsm`), with the format carried as the client's own enum
- ✅ The queue state (`lsx`) pushed back, which paints *searching* in the window
- ✅ Matchmaking on enrolment, one queue per format, drawn under a lock
- ✅ Everybody re-checked as still connected before anyone loses their place in the queue
- ✅ The fight itself, with the Koliseo rulebook, and both sides returned to roleplay at the end
- ✅ The winner is paid — kamas, Kolichas (item 12736), Vitorichas (34478) and experience. The loser gets nothing
- 🟡 The amounts are constants, not a formula; experience is 6.67% of the winner's level band
- 🚧 The *match found* popup with accept and refuse
- 🚧 Fights are held on an ordinary arena; the real game picks one of the Koliseo maps at random
- ❌ Rankings (`iqt`, `irc`)
- ❌ The `lst` redirect to a separate Koliseo server. Jondo is one server and holds the fight in place

### ✨ Spell effect engine

One engine for all eighteen classes, driven entirely by client data: everything comes out of
`SpellLevels.EffectsJson` and the `Effects` catalogue, and each thing a spell can do — push, shield,
carry, summon, copy — is one primitive that every spell using it shares. Of the **179 effects the
836 class spells use, 71 have a branch in the engine, 69 need no code** — they are characteristics
read from the `Effects` table — **and 39 are missing**. The table, effect by effect with how many
spells each touches, is [`docs/effect-coverage.txt`](docs/effect-coverage.txt).

- ✅ Effects, triggers and target masks read from the spell — `I` on cast, `TB` turn start, `TE` turn end, `DBE` when hit by an enemy, `DM`/`DR` when hurt in melee or at range, `X` on death, `CCMPARR` per tile walked; `a` allies (the caster included when he stands in his own zone), `A` enemies, `g` the other allies, `c` the caster when he is in the zone, `l`/`L` the players, `m`/`M` the monsters, `i`/`I` and `j`/`J` the summons — lower case the caster's side, upper case the other —, `O` whoever dealt the triggering blow, `P`/`p` the caster's own summons, `h` the caster's summoner, `E<n>`/`e<n>` gated on a state, `V<n>`/`v<n>` on a life threshold, `*E<n>`/`*e<n>` conditions on the caster. States and positions are judged on one snapshot taken before the cast: a row after a pull still reaches whoever was in the zone
- ✅ Rows for the client only are not read — the client's `ForClientOnly` bit marks the sheet's copy of what a spell does through a sub-cast (Furor's "+20", Vitalidad's "+N%", Manticolmillo's "+15 huida", Virtud's shield); the real server sends none of them
- ✅ One draw per cast — the `random` shares of a spell level add up to 100 and one draw picks a row and everything in its `group`: Bumerán Pérfido steals in one element and boosts that element's characteristic
- ✅ Stack limits — a spell level's `maxStack` says how many equivalent rows live together: `-1` without limit (Fervor, Tumulto), `1` the new row replaces the old (Espada del Juicio, announced gone before the new one), `2` and up a cap (Presión, Espada Destructora)
- ✅ States need no code — effect 950 sets a number, 951 clears it, the masks do the rest; the 103 states the client flags — invulnerable, cannot be moved or pushed, incurable — are read from `datos/spell_states.json`: an invulnerable target takes no blow at all, a pinned one no push
- ✅ Area shapes from `zoneDescr` — point, circle, cross (`X` and `Q`), the bar across the cast (`T`), line, half-line, ring (`O`), perpendicular line (`-`), square, half-circle, segment, whole map — with the inner edge (`param2`) honoured and each spell's own per-tile falloff
- ✅ Displacement — push, pull, step back, step forward, push without damage, and push or pull to the aimed cell (783/1043); direction taken from the centre of the area, stopping at walls, holes and fighters
- ✅ Teleports — to a cell, back to the previous position, symmetrical around the caster or the target — and position swaps
- ✅ Carry and throw (50/51) — the Pandawa's Karcham and Chamrak and the Tymobot's Pinzas share the two primitives
- ✅ Illusions (1097) — Tymadura: the caster jumps to the aimed cell and copies with his stats of the moment appear around the cell he left; they hold a cell, do not play, and go at the first damaging hit. His own side sees him translucent among opaque copies; the other side sees the copies dressed as him
- ✅ Criticals rolled against the spell's probability plus the character's, using the spell's critical effect list down the whole chain: a chained spell with a critical list of its own runs it and its rows carry the flag (Virtud's critical shield is 550% of the level, from 29723's own critical entry); one without runs its ordinary list unflagged
- ✅ Point steal, life steal, erosion of maximum HP and damage-taken multipliers
- ✅ Healing in all five elements, AP given back, best-element and worst-element damage (2822, 2832) and life steal
- ✅ Shields, by caster level or by HP: buff row 1040 and characteristic 96, as many rows as the level's `maxStack`; a replaced row takes its points with it
- ✅ Vitality percentages (1033/1078) — of the maximum life, base and gear included; announced as the flat rows the client draws, 153 down and 125 up, with the points
- ✅ Buff panel — icon, value, remaining rounds and dispellable flag; buffs start on their delay and expire on their round
- ✅ Cooldowns and cast limits — per turn, per target, minimum interval, initial cooldown; a spell that needs an empty cell, or a taken one, is refused before the AP go
- ✅ Nine sub-cast families, one table — 792 is cast by the target at its own cell, 1160 by the caster at the candidate's, 1017 back at the parent caster, 2160 at the nearest eligible target under a budget, 2794 at the parent cell
- ✅ Glyphs, traps and runes — 623 spells, one system: the four families share a shape and differ in when they fire, and a glyph that fires goes through the ordinary cast path
- ✅ Summons as real fighters — own sheet, behaviour spell, lifetime, and they all fall when their summoner dies. Whether one plays is bit 6 of its template's `m_flags`; those that do are driven by their owner from his own client. Capacity is the template's `summonCost` added up
- ✅ Bombs — a summon that costs nothing against the limit, stays out of the carousel, detonates through its own explosion (1009) once per chain, climbs a combo through spells 20497 and 20500, and lines up into walls of two or three with one to six cells between them, charged on entry and at turn start. The +1 AP per living bomb and the chain reaction are not done
- ✅ Class passives — each class casts its own initial spell before the first turn (*La Astucia del Tymador*, *El Alcance de Ocra*, *La Sombra de Sram*, *El Escudo de Feca*…), kept in `content/fights/class_passives.json`. The initial spells of a character's own choices go with it
- ✅ Hooked spells fire on every trigger — turn start, turn end, when hit, on death and per step walked — from their original caster, chained spells included, inside one sequence of the bearer's; a hooked row with a delay waits that many rounds (Furor's decay fires at the end of the turn after the cast, and hooks the grade it falls to); every chained cast is announced once per grade before the first thing it does, and a 406 after the rows it takes
- ✅ Delayed effects — a `delay` in the catalogue is a hidden row with trigger `Y` that fires at the first turn of its round (the survival beacon's lifetime, Paso de Cacería's +1 MP)
- ✅ Points and rows across turns — a turn starts with the maximum plus the live point buffs; an expired row falls at the start of its caster's turn
- ✅ AP and MP removal against dodge — rolled point by point with the game's own odds, `(points left / maximum) × (retira + 2) / (esquiva + 2) × ½`, between 10% and 90%; *retira* and *esquiva* are a tenth of wisdom plus the gear (410–413, 160–163) and the live rows, monsters carry their grade's `paDodge`/`pmDodge`. What is dodged goes out as `jwe 308/309`, what lands as a `-N PA/PM` row (168/169)
- ✅ A summon with nothing to play hands its turn on; every summon that can act is its owner's to play by hand
- ✅ *-N de daños recibidos* (105, 265) and *daños sufridos x#1%* (1163) under a damage kind — rows read by the blow, whose letters say which blows: `DR` ranged, `DM`/`DCAC` melee, `D` any, `DTB`/`DTE` a turn's poison. The elemental and per-source letters are registered and not yet read
- ✅ Item attitudes — the six Dofus and the trophies grant their spell through effect 1175
- ✅ Appearance-changing spells — the transform replaces the root bones and keeps colours, skins, scale and pets, through combat action 149
- ✅ Script markers 3792 and 3793 do nothing: their value is a script id, not an effect
- ✅ The characteristic sheet in the shape the client expects: 53 entries in a fixed order, and a single-characteristic refresh replaces its entry
- ❌ A cooldown pinned to a number of turns (1045), a spell's own basic-healing bonus (2935), damage as a share of the damage taken (1223), best-element healing (3002), damage sharing and interception, portals, revealing invisibles, MP steal — the full list is in the coverage table
- ❌ Area shapes `G` (55 effects), `*` (10) and `;`, which fall back to the centre tile alone, and the target-mask letters the check-list below names

> Only the **Ocra**, the **Tymador** and the **Yopuka** have been checked against the real client spell by spell; the check-list below says which spells. A spell only works when all of its effects resolve, and the gaps concentrate in a handful of effect families.

### 🔬 Spell check-list

Every class spell, one line each, in the order of the spell book. ✅ means it has been checked
against the real client, or against its own capture, and does what the game does. ❌ means it has
not — either the engine cannot resolve part of it on paper, and the reason follows the dash (an
effect with no implementation, a target mask or an area shape the engine does not read; sub-casts
are followed, so a gap in a chained spell shows on the spell that starts the chain), or it resolves
on paper and has not been checked yet. A ✅ with "unread on paper" after the dash works in the
game while a letter of its sheet is still not read.

The list is generated from `world.db` and the engine's own source by `tools/spell_checklist.py`;
the ✅ are set by hand in it. Rows for the client only are left out of the count. Most crosses
come from the target-mask letters the engine does not read yet (`T`, `R`, `U`, `r`, `b<n>`,
`PB`/`pb`, `H`/`h`, `D`/`d`), then the area shape `G`, and only then the effects: a cooldown pinned
to a number of turns (1045), a spell's own basic-healing bonus (2935), maximised random rolls
(782), damage as a share of the damage taken (1223).

| Class | Seen working | Resolve on paper | Spells |
|---|:---:|:---:|:---:|
| Feca | 0 | 29 | 44 |
| Osamodas | 0 | 37 | 44 |
| Anutrof | 0 | 40 | 44 |
| Sram | 0 | 38 | 44 |
| Xelor | 0 | 10 | 44 |
| Zurcarák | 0 | 6 | 44 |
| Aniripsa | 0 | 29 | 44 |
| Yopuka | 22 | 42 | 44 |
| Ocra | 7 | 32 | 44 |
| Sadida | 0 | 31 | 44 |
| Sacrógrito | 0 | 35 | 44 |
| Pandawa | 0 | 35 | 44 |
| Tymador | 13 | 42 | 44 |
| Zobal | 0 | 37 | 44 |
| Steamer | 0 | 25 | 44 |
| Selatrop | 0 | 11 | 44 |
| Hipermago | 0 | 8 | 44 |
| Uginak | 0 | 4 | 44 |
| Forjalanza | 0 | 33 | 44 |
| **All** | **42** | **524** | **836** |

<details><summary><b>Feca</b> — 0 of 44 seen working, 29 resolve on paper</summary>

- ❌ Somnolencia
- ❌ Maniobra
- ❌ Languidez
- ❌ Atonía
- ❌ Murallón
- ❌ Fortificación
- ❌ Tifón
- ❌ Borrasca
- ❌ Escalofrío
- ❌ Chaparrón
- ❌ Barricada
- ❌ Pavés
- ❌ Recelo — effect 402 (places an end-of-turn glyph), mask `U`
- ❌ Parapeto — effect 1165 (places to glyph), effect 2018 (dispels glyphs), mask `U`
- ❌ Letargo
- ❌ Reagrupamiento
- ❌ Nimbo
- ❌ Estrato — shape `G`
- ❌ Bastión
- ❌ Tregua
- ❌ Puñalada
- ❌ Tetania
- ❌ Pompa
- ❌ Cencerro
- ❌ Silbo
- ❌ Cayado
- ❌ Sopor
- ❌ Escapadita
- ❌ Aprisco — shape `G`
- ❌ Excursión — effect 1165 (places to glyph)
- ❌ Escudo feca — mask `b1`
- ❌ Posición Defensiva — effect 1026 (triggers glyphs), mask `b1`
- ❌ Refuerzo
- ❌ Ataraxia
- ❌ Prado — shape `*`
- ❌ Pasto — effect 2018 (dispels glyphs)
- ❌ Valle
- ❌ Escarcha — effect 2018 (dispels glyphs), shape `G`
- ❌ Tierra Batida
- ❌ Refugio — effect 2018 (dispels glyphs)
- ❌ Trashumancia — effect 1026 (triggers glyphs)
- ❌ Égida — effect 765 (intercepts damage), mask `b1`
- ❌ Tierra Quemada — shape `G`
- ❌ Vigía — effect 2018 (dispels glyphs)

</details>

<details><summary><b>Osamodas</b> — 0 of 44 seen working, 37 resolve on paper</summary>

- ❌ Grito de Cuerbok
- ❌ Colmillos de Milubo
- ❌ Pinchos de Prespic
- ❌ Desplume
- ❌ Dientes de Piranya
- ❌ Corazón Salvaje
- ❌ Grito del Oso — shape `G`
- ❌ Baba de Sapo — shape `G`
- ❌ Látigo
- ❌ Fusta
- ❌ Tofu
- ❌ Garras de Chtigre
- ❌ Jalató
- ❌ Garras de Buitre — effect 786 (heals the attacker for #1% of the damage), shape `F`
- ❌ Saponito
- ❌ Vellocino de Oro
- ❌ Dragún
- ❌ Mordedura de Serpiente
- ❌ Séquito Salvaje — effect 285 (#1: -#3 AP)
- ❌ Pacto Bestial — effect 1045 (#1: cooldown pinned to #3 turns), mask `u`
- ❌ Chute Motivador
- ❌ Comunión Animal — effect 1061 (shares damage)
- ❌ Salta la Ranadina
- ❌ Tornado de Plumas
- ❌ Soplido Dracónico
- ❌ Golpe del Crujidor
- ❌ Carga Bestial
- ❌ Canto de Fénix
- ❌ Golpazo Aéreo
- ❌ Torbellino
- ❌ Disciplina
- ❌ Fuete
- ❌ Gorditofu
- ❌ Crujintesco
- ❌ Jalatorpe
- ❌ Cocolérico
- ❌ Saponcio
- ❌ Azufrénix
- ❌ Dragonito
- ❌ Escararrayo
- ❌ Lazo Espiritual — effect 2184 (Sigue to the lanzador)
- ❌ Relevo Espiritual
- ❌ Espíritu Glotón
- ❌ Espíritu Burlón

</details>

<details><summary><b>Anutrof</b> — 0 of 44 seen working, 40 resolve on paper</summary>

- ❌ Lanzamiento de Monedas
- ❌ Moneda sonante
- ❌ Pala Fantomática
- ❌ Último Recurso
- ❌ Mochila Animada
- ❌ Morral Animado
- ❌ Jarabe de Pala
- ❌ Desprendimiento
- ❌ Bancarrota
- ❌ Lanzamiento de Pala
- ❌ Fiebre del Oro
- ❌ Andador
- ❌ Caja de Pandora
- ❌ Caja de Herramientas — mask `u`
- ❌ Fuerza de la Edad
- ❌ Búsqueda de Oro
- ❌ Llave del Tesoro
- ❌ Llave de Brazo
- ❌ Subterráneo
- ❌ Laya de los Ancianos
- ❌ Palas animadas
- ❌ Laya Animada
- ❌ Avaricia
- ❌ Decadencia
- ❌ Pala Aurífera
- ❌ Turbera
- ❌ Torpeza
- ❌ Edad de Oro
- ❌ Terraplenado — mask `D`, mask `H`, mask `d`
- ❌ Fuego de Mina — effect 786 (heals the attacker for #1% of the damage)
- ❌ Oportunidad
- ❌ Explosión de Grisú
- ❌ Debilitación
- ❌ Obsolescencia
- ❌ Jubilación Anticipada
- ❌ Pala de la Fortuna
- ❌ Corrupción
- ❌ Túnel de Fortuna
- ❌ Caducidad
- ❌ Tamizado — shape `G`
- ❌ Pala de los Ancianos
- ❌ Filón
- ❌ Cofre Animado
- ❌ Arcón Animado

</details>

<details><summary><b>Sram</b> — 0 of 44 seen working, 38 resolve on paper</summary>

- ❌ Invisibilidad
- ❌ Bruma
- ❌ Trampas solapadas
- ❌ Zalagarda — effect 786 (heals the attacker for #1% of the damage)
- ❌ Truhanería
- ❌ Abrojo
- ❌ Arsénico
- ❌ Toxinas
- ❌ Trampa Repulsiva
- ❌ Trampa Espeluznante
- ❌ Engaño
- ❌ Rebanacuellos
- ❌ Doble — effect 180 (summons to double of the caster), effect 2027 (Toma el control de la entidad), mask `D`, mask `H`, mask `U`
- ❌ Conspirador — effect 180 (summons to double of the caster), effect 2027 (Toma el control de la entidad), mask `D`, mask `H`, mask `U`
- ❌ Trampa Fangosa
- ❌ Epidemia — mask `D`, mask `H`
- ❌ Extorsión
- ❌ Registro
- ❌ Crueldad
- ❌ Mala Sombra — effect 2973 (heals #1-#2% of the damage dealt), effect 781 (minimises the target random rolls)
- ❌ Trampa Funesta
- ❌ Efracción
- ❌ Trampa de Inmovilización
- ❌ Fosa Común
- ❌ Trampa Miserable
- ❌ Trampa de Fragmentación
- ❌ Estafa
- ❌ Hurto
- ❌ Pillaje
- ❌ Ataque Mortal
- ❌ Miedo
- ❌ Equivocación
- ❌ Karadura
- ❌ Perfidia
- ❌ Concentración de Chakra
- ❌ Artimaña
- ❌ Trampas mortales
- ❌ Calamidad — shape `G`
- ❌ Escapatoria
- ❌ Marca Mortuoria
- ❌ Trampa de Deriva
- ❌ Trampa Insidiosa
- ❌ Estratagema
- ❌ Inyección Tóxica

</details>

<details><summary><b>Xelor</b> — 0 of 44 seen working, 10 resolve on paper</summary>

- ❌ Teletransportación — effect 1026 (triggers glyphs), effect 1045 (#1: cooldown pinned to #3 turns), effect 1223 (damage: #1-#2% of the final damage taken), mask `T`
- ❌ Astrolabio — effect 1026 (triggers glyphs), effect 1045 (#1: cooldown pinned to #3 turns), effect 1223 (damage: #1-#2% of the final damage taken), mask `T`
- ❌ Perturbación — effect 1026 (triggers glyphs), effect 1045 (#1: cooldown pinned to #3 turns), effect 1223 (damage: #1-#2% of the final damage taken), mask `T`
- ❌ Rueda Dentada — effect 1026 (triggers glyphs), effect 1045 (#1: cooldown pinned to #3 turns), effect 1223 (damage: #1-#2% of the final damage taken), mask `T`
- ❌ Recuerdo — effect 1026 (triggers glyphs), effect 1045 (#1: cooldown pinned to #3 turns), effect 1223 (damage: #1-#2% of the final damage taken), mask `T`
- ❌ Permutación — effect 1026 (triggers glyphs), effect 1045 (#1: cooldown pinned to #3 turns), effect 1223 (damage: #1-#2% of the final damage taken), mask `T`
- ❌ Marchitación
- ❌ Aguja — effect 1406 (removes the effects of grade #1 of spell #2)
- ❌ Rebobinamiento — effect 1026 (triggers glyphs), effect 1045 (#1: cooldown pinned to #3 turns), effect 1099 (teleports to the turn-start position), effect 1223 (damage: #1-#2% of the final damage taken), mask `T`
- ❌ Remanencia — effect 1026 (triggers glyphs), effect 1045 (#1: cooldown pinned to #3 turns), effect 1223 (damage: #1-#2% of the final damage taken), mask `T`
- ❌ Refracción — effect 1026 (triggers glyphs), effect 1045 (#1: cooldown pinned to #3 turns), effect 1223 (damage: #1-#2% of the final damage taken), mask `T`
- ❌ Regulador — effect 1026 (triggers glyphs), effect 1045 (#1: cooldown pinned to #3 turns), effect 1223 (damage: #1-#2% of the final damage taken), effect 290 (#1: +#3 cast(s) per turn), mask `T`
- ❌ Cómplice
- ❌ Esfera de Xelor
- ❌ Congelación — effect 1026 (triggers glyphs), effect 1045 (#1: cooldown pinned to #3 turns), effect 1223 (damage: #1-#2% of the final damage taken), mask `T`
- ❌ Polvo — effect 1026 (triggers glyphs), effect 1045 (#1: cooldown pinned to #3 turns), effect 1223 (damage: #1-#2% of the final damage taken), mask `T`
- ❌ Ralentización
- ❌ Reloj de Arena de Xelor — effect 1406 (removes the effects of grade #1 of spell #2)
- ❌ Engranaje — effect 1026 (triggers glyphs), effect 1045 (#1: cooldown pinned to #3 turns), effect 1223 (damage: #1-#2% of the final damage taken), mask `T`
- ❌ Cuentagotas — effect 1026 (triggers glyphs), effect 1045 (#1: cooldown pinned to #3 turns), effect 1223 (damage: #1-#2% of the final damage taken), mask `T`
- ❌ Borroso Temporal
- ❌ Conservación
- ❌ Distorsión — effect 1026 (triggers glyphs), effect 1045 (#1: cooldown pinned to #3 turns), effect 1223 (damage: #1-#2% of the final damage taken), mask `T`, shape `G`
- ❌ Arenas del Tiempo — effect 1026 (triggers glyphs), effect 1045 (#1: cooldown pinned to #3 turns), effect 1223 (damage: #1-#2% of the final damage taken), mask `T`
- ❌ El Tiempo Vuela — effect 1026 (triggers glyphs), effect 1045 (#1: cooldown pinned to #3 turns), effect 1223 (damage: #1-#2% of the final damage taken), mask `T`
- ❌ Premonición — effect 1026 (triggers glyphs), effect 1045 (#1: cooldown pinned to #3 turns), effect 1101 (teleports o intercambia posiciones), effect 1223 (damage: #1-#2% of the final damage taken), mask `T`
- ❌ Rayo Oscuro — effect 1026 (triggers glyphs), effect 1045 (#1: cooldown pinned to #3 turns), effect 1223 (damage: #1-#2% of the final damage taken), mask `T`
- ❌ Desecamiento — effect 1026 (triggers glyphs), effect 1045 (#1: cooldown pinned to #3 turns), effect 1223 (damage: #1-#2% of the final damage taken), mask `T`
- ❌ Paradoja — effect 1026 (triggers glyphs), effect 1045 (#1: cooldown pinned to #3 turns), effect 1223 (damage: #1-#2% of the final damage taken), mask `T`
- ❌ Falla — effect 1026 (triggers glyphs), effect 1045 (#1: cooldown pinned to #3 turns), effect 1223 (damage: #1-#2% of the final damage taken), mask `T`, mask `u`
- ❌ Syncro
- ❌ Tañido — shape `G`
- ❌ Petrificación — effect 1026 (triggers glyphs), effect 1045 (#1: cooldown pinned to #3 turns), effect 1223 (damage: #1-#2% of the final damage taken), effect 285 (#1: -#3 AP), mask `T`
- ❌ Reloj de Bolsillo — effect 1026 (triggers glyphs), effect 1045 (#1: cooldown pinned to #3 turns), effect 1223 (damage: #1-#2% of the final damage taken), effect 2018 (dispels glyphs), mask `T`
- ❌ Reloj — effect 1026 (triggers glyphs), effect 1045 (#1: cooldown pinned to #3 turns), effect 1223 (damage: #1-#2% of the final damage taken), mask `T`
- ❌ Reloj de Agua — effect 1026 (triggers glyphs), effect 1045 (#1: cooldown pinned to #3 turns), effect 1223 (damage: #1-#2% of the final damage taken), mask `T`
- ❌ Golpe de Xelor — effect 1026 (triggers glyphs), effect 1045 (#1: cooldown pinned to #3 turns), effect 1223 (damage: #1-#2% of the final damage taken), mask `T`
- ❌ Péndulo — effect 1026 (triggers glyphs), effect 1045 (#1: cooldown pinned to #3 turns), effect 1223 (damage: #1-#2% of the final damage taken), mask `T`
- ❌ Momificación — effect 1026 (triggers glyphs), effect 1045 (#1: cooldown pinned to #3 turns), effect 1223 (damage: #1-#2% of the final damage taken), mask `T`
- ❌ 25ª Hora
- ❌ Rolbac — effect 1026 (triggers glyphs), effect 1045 (#1: cooldown pinned to #3 turns), effect 1223 (damage: #1-#2% of the final damage taken), mask `T`
- ❌ Inestabilidad
- ❌ Desincronización
- ❌ Espaciotiempo — effect 1026 (triggers glyphs), effect 1045 (#1: cooldown pinned to #3 turns), effect 1223 (damage: #1-#2% of the final damage taken), mask `T`

</details>

<details><summary><b>Zurcarák</b> — 0 of 44 seen working, 6 resolve on paper</summary>

- ❌ Espíritu Felino — effect 782 (Maximiza los efectos aleatorios en el objetivo)
- ❌ Kraps — effect 782 (Maximiza los efectos aleatorios en el objetivo)
- ❌ Garra Invocadora
- ❌ Caricia Invocadora
- ❌ Golpe de Fortuna — effect 2935 (#1: +#3 basic healing on that spell), effect 782 (Maximiza los efectos aleatorios en el objetivo)
- ❌ Redistribución — effect 2935 (#1: +#3 basic healing on that spell), effect 782 (Maximiza los efectos aleatorios en el objetivo)
- ❌ Olfato — effect 2935 (#1: +#3 basic healing on that spell), effect 782 (Maximiza los efectos aleatorios en el objetivo)
- ❌ Rueda de la Fortuna — effect 2935 (#1: +#3 basic healing on that spell), effect 781 (minimises the target random rolls), effect 782 (Maximiza los efectos aleatorios en el objetivo)
- ❌ Reflejos — effect 782 (Maximiza los efectos aleatorios en el objetivo)
- ❌ Lametazo — effect 782 (Maximiza los efectos aleatorios en el objetivo)
- ❌ Truco — effect 285 (#1: -#3 AP), effect 287 (#1: +#3% de crítico), effect 2935 (#1: +#3 basic healing on that spell), effect 296 (#1: +#3 AP), effect 782 (Maximiza los efectos aleatorios en el objetivo)
- ❌ Todo o Nada — effect 3002 (#1-#2 best-element healing), effect 782 (Maximiza los efectos aleatorios en el objetivo)
- ❌ Salto del Felino
- ❌ Trenzado
- ❌ Topkaj — effect 782 (Maximiza los efectos aleatorios en el objetivo)
- ❌ Garra Juguetona — effect 782 (Maximiza los efectos aleatorios en el objetivo)
- ❌ Jass — effect 2935 (#1: +#3 basic healing on that spell), effect 782 (Maximiza los efectos aleatorios en el objetivo)
- ❌ Desdicha — effect 782 (Maximiza los efectos aleatorios en el objetivo)
- ❌ Cara o Cruz — effect 782 (Maximiza los efectos aleatorios en el objetivo)
- ❌ Fantasmada — effect 782 (Maximiza los efectos aleatorios en el objetivo)
- ❌ Segunda Oportunidad — effect 2935 (#1: +#3 basic healing on that spell), effect 782 (Maximiza los efectos aleatorios en el objetivo)
- ❌ Nueve Vidas — effect 782 (Maximiza los efectos aleatorios en el objetivo)
- ❌ Farol — effect 2935 (#1: +#3 basic healing on that spell), effect 782 (Maximiza los efectos aleatorios en el objetivo)
- ❌ Rekop
- ❌ Almohadillas — effect 782 (Maximiza los efectos aleatorios en el objetivo)
- ❌ Bufido — effect 782 (Maximiza los efectos aleatorios en el objetivo)
- ❌ Yams — effect 782 (Maximiza los efectos aleatorios en el objetivo)
- ❌ Lengua Raspadora — effect 782 (Maximiza los efectos aleatorios en el objetivo)
- ❌ Belote — effect 2935 (#1: +#3 basic healing on that spell), effect 782 (Maximiza los efectos aleatorios en el objetivo)
- ❌ Peligro — effect 782 (Maximiza los efectos aleatorios en el objetivo)
- ❌ Baraka — effect 2935 (#1: +#3 basic healing on that spell), effect 782 (Maximiza los efectos aleatorios en el objetivo)
- ❌ Osadía — effect 782 (Maximiza los efectos aleatorios en el objetivo)
- ❌ Ruleta
- ❌ Tarot de Zurcarák — effect 1045 (#1: cooldown pinned to #3 turns), effect 1099 (teleports to the turn-start position), effect 285 (#1: -#3 AP), effect 290 (#1: +#3 cast(s) per turn), effect 2935 (#1: +#3 basic healing on that spell), effect 782 (Maximiza los efectos aleatorios en el objetivo)
- ❌ Castillo de Naipes — effect 2935 (#1: +#3 basic healing on that spell), effect 782 (Maximiza los efectos aleatorios en el objetivo)
- ❌ Buena Estrella — effect 782 (Maximiza los efectos aleatorios en el objetivo)
- ❌ Blakjak — effect 2935 (#1: +#3 basic healing on that spell), effect 782 (Maximiza los efectos aleatorios en el objetivo)
- ❌ Destino de Zurcarák — effect 782 (Maximiza los efectos aleatorios en el objetivo)
- ❌ Percepción — effect 202 (reveals invisible entities)
- ❌ Predación — effect 202 (reveals invisible entities)
- ❌ Ovillo — effect 782 (Maximiza los efectos aleatorios en el objetivo)
- ❌ Garra de Ceangal — effect 782 (Maximiza los efectos aleatorios en el objetivo)
- ❌ Feliación — effect 782 (Maximiza los efectos aleatorios en el objetivo)
- ❌ Desventura — effect 782 (Maximiza los efectos aleatorios en el objetivo)

</details>

<details><summary><b>Aniripsa</b> — 0 of 44 seen working, 29 resolve on paper</summary>

- ❌ Palabra de Amistad
- ❌ Palabra Alquímica — effect 290 (#1: +#3 cast(s) per turn), mask `x`
- ❌ Palabra Escandalosa
- ❌ Grito Ensordecedor
- ❌ Palabra Juguetona
- ❌ Palabra Maliciosa
- ❌ Palabra Vampírica
- ❌ Sollozos — effect 2973 (heals #1-#2% of the damage dealt)
- ❌ Palabra Estimulante
- ❌ Palabra de Declive — effect 2935 (#1: +#3 basic healing on that spell)
- ❌ Blasfemia
- ❌ Ungüento Ancestral
- ❌ Pintura de Guerra
- ❌ Palabra Secreta — effect 2935 (#1: +#3 basic healing on that spell)
- ❌ Lamentos — effect 2973 (heals #1-#2% of the damage dealt)
- ❌ Demencia
- ❌ Palabra Turbulenta
- ❌ Palabra Furiosa
- ❌ Palabra Revitalizante
- ❌ Palabra Galvanizadora
- ❌ Palabra Bromista
- ❌ Palabra Censurada
- ❌ Palabra Florida — shape `*`
- ❌ Bosquecillo Encantado
- ❌ Palabra de Juventud
- ❌ Palabra Deprimente — effect 2935 (#1: +#3 basic healing on that spell)
- ❌ Grito de Guerra
- ❌ Palabra Ritual
- ❌ Palabra Prohibida
- ❌ Palabra Exangüe — effect 786 (heals the attacker for #1% of the damage)
- ❌ Palabra Abrumadora — effect 2935 (#1: +#3 basic healing on that spell)
- ❌ Palabra Desanimadora — effect 2935 (#1: +#3 basic healing on that spell)
- ❌ Ladronceo — effect 320 (steals #1-#2 range)
- ❌ Palabra Entretenida
- ❌ Palabra de Vuelo
- ❌ Fuente de Juventud — effect 402 (places an end-of-turn glyph)
- ❌ Pincel Tribal
- ❌ Coro Estridente — effect 2935 (#1: +#3 basic healing on that spell)
- ❌ Crioterapia
- ❌ Murmullo — effect 77 (Roba #1-#2 MP)
- ❌ Palabra de Pavor
- ❌ Escalpelo — effect 3002 (#1-#2 best-element healing)
- ❌ Palabra de Reconstitución
- ❌ Palabra de Solidaridad

</details>

<details><summary><b>Yopuka</b> — 22 of 44 seen working, 42 resolve on paper</summary>

- ✅ Machete
- ❌ Acumulación
- ✅ Intimidación
- ❌ Conquista
- ✅ Salto — the x115% row on the enemies around the arrival, under D
- ❌ Agitación
- ✅ Fervor
- ❌ Amenaza
- ✅ Espada Divina
- ❌ Espada del Juicio
- ✅ Espada Destructora — the T is the bar across the cast, and two casts erode 26%
- ❌ Fustigación
- ✅ Aguante
- ❌ Pugilato
- ✅ Soplido
- ❌ Congregación
- ✅ Concentración — the L,M,l,m,c and J,j rows: monsters and players, and the summons
- ❌ Sentencia
- ✅ Furor — 28604's rows alone: Furor I and II, and the decay at the end of the turn after
- ❌ Ira de Yopuka
- ✅ Fricción — the state lands on the enemy it just pulled, and the DBE hook pulls him again
- ❌ Golpe por Golpe
- ✅ Influencia — the Invulnerable state takes the whole blow, and the -100 PM row
- ❌ Duelo Yopukil
- ✅ Potencia
- ❌ Vindicta
- ✅ Virtud — 29723's rows alone: the shield around, 550% on a critical, and one -50 per ally in contact
- ❌ Masacre — effect 1223 (damage: #1-#2% of the final damage taken)
- ✅ Tempestad de Potencia
- ❌ Casca
- ✅ Espada Celeste
- ❌ Cénit — effect 1013 (#1-#2 air damage (% MP restantes))
- ✅ Vitalidad — 25215's row alone: +20% of the maximum life on oneself, +10% on the others
- ❌ Violencia
- ✅ Espada de Yopuka
- ❌ Cuchillo de Carnicero
- ✅ Espada del Destino
- ❌ Tumulto
- ✅ Presión — the two casts add up to 20% (its maxStack is 2)
- ❌ Fractura
- ✅ Oleada
- ❌ Anillo Destructor
- ✅ Precipitación
- ❌ Determinación

</details>

<details><summary><b>Ocra</b> — 7 of 44 seen working, 32 resolve on paper</summary>

- ✅ Flecha Helada — the critical roll, measured
- ❌ Flecha Acosante
- ❌ Flecha de Pelea
- ❌ Diamantes Destructores — shape `F`
- ❌ Flecha Azotadora
- ❌ Flecha Asaltante
- ❌ Flecha Vagabunda
- ❌ Flecha Evasiva
- ✅ Paso de Cacería — the jump, and the +1 MP the turn after
- ❌ Baliza Táctica
- ❌ Disparos Lejanos — mask `b9`
- ❌ Tiro Penetrante
- ❌ Flecha Detonadora
- ❌ Flecha Ralentizante
- ❌ Flecha de Abolición — mask `PB`, mask `pb`
- ❌ Flecha Perseguidora
- ❌ Flecha de Retroceso
- ❌ Flecha Impactante
- ❌ Flecha Inmovilizadora — effect 77 (Roba #1-#2 MP)
- ❌ Flecha Tiránica
- ❌ Tiros Potentes
- ❌ Flechas Amorosas — effect 1061 (shares damage), mask `d`
- ❌ Flecha de Dispersión
- ❌ Flechas Flamígeras
- ❌ Flecha Explosiva
- ❌ Flecha Masacrante
- ❌ Ojo de Topo — effect 202 (reveals invisible entities)
- ❌ Lluvia de Flechas
- ❌ Ojo por Ojo
- ❌ Flecha Paralizadora — shape `G`
- ✅ Baliza de Supervivencia — she plays her turn on her own and dies two rounds later through her own 141
- ✅ Represalias
- ✅ Tiro de Repliegue
- ❌ Vendetta
- ✅ Flecha Castigadora — its start, measured
- ❌ Flecha del Juicio — effect 1016 (#1-#2 earth damage (% MP restantes))
- ❌ Flecha de Expiación
- ❌ Flecha de Redención — effect 1406 (removes the effects of grade #1 of spell #2)
- ❌ Flecha Percutiente — mask `PB`, mask `pb`
- ❌ Flecha Búmeran
- ❌ Flecha Voraz
- ✅ Flecha Fulminante — the rebound, measured from both sides
- ❌ Agudeza Absoluta — effect 289 (#1: line of sight disabled)
- ❌ Centinela — effect 202 (reveals invisible entities)

</details>

<details><summary><b>Sadida</b> — 0 of 44 seen working, 31 resolve on paper</summary>

- ❌ La Loca
- ❌ La Loca Transmutada
- ❌ Árbol
- ❌ Árbol Frondoso
- ❌ Zarza
- ❌ Zarza Insolente
- ❌ Plaga — shape `G`
- ❌ Bosque Encantado — shape `G`
- ❌ La Bloqueadora
- ❌ La Bloqueadora Transmutada
- ❌ Lágrima de Sadida — effect 786 (heals the attacker for #1% of the damage)
- ❌ Subida de Savia — effect 2973 (heals #1-#2% of the damage dealt), effect 786 (heals the attacker for #1% of the damage), shape `G`
- ❌ Savia Paralizante
- ❌ Miasmas
- ❌ Zarza Tranquilizadora — effect 3002 (#1-#2 best-element healing)
- ❌ Trasplante
- ❌ Potencia Silvestre — effect 2796 (kills the target and replaces it with summon: #1)
- ❌ Influencia Vegetal — effect 2796 (kills the target and replaces it with summon: #1)
- ❌ La Sacrificada
- ❌ La Sacrificada Transmutada
- ❌ Temblor — effect 202 (reveals invisible entities)
- ❌ Mandrágora
- ❌ Don Natural — effect 1061 (shares damage), effect 3002 (#1-#2 best-element healing), mask `d`
- ❌ Armonía — effect 1061 (shares damage)
- ❌ Sacrificio Vudú — effect 1045 (#1: cooldown pinned to #3 turns)
- ❌ Cardos Ardientes
- ❌ Contagio
- ❌ Manglar — effect 786 (heals the attacker for #1% of the damage)
- ❌ Inoculación
- ❌ Fuerza de la Naturaleza
- ❌ La Hinchable
- ❌ La Hinchable Transmutada
- ❌ Zarzas Agresivas
- ❌ Fetiches Calcinados — effect 1223 (damage: #1-#2% of the final damage taken)
- ❌ Árbol de Vida
- ❌ Altruismo Vegetal
- ❌ Matorral Ardiente
- ❌ Fuego Montés
- ❌ Cicuta
- ❌ Viento Envenenado
- ❌ Hierbas Locas
- ❌ Maldición Vudú
- ❌ La Superpoderosa
- ❌ La Superpoderosa Transmutada

</details>

<details><summary><b>Sacrógrito</b> — 0 of 44 seen working, 35 resolve on paper</summary>

- ❌ Mutilación
- ❌ Pacto de Sangre
- ❌ Espada Voraz
- ❌ Espada Bailarina
- ❌ Rapapolvo
- ❌ Fulgor
- ❌ Asalto
- ❌ Aversión
- ❌ Transposición
- ❌ Fluctuación
- ❌ Condensación
- ❌ Aflujo
- ❌ Hostilidad
- ❌ Proyección
- ❌ Corona de Espinas — effect 1223 (damage: #1-#2% of the final damage taken)
- ❌ Picota
- ❌ Transfusión — effect 89 (neutral damage: #1-#2% of the caster HP)
- ❌ Lazos de Sangre
- ❌ Hecatombe
- ❌ Corte
- ❌ Baño de Sangre — shape `G`
- ❌ Inmolación
- ❌ Sacrificio — effect 765 (intercepts damage)
- ❌ Penitencia
- ❌ Desolación
- ❌ Desencadenamiento — shape `G`
- ❌ Disolución
- ❌ Carnicería
- ❌ Libación
- ❌ Castigo — effect 89 (neutral damage: #1-#2% of the caster HP)
- ❌ Berserker
- ❌ Ritual de Jashin — effect 1223 (damage: #1-#2% of the final damage taken)
- ❌ Absorción
- ❌ Furia
- ❌ Suplicio — effect 786 (heals the attacker for #1% of the damage)
- ❌ Nerviosismo
- ❌ Estasis
- ❌ Escozor
- ❌ Atracción
- ❌ Perfusión — effect 2020 (heals #1-#2% of the damage taken)
- ❌ Punición
- ❌ Locura Sanguinaria
- ❌ Hemorragia
- ❌ Aniquilamiento

</details>

<details><summary><b>Pandawa</b> — 0 of 44 seen working, 35 resolve on paper</summary>

- ❌ Palma Explosiva — effect 296 (#1: +#3 AP)
- ❌ Destilación — shape `G`
- ❌ Resaca
- ❌ Soplido Flamígero
- ❌ Comilona
- ❌ Tranka
- ❌ Terror
- ❌ Consuelo
- ❌ Ventolera
- ❌ Jarana
- ❌ Karcham
- ❌ Chamrak
- ❌ Ola Marejadora
- ❌ Pandjiu
- ❌ Desalojo
- ❌ Soplido Alcoholizado
- ❌ Ebriedad
- ❌ Embriaguez
- ❌ Estabilización
- ❌ Escalada — effect 290 (#1: +#3 cast(s) per turn), effect 291 (#1: +#3 lanzamiento(s) per objetivo)
- ❌ Enlace Espirituoso
- ❌ Bambú
- ❌ Etilo
- ❌ Entumecimiento
- ❌ Aguardiente — mask `K`
- ❌ Aguachirle
- ❌ Deshonra
- ❌ Maceración
- ❌ Fermentación
- ❌ Bambusería
- ❌ Propulsión — mask `K`
- ❌ Absenta
- ❌ Camilla — mask `K`
- ❌ Alcoshu
- ❌ Pandikulación — mask `K`
- ❌ Licor
- ❌ Náuseas
- ❌ Cascada — mask `K`
- ❌ Leche de Bambú
- ❌ Interdicción
- ❌ Frasco Explosivo
- ❌ Pandatak
- ❌ Pandenkulo
- ❌ Mano de Pandawa — effect 297 (#1: casilla ocupada necesaria desactivada), effect 299 (#1: casilla libre necesaria activada)

</details>

<details><summary><b>Tymador</b> — 13 of 44 seen working, 42 resolve on paper</summary>

- ✅ Detonador
- ❌ Estopín
- ✅ Explobomba
- ❌ Explobomba Resiliente
- ✅ Tornabombas
- ❌ Tornabomba Resiliente
- ❌ Patada
- ❌ Ardid
- ❌ Extracción
- ❌ Cadencia
- ❌ Imantación — mask `b12`; does nothing on an empty cell, as it should
- ❌ Cruce
- ✅ Fusil
- ❌ Obliteración
- ❌ Jugarreta
- ❌ Bomba Ambulante — mask `U`
- ✅ Bombas de agua
- ❌ Bomba de Agua Resiliente
- ✅ Tymobot — and its own Empujoncito, Aspirador and Pinzas, driven from the owner's client; it dies at the end of its turn
- ❌ Megabomba
- ❌ Bombardeo
- ❌ Metralla
- ❌ Receptación
- ❌ Emplomado
- ✅ Tymadura — byte for byte against its capture
- ❌ Argucia
- ❌ Púlsar
- ❌ Perdigonazo
- ✅ Remisión
- ❌ Búnker
- ❌ Dagas Bumerán
- ❌ Tromba
- ❌ Polvo
- ❌ Bomba Pegajosa
- ✅ Kabúm
- ❌ Impostura
- ✅ Último Aliento
- ❌ Trampa Magnética
- ✅ Mosquete
- ❌ Granalla
- ✅ Colado
- ❌ Arcabuz
- ✅ Sismobomba
- ❌ Sismobomba Resiliente

</details>

<details><summary><b>Zobal</b> — 0 of 44 seen working, 37 resolve on paper</summary>

- ❌ Boliche
- ❌ Ronda
- ❌ Catalepsia
- ❌ Apostasía
- ❌ Máscara Eskérdikat — effect 1036 (#1: -#3 cooldown), effect 1045 (#1: cooldown pinned to #3 turns), mask `b14`
- ❌ Máscara de Cobarde — effect 1036 (#1: -#3 cooldown), effect 1045 (#1: cooldown pinned to #3 turns), mask `b14`
- ❌ Brincadeira
- ❌ Picado
- ❌ Apoyo
- ❌ Pivote
- ❌ Máscara Sáikopat — effect 1036 (#1: -#3 cooldown), effect 1045 (#1: cooldown pinned to #3 turns), mask `b14`
- ❌ Máscara de Histérico — effect 1036 (#1: -#3 cooldown), effect 1045 (#1: cooldown pinned to #3 turns), mask `b14`
- ❌ Furial
- ❌ Bocciara
- ❌ Cabriola
- ❌ Purgatorio
- ❌ Tortoruga
- ❌ Armaduro
- ❌ Esprín
- ❌ Scudo
- ❌ Apatía
- ❌ Retención
- ❌ Coraza
- ❌ Ginga
- ❌ Fogosidad
- ❌ Mascarada — effect 279 (damage neutrales: #1-#2% HP faltantes of the lanzador), effect 89 (neutral damage: #1-#2% of the caster HP)
- ❌ Desbandada
- ❌ Comedia
- ❌ Parafuso
- ❌ Martelo
- ❌ Ponteira
- ❌ Agular
- ❌ Trance
- ❌ Neurosis
- ❌ Cabalgata
- ❌ Reclamo
- ❌ Infernus
- ❌ Distancia
- ❌ Carnavalo
- ❌ Transfiguración
- ❌ Máscara de Intrépido — effect 1036 (#1: -#3 cooldown), effect 1045 (#1: cooldown pinned to #3 turns), mask `b14`
- ❌ Máscara de Incansable — effect 1036 (#1: -#3 cooldown), effect 1045 (#1: cooldown pinned to #3 turns), mask `b14`
- ❌ Mueca
- ❌ Difracción

</details>

<details><summary><b>Steamer</b> — 0 of 44 seen working, 25 resolve on paper</summary>

- ❌ Torpedo
- ❌ Timón — shape `*`
- ❌ Catalejo
- ❌ Corrosión
- ❌ Amarre
- ❌ Ventalla — effect 1036 (#1: -#3 cooldown), effect 2027 (Toma el control de la entidad)
- ❌ Evolución — effect 2027 (Toma el control de la entidad)
- ❌ Sobretensión — effect 2027 (Toma el control de la entidad)
- ❌ Albarrama — effect 765 (intercepts damage)
- ❌ Recursividad — effect 1023 (Intercambio de posiciones (forzado)), effect 2017 (#1)
- ❌ Escafandra
- ❌ Blindaje
- ❌ Anclaje
- ❌ Cortocircuito — effect 2027 (Toma el control de la entidad)
- ❌ Guardianas
- ❌ Perforadora
- ❌ Corriente
- ❌ Harmatán
- ❌ Sabotaje — effect 1036 (#1: -#3 cooldown), effect 2027 (Toma el control de la entidad)
- ❌ Periscopio
- ❌ Asistencia
- ❌ Derivación — effect 1023 (Intercambio de posiciones (forzado))
- ❌ Aspiración
- ❌ Pistón
- ❌ Tactiquillas
- ❌ Batiscafo
- ❌ Arponeras
- ❌ Arrastrero
- ❌ Socorrismo — effect 3002 (#1-#2 best-element healing)
- ❌ Salvamento
- ❌ Tridente — shape `F`
- ❌ Cabestrante — shape `G`
- ❌ Sónar — effect 202 (reveals invisible entities)
- ❌ Emboscada
- ❌ Compás — effect 1023 (Intercambio de posiciones (forzado))
- ❌ Brújula — effect 1023 (Intercambio de posiciones (forzado))
- ❌ Turbina — effect 2027 (Toma el control de la entidad)
- ❌ Piratería — shape `G`
- ❌ Buceo
- ❌ Zambullida
- ❌ Resacón
- ❌ Espuma de Mar
- ❌ Marea — effect 1045 (#1: cooldown pinned to #3 turns), effect 290 (#1: +#3 cast(s) per turn), effect 2905 (#1: alcance máximo fijado en #3), effect 2906 (#1: alcance mínimo fijado en #3)
- ❌ Vapor — effect 2027 (Toma el control de la entidad)

</details>

<details><summary><b>Selatrop</b> — 0 of 44 seen working, 11 resolve on paper</summary>

- ❌ Portal — effect 1181 (places a portal (+#3% damage, +#1% per cell travelled))
- ❌ Errancia — effect 1181 (places a portal (+#3% damage, +#1% per cell travelled))
- ❌ Insulto — mask `r`
- ❌ Desprecio
- ❌ Audacia — mask `R`, mask `r`
- ❌ Tribulación
- ❌ Shock — mask `R`, mask `r`
- ❌ Convulsión
- ❌ Rayo de Wakfu
- ❌ Resplandor — mask `R`, mask `r`
- ❌ Neutral — effect 1183 (deactivates a portal)
- ❌ Interrupción — effect 1183 (deactivates a portal)
- ❌ Afrenta — mask `R`
- ❌ Aplomo — mask `R`
- ❌ Trascendencia — effect 290 (#1: +#3 cast(s) per turn)
- ❌ Exilio — effect 1181 (places a portal (+#3% damage, +#1% per cell travelled)), effect 1182 (Teleportal)
- ❌ Terapia
- ❌ Puño Relámpago — mask `R`, mask `r`
- ❌ Distribución — effect 2973 (heals #1-#2% of the damage dealt)
- ❌ Soberbia — effect 1223 (damage: #1-#2% of the final damage taken)
- ❌ Estela — effect 1181 (places a portal (+#3% damage, +#1% per cell travelled))
- ❌ Estupor — effect 1181 (places a portal (+#3% damage, +#1% per cell travelled))
- ❌ Acoso — mask `R`, mask `r`
- ❌ Cataclismo — effect 2973 (heals #1-#2% of the damage dealt), shape `G`
- ❌ Sanación
- ❌ Conjuro — mask `R`
- ❌ Insolencia — mask `R`, mask `r`
- ❌ Desdén — mask `R`
- ❌ Odisea
- ❌ Éxodo
- ❌ Cábala — mask `R`, mask `r`
- ❌ Resiliencia — mask `R`
- ❌ Aflicción — mask `R`
- ❌ Ofensiva
- ❌ Resonancia — effect 1181 (places a portal (+#3% damage, +#1% per cell travelled)), effect 1182 (Teleportal)
- ❌ Vestigio
- ❌ Mofa — effect 320 (steals #1-#2 range), mask `R`, mask `r`
- ❌ Sinecura — mask `R`
- ❌ Extinción — mask `R`
- ❌ Sermón — mask `R`, mask `r`
- ❌ Ridículo — mask `R`
- ❌ Sarcasmo — mask `R`
- ❌ Ayuda Mutua
- ❌ Coalición — mask `R`, mask `r`

</details>

<details><summary><b>Hipermago</b> — 0 of 44 seen working, 8 resolve on paper</summary>

- ❌ Onda Sísmica — effect 320 (steals #1-#2 range)
- ❌ Tizón — effect 320 (steals #1-#2 range)
- ❌ Éter — effect 320 (steals #1-#2 range)
- ❌ Catarata — effect 320 (steals #1-#2 range)
- ❌ Runificación — effect 2023 (triggers runes)
- ❌ Manifestación — effect 2023 (triggers runes)
- ❌ Lanzallamas — effect 320 (steals #1-#2 range)
- ❌ Lanzas Telúricas — effect 320 (steals #1-#2 range)
- ❌ Estalagmita — effect 320 (steals #1-#2 range)
- ❌ Onda Celeste — effect 320 (steals #1-#2 range)
- ❌ Tormenta — effect 320 (steals #1-#2 range)
- ❌ Huracán — effect 320 (steals #1-#2 range)
- ❌ Lanza Solar — effect 320 (steals #1-#2 range)
- ❌ Cometa — effect 320 (steals #1-#2 range)
- ❌ Polaridad
- ❌ Convección
- ❌ Trazo Flamígero — effect 320 (steals #1-#2 range)
- ❌ Estalactita — effect 320 (steals #1-#2 range)
- ❌ Glaciar — effect 320 (steals #1-#2 range)
- ❌ Volcán — effect 320 (steals #1-#2 range)
- ❌ Propagación — effect 320 (steals #1-#2 range)
- ❌ Prisma Rúnico — effect 2023 (triggers runes)
- ❌ Escudo Elemental — effect 320 (steals #1-#2 range), mask `H`, mask `o`
- ❌ Guardián Elemental
- ❌ Hoja Astral — effect 320 (steals #1-#2 range)
- ❌ Deflagración — effect 320 (steals #1-#2 range)
- ❌ Contribución
- ❌ Impronta — effect 798 (#1: objetivo visible necesario activado)
- ❌ Diluvio — effect 320 (steals #1-#2 range)
- ❌ Asteroide — effect 320 (steals #1-#2 range)
- ❌ Sobrecarga Rúnica — effect 2023 (triggers runes)
- ❌ Sublimación — effect 320 (steals #1-#2 range)
- ❌ Ráfaga — effect 320 (steals #1-#2 range)
- ❌ Brecha — effect 320 (steals #1-#2 range)
- ❌ Meteoro — effect 320 (steals #1-#2 range)
- ❌ Avalancha — effect 320 (steals #1-#2 range)
- ❌ Ciclo Elemental — effect 320 (steals #1-#2 range)
- ❌ Corriente Cuadramental — effect 320 (steals #1-#2 range)
- ❌ Travesía
- ❌ Repulsión Rúnica — effect 2023 (triggers runes)
- ❌ Drenaje Elemental
- ❌ Tributo
- ❌ Supernova — effect 320 (steals #1-#2 range)
- ❌ Torrente Arcano

</details>

<details><summary><b>Uginak</b> — 0 of 44 seen working, 4 resolve on paper</summary>

- ❌ Convergencia
- ❌ Busca
- ❌ Presa — effect 1045 (#1: cooldown pinned to #3 turns), effect 786 (heals the attacker for #1% of the damage)
- ❌ Animal de Caza — effect 1045 (#1: cooldown pinned to #3 turns)
- ❌ Moloso — effect 2905 (#1: alcance máximo fijado en #3), effect 2906 (#1: alcance mínimo fijado en #3), shape `G`
- ❌ Mandíbula — effect 2905 (#1: alcance máximo fijado en #3), effect 2906 (#1: alcance mínimo fijado en #3), shape `G`
- ❌ Cúbito — shape `G`
- ❌ Calcáneo — shape `G`
- ❌ Carcasa — shape `G`
- ❌ Batida — shape `G`
- ❌ Ojeo — effect 77 (Roba #1-#2 MP), shape `G`
- ❌ Ladrar — shape `G`
- ❌ Amaine — effect 2905 (#1: alcance máximo fijado en #3), effect 2906 (#1: alcance mínimo fijado en #3)
- ❌ Afección — effect 2905 (#1: alcance máximo fijado en #3), effect 2906 (#1: alcance mínimo fijado en #3)
- ❌ Lanzagozquetes — mask `U`
- ❌ Gangrena — effect 2905 (#1: alcance máximo fijado en #3), effect 2906 (#1: alcance mínimo fijado en #3)
- ❌ Dogo — shape `G`
- ❌ Restos — shape `G`
- ❌ Tibia — effect 2905 (#1: alcance máximo fijado en #3), effect 2906 (#1: alcance mínimo fijado en #3), shape `G`
- ❌ Húmero — effect 2905 (#1: alcance máximo fijado en #3), effect 2906 (#1: alcance mínimo fijado en #3), shape `G`
- ❌ Rastreo — shape `G`
- ❌ Despiece — effect 2905 (#1: alcance máximo fijado en #3), effect 2906 (#1: alcance mínimo fijado en #3), shape `G`
- ❌ Sabueso — shape `G`
- ❌ Tetanización — effect 2905 (#1: alcance máximo fijado en #3), effect 2906 (#1: alcance mínimo fijado en #3), shape `G`
- ❌ Arcanino — effect 2905 (#1: alcance máximo fijado en #3), effect 2906 (#1: alcance mínimo fijado en #3)
- ❌ Caninos — effect 2905 (#1: alcance máximo fijado en #3), effect 2906 (#1: alcance mínimo fijado en #3)
- ❌ Pelaje Protector — effect 2905 (#1: alcance máximo fijado en #3), effect 2906 (#1: alcance mínimo fijado en #3)
- ❌ Ferocidad
- ❌ Carroña — effect 2905 (#1: alcance máximo fijado en #3), effect 2906 (#1: alcance mínimo fijado en #3), shape `G`
- ❌ Radio — effect 2905 (#1: alcance máximo fijado en #3), effect 2906 (#1: alcance mínimo fijado en #3), shape `G`
- ❌ Hueso con Tuétano — shape `G`
- ❌ Bozal — effect 1019 (#1), shape `G`
- ❌ Pánico
- ❌ Caza — effect 1165 (places to glyph)
- ❌ Amarok — shape `G`
- ❌ Cerbero — shape `G`
- ❌ Ladrido — effect 2905 (#1: alcance máximo fijado en #3), effect 2906 (#1: alcance mínimo fijado en #3)
- ❌ Enojo — effect 2905 (#1: alcance máximo fijado en #3), effect 2906 (#1: alcance mínimo fijado en #3)
- ❌ Cacería — shape `G`
- ❌ Vértebra — shape `G`
- ❌ Olfacción — effect 2905 (#1: alcance máximo fijado en #3), effect 2906 (#1: alcance mínimo fijado en #3)
- ❌ Ensañamiento — effect 2905 (#1: alcance máximo fijado en #3), effect 2906 (#1: alcance mínimo fijado en #3)
- ❌ Clamor de la Manada — effect 1036 (#1: -#3 cooldown)
- ❌ Luna Nueva — effect 2905 (#1: alcance máximo fijado en #3), effect 2906 (#1: alcance mínimo fijado en #3)

</details>

<details><summary><b>Forjalanza</b> — 0 of 44 seen working, 33 resolve on paper</summary>

- ❌ Lanza del Lago
- ❌ Chuzo Sísmico — shape `R`
- ❌ Lanzapiedras
- ❌ Jabalina Rayo
- ❌ Epílogo
- ❌ Anticipación
- ❌ Lanza de Incendios
- ❌ Lluvia Dorena — shape `G`
- ❌ Carga Heroica
- ❌ Galantería
- ❌ Colapso — shape `*`
- ❌ Lanza Ciclón
- ❌ Al Tridente — shape `F`
- ❌ Maelstrom
- ❌ Falange
- ❌ Oriflama
- ❌ Estocada Ardiente
- ❌ Octava
- ❌ Golpiza de Bronce
- ❌ Sublevación
- ❌ Balestra
- ❌ Molino de Viento
- ❌ Talón de Barro
- ❌ Posición de Fondo
- ❌ Kyrja
- ❌ Vajra
- ❌ Muspel
- ❌ Ydra — shape `*`
- ❌ Punzón
- ❌ Abrazo de Valquíride — mask `H`
- ❌ Tierra Media — shape `G`
- ❌ Despeje
- ❌ Caballería
- ❌ Renombre
- ❌ Jormun
- ❌ Cadena Candente — shape `G`
- ❌ Preludio al Hierro
- ❌ Crepúsculo
- ❌ Noa — shape `G`
- ❌ Elding
- ❌ Eclipse — effect 1036 (#1: -#3 cooldown), effect 289 (#1: line of sight disabled), effect 2905 (#1: alcance máximo fijado en #3), effect 2906 (#1: alcance mínimo fijado en #3), effect 299 (#1: casilla libre necesaria activada), effect 314 (#1: casilla ocupada necesaria activada)
- ❌ Holmgang — effect 2018 (dispels glyphs)
- ❌ Jabalina Keatina
- ❌ Molino Rojo

</details>

### 🎯 Combat challenges

- ✅ The preparation phase: two candidates with a 15-second timer, the player marks and validates, and the server fixes whatever is left when you declare ready
- ✅ **15 of the 16** watched live, with every rule taken from the challenge's own translated description
- ✅ Results travel the moment they happen — a failure the instant the challenge breaks, a success at the end, a defeat failing them all at once
- ✅ The bonus is folded into experience, kamas and drop rates on a win
- ✅ Dungeon and anomaly challenges are imposed at 0% and carry achievements, written once and never offered again
- ❌ *Hired Killer* (35), which needs the server to designate and re-designate the target
- ❌ Challenges without a known percentage: the client ships no bonus field for them

### ❌ Not implemented at all

- Achievements

---

## 🛠️ Jondo Studio
<img width="2560" height="1508" alt="image" src="https://github.com/user-attachments/assets/14ee4541-d473-4bd1-81dd-617d03c8ba82" />
<img width="2558" height="1502" alt="image" src="https://github.com/user-attachments/assets/21917c8d-4e7a-43a1-a7bc-73dddee31137" />
<img width="2558" height="1496" alt="image" src="https://github.com/user-attachments/assets/39afe77a-c451-43a2-a67b-8ab5e6abe4c4" />
<img width="2558" height="1508" alt="image" src="https://github.com/user-attachments/assets/c139b38c-232d-4a58-9f45-572e643ccd93" />

> ⚠️ **Very early.** The Studio changes often. Keep a copy of `content/` before a long session. Nothing
> in it can damage `world.db` or a running server.

The world editor. A third executable next to the launcher and the server, and it needs neither of
them running: it opens `content/` and the data files through the same paths the server uses. Built
with **Avalonia**, so it runs on Windows, macOS and Linux.

It unpacks `world.db` from `datos/world.zip` the first time it runs, the way the server does.

The client holds every item, spell and monster, but not what the real server decided: which reply in
a dialogue leads to which line, where an NPC stands and what it does there, which interactive
teleport comes back to which map. Those have to be authored, and the Studio is where.

### Three layers, and every row says where it came from

The data lives in three places: `dofus3_data/` is a raw dump of the client, `datos/*.json` is
regenerated by the tools in `tools/`, and `world.db` is a 240 MB binary. Only the last layer is ever
edited:

| layer | where from | who edits it |
|---|---|---|
| **base** | generated from the client dump | nobody |
| **measured** | learned from packet captures | nobody |
| **authored** | decided by a person | this is the one, and it always wins |

The authored layer is `content/`, in versioned JSON, so a change is a reviewable diff. It stores
deltas, not copies, and it can erase a row it did not write. Every row carries its provenance.

### What it does today

Nine sections, **in Spanish, English or French** — the language switch changes both the editor's own
words and the game's, which are read straight out of the client's `Content/I18n/{lang}.bin`, 339,342
texts per language.

The creatures are drawn out of the client's own bundles. Monsters come from a picto atlas, 5,130 of
the 5,134 covered. NPCs are assembled the way the client assembles them: bones, a still frame, and
the skins the look names. That renderer lives in `Jondo.Unity.Sprites`, and the launcher draws its
account portraits with it.

- ✅ **Overview** — which files it read and what came out of each
- ✅ **Traffic** — the client-server conversation, live and back through the log, every frame read against the protocol the client itself declares. A packet can be named on the spot, from the **513 real message names** the client ships in its metadata
- ✅ **Packets** — every kind of packet seen, with a status ladder: unknown, named, documented, handled, ignored
- ✅ **NPCs** — all 422 placements, with the provenance column and the NPC drawn on the map
- ✅ **Dialogues** — which reply leads to which line, with the text on screen
- ✅ **Monsters** — open a group, take a monster out, put another in, move it
- ✅ **Spells** — every spell with its effects, and the map showing how far it reaches and what it would hit, computed by the fight engine's own `Zone.Casillas`
- ✅ **Passages** — two maps side by side, a door picked on each, and one button that joins them both ways
- ✅ **Map cells** — the three layers painted one at a time, click to toggle and drag to paint a run
- ✅ A section that fails shows its error inside the editor, and `Jondo Studio.exe --selftest` builds all nine in all three languages against the real data

Everything it writes goes to `content/`. Nothing opens `world.db` for writing and nothing talks to a running server.

### What is being worked on

- 🚧 NPC actions per placement
- 🚧 Editing spells: the simulator is there; changing a spell's numbers is not
- 🚧 Shops, loot tables and dungeons
- 🚧 Editing quests: the engine plays them and the Studio shows them, but nothing writes one yet
- 🚧 A thin admin channel so a running server can be told to reload one domain, without a restart

The full plan is in **`docs/world-editor.md`**.

---

## 🧪 Tests

`Jondo.Unity.Tests` — **1,491 xUnit tests**, grouped by domain: `Auth`, `Combat`, `Commands`,
`Content`, `Diagnostics`, `Economy`, `Launcher`, `Movement`, `Network`, `Protocol`, `Quests`,
`Security`, `Sessions`, `Sprites`, `Studio`, `World`. They run in about half a minute.

```bash
dotnet test Jondo.Unity.Tests
```

A few of them run against `logs/gameserver_traffic.log` and `bases/world.db` when they are on the
machine, and skip when they are not.

**Publishing the server runs them first and fails if any is red.** The escape hatch is
`-p:SkipTests=true`.

### Three kinds of check, three homes

* **At startup** run the questions of the form *"is the data I was shipped sane?"* — the fight
  sheet's 53 characteristics in their order, the interactive registry, the monster spellbooks, the
  vendor placements, the profession catalogue. The server refuses to boot when one fails.
* **In the test project** live the questions of the form *"is this code correct?"* — the content
  layers, the collision damage formula, the Jondo Coin bands, frame limits, protobuf parsing,
  password hashing, log censorship, session isolation, and frames compared byte for byte against
  captures.
* **Architecture tests** ask *"is this code shaped right?"* — they read the fight engine's own
  source and fail on the shapes a multi-client engine cannot afford, with an exception list where
  every entry carries a written reason.

Portraits are checked by counting: the animation name has to end in the direction that faces the
camera, and the head slot has to contribute more than zero triangles.

---

## 🔎 Surviving the next patch

Every protobuf message in Dofus 3 is named with three random letters — `kub`, `jru`, `lqu` — and on some patches Ankama reshuffles the lot. Nothing else about the protocol changes shape. **`protocolbuilder`** is the command line for that; **`Jondo Desofuscador.exe`** is the same engine behind one window and one button.

Eight consecutive real clients (3.6.4.3 → 3.6.10.10) were compared patch by patch:

- Ankama does not reshuffle on every patch: three of the seven jumps keep all 2,169 names, one for one. The tool checks for the identity mapping first.
- The matcher never looks at names, only at field numbers, kinds and neighbourhood. It resolves 71.1% of pairs with zero wrong pairings over 6,505; what it cannot decide, it leaves alone.
- On a patch that does reshuffle, structure alone resolves about 11%.
- Chaining through intermediate versions is worse than the direct jump.
- 49 opcodes only exist in 3.6.4.3.

The **`Op` layer** is one generated file, `Jondo.Unity.Protocol/Op.cs`, so applying a mapping never means editing the emulator by hand.

```bash
protocolbuilder proto    <client dll> [out.proto]      the client's own message shapes
protocolbuilder mapear   <old client> <new client>     who is who between two versions
protocolbuilder capa     <client> <anchors> . --aplicar  regenerate Op.cs and migrate call sites
protocolbuilder bajar    3.6.4.3 3.6.10.10 clientes    fetch old clients from the CDN, 183 MB each
protocolbuilder cadena   clientes                      measure each patch on its own
```

> `proto` also settles what a message carries from the client's own schema: `lth { bool, bool }` is two booleans.

Full write-up in `docs/desofuscacion.md`.

---

## 🧱 Source layout

The three executables:
* **`Jondo.Unity.Server`** → `Jondo Server.exe` — proxies, network parser, handlers, managers, database and the server's log window. The spell effect engine lives in `Managers/`: `SpellEffects` reads the spell data, `EffectEngine` applies it, and `Summons` builds summoned fighters from monster templates
* **`Jondo.Unity.Launcher`** → `Jondo Emulator Launcher.exe` — the player's window, in Avalonia. References the contract and the sprite renderer
* **`Jondo.Unity.Studio`** → `Jondo Studio.exe` — the world editor, in Avalonia

Shared:
* **`Jondo.Unity.Contract`** — paths, settings and the shared palette
* **`Jondo.Unity.Contract.WinForms`** — the Windows Forms shell of the server window
* **`Jondo.Unity.Core`** — networking infrastructure and TCP servers
* **`Jondo.Unity.Auth`** — authentication and HAAPI handlers
* **`Jondo.Unity.Protocol`** — message definitions and the generated `Op` layer
* **`Jondo.Unity.World`** — world logic, `FightInstance`, the fight rulebooks (`FightRules`), buffs and states (`Buff`), area shapes and displacement (`Zone`), isometric geometry (`MapGeometry`), the criterion evaluator (`Criterion`), raids and the kanojedo content
* **`Jondo.Unity.Sprites`** — draws a character or an NPC out of the client's own bones, skins and atlases. Shared by the Studio and the launcher
* **`Jondo.Unity.Cytrus`** — reads Ankama's Cytrus manifests and plans the bundle requests; the launcher's texture-pack download uses it
* **`Jondo.Unity.Parser`** — capture parsing
* **`Jondo.Unity.Tests`** — the xUnit tests, and the gate on publishing

The protocol toolchain, which the emulator does not depend on:
* **`Jondo.Unity.Reversing`** — reads a client with Cpp2IL, rebuilds the `.proto`, matches two versions, indexes the code, downloads old clients from the CDN (`Cytrus`) and generates the `Op` layer
* **`Jondo.Unity.ProtocolBuilder`** → `protocolbuilder` · **`Jondo.Unity.Deobfuscator`** → `Jondo Desofuscador.exe`
* **`JondoFix`** — the MelonLoader client mod, source plus the compiled dll

Documentation index in `docs/README.md`. Start with `docs/protocol.md` (how a message travels), `docs/opcodes.md` (what each opcode means and where it was seen), `docs/fight.md` (a fight on the wire, opcode by opcode) and `docs/desofuscacion.md` (surviving a patch).

---

## 💾 Database and persistence

Three **SQLite** databases in `bases/`, and one folder of text:

* **`world.db`** — characters, inventories, positions, map persistence, spells, monsters, appearances, wardrobe, haven bags, guilds and raids. Distributed compressed as `datos/world.zip` (24.8 MB) and extracted on first run.
* **`auth.db`** — accounts and authentication sessions, created on first run.
* **`paquetes.db`** — the packets the server does not yet know how to answer, deduplicated by protobuf shape. It carries nothing needed to play and can be deleted to start over.
* **`content/`** — the authored layer, in versioned JSON. The only one edited by hand, and the only one nothing regenerates. See [Jondo Studio](#-jondo-studio).

Files are looked up in `datos/`, then `bases/`, then the root.

Some regression guards run at startup and throw, so the server refuses to boot when the data it was shipped does not match what the code expects — see [Tests](#-tests).
