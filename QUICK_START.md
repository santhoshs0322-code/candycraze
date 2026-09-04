# CandyCraze Google Auth - Quick Start

## Your Credentials ✅

```
Google Client ID: 787050963672-n0c7i29los0rshojcve4ckal097bhagd.apps.googleusercontent.com
Backend URL: https://candycraze.onrender.com/
```

## What You Need to Do Now:

### 1. Get SHA-1 Fingerprint (5 min)

Open **PowerShell** and run:

```powershell
keytool -list -v -keystore "C:\Users\sandy\.android\debug.keystore" -alias androiddebugkey -storepass android -keypass android
```

Look for the line that says **SHA1** and copy the value (looks like: `AA:BB:CC:DD:...`)

### 2. Update Google Cloud Console (5 min)

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Go to **APIs & Services** → **Credentials**
3. Find your Android OAuth Client ID
4. Click **Edit** (pencil icon)
5. In "SHA-1 certificate fingerprints" field, paste your SHA-1
6. Click **Save**

### 3. Setup Unity (20 min)

#### Step 3A: Import Google Play Games Plugin

1. Download from: https://github.com/playgameservices/play-games-plugin-for-unity/releases
2. In Unity: **Assets** → **Import Package** → **Custom Package**
3. Select the `.unitypackage` file and **Import All**

#### Step 3B: Configure Plugin

1. Go to **Window** → **Google Play Games** → **Setup Android**
2. Enter:
   - **Package name:** `com.CandyCraze.Game`
   - **Google Play Games App ID:** (leave blank for now)
3. Click **Setup**

#### Step 3C: Add UI to Home Scene

Create these UI elements in your home scene:

**Top-Right Corner:**
- Button (Call it "ProfileIcon")
  - Position: Top-right
  - Size: Small (50x50)
  - Image/Icon: A user avatar icon

**Overlay Panel (initially hidden):**
- Panel (Call it "LoginPanel")
  - Position: Center or bottom
  - Contains:
    - Text: "Logged in as: [Name]"
    - Button: "Sign Out"
    - Button: "Sign In" (if not logged in)

#### Step 3D: Attach Scripts

1. Create empty GameObjects in your scene:
   - `GoogleAuthManager`
   - `CloudSaveManager`
   - `LoginUIManager`

2. Add components:
   - Select `GoogleAuthManager` → **Add Component** → **GoogleAuthManager.cs**
   - Select `CloudSaveManager` → **Add Component** → **CloudSaveManager.cs**
   - Select `LoginUIManager` → **Add Component** → **LoginUI.cs**

3. Link UI elements:
   - In `LoginUIManager` (LoginUI component):
     - Drag ProfileIcon Button into "Profile Icon Button" field
     - Drag LoginPanel into "Login Panel" field
     - Drag Status Text into "Status Text" field
     - Drag Sign In Button into "Sign In Button" field
     - Drag Sign Out Button into "Sign Out Button" field

### 4. Build & Test (10 min)

1. **File** → **Build Settings**
2. Select **Android**
3. **Player Settings** → Set:
   - **Company Name:** CandyCraze
   - **Product Name:** CandyCraze
   - **Bundle Identifier:** `com.CandyCraze.Game`
4. **Build and Run** on your Android device

### 5. Test Login

1. App launches on device
2. Tap the profile icon (top-right)
3. Tap "Sign In"
4. Google login screen appears
5. Sign in with your Google account
6. Profile icon shows your name ✅

---

## Using Cloud Save in Your Game Code

### Upload Save:

```csharp
// When player finishes a level
string saveData = JsonUtility.ToJson(gameState);
CloudSaveManager.Instance.UploadSave(saveData);

// Listen for result
CloudSaveManager.Instance.OnSaveComplete += (success, msg) => {
    if (success) {
        Debug.Log("Save uploaded to cloud!");
    } else {
        Debug.Log("Save failed: " + msg);
    }
};
```

### Download Save:

```csharp
// When app starts
CloudSaveManager.Instance.DownloadSave();

// Listen for result
CloudSaveManager.Instance.OnLoadComplete += (success, msg) => {
    if (success) {
        GameState gameState = JsonUtility.FromJson<GameState>(msg);
        Debug.Log("Save loaded from cloud!");
    } else {
        Debug.Log("Load failed: " + msg);
    }
};
```

---

## Important Notes

✅ **Saves are only uploaded/downloaded when device is ONLINE**
- Check is automatic (network reachability)
- If offline, save is queued locally
- Retries automatically when connection returns

✅ **Token is auto-managed**
- GoogleAuthManager handles token refresh
- CloudSaveManager sends token with every request
- Backend verifies token and returns only that user's save

✅ **Multiple devices**
- Each Google account has ONE cloud save in database
- If you sign in on 2 devices, they share the same save
- Last upload wins (overwrite behavior)

---

## Troubleshooting

| Issue | Solution |
|-------|----------|
| "Sign in failed" | Check SHA-1 matches in Google Cloud Console |
| "Network error 401" | Token not being sent correctly (check firebase token) |
| "App crashes on startup" | Plugin not imported - check Assets/GooglePlayGames folder exists |
| "Save says offline" | WiFi/mobile data turned off - turn on and retry |
| "Can't download save" | No previous save exists for this account (expected first time) |

---

## Files You Have

- ✅ `GoogleAuthManager.cs` — Handles login
- ✅ `CloudSaveManager.cs` — Handles cloud save/load
- ✅ `LoginUI.cs` — Manages UI
- ✅ Backend running at `https://candycraze.onrender.com/`
- ✅ Google OAuth credentials created

---

## Next: Integrate into Your Game

Once you've tested login/logout working, integrate cloud save into your game loop:

1. **On Level Complete:**
   ```csharp
   gameState.levelCompleted = levelNumber;
   gameState.score = playerScore;
   CloudSaveManager.Instance.UploadSave(JsonUtility.ToJson(gameState));
   ```

2. **On Game Start:**
   ```csharp
   if (GoogleAuthManager.Instance.IsAuthenticated()) {
       CloudSaveManager.Instance.DownloadSave();
   }
   ```

---

## Support

For detailed setup instructions, see: `GOOGLE_AUTH_SETUP.md`

Good luck! 🚀
