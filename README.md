# Quiet on the Set
Prevent someone from turning up the volume on a Windows machine beyond a maximum volume

This is a very simple Windows Forms application that lets you set a maximum value on for your volume, and then lock the setting so that a user cannot change the volume.

*Disclaimer: The "lock" provides a basic level of control, but it could be easily bypassed by someone with technical knowledge.*

![](https://i.imgur.com/RFbVDvX.png)

Based on the [original code](https://github.com/troylar/quiet-on-the-set) by Troy Larson, with a few improvements:
* Single instance check
* Command line arguments
* New icon that shows the lock status
* Context menu on the tray icon
* Ability to hide notifications

## Usage ##
1. Run the [installer](https://github.com/RenOfHeavens/quiet-on-the-set/releases)
2. Use the slider to pick the maximum volume
3. At this point, you can choose to use a password, or you can just leave it blank and click **Lock**. If you don't set a password, you only need to click **Unlock** to unlock it again.
4. At this point, you can close the app and it will minimize to the system tray. **The only way you can leave the app is to click the Exit button when it's unlocked.**
5. If you want this to run every time the current user logs in, click the *Start Automatically on Login* checkbox

## Command Line Arguments ##
`lock` Lock the maximum volume

`maxvol=50` Set the maximum volume to 50%

`min` Start Minimized

`quiet` Hide Notifications

**Example Usage:**
```
QuietOnTheSetUI.exe -lock -maxvol=20
QuietOnTheSetUI.exe /lock /min /maxvol=30 
QuietOnTheSetUI.exe maxvol=40 lock quiet min
```
