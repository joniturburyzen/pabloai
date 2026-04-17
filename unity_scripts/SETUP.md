# Pablo AI — Unity 6 Setup

## 1. Crear proyecto
Unity Hub → New Project → **2D (URP)** → nombre: `PabloAI`

## 2. Importar assets
- Arrastra `sin_fondo_NI_202604131055 (1).png` a `Assets/Sprites/`
- En Inspector: Texture Type = **Sprite (2D and UI)** → Apply

## 3. Crear la escena

### Jerarquía de objetos:
```
Main Camera
Canvas (Screen Space - Overlay, Scaler: Scale with Screen Size 1080×1920)
├── Background          ← Image, color oscuro #1A1A2E
├── PabloContainer      ← RectTransform, anclado al centro-arriba
│   └── PabloImage      ← Image, sprite = pablo.png, Width=400, Height=600
├── ChatPanel           ← Image semitransparente, anclado abajo, ~45% altura
│   ├── MessageScroll   ← ScrollRect, vertical
│   │   └── Viewport
│   │       └── Content ← Vertical Layout Group + Content Size Fitter
│   ├── ThinkingDots    ← TextMeshProUGUI, texto "..."
│   └── InputRow
│       ├── InputField  ← TMP_InputField, placeholder "Escríbele a Pablito..."
│       └── SendButton  ← Button, texto "→"
GameManager             ← Empty GameObject con los 3 scripts
```

## 4. Añadir scripts
Arrastra los 3 .cs a `Assets/Scripts/` y añádelos al GameObject `GameManager`:
- `GeminiService.cs`
- `PabloAnimator.cs`
- `ChatUI.cs`

## 5. Conectar referencias en Inspector

**PabloAnimator:**
- Pablo Rect → PabloImage (RectTransform)
- Pablo Image → PabloImage (Image component)

**ChatUI:**
- Gemini → GameManager
- Pablo → GameManager
- Scroll Rect → MessageScroll
- Message Container → Content (hijo del Viewport)
- Input Field → InputField
- Send Button → SendButton
- Thinking Indicator → ThinkingDots

## 6. Build WebGL
File → Build Settings → WebGL → Switch Platform → Build
Sube la carpeta `Build/` a GitHub Pages o itch.io

## Notas
- La API key ya está en GeminiService.cs
- Si quieres cambiarla: Inspector → GameManager → GeminiService → Api Key
- ThinkingDots: ponlo inactivo por defecto en la escena (SetActive false al inicio)
