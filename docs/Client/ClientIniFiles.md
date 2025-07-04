# Client INI Files

Marvel Heroes configuration files are located in: `C:\Users\User Name\Documents\My Games\Marvel Heroes\MarvelGame\Config`.

## Enabling WASD Movement

One of the requested features in Marvel Heroes is movement using the WASD keys. Fortunately, Unreal Engine 3 is flexible enough to allow this without any external software - just a few changes to the INI files.

The file that manages keybindings is called: `MarvelInputUserSettings.ini`. You can find information about the general INI file syntax in the [official documentation](https://docs.unrealengine.com/udk/Three/ConfigurationFiles.html#File%20Format).

* Lines starting with `!` clear keybinds and can be ignored.
* Lines starting with `+` define keybinds and must be modified to change the controls.

### Example: Changing Arrow Keys to WASD-style Movement

By default, the arrow keys are used to scroll the map, which isn’t essential. You can repurpose them for movement controls using these commands:

* `MoveForward`
* `MoveBackward`
* `StrafeLeft`
* `StrafeRight`

These are self-explanatory and correspond to moving your character. Look for the following section in `MarvelInputUserSettings.ini` (note: a similar section starting with `!` may appear - ignore those lines as mentioned above):

```
+Bindings=(Name="Up",Command="MapShiftUpExec | OnRelease MapShiftUpStopExec",Control=False,Shift=False,Alt=False,bIgnoreCtrl=False,bIgnoreShift=False,bIgnoreAlt=False)
+Bindings=(Name="Down",Command="MapShiftDownExec | OnRelease MapShiftDownStopExec",Control=False,Shift=False,Alt=False,bIgnoreCtrl=False,bIgnoreShift=False,bIgnoreAlt=False)
+Bindings=(Name="Left",Command="MapShiftLeftExec | OnRelease MapShiftLeftStopExec",Control=False,Shift=False,Alt=False,bIgnoreCtrl=False,bIgnoreShift=False,bIgnoreAlt=False)
+Bindings=(Name="Right",Command="MapShiftRightExec | OnRelease MapShiftRightStopExec",Control=False,Shift=False,Alt=False,bIgnoreCtrl=False,bIgnoreShift=False,bIgnoreAlt=False)
```

Replace that section with the following:

```
+Bindings=(Name="Up",Command="MoveForward | OnRelease MoveForward",Control=False,Shift=False,Alt=False,bIgnoreCtrl=False,bIgnoreShift=False,bIgnoreAlt=False)
+Bindings=(Name="Down",Command="MoveBackward | OnRelease MoveBackward",Control=False,Shift=False,Alt=False,bIgnoreCtrl=False,bIgnoreShift=False,bIgnoreAlt=False)
+Bindings=(Name="Left",Command="StrafeLeft | OnRelease StrafeLeft",Control=False,Shift=False,Alt=False,bIgnoreCtrl=False,bIgnoreShift=False,bIgnoreAlt=False)
+Bindings=(Name="Right",Command="StrafeRight | OnRelease StrafeRight",Control=False,Shift=False,Alt=False,bIgnoreCtrl=False,bIgnoreShift=False,bIgnoreAlt=False)
```

After saving the file and launching the game, your hero should move in the indicated direction when you press the arrow keys. You’re free to bind these commands to other keys - just be careful not to create any conflicts.

For a deeper explanation of the file format used in `MarvelInputUserSettings.ini`, refer to the [official Unreal Engine 3 keybinding documentation](https://docs.unrealengine.com/udk/Three/KeyBinds.html).