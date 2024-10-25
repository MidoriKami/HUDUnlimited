# HUD Unlimited
[![Download count](https://img.shields.io/endpoint?url=https://qzysathwfhebdai6xgauhz4q7m0mzmrf.lambda-url.us-east-1.on.aws/HUDUnlimited)](https://github.com/MidoriKami/HUDUnlimited)

HUDUnlimited is a FFXIV/Dalamud Plugin that gives you complete control over your ingame user interface.

![image](https://github.com/user-attachments/assets/ae47778b-b812-4780-be65-383bd8528945)

## How does it work?

The game user interface is a `tree` of User Interface Nodes, these node all must have some basic information to be able to be seen in the correct spots on the screen.

HUDUnlimited allows you to edit this basic information for **any** User Interface Node.

> [!NOTE]  
> The UI is built with parent elements, and child elements.
> 
> For example, moving a parent element will move all the children along with it, as the childrens position is relative to the parent.

## Importing/Exporting Presets

The preset browser allows you to select which edits that you have made and export them into a sharable code.

![image](https://github.com/user-attachments/assets/3b54cec5-57b9-4026-b05a-b9d1d397f74e)

```
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
```
