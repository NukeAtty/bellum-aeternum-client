# Main Menu (MainMenu.ini) #

The main menu of the client is an `XNAWindow` named `MainMenu`. Its layout, visuals and
extra child controls are configured through an INI file named `MainMenu.ini`.

The file is resolved the same way as every other window INI: the theme-specific copy is
loaded first, falling back to the base resource path. In this repository the DTA theme copy
lives at `DXMainClient/Resources/DTA/MainMenu.ini`.

> [!NOTE]
> The menu buttons and the status labels are *created in code* (`DXMainClient/DXGUI/Generic/MainMenu.cs`).
> The INI file can only **configure** their attributes (position, texture, color, etc.), not add or remove them.
> Anything that is *not* part of the built-in set must be added through the `[ExtraControls]` section.

## Structure #

`MainMenu.ini` is organized into sections. Each section is named after the control it configures.

```
[MainMenu]                 <- the window itself
[ExtraControls]            <- additional child controls (name:TypeName)
[<ControlName>]            <- one section per child control / extra control
```

### `[MainMenu]` section #

Configures the window itself. It supports all attributes of a plain `XNAControl` / `XNAPanel`
(see [Common attributes](#common-attributes) below), most commonly:

```ini
[MainMenu]
Size=1920,1080
DrawBorders=false
```

The `BackgroundTexture` for the window is set in code (`MainMenu.cs`), so it is not normally
configured here.

### `[ExtraControls]` section #

Adds child controls that do not exist in the code. Each entry maps a unique control name to a
registered control type, in the form `Name:TypeName`:

```ini
[ExtraControls]
0=Logo:XNAExtraPanel
1=txtVersion:XNALabel
2=btnRankedMatch:XNALinkButton
3=AnimatedBackground:XNAAnimatedBackground
```

The keys (`0`, `1`, ...) only need to be unique; they are not used by the client. The
control is then configured in its own `[Name]` section. Extra controls are drawn *behind* the
built-in controls (they receive a negative `DrawOrder` automatically).

## Built-in controls #

These names are fixed and are always created by the code. Configure them via their own sections.

| Name               | Type        | Purpose                                    |
| ------------------ | ----------- | ------------------------------------------ |
| `btnNewCampaign`   | Button      | Starts a new campaign                      |
| `btnLoadGame`      | Button      | Opens the load-game window                 |
| `btnSkirmish`      | Button      | Opens the skirmish lobby                   |
| `btnCnCNet`        | Button      | Switches to the CnCNet lobby               |
| `btnLan`           | Button      | Opens the LAN lobby                        |
| `btnOptions`       | Button      | Opens the options window                   |
| `btnMapEditor`     | Button      | Launches the map editor                    |
| `btnStatistics`    | Button      | Opens the statistics window                |
| `btnCredits`       | Button      | Opens the credits URL                      |
| `btnExtras`        | Button      | Opens the extras window                    |
| `btnExit`          | Button      | Exits the client                           |
| `lblCnCNetStatus`  | Label       | "Players Online:" caption                  |
| `lblCnCNetPlayerCount` | Label   | Player count number                        |
| `lblVersion`       | Link label  | Game version (clickable)                   |
| `lblUpdateStatus`  | Link label  | Update status (clickable)                  |

## Common attributes #

These attributes are understood by every control (`XNAControl` base class) and can be used in
any section, including `[MainMenu]`.

| Attribute                | Description                                                        |
| ------------------------ | ------------------------------------------------------------------ |
| `Location=x,y`           | Position of the control (top-left corner). Aliases: `X=x`, `Y=y`.  |
| `Size=w,h`               | Size of the control. Aliases: `Width=w`, `Height=h`.               |
| `Text=...`               | Text content. `@` is replaced by a newline.                        |
| `Visible=true/false`     | Visibility; also toggles `Enabled` when parsed.                    |
| `Enabled=true/false`     | Whether the control accepts input.                                 |
| `DrawOrder=n`            | Z-order relative to sibling controls (lower = drawn first/behind). |
| `UpdateOrder=n`          | Update order relative to siblings.                                 |
| `RemapColor=r,g,b`       | Tint applied to the control (and its texture).                     |
| `DistanceFromRightBorder=n`  | Positions the control `n` pixels from the parent's right edge. |
| `DistanceFromBottomBorder=n` | Positions the control `n` pixels from the parent's bottom edge. |
| `FillWidth=n`            | Stretches the control to fill the remaining width minus `n`.       |
| `FillHeight=n`           | Stretches the control to fill the remaining height minus `n`.      |
| `ControlDrawMode=UniqueRenderTarget` | Draws the control into its own render target.        |

## Control types #

### `XNAClientButton` (button) #

Used by the built-in menu buttons and available via `ExtraControls`. It is an `XNAButton` with
tooltip support.

| Attribute             | Description                                                        |
| --------------------- | ------------------------------------------------------------------ |
| `IdleTexture`         | Texture shown when not hovered. Also sets the control size.        |
| `HoverTexture`        | Texture shown while hovered.                                       |
| `HoverSoundEffect`    | Sound played on hover, e.g. `Audio/SE/MainMenu/button.wav`.        |
| `ClickSoundEffect`    | Sound played on click.                                             |
| `TextColorIdle`       | Text color when idle.                                              |
| `TextColorHover`      | Text color when hovered.                                           |
| `FontIndex`           | Font index from `Fonts.ini`.                                       |
| `Text`                | Button caption (`@` = newline).                                    |
| `AdaptiveText`        | Whether the text size adapts to the texture (default `true`).      |
| `AlphaRate`           | Alpha transition rate for the hover animation.                     |
| `TextShadowDistance`  | Distance of the text shadow.                                       |
| `MatchTextureSize`    | Sets `Width`/`Height` to the idle texture size.                    |
| `ToolTip`             | Tooltip text shown on hover.                                       |

If `IdleTexture`/`HoverTexture`/`HoverSoundEffect` are omitted, the client falls back to
`<width>pxbtn.png`, `<width>pxbtn_c.png` and `Audio/SE/button.wav` respectively.

### `XNALinkButton` (link button) #

A button that launches a program or URL on click. Inherits everything from `XNAClientButton`
plus:

| Attribute | Description                                  |
| --------- | -------------------------------------------- |
| `URL`     | Path/URL launched on click (Windows/macOS).  |
| `UnixURL` | Path/URL launched on click on Linux.         |
| `Arguments` | Command-line arguments for the URL.       |

### `XNAExtraPanel` (panel) #

A non-interactive panel that auto-sizes to its background texture.

| Attribute          | Description                                                        |
| ------------------ | ------------------------------------------------------------------ |
| `BackgroundTexture`| Texture to draw. If `Width`/`Height` are zero, sizes to the texture. |

### `XNALabel` (label) #

| Attribute          | Description                                  |
| ------------------ | -------------------------------------------- |
| `Text`             | Label text (`@` = newline).                  |
| `RemapColor`/`TextColor` | Text color (e.g. `45,228,255`).         |
| `FontIndex`        | Font index from `Fonts.ini`.                 |
| `AnchorPoint=x,y`  | Anchor used together with `TextAnchor`.      |
| `TextAnchor`       | Anchor mode, e.g. `LEFT`, `CENTER`, `RIGHT`. |
| `TextShadowDistance` | Distance of the text shadow.              |

### `XNALinkLabel` / `XNAClientLinkLabel` (link label) #

A label that acts as a hyperlink. `XNAClientLinkLabel` additionally plays sounds and shows a
tooltip.

| Attribute          | Description                                     |
| ------------------ | ----------------------------------------------- |
| `Text`             | Label text.                                     |
| `IdleColor`        | Text color when idle.                           |
| `HoverColor`       | Text color when hovered.                        |
| `DrawUnderline`    | Whether the text is underlined (default `true`).|
| `ToolTip`          | Tooltip text (client link label only).          |
| `URL`              | URL opened on click (client link label only).   |
| `UnixURL`          | URL opened on click on Linux (client link label only). |
| `HoverSoundEffect` | Sound played on hover (client link label only). |
| `ClickSoundEffect` | Sound played on click (client link label only). |

### `XNAAnimatedBackground` (animated background) #

A modder-defined control that draws a stack of PNG layers. Each layer can independently move,
rotate around a configurable pivot and have its own z-order.

The control itself is configured with:

| Attribute | Description                              |
| --------- | ---------------------------------------- |
| `Size=w,h`| Size of the control (defines the coordinate space and the `Loop` wrap bounds). |

Layers are declared with `LayerN` keys, where `N` is any non-negative integer. Two value
formats are supported:

Positional shorthand (backwards compatible):

```
Layer0=texture.png,x,y,speed,axisX,axisY[,zIndex]
```

Named properties (recommended):

```
Layer0=texture.png
Layer0.Position=640,360
Layer0.Velocity=10,0
Layer0.RotationSpeed=15
Layer0.Axis=0,0
Layer0.ZIndex=1.5
Layer0.Alpha=0.8
Layer0.Scale=1,1
Layer0.Loop=true
```

Layer property reference:

| Property               | Description                                                                                     |
| ---------------------- | ----------------------------------------------------------------------------------------------- |
| `LayerN` (bare)        | The texture path, e.g. `MainMenu/radar.png`.                                                    |
| `LayerN.Position=x,y`  | Position of the texture's **center**, relative to the control (0,0 = top-left).                  |
| `LayerN.Velocity=vx,vy`| Movement speed in pixels per second.                                                            |
| `LayerN.RotationSpeed=s`| Rotation speed in degrees per second (positive = clockwise).                                    |
| `LayerN.Axis=ax,ay`    | Rotation pivot offset from the texture's center, in pixels. `0,0` rotates around the center.     |
| `LayerN.ZIndex=z`      | Explicit draw order; higher values are drawn on top. Defaults to the layer index.               |
| `LayerN.Alpha=a`       | Opacity multiplier from 0 to 1.                                                                  |
| `LayerN.Scale=sx,sy`   | Scale multiplier (or `Scale=s` for uniform scaling). Defaults to 1.                             |
| `LayerN.Loop=true/false`| If enabled, a moving layer wraps around the control's bounds for seamless parallax. Default `false`. |

Notes:

- Layers are sorted by `ZIndex` before drawing, so z-order is independent of the order in which
  the `LayerN` keys appear in the INI.
- `Position` is the texture center. To anchor a specific point of the texture to a fixed screen
  coordinate, combine `Position` and `Axis` (the anchored point lands at `Position + Axis`).
- The control's z-order relative to sibling controls is the standard `DrawOrder` attribute.

## Example #

```ini
[MainMenu]
Size=1920,1080
DrawBorders=false

[ExtraControls]
0=Logo:XNAExtraPanel
1=txtVersion:XNALabel
2=btnRankedMatch:XNALinkButton
3=AnimatedBackground:XNAAnimatedBackground

[Logo]
Location=369,8
BackgroundTexture=MainMenu/Logo.png

[txtVersion]
Text=Version:
RemapColor=45,228,255
Location=990,91

[btnRankedMatch]
Location=490,423
IdleTexture=MainMenu/rankedmatch.png
HoverTexture=MainMenu/rankedmatch_c.png
HoverSoundEffect=MainMenu/button.wav
URL=CnCNetQM.exe

[AnimatedBackground]
Size=1920,1080

; Rotating radar whose bottom-edge center is pinned to the screen center (960,540).
; radar.png is 753x527, so Axis.Y = 527/2 = 263.5 and Position.Y = 540 - 263.5 = 276.5.
Layer1=MainMenu/radar.png
Layer1.Position=960,276.5
Layer1.RotationSpeed=30
Layer1.Axis=0,263.5
Layer1.ZIndex=1

; Static panel drawn above the radar.
Layer2=MainMenu/mainmenupnl.png
Layer2.Position=960,540
Layer2.ZIndex=2
```
