# ![ResourcefulHands [W.I.P]](https://file.garden/Z9BrE5QDFXkNPYaw/rhlogobanner_trimmed_big.png "ResourcefulHands [W.I.P]")

#### __Contributors:__
 - triggered0idiot \[creator\]
 - Collin8000
 - galfar-coder
 - notTamion

***ResourcefulHands is resource pack mod for White Knuckle*** that is designed to be a user friendly alterative to editing game files. ResourcefulHands works both on the current early access build and the demo as of writing.

## UI
ResourcefulHands can handle several user made packs at the same time with order re-arranging.
# ![ResourcefulHands Packs UI](https://file.garden/Z9BrE5QDFXkNPYaw/rh_showcase3.png "ResourcefulHands Packs UI")

## Commands
Additionally, ResourcefulHands comes with built in commands to use if you prefer:
  - dumptopack -> Dumps all game assets to a dummy pack, use to assist in replacing files.
  - reloadpacks -> Reloads all loaded packs and ingame assets.
  - reorderpack -> Moves a pack in the load order.
  - listpacks -> Lists every pack.
  - enablepack -> Enables a pack's contents.
  - disablepack -> Disables a pack's contents.
  - rhtoggledebug -> toggles a menu at the top left that shows playing sounds
  - assignhandpack -> Assigns a texture pack to a specific hand (left/right)
  - clearhandpack -> Clears a hand's assigned texture pack
  - listhandpacks -> Shows current hand texture pack assignments 

## Resource Pack Structure
--> Textures/<br>
any .png or .jpg file with the same name as an ingame texture will replace it<br>
currently the custom texture should try to be in the same format as the original texture or else it may not work correctly<br>
<sub><sup> *currently the mass has special properties, to edit its texture use DeathFloor_02 for the mass and _CORRUPTTEXTURE for the effect it does* </sup></sub><br>
for hand-specific textures, prefix files with Left_ or Right_ (e.g., Left_Hands_Sprite_library.png)<br>

--> Sounds/<br>
any .mp3, .wav or .ogg file with the same name as an ingame sound will replace it<br>
currently the custom sound should try to be in the same format as the original sound or else it may not work correctly<br>

<br>
--> pack.png<br>
pack icon for the new settings menu<br>
--> info.json<br>
tells the mod information about your pack<br>

```json
{
    "name":"your pack name here",
    "desc":"your description here",

    "author":"your name here",
    "steamid":0,

    "guid":"some.unqiue.text.dont.change.between.versions",
    "only-in-full-game":false,
    
    "textures-folder":"Textures",
    "sounds-folder":"Sounds",
    "icon-file":"pack.png",
    
    "format-version":2
}
```

## Feature comparison
| Feature                        | Bundle editing | ResourcefulHands  |
| ------------------------------ |:--:|:--:|
| Swapping hand sprites          | ✔️ | ✔️ |
| Swapping environment textures  | ✔️ | ~ |
| Swapping mass textures         | ✔️ | ✔️ |
| Swapping models                | ✔️ | ❌ |
| Swapping shaders               | ✔️ | ❌ |
| Supports game updates          | ❌ | ~ |
| Cross compatable with demo     | ❌ | ~ |
| Hot reloading                  | ❌ | ~ |
###### ✔️ = feature is fully functional as of writing<br>❌ = feature is broken or doesn't exist as of writing<br>~ = feature somewhat works but there may be bugs

## Planned features
| Feature                        | ResourcefulHands  |
| ------------------------------ |:--:|
| Swapping hand sprites          | ✔️ |
| Swapping environment textures  | ~ |
| Swapping mass textures         | ✔️ |
| Swapping models                | ❌ |
| Swapping shaders               | ❌ |
| Supports game updates          | ~ |
| Cross compatable with demo     | ~ |
| Hot reloading                  | ~ |
| Efficient reloads              | ✔️ |
| Pack management UI             | ~ |
| Pack debugging tools           | ~ |
###### ✔️ = feature is fully functional as of writing<br>❌ = feature doesn't exist as of writing<br>~ = feature somewhat works but needs to be implemented/tested fully
