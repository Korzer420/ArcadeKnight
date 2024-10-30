# ArcadeKnight
A Hollow Knight mod that offers some minigames with custom stage support.

This mod is a collection of small minigames with each one having its own rules, scores, maps etc.

## How do I play?
In certain spots in the overworld you can open a selection menu for a specific minigame (for the exact position, read the individual minigame segment). There you can select a level via left and right, as well as selecting the difficulty with up or down.
Pressing confirm will then bring you into the minigame map, where you objective depends on the selected game. Each difficulty level and different stage has its own highscores that will be tracked. Besides the basic ones, you can also play custom levels (Look under "Custom Level" for more info.

## General Minigame rules
Before talking about each individual minigame, here are few general information:
- All transitions in the minigame room are disabled and replaced by hazard respawn (although they deal no damage)
- Dream Gate is disabled while in a minigame.
- A minigame grants by default: Mothwing Cloak, Mantis Claw, Monarch Wings, Isma's Tear and Crystal Heart. (This will be reset to the original states upon leaving the minigame.) Depending on the course configuration this might differ.
- Each minigame places a Dream Gate sprite at the start where you spawn. You can always exit through this sprite to cancel the minigame (your progress will be lost).
- You can disable the camera preview at the start of minigames in the settings menu of the mod.
- You can benchwarp out of a minigame sequence safely. It will cancel the minigame like the dream gate described above.
- All normal hazard respawns are disabled.
- The shade does not spawn in the minigame room, even if it should (to prevent cheese). You need to enter the room normally to obtain it.

### Restriction sign
You will notice signs placed somewhere in certain levels. These sign act as a toggle for certain abilities. 
"Passing" the sign will take/give you the ability displayed on it. Depending on the setup, moving back to the sign revokes its effect.
There is no easy ingame way to tell how the sign may grant/remove its effect, so it comes down to practice. 
Generally speaking (at least for non-custom stages) signs grant their effect in the context of the level layout. 
For example, If the challenge requires you to go from left to right and you pass the sign from the left side, it will likely revoke its effect if you exit it on the left side.
While most ones should be obvious, there are a few that may require an extra explanation.
- Damage Penalty: The sign with the broken mask crossed out, will activate a damage penalty. If you take any damage, a score penalty is applied. Note that falling through the hazard at the transition point does respawn you, but doesn't cause damage.
- Checkpoint: The sign with the dream gate will move the "Cancel" dream gate from the start of the level to the sign. If you hazard respawn, you'll land there instead. Only one checkpoint can be active at once. Note that passing the starting point will reset the spawn location to the original start.

### Score penalty
Depending on the minigame and certain objects (like the "Damage penalty" sign) a penalty can be applied to your score. How exactly a score penalty works is covered in each minigames description.

### Initial rules
Certain stages could assign initial rules (like no CDash) right at the start. While technically this information is not visible to the player, the built-in stages place "fake restriction sign" at the start, to communicate initial rules.
Again, this is more a "try out for yourself, what abilities you have" kinda thing.

### Practice Mode
Certain minigames have a practice mode right before the actual minigame. This allows you practice setups, the route etc. While in practice mode, you take no damage (you can still get hit, but take no damage. In case you want to try some damage boost strats)
Also the score doesn't start until the actual run. To leave practice mode, you have to reach the goal platform. In practice mode the goal platform has a dream gate exit instead of a normal trigger, so you can land there safely without the fear of starting the real minigame yet. You can disable the practice mode in the mod settings. Note that disabling practice mode will also skip the preview, regardless if that flag is enabled or not.

## Minigames

### Gorbs Parkour
Reach the goal platform while touching the ground as few times as possible. While in this minigame, any ground movement is prevented (no walking, no dashing). Note that hazard respawn put you at the start on the ground.
You can start this minigame at the Gorb Statue in Howling Cliffs. Your score equals the amount of times, you touched the ground, so 0 is the perfect score.
A score penalty adds 1 extra point the score.

### No Eyes Trial
Reach the goal platform while the room is shrouded in darkness. This minigame uses a "practice mode" to give you some time to develop normalized strats to navigate while your sight is limited.
This minigame does work with a timer. Reach the goal as fast as possible. The timer start upon exiting practice mode, so you're in no rush to learn setups and the route.
A score penalty adds 5/10/15 seconds at the end (depending on difficulty), displayed as red text under the normal timer.

## Custom Stages
This mod does provide "fairly easy" options to create your own stages and share them with other people. **DISCLAIMER: I'm not responsible for any offensive message, that might be caused by custom stages! You can always identify the normal built-in stages, since they are the only stages in the stage selection menu that do not have the author text "(by X)".**

### Playing Custom Stages
Playing custom stages is simple. Close the game (if it is running), go to the "Mods" folder of your Hollow Knight installation, then move into the folder "ArcadeKnight" and then "CustomStages". Here, you can place json files that contain one or more stages. Then start the game. If everything worked fine, you should be able to select the custom stages at their respective minigame. You can also check your modlog.txt file, as it will provide you with more information which files got loaded and which didn't (and why). If you're unsure where you find you Hollow Knight installation/modlog you can always look/ask in the modding Discord of Hollow Knight (or the modding section of the normal HK Discord). When the mod detects a highscore for a custom stage that you might've losted/removed the highscore will still be saved. Unless you delete the save file the record was done on, all recordings will be saved regardless if the stage does still exist.

### Creating Custom Stages
Creating your own custom stages is a bit more complex, but nothing to be scared of. For this I recommend knowledge of: 
- The json format to create the files.
- The DebugMod for stuff like no-clip, reading the current position, showing hitboxes and toggling items.
- UnityExplorerPlus for removing unwanted objects or less time consuming placing of obstacles. Don't worry, you don't need to know much of it. I'll try to explain the needed features simply.

You can always ask in the modding discord of Hollow Knight for help to operate with the Debug and UnityExplorer Mod.

#### Rules
Before stages can be played they have to satisfy certain conditions:
- The name has to be unique. If a stage has the same name as another one, the first one takes priority. Note that the name only has be unique in the intended minigame, not the whole mod.
- All three level (easy, normal and hard) must be provided (meaning a start and endpoint to all three)
- A scene has to be assigned, where the minigame plays out.
If any rule couldn't be satified, it will provide the missing information in the modlog.

#### Format
The custom stages have to be provided as json file(s). You can put one or more per json file.
You can see the json structure here: https://github.com/Korzer420/ArcadeKnight/blob/main/CustomStageFormat.md
Each course allows you to remove objects, create a camera preview path and place extra obstacles.
The obstacle types are:
- CourseObstacle: Spawns a preloaded object (Wingmould, Platform, or Box), that the player can interact with.
- SpikeObstacle: Spawns a small spike element. (WIP)
- RespawnObstacle: Spawns a sign that sets a checkpoint upon entering it.
- GateObstacle: Spawns a gate and a lever, which opens said gate.
- RestrictObstacle: Spawns a sign that toggle a player ability upon entering its hitbox.

#### Tips
Generally speaking, upon understanding the format and copying repeated data, you should be fine creating your level.
The built-in level were also designed with this json system, so you can look up examples yourself (at: https://github.com/Korzer420/ArcadeKnight/tree/main/ArcadeKnight/Resources/Data)
Once you created your json, you can put it in the CustomStage folder (like described in "Playing Custom Stages") and test them.
Here are a few tips, that I used while creating these stages:
- Copy/Paste your course two times in the other difficulties to fill all three required courses, to test quickly and not having to design all three at the same time.
- Use DebugMod for all "easy" coordinate specific things, like the start/end position. Move to the position via NoClip, press F1 and look at your coordinates.
- If you want to create a perfect slidable wall/bridge/ceiling of platforms, each platform has to be 2.7 units away. Knowing this, you can easier place multiple platforms in a row without having to restart the game every time. For example: If you want to create a normal bridge and your first platform is at 20, 11.3 (which you determined by DebugMod), you know that if you want to put a platform right next to it for a perfect bridge, it must be at 22.7, 11.3 (2.7 units further).
- Place a sign always 1 unit above the coordinates that the Knight has on the ground (to correctly show the signs legs).If the Knight stands at the ground at Y coordinate 3.4, the sign should be at 4.4.
- Each rule you establish via "Restrictions" in the CourseData should be telegraphed by a Restriction sign somewhere near the start. The sign itself does nothing as the initial rules already set its value, but it is a better clue for the player.
- Use DebugMods "Show Hitboxes" option at page 5 to look at the area of Restriction and Respawn signs.
- The modlog should provide you with additional information about how your level loaded and what might not be right yet (like invalid data or unknown obstacles)
- Remember that the player can respawn at the start via a hazard/transition. In case you want the player to travel back on their own, set a checkpoint to mess with them.^^
- Use UnityExplorerPlus to find the name of the object you want to delete or to move an object in a sign without the need of resetting the game and adjusting it in the json file.
- Install UnityExplorer(Plus), open the game, go to the scene that you'd like to modify and press F7, then (if done for the first time) click "Options" in the header that appears after a few seconds. Scroll down in the option menu until you find "World Mouse - Inspect Key", click on the arrow below and select a key that you like for inspecting (I personally recommend "Tab"), then save the options. Now you can press that assigned key to let your cursor display you information about the object it hovers over. When pressing Left on the mouse, you can view that element and modify its position/rotation etc. You can also disable them by unchecking the "ActiveSelf" field on the left side of the window. Note that sometimes the object you want to select it blocked by other stuff (like sound, darkness or camera lock areas), just click on your element and deactivate the viewed one over and over again until deactivating the shown object does indeed remove your desired one, then you know that you have the correct object and can view its name + work with it.
- Note that UnityExplorerPlus sometimes does mess with HK UI. When UnityExplorer is active, you might not be able to select a level in the selection menu and be softlock there. Similiar goes for the main menu. Pressing F7 before entering a menu MIGHT prevent this. In the worst case, just Alt F4^^

If you need help understanding some aspects, or would like to request an additional feature (obstacle, sign value etc.), you can always open an issue on this github or talk to the mod developer on the HK Modding discord.
