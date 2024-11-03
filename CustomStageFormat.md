The core object is CourseMetaData, which looks like this:
```
{
  "Name": "MyCoolCustomStage",
  "Author": "HornetIsVoid",
  "Scene": "Tutorial_01",
  "Minigame": 0,
  "EasyCourse": {...},
  "NormalCourse": {...},
  "HardCourse": {...}
}
```
| Field | Type | Description |
|-------|-------|------------|
| Name | string | The name of the stage, must be unique in the minigame! |
| Author | string | Your name. It will be displaced while in the selection screen of that minigame. This field is optional|
| Scene | string | The room name, the stage takes place in. |
| EasyCourse | CourseData | The course of the easy difficulty. |
| NormalCourse | CourseData | The course of the normal difficulty. |
| HardCourse | CourseData | The course of the hard difficulty. |

**All three difficulty levels must be provided!**

Under the three course field, the actual stage data will be provided. The format looks like this
```
{
  "StartPositionX": 24.4,
  "StartPositionY": 4.4,
  "EndPositionX": 50.4,
  "EndPositionY": 5.4,
  "ObjectsToRemove":
  [
    "Object1",
    "Object2"
  ],
  "Restrictions":
  [
    "hasSuperDash"
  ],
  "PreviewPoints":
  [
    {"Item1": 23.4, "Item2": 12.2},
    {"Item1": 48.4, "Item2": 17.1},
  ],
  "Obstacles":
  [
    ...
  ]
}
```
| Field | Type | Description |
|-------|------|-------------|
| StartPositionX | float | The x coordinate of the start position. Should be on the ground to spawn the "Cancel dream gate" correctly. |
| StartPositionY | float | The y coordinate of the start position. Should be on the ground to spawn the "Cancel dream gate" correctly. |
| EndPositionX | float | The x coordinate of the end position. |
| EndPositionY | float | The y coordinate of the end position. |
| ObjectsToRemove | List of string | The name of objects you'd like to remove from the minigame scene (like tolls, breakable walls and such) |
| Restrictions | List of string | Initial rules that should be placed upon the player. Each field that you place here, will its value set to false. For example, if you put "hasSuperDash" in here, the player cannot CDash at the start of the minigame.|
| PreviewPoints | List of float Tuples | Lists of coordinates that the camera pans to in the preview sequence (in order). "Item1" is the x coordinate, while "Item2" is the y coordinate. Note that the end position is automatically added to the preview points.|
| Obstacles | Various Obstacle Types | Obstacles/Objects placed in the level.|

Obstacles are the more annoying elements to implement, but also cover basically all extra elements.
There are currently 5 different types of obstacles.
Here is a list of obstacle examples
```
{
  "$type": "ArcadeKnight.Obstacles.RestrictObstacle, ArcadeKnight",
  "XPosition": 77.77,
  "YPosition": 53.4,
  "SetValue": true,
  "AffectedAbility": "damagePenalty",
  "RevertDirection": 2,
  "Height": 4,
  "Width": 2,
  "Rotation": 0
},
{
  "$type": "ArcadeKnight.Obstacles.GateObstacle, ArcadeKnight",
  "XPosition": 116.3,
  "YPosition": 65.4,
  "Rotation": 0,
  "GateXPosition": 26.66,
  "GateYPosition": 17.48,
  "GateRotation": 90
},
{
  "$type": "ArcadeKnight.Obstacles.RespawnObstacle, ArcadeKnight",
  "XPosition": 123.21,
  "YPosition": 63.5,
  "Height": 4,
  "Width": 2
},
{
  "$type": "ArcadeKnight.Obstacles.SpikeObstacle, ArcadeKnight",
  "XPosition": 39.78,
  "YPosition": 7.4
},
{
  "$type": "ArcadeKnight.Obstacles.CourseObstacle, ArcadeKnight",
  "XPosition": 59.13,
  "YPosition": 25.49,
  "ObjectName": "Wingmould",
  "Rotation": 45
}
```
| Field | Type | Description |
|-------|------|-------------|
| $type | string | The type of element. All are written like this "ArcadeKnight.Obstacles.X, ArcadeKnight", while "X" is the specific type. Either "SpikeObstacle", "CourseObstacle", "GateObstacle", "RespawnObstacle", "RestrictObstacle" or "ImposterObstacle"|
| XPosition | float | The x coordinate of the obstacle.|
| YPosition | float | The y coordinate of the obstacle.|
| Rotation | float | The rotation of the obstacle.|
| **CourseObstacles**|||
| ObjectName | string | The name of the object you'd like to spawn. Currently only support "Platform", "Wingmould" and "Block".|
| **RespawnObstacle** |||
| Height| float | The heigth of the hitbox.|
| Width | float | The width of the hitbox.|
| ActivatedOnce | bool | If true, the checkpoint can only activate one time.|
| HorizontalOffset | float | The horizontal offset of the hitbox, does not affect the sign sprite.|
| VerticalOffset | float | The vertical offset of the hitbox, does not affect the sign sprite.|
| **GateObstacle**|||
| GateXPosition | float | The x coordinate of the gate. (The normal XPosition property is the x coordinate of the lever) |
| GateYPosition | float | The y coordinate of the gate. (The normal YPosition property is the y coordinate of the lever) |
| GateRotation | float | The rotation of the gate. (The normal Rotation property is the rotation of the lever) |
| **RestrictObstacle** |||
| Height| float | The heigth of the hitbox. If this is not set, the height will be either 1 (if "RevertDirection" is Up or Down), or indefinitely (if "RevertDirection" is Left or Right). If "RevertDirection" is "None" this will remain 0. |
| Width | float | The width of the hitbox. If this is not set, the width will be either indefinitely (if "RevertDirection" is Up or Down), or 1 (if "RevertDirection" is Left or Right). If "RevertDirection" is "None" this will remain 0.|
| AffectedAbility | string | The ability that you want to set. Must be one of: "canDash", "hasWalljump", "hasDoubleJump", "hasSuperDash", "damagePenalty", "hasAcidArmour", "hasDashSlash", "hasGreatSlash", "hasCycloneSlash", "canFocus", "canFireball", "canDive", "canScream".|
| SetValue | bool | The value you want "AffectedAbility" to set.|
| RevertDirection | int | The direction at which the sign should revert its effect. For example, if this is 0 (left), exiting the sign through the left side (when this effect was triggered already), will revoke its effect. Left = 0, Up = 1, Right = 2, Down = 3, None = 4. If "None" (4) the sign can only be activated one time. |
| HorizontalOffset | float | The horizontal offset of the hitbox, does not affect the sign sprite.|
| VerticalOffset | float | The vertical offset of the hitbox, does not affect the sign sprite.|
| **ImposterObstacle**| | Xeros Mirror World only!|
| AlwaysReal | bool | If true, this object will never be modified. At least 5/7/10 imposter obstacles with this set to "false" have to exist (depending on difficulty).|
